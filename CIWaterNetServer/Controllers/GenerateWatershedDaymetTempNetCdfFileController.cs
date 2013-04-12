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
    public class GenerateWatershedDaymetTempNetCdfFileController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
               
        private string _watershedFilePath = string.Empty;
        private string _targetWSDEMFile = string.Empty;
        private string _targetPythonScriptFile = string.Empty; 
        private string _inputTempDaymetDataFilePath = string.Empty;
        private string _outNetCDFTempDataFilePath = string.Empty;
        private string _outTempDataRasterFilePath = string.Empty;

        public GenerateWatershedDaymetTempNetCdfFileController()
        {
            if (EnvironmentSettings.IsLocalHost)
            {
                _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\CalculateWatershedDaymetTempGDAL.py";
                _inputTempDaymetDataFilePath = @"E:\temp\DaymetTimeSeriesData\Logan\tempdatasets";
                _outNetCDFTempDataFilePath = @"E:\temp\DaymetTimeSeriesData\Logan\tempdatasets\OutNetCDF";
                _outTempDataRasterFilePath = @"E:\temp\DaymetTimeSeriesData\Logan\tempdatasets\Raster";
                _watershedFilePath = @"E:\CIWaterData\Temp";
            }
            else
            {
                _targetPythonScriptFile = @"C:\CIWaterPythonScripts\CalculateWatershedDaymetTempGDAL.py";
                _inputTempDaymetDataFilePath = @"C:\CIWaterData\DaymetTimeSeriesData";
                _outNetCDFTempDataFilePath = @"C:\CIWaterData\Temp\TempData\NetCDF";
                _outTempDataRasterFilePath = @"C:\CIWaterData\Temp\TempData\Raster";
                _watershedFilePath = @"C:\CIWaterData\Temp";
            }
        }

        public HttpResponseMessage GetWatershedTempNetCDFFile(string outNetCDFDataVariableName, string sourceNetCDFFileNamePatternToMatch)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            CancellationToken ct = new CancellationToken();
          
            Task<ResponseMessage> task = Task<ResponseMessage>.Factory.StartNew(() => DataProcessor.GetWatershedSingleTempDataPointNetCDFFile(ct, outNetCDFDataVariableName, sourceNetCDFFileNamePatternToMatch));
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
