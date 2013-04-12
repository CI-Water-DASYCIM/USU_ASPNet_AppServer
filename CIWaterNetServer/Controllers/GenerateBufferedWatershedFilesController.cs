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

            string inputWSShapeFileName = "watershed.shp";
            string outputBufferedWSShapeFileName = "ws_buffered.shp";
            string outputBufferedWSRasterFileName = "ws_buffered.tif";

            if (EnvironmentSettings.IsLocalHost)
            {
                _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\CreateBufferedWatershedFiles.py";
                _inputShapeFilePath = @"E:\CIWaterData\Temp";
                _outputWatershedFilePath = @"E:\CIWaterData\Temp";
                _inputReferenceDEMFile = @"E:\CIWaterData\DEM\gsl100.tif";
            }
            else
            {
                _targetPythonScriptFile = @"C:\CIWaterPythonScripts\CreateBufferedWatershedFiles.py";
                _inputShapeFilePath = @"C:\CIWaterData\Temp";
                _outputWatershedFilePath = @"C:\CIWaterData\Temp";
                _inputReferenceDEMFile = @"C:\CIWaterData\DEM\gsl100.tif";
            }

            //check if shape file exists
            _inputWatershedShapeFile = Path.Combine(_inputShapeFilePath, inputWSShapeFileName);
            if (!File.Exists(_inputWatershedShapeFile))
            {
                string errMsg = "Internal error: Specified shape file " + _inputWatershedShapeFile + " was not found.";
                logger.Error(errMsg);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
            }

            // check if the reference DEM file exists
            if (!File.Exists(_inputReferenceDEMFile))
            {
                string errMsg = "Internal error: Specified reference DEM file " + _inputReferenceDEMFile + " was not found.";
                logger.Error(errMsg);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
            }

            _outputBufferedWSShapeFile = Path.Combine(_outputWatershedFilePath, outputBufferedWSShapeFileName);
            _outputBufferedWSRasterFile = Path.Combine(_outputWatershedFilePath, outputBufferedWSRasterFileName);

            // if the output watershed shape file alreday exists, then delete it
            if (File.Exists(_outputBufferedWSShapeFile))
            {
                File.Delete(_outputBufferedWSShapeFile);
            }

            // if the output watershed rsater file alreday existx, then delet it
            if (File.Exists(_outputBufferedWSRasterFile))
            {
                File.Delete(_outputBufferedWSRasterFile);
            }

            try
            {
                List<string> arguments = new List<string>();
                arguments.Add(@"C:\Python27\ArcGIS10.1\Python.exe");
                arguments.Add(_targetPythonScriptFile);
                arguments.Add(_inputReferenceDEMFile);
                arguments.Add(_inputWatershedShapeFile);
                arguments.Add(_outputBufferedWSShapeFile);
                arguments.Add(_outputBufferedWSRasterFile);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); //>> new code
                object command = commandString; //>>>new code

                Python.PythonHelper.ExecuteCommand(command); //>>> new code
                //Python.PythonHelper.ExecuteScript(_targetPythonScriptFile, arguments);
                string responseMsg = "Buffered watershed shape and raster files were created.";
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
