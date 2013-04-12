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

        public static ResponseMessage GetWatershedSingleTempDataPointNetCDFFile(CancellationToken ct, string outNetCDFDataVariableName, string sourceNetCDFFileNamePatternToMatch)
        {
            ResponseMessage response = new ResponseMessage();
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }

            string wsDEMFileName = "ws_dem.tif";
            //string resampledWSDEMFileName = "ResampledWSDEM.tif";
            string watershedFilePath = string.Empty;
            string inputWSDEMRasterFile = string.Empty;
            string targetPythonScriptFile = string.Empty;
            string inputTempDaymetDataFilePath = string.Empty;
            string outNetCDFTempDataFilePath = string.Empty;
            string outNetCDFTempDataFileName = outNetCDFDataVariableName + "_daily_one_data.nc";
            string outTempDataRasterFilePath = string.Empty;

            if (EnvironmentSettings.IsLocalHost)
            {
                targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\CalculateWatershedDaymetTempGDAL.py";
                inputTempDaymetDataFilePath = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\tempdatasets";
                outNetCDFTempDataFilePath = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\tempdatasets\OutNetCDF";
                outTempDataRasterFilePath = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\tempdatasets\Raster";
                watershedFilePath = @"E:\CIWaterData\Temp";
            }
            else
            {
                targetPythonScriptFile = @"C:\CIWaterPythonScripts\CalculateWatershedDaymetTempGDAL.py";
                inputTempDaymetDataFilePath = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\tempdatasets";
                outNetCDFTempDataFilePath = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\tempdatasets\OutNetCDF";
                outTempDataRasterFilePath = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\tempdatasets\Raster";
                watershedFilePath = @"C:\CIWaterData\Temp";
            }

            if (string.IsNullOrEmpty(outNetCDFDataVariableName))
            {
                string errMsg = "Data variable name for the output netcdf file is missing.";
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

            inputWSDEMRasterFile = Path.Combine(watershedFilePath, wsDEMFileName);

            if (!File.Exists(inputWSDEMRasterFile))
            {
                string errMsg = "Internal error: No DEM file for the watershed was found.";
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;

            }

            //get names of all the input temp netcdf files from the inputTempDaymetDataFilePath
            DirectoryInfo di = new DirectoryInfo(inputTempDaymetDataFilePath);
            var tempNetcdfFiles = di.GetFiles(sourceNetCDFFileNamePatternToMatch); // e.g "tmin*.nc"

            if (tempNetcdfFiles.Count() == 0)
            {
                string errMsg = string.Format("No temperature data files were found in the specified directory- {0}\n for file name matching - {1}",
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
                //create the list of arguments for the python script
                List<string> arguments = new List<string>();
                arguments.Add(@"C:\Python27\ArcGIS10.1\Python.exe"); //new code
                arguments.Add(targetPythonScriptFile); // new code
                arguments.Add(inputTempDaymetDataFilePath);
                arguments.Add(outNetCDFTempDataFilePath);
                arguments.Add(outNetCDFTempDataFileName);
                arguments.Add(outTempDataRasterFilePath);
                arguments.Add(inputWSDEMRasterFile);
                arguments.Add(tempNetCDFFileListString);
                arguments.Add(outNetCDFDataVariableName);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); //>> new code
                object command = commandString; //>>>new code

                //execute script
                Python.PythonHelper.ExecuteCommand(command); //>>> new code
                //Python.PythonHelper.ExecuteScript(targetPythonScriptFile, arguments);
                string responseMsg = string.Format("Watershed Daymet temperature ({0}) NetCDF file was created.", outNetCDFDataVariableName);
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

        public static ResponseMessage GetWatershedSinglePrecpDataPointNetCDFFile(CancellationToken ct, string sourceNetCDFFileNamePatternToMatch)
        {
            ResponseMessage response = new ResponseMessage();
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }

            string wsDEMFileName = "ws_dem.tif";
            //string resampledWSDEMFileName = "ResampledWSDEM.tif";
            string _watershedFilePath = string.Empty;
            string _inputWSDEMRasterFile = string.Empty;
            string _targetPythonScriptFile = string.Empty;
            string _inputPrecpDaymetDataFilePath = string.Empty;
            string _outNetCDFPrecpDataFilePath = string.Empty;
            string _outNetCDFPrecpDataFileName = "precp_daily_one_data.nc";
            string _outPrecpDataRasterFilePath = string.Empty;

            if (EnvironmentSettings.IsLocalHost)
            {
                _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\CalculateWatershedDaymetPrecpGDAL.py";
                _inputPrecpDaymetDataFilePath = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\precdatasets";
                _outNetCDFPrecpDataFilePath = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\precdatasets\OutNetCDF";
                _outPrecpDataRasterFilePath = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\precdatasets\Raster";
                _watershedFilePath = @"E:\CIWaterData\Temp";
            }
            else
            {
                _targetPythonScriptFile = @"C:\CIWaterPythonScripts\CalculateWatershedDaymetPrecpGDAL.py";
                _inputPrecpDaymetDataFilePath = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\precdatasets";
                _outNetCDFPrecpDataFilePath = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\precdatasets\OutNetCDF";
                _outPrecpDataRasterFilePath = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\precdatasets\Raster";
                _watershedFilePath = @"C:\CIWaterData\Temp";
            }

            _inputWSDEMRasterFile = Path.Combine(_watershedFilePath, wsDEMFileName);

            if (!File.Exists(_inputWSDEMRasterFile))
            {
                string errMsg = "Internal error: No DEM file for the watershed was found.";
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            //get names of all the input precp netcdf files from the _inputPrecpDaymetDataFilePath
            DirectoryInfo di = new DirectoryInfo(_inputPrecpDaymetDataFilePath);
            var precpNetcdfFiles = di.GetFiles(sourceNetCDFFileNamePatternToMatch); // e. g. "prcp*.nc"

            if (precpNetcdfFiles.Count() == 0)
            {
                string errMsg = "No precp data files were found in the specified directory:" + _inputPrecpDaymetDataFilePath;
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
                arguments.Add(@"C:\Python27\ArcGIS10.1\Python.exe"); //TODO: put this magic string in EnvironmentSettings class
                arguments.Add(_targetPythonScriptFile);
                arguments.Add(_inputPrecpDaymetDataFilePath);
                arguments.Add(_outNetCDFPrecpDataFilePath);
                arguments.Add(_outNetCDFPrecpDataFileName);
                arguments.Add(_outPrecpDataRasterFilePath);
                arguments.Add(_inputWSDEMRasterFile);
                arguments.Add(precpNetCDFFileListString);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); //>> new code
                object command = commandString; //>>>new code

                //execute script
                Python.PythonHelper.ExecuteCommand(command); //>>> new code
                //Python.PythonHelper.ExecuteScript(_targetPythonScriptFile, arguments);
                string responseMsg = "Watershed Daymet precipitation daily single data point NetCDF file was created.";
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

        public static ResponseMessage GetWatershedSingleVaporPresDataPointNetCDFFile(CancellationToken ct, string sourceNetCDFFileNamePatternToMatch)
        {
            ResponseMessage response = new ResponseMessage();
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }

            string wsDEMFileName = "ws_dem.tif";            
            string watershedFilePath = string.Empty;
            string inputWSDEMRasterFile = string.Empty;
            string targetPythonScriptFile = string.Empty;
            string inputVpDaymetDataFilePath = string.Empty;
            string outNetCDFVpDataFilePath = string.Empty;
            string outNetCDFVpDataFileName = "vp_daily_one_data.nc";
            string outVpDataRasterFilePath = string.Empty;

            if (EnvironmentSettings.IsLocalHost)
            {
                targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\CalculateWatershedDaymetVPDGDAL.py";
                inputVpDaymetDataFilePath = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\vpdatasets";
                outNetCDFVpDataFilePath = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\vpdatasets\OutNetCDF";
                outVpDataRasterFilePath = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\vpdatasets\Raster";
                watershedFilePath = @"E:\CIWaterData\Temp";
            }
            else
            {
                targetPythonScriptFile = @"C:\CIWaterPythonScripts\CalculateWatershedDaymetVPDGDAL.py";
                inputVpDaymetDataFilePath = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\vpdatasets";
                outNetCDFVpDataFilePath = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\vpdatasets\OutNetCDF";
                outVpDataRasterFilePath = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\vpdatasets\Raster";
                watershedFilePath = @"C:\CIWaterData\Temp";
            }

            // if resampled version of the ws DEM file is available, then use that
            inputWSDEMRasterFile = Path.Combine(watershedFilePath, wsDEMFileName);

            if (!File.Exists(inputWSDEMRasterFile))
            {
                string errMsg = "Internal error: No DEM file for the watershed was found.";
                logger.Error(errMsg);
                response.StatusCode = ResponseStatus.NotFound;
                response.Content = new StringContent(errMsg);
                return response;
            }

            //get names of all the input vapor pressure netcdf files from the _inputVpDaymetDataFilePath
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
                arguments.Add(@"C:\Python27\ArcGIS10.1\Python.exe");
                arguments.Add(targetPythonScriptFile);
                arguments.Add(inputVpDaymetDataFilePath);
                arguments.Add(outNetCDFVpDataFilePath);
                arguments.Add(outNetCDFVpDataFileName);
                arguments.Add(outVpDataRasterFilePath);
                arguments.Add(inputWSDEMRasterFile);
                arguments.Add(vpNetCDFFileListString);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); //>> new code
                object command = commandString; //>>>new code

                //execute script
                Python.PythonHelper.ExecuteCommand(command); //>>> new code
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
                
        public static ResponseMessage GetWatershedMultiplePrecpDataPointsNetCDFFile(CancellationToken ct, int timeStep)
        {
            ResponseMessage response = new ResponseMessage();
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            string _targetPythonScriptFile = string.Empty;
            string _inputSingleDailyPrecpDataFile = string.Empty;
            string _outNetCDFMultipleDailyPrecpDataFile = string.Empty;
            string _destinationPathForNetCDFMultipleDailyPrecpDataFile = string.Empty;
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

            if (EnvironmentSettings.IsLocalHost)
            {
                _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\GenerateWatershedDaymetMultiplePrecpDataPointsPerDay.py";
                _inputSingleDailyPrecpDataFile = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\precdatasets\OutNetCDF\precp_daily_one_data.nc";
                _outNetCDFMultipleDailyPrecpDataFile = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\precdatasets\OutNetCDF\precp_daily_multiple_data.nc";
                _destinationPathForNetCDFMultipleDailyPrecpDataFile = @"E:\CIWaterData\Temp";
            }
            else
            {
                _targetPythonScriptFile = @"C:\CIWaterPythonScripts\GenerateWatershedDaymetMultiplePrecpDataPointsPerDay.py";
                _inputSingleDailyPrecpDataFile = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\precdatasets\OutNetCDF\precp_daily_one_data.nc";
                _outNetCDFMultipleDailyPrecpDataFile = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\precdatasets\OutNetCDF\precp_daily_multiple_data.nc";
                _destinationPathForNetCDFMultipleDailyPrecpDataFile = @"C:\CIWaterData\Temp";
            }

            try
            {
                //create the list of arguments for the python script
                List<string> arguments = new List<string>();
                //arguments.Add("python");
                arguments.Add(@"C:\Python27\ArcGIS10.1\Python.exe");
                arguments.Add(_targetPythonScriptFile);
                arguments.Add(_inputSingleDailyPrecpDataFile);
                arguments.Add(_outNetCDFMultipleDailyPrecpDataFile);
                arguments.Add(_destinationPathForNetCDFMultipleDailyPrecpDataFile);
                arguments.Add(timeStep.ToString());

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments);
                object command = commandString; 

                //execute script
                Python.PythonHelper.ExecuteCommand(command); 
                //Python.PythonHelper.ExecuteScript(_targetPythonScriptFile, arguments);
                string responseMsg = "Watershed Daymet daily multiple precipitation NetCDF file was created.";
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

        public static ResponseMessage GetWatershedMultipleVaporPresDataPointsNetCDFFile(CancellationToken ct, int timeStep)
        {
            ResponseMessage response = new ResponseMessage();
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            string _targetPythonScriptFile = string.Empty;
            string _inputSingleDailyVpDataFile = string.Empty;
            string _outNetCDFMultipleDailyVpDataFile = string.Empty;

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

            if (EnvironmentSettings.IsLocalHost)
            {
                _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\GenerateWatershedDaymetMultipleVpdDataPointsPerDay.py";
                _inputSingleDailyVpDataFile = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\vpdatasets\OutNetCDF\vp_daily_one_data.nc";
                _outNetCDFMultipleDailyVpDataFile = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\vpdatasets\OutNetCDF\vp_daily_multiple_data.nc";
            }
            else
            {
                _targetPythonScriptFile = @"C:\CIWaterPythonScripts\GenerateWatershedDaymetMultipleVpdDataPointsPerDay.py";
                _inputSingleDailyVpDataFile = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\vpdatasets\OutNetCDF\vp_daily_one_data.nc";
                _outNetCDFMultipleDailyVpDataFile = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\vpdatasets\OutNetCDF\vp_daily_multiple_data.nc";
            }

            try
            {
                //create the list of arguments for the python script
                List<string> arguments = new List<string>();
                //arguments.Add("python");
                arguments.Add(@"C:\Python27\ArcGIS10.1\Python.exe");
                arguments.Add(_targetPythonScriptFile);
                arguments.Add(_inputSingleDailyVpDataFile);
                arguments.Add(_outNetCDFMultipleDailyVpDataFile);
                arguments.Add(timeStep.ToString());

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments);
                object command = commandString; 

                //execute script
                Python.PythonHelper.ExecuteCommand(command);
                //Python.PythonHelper.ExecuteScript(_targetPythonScriptFile, arguments);
                string responseMsg = "Watershed Daymet daily multiple vapor pressure NetCDF file was created.";
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
                
        public static ResponseMessage GetWatershedMultipleTempDataPointsNetCDFFile(CancellationToken ct, int timeStep)
        {
            ResponseMessage response = new ResponseMessage();
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            string _targetPythonScriptFile = string.Empty;
            string _inputSingleDailyTminFileName = "tmin_daily_one_data.nc";
            string _inputSingleDailyTmaxFileName = "tmax_daily_one_data.nc";
            string _inputTempFilePath = string.Empty;
            string _outNetCDFMultipleDailyTempDataFileName = "ta_daily_multiple_data.nc";
            string _outNetCDFMultipleDailyTempDataFilePath = string.Empty;

            //this is the dir location where the output netcdf file will be finally saved
            string _destinationPathForNetCDFMultipleDailyTempDataFile = string.Empty;
            string _outNetCDFDataVariableName = "T";

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

            if (EnvironmentSettings.IsLocalHost)
            {
                _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\GenerateWatershedDaymetMultipleTempDataPerDay.py";
                _inputTempFilePath = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\tempdatasets\OutNetCDF";
                _outNetCDFMultipleDailyTempDataFilePath = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\tempdatasets\OutNetCDF";
                _destinationPathForNetCDFMultipleDailyTempDataFile = @"E:\CIWaterData\Temp";
            }
            else
            {
                _targetPythonScriptFile = @"C:\CIWaterPythonScripts\GenerateWatershedDaymetMultipleTempDataPerDay.py";
                _inputTempFilePath = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\tempdatasets\OutNetCDF";
                _outNetCDFMultipleDailyTempDataFilePath = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\tempdatasets\OutNetCDF";
                _destinationPathForNetCDFMultipleDailyTempDataFile = @"C:\CIWaterData\Temp";
            }

            try
            {
                //create the list of arguments for the python script
                List<string> arguments = new List<string>();
                //arguments.Add("python");
                arguments.Add(@"C:\Python27\ArcGIS10.1\Python.exe");
                arguments.Add(_targetPythonScriptFile);
                arguments.Add(_inputTempFilePath);
                arguments.Add(_outNetCDFMultipleDailyTempDataFilePath);
                arguments.Add(_destinationPathForNetCDFMultipleDailyTempDataFile);
                arguments.Add(_inputSingleDailyTminFileName);
                arguments.Add(_inputSingleDailyTmaxFileName);
                arguments.Add(_outNetCDFMultipleDailyTempDataFileName);
                arguments.Add(_outNetCDFDataVariableName);
                arguments.Add(timeStep.ToString());

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); 
                object command = commandString; 

                //execute script
                Python.PythonHelper.ExecuteCommand(command);
                //Python.PythonHelper.ExecuteScript(_targetPythonScriptFile, arguments);
                string responseMsg = "Watershed Daymet daily multiple temperature NetCDF file was created.";
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
               
        public static ResponseMessage GetWatershedMultipleRHDataPointsNetCDFFile(CancellationToken ct, int timeStep)
        {
            ResponseMessage response = new ResponseMessage();
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            string targetPythonScriptFile = string.Empty;
            string inputMultipleDailyTaFileName = "ta_daily_multiple_data.nc";
            string inputMultipleDailyVpFileName = "vp_daily_multiple_data.nc";
            string inputMultipleDailyTaFilePath = string.Empty;
            string inputMultipleDailyVpFilePath = string.Empty;
            string inputMultipleDailyTaFile = string.Empty;
            string inputMultipleDailyVpFile = string.Empty;
            string outNetCDFMultipleDailyRHDataFileName = "rh_daily_multiple_data.nc";
            string outNetCDFMultipleDailyRHDataFilePath = string.Empty;
            string outNetCDFMultipleDailyRHDataFile = string.Empty;
            string destinationPathForNetCDFMultipleDailyRHDataFile = string.Empty;
            string outNetCDFDataVariableName = "rh";

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

            if (EnvironmentSettings.IsLocalHost)
            {
                targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\GenerateWatershedDaymetMultipleRHDataPerDay.py";
                inputMultipleDailyTaFilePath = @"E:\CIWaterData\Temp";
                inputMultipleDailyVpFilePath = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\vpdatasets\OutNetCDF";
                outNetCDFMultipleDailyRHDataFilePath = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\vpdatasets\OutNetCDF";
                destinationPathForNetCDFMultipleDailyRHDataFile = @"E:\CIWaterData\Temp";
            }
            else
            {
                targetPythonScriptFile = @"C:\CIWaterPythonScripts\GenerateWatershedDaymetMultipleRHDataPerDay.py";
                inputMultipleDailyTaFilePath = @"C:\CIWaterData\Temp";
                inputMultipleDailyVpFilePath = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\vpdatasets\OutNetCDF";
                outNetCDFMultipleDailyRHDataFilePath = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\vpdatasets\OutNetCDF";
                destinationPathForNetCDFMultipleDailyRHDataFile = @"C:\CIWaterData\Temp";
            }

            inputMultipleDailyTaFile = Path.Combine(inputMultipleDailyTaFilePath, inputMultipleDailyTaFileName);
            inputMultipleDailyVpFile = Path.Combine(inputMultipleDailyVpFilePath, inputMultipleDailyVpFileName);
            outNetCDFMultipleDailyRHDataFile = Path.Combine(outNetCDFMultipleDailyRHDataFilePath, outNetCDFMultipleDailyRHDataFileName);

            try
            {
                //create the list of arguments for the python script
                List<string> arguments = new List<string>();
                //arguments.Add("python");
                arguments.Add(@"C:\Python27\ArcGIS10.1\Python.exe");
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

                //execute script
                Python.PythonHelper.ExecuteCommand(command);
                //Python.PythonHelper.ExecuteScript(targetPythonScriptFile, arguments);
                string responseMsg = "Watershed Daymet daily multiple RH NetCDF file was created.";
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
                
        public static ResponseMessage GetWatershedMultipleWindDataPointsNetCDFFile(CancellationToken ct, float constantWindSpeed)
        {
            ResponseMessage response = new ResponseMessage();
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            string targetPythonScriptFile = string.Empty;
            string inputMultipleDailyPrecpDataFile = string.Empty;
            string outNetCDFMultipleDailyWindDataFile = string.Empty;
            string destinationPathForNetCDFMultipleDailyWindDataFile = string.Empty;
            // validate constant wind speed           
            if (constantWindSpeed < 0 || constantWindSpeed > 20)
            {
                string errMsg = string.Format("Provided constant wind speed value ({0}) is invalid.", constantWindSpeed);
                errMsg += "\nValid range is 0 to 20 m/sec";
                response.Content = new StringContent(errMsg);
                response.StatusCode = ResponseStatus.BadRequest;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
                logger.Fatal(errMsg);
                return response;
            }

            if (EnvironmentSettings.IsLocalHost)
            {
                targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\GenerateWatershedDaymetMultipleWindDataPerDay.py";
                inputMultipleDailyPrecpDataFile = @"E:\CIWaterData\Temp\precp_daily_multiple_data.nc";
                outNetCDFMultipleDailyWindDataFile = @"E:\CIWaterData\DaymetTimeSeriesData\Logan\winddatasets\OutNetCDF\wind_daily_multiple_data.nc";
                destinationPathForNetCDFMultipleDailyWindDataFile = @"E:\CIWaterData\Temp";
            }
            else
            {
                targetPythonScriptFile = @"C:\CIWaterPythonScripts\GenerateWatershedDaymetMultipleWindDataPerDay.py";
                inputMultipleDailyPrecpDataFile = @"C:\CIWaterData\Temp\precp_daily_multiple_data.nc";
                outNetCDFMultipleDailyWindDataFile = @"C:\CIWaterData\DaymetTimeSeriesData\Logan\winddatasets\OutNetCDF\wind_daily_multiple_data.nc";
                destinationPathForNetCDFMultipleDailyWindDataFile = @"C:\CIWaterData\Temp";
            }

            try
            {
                //create the list of arguments for the python script
                List<string> arguments = new List<string>();
                //arguments.Add("python");
                arguments.Add(@"C:\Python27\ArcGIS10.1\Python.exe");
                arguments.Add(targetPythonScriptFile);
                arguments.Add(inputMultipleDailyPrecpDataFile);
                arguments.Add(outNetCDFMultipleDailyWindDataFile);
                arguments.Add(destinationPathForNetCDFMultipleDailyWindDataFile);
                arguments.Add(constantWindSpeed.ToString());

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); 
                object command = commandString;

                //execute script
                Python.PythonHelper.ExecuteCommand(command);
                //Python.PythonHelper.ExecuteScript(targetPythonScriptFile, arguments);
                string responseMsg = "Watershed Daymet daily multiple vapor pressure NetCDF file was created.";
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
   