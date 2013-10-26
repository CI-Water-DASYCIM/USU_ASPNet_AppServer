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
    public class GenerateWatershedNetCDFFileController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public HttpResponseMessage GetWatershedNetCDFFile()
        {
            string workingRootDirPath = Guid.NewGuid().ToString();
            return CreateWatershedNetCDFFile(workingRootDirPath);
        }

        public HttpResponseMessage GetWatershedNetCDFFile(string workingRootDirPath)
        {            
            return CreateWatershedNetCDFFile(workingRootDirPath);
        }

        private HttpResponseMessage CreateWatershedNetCDFFile(string workingRootDirPath)
        {
            HttpResponseMessage response = new HttpResponseMessage();

            logger.Info("Creating watershed netcdf file...");

            string inputWSRasterDirPath = string.Empty;        
            string outputNetCDFFilePath = string.Empty;
            string targetPythonScriptFile = string.Empty;
            string inputBufferedWSRasterFileName = UEB.UEBSettings.WATERSHED_BUFERRED_RASTER_FILE_NAME; 
            string outputWSNetCDFFileName = UEB.UEBSettings.WATERSHED_NETCDF_FILE_NAME; 
                        
            targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "CreateWatershedNetCDFFile.py");
            inputWSRasterDirPath = workingRootDirPath; 
            outputNetCDFFilePath = workingRootDirPath; 

            // check if the python script file exists
            if (!File.Exists(targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate watershed netcdf file was not found.", targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // if the output dir path does not exist, then create that dir path
            if (!Directory.Exists(outputNetCDFFilePath))
            {
                Directory.CreateDirectory(outputNetCDFFilePath);
            }
            
            //check if the input buffered watershed raster file exists
            string inputBufferedRasterFile = Path.Combine(inputWSRasterDirPath, inputBufferedWSRasterFileName);
            if (!File.Exists(inputBufferedRasterFile))
            {
                string errMsg = string.Format("Buffered watershed raster file ({0}) was not found.", inputBufferedRasterFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }
                                                
            //if netcdf file exists then delete it
            string outputWSNetCDFFile = Path.Combine(outputNetCDFFilePath, outputWSNetCDFFileName);
            if (File.Exists(outputWSNetCDFFile))
            {
                File.Delete(outputWSNetCDFFile);                
            }
                      
            try
            {                
                List<string> arguments = new List<string>();
                arguments.Add(EnvironmentSettings.PythonExecutableFile); 
                arguments.Add(targetPythonScriptFile);
                arguments.Add(inputBufferedRasterFile);                              
                arguments.Add(outputWSNetCDFFile);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); 
                object command = commandString; 
                
                // execute python script
                Python.PythonHelper.ExecuteCommand(command);                 
                string responseMsg = string.Format("Watershed netcdf file ({0}) was created.", outputWSNetCDFFile);
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
