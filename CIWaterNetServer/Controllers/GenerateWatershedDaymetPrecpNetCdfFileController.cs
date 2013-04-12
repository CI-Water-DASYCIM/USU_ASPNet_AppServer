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
using UWRL.CIWaterNetServer.Daymet;
using UWRL.CIWaterNetServer.Helpers;

namespace UWRL.CIWaterNetServer.Controllers
{
    public class GenerateWatershedDaymetPrecpNetCdfFileController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
              
        private string _watershedFilePath = string.Empty;
        private string _targetWSDEMFile = string.Empty;
        private string _targetPythonScriptFile = string.Empty; 
        private string _inputPrecpDaymetDataFilePath = string.Empty;
        private string _outNetCDFPrecpDataFilePath = string.Empty;
        private string _outPrecpDataRasterFilePath = string.Empty;

        public GenerateWatershedDaymetPrecpNetCdfFileController()
        {
            if (EnvironmentSettings.IsLocalHost)
            {
                _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\CalculateWatershedDaymetPrecpGDAL.py";
                _inputPrecpDaymetDataFilePath = @"E:\temp\DaymetTimeSeriesData\Logan\precdatasets";
                _outNetCDFPrecpDataFilePath = @"E:\temp\DaymetTimeSeriesData\Logan\precdatasets\OutNetCDF";
                _outPrecpDataRasterFilePath = @"E:\temp\DaymetTimeSeriesData\Logan\precdatasets\Raster3";
                _watershedFilePath = @"E:\CIWaterData\Temp";
            }
            else
            {
                _targetPythonScriptFile = @"C:\CIWaterPythonScripts\CalculateWatershedDaymetPrecpGDAL.py";
                _inputPrecpDaymetDataFilePath = @"C:\CIWaterData\DaymetTimeSeriesData";
                _outNetCDFPrecpDataFilePath = @"C:\CIWaterData\Temp\PrecpData\NetCDF";
                _outPrecpDataRasterFilePath = @"C:\CIWaterData\Temp\PrecpData\Raster";
                _watershedFilePath = @"C:\CIWaterData\Temp";
            }
        }

        public HttpResponseMessage GetWatershedPrecpNetCDFFile(string sourceNetCDFFileNamePatternToMatch)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            var tokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = tokenSource.Token;
            Task<ResponseMessage> task = Task<ResponseMessage>.Factory.StartNew(() => DataProcessor.GetWatershedSinglePrecpDataPointNetCDFFile(cancellationToken, sourceNetCDFFileNamePatternToMatch));
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
