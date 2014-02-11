using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using UWRL.CIWaterNetServer.UEB;

namespace UWRL.CIWaterNetServer.Controllers
{
    public class RunQueuedJobsController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public HttpResponseMessage GetRunJobs()
        {
            HttpResponseMessage response = new HttpResponseMessage();

            try
            {
                PackageBuilder uebPkgBuilder = new PackageBuilder();
                int numberOfJobsStarted = uebPkgBuilder.Run();
                response.StatusCode = HttpStatusCode.OK;
                response.Content = new StringContent("Number of queued jobs started:" + numberOfJobsStarted);
                logger.Info("Number of queued jobs started:" + numberOfJobsStarted);
            }
            catch (Exception ex)
            {
                logger.Fatal(ex.Message);
                logger.Fatal("One or more queued jobs failed.");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

            return response;
        }
    }
}
