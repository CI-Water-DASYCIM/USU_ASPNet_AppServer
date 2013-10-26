using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Web;
using UWRL.CIWaterNetServer.Helpers;

namespace UWRL.CIWaterNetServer.Daymet
{
    public static class DataProcessor
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static ResponseMessage GetWatershedSingleTempDataPointNetCDFFile(CancellationToken ct, string outNetCDFDataVariableName, string sourceNetCDFFileNamePatternToMatch, string workingRootDirPath, DateTime simulationStartDate, DateTime simulationEndDate)
        {
            ResponseMessage response = new ResponseMessage();

            string wsDEMFileName = UEB.UEBSettings.WATERSHED_DEM_RASTER_FILE_NAME;                  
            string watershedFilePath = string.Empty;
            string inputWSDEMRasterFile = string.Empty;
            string targetPythonScriptFile = string.Empty;
            string inputTempDaymetDataFilePath = string.Empty;
            string outNetCDFTempDataFilePath = string.Empty;
            string outNetCDFTempDataFileName = string.Empty; 
            string outTempDataRasterFilePath = string.Empty;

            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }

            if (string.IsNullOrEmpty(outNetCDFDataVariableName))
            {
                string errMsg = "Variable name for the output netcdf temperature file is missing.";
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.BadRequest;
                response.Content = new StringContent(errMsg);
                return response;
            }
            else if (outNetCDFDataVariableName != UEB.UEBSettings.WATERSHED_SINGLE_TEMP_MIN_NETCDF_VARIABLE_NAME && outNetCDFDataVariableName != UEB.UEBSettings.WATERSHED_SINGLE_TEMP_MAX_NETCDF_VARIABLE_NAME)
            {
                string errMsg = string.Format("Variable name for the output netcdf temperature file must be either '{0}' or '{1}'.", 
                                    UEB.UEBSettings.WATERSHED_SINGLE_TEMP_MIN_NETCDF_VARIABLE_NAME, 
                                    UEB.UEBSettings.WATERSHED_SINGLE_TEMP_MAX_NETCDF_VARIABLE_NAME);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.BadRequest;
                response.Content = new StringContent(errMsg);
                return response;
            }

            if (string.IsNullOrEmpty(sourceNetCDFFileNamePatternToMatch))
            {
                string errMsg = "Input Daymet temperature data file name pattern is missing";
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.BadRequest;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // validate simluation start and end dates
            if (simulationStartDate >= simulationEndDate)
            {
                string errMsg = "Simulation end date must be a date after simulation start date.";
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.BadRequest;
                response.Content = new StringContent(errMsg);
                return response;
            }

            if (outNetCDFDataVariableName == UEB.UEBSettings.WATERSHED_SINGLE_TEMP_MAX_NETCDF_VARIABLE_NAME)
            {
                outNetCDFTempDataFileName = UEB.UEBSettings.WATERSHED_SINGLE_TEMP_MAX_NETCDF_FILE_NAME;
            }
            else
            {
                outNetCDFTempDataFileName = UEB.UEBSettings.WATERSHED_SINGLE_TEMP_MIN_NETCDF_FILE_NAME;
            }
            
            targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "CalculateWatershedDaymetTempGDAL.py");
            inputTempDaymetDataFilePath = UEB.UEBSettings.DAYMET_RESOURCE_TEMP_DIR_PATH;
            outNetCDFTempDataFilePath = Path.Combine(workingRootDirPath, UEB.UEBSettings.DAYMET_NETCDF_OUTPUT_TEMP_SUB_DIR_PATH);
            outTempDataRasterFilePath = Path.Combine(workingRootDirPath, UEB.UEBSettings.DAYMET_RASTER_OUTPUT_TEMP_SUB_DIR_PATH);
            watershedFilePath = workingRootDirPath; 
                        
            inputWSDEMRasterFile = Path.Combine(watershedFilePath, wsDEMFileName);

            if (!File.Exists(inputWSDEMRasterFile))
            {
                string errMsg = string.Format("DEM file ({0}) for the watershed was not found.", inputWSDEMRasterFile);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }
            
