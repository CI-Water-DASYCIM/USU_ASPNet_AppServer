using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using UWRL.CIWaterNetServer.Helpers;

namespace UWRL.CIWaterNetServer.Controllers
{
    public class CheckUEBRunStatusController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public HttpResponseMessage GetStatus(string uebRunJobID)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            if (string.IsNullOrEmpty(uebRunJobID))
            {
                string errMsg = string.Format("No UEB run job ID was provided.");
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // set the package request processing root dir path
            string modelRunRootPath = Path.Combine(UEB.UEBSettings.WORKING_DIR_PATH, uebRunJobID, UEB.UEBSettings.UEB_RUN_FOLDER_NAME);            
            string uebRunStatusFile = Path.Combine(modelRunRootPath, UEB.UEBSettings.UEB_RUN_STATUS_FILE_NAME);
            
            if (Directory.Exists(modelRunRootPath) == false)
            {
                string errMsg = string.Format("No UEB run job was found for the provided package ID: {0}.", uebRunJobID);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            if (File.Exists(uebRunStatusFile) == false)
            {
                string errMsg = string.Format("Internal app server error:UEB run status file ({0}) was not found.", uebRunStatusFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            string status = string.Empty;
            using (StreamReader sr = new StreamReader(uebRunStatusFile))
            {
                status = sr.ReadLine();
            }

            if (status == UebRunStatus.Failed)
            {
                // delete the temp dir
                string modelRunRootGuidPath = Path.Combine(UEB.UEBSettings.WORKING_DIR_PATH, uebRunJobID);
                if (Directory.Exists(modelRunRootGuidPath))
                {
                    Directory.Delete(modelRunRootGuidPath, true);
                }
            }

            string msg = string.Format("UEB run status for run job ID: ({0}) is {1} .", uebRunJobID, status);
            logger.Info(msg);
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(status);
            return response;
        }
    }
}
