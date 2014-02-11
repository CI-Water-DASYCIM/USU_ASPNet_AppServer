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

        public HttpResponseMessage GetWatershedLandCoverVariablesData()
        {
            string workingRootDirPath = Guid.NewGuid().ToString();
            return CreateWatershedLandCoverVariablesData(workingRootDirPath);
        }

        public HttpResponseMessage GetWatershedLandCoverVariablesData(string workingRootDirPath)
        {
            return CreateWatershedLandCoverVariablesData(workingRootDirPath);
        }

        private HttpResponseMessage CreateWatershedLandCoverVariablesData(string workingRootDirPath)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            
            logger.Info("Creating watershed land cover specific data netcdf files...");

            string inputWSNLCDDataSetFilePath = string.Empty; 
            string inputWSNLCDFile = string.Empty;
            string targetPythonScriptFile = string.Empty;
            string wsNLCDRasterFileName = UEB.UEBSettings.WATERSHED_NLCD_RASTER_FILE_NAME;            
            string outWSCanopyCoverNetCDFFileName = UEB.UEBSettings.WATERSHED_NLCD_CC_NETCDF_FILE_NAME; 
            string outWSHeightOfCanopyNetCDFFileName = UEB.UEBSettings.WATERSHED_NLCD_HC_NETCDF_FILE_NAME; 
            string outWSLAINetCDFFileName = UEB.UEBSettings.WATERSHED_NLCD_LAI_NETCDF_FILE_NAME; 
            string outWScanopyYCageNetCDFFileName = UEB.UEBSettings.WATERSHED_NLCD_YCAGE_NETCDF_FILE_NAME; 
                        
            targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "GenerateLandCoverRelatedSiteVariablesData.py");
            inputWSNLCDDataSetFilePath = workingRootDirPath; // UEB.UEBSettings.WORKING_DIR_PATH;
            
            // check if the python script file exists
            if (!File.Exists(targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate land cover related site variable data files for the watershed was not found.", targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode =  HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }
            
            // if resampled version of the ws DEM file is available, then use that
            inputWSNLCDFile = Path.Combine(inputWSNLCDDataSetFilePath, wsNLCDRasterFileName);

            if (!File.Exists(inputWSNLCDFile))
            {
                string errMsg = string.Format("Internal error: NLCD dataset file ({0}) for watershed was not found.", inputWSNLCDDataSetFilePath);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;              
            }

            try
            {
                List<string> arguments = new List<string>();
                arguments.Add(EnvironmentSettings.PythonExecutableFile); 
                arguments.Add(targetPythonScriptFile);
                arguments.Add(inputWSNLCDFile);
                arguments.Add(outWSCanopyCoverNetCDFFileName);
                arguments.Add(outWSHeightOfCanopyNetCDFFileName);
                arguments.Add(outWSLAINetCDFFileName);
                arguments.Add(outWScanopyYCageNetCDFFileName);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); 
                object command = commandString; 

                // execute python script
                Python.PythonHelper.ExecuteCommand(command);                                 
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
