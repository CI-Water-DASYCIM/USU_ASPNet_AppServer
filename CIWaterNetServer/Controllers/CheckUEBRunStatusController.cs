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
    public class CheckUEBRunStatusController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        ServiceContext db = new ServiceContext();

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

            ServiceLog serviceLog = db.ServiceLogs.FirstOrDefault(sl => sl.JobID == uebRunJobID);

            if (serviceLog == null)
            {
                string errMsg = string.Format("No UEB run job was found for the provided UEB run job ID: {0}.", uebRunJobID);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.OK;
                response.Content = new StringContent(RunStatus.Error);
                return response;
            }
            
            string msg = string.Format("UEB run status for run job ID: ({0}) is {1} .", uebRunJobID, serviceLog.RunStatus);
            logger.Info(msg);
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(serviceLog.RunStatus);
            return response;
        }
    }
}
