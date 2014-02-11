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

        public HttpResponseMessage GetWatershedLatLonValues()
        {
            string workingRootDirPath = Guid.NewGuid().ToString();
            return CreateWatershedLatLonValues(workingRootDirPath);
        }

        public HttpResponseMessage GetWatershedLatLonValues(string workingRootDirPath)
        {
            return CreateWatershedLatLonValues(workingRootDirPath);
        }

        private HttpResponseMessage CreateWatershedLatLonValues(string workingRootDirPath)
        {
            HttpResponseMessage response = new HttpResponseMessage();

            logger.Info("Creating watershed lat and lon netcdf files...");

            string inputWatershedShapeFilePath = string.Empty; 
            string inputWSDEMRasterFile = string.Empty;
            string targetPythonScriptFile = string.Empty;
            string wsDEMFileName = UEB.UEBSettings.WATERSHED_DEM_RASTER_FILE_NAME; 
            
            // these file names be used to generate either text file with (.txt) extension
            // or netcdf file with (.nc) extension
            string outputWSLatFileNameWithNoExtension = UEB.UEBSettings.WATERSHED_LATITUDE_FILE_NAME_WITHOUT_EXTENSION; 
            string outputWSLonFileNameWithNoExtension = UEB.UEBSettings.WATERSHED_LONGITIDUE_FILE_NAME_WITHOUT_EXTENSION; 
                        
            targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "CalculateWatershedLatLonValues.py");
            inputWatershedShapeFilePath = workingRootDirPath; // UEB.UEBSettings.WORKING_DIR_PATH;

            // check if the python script file exists
            if (!File.Exists(targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate watershed lat/lon data files was not found.", targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }
                        
            inputWSDEMRasterFile = Path.Combine(inputWatershedShapeFilePath, wsDEMFileName);

            if (!File.Exists(inputWSDEMRasterFile))
            {
                string errMsg = string.Format("No watershed DEM raster file ({0}) was found.", inputWSDEMRasterFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            try
            {
                List<string> arguments = new List<string>();
                arguments.Add(EnvironmentSettings.PythonExecutableFile); 
                arguments.Add(targetPythonScriptFile);
                arguments.Add(inputWSDEMRasterFile);
                arguments.Add(outputWSLatFileNameWithNoExtension);
                arguments.Add(outputWSLonFileNameWithNoExtension);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); //>> new code
                object command = commandString;

                // execute pyhton script
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
