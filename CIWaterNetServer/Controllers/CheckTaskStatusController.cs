using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using UWRL.CIWaterNetServer.Daymet;

namespace UWRL.CIWaterNetServer.Controllers
{
    // this service will be used by client for polling the status of a specific task the client requested
    public class CheckTaskStatusController : ApiController
    {
        public HttpResponseMessage GetTaskStatus(string taskID)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            var task = Helpers.TaskDataStore.GetTask(taskID);

            if (task == null)
            {
                response.StatusCode = HttpStatusCode.OK;
                response.Content = new StringContent(string.Format("Task: {0} does not exist. It might have already been processed.", taskID));
            }
            else
            {             
                Task taskObj = task as Task;
                if (taskObj.IsCompleted)
                {
                    var taskResponse = Helpers.TaskDataStore.GetTaskResult(taskID);
                    if (taskResponse is HttpResponseMessage)
                    {
                        response = taskResponse as HttpResponseMessage;
                    }
                    else if(taskResponse is ResponseMessage)
                    {
                        ResponseMessage daymetResponse = taskResponse as ResponseMessage;
                        response = DataProcessor.GetHttpResponse(daymetResponse);
                    }

                    Helpers.TaskDataStore.RemoveResult(taskID);
                    Helpers.TaskDataStore.RemoveTask(taskID);
                                   
                }
                else if (taskObj.IsFaulted)
                {
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    response.Content = new StringContent(string.Format("Task: {0} has been faulted.", taskID));
                    Helpers.TaskDataStore.RemoveResult(taskID);
                    Helpers.TaskDataStore.RemoveTask(taskID);                    
                }
                else if (taskObj.IsCanceled)
                {
                    response.StatusCode = HttpStatusCode.Forbidden;
                    response.Content = new StringContent(string.Format("Task: {0} has been canceled.", taskID));
                    Helpers.TaskDataStore.RemoveResult(taskID);
                    Helpers.TaskDataStore.RemoveTask(taskID);                    
                }
                else
                {
                    response.StatusCode = HttpStatusCode.OK;
                    response.Content = new StringContent(string.Format("Task: {0} is under progress.", taskID));
                }               
            }

            return response;
        }
    }
}
