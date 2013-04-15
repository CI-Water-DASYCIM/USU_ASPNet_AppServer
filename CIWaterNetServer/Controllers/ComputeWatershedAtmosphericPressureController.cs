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
    public class ComputeWatershedAtmosphericPressureController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private string _inputWatershedFilePath = string.Empty;    
        private string _inputWSDEMFile = string.Empty;
        private string _targetPythonScriptFile = string.Empty;

        public HttpResponseMessage GetWatershedAtmosphericPressure()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            logger.Info("Calculating atmospheric pressure for the watershed...");

            string wsDEMFileName = UEB.UEBSettings.WATERSHED_DEM_RASTER_FILE_NAME; // "ws_dem.tif";            
            string outputWSAtmPresFileName = UEB.UEBSettings.WATERSHED_ATMOSPHERIC_PRESSURE_FILE_NAME; // "ws_atom_pres.txt";

            //if (EnvironmentSettings.IsLocalHost)
            //{
            //    _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\CalculateWatershedAtmosphericPressure.py";
            //    _inputWatershedFilePath = @"E:\CIWaterData\Temp";
            //}
            //else
            //{
            //    _targetPythonScriptFile = @"C:\CIWaterPythonScripts\CalculateWatershedAtmosphericPressure.py";
            //    _inputWatershedFilePath = @"C:\CIWaterData\Temp";
            //}
             
            // begin new code
            _targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "CalculateWatershedAtmosphericPressure.py");
            _inputWatershedFilePath = UEB.UEBSettings.WORKING_DIR_PATH;
            // end of new code

            _inputWSDEMFile = Path.Combine(_inputWatershedFilePath, wsDEMFileName);

            if (!File.Exists(_inputWSDEMFile))
            {
                string errMsg = string.Format("DEM file ({0}) for the watershed was not found.", _inputWSDEMFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
                //return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);                                
            }

            try
            {
                List<string> arguments = new List<string>();
                arguments.Add(EnvironmentSettings.PythonExecutableFile); // @"C:\Python27\ArcGIS10.1\Python.exe");
                arguments.Add(_targetPythonScriptFile);
                arguments.Add(_inputWSDEMFile);
                arguments.Add(outputWSAtmPresFileName);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); //>> new code
                object command = commandString; //>>>new code

                Python.PythonHelper.ExecuteCommand(command); //>>> new code
                string responseMsg = string.Format("Watershed atmospheric pressure was calculated and written to a text file ({0}).", outputWSAtmPresFileName);
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
