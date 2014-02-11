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
using UWRL.CIWaterNetServer.DAL;
using UWRL.CIWaterNetServer.Helpers;
using UWRL.CIWaterNetServer.Models;

namespace UWRL.CIWaterNetServer.Controllers
{
    public class UEBModelRunOutputController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public HttpResponseMessage GetModelRunOutput(string uebRunJobID)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            ServiceContext db = new ServiceContext();

            string modelRunRootPath = string.Empty;
            string modelRunOutputZipFile = string.Empty;
            string uebRunStatusFileName = UEB.UEBSettings.UEB_RUN_STATUS_FILE_NAME;
            string uebRunStatusFile = string.Empty;

            if (string.IsNullOrEmpty(uebRunJobID))
            {
                string errMsg = string.Format("No UEB run job ID was provided");
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent(errMsg);
                return response;
            }

            modelRunRootPath = Path.Combine(UEB.UEBSettings.WORKING_DIR_PATH, uebRunJobID, UEB.UEBSettings.UEB_RUN_FOLDER_NAME);
            modelRunOutputZipFile = Path.Combine(modelRunRootPath, "outputszip", UEB.UEBSettings.UEB_RUN_OUTPUT_ZIP_FILE_NAME);

            ServiceLog serviceLog = db.ServiceLogs.FirstOrDefault(sl => sl.JobID == uebRunJobID);

            if (serviceLog == null)
            {
                string errMsg = string.Format("No UEB run job was found for the provided job ID: {0}.", uebRunJobID);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            if (serviceLog.RunStatus != RunStatus.Success)
            {
                string errMsg = string.Empty;

                if (serviceLog.RunStatus == RunStatus.InQueue)
                {
                    errMsg = string.Format("UEB run request still in a queue to be processed for job ID: {0}.", uebRunJobID);
                }
                else if (serviceLog.RunStatus == RunStatus.Processing)
                {
                    errMsg = string.Format("UEB run request is currently being processed for job ID: {0}.", uebRunJobID);
                }
                else if (serviceLog.RunStatus == RunStatus.Error)
                {
                    errMsg = string.Format("UEB run request failed processing for job ID: {0}.", uebRunJobID);
                }

                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // check model run root path for the given run job exists
            if (Directory.Exists(modelRunRootPath) == false) // if the folder doesnot exist at this point, it means model run output has been already sent to client in the past
            {
                string errMsg = string.Format("UEB model run output has been deleted for the provided job ID: {0}.", uebRunJobID);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }
                                
            try
            {
                FileStream fileStream = File.Open(modelRunOutputZipFile, FileMode.Open, FileAccess.Read);
                response.StatusCode = HttpStatusCode.OK;                    
                response.Content = new StreamContent(fileStream); 
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response.Content.Headers.ContentLength = fileStream.Length;
                    
                // Ref: http://weblogs.asp.net/cibrax/archive/2011/04/25/implementing-caching-in-your-wcf-web-apis.aspx
                // set the browser to cache this response for 120 secs only
                response.Content.Headers.Expires = DateTime.Now.AddSeconds(120);
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = UEB.UEBSettings.UEB_RUN_OUTPUT_ZIP_FILE_NAME
                };

                logger.Info(string.Format("UEB run output zip file for UEB run job ID: {0} was sent to the client.", uebRunJobID));
                    
                // delete temporary working folder used for running ueb model
                Task deleteTask = new Task(() =>
                {
                    // the zip file may be locked untl the client finish reading the 
                    // data from the file stream object
                    while (FileManager.IsFileLocked(new FileInfo(modelRunOutputZipFile)))
                    {
                        // wait a second
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }

                    string modelRunJobIDPath = Path.Combine(UEB.UEBSettings.WORKING_DIR_PATH, uebRunJobID);
                    if (Directory.Exists(modelRunJobIDPath))
                    {
                        Directory.Delete(modelRunJobIDPath, true);
                        logger.Info(string.Format("UEB run temporary folder: {0} was deleted.", modelRunJobIDPath));
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
