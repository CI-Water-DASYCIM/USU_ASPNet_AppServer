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
    public class GenerateWatershedLandCoverVariablesDataController : ApiController
    {                
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string _inputWSNLCDDataSetFilePath = string.Empty; 
        private string _inputWSNLCDFile = string.Empty;
        private string _targetPythonScriptFile = string.Empty;

        public HttpResponseMessage GetWatershedLandCoverVariablesData()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            
            logger.Info("Creating watershed land cover specific data netcdf files...");  

            string wsNLCDRasterFileName = "ws_nlcd.img";            
            string outWSCanopyCoverNetCDFFileName = "cc_nlcd_.nc";
            string outWSHeightOfCanopyNetCDFFileName = "hc_nlcd_.nc";
            string outWSLAINetCDFFileName = "lai_nlcd_.nc";
            string outWScanopyYCageNetCDFFileName = "ycage_nlcd_.nc";

            if (EnvironmentSettings.IsLocalHost)
            {
                _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\GenerateLandCoverRelatedSiteVariablesData.py";
                _inputWSNLCDDataSetFilePath = @"E:\CIWaterData\Temp";                
            }
            else
            {
                _targetPythonScriptFile = @"C:\CIWaterPythonScripts\GenerateLandCoverRelatedSiteVariablesData.py";
                _inputWSNLCDDataSetFilePath = @"C:\CIWaterData\Temp";                
            }

            // if resampled version of the ws DEM file is available, then use that
            _inputWSNLCDFile = Path.Combine(_inputWSNLCDDataSetFilePath, wsNLCDRasterFileName);

            if (!File.Exists(_inputWSNLCDFile))
            {
                string errMsg = string.Format("Internal error: NLCD dataset file ({0}) for watershed was not found.", _inputWSNLCDDataSetFilePath);
                logger.Error(errMsg);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);                
            }

            try
            {
                List<string> arguments = new List<string>();
                arguments.Add(@"C:\Python27\ArcGIS10.1\Python.exe");
                arguments.Add(_targetPythonScriptFile);
                arguments.Add(_inputWSNLCDFile);
                arguments.Add(outWSCanopyCoverNetCDFFileName);
                arguments.Add(outWSHeightOfCanopyNetCDFFileName);
                arguments.Add(outWSLAINetCDFFileName);
                arguments.Add(outWScanopyYCageNetCDFFileName);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); //>> new code
                object command = commandString; //>>>new code

                Python.PythonHelper.ExecuteCommand(command); //>>> new code
                                
                string responseMsg = "Gridded land cover site varaibles datasets for the watershed domain were created.";
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
