using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using UWRL.CIWaterNetServer.DAL;
using UWRL.CIWaterNetServer.Daymet;
using UWRL.CIWaterNetServer.Helpers;
using UWRL.CIWaterNetServer.Models;
using UWRL.CIWaterNetServer.UEB;

namespace UWRL.CIWaterNetServer.Controllers
{    
    public class GenerateUEBPackageController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
                
        private string _sourceFilePath = string.Empty;
        private string _targetTempPackageFilePath = string.Empty;
        private string _targetPackageDirPath = string.Empty;
        private string _packageZipFileName = UEB.UEBSettings.UEB_PACKAGE_FILE_NAME; // "UEBPackage.zip";
        private string _clientPackageRequestDirPath = string.Empty;
        private string _packageRequestProcessRootDirPath = string.Empty;
        
        /// <summary>
        /// Creates UEB model package
        /// <remarks>
        /// POST is used since the input data needs to come in as a zip file in the body of the request
        /// and can't be sent as query parameter
        /// </remarks>
        /// </summary>
        /// <returns></returns>
        public HttpResponseMessage PostUEBPackageCreate()
        {
            string uebPackageBuildRequestJson = string.Empty;
            HttpResponseMessage response = new HttpResponseMessage();                        
            Stream uebPackageBuildRequestZipFile = null;
            ServiceContext db = new ServiceContext();
            ServiceLog serviceLog = null;

            var t_stream = this.Request.Content.ReadAsStreamAsync().ContinueWith(s =>
            {
                uebPackageBuildRequestZipFile = s.Result;
            });
            t_stream.Wait();

            // generate a guid to pass on to the client as a job ID
            Guid jobGuid = Guid.NewGuid();

            // set the package request processing root dir path
            _packageRequestProcessRootDirPath = Path.Combine(UEB.UEBSettings.WORKING_DIR_PATH, jobGuid.ToString());

            // set other directory paths necessary for processing the request to build ueb package
            _sourceFilePath = _packageRequestProcessRootDirPath;
            _targetTempPackageFilePath = Path.Combine(_packageRequestProcessRootDirPath, UEB.UEBSettings.PACKAGE_FILES_OUTPUT_SUB_DIR_PATH);
            _targetPackageDirPath = Path.Combine(_packageRequestProcessRootDirPath, UEB.UEBSettings.PACKAGE_OUTPUT_SUB_DIR_PATH);

            // set the path for saving the client request zip file            
            _clientPackageRequestDirPath = Path.Combine(_packageRequestProcessRootDirPath, UEB.UEBSettings.PACKAGE_BUILD_REQUEST_SUB_DIR_PATH);
            string packageRequestZipFile = Path.Combine(_clientPackageRequestDirPath, UEB.UEBSettings.PACKAGE_BUILD_REQUEST_ZIP_FILE_NAME);
            try
            {
                var service = db.Services.First(s => s.APIName == "GenerateUEBPackageController.PostUEBPackageCreate");
                                                               
                serviceLog = new ServiceLog
                {
                    JobID = jobGuid.ToString(),
                    ServiceID = service.ServiceID,
                    CallTime = DateTime.Now,                   
                    RunStatus = RunStatus.InQueue
                };

                db.ServiceLogs.Add(serviceLog);
                db.SaveChanges();

                serviceLog = db.ServiceLogs.First(s => s.JobID == serviceLog.JobID);

                if (Directory.Exists(_packageRequestProcessRootDirPath))
                {
                    Directory.Delete(_packageRequestProcessRootDirPath);
                }

                Directory.CreateDirectory(_packageRequestProcessRootDirPath);

                if (Directory.Exists(_targetTempPackageFilePath))
                {
                    Directory.Delete(_targetTempPackageFilePath);
                }

                Directory.CreateDirectory(_targetTempPackageFilePath);


                if (Directory.Exists(_clientPackageRequestDirPath))
                {
                    Directory.Delete(_clientPackageRequestDirPath);
                }

                Directory.CreateDirectory(_clientPackageRequestDirPath);

                using (var fileStream = File.Create(packageRequestZipFile))
                {
                    uebPackageBuildRequestZipFile.CopyTo(fileStream);
                }

                // unzip the request zip file
                ZipFile.ExtractToDirectory(packageRequestZipFile, _clientPackageRequestDirPath);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent(ex.Message);

                serviceLog.RunStatus = RunStatus.Error;                
                serviceLog.Error = ex.Message;
                db.SaveChanges();

                return response;
            }

            // read the file with json extension to a string - uebPackageBuildRequestJson
            string[] jsonFiles = Directory.GetFiles(_clientPackageRequestDirPath, "*.json");
            if (jsonFiles.Length != 1)
            {
                string errMsg = "Either no json file was found in the package request or there are multiple json files";
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent(errMsg);

                serviceLog.RunStatus = RunStatus.Error;                
                serviceLog.Error = errMsg;
                db.SaveChanges();

                return response;
            }

            using (var fileReader = new StreamReader(jsonFiles[0]))
            {
                uebPackageBuildRequestJson = fileReader.ReadToEnd();
            }

            UEB.UEBPackageRequest pkgRequest;
            try
            {
                pkgRequest = JsonConvert.DeserializeObject<UEB.UEBPackageRequest>(uebPackageBuildRequestJson);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent(ex.Message);

                serviceLog.RunStatus = RunStatus.Error;                
                serviceLog.Error = ex.Message;
                db.SaveChanges();

                return response;
            }

            // check if the client request is valid
            string validationResult = ValidateUEBPackageRequest(pkgRequest);
            if (string.IsNullOrEmpty(validationResult) == false)
            {
                logger.Error(validationResult);
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent(validationResult);

                serviceLog.RunStatus = RunStatus.Error;                
                serviceLog.Error = validationResult;
                db.SaveChanges();

                return response;
            }

            // if the domain file is a shape file, extract the domain zip file to the root processing dir path
            string domainFile = Path.Combine(_clientPackageRequestDirPath, pkgRequest.DomainFileName);

            if (Path.GetExtension(domainFile) == ".zip")
            {
                // unzip the domain shape zip file
                ZipFile.ExtractToDirectory(domainFile, _packageRequestProcessRootDirPath);
            }
            
            // copy all the files with extensions (.dat, .nc) that came with the client uebpackagerequest zip file
            // to the package root processing folder
            string[] files = Directory.GetFiles(_clientPackageRequestDirPath);
            string fileName = string.Empty;
            string destFile = string.Empty;

            foreach (string file in files)
            {
                if (Path.GetExtension(file) == ".nc" || Path.GetExtension(file) == ".dat")
                {
                    // Use static Path methods to extract only the file name from the path.
                    fileName = Path.GetFileName(file);
                    destFile = Path.Combine(_packageRequestProcessRootDirPath, fileName);

                    // Copy the file and overwrite destination file if they already exist.
                    File.Copy(file, destFile, true);                                        
                }
            }
                        
            PackageCreationStatus pkgStatus = new PackageCreationStatus();
            pkgStatus.Message = "UEB package build request is now in a job queue.";
            pkgStatus.PackageID = jobGuid.ToString();
            string jsonResponse = JsonConvert.SerializeObject(pkgStatus, Formatting.Indented);
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(jsonResponse);
            
            //PK:2/11/2014 (Added): start the package build process  - this would start the package build process for this request
            // if no other requests are in queue. Othewise it will start the build process for the build request that
            // has been in queue for the longest time (first-in and first-out principle)
            PackageBuilder uebPkgBuilder = new PackageBuilder();
            int numberOfJobsStarted = uebPkgBuilder.Run();            
            logger.Info("Number of queued jobs started:" + numberOfJobsStarted);

            return response;
        }
                
