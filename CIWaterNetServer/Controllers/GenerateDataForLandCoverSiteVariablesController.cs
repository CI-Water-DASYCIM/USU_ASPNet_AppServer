using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using UWRL.CIWaterNetServer.Helpers;

namespace UWRL.CIWaterNetServer.Controllers
{
    public class GenerateDataForLandCoverSiteVariablesController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string _inputWatershedFilePath =string.Empty;
        private string _outputWSNLCDFile = string.Empty; 
        private string _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\GenerateLandCoverRelatedSiteVariablesData.py";

        public HttpResponseMessage GetWatershedLandCoverData()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            string clippedWSNLCDFileName = "ws_nlcd_data.img";

            if (EnvironmentSettings.IsLocalHost)
            {
                _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\GenerateLandCoverRelatedSiteVariablesData.py";
                _inputWatershedFilePath = @"E:\CIWaterData\Temp";
            }
            else
            {
                _targetPythonScriptFile = @"C:\CIWaterPythonScripts\GenerateLandCoverRelatedSiteVariablesData.py";
                _inputWatershedFilePath = @"C:\CIWaterData\Temp";
            }

            // if resampled version of the ws DEM file is available, then use that
            _outputWSNLCDFile = Path.Combine(_inputWatershedFilePath, clippedWSNLCDFileName);

            if (!File.Exists(_outputWSNLCDFile))
            {
                string errMsg = "Internal error: No NLCD dataset raster file was found for the watershed.";
                logger.Error(errMsg);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
            }

            try
            {
                List<string> arguments = new List<string>();
                arguments.Add(_outputWSNLCDFile);
                Python.PythonHelper.ExecuteScript(_targetPythonScriptFile, arguments);
                string responseMsg = "Watershed specific land cover related netcdf data files were created.";
                response.Content = new StringContent(responseMsg);
                response.StatusCode = HttpStatusCode.OK;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Info(responseMsg);
            }
            catch (Exception ex)
            {
                response.Content = new StringContent(ex.Message);
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Fatal(ex.Message);
            }
            return response;
        }
    }
}