            // check if the python script file exists
            if (!File.Exists(targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate temperature data for the watershed was not found.", targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // if the output dir paths do not exist, then create those paths
            if (!Directory.Exists(outNetCDFTempDataFilePath))
            {
                Directory.CreateDirectory(outNetCDFTempDataFilePath);
            }

            if (!Directory.Exists(outTempDataRasterFilePath))
            {
                Directory.CreateDirectory(outTempDataRasterFilePath);
            }
            
            // get names of all the input temp netcdf files from the inputTempDaymetDataFilePath
            DirectoryInfo di = new DirectoryInfo(inputTempDaymetDataFilePath);
            var tempNetcdfFiles = di.GetFiles(sourceNetCDFFileNamePatternToMatch); // e.g "tmin*.nc"

            if (tempNetcdfFiles.Count() == 0)
            {
                string errMsg = string.Format("No temperature data files were found in the " + 
                                    "specified directory- {0}\n for file name matching - {1}",
                                    inputTempDaymetDataFilePath, sourceNetCDFFileNamePatternToMatch);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            string tempNetCDFFileListString = string.Empty;

            foreach (FileInfo file in tempNetcdfFiles)
            {
                tempNetCDFFileListString += file.Name + ";";
            }

            // remove the last semicolon from the file list string
            tempNetCDFFileListString = tempNetCDFFileListString.Remove(tempNetCDFFileListString.LastIndexOf(';'));

            try
            {
                // create the list of arguments for the python script
                List<string> arguments = new List<string>();
                arguments.Add(EnvironmentSettings.PythonExecutableFile); 
                arguments.Add(targetPythonScriptFile); // new code
                arguments.Add(inputTempDaymetDataFilePath);
                arguments.Add(outNetCDFTempDataFilePath);
                arguments.Add(outNetCDFTempDataFileName);
                arguments.Add(outTempDataRasterFilePath);
                arguments.Add(inputWSDEMRasterFile);
                arguments.Add(tempNetCDFFileListString);
                arguments.Add(outNetCDFDataVariableName);
                arguments.Add(simulationStartDate.ToString("yyyy/MM/dd"));
                arguments.Add(simulationEndDate.ToString("yyyy/MM/dd"));

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); 
                object command = commandString; 

                // execute python script
                Python.PythonHelper.ExecuteCommand(command);
                string responseMsg = string.Format("Watershed Daymet temperature ({0}) NetCDF file was created.", outNetCDFTempDataFileName);
                response.Content = new StringContent(responseMsg);
                response.StatusCode = ResponseStatus.OK;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Info(responseMsg);
            }
            catch (Exception ex)
            {
                response.Content = new StringContent(ex.Message);
                response.StatusCode = ResponseStatus.InternalServerError;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Fatal(ex.Message);
            }
            return response;
        }

        public static ResponseMessage GetWatershedSinglePrecpDataPointNetCDFFile(CancellationToken ct, string sourceNetCDFFileNamePatternToMatch, string workingRootDirPath, DateTime simulationStartDate, DateTime simulationEndDate)
        {
            ResponseMessage response = new ResponseMessage();
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }

            string wsDEMFileName = UEB.UEBSettings.WATERSHED_DEM_RASTER_FILE_NAME; 
            string watershedFilePath = string.Empty;
            string inputWSDEMRasterFile = string.Empty;
            string targetPythonScriptFile = string.Empty;
            string inputPrecpDaymetDataFilePath = string.Empty;
            string outNetCDFPrecpDataFilePath = string.Empty;
            string outNetCDFPrecpDataFileName = UEB.UEBSettings.WATERSHED_SINGLE_PRECP_NETCDF_FILE_NAME; 
            string outPrecpDataRasterFilePath = string.Empty;
                        
            targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "CalculateWatershedDaymetPrecpGDAL.py");
            inputPrecpDaymetDataFilePath = UEB.UEBSettings.DAYMET_RESOURCE_PRECP_DIR_PATH;
            outNetCDFPrecpDataFilePath = Path.Combine(workingRootDirPath, UEB.UEBSettings.DAYMET_NETCDF_OUTPUT_PRECP_SUB_DIR_PATH);
            outPrecpDataRasterFilePath = Path.Combine(workingRootDirPath, UEB.UEBSettings.DAYMET_RASTER_OUTPUT_PRECP_SUB_DIR_PATH);
            watershedFilePath = workingRootDirPath; 

            inputWSDEMRasterFile = Path.Combine(watershedFilePath, wsDEMFileName);
            
