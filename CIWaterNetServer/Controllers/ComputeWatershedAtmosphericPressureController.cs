﻿using NLog;
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
    public class ComputeWatershedAtmosphericPressureController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private string _inputWatershedFilePath = string.Empty;    
        private string _inputWSDEMFile = string.Empty;
        private string _targetPythonScriptFile = string.Empty;

        public HttpResponseMessage GetWatershedAtmosphericPressure()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            logger.Info("Calculating atmospheric pressure for the watershed...");

            string wsDEMFileName = "ws_dem.tif";
            //string resampledWSDEMFileName = "ResampledWSDEM.tif";
            string outputWSAtmPresFileName = "ws_atom_pres.txt";

            if (EnvironmentSettings.IsLocalHost)
            {
                _targetPythonScriptFile = @"E:\SoftwareProjects\CIWaterPythonScripts\CalculateWatershedAtmosphericPressure.py";
                _inputWatershedFilePath = @"E:\CIWaterData\Temp";
            }
            else
            {
                _targetPythonScriptFile = @"C:\CIWaterPythonScripts\CalculateWatershedAtmosphericPressure.py";
                _inputWatershedFilePath = @"C:\CIWaterData\Temp";
            }
                        
            _inputWSDEMFile = Path.Combine(_inputWatershedFilePath, wsDEMFileName);

            if (!File.Exists(_inputWSDEMFile))
            {
                string errMsg = "Internal error: No DEM file for the watershed was found.";
                logger.Error(errMsg);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);                                
            }

            try
            {
                List<string> arguments = new List<string>();
                arguments.Add(@"C:\Python27\ArcGIS10.1\Python.exe");
                arguments.Add(_targetPythonScriptFile);
                arguments.Add(_inputWSDEMFile);
                arguments.Add(outputWSAtmPresFileName);

                // create a string containing all the argument items separated by a space
                string commandString = string.Join(" ", arguments); //>> new code
                object command = commandString; //>>>new code

                Python.PythonHelper.ExecuteCommand(command); //>>> new code
                //Python.PythonHelper.ExecuteScript(_targetPythonScriptFile, arguments);
                string responseMsg = "Atmospheric pressure was calculated for the watershed and written to a text file.";
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
