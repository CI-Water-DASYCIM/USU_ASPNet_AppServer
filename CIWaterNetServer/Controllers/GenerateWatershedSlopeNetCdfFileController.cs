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
    public class GenerateWatershedSlopeNetCdfFileController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string _inputWatershedFilePath = string.Empty; 
        private string _inputWSDEMRasterFile = string.Empty;
        private string _targetPythonScriptFile = string.Empty;

        public HttpResponseMessage GetWatershedSlopeNetCDFFile()
        {
            HttpResponseMessage response = new HttpResponseMessage();

            logger.Info("Creating watershed slope netcdf file...");  

            string wsDEMFileName = "ws_dem.tif";
            string outputWSNetCdfSlopeFileName = "slope.nc";

            if (EnvironmentSettings.IsLocalHost)
            {
                _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\CalculateWatershedSlope.py";
                _inputWatershedFilePath = @"E:\CIWaterData\Temp";
            }
            else
            {
                _targetPythonScriptFile = @"C:\CIWaterPythonScripts\CalculateWatershedSlope.py";
                _inputWatershedFilePath = @"C:\CIWaterData\Temp";
            }
                        
            _inputWSDEMRasterFile = Path.Combine(_inputWatershedFilePath, wsDEMFileName);

            if (!File.Exists(_inputWSDEMRasterFile))
            {
                string errMsg = string.Format("Internal error: No DEM file ({0}) for the watershed was found.", _inputWSDEMRasterFile);
                logger.Error(errMsg);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
            }

            try
            {
                List<string> arguments = new List<string>();
                arguments.Add(@"C:\Python27\ArcGIS10.1\Python.exe");
                arguments.Add(_targetPythonScriptFile);
                arguments.Add(_inputWSDEMRasterFile);
                arguments.Add(outputWSNetCdfSlopeFileName);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); //>> new code
                object command = commandString; //>>>new code

                Python.PythonHelper.ExecuteCommand(command); //>>> new code
                               
                string responseMsg = "Watershed slope NetCDF file was created.";
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