            if (!File.Exists(inputWSDEMRasterFile))
            {
                string errMsg = string.Format("DEM file ({0}) for the watershed was not found.", inputWSDEMRasterFile);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            if (string.IsNullOrEmpty(sourceNetCDFFileNamePatternToMatch))
            {
                string errMsg = "Input Daymet precipitation data file name pattern is missing";
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.BadRequest;
                response.Content = new StringContent(errMsg);
                return response;
            }
            
            // validate simluation start and end dates
            if (simulationStartDate >= simulationEndDate)
            {
                string errMsg = "Simulation end date must be a date after simulation start date.";
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.BadRequest;
                response.Content = new StringContent(errMsg);
                return response;
            }
            // check if the python script file exists
            if (!File.Exists(targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate precipitation data for the watershed was not found.", targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // if the output dir paths do not exist, then create those paths
            if (!Directory.Exists(outNetCDFPrecpDataFilePath))
            {
                Directory.CreateDirectory(outNetCDFPrecpDataFilePath);
            }

            if (!Directory.Exists(outPrecpDataRasterFilePath))
            {
                Directory.CreateDirectory(outPrecpDataRasterFilePath);
            }
            
            //get names of all the input precp netcdf files from the _inputPrecpDaymetDataFilePath
            DirectoryInfo di = new DirectoryInfo(inputPrecpDaymetDataFilePath);
            var precpNetcdfFiles = di.GetFiles(sourceNetCDFFileNamePatternToMatch); // e. g. "prcp*.nc"

            if (precpNetcdfFiles.Count() == 0)
            {
                string errMsg = string.Format("No precp data files were found in the " +
                                    "specified directory- {0}\n for file name matching - {1}",
                                    inputPrecpDaymetDataFilePath, sourceNetCDFFileNamePatternToMatch);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }
            string precpNetCDFFileListString = string.Empty;

            foreach (FileInfo file in precpNetcdfFiles)
            {
                precpNetCDFFileListString += file.Name + ";";
            }

            // remove the last semicolon from the file list string
            precpNetCDFFileListString = precpNetCDFFileListString.Remove(precpNetCDFFileListString.LastIndexOf(';'));

            try
            {
                //create the list of arguments for the python script
                List<string> arguments = new List<string>();
                arguments.Add(EnvironmentSettings.PythonExecutableFile); 
                arguments.Add(targetPythonScriptFile);
                arguments.Add(inputPrecpDaymetDataFilePath);
                arguments.Add(outNetCDFPrecpDataFilePath);
                arguments.Add(outNetCDFPrecpDataFileName);
                arguments.Add(outPrecpDataRasterFilePath);
                arguments.Add(inputWSDEMRasterFile);
                arguments.Add(precpNetCDFFileListString);
                arguments.Add(simulationStartDate.ToString("yyyy/MM/dd"));
                arguments.Add(simulationEndDate.ToString("yyyy/MM/dd"));

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); 
                object command = commandString; 

                //execute python script
                Python.PythonHelper.ExecuteCommand(command);
                string responseMsg = string.Format("Watershed Daymet precipitation ({0}) NetCDF file was created.", outNetCDFPrecpDataFileName);
                response.Content = new StringContent(responseMsg);
                response.StatusCode = ResponseStatus.OK;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Info(responseMsg);
            }
            catch (Exception ex)
            {
                response.Content = new StringContent(ex.Message);
                response.StatusCode = ResponseStatus.InternalServerError;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Fatal(ex.Message);
            }
            return response;
        }

        public static ResponseMessage GetWatershedSingleVaporPresDataPointNetCDFFile(CancellationToken ct, string sourceNetCDFFileNamePatternToMatch, string workingRootDirPath, DateTime simulationStartDate, DateTime simulationEndDate)
        {
            ResponseMessage response = new ResponseMessage();
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }

            string wsDEMFileName = UEB.UEBSettings.WATERSHED_DEM_RASTER_FILE_NAME;             
            string watershedFilePath = string.Empty;
            string inputWSDEMRasterFile = string.Empty;
            string targetPythonScriptFile = string.Empty;
            string inputVpDaymetDataFilePath = string.Empty;
            string outNetCDFVpDataFilePath = string.Empty;
            string outNetCDFVpDataFileName = UEB.UEBSettings.WATERSHED_SINGLE_VP_NETCDF_FILE_NAME; 
            string outVpDataRasterFilePath = string.Empty;
                        
            targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "CalculateWatershedDaymetVPDGDAL.py");
            inputVpDaymetDataFilePath = UEB.UEBSettings.DAYMET_RESOURCE_VP_DIR_PATH;
            outNetCDFVpDataFilePath = Path.Combine(workingRootDirPath, UEB.UEBSettings.DAYMET_NETCDF_OUTPUT_VP_SUB_DIR_PATH);
            outVpDataRasterFilePath = Path.Combine(workingRootDirPath, UEB.UEBSettings.DAYMET_RASTER_OUTPUT_VP_SUB_DIR_PATH);
            watershedFilePath = workingRootDirPath; // UEB.UEBSettings.WORKING_DIR_PATH;
            
            // if resampled version of the ws DEM file is available, then use that
            inputWSDEMRasterFile = Path.Combine(watershedFilePath, wsDEMFileName);

