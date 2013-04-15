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

        private string _inputReferenceDEMFile = string.Empty;
        private string _inputWatershedShapeFile = string.Empty;
        private string _outputBufferedWSShapeFile = string.Empty;
        private string _outputBufferedWSRasterFile = string.Empty;
        private string _inputShapeFilePath = string.Empty;
        private string _outputWatershedFilePath = string.Empty;
        private string _targetPythonScriptFile = string.Empty;

        public HttpResponseMessage GetBufferedWatershedFiles()
        {
            HttpResponseMessage response = new HttpResponseMessage();

            logger.Info("Creating watershed buffered shape and raster files...");

            string inputWSShapeFileName = UEB.UEBSettings.WATERSHED_SHAPE_FILE_NAME; // "watershed.shp";
            string outputBufferedWSShapeFileName = UEB.UEBSettings.WATERSHED_BUFERRED_SHAPE_FILE_NAME; // "ws_buffered.shp";
            string outputBufferedWSRasterFileName = UEB.UEBSettings.WATERSHED_BUFERRED_RASTER_FILE_NAME; // "ws_buffered.tif";

            
            //if (EnvironmentSettings.IsLocalHost)
            //{
            //    _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\CreateBufferedWatershedFiles.py";
            //    _inputShapeFilePath = @"E:\CIWaterData\Temp";
            //    _outputWatershedFilePath = @"E:\CIWaterData\Temp";
            //    _inputReferenceDEMFile = @"E:\CIWaterData\DEM\gsl100.tif";
            //}
            //else
            //{
            //    _targetPythonScriptFile = @"C:\CIWaterPythonScripts\CreateBufferedWatershedFiles.py";
            //    _inputShapeFilePath = @"C:\CIWaterData\Temp";
            //    _outputWatershedFilePath = @"C:\CIWaterData\Temp";
            //    _inputReferenceDEMFile = @"C:\CIWaterData\DEM\gsl100.tif";
            //}

            // begin of new code
            _targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "CreateBufferedWatershedFiles.py");
            _inputShapeFilePath = UEB.UEBSettings.WORKING_DIR_PATH;
            _outputWatershedFilePath = UEB.UEBSettings.WORKING_DIR_PATH;
            _inputReferenceDEMFile = Path.Combine(UEB.UEBSettings.DEM_RESOURCE_DIR_PATH, UEB.UEBSettings.DEM_RESOURCE_FILE_NAME);

            // check if the python script file exists
            if (!File.Exists(_targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to bufrred watershed shape and raster " + 
                                                "files was not found.", _targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }
            
            // end of new code

            //check if shape file exists
            _inputWatershedShapeFile = Path.Combine(_inputShapeFilePath, inputWSShapeFileName);
            if (!File.Exists(_inputWatershedShapeFile))
            {
                string errMsg = string.Format("Specified shape file ({0}) was not found.", _inputWatershedShapeFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
                //return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
            }

            // check if the reference DEM file exists
            if (!File.Exists(_inputReferenceDEMFile))
            {
                string errMsg = string.Format("Specified reference DEM file ({0}) was not found.",  _inputReferenceDEMFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
                //return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
            }

            // begin new code
            // if the output dir path does not exist, then create  this path
            if (!Directory.Exists(_outputWatershedFilePath))
            {
                Directory.CreateDirectory(_outputWatershedFilePath);
            }
            // end new code

            _outputBufferedWSShapeFile = Path.Combine(_outputWatershedFilePath, outputBufferedWSShapeFileName);
            _outputBufferedWSRasterFile = Path.Combine(_outputWatershedFilePath, outputBufferedWSRasterFileName);

            // if the output watershed shape file alreday exists, then delete it
            if (File.Exists(_outputBufferedWSShapeFile))
            {
                File.Delete(_outputBufferedWSShapeFile);
            }

            // if the output watershed raster file alreday existx, then delete it
            if (File.Exists(_outputBufferedWSRasterFile))
            {
                File.Delete(_outputBufferedWSRasterFile);
            }

            try
            {
                List<string> arguments = new List<string>();
                arguments.Add(EnvironmentSettings.PythonExecutableFile);
                arguments.Add(_targetPythonScriptFile);
                arguments.Add(_inputReferenceDEMFile);
                arguments.Add(_inputWatershedShapeFile);
                arguments.Add(_outputBufferedWSShapeFile);
                arguments.Add(_outputBufferedWSRasterFile);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); 
                object command = commandString; 

                Python.PythonHelper.ExecuteCommand(command);
                string responseMsg = string.Format("Buffered watershed shape file ({0}) and raster file ({0}) were created.", _outputBufferedWSShapeFile, _outputBufferedWSRasterFile);
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
