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
    public class CreateWatershedDEMFileController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string _inputBufferedWSRasterFile = string.Empty; 
        private string _inputReferenceDEMFile = string.Empty;
        private string _targetPythonScriptFile = string.Empty;
        private string _outputWSDEMRasterFileName = UEB.UEBSettings.WATERSHED_DEM_RASTER_FILE_NAME; 

        public CreateWatershedDEMFileController()
        {            
            _targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "CreateWatershedDEMFile.py");
            //_inputBufferedWSRasterFile = Path.Combine(UEB.UEBSettings.WORKING_DIR_PATH, UEB.UEBSettings.WATERSHED_BUFERRED_RASTER_FILE_NAME);
            _inputReferenceDEMFile = Path.Combine(UEB.UEBSettings.DEM_RESOURCE_DIR_PATH, UEB.UEBSettings.DEM_RESOURCE_FILE_NAME);            
        }

        public HttpResponseMessage GetWatershedDEMFile()
        {
            string workingRootDirPath = Guid.NewGuid().ToString();
            return GenerateWSDEMFile(0, workingRootDirPath);            
        }

        public HttpResponseMessage GetWatershedDEMFile(int cellSize)
        {
            string workingRootDirPath = Guid.NewGuid().ToString();
            return GenerateWSDEMFile(cellSize, workingRootDirPath);
        }

        public HttpResponseMessage GetWatershedDEMFile(int cellSize, string workingRootDirPath)
        {            
            return GenerateWSDEMFile(cellSize, workingRootDirPath);
        }

        private HttpResponseMessage GenerateWSDEMFile(int cellSize, string workingRootDirPath)
        {
            HttpResponseMessage response = new HttpResponseMessage();

            logger.Info("Creating watershed DEM raster file...");  

            cellSize = Math.Abs(cellSize);
            _inputBufferedWSRasterFile = Path.Combine(workingRootDirPath, UEB.UEBSettings.WATERSHED_BUFERRED_RASTER_FILE_NAME);

            // check if buffered watershed file exists
            if (!File.Exists(_inputBufferedWSRasterFile))
            {
                string errMsg = string.Format("Internal error: Buffered watershed raster file ({0}) was not found.", _inputBufferedWSRasterFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;                
            }

            // check if reference DEM file that includes watershed exists
            if (!File.Exists(_inputReferenceDEMFile))
            {
                string errMsg = string.Format("Internal error: Reference DEM file ({0}) was not found.", _inputReferenceDEMFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;                
            }
                        
            // check if the python script file exists
            if (!File.Exists(_targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate temperature data for the watershed was not found.", _targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }
            
            try
            {
                List<string> arguments = new List<string>();
                arguments.Add(EnvironmentSettings.PythonExecutableFile); 
                arguments.Add(_targetPythonScriptFile);
                arguments.Add(_inputReferenceDEMFile);
                arguments.Add(_inputBufferedWSRasterFile);
                arguments.Add(_outputWSDEMRasterFileName);

                if (cellSize > 0)
                {
                    arguments.Add(cellSize.ToString());
                }

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); 
                object command = commandString; 

                // execute python script
                Python.PythonHelper.ExecuteCommand(command);                 
                string responseMsg = string.Format("Watershed DEM raster file ({0}) was created.", _outputWSDEMRasterFileName);
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