        #region private methods
        // TODO: Not used. Need to be deleted
        private void CreatePackageBuildStatusFile()
        {            
            if (Directory.Exists(_targetPackageDirPath) == false)
            {
                Directory.CreateDirectory(_targetPackageDirPath);
            }

            string fileName = UEB.UEBSettings.PACKAGE_BUILD_STATUS_FILE_NAME;
            string fileToWriteTo = Path.Combine(_targetPackageDirPath, fileName);
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine(PackageBuildStatus.Processing);
            }
        }

        // TODO: Not used. Need to be deleted
        private void UpdatePackageBuildStatusFile(string pkgBuildStatus)
        {
            string fileName = UEB.UEBSettings.PACKAGE_BUILD_STATUS_FILE_NAME;
            string fileToWriteTo = Path.Combine(_targetPackageDirPath, fileName);
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine(pkgBuildStatus);
            }
        }
                        
        private string ValidateUEBPackageRequest(UEBPackageRequest uebPkgRequest)
        {            
            bool isInputError = false;

            // validate simulation start and end dates
            StringBuilder errMsg = new StringBuilder("Invalid UEB Build Request:");
            errMsg.AppendLine();

            if (uebPkgRequest.StartDate > uebPkgRequest.EndDate)
            {
                errMsg.AppendLine("Simulation end date needs to be a date after start date.");
                isInputError = true;
            }

            List<byte> validTimeStepValues = new List<byte> { 1, 2, 3, 4, 6 };
            // validate timeStep
            if (validTimeStepValues.Contains(uebPkgRequest.TimeStep) == false)
            {
                errMsg.AppendLine("Time step needs to be one of the following values:");
                errMsg.AppendLine("1, 2, 3, 4, 6");
                isInputError = true;
            }

            // validate grid cell size
            if (uebPkgRequest.GridCellSize < 100)
            {
                errMsg.AppendLine("Grid cell size must be at least 100 meters.");
                isInputError = true;
            }

            // validate watershed buffer size
            if (uebPkgRequest.BufferSize < 100)
            {
                errMsg.AppendLine("Watershed buffer size must be at least 100 meters.");
                isInputError = true;
            }

            // check the domain file exists
            if(string.IsNullOrEmpty(uebPkgRequest.DomainFileName))
            {
                errMsg.AppendLine("Domain file is missing in the request.");
                isInputError = true;
            }
            else
            {
                string domainFile = Path.Combine(_clientPackageRequestDirPath, uebPkgRequest.DomainFileName);
                if (File.Exists(domainFile) == false)
                {
                    errMsg.AppendLine("Domain file is missing in the request.");
                    isInputError = true;
                }
            }
            

            // check the parameters file exists
            if (string.IsNullOrEmpty(uebPkgRequest.ModelParametersFileName))
            {
                errMsg.AppendLine("Model parameters file is missing in the request.");
                isInputError = true;
            }
            else
            {
                string paramFile = Path.Combine(_clientPackageRequestDirPath, uebPkgRequest.ModelParametersFileName);
                if (File.Exists(paramFile) == false)
                {
                    errMsg.AppendLine("Model parameters file is missing in the request.");
                    isInputError = true;
                }
            }
           
            // check the output control file exists
            if (string.IsNullOrEmpty(uebPkgRequest.OutputControlFileName))
            {
                errMsg.AppendLine("Output control file is missing in the request.");
                isInputError = true;
            }
            else
            {
                string outputControlFile = Path.Combine(_clientPackageRequestDirPath, uebPkgRequest.OutputControlFileName);
                if (File.Exists(outputControlFile) == false)
                {
                    errMsg.AppendLine("Output control file is missing in the request.");
                    isInputError = true;
                }
            }
            
            // check the aggregated output control file exists
            if (string.IsNullOrEmpty(uebPkgRequest.AggregatedOutputControlFileName))
            {
                errMsg.AppendLine("Aggregated output control file is missing in the request.");
                isInputError = true;
            }
            else
            {
                string aggOutputControlFile = Path.Combine(_clientPackageRequestDirPath, uebPkgRequest.AggregatedOutputControlFileName);
                if (File.Exists(aggOutputControlFile) == false)
                {
                    errMsg.AppendLine("Aggregated output control file is missing in the request.");
                    isInputError = true;
                }
            }           

            if (isInputError)
            {
                return errMsg.ToString();
            }
            else
            {
                return string.Empty;
            }
        }
                        
        #endregion
    }

    public class PackageCreationStatus
    {
        public string Message { get; set; }
        public string PackageID { get; set; }
    }   
    
}
