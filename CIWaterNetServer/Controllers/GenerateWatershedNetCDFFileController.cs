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
        private string _outputNetCDFFilePath = string.Empty;
        private string _targetPythonScriptFile = string.Empty;

        public HttpResponseMessage GetWatershedNetCDFFile()
        {
            HttpResponseMessage response = new HttpResponseMessage();

            logger.Info("Creating watershed netcdf file...");

            string inputBufferedWSRasterFileName = UEB.UEBSettings.WATERSHED_BUFERRED_RASTER_FILE_NAME; // "ws_buffered.tif";
            string inputWSDEMRasterFileName = UEB.UEBSettings.WATERSHED_DEM_RASTER_FILE_NAME; // "ws_dem.tif";
            string outputWSNetCDFFileName = UEB.UEBSettings.WATERSHED_NETCDF_FILE_NAME; // "watershed.nc";
            
            //if (EnvironmentSettings.IsLocalHost)
            //{
            //    _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\CreateWatershedNetCDFFile.py";
            //    _inputWSRasterDirPath = @"E:\CIWaterData\Temp";
            //    _outputNetCDFFilePath = @"E:\CIWaterData\Temp";                
            //}
            //else
            //{
            //    _targetPythonScriptFile = @"C:\CIWaterPythonScripts\CreateWatershedNetCDFFile.py";
            //    _inputWSRasterDirPath = @"C:\CIWaterData\Temp";
            //    _outputNetCDFFilePath = @"C:\CIWaterData\Temp";                
            //}
            
            // begin new code
            _targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "CreateWatershedNetCDFFile.py");
            _inputWSRasterDirPath = UEB.UEBSettings.WORKING_DIR_PATH;
            _outputNetCDFFilePath = UEB.UEBSettings.WORKING_DIR_PATH;

            // check if the python script file exists
            if (!File.Exists(_targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate watershed netcdf file was not found.", _targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // if the output dir path does not exist, then create that dir path
            if (!Directory.Exists(_outputNetCDFFilePath))
            {
                Directory.CreateDirectory(_outputNetCDFFilePath);
            }
                       
            // end of new code

            //check if the input buffered watershed raster file exists
            string inputBufferedRasterFile = Path.Combine(_inputWSRasterDirPath, inputBufferedWSRasterFileName);
            if (!File.Exists(inputBufferedRasterFile))
            {
                string errMsg = string.Format("Buffered watershed raster file ({0}) was not found.", inputBufferedRasterFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
                //return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
            }

            //check if the input buffered watershed DEM raster file exists
            string inputWSDEMRasterFile = Path.Combine(_inputWSRasterDirPath, inputWSDEMRasterFileName);
            if (!File.Exists(inputWSDEMRasterFile))
            {
                string errMsg = string.Format("Watershed DEM raster file  ({0}) was not found.", inputWSDEMRasterFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
                //return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
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
                arguments.Add(EnvironmentSettings.PythonExecutableFile); //@"C:\Python27\ArcGIS10.1\Python.exe";
                arguments.Add(_targetPythonScriptFile);
                arguments.Add(inputBufferedRasterFile);
                arguments.Add(inputWSDEMRasterFile);                
                arguments.Add(outputWSNetCDFFile);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); //>> new code
                object command = commandString; 
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
