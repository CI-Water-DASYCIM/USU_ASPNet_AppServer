using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

            try
            {
                //string targetPackageDirPath = Path.Combine(targetPackageRootDirPath, packageID);
                string zipUEBPackageFile = Path.Combine(targetPackageDirPath, packageZipFileName);

                 // make sure the package zip file exists
                if (!File.Exists(zipUEBPackageFile))
                {
                    string errMsg = string.Format("No package file ({0}) was found.", zipUEBPackageFile);
                    logger.Error(errMsg);
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
                }
                
                // load the zip file to memory
                //MemoryStream ms = new MemoryStream();

                //using (FileStream file = new FileStream(zipUEBPackageFile, FileMode.Open, FileAccess.Read)) 
                //{
                //    file.CopyTo(ms); // this causes memory error for large package file and hence the whole using block was commented out
                //    file.Close();
                //    if (file.Length == 0)
                //    {
                //        string errMsg = "No package zip file was found.";
                //        logger.Error(errMsg);
                //        response = Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
                //        return response;
                //    }
                //    response.StatusCode = HttpStatusCode.OK;
                //    ms.Position = 0; 
                //    response.Content = new StreamContent(ms);
                //    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                //    response.Content.Headers.ContentLength = file.Length;
                //    // Ref: http://weblogs.asp.net/cibrax/archive/2011/04/25/implementing-caching-in-your-wcf-web-apis.aspx
                //    // set the browser to cache this response for 120 secs only
                //    response.Content.Headers.Expires = DateTime.Now.AddSeconds(120);
                //    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                //    {
                //        FileName = packageZipFileName
                //    };

                //    logger.Info(string.Format("UEB package zip file for PackageID: {0} was sent to the client.", packageID));
                //}

                // New code replaced the old code above to avoid out of memory exception in case of large package zip file
                FileStream fileStream = File.Open(zipUEBPackageFile, FileMode.Open, FileAccess.Read);
                response.StatusCode = HttpStatusCode.OK;
                //file.Position = 0; //ms.Position = 0
                response.Content = new StreamContent(fileStream); //ms
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
                // end of new code

                // once the client gets the package delete the package dir
                // make sure unintentaionally we are not deleting any necessary folder
                if (targetPackageRootDirPath.Contains(packageID))
                {
                    //Directory.Delete(targetPackageRootDirPath, true);
                }
                
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
