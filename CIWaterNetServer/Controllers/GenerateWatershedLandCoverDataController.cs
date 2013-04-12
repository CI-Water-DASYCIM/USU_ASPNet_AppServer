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
    public class GenerateWatershedLandCoverDataController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string _inputWatershedDEMFilePath = string.Empty;
        private string _inputReferenceProjNLCDDataSetFile = string.Empty;       
        private string _inputWSDEMRasterFile = string.Empty;
        private string _targetPythonScriptFile = string.Empty;
       
        public HttpResponseMessage GetWatershedLandCoverData()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            
            logger.Info("Creating watershed NLCD dataset from the original NLCD dataset...");  
                        
            string inputWsDEMFileName = "ws_dem.tif";
            string outputWSNLCDDataSetFileName = "ws_nlcd.img";

            if (EnvironmentSettings.IsLocalHost)
            {
                _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\CreateWatershedNLCDDataSet.py";
                _inputWatershedDEMFilePath = @"E:\CIWaterData\Temp";
                _inputReferenceProjNLCDDataSetFile = @"E:\CIWaterData\NLCDDataSetUSA\ProjNLCD2006_LC_N36W096_v1.img";
            }
            else
            {
                _targetPythonScriptFile = @"C:\CIWaterPythonScripts\CreateWatershedNLCDDataSet.py";
                _inputWatershedDEMFilePath = @"C:\CIWaterData\Temp";
                _inputReferenceProjNLCDDataSetFile = @"C:\CIWaterData\NLCDDataSetUSA\ProjNLCD2006_LC_N36W096_v1.img";
            }
                        
            //set the path of the ws DEM file
            _inputWSDEMRasterFile = Path.Combine(_inputWatershedDEMFilePath, inputWsDEMFileName);
                        
            // check  ws DEM raster file exists
            if (!File.Exists(_inputWSDEMRasterFile))
            {
                string errMsg = string.Format("Internal error: Watershed DEM raster file ({0}) was not found.", _inputWSDEMRasterFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;                
            }

            // check reference nlcd dataset file exists
            if (!File.Exists(_inputReferenceProjNLCDDataSetFile))
            {
                string errMsg = string.Format("Internal error: Reference NLCD dataset file ({0}) was not found.", _inputReferenceProjNLCDDataSetFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;                
            }

            try
            {
                List<string> arguments = new List<string>();
                arguments.Add(@"C:\Python27\ArcGIS10.1\Python.exe");
                arguments.Add(_targetPythonScriptFile);
                arguments.Add(_inputReferenceProjNLCDDataSetFile);
                arguments.Add(_inputWSDEMRasterFile);
                arguments.Add(outputWSNLCDDataSetFileName);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); //>> new code
                object command = commandString; //>>>new code

                Python.PythonHelper.ExecuteCommand(command); //>>> new code
                                
                string responseMsg = "Watershed land cover data set file was created.";
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
