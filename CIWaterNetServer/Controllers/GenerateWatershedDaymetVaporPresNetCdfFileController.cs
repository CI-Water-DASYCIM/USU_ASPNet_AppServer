using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using UWRL.CIWaterNetServer.Daymet;

namespace UWRL.CIWaterNetServer.Controllers
{
    // This service method is only for testing
    public class GenerateWatershedDaymetVaporPresNetCdfFileController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public HttpResponseMessage GetWatershedVpNetCDFFile(string sourceNetCDFFileNamePatternToMatch)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            var tokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = tokenSource.Token;

            Task<ResponseMessage> task = Task<ResponseMessage>.Factory.StartNew(() => DataProcessor.GetWatershedSingleVaporPresDataPointNetCDFFile(cancellationToken, sourceNetCDFFileNamePatternToMatch));
            Helpers.TaskDataStore.SetTask(task.Id.ToString(), task);
            // do this when this task ends
            task.ContinueWith(t =>
                {                    
                    ResponseMessage daymetResponse = t.Result;
                    Helpers.TaskDataStore.SetTaskResult(task.Id.ToString(), daymetResponse);                   
                   
                });

            // return response while the task is still running
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(string.Format("Task: {0} is under progress.", task.Id));
            return response;                        
        }
    }
}
