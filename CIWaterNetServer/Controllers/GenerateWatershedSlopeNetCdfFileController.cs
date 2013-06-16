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

        public HttpResponseMessage GetWatershedSlopeNetCDFFile()
        {
            string workingRootDirPath = Guid.NewGuid().ToString();
            return CreateWatershedSlopeNetCDFFile(workingRootDirPath);
        }

        public HttpResponseMessage GetWatershedSlopeNetCDFFile(string workingRootDirPath)
        {
            return CreateWatershedSlopeNetCDFFile(workingRootDirPath);
        }

        private HttpResponseMessage CreateWatershedSlopeNetCDFFile(string workingRootDirPath)
        {
            HttpResponseMessage response = new HttpResponseMessage();

            logger.Info("Creating watershed slope netcdf file...");

            string inputWatershedFilePath = string.Empty; 
            string inputWSDEMRasterFile = string.Empty;
            string targetPythonScriptFile = string.Empty;
            string wsDEMFileName = UEB.UEBSettings.WATERSHED_DEM_RASTER_FILE_NAME; 
            string outputWSNetCdfSlopeFileName = UEB.UEBSettings.WATERSHED_SLOPE_NETCDF_FILE_NAME; 
                        
            targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "CalculateWatershedSlope.py");
            inputWatershedFilePath = workingRootDirPath; // UEB.UEBSettings.WORKING_DIR_PATH;

            // check if the python script file exists
            if (!File.Exists(targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate watershed slope netcdf file was not found.", targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode =  HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }
            
            inputWSDEMRasterFile = Path.Combine(inputWatershedFilePath, wsDEMFileName);

            if (!File.Exists(inputWSDEMRasterFile))
            {
                string errMsg = string.Format("No DEM file ({0}) for the watershed was found.", inputWSDEMRasterFile);
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
                arguments.Add(outputWSNetCdfSlopeFileName);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); 
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
