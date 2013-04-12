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

        private string _inputWSRasterDirPath = string.Empty;
        //private string _inputLargerDEMFile = string.Empty;
        private string _outputNetCDFFilePath = string.Empty;
        private string _targetPythonScriptFile = string.Empty;

        public HttpResponseMessage GetWatershedNetCDFFile()
        {
            HttpResponseMessage response = new HttpResponseMessage();

            logger.Info("Creating watershed netcdf file...");  

            string inputBufferedWSRasterFileName = "ws_buffered.tif";
            string inputWSDEMRasterFileName = "ws_dem.tif";
            string outputWSNetCDFFileName = "watershed.nc";
            
            if (EnvironmentSettings.IsLocalHost)
            {
                _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\CreateWatershedNetCDFFile.py";
                _inputWSRasterDirPath = @"E:\CIWaterData\Temp";
                _outputNetCDFFilePath = @"E:\CIWaterData\Temp";                
            }
            else
            {
                _targetPythonScriptFile = @"C:\CIWaterPythonScripts\CreateWatershedNetCDFFile.py";
                _inputWSRasterDirPath = @"C:\CIWaterData\Temp";
                _outputNetCDFFilePath = @"C:\CIWaterData\Temp";                
            }
            
            //check if buffered watershed raster file exists
            string inputBufferedRasterFile = Path.Combine(_inputWSRasterDirPath, inputBufferedWSRasterFileName);
            if (!File.Exists(inputBufferedRasterFile))
            {
                string errMsg = "Internal error: Buffered watershed raster file " + inputBufferedRasterFile + " was not found.";
                logger.Error(errMsg);                
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
            }

            //check if buffered watershed DEM raster file exists
            string inputWSDEMRasterFile = Path.Combine(_inputWSRasterDirPath, inputWSDEMRasterFileName);
            if (!File.Exists(inputWSDEMRasterFile))
            {
                string errMsg = "Internal error: Watershed DEM raster file " + inputWSDEMRasterFile + " was not found.";
                logger.Error(errMsg);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
            }
                        
            //if netcdf file exists then delete it
            string outputWSNetCDFFile = Path.Combine(_outputNetCDFFilePath, outputWSNetCDFFileName);
            if (File.Exists(outputWSNetCDFFile))
            {
                File.Delete(outputWSNetCDFFile);                
            }
                      
            try
            {                
                List<string> arguments = new List<string>();
                arguments.Add(@"C:\Python27\ArcGIS10.1\Python.exe");
                arguments.Add(_targetPythonScriptFile);
                arguments.Add(inputBufferedRasterFile);
                arguments.Add(inputWSDEMRasterFile);                
                arguments.Add(outputWSNetCDFFile);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); //>> new code
                object command = commandString; //>>>new code

                Python.PythonHelper.ExecuteCommand(command); //>>> new code                
                string responseMsg = "Watershed netcdf file was created.";
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
