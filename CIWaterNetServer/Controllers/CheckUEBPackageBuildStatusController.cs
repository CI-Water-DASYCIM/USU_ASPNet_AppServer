using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace UWRL.CIWaterNetServer.Controllers
{
    public class CheckUEBPackageBuildStatusController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public HttpResponseMessage GetStatus(string packageID)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            if (string.IsNullOrEmpty(packageID))
            {
                string errMsg = string.Format("No package ID was provided");
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // set the package request processing root dir path
            string packageRequestProcessRootDirPath = Path.Combine(UEB.UEBSettings.WORKING_DIR_PATH, packageID);
            string targetPackageDirPath = Path.Combine(packageRequestProcessRootDirPath, UEB.UEBSettings.PACKAGE_OUTPUT_SUB_DIR_PATH);
            string packageStatusFile = Path.Combine(targetPackageDirPath, UEB.UEBSettings.PACKAGE_BUILD_STATUS_FILE_NAME);
            if (Directory.Exists(targetPackageDirPath) == false)
            {
                string errMsg = string.Format("No UEB package build request was found for the provided package ID: {0}.", packageID);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            if (File.Exists(packageStatusFile) == false)
            {
                string errMsg = string.Format("Internal error:Package status file ({0}) was not found", packageStatusFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            string status = string.Empty;
            using (StreamReader sr = new StreamReader(packageStatusFile))
            {
                status = sr.ReadLine();
            }

            string msg = string.Format("Package build status for packageID: ({0}) is {1} .", packageID, status);
            logger.Info(msg);
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(status);
            return response;
        }
    }
}
