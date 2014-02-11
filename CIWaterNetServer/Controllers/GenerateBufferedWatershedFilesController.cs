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
    public class GenerateBufferedWatershedFilesController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
                
        public HttpResponseMessage GetBufferedWatershedFiles(string workingRootDirPath, int watershedBufferSize)
        {
            return CreateBufferedWatershedFiles(workingRootDirPath, watershedBufferSize);
        }

        private HttpResponseMessage CreateBufferedWatershedFiles(string workingRootDirPath, int watershedBufferSize)
        {
            HttpResponseMessage response = new HttpResponseMessage();

            logger.Info("Creating watershed buffered shape and raster files...");

            string inputReferenceDEMFile = string.Empty;
            string inputWatershedShapeFile = string.Empty;
            string outputBufferedWSShapeFile = string.Empty;
            string outputBufferedWSRasterFile = string.Empty;
            string inputShapeFilePath = string.Empty;
            string outputWatershedFilePath = string.Empty;
            string targetPythonScriptFile = string.Empty;
            string inputWSShapeFileName = UEB.UEBSettings.WATERSHED_SHAPE_FILE_NAME; 
            string outputBufferedWSShapeFileName = UEB.UEBSettings.WATERSHED_BUFERRED_SHAPE_FILE_NAME; 
            string outputBufferedWSRasterFileName = UEB.UEBSettings.WATERSHED_BUFERRED_RASTER_FILE_NAME; 
                        
            targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "CreateBufferedWatershedFiles.py");
            inputShapeFilePath = workingRootDirPath; 
            outputWatershedFilePath = workingRootDirPath; 
            inputReferenceDEMFile = Path.Combine(UEB.UEBSettings.DEM_RESOURCE_DIR_PATH, UEB.UEBSettings.DEM_RESOURCE_FILE_NAME);

            // check if the python script file exists
            if (!File.Exists(targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate buffered watershed shape and raster " + 
                                                "files was not found.", targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }
                        
            // check if shape file exists
            inputWatershedShapeFile = Path.Combine(inputShapeFilePath, inputWSShapeFileName);
            if (!File.Exists(inputWatershedShapeFile))
            {
                string errMsg = string.Format("Specified shape file ({0}) was not found.", inputWatershedShapeFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;                
            }

            // check if the reference DEM file exists
            if (!File.Exists(inputReferenceDEMFile))
            {
                string errMsg = string.Format("Specified reference DEM file ({0}) was not found.",  inputReferenceDEMFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;                
            }
                        
            // if the output dir path does not exist, then create  this path
            if (!Directory.Exists(outputWatershedFilePath))
            {
                Directory.CreateDirectory(outputWatershedFilePath);
            }
            
            outputBufferedWSShapeFile = Path.Combine(outputWatershedFilePath, outputBufferedWSShapeFileName);
            outputBufferedWSRasterFile = Path.Combine(outputWatershedFilePath, outputBufferedWSRasterFileName);

            // if the output watershed shape file alreday exists, then delete it
            if (File.Exists(outputBufferedWSShapeFile))
            {
                File.Delete(outputBufferedWSShapeFile);
            }

            // if the output watershed raster file alreday exists, then delete it
            if (File.Exists(outputBufferedWSRasterFile))
            {
                File.Delete(outputBufferedWSRasterFile);
            }

            try
            {
                List<string> arguments = new List<string>();
                arguments.Add(EnvironmentSettings.PythonExecutableFile);
                arguments.Add(targetPythonScriptFile);
                arguments.Add(inputReferenceDEMFile);
                arguments.Add(inputWatershedShapeFile);
                arguments.Add(outputBufferedWSShapeFile);
                arguments.Add(outputBufferedWSRasterFile);
                arguments.Add(watershedBufferSize.ToString());

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); 
                object command = commandString; 

                // execute python script
                Python.PythonHelper.ExecuteCommand(command);
                string responseMsg = string.Format("Buffered watershed shape file ({0}) and raster file ({0}) were created.", outputBufferedWSShapeFile, outputBufferedWSRasterFile);
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
