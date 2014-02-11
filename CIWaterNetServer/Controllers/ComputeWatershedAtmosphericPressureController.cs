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

        public HttpResponseMessage GetWatershedAtmosphericPressure()
        {
            string workingRootDirPath = Guid.NewGuid().ToString();
            return ComputeWatershedAtmosphericPressure(workingRootDirPath);
        }

        public HttpResponseMessage GetWatershedAtmosphericPressure(string workingRootDirPath)
        {
            return ComputeWatershedAtmosphericPressure(workingRootDirPath);
        }

        private HttpResponseMessage ComputeWatershedAtmosphericPressure(string workingRootDirPath)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            logger.Info("Calculating atmospheric pressure for the watershed...");

            string inputWatershedFilePath = string.Empty;    
            string inputWSDEMFile = string.Empty;
            string targetPythonScriptFile = string.Empty;
            string wsDEMFileName = UEB.UEBSettings.WATERSHED_DEM_RASTER_FILE_NAME;            
            string outputWSAtmPresFileName = UEB.UEBSettings.WATERSHED_ATMOSPHERIC_PRESSURE_FILE_NAME; 
                        
            targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "CalculateWatershedAtmosphericPressure.py");
            inputWatershedFilePath = workingRootDirPath; // UEB.UEBSettings.WORKING_DIR_PATH;            
            inputWSDEMFile = Path.Combine(inputWatershedFilePath, wsDEMFileName);

            if (!File.Exists(inputWSDEMFile))
            {
                string errMsg = string.Format("DEM file ({0}) for the watershed was not found.", inputWSDEMFile);
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
                arguments.Add(inputWSDEMFile);
                arguments.Add(outputWSAtmPresFileName);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); 
                object command = commandString; 

                // execute python script
                Python.PythonHelper.ExecuteCommand(command); 
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
