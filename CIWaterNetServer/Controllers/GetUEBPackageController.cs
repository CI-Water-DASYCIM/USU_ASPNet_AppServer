using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using UWRL.CIWaterNetServer.Helpers;

namespace UWRL.CIWaterNetServer.Controllers
{
    public class GetUEBPackageController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        
        /// <summary>
        /// Sends the package zip file to the client (browser)
        /// Pacakge zip file must pre-exists
        /// </summary>
        /// <param name="packageID">Id of the package</param>
        /// <returns>A zip file containing all input data files and control files</returns>
        public HttpResponseMessage GetPackageDownload(string packageID)
        {
            HttpResponseMessage response = new HttpResponseMessage();

            string targetPackageRootDirPath = Path.Combine(UEB.UEBSettings.WORKING_DIR_PATH, packageID);
            string targetPackageDirPath = Path.Combine(targetPackageRootDirPath, UEB.UEBSettings.PACKAGE_OUTPUT_SUB_DIR_PATH);                
            string packageZipFileName = UEB.UEBSettings.UEB_PACKAGE_FILE_NAME;
            if (string.IsNullOrEmpty(packageID))
            {
                string errMsg = string.Format("No package ID was provided");
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent(errMsg);
                return response;
            }
                                                
            if (Directory.Exists(targetPackageRootDirPath) == false)
            {
                string errMsg = string.Format("No UEB package build request was found for the provided package ID: {0}.", packageID);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            if (Directory.Exists(targetPackageDirPath) == false)
            {
                string errMsg = string.Format("No UEB package was found for the provided package ID: {0}.", packageID);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }
            
            //TODO: before doing the following check if the package zip file exists, check that the package status is not still
            // processing. If it is still processing then return a message accordingle. Only if the package
            // status is "Complete" then do the following check for zipfile exists or not
            try
            {                
                string zipUEBPackageFile = Path.Combine(targetPackageDirPath, packageZipFileName);

                 // make sure the package zip file exists
                if (!File.Exists(zipUEBPackageFile))
                {
                    string errMsg = string.Format("No package file ({0}) was found.", zipUEBPackageFile);
                    logger.Error(errMsg);
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
                }
                                
                FileStream fileStream = File.Open(zipUEBPackageFile, FileMode.Open, FileAccess.Read);
                response.StatusCode = HttpStatusCode.OK;
                response.Content = new StreamContent(fileStream); 
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response.Content.Headers.ContentLength = fileStream.Length;
                // Ref: http://weblogs.asp.net/cibrax/archive/2011/04/25/implementing-caching-in-your-wcf-web-apis.aspx
                // set the browser to cache this response for 120 secs only
                response.Content.Headers.Expires = DateTime.Now.AddSeconds(120);
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = packageZipFileName
                };
                                
                logger.Info(string.Format("UEB package zip file for PackageID: {0} was sent to the client.", packageID));
                                             
                // delete temporary working folder used for creating ueb model package
                Task deleteTask = new Task(() =>
                {
                    // the zip file may be locked untl the client finish reading the 
                    // data from the file stream object
                    while (FileManager.IsFileLocked(new FileInfo(zipUEBPackageFile)))
                    {
                        // wait a second
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                                        
                    if (Directory.Exists(targetPackageRootDirPath))
                    {
                        Directory.Delete(targetPackageRootDirPath, true);
                        logger.Info(string.Format("UEB build package temporary folder: {0} was deleted.", targetPackageRootDirPath));
                    }
                });

                deleteTask.Start();
                
            }
            catch (Exception ex)
            {
                logger.Fatal(ex.Message);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }              
            
            return response;
        }
    }
}
