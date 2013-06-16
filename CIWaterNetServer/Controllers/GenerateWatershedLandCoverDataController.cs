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

        public HttpResponseMessage GetWatershedLandCoverData()
        {
            string workingRootDirPath = Guid.NewGuid().ToString();
            return CreateWatershedLandCoverData(workingRootDirPath);
        }

        public HttpResponseMessage GetWatershedLandCoverData(string workingRootDirPath)
        {
            return CreateWatershedLandCoverData(workingRootDirPath);
        }

        private HttpResponseMessage CreateWatershedLandCoverData(string workingRootDirPath)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            
            logger.Info("Creating watershed NLCD dataset from the reference NLCD dataset...");  
            
            string inputWatershedDEMFilePath = string.Empty;
            string inputReferenceProjNLCDDataSetFile = string.Empty;       
            string inputWSDEMRasterFile = string.Empty;
            string targetPythonScriptFile = string.Empty;
            string inputWsDEMFileName = UEB.UEBSettings.WATERSHED_BUFERRED_RASTER_FILE_NAME; 
            string outputWSNLCDDataSetFileName = UEB.UEBSettings.WATERSHED_NLCD_RASTER_FILE_NAME; 
                        
            targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "CreateWatershedNLCDDataSet.py");
            inputWatershedDEMFilePath = workingRootDirPath; // UEB.UEBSettings.WORKING_DIR_PATH;
            inputReferenceProjNLCDDataSetFile = Path.Combine(UEB.UEBSettings.NLCD_RESOURCE_DIR_PATH, UEB.UEBSettings.NLCD_RESOURCE_FILE_NAME);

            // check if the python script file exists
            if (!File.Exists(targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate watershed land cover data was not found.", targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode =  HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }
            
            //set the path of the ws DEM file
            inputWSDEMRasterFile = Path.Combine(inputWatershedDEMFilePath, inputWsDEMFileName);
                        
            // check  ws DEM raster file exists
            if (!File.Exists(inputWSDEMRasterFile))
            {
                string errMsg = string.Format("Watershed DEM raster file ({0}) was not found.", inputWSDEMRasterFile);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(errMsg);
                return response;                
            }

            // check reference nlcd dataset file exists
            if (!File.Exists(inputReferenceProjNLCDDataSetFile))
            {
                string errMsg = string.Format("Reference NLCD dataset file ({0}) was not found.", inputReferenceProjNLCDDataSetFile);
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
                arguments.Add(inputReferenceProjNLCDDataSetFile);
                arguments.Add(inputWSDEMRasterFile);
                arguments.Add(outputWSNLCDDataSetFileName);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); 
                object command = commandString; 

                // execute python script
                Python.PythonHelper.ExecuteCommand(command);                                 
                string responseMsg = string.Format("Watershed land cover data set file ({0}) was created.", outputWSNLCDDataSetFileName);
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
