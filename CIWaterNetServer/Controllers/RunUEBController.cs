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
using System.Threading.Tasks;
using System.Web.Http;
using UWRL.CIWaterNetServer.Helpers;


namespace UWRL.CIWaterNetServer.Controllers
{
    public class RunUEBController : ApiController
    {

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public HttpResponseMessage PostRunUEB()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            string modelRunRootPath = string.Empty;
            string uebInputPackageZipFileName = "ueb_input_package.zip";
            string uebInputPackageZipFile = string.Empty;
            string uebExecutableFilesPath = string.Empty;
            string uebExecutionControlFileName = UEB.UEBSettings.UEB_EXECUTION_CONTROL_FILE_NAME;
            string msg = string.Empty;

            // get the ueb model package zip file from the request
            Stream uebPackageZipFileFromClient = null;

            try
            {
                var t_stream = this.Request.Content.ReadAsStreamAsync().ContinueWith(s =>
                {
                    uebPackageZipFileFromClient = s.Result;
                });
                t_stream.Wait();

                msg = string.Format("Input model package zip file was read from the http request.");
                logger.Info(msg);
            }
            catch (Exception ex)
            {
                logger.Fatal(ex.Message);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

            if (uebPackageZipFileFromClient == null)
            {
                string errMsg = "No model package file was received from the client.";
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // generate a guid to pass on to the client as a job ID and use this as part of creating a unique folder
            // for model run output
            Guid jobGuid = Guid.NewGuid();
            modelRunRootPath = Path.Combine(UEB.UEBSettings.WORKING_DIR_PATH, jobGuid.ToString(), UEB.UEBSettings.UEB_RUN_FOLDER_NAME);
            uebInputPackageZipFile = Path.Combine(modelRunRootPath, uebInputPackageZipFileName);
            Directory.CreateDirectory(modelRunRootPath);
            msg = string.Format("Directory ({0}) was created for running ueb model.", modelRunRootPath);
            logger.Info(msg);

            try
            {
                // save the recieved package file locally for ueb to use
                using (var fileStream = File.Create(uebInputPackageZipFile))
                {
                    uebPackageZipFileFromClient.CopyTo(fileStream);
                }

                msg = string.Format("Input model package zip file was saved: {0}.", uebInputPackageZipFile);
                logger.Info(msg);
            }
            catch (Exception ex)
            {
                logger.Fatal(ex.Message);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

            // unzip the request zip file
            ZipFile.ExtractToDirectory(uebInputPackageZipFile, modelRunRootPath);
            //File.Delete(uebInputPackageZipFile);

            // copy the UEB executables and dlls to model run folder
            uebExecutableFilesPath = UEB.UEBSettings.UEB_EXECUTABLE_DIR_PATH;
            string[] files = Directory.GetFiles(uebExecutableFilesPath);
            string fileName = string.Empty;
            string destFile = string.Empty;

            foreach (string file in files)
            {
                // Use static Path methods to extract only the file name from the path.
                fileName = Path.GetFileName(file);
                destFile = Path.Combine(modelRunRootPath, fileName);

                // Copy the file and overwrite destination file if they already exist.
                File.Copy(file, destFile, true);
            }

            //Run ueb in async mode
            RunUEB(modelRunRootPath, uebInputPackageZipFile, jobGuid.ToString());
            UpdateUEBRunStatusFile(modelRunRootPath, UebRunStatus.Processing);

            UebRunStatusResponse uebRunStatus = new UebRunStatusResponse();
            uebRunStatus.Message = "UEB execution has started.";
            uebRunStatus.RunJobID = jobGuid.ToString();
            string jsonResponse = JsonConvert.SerializeObject(uebRunStatus, Formatting.Indented);
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(jsonResponse);
            return response;
        }

        private void RunUEB(string modelRunRootPath, string uebInputPackageZipFile, string runJobID)
        {
            try
            {
                Task uebRunTask = new Task(() =>
                {
                    System.Diagnostics.Process proc = null;
                    List<string> arguments = new List<string>();                    
                    arguments.Add(Path.Combine(modelRunRootPath, "UEBGrid.exe"));                    
                    arguments.Add(UEB.UEBSettings.UEB_EXECUTION_CONTROL_FILE_NAME);

                    // create a string containing all the argument items separated by a space
                    string commandString = string.Join(" ", arguments);
                    object command = commandString;

                    // Create the ProcessStartInfo using "cmd" as the program to be run,
                    // and "/c " as the parameters.
                    // "/c" tells cmd that you want it to execute the command that follows,
                    // then exit.
                    System.Diagnostics.ProcessStartInfo procStartInfo = new
                        System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);

                    procStartInfo.WorkingDirectory = modelRunRootPath;
                    // The following commands are needed to redirect the standard output.
                    // redirect to the Process.StandardOutput StreamReader.
                    procStartInfo.RedirectStandardOutput = true;
                    procStartInfo.RedirectStandardError = true;
                    procStartInfo.UseShellExecute = false;

                    // Do not create the black window.
                    procStartInfo.CreateNoWindow = true;

                    // Now you create a process, assign its ProcessStartInfo, and start it.
                    logger.Info(string.Format("Starting UEB run for job ID: {0}.", runJobID));
                    proc = new System.Diagnostics.Process();
                    proc.StartInfo = procStartInfo;
                    proc.Start();

                    // Get the output into a string.
                    string result = proc.StandardOutput.ReadToEnd();
                    string errors = proc.StandardError.ReadToEnd();
                   
                    logger.Info(string.Format("Ending UEB run for job ID: {0}.", runJobID));

                    if (result.Contains("successfully performed") == false)
                    {
                        string errMsg = string.Format("UEB run failed for job ID:{0}.", runJobID);
                        logger.Error(errMsg);
                        logger.Error(errors);
                        logger.Error(result);
                        UpdateUEBRunStatusFile(modelRunRootPath, UebRunStatus.Failed);
                    }
                    else
                    {
                        string msg = string.Format("UEB run was successful for job ID:{0}", runJobID);
                        logger.Info(msg);
                        string modelOutputZipPath = Path.Combine(modelRunRootPath, "outputszip");
                        string modelOutputPath = Path.Combine(modelRunRootPath, UEB.UEBSettings.MODEL_OUTPUT_FOLDER_NAME);
                        string modelOutputZipFile = Path.Combine(modelOutputZipPath, UEB.UEBSettings.UEB_RUN_OUTPUT_ZIP_FILE_NAME);
                        Directory.CreateDirectory(modelOutputZipPath);
                        ZipFile.CreateFromDirectory(modelOutputPath, modelOutputZipFile);                        
                        msg = string.Format("UEB run output zip file was created for UEB run job ID:{0}", runJobID);
                        logger.Info(msg);
                        UpdateUEBRunStatusFile(modelRunRootPath, UebRunStatus.Complete);
                    }
                });

                uebRunTask.Start();
            }
            catch (Exception ex)
            {
                string errMsg = string.Format("UEB run failed for job ID:{0}.", runJobID);
                logger.Error(errMsg);
                logger.Error(ex.Message);
                UpdateUEBRunStatusFile(modelRunRootPath, UebRunStatus.Failed);
            }
        }

        private void UpdateUEBRunStatusFile(string modelRunRootPath, string uebRunStatus)
        {
            string fileName = UEB.UEBSettings.UEB_RUN_STATUS_FILE_NAME;
            string fileToWriteTo = Path.Combine(modelRunRootPath, fileName);
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine(uebRunStatus);
            }
        }
    }

    public class UebRunStatusResponse
    {
        public string Message { get; set; }
        public string RunJobID { get; set; }
    }
}