            if (!File.Exists(inputWSDEMRasterFile))
            {
                string errMsg = string.Format("DEM file ({0}) for the watershed was not found.", inputWSDEMRasterFile);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            if (string.IsNullOrEmpty(sourceNetCDFFileNamePatternToMatch))
            {
                string errMsg = "Input Daymet vapor pressure data file name pattern is missing";
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.BadRequest;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // validate simluation start and end dates
            if (simulationStartDate >= simulationEndDate)
            {
                string errMsg = "Simulation end date must be a date after simulation start date.";
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.BadRequest;
                response.Content = new StringContent(errMsg);
                return response;
            }
            
            // check if the python script file exists
            if (!File.Exists(targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate vapor pressure data for the watershed was not found.", targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // if the output dir paths do not exist, then create those paths
            if (!Directory.Exists(outNetCDFVpDataFilePath))
            {
                Directory.CreateDirectory(outNetCDFVpDataFilePath);
            }

            if (!Directory.Exists(outVpDataRasterFilePath))
            {
                Directory.CreateDirectory(outVpDataRasterFilePath);
            }
            
            // get names of all the input vapor pressure netcdf files from the _inputVpDaymetDataFilePath
            DirectoryInfo di = new DirectoryInfo(inputVpDaymetDataFilePath);
            var vpNetcdfFiles = di.GetFiles(sourceNetCDFFileNamePatternToMatch); // e. g. "vp*.nc"

            if (vpNetcdfFiles.Count() == 0)
            {
                string errMsg = "No vapor pressure data files were found in the specified directory:" + inputVpDaymetDataFilePath;
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            string vpNetCDFFileListString = string.Empty;

            foreach (FileInfo file in vpNetcdfFiles)
            {
                vpNetCDFFileListString += file.Name + ";";
            }

            // remove the last semicolon from the file list string
            vpNetCDFFileListString = vpNetCDFFileListString.Remove(vpNetCDFFileListString.LastIndexOf(';'));

            try
            {
                //create the list of arguments for the python script
                List<string> arguments = new List<string>();
                arguments.Add(EnvironmentSettings.PythonExecutableFile);
                arguments.Add(targetPythonScriptFile);
                arguments.Add(inputVpDaymetDataFilePath);
                arguments.Add(outNetCDFVpDataFilePath);
                arguments.Add(outNetCDFVpDataFileName);
                arguments.Add(outVpDataRasterFilePath);
                arguments.Add(inputWSDEMRasterFile);
                arguments.Add(vpNetCDFFileListString);
                arguments.Add(simulationStartDate.ToString("yyyy/MM/dd"));
                arguments.Add(simulationEndDate.ToString("yyyy/MM/dd"));

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); 
                object command = commandString;

                // execute python script
                Python.PythonHelper.ExecuteCommand(command);
                string responseMsg = "Watershed Daymet vapor pressure NetCDF file was created.";
                response.Content = new StringContent(responseMsg);
                response.StatusCode = ResponseStatus.OK;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Info(responseMsg);
            }
            catch (Exception ex)
            {
                response.Content = new StringContent(ex.Message);
                response.StatusCode = ResponseStatus.InternalServerError;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Fatal(ex.Message);
            }
            return response;
        }

        public static ResponseMessage GetWatershedMultiplePrecpDataPointsNetCDFFile(CancellationToken ct, int timeStep, string workingRootDirPath, DateTime simulationStartDate)
        {
            ResponseMessage response = new ResponseMessage();
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }

            string targetPythonScriptFile = string.Empty;
            string inputSingleDailyPrecpDataFile = string.Empty;
            string inputSingleDailyPrecpDataFilePath = string.Empty;
            string outNetCDFMultipleDailyPrecpDataFile = string.Empty;
            string outNetCDFMultipleDailyPrecpDataFilePath = string.Empty;
            string destinationPathForNetCDFMultipleDailyPrecpDataFile = string.Empty;
            
            // validate timeStep value
            List<int> allowedTimeStepValues = new List<int> { 1, 2, 3, 4, 6 };
            if (allowedTimeStepValues.Contains(timeStep) == false)
            {
                string errMsg = string.Format("Provided time step value ({0}) is invalid", timeStep);
                response.Content = new StringContent(errMsg);
                response.StatusCode = ResponseStatus.BadRequest;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Fatal(errMsg);
                return response;
            }
                        
            targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "GenerateWatershedDaymetMultiplePrecpDataPointsPerDay.py");
            inputSingleDailyPrecpDataFilePath = Path.Combine(workingRootDirPath, UEB.UEBSettings.DAYMET_NETCDF_OUTPUT_PRECP_SUB_DIR_PATH);
            inputSingleDailyPrecpDataFile = Path.Combine(inputSingleDailyPrecpDataFilePath, UEB.UEBSettings.WATERSHED_SINGLE_PRECP_NETCDF_FILE_NAME);
            outNetCDFMultipleDailyPrecpDataFilePath = Path.Combine(workingRootDirPath, UEB.UEBSettings.DAYMET_NETCDF_OUTPUT_PRECP_SUB_DIR_PATH);
            outNetCDFMultipleDailyPrecpDataFile = Path.Combine(outNetCDFMultipleDailyPrecpDataFilePath, UEB.UEBSettings.WATERSHED_MULTIPLE_PRECP_NETCDF_FILE_NAME);
            destinationPathForNetCDFMultipleDailyPrecpDataFile = workingRootDirPath; 

            // check if the python script file exists
            if (!File.Exists(targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate multiple daily precipitation data for the watershed was not found.", targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // check if the daily single precipitation netcdf input file exists
            if (!File.Exists(inputSingleDailyPrecpDataFile))
            {
                string errMsg = string.Format("Watershed daily single precipitation data netcdf file ({0}) was nout found.", inputSingleDailyPrecpDataFile);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // if the output dir paths do not exist, then create those paths
            if (!Directory.Exists(destinationPathForNetCDFMultipleDailyPrecpDataFile))
            {
                Directory.CreateDirectory(destinationPathForNetCDFMultipleDailyPrecpDataFile);
            }

            if (!Directory.Exists(outNetCDFMultipleDailyPrecpDataFilePath))
            {
                Directory.CreateDirectory(outNetCDFMultipleDailyPrecpDataFilePath);
            }
            
            try
            {
                //create the list of arguments for the python script
                List<string> arguments = new List<string>();
                arguments.Add(EnvironmentSettings.PythonExecutableFile);
                arguments.Add(targetPythonScriptFile);
                arguments.Add(inputSingleDailyPrecpDataFile);
                arguments.Add(outNetCDFMultipleDailyPrecpDataFile);
                arguments.Add(destinationPathForNetCDFMultipleDailyPrecpDataFile);
                arguments.Add(timeStep.ToString());
                arguments.Add(simulationStartDate.ToString("yyyy/MM/dd"));

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments);
                object command = commandString; 

                // execute the python script
                Python.PythonHelper.ExecuteCommand(command);
                string responseMsg = string.Format("Watershed Daymet daily multiple precipitation ({0}) NetCDF file was created.", outNetCDFMultipleDailyPrecpDataFile);
                response.Content = new StringContent(responseMsg);
                response.StatusCode = ResponseStatus.OK;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Info(responseMsg);
            }
            catch (Exception ex)
            {
                response.Content = new StringContent(ex.Message);
                response.StatusCode = ResponseStatus.InternalServerError;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Fatal(ex.Message);
            }
            return response;
        }

        public static ResponseMessage GetWatershedMultipleVaporPresDataPointsNetCDFFile(CancellationToken ct, int timeStep, string workingRootDirPath, DateTime simulationStartDate)
        {
            ResponseMessage response = new ResponseMessage();
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }

            string targetPythonScriptFile = string.Empty;
            string inputSingleDailyVpDataFile = string.Empty;
            string outNetCDFMultipleDailyVpDataFile = string.Empty;
            string outNetCDFMultipleDailyVpDataFilePath = string.Empty;

            // validate timeStep value
            List<int> allowedTimeStepValues = new List<int> { 1, 2, 3, 4, 6 };
            if (allowedTimeStepValues.Contains(timeStep) == false)
            {
                string errMsg = string.Format("Provided time step value ({0}) is invalid", timeStep);
                response.Content = new StringContent(errMsg);
                response.StatusCode = ResponseStatus.BadRequest;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Fatal(errMsg);
                return response;
            }
                        
            targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "GenerateWatershedDaymetMultipleVpdDataPointsPerDay.py");
            string[] paths = new string[3] { workingRootDirPath, UEB.UEBSettings.DAYMET_NETCDF_OUTPUT_VP_SUB_DIR_PATH, UEB.UEBSettings.WATERSHED_SINGLE_VP_NETCDF_FILE_NAME };
            inputSingleDailyVpDataFile = Path.Combine(paths); // Path.Combine(UEB.UEBSettings.DAYMET_NETCDF_OUTPUT_VP_SUB_DIR_PATH, UEB.UEBSettings.WATERSHED_SINGLE_VP_NETCDF_FILE_NAME);
            outNetCDFMultipleDailyVpDataFilePath = Path.Combine(workingRootDirPath, UEB.UEBSettings.DAYMET_NETCDF_OUTPUT_VP_SUB_DIR_PATH);
            outNetCDFMultipleDailyVpDataFile = Path.Combine(outNetCDFMultipleDailyVpDataFilePath, UEB.UEBSettings.WATERSHED_MULTIPLE_VP_NETCDF_FILE_NAME);

            // check if the python script file exists
            if (!File.Exists(targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate multiple daily vapor pressure data for the watershed was not found.", targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // check if the daily single vp netcdf input file exists
            if (!File.Exists(inputSingleDailyVpDataFile))
            {
                string errMsg = string.Format("Watershed daily single vapor pressure data netcdf file ({0}) was nout found.", inputSingleDailyVpDataFile);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // if the output dir path does not exist, then create that directory
            if (!Directory.Exists(outNetCDFMultipleDailyVpDataFilePath))
            {
                Directory.CreateDirectory(outNetCDFMultipleDailyVpDataFilePath);
            }
            
            try
            {
                // create the list of arguments for the python script
                List<string> arguments = new List<string>();                
                arguments.Add(EnvironmentSettings.PythonExecutableFile);
                arguments.Add(targetPythonScriptFile);
                arguments.Add(inputSingleDailyVpDataFile);
                arguments.Add(outNetCDFMultipleDailyVpDataFile);
                arguments.Add(timeStep.ToString());
                arguments.Add(simulationStartDate.ToString("yyyy/MM/dd"));

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments);
                object command = commandString; 

                // execute python script
                Python.PythonHelper.ExecuteCommand(command);
                string responseMsg = string.Format("Watershed Daymet daily multiple vapor pressure ({0}) NetCDF file was created.", outNetCDFMultipleDailyVpDataFile);
                response.Content = new StringContent(responseMsg);
                response.StatusCode = ResponseStatus.OK;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Info(responseMsg);
            }
            catch (Exception ex)
            {
                response.Content = new StringContent(ex.Message);
                response.StatusCode = ResponseStatus.InternalServerError;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Fatal(ex.Message);
            }
            return response;
        }

        public static ResponseMessage GetWatershedMultipleTempDataPointsNetCDFFile(CancellationToken ct, int timeStep, string workingRootDirPath, DateTime simulationStartDate)
        {
            ResponseMessage response = new ResponseMessage();
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }

            string targetPythonScriptFile = string.Empty;
            string inputSingleDailyTminFileName = UEB.UEBSettings.WATERSHED_SINGLE_TEMP_MIN_NETCDF_FILE_NAME; 
            string inputSingleDailyTmaxFileName = UEB.UEBSettings.WATERSHED_SINGLE_TEMP_MAX_NETCDF_FILE_NAME; 
            string inputTempFilePath = string.Empty;
            string outNetCDFMultipleDailyTempDataFileName = UEB.UEBSettings.WATERSHED_MULTIPLE_TEMP_NETCDF_FILE_NAME; 
            string outNetCDFMultipleDailyTempDataFilePath = string.Empty;

            // this is the dir location where the output netcdf file will be finally saved
            string destinationPathForNetCDFMultipleDailyTempDataFile = string.Empty;
            string outNetCDFDataVariableName = UEB.UEBSettings.WATERSHED_MULTIPLE_TEMP_NETCDF_VARIABLE_NAME; 

            // validate timeStep value
            List<int> allowedTimeStepValues = new List<int> { 1, 2, 3, 4, 6 };
            if (allowedTimeStepValues.Contains(timeStep) == false)
            {
                string errMsg = string.Format("Provided time step value ({0}) is invalid", timeStep);
                response.Content = new StringContent(errMsg);
                response.StatusCode = ResponseStatus.BadRequest;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Fatal(errMsg);
                return response;
            }
                        
            targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "GenerateWatershedDaymetMultipleTempDataPerDay.py");
            inputTempFilePath = Path.Combine(workingRootDirPath, UEB.UEBSettings.DAYMET_NETCDF_OUTPUT_TEMP_SUB_DIR_PATH);
            outNetCDFMultipleDailyTempDataFilePath = Path.Combine(workingRootDirPath, UEB.UEBSettings.DAYMET_NETCDF_OUTPUT_TEMP_SUB_DIR_PATH);
            destinationPathForNetCDFMultipleDailyTempDataFile = workingRootDirPath; 

            // check if the python script file exists
            if (!File.Exists(targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate multiple daily temerature data for the watershed was not found.", targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // check if the daily single temp netcdf input file dir path  exists
            if (!Directory.Exists(inputTempFilePath))
            {
                string errMsg = string.Format("Watershed daily single temperature data netcdf file path ({0}) was nout found.", inputTempFilePath);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // if the output dir paths do not exist, then create those paths
            if (!Directory.Exists(destinationPathForNetCDFMultipleDailyTempDataFile))
            {
                Directory.CreateDirectory(destinationPathForNetCDFMultipleDailyTempDataFile);
            }

            if (!Directory.Exists(outNetCDFMultipleDailyTempDataFilePath))
            {
                Directory.CreateDirectory(outNetCDFMultipleDailyTempDataFilePath);
            }
            
            try
            {
                // create the list of arguments for the python script
                List<string> arguments = new List<string>();
                arguments.Add(EnvironmentSettings.PythonExecutableFile);
                arguments.Add(targetPythonScriptFile);
                arguments.Add(inputTempFilePath);
                arguments.Add(outNetCDFMultipleDailyTempDataFilePath);
                arguments.Add(destinationPathForNetCDFMultipleDailyTempDataFile);
                arguments.Add(inputSingleDailyTminFileName);
                arguments.Add(inputSingleDailyTmaxFileName);
                arguments.Add(outNetCDFMultipleDailyTempDataFileName);
                arguments.Add(outNetCDFDataVariableName);
                arguments.Add(timeStep.ToString());
                arguments.Add(simulationStartDate.ToString("yyyy/MM/dd"));

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); 
                object command = commandString; 

                // execute python script
                Python.PythonHelper.ExecuteCommand(command);                
                string responseMsg = string.Format("Watershed Daymet daily multiple temerature ({0}) NetCDF file was created.", outNetCDFMultipleDailyTempDataFileName);
                response.Content = new StringContent(responseMsg);
                response.StatusCode = ResponseStatus.OK;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Info(responseMsg);
            }
            catch (Exception ex)
            {
                response.Content = new StringContent(ex.Message);
                response.StatusCode = ResponseStatus.InternalServerError;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Fatal(ex.Message);
            }
            return response;
        }

        public static ResponseMessage GetWatershedMultipleRHDataPointsNetCDFFile(CancellationToken ct, int timeStep, string workingRootDirPath)
        {                        
            ResponseMessage response = new ResponseMessage();
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            string targetPythonScriptFile = string.Empty;
            string inputMultipleDailyTaFileName = UEB.UEBSettings.WATERSHED_MULTIPLE_TEMP_NETCDF_FILE_NAME; 
            string inputMultipleDailyVpFileName = UEB.UEBSettings.WATERSHED_MULTIPLE_VP_NETCDF_FILE_NAME; 
            string inputMultipleDailyTaFilePath = string.Empty;
            string inputMultipleDailyVpFilePath = string.Empty;
            string inputMultipleDailyTaFile = string.Empty;
            string inputMultipleDailyVpFile = string.Empty;
            string outNetCDFMultipleDailyRHDataFileName = UEB.UEBSettings.WATERSHED_MULTIPLE_RH_NETCDF_FILE_NAME; 
            string outNetCDFMultipleDailyRHDataFilePath = string.Empty;
            string outNetCDFMultipleDailyRHDataFile = string.Empty;
            string destinationPathForNetCDFMultipleDailyRHDataFile = string.Empty;
            string outNetCDFDataVariableName = UEB.UEBSettings.WATERSHED_MULTIPLE_RH_NETCDF_VARIABLE_NAME; 

            // validate timeStep value
            List<int> allowedTimeStepValues = new List<int> { 1, 2, 3, 4, 6 };
            if (allowedTimeStepValues.Contains(timeStep) == false)
            {
                string errMsg = string.Format("Provided time step value ({0}) is invalid.", timeStep);
                response.Content = new StringContent(errMsg);
                response.StatusCode = ResponseStatus.BadRequest;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Fatal(errMsg);
                return response;
            }
                        
            targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "GenerateWatershedDaymetMultipleRHDataPerDay.py");
            inputMultipleDailyTaFilePath = workingRootDirPath; 
            inputMultipleDailyVpFilePath = Path.Combine(workingRootDirPath, UEB.UEBSettings.DAYMET_NETCDF_OUTPUT_VP_SUB_DIR_PATH);
            outNetCDFMultipleDailyRHDataFilePath = Path.Combine(workingRootDirPath, UEB.UEBSettings.DAYMET_NETCDF_OUTPUT_RH_SUB_DIR_PATH);
            destinationPathForNetCDFMultipleDailyRHDataFile = workingRootDirPath; 

            // check if the python script file exists
            if (!File.Exists(targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate multiple daily rh data for the watershed was not found.", targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }
            
            inputMultipleDailyTaFile = Path.Combine(inputMultipleDailyTaFilePath, inputMultipleDailyTaFileName);
            inputMultipleDailyVpFile = Path.Combine(inputMultipleDailyVpFilePath, inputMultipleDailyVpFileName);
            outNetCDFMultipleDailyRHDataFile = Path.Combine(outNetCDFMultipleDailyRHDataFilePath, outNetCDFMultipleDailyRHDataFileName);
            
            // check if the daily multiple temp netcdf input file exists
            if (!File.Exists(inputMultipleDailyTaFile))
            {
                string errMsg = string.Format("Watershed daily multiple temperature data netcdf file ({0}) was nout found.", inputMultipleDailyTaFile);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // check if the daily multiple vp netcdf input file exists
            if (!File.Exists(inputMultipleDailyVpFile))
            {
                string errMsg = string.Format("Watershed daily multiple vapor pressure data netcdf file ({0}) was nout found.", inputMultipleDailyVpFile);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // if the output dir paths do not exist, then create those paths
            if (!Directory.Exists(outNetCDFMultipleDailyRHDataFilePath))
            {
                Directory.CreateDirectory(outNetCDFMultipleDailyRHDataFilePath);
            }

            if (!Directory.Exists(destinationPathForNetCDFMultipleDailyRHDataFile))
            {
                Directory.CreateDirectory(destinationPathForNetCDFMultipleDailyRHDataFile);
            }
            
            try
            {
                // create the list of arguments for the python script
                List<string> arguments = new List<string>();
                arguments.Add(EnvironmentSettings.PythonExecutableFile);
                arguments.Add(targetPythonScriptFile);
                arguments.Add(inputMultipleDailyTaFile);
                arguments.Add(inputMultipleDailyVpFile);
                arguments.Add(outNetCDFMultipleDailyRHDataFile);
                arguments.Add(destinationPathForNetCDFMultipleDailyRHDataFile);
                arguments.Add(outNetCDFDataVariableName);
                arguments.Add(timeStep.ToString());

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments);
                object command = commandString; 

                // execute python script
                Python.PythonHelper.ExecuteCommand(command);
                string responseMsg = string.Format("Watershed Daymet daily multiple RH ({0}) NetCDF file was created.", outNetCDFMultipleDailyRHDataFile);
                response.Content = new StringContent(responseMsg);
                response.StatusCode = ResponseStatus.OK;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Info(responseMsg);
            }
            catch (Exception ex)
            {
                response.Content = new StringContent(ex.Message);
                response.StatusCode = ResponseStatus.InternalServerError;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Fatal(ex.Message);
            }
            return response;
        }

        public static ResponseMessage GetWatershedMultipleWindDataPointsNetCDFFile(CancellationToken ct, float constantWindSpeed, string workingRootDirPath)
        {
            ResponseMessage response = new ResponseMessage();

            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }

            string targetPythonScriptFile = string.Empty;
            string inputMultipleDailyPrecpDataFile = string.Empty;
            string outNetCDFMultipleDailyWindDataFile = string.Empty;
            string outNetCDFMultipleDailyWindDataFilePath = string.Empty;
            string destinationPathForNetCDFMultipleDailyWindDataFile = string.Empty;
                        
            targetPythonScriptFile = Path.Combine(UEB.UEBSettings.PYTHON_SCRIPT_DIR_PATH, "GenerateWatershedDaymetMultipleWindDataPerDay.py");
            inputMultipleDailyPrecpDataFile = Path.Combine(workingRootDirPath, UEB.UEBSettings.WATERSHED_MULTIPLE_PRECP_NETCDF_FILE_NAME);
            outNetCDFMultipleDailyWindDataFilePath = Path.Combine(workingRootDirPath, UEB.UEBSettings.DAYMET_NETCDF_OUTPUT_WIND_SUB_DIR_PATH);
            outNetCDFMultipleDailyWindDataFile = Path.Combine(outNetCDFMultipleDailyWindDataFilePath, UEB.UEBSettings.WATERSHED_MULTIPLE_WIND_NETCDF_FILE_NAME);
            destinationPathForNetCDFMultipleDailyWindDataFile = workingRootDirPath; 

            // check if the python script file exists
            if (!File.Exists(targetPythonScriptFile))
            {
                string errMsg = string.Format("Python script file ({0}) to generate multiple daily wind data for the watershed was not found.", targetPythonScriptFile);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // check if the daily multiple precipitation netcdf input file exists
            if (!File.Exists(inputMultipleDailyPrecpDataFile))
            {
                string errMsg = string.Format("Watershed daily multiple precipitation data netcdf file ({0}) was nout found.", inputMultipleDailyPrecpDataFile);
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            // if the output dir paths do not exist, then create those paths
            if (!Directory.Exists(destinationPathForNetCDFMultipleDailyWindDataFile))
            {
                Directory.CreateDirectory(destinationPathForNetCDFMultipleDailyWindDataFile);
            }

            if (!Directory.Exists(outNetCDFMultipleDailyWindDataFilePath))
            {
                Directory.CreateDirectory(outNetCDFMultipleDailyWindDataFilePath);
            }

            try
            {
                // create the list of arguments for the python script
                List<string> arguments = new List<string>();
                arguments.Add(EnvironmentSettings.PythonExecutableFile);
                arguments.Add(targetPythonScriptFile);
                arguments.Add(inputMultipleDailyPrecpDataFile);
                arguments.Add(outNetCDFMultipleDailyWindDataFile);
                arguments.Add(destinationPathForNetCDFMultipleDailyWindDataFile);
                arguments.Add(constantWindSpeed.ToString());

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); 
                object command = commandString;

                //execute python script
                Python.PythonHelper.ExecuteCommand(command);
                string responseMsg = string.Format("Watershed Daymet daily multiple wind ({0}) NetCDF file was created.", outNetCDFMultipleDailyWindDataFile);
                response.Content = new StringContent(responseMsg);
                response.StatusCode = ResponseStatus.OK;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Info(responseMsg);
            }
            catch (Exception ex)
            {
                response.Content = new StringContent(ex.Message);
                response.StatusCode = ResponseStatus.InternalServerError;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Fatal(ex.Message);
            }
            return response;
        }

        public static HttpResponseMessage GetHttpResponse(ResponseMessage daymetResponse)
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            httpResponse.Content = daymetResponse.Content;

            if (daymetResponse.StatusCode == ResponseStatus.OK)
            {
                httpResponse.StatusCode = HttpStatusCode.OK;
            }
            else if (daymetResponse.StatusCode == ResponseStatus.NotFound)
            {
                httpResponse.StatusCode = HttpStatusCode.NotFound;
            }
            else if (daymetResponse.StatusCode == ResponseStatus.InternalServerError)
            {
                httpResponse.StatusCode = HttpStatusCode.InternalServerError;
            }
            else if (daymetResponse.StatusCode == ResponseStatus.BadRequest)
            {
                httpResponse.StatusCode = HttpStatusCode.BadRequest;
            }
            else
            {
                httpResponse.StatusCode = HttpStatusCode.Continue;
            }

            return httpResponse;

        }

        private static ResponseMessage GetCancellationResponse()
        {
            ResponseMessage response = new ResponseMessage();
            response.StatusCode = ResponseStatus.BadRequest;
            response.Content = new StringContent("Job has been cancelled.");
            return response;
        }
    }

    public class ResponseMessage
    {
        public ResponseStatus StatusCode = ResponseStatus.OK;
        public StringContent Content = null;
    }

    public enum ResponseStatus
    {
        OK,
        Error,
        BadRequest,
        NotFound,
        InternalServerError
    }
}
   