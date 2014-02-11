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
    public class GenerateWatershedAspectNetCdfFileController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public HttpResponseMessage GetWatershedAspectNetCDFFile()
        {
            string workingRootDirPath = Guid.NewGuid().ToString();
            return CreateWatershedAspectNetCDFFile(workingRootDirPath);
        }

        public HttpResponseMessage GetWatershedAspectNetCDFFile(string workingRootDirPath)
        {
            return CreateWatershedAspectNetCDFFile(workingRootDirPath);
        }

        private HttpResponseMessage CreateWatershedAspectNetCDFFile(string workingRootDirPath)
        {
            HttpResponseMessage response = new HttpResponseMessage();

            logger.Info("Creating watershed aspect netcdf file...");

            string inputWatershedFilePath = string.Empty; 
            string inputWSDEMFile = string.Empty;
            string targetPythonScriptFile = string.Empty;
            string wsDEMFileName = UEB.UEBSettings.WATERSHED_DEM_RASTER_FILE_NAME; 
            string outWSAspectNetCDFFileName = UEB.UEBSettings.WATERSHED_ASPECT_NETCDF_FILE_NAME; 
                        
            targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "CalculateWatershedAspect.py");
            inputWatershedFilePath = workingRootDirPath; // UEB.UEBSettings.WORKING_DIR_PATH;            
            inputWSDEMFile = Path.Combine(inputWatershedFilePath, wsDEMFileName);

            if (!File.Exists(inputWSDEMFile))
            {
                string errMsg = string.Format("No DEM file ({0}) for the watershed was found.", inputWSDEMFile);
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
                arguments.Add(outWSAspectNetCDFFileName);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); 
                object command = commandString;

                // execute python script
                Python.PythonHelper.ExecuteCommand(command);                                
                string responseMsg = string.Format("Watershed aspect NetCDF file ({0}) was created.", outWSAspectNetCDFFileName);
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
