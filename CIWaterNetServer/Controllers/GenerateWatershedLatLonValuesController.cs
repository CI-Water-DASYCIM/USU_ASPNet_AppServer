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
    public class GenerateWatershedLatLonValuesController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string _inputWatershedShapeFilePath = @"C:\CIWaterData\Temp"; 
        private string _inputWSDEMRasterFile = string.Empty;
        private string _targetPythonScriptFile = string.Empty;

        public HttpResponseMessage GetWatershedLatLonValues()
        {
            HttpResponseMessage response = new HttpResponseMessage();

            logger.Info("Creating watershed lat and lon netcdf files...");

            string wsDEMFileName = UEB.UEBSettings.WATERSHED_DEM_RASTER_FILE_NAME; // "ws_dem.tif";
            
            // these file names be used to generate either text file with (.txt) extension
            // or netcdf file with (.nc) extension
            string outputWSLatFileNameWithNoExtension = UEB.UEBSettings.WATERSHED_LATITUDE_FILE_NAME_WITHOUT_EXTENSION; // "lat";
            string outputWSLonFileNameWithNoExtension = UEB.UEBSettings.WATERSHED_LONGITIDUE_FILE_NAME_WITHOUT_EXTENSION; // "lon";

            //if (EnvironmentSettings.IsLocalHost)
            //{
            //    _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\CalculateWatershedLatLonValues.py";
            //    _inputWatershedShapeFilePath = @"E:\CIWaterData\Temp";                
            //}
            //else
            //{
            //    _targetPythonScriptFile = @"C:\CIWaterPythonScripts\CalculateWatershedLatLonValues.py";
            //    _inputWatershedShapeFilePath = @"C:\CIWaterData\Temp";                
            //}
                  
            // begin new code
            _targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "CalculateWatershedLatLonValues.py");
            _inputWatershedShapeFilePath = UEB.UEBSettings.WORKING_DIR_PATH;

            // check if the python script file exists
            if (!File.Exists(_targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate watershed lat/lon netcdf files was not found.", _targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // end of new code
            _inputWSDEMRasterFile = Path.Combine(_inputWatershedShapeFilePath, wsDEMFileName);

            if (!File.Exists(_inputWSDEMRasterFile))
            {
                string errMsg = string.Format("No watershed DEM raster file ({0}) was found.", _inputWSDEMRasterFile);
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
                arguments.Add(outputWSLatFileNameWithNoExtension);
                arguments.Add(outputWSLonFileNameWithNoExtension);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); //>> new code
                object command = commandString;
                Python.PythonHelper.ExecuteCommand(command);                                 
                string responseMsg = string.Format("Watershed lat/lon value files were created.");
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
