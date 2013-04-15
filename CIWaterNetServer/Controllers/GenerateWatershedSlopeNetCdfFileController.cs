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

            string wsDEMFileName = UEB.UEBSettings.WATERSHED_DEM_RASTER_FILE_NAME; // "ws_dem.tif";
            string outputWSNetCdfSlopeFileName = UEB.UEBSettings.WATERSHED_SLOPE_NETCDF_FILE_NAME; // "slope.nc";

            //if (EnvironmentSettings.IsLocalHost)
            //{
            //    _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\CalculateWatershedSlope.py";
            //    _inputWatershedFilePath = @"E:\CIWaterData\Temp";
            //}
            //else
            //{
            //    _targetPythonScriptFile = @"C:\CIWaterPythonScripts\CalculateWatershedSlope.py";
            //    _inputWatershedFilePath = @"C:\CIWaterData\Temp";
            //}
              
            // begin new code
            _targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "CalculateWatershedSlope.py");
            _inputWatershedFilePath = UEB.UEBSettings.WORKING_DIR_PATH;

            // check if the python script file exists
            if (!File.Exists(_targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate watershed slope netcdf file was not found.", _targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode =  HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // end of new code

            _inputWSDEMRasterFile = Path.Combine(_inputWatershedFilePath, wsDEMFileName);

            if (!File.Exists(_inputWSDEMRasterFile))
            {
                string errMsg = string.Format("No DEM file ({0}) for the watershed was found.", _inputWSDEMRasterFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
                //return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
            }

            try
            {
                List<string> arguments = new List<string>();
                arguments.Add(EnvironmentSettings.PythonExecutableFile); //@"C:\Python27\ArcGIS10.1\Python.exe");
                arguments.Add(_targetPythonScriptFile);
                arguments.Add(_inputWSDEMRasterFile);
                arguments.Add(outputWSNetCdfSlopeFileName);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); //>> new code
                object command = commandString; 
                Python.PythonHelper.ExecuteCommand(command); 
                               
                string responseMsg = string.Format("Watershed slope NetCDF file ({0}) was created.", outputWSNetCdfSlopeFileName);
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
