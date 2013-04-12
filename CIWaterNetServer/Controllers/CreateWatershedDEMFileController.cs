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
        private string _outputWSDEMRasterFileName = "ws_dem.tif";

        public CreateWatershedDEMFileController()
        {
            if (EnvironmentSettings.IsLocalHost)
            {
                _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\CreateWatershedDEMFile.py";
                _inputBufferedWSRasterFile = @"E:\CIWaterData\Temp\ws_buffered.tif"; 
                _inputReferenceDEMFile = @"E:\CIWaterData\DEM\gsl100.tif";                
            }
            else
            {
                _targetPythonScriptFile = @"C:\CIWaterPythonScripts\CreateWatershedDEMFile.py";
                _inputBufferedWSRasterFile = @"C:\CIWaterData\Temp\ws_buffered.tif"; 
                _inputReferenceDEMFile = @"C:\CIWaterData\DEM\gsl100.tif";                
            }

        }
        public HttpResponseMessage GetWatershedDEMFile()
        {
            return GenerateWSDEMFile(0);            
        }

        public HttpResponseMessage GetWatershedDEMFile(int cellSize)
        {
            return GenerateWSDEMFile(cellSize);
        }

        private HttpResponseMessage GenerateWSDEMFile(int cellSize)
        {
            HttpResponseMessage response = new HttpResponseMessage();

            logger.Info("Creating watershed DEM raster file...");  

            cellSize = Math.Abs(cellSize);
            
            //check if buffered watershed file exists
            if (!File.Exists(_inputBufferedWSRasterFile))
            {
                string errMsg = string.Format("Internal error: Buffered watershed raster file ({0}) was not found.", _inputBufferedWSRasterFile);
                logger.Error(errMsg);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
            }

            //check if larger DEM file that includes watershed exists
            if (!File.Exists(_inputReferenceDEMFile))
            {
                string errMsg = string.Format("Internal error: Reference DEM file ({0}) was not found.", _inputReferenceDEMFile);
                logger.Error(errMsg);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
            }

            try
            {
                List<string> arguments = new List<string>();
                arguments.Add(@"C:\Python27\ArcGIS10.1\Python.exe");
                arguments.Add(_targetPythonScriptFile);
                arguments.Add(_inputReferenceDEMFile);
                arguments.Add(_inputBufferedWSRasterFile);
                arguments.Add(_outputWSDEMRasterFileName);

                if (cellSize > 0)
                {
                    arguments.Add(cellSize.ToString());
                }

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); //>> new code
                object command = commandString; //>>>new code

                Python.PythonHelper.ExecuteCommand(command); 
                //Python.PythonHelper.ExecuteScript(_targetPythonScriptFile, arguments);
                string responseMsg = "Watershed DEM raster file was created.";
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
