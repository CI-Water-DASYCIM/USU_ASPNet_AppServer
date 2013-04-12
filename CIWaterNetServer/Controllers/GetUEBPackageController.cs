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
        private string _targetPackageDirPath = string.Empty;
        private const string _packageZipFileName = "UEBPackage.zip";

        #region constructor
        public GetUEBPackageController()
        {
            if (EnvironmentSettings.IsLocalHost)
            {   
                _targetPackageDirPath = @"E:\CIWaterData\Temp\UEBPackageZip";
            }
            else
            {
                _targetPackageDirPath = @"C:\CIWaterData\Temp\UEBPackageZip";
            }
        }
        #endregion

        /// <summary>
        /// Creates a package zip file and sends it back to the client (browser)
        /// All necessary files that need to be part of the pacakge must pre-exists
        /// </summary>
        /// <param name="packageID">Id of the package</param>
        /// <returns>A zip file containing all input data files and control files</returns>
        public HttpResponseMessage GetPackageDownload(string packageID)
        {
            HttpResponseMessage response = new HttpResponseMessage();

            // if no pacakageID was provided then get the latest one
            // using the latest folder
            if (string.IsNullOrEmpty(packageID))
            {                
                packageID = new DirectoryInfo(_targetPackageDirPath).GetDirectories()
                       .OrderByDescending(d => d.LastWriteTimeUtc).First().Name;
            }
            
            _targetPackageDirPath += @"\" + packageID;
            if (Directory.Exists(_targetPackageDirPath) == false)
            {
                string errMsg = string.Format("Internal error: No package folder ({0}) was found.", _targetPackageDirPath);
                logger.Error(errMsg);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
            }
            
            try{
                //create a zip file of all shapes files
                string zipUEBPackage = Path.Combine(_targetPackageDirPath, _packageZipFileName);

                if (!File.Exists(zipUEBPackage))
                {
                    string errMsg = string.Format("Internal error: No package ({0}) was found.", zipUEBPackage);
                    logger.Error(errMsg);
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
                }
                
                //load the zip file to memory
                MemoryStream ms = new MemoryStream();

                using (FileStream file = new FileStream(zipUEBPackage, FileMode.Open, FileAccess.Read))
                {
                    file.CopyTo(ms);
                    file.Close();
                    if (ms.Length == 0)
                    {
                        string errMsg = "No package zip file was found.";
                        logger.Error(errMsg);
                        response = Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
                        return response;
                    }
                    response.StatusCode = HttpStatusCode.OK;
                    ms.Position = 0;
                    response.Content = new StreamContent(ms);
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    
                    //CacheControlHeaderValue cacheControlHeaderValue = new CacheControlHeaderValue();
                    //cacheControlHeaderValue.NoCache = true;
                    //response.Headers.CacheControl = cacheControlHeaderValue;

                    //Ref: http://weblogs.asp.net/cibrax/archive/2011/04/25/implementing-caching-in-your-wcf-web-apis.aspx
                    //set the browser to cache this response for 120 secs only
                    response.Content.Headers.Expires = DateTime.Now.AddSeconds(120);
                    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = _packageZipFileName
                    };

                    logger.Info("UEB package zip file was sent to the client.");
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
