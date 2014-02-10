using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using UWRL.CIWaterNetServer.DAL;
using UWRL.CIWaterNetServer.Helpers;
using UWRL.CIWaterNetServer.Models;

namespace UWRL.CIWaterNetServer.Controllers
{
    public class CheckUEBPackageBuildStatusController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        ServiceContext db = new ServiceContext();

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
            
            ServiceLog serviceLog = db.ServiceLogs.FirstOrDefault(sl => sl.JobID == packageID);

            if (serviceLog == null)
            {
                string errMsg = string.Format("No UEB package build request was found for the provided package ID: {0}.", packageID);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }
                        
            string msg = string.Format("Package build status for packageID: ({0}) is {1} .", packageID, serviceLog.RunStatus);
            logger.Info(msg);
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(serviceLog.RunStatus);
            return response;
        }
    }
}
