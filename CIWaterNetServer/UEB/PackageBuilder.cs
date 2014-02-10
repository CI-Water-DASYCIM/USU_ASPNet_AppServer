using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using UWRL.CIWaterNetServer.Controllers;
using UWRL.CIWaterNetServer.DAL;
using UWRL.CIWaterNetServer.Daymet;
using UWRL.CIWaterNetServer.Models;

namespace UWRL.CIWaterNetServer.UEB
{
    public class PackageBuilder
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string _sourceFilePath = string.Empty;
        private string _targetTempPackageFilePath = string.Empty;
        private string _targetPackageDirPath = string.Empty;
        private string _packageZipFileName = UEB.UEBSettings.UEB_PACKAGE_FILE_NAME; // "UEBPackage.zip";
        private string _clientPackageRequestDirPath = string.Empty;
        private string _packageRequestProcessRootDirPath = string.Empty;

        public int Run()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            ResponseMessage daymetResponse;

            var tokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = tokenSource.Token;
            ServiceContext db = new ServiceContext();
            int numberOfJobsStarted = 0;

            // check if a ueb build package process is currently running
            ServiceLog serviceInProcess = db.ServiceLogs.OrderBy(s => s.CallTime).FirstOrDefault(s => s.RunStatus == RunStatus.Processing & s.Service.APIName == "GenerateUEBPackageController.PostUEBPackageCreate");

            if (serviceInProcess != null)
            {
                // In order to avoid a situtation where the status of UEB package build
                // did not get changed from Processing to some other status, no other build package
                // process could start. So here we change the status of a package build process from Processing to 
                // Error if the process has started more than 10 hours ago
                // TODO: the 10 hour limit need to be specified in the ueb.cfig file
                // The JobRunner console application that runs this function using the 
                // windows task scheduler has been configured to stop the JobRunner application if it runs more than 10 hours
                if (DateTime.Now.Subtract((DateTime)serviceInProcess.StartTime).TotalHours > 10)
                {
                    serviceInProcess.FinishTime = DateTime.Now;
                    serviceInProcess.RunStatus = RunStatus.Error;
                    serviceInProcess.Error = "Processing took too long.";
                    db.SaveChanges();
                }
                else
                {
                    return numberOfJobsStarted;
                }
            }

            ServiceLog serviceInQueue = db.ServiceLogs.OrderBy(s => s.CallTime).FirstOrDefault(s => s.RunStatus == RunStatus.InQueue & s.Service.APIName == "GenerateUEBPackageController.PostUEBPackageCreate");

            if (serviceInQueue == null) return numberOfJobsStarted;

            serviceInQueue.RunStatus = RunStatus.Processing;
            serviceInQueue.StartTime = DateTime.Now;
            db.SaveChanges();

            // set the package request processing root dir path
            string jobGuid = serviceInQueue.JobID;
            _packageRequestProcessRootDirPath = Path.Combine(UEB.UEBSettings.WORKING_DIR_PATH, serviceInQueue.JobID);

            // set other directory paths necessary for processing the request to build ueb package
            _sourceFilePath = _packageRequestProcessRootDirPath;
            _targetTempPackageFilePath = Path.Combine(_packageRequestProcessRootDirPath, UEB.UEBSettings.PACKAGE_FILES_OUTPUT_SUB_DIR_PATH);
            _targetPackageDirPath = Path.Combine(_packageRequestProcessRootDirPath, UEB.UEBSettings.PACKAGE_OUTPUT_SUB_DIR_PATH);

            // set the path for saving the client request zip file            
            _clientPackageRequestDirPath = Path.Combine(_packageRequestProcessRootDirPath, UEB.UEBSettings.PACKAGE_BUILD_REQUEST_SUB_DIR_PATH);

            // read the file with json extension to a string - uebPackageBuildRequestJson
            string[] jsonFiles = Directory.GetFiles(_clientPackageRequestDirPath, "*.json");
            string uebPackageBuildRequestJson = string.Empty;

            using (var fileReader = new StreamReader(jsonFiles[0]))
            {
                uebPackageBuildRequestJson = fileReader.ReadToEnd();
            }

            UEB.UEBPackageRequest uebPkgRequest;
            uebPkgRequest = JsonConvert.DeserializeObject<UEB.UEBPackageRequest>(uebPackageBuildRequestJson);
            string scriptErrMsg = string.Empty;
            numberOfJobsStarted = 1;

            try
            {
                Task mainTask = new Task(() =>
                {
                    float constantWindSpeed = UEB.UEBSettings.WATERSHED_CONSTANT_WIND_SPEED;

                    response = GenerateBufferedWatershedFiles(cancellationToken, uebPkgRequest.BufferSize);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        tokenSource.Cancel();
                        scriptErrMsg = "Python script to generate buffered watershed shape files seems to have failed.";
                        logger.Error( scriptErrMsg + " for job id:" + jobGuid);
                    }

                    response = GenerateWatershedDEMFile(cancellationToken, uebPkgRequest.GridCellSize);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        tokenSource.Cancel();
                        scriptErrMsg = "Python script to generate watershed DEM file seems to have failed.";
                        logger.Error(scriptErrMsg + " for job id:" + jobGuid);
                    }

                    response = GenerateWatershedNetCDFFile(cancellationToken);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        tokenSource.Cancel();
                        scriptErrMsg = "Python script to generate waterhsed netcdf domain file seems to have failed.";
                        logger.Error(scriptErrMsg + " for job id:" + jobGuid);
                    }

                    if (uebPkgRequest.SiteInitialConditions.is_apr_derive_from_elevation)
                    {
                        response = GetWatershedAtmosphericPressure(cancellationToken);
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            tokenSource.Cancel();
                            scriptErrMsg = "Python script to generate atmospheric pressure netcdf file seems to have failed.";
                            logger.Error(scriptErrMsg + " for job id:" + jobGuid);
                        }
                    }

                    response = GetWatershedSlopeNetCDFFile(cancellationToken);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        tokenSource.Cancel();
                        scriptErrMsg = "Python script to generate Slope netcdf file seems to have failed.";
                        logger.Error(scriptErrMsg + " for job id:" + jobGuid);
                    }

                    response = GetWatershedAspectNetCDFFile(cancellationToken);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        tokenSource.Cancel();
                        scriptErrMsg = "Python script to generate Aspect netcdf file seems to have failed.";
                        logger.Error(scriptErrMsg + " for job id:" + jobGuid);
                    }

                    response = GetWatershedLatLonValues(cancellationToken);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        tokenSource.Cancel();
                        scriptErrMsg = "Python script to generate Lat/Lon data points netcdf file seems to have failed.";
                        logger.Error(scriptErrMsg + " for job id:" + jobGuid);
                    }

                    response = GetWatershedLandCoverData(cancellationToken);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        tokenSource.Cancel();
                        scriptErrMsg = "Python script to generate land cover data seems to have failed.";
                        logger.Error(scriptErrMsg + " for job id:" + jobGuid);
                    }

                    response = GetWatershedLandCoverVariablesData(cancellationToken);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        tokenSource.Cancel();
                        scriptErrMsg = "Python script to generate land cover variables data points netcdf file seems to have failed.";
                        logger.Error(scriptErrMsg + " for job id:" + jobGuid);
                    }

                    string outputTminDataVarName = UEB.UEBSettings.WATERSHED_SINGLE_TEMP_MIN_NETCDF_VARIABLE_NAME; // "tmin";
                    string inputDaymetTminFileNamePattern = UEB.UEBSettings.DAYMET_RESOURCE_TEMP_MIN_FILE_NAME_PATTERN; // "tmin*.nc";
                    daymetResponse = DataProcessor.GetWatershedSingleTempDataPointNetCDFFile(cancellationToken, outputTminDataVarName, 
                        inputDaymetTminFileNamePattern, _packageRequestProcessRootDirPath, uebPkgRequest.StartDate, uebPkgRequest.EndDate);
                    
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                        scriptErrMsg = "Python script to generate single Temp Min data points per day netcdf file seems to have failed.";
                        logger.Error(scriptErrMsg + " for job id:" + jobGuid);
                    }

                    string outputTmaxDataVarName = UEB.UEBSettings.WATERSHED_SINGLE_TEMP_MAX_NETCDF_VARIABLE_NAME; ; // "tmax";
                    string inputDaymetTmaxFileNamePattern = UEB.UEBSettings.DAYMET_RESOURCE_TEMP_MAX_FILE_NAME_PATTERN; ; // "tmax*.nc";
                    daymetResponse = DataProcessor.GetWatershedSingleTempDataPointNetCDFFile(cancellationToken, outputTmaxDataVarName, 
                        inputDaymetTmaxFileNamePattern, _packageRequestProcessRootDirPath, uebPkgRequest.StartDate, uebPkgRequest.EndDate);
                    
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                        scriptErrMsg = "Python script to generate single Temp Max data points per day netcdf file seems to have failed.";
                        logger.Error(scriptErrMsg + " for job id:" + jobGuid);
                    }

                    daymetResponse = DataProcessor.GetWatershedMultipleTempDataPointsNetCDFFile(cancellationToken, uebPkgRequest.TimeStep, 
                        _packageRequestProcessRootDirPath, uebPkgRequest.StartDate);
                    
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                        scriptErrMsg = "Python script to generate multiple Temperature data points per day netcdf file seems to have failed.";
                        logger.Error(scriptErrMsg + " for job id:" + jobGuid);
                    }

                    string inputDaymetVpFileNamePattern = UEB.UEBSettings.DAYMET_RESOURCE_VP_FILE_NAME_PATTERN; // "vp*.nc";
                    daymetResponse = DataProcessor.GetWatershedSingleVaporPresDataPointNetCDFFile(cancellationToken, inputDaymetVpFileNamePattern, 
                        _packageRequestProcessRootDirPath, uebPkgRequest.StartDate, uebPkgRequest.EndDate);
                    
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                        scriptErrMsg = "Python script to generate single VP data points per day netcdf file seems to have failed.";
                        logger.Error(scriptErrMsg + " for job id:" + jobGuid);
                    }

                    daymetResponse = DataProcessor.GetWatershedMultipleVaporPresDataPointsNetCDFFile(cancellationToken, uebPkgRequest.TimeStep, 
                        _packageRequestProcessRootDirPath, uebPkgRequest.StartDate);
                    
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                        scriptErrMsg = "Python script to generate multiple VP data points per day netcdf file seems to have failed.";
                        logger.Error(scriptErrMsg + " for job id:" + jobGuid);
                    }

                    string inputDaymetPrcpFileNamePattern = UEB.UEBSettings.DAYMET_RESOURCE_PRECP_FILE_NAME_PATTERN; // "prcp*.nc";
                    daymetResponse = DataProcessor.GetWatershedSinglePrecpDataPointNetCDFFile(cancellationToken, inputDaymetPrcpFileNamePattern, 
                        _packageRequestProcessRootDirPath, uebPkgRequest.StartDate, uebPkgRequest.EndDate);
                    
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                        scriptErrMsg = "Python script to generate single  Prec data points per day netcdf file seems to have failed.";
                        logger.Error(scriptErrMsg + " for job id:" + jobGuid);
                    }

                    daymetResponse = DataProcessor.GetWatershedMultiplePrecpDataPointsNetCDFFile(cancellationToken, uebPkgRequest.TimeStep, 
                        _packageRequestProcessRootDirPath, uebPkgRequest.StartDate);
                    
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                        scriptErrMsg = "Python script to generate multiple Prec data points per day netcdf file seems to have failed.";
                        logger.Error(scriptErrMsg + " for job id:" + jobGuid);
                    }

                    daymetResponse = DataProcessor.GetWatershedMultipleWindDataPointsNetCDFFile(cancellationToken, constantWindSpeed, _packageRequestProcessRootDirPath);
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                        scriptErrMsg = "Python script to generate multiple Wind data points per day netcdf file seems to have failed.";
                        logger.Error(scriptErrMsg + " for job id:" + jobGuid);
                    }

                    daymetResponse = DataProcessor.GetWatershedMultipleRHDataPointsNetCDFFile(cancellationToken, uebPkgRequest.TimeStep, _packageRequestProcessRootDirPath);
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                        scriptErrMsg = "Python script to generate multiple RH data points per day netcdf file seems to have failed.";
                        logger.Error(scriptErrMsg + " for job id:" + jobGuid);
                    }

                    if (tokenSource.IsCancellationRequested == false)
                    {
                        response = CreateUEBpackageZipFile(jobGuid, uebPkgRequest.StartDate, uebPkgRequest.EndDate, uebPkgRequest.TimeStep, uebPkgRequest);
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            tokenSource.Cancel();
                        }
                    }
                    else
                    {
                        logger.Error("One of the python scripts execution got cancelled due to execution error. No package was created for job id:" + jobGuid);
                    }

                }, tokenSource.Token);

                mainTask.Start();
               
                Helpers.TaskDataStore.SetTask(jobGuid, mainTask);
                mainTask.ContinueWith((t) =>
                {
                    if (mainTask.IsCanceled || mainTask.IsFaulted || tokenSource.IsCancellationRequested)
                    {
                        CleanUpOnFailure();
                        string errorMsg = "UEB pacakage could not be created for job id:" + jobGuid;
                        logger.Error(errorMsg);
                        
                        serviceInQueue.RunStatus = RunStatus.Error;
                        serviceInQueue.FinishTime = DateTime.Now;
                        serviceInQueue.Error = scriptErrMsg;
                        db.SaveChanges();   
                    }
                    else
                    {
                        HttpResponseMessage mainTaskResponse = new HttpResponseMessage();
                        mainTaskResponse.StatusCode = HttpStatusCode.OK;
                        mainTaskResponse.Content = new StringContent("UEB package creation is complete for job id:" + jobGuid);
                        Helpers.TaskDataStore.SetTaskResult(jobGuid, mainTaskResponse);
                        logger.Info(mainTaskResponse.Content.ToString());
                        
                        serviceInQueue.RunStatus = RunStatus.Success;
                        serviceInQueue.FinishTime = DateTime.Now;
                        db.SaveChanges();                        
                    }
                });
            }
            catch (Exception ex)
            {
                HttpResponseMessage mainTaskResponse = new HttpResponseMessage();
                mainTaskResponse.StatusCode = HttpStatusCode.Forbidden;
                mainTaskResponse.Content = new StringContent("UEB package creation was unsuccessful for job id:" + jobGuid + "\n" + ex.Message);
                Helpers.TaskDataStore.SetTaskResult(jobGuid, mainTaskResponse);
                logger.Error(mainTaskResponse.Content.ToString());

                serviceInQueue.RunStatus = RunStatus.Error;
                serviceInQueue.FinishTime = DateTime.Now;
                serviceInQueue.Error = ex.Message;
                db.SaveChanges();                
                CleanUpOnFailure();               
            }

            return numberOfJobsStarted;
        }

        /// TODO: This is a parallel version of creating a ueb model package initially written
        /// which was not working due to arcpy licensing issue. This code may be used to develop
        /// a parallel version of executing various python scripts in this PackageBuilder.cs class file
        /// <summary>
        /// Creates all data files and control files needed to run UEB model and then creates a package zip file
        /// that contains all those files
        /// </summary>
        /// <returns>PackageID/JobID which the client can query the server to see the status of package creation</returns>
        private HttpResponseMessage GetUEBPackageCreateParallel(DateTime startDate, DateTime endDate, byte timeStep)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            var tokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = tokenSource.Token;
            List<byte> validTimeStepValues = new List<byte> { 1, 2, 3, 4, 6 };

            // validate timeStep
            if (validTimeStepValues.Contains(timeStep) == false)
            {
                string errMsg = string.Format("Provided time setp value ({0}) is invalid", timeStep);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent(errMsg);
                return response;

            }

            // validate start and end dates
            if (startDate >= endDate)
            {
                string errMsg = string.Format("Provided start date value ({0}) is invalid.\n Start date needs to be a date before the end date.", startDate);
                logger.Error(errMsg);
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent(errMsg);
                return response;
            }

            //generate a guid to pass on the client as a job ID
            Guid jobGuid = Guid.NewGuid();
            try
            {
                Task mainTask = new Task(() =>
                {
                    ResponseMessage daymetResponse;
                    //int timeStep = 6; //TODO: this value needs to be passed as a parameter in GetUEBPackage method 
                    float constantWindSpeed = UEB.UEBSettings.WATERSHED_CONSTANT_WIND_SPEED;

                    // create a task array to monitor all tasks related to daymet data processing                    
                    List<Task> daymetTaskList = new List<Task>();

                    // start a new task to generate netcdf file for the watershed which would also generate watershed buffered shape file
                    Task<HttpResponseMessage> taskCreateWsNetCDFFile = Task<HttpResponseMessage>.Factory.StartNew((t) =>
                        GenerateWatershedNetCDFFile(cancellationToken), tokenSource.Token, TaskCreationOptions.AttachedToParent);

                    Helpers.TaskDataStore.SetTask(taskCreateWsNetCDFFile.Id.ToString(), taskCreateWsNetCDFFile);
                    Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskCreateWsNetCDFFile.Id.ToString());

                    // create a task to generate watershed DEM file
                    int gridCellSize = 100;
                    Task<HttpResponseMessage> taskCreateWsDEMFile = new Task<HttpResponseMessage>(() =>
                        GenerateWatershedDEMFile(cancellationToken, gridCellSize), tokenSource.Token, TaskCreationOptions.AttachedToParent);

                    // create a task to generate watershed atmospheric pressure value
                    Task<HttpResponseMessage> taskAtmPres = new Task<HttpResponseMessage>(() =>
                        GetWatershedAtmosphericPressure(cancellationToken), tokenSource.Token, TaskCreationOptions.AttachedToParent);

                    // create a task for slope generation
                    //Task<HttpResponseMessage> taskSlope = new Task<HttpResponseMessage>(() =>
                    //    GetWatershedSlopeNetCDFFile(cancellationToken), tokenSource.Token, TaskCreationOptions.AttachedToParent);

                    // create a task for slope generation
                    //Task<HttpResponseMessage> taskAspect = new Task<HttpResponseMessage>(() =>
                    //    GetWatershedAspectNetCDFFile(cancellationToken), tokenSource.Token, TaskCreationOptions.AttachedToParent);

                    // make it wait since the ws netcdf file creation task uses the same input file as this one
                    try
                    {
                        taskCreateWsNetCDFFile.Wait();
                    }
                    catch (Exception e)
                    {
                        logger.Info(e.Message);
                    }

                    // start the task to generate ws dem file after the task to generate ws netcdf file/buffered watershed is done
                    taskCreateWsNetCDFFile.ContinueWith((t) =>
                    {
                        response = t.Result;
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            tokenSource.Cancel();
                        }
                        else
                        {
                            taskCreateWsDEMFile.Start();
                            Helpers.TaskDataStore.SetTask(taskCreateWsDEMFile.Id.ToString(), taskCreateWsDEMFile);
                            Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskCreateWsDEMFile.Id.ToString());
                        }

                    }, TaskContinuationOptions.NotOnCanceled);

                    // make it wait since the dem file creation task uses the same input file as this one
                    try
                    {
                        taskCreateWsDEMFile.Wait();
                    }
                    catch (Exception e)
                    {
                        logger.Info(e.Message);
                    }

                    // start the task to generate atm pressure after the task to generate ws dem file is done
                    taskCreateWsDEMFile.ContinueWith((t) =>
                    {
                        response = t.Result;
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            tokenSource.Cancel();
                        }
                        else
                        {
                            taskAtmPres.Start();
                            Helpers.TaskDataStore.SetTask(taskAtmPres.Id.ToString(), taskAtmPres);
                            Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskAtmPres.Id.ToString());
                            //taskSlope.Start();
                            //Helpers.TaskDataStore.SetTask(taskSlope.Id.ToString(), taskSlope);
                            //Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskSlope.Id.ToString());
                            //taskAspect.Start();
                            //Helpers.TaskDataStore.SetTask(taskAspect.Id.ToString(), taskAspect);
                            //Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskAspect.Id.ToString());
                        }

                    }, TaskContinuationOptions.NotOnCanceled);

                    // make it wait since the atmo pressure calculation uses the same input file as this one
                    //try
                    //{
                    //    taskAtmPres.Wait();
                    //}
                    //catch (Exception e)
                    //{
                    //    logger.Info(e.Message);
                    //}

                    // update task data store at the end of tmin task
                    taskAtmPres.ContinueWith(t =>
                    {
                        response = t.Result;
                        Helpers.TaskDataStore.SetTaskResult(taskAtmPres.Id.ToString(), response);
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            tokenSource.Cancel();
                        }
                    }, TaskContinuationOptions.NotOnCanceled);

                    // start a task to generate ws slope data


                    Task<HttpResponseMessage> taskSlope = Task<HttpResponseMessage>.Factory.StartNew((t) =>
                        GetWatershedSlopeNetCDFFile(cancellationToken), tokenSource.Token, TaskCreationOptions.AttachedToParent);
                    Helpers.TaskDataStore.SetTask(taskSlope.Id.ToString(), taskSlope);
                    Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskSlope.Id.ToString());


                    // make it wait since the aspect calculation uses the same input file as this one
                    //try
                    //{
                    //    taskSlope.Wait();
                    //}
                    //catch (Exception e)
                    //{
                    //    logger.Info(e.Message);
                    //}

                    // update task data store at the end of slope task
                    taskSlope.ContinueWith(t =>
                    {
                        response = t.Result;
                        Helpers.TaskDataStore.SetTaskResult(taskSlope.Id.ToString(), response);
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            tokenSource.Cancel();
                        }
                    }, TaskContinuationOptions.NotOnCanceled);

                    // start a task to generate aspect data
                    Task<HttpResponseMessage> taskAspect = Task<HttpResponseMessage>.Factory.StartNew((t) =>
                        GetWatershedAspectNetCDFFile(cancellationToken), tokenSource.Token, TaskCreationOptions.AttachedToParent);
                    Helpers.TaskDataStore.SetTask(taskAspect.Id.ToString(), taskAspect);
                    Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskAspect.Id.ToString());

                    // make it wait since the LatLon calculation uses the same input file as this one
                    //try
                    //{
                    //    taskAspect.Wait();
                    //}
                    //catch (Exception e)
                    //{
                    //    logger.Info(e.Message);
                    //}


                    // update task data store at the end of aspect task
                    taskAspect.ContinueWith(t =>
                    {
                        response = t.Result;
                        Helpers.TaskDataStore.SetTaskResult(taskAspect.Id.ToString(), response);
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            tokenSource.Cancel();
                        }
                    }, TaskContinuationOptions.NotOnCanceled);

                    // start a task to generate ws lat/lon data
                    Task<HttpResponseMessage> taskLatLon = Task<HttpResponseMessage>.Factory.StartNew((t) =>
                        GetWatershedLatLonValues(cancellationToken), tokenSource.Token, TaskCreationOptions.AttachedToParent);
                    Helpers.TaskDataStore.SetTask(taskLatLon.Id.ToString(), taskLatLon);
                    Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskLatLon.Id.ToString());
                    // make it wait since the land cover for watershed calculation uses the same input file as this one
                    //try
                    //{
                    //    taskLatLon.Wait();
                    //}
                    //catch (Exception e)
                    //{
                    //    logger.Info(e.Message);
                    //}

                    // update task data store at the end of tmin task
                    taskLatLon.ContinueWith(t =>
                    {
                        response = t.Result;
                        Helpers.TaskDataStore.SetTaskResult(taskLatLon.Id.ToString(), response);
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            tokenSource.Cancel();
                        }
                    }, TaskContinuationOptions.NotOnCanceled);


                    // start a task to generate NLCD dataset for the watershed
                    Task<HttpResponseMessage> taskLandCoverData = Task<HttpResponseMessage>.Factory.StartNew((t) =>
                        GetWatershedLandCoverData(cancellationToken), tokenSource.Token, TaskCreationOptions.AttachedToParent);
                    Helpers.TaskDataStore.SetTask(taskLandCoverData.Id.ToString(), taskLandCoverData);
                    Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskLandCoverData.Id.ToString());

                    // set a task to generate land cover variable data
                    Task<HttpResponseMessage> taskLandCoverVariables = new Task<HttpResponseMessage>(() =>
                        GetWatershedLandCoverVariablesData(cancellationToken), tokenSource.Token, TaskCreationOptions.AttachedToParent);

                    // start land cover varaible data task when land cover data task ends
                    taskLandCoverData.ContinueWith(t =>
                    {
                        response = t.Result;
                        Helpers.TaskDataStore.SetTaskResult(taskLandCoverData.Id.ToString(), response);
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            tokenSource.Cancel();
                        }
                        else if (taskLandCoverVariables.IsCanceled == false)
                        {
                            taskLandCoverVariables.Start();
                            Helpers.TaskDataStore.SetTask(taskLandCoverVariables.Id.ToString(), taskLandCoverVariables);
                            Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskLandCoverVariables.Id.ToString());
                        }
                    }, TaskContinuationOptions.NotOnCanceled);

                    // update task data store when this task is done
                    taskLandCoverVariables.ContinueWith(t =>
                    {
                        response = t.Result;
                        Helpers.TaskDataStore.SetTaskResult(taskLandCoverVariables.Id.ToString(), response);
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            tokenSource.Cancel();
                        }
                    }, TaskContinuationOptions.NotOnCanceled);

                    // put the tmin and tmax tasks into an array to monitor when both of them get finished
                    Task<ResponseMessage>[] tMinTmaxTasks = new Task<ResponseMessage>[2];

                    // start a task to generate daily single tmin data netcdf file for the watershed
                    string outputTminDataVarName = "tmin";
                    string inputDaymetTminFileNamePattern = "tmin*.nc";
                    Task<ResponseMessage> taskTmin = Task<ResponseMessage>.Factory.StartNew((t) =>
                    {
                        return DataProcessor.GetWatershedSingleTempDataPointNetCDFFile(cancellationToken, outputTminDataVarName, inputDaymetTminFileNamePattern, _packageRequestProcessRootDirPath, startDate, endDate);
                    }, tokenSource.Token, TaskCreationOptions.AttachedToParent);

                    Helpers.TaskDataStore.SetTask(taskTmin.Id.ToString(), taskTmin);
                    Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskTmin.Id.ToString());
                    tMinTmaxTasks[0] = taskTmin;
                    daymetTaskList.Add(taskTmin);

                    // make this wait as the following tmax task uses the same input file                    
                    //try
                    //{
                    //    taskTmin.Wait();
                    //}
                    //catch (Exception e)
                    //{
                    //    logger.Info(e.Message);
                    //}

                    //string outputTmaxDataVarName = "tmax";
                    //string inputDaymetTmaxFileNamePattern = "tmax*.nc";
                    //Task<ResponseMessage> taskTmax = new Task<ResponseMessage>(() =>
                    //   DataProcessor.GetWatershedSingleTempDataPointNetCDFFile(cancellationToken, outputTmaxDataVarName, inputDaymetTmaxFileNamePattern));
                    // update task data store at the end of tmin task
                    taskTmin.ContinueWith(t =>
                    {
                        daymetResponse = t.Result;
                        if (daymetResponse.StatusCode != ResponseStatus.OK)
                        {
                            tokenSource.Cancel();
                        }

                        Helpers.TaskDataStore.SetTaskResult(taskTmin.Id.ToString(), daymetResponse);
                    }, TaskContinuationOptions.NotOnCanceled);


                    // start a task to generate daily single tmax data netcdf file for the watershed
                    string outputTmaxDataVarName = "tmax";
                    string inputDaymetTmaxFileNamePattern = "tmax*.nc";
                    Task<ResponseMessage> taskTmax = Task<ResponseMessage>.Factory.StartNew((t) =>
                    {
                        return DataProcessor.GetWatershedSingleTempDataPointNetCDFFile(cancellationToken, outputTmaxDataVarName, inputDaymetTmaxFileNamePattern, _packageRequestProcessRootDirPath, startDate, endDate);
                    }, tokenSource.Token, TaskCreationOptions.AttachedToParent);

                    Helpers.TaskDataStore.SetTask(taskTmax.Id.ToString(), taskTmax);
                    Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskTmax.Id.ToString());
                    tMinTmaxTasks[1] = taskTmax;
                    daymetTaskList.Add(taskTmax);

                    // make this wait as the following tmax task uses the same input file                    
                    //try
                    //{
                    //    taskTmax.Wait();
                    //}
                    //catch (Exception e)
                    //{
                    //    logger.Info(e.Message);
                    //}

                    // create a task that will monitor when all tasks in tMinTmaxTasks array are complete
                    Task tMinTMaxTaskManager = Task.WhenAll(tMinTmaxTasks);

                    // put the multiple temp and vp tasks into an array to monitor when they both get done so that we can start
                    // the task for multiple rh data file generation
                    Task<ResponseMessage>[] taVpMultipleTasks = new Task<ResponseMessage>[2];

                    // set a task to generate daily multiple temperature data netcdf file for watershed when tmin and tmax netcdf file generation have finished         
                    Task<ResponseMessage> taskMultipleTemp = new Task<ResponseMessage>(() =>
                        DataProcessor.GetWatershedMultipleTempDataPointsNetCDFFile(cancellationToken, timeStep, _packageRequestProcessRootDirPath, startDate), tokenSource.Token, TaskCreationOptions.AttachedToParent);
                    daymetTaskList.Add(taskMultipleTemp);
                    taVpMultipleTasks[0] = taskMultipleTemp;
                    tMinTMaxTaskManager.ContinueWith(t =>
                    {
                        if (taskMultipleTemp.IsCanceled == false)
                        {
                            taskMultipleTemp.Start();
                            Helpers.TaskDataStore.SetTask(taskMultipleTemp.Id.ToString(), taskMultipleTemp);
                            Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskMultipleTemp.Id.ToString());
                        }
                    });

                    // set a task to generate daily multiple vp data netcdf file for waterhed      
                    Task<ResponseMessage> taskMultipleVp = new Task<ResponseMessage>(() =>
                        DataProcessor.GetWatershedMultipleVaporPresDataPointsNetCDFFile(cancellationToken, timeStep, _packageRequestProcessRootDirPath, startDate), tokenSource.Token, TaskCreationOptions.AttachedToParent);
                    daymetTaskList.Add(taskMultipleVp);
                    taVpMultipleTasks[1] = taskMultipleVp;

                    // create a task that will monitor when all task in taVpMultipleTasks array completes
                    Task taVpMultipleTaskManager = Task.WhenAll(taVpMultipleTasks);

                    // when tmax data generation completes (tmin task is also done at this time), start ta multiplda daily data genration task
                    taskTmax.ContinueWith(t =>
                    {
                        daymetResponse = t.Result;
                        Helpers.TaskDataStore.SetTaskResult(taskTmax.Id.ToString(), daymetResponse);
                        if (daymetResponse.StatusCode != ResponseStatus.OK)
                        {
                            tokenSource.Cancel();
                        }
                        else if (taskMultipleTemp.IsCanceled == false)
                        {
                            //taskMultipleTemp.Start();
                            //Helpers.TaskDataStore.SetTask(taskMultipleTemp.Id.ToString(), taskMultipleTemp);
                            //Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskMultipleTemp.Id.ToString());
                        }
                    }, TaskContinuationOptions.NotOnCanceled);


                    // start a task to generate daily single vapor pressure netcdf file for watershed - note this task may start independent of tmax and ta multiple data generation tasks
                    string inputDaymetVpFileNamePattern = "vp*.nc";
                    Task<ResponseMessage> taskVp = Task<ResponseMessage>.Factory.StartNew((t) =>
                    {
                        return DataProcessor.GetWatershedSingleVaporPresDataPointNetCDFFile(cancellationToken, inputDaymetVpFileNamePattern, _packageRequestProcessRootDirPath, startDate, endDate);
                    }, tokenSource.Token, TaskCreationOptions.AttachedToParent);

                    Helpers.TaskDataStore.SetTask(taskVp.Id.ToString(), taskVp);
                    Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskVp.Id.ToString());
                    daymetTaskList.Add(taskVp);

                    //make this wait since the single precp task uses the same input file                    
                    //try
                    //{
                    //    taskVp.Wait();
                    //}
                    //catch (Exception e)
                    //{
                    //    logger.Info(e.Message);
                    //}

                    // start a task to generate daily single prec netcdf file for watershed
                    string inputDaymetPrcpFileNamePattern = "prcp*.nc";
                    Task<ResponseMessage> taskPrecp = Task<ResponseMessage>.Factory.StartNew((t) =>
                    {
                        return DataProcessor.GetWatershedSinglePrecpDataPointNetCDFFile(cancellationToken, inputDaymetPrcpFileNamePattern, _packageRequestProcessRootDirPath, startDate, endDate);
                    }, tokenSource.Token, TaskCreationOptions.AttachedToParent);

                    Helpers.TaskDataStore.SetTask(taskPrecp.Id.ToString(), taskPrecp);
                    Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskPrecp.Id.ToString());
                    daymetTaskList.Add(taskPrecp);

                    // set a task to generate daily multiple prec data netcdf file for waterhed      
                    Task<ResponseMessage> taskMultiplePrecp = new Task<ResponseMessage>(() => DataProcessor.GetWatershedMultiplePrecpDataPointsNetCDFFile(cancellationToken, timeStep, _packageRequestProcessRootDirPath, startDate), tokenSource.Token, TaskCreationOptions.AttachedToParent);
                    daymetTaskList.Add(taskMultiplePrecp);

                    // when the daily single prec netcdf task ends start the daily multiple precp task
                    taskPrecp.ContinueWith(t =>
                    {
                        daymetResponse = t.Result;
                        Helpers.TaskDataStore.SetTaskResult(taskPrecp.Id.ToString(), daymetResponse);
                        if (daymetResponse.StatusCode != ResponseStatus.OK)
                        {
                            tokenSource.Cancel();
                        }
                        else if (taskMultiplePrecp.IsCanceled == false)
                        {
                            taskMultiplePrecp.Start();
                            Helpers.TaskDataStore.SetTask(taskMultiplePrecp.Id.ToString(), taskMultiplePrecp);
                            Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskMultiplePrecp.Id.ToString());
                        }
                    }, TaskContinuationOptions.NotOnCanceled);

                    // set a task to generate daily multiple wind data netcdf file for watershed      
                    Task<ResponseMessage> taskMultipleWind = new Task<ResponseMessage>(() => DataProcessor.GetWatershedMultipleWindDataPointsNetCDFFile(cancellationToken, constantWindSpeed, _packageRequestProcessRootDirPath), tokenSource.Token, TaskCreationOptions.AttachedToParent);
                    daymetTaskList.Add(taskMultipleWind);

                    // when daily multiple precp task is done start the multiple wind task - wind task uses the precp netcdf file
                    taskMultiplePrecp.ContinueWith(t =>
                    {
                        daymetResponse = t.Result;
                        Helpers.TaskDataStore.SetTaskResult(taskMultiplePrecp.Id.ToString(), daymetResponse);
                        if (daymetResponse.StatusCode != ResponseStatus.OK)
                        {
                            tokenSource.Cancel();
                        }
                        else if (taskMultipleWind.IsCanceled == false)
                        {
                            taskMultipleWind.Start();
                            Helpers.TaskDataStore.SetTask(taskMultipleWind.Id.ToString(), taskMultipleWind);
                            Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskMultipleWind.Id.ToString());
                        }
                    }, TaskContinuationOptions.NotOnCanceled);

                    // when daily multiple wind task is done update the task data store
                    taskMultipleWind.ContinueWith(t =>
                    {
                        daymetResponse = t.Result;
                        Helpers.TaskDataStore.SetTaskResult(taskMultipleWind.Id.ToString(), daymetResponse);
                        if (daymetResponse.StatusCode != ResponseStatus.OK)
                        {
                            tokenSource.Cancel();
                        }
                    }, TaskContinuationOptions.NotOnCanceled);

                    // when the daily single vp netcdf task ends start the daily multiple vp task
                    taskVp.ContinueWith(t =>
                    {
                        daymetResponse = t.Result;
                        Helpers.TaskDataStore.SetTaskResult(taskVp.Id.ToString(), daymetResponse);
                        if (daymetResponse.StatusCode != ResponseStatus.OK)
                        {
                            tokenSource.Cancel();
                        }
                        else if (taskMultipleVp.IsCanceled == false)
                        {
                            taskMultipleVp.Start();
                            Helpers.TaskDataStore.SetTask(taskMultipleVp.Id.ToString(), taskMultipleVp);
                            Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskMultipleVp.Id.ToString());
                        }
                    }, TaskContinuationOptions.NotOnCanceled);

                    // when daily multiple vp task is done update the task data store
                    taskMultipleVp.ContinueWith(t =>
                    {
                        daymetResponse = t.Result;
                        Helpers.TaskDataStore.SetTaskResult(taskMultipleVp.Id.ToString(), daymetResponse);
                        if (daymetResponse.StatusCode != ResponseStatus.OK)
                        {
                            tokenSource.Cancel();
                        }
                    }, TaskContinuationOptions.NotOnCanceled);

                    // set a task to generate daily multiple rh data netcdf file for waterhed      
                    Task<ResponseMessage> taskMultipleRH = new Task<ResponseMessage>(() =>
                        DataProcessor.GetWatershedMultipleRHDataPointsNetCDFFile(cancellationToken, timeStep, _packageRequestProcessRootDirPath), tokenSource.Token, TaskCreationOptions.AttachedToParent);
                    daymetTaskList.Add(taskMultipleRH);

                    // start the multiple daily rh task when multiple temp and multiple vp tasks have finished
                    taVpMultipleTaskManager.ContinueWith((t) =>
                    {
                        if (taskMultipleRH.IsCanceled == false)
                        {
                            taskMultipleRH.Start();
                            Helpers.TaskDataStore.SetTask(taskMultipleRH.Id.ToString(), taskMultipleRH);
                            Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskMultipleRH.Id.ToString());
                        }
                    }, TaskContinuationOptions.NotOnCanceled);

                    // when daily multiple rh task is done update the task data store
                    taskMultipleRH.ContinueWith(t =>
                    {
                        daymetResponse = t.Result;
                        Helpers.TaskDataStore.SetTaskResult(taskMultipleRH.Id.ToString(), daymetResponse);
                        if (daymetResponse.StatusCode != ResponseStatus.OK)
                        {
                            tokenSource.Cancel();
                        }
                    }, TaskContinuationOptions.NotOnCanceled);


                    // set a task to monitor all daymet data processing tasks we have defined above
                    Task daymetTaskManager = Task.WhenAll(daymetTaskList);

                    // create a task to generate the packgae zip file
                    Task<HttpResponseMessage> pkgTask = new Task<HttpResponseMessage>(() => CreateUEBpackageZipFile(jobGuid.ToString(), startDate, endDate, timeStep, null), tokenSource.Token, TaskCreationOptions.AttachedToParent);

                    // when all daymet tasks are done, start the task for creating the package zip file
                    daymetTaskManager.ContinueWith((t) =>
                    {
                        pkgTask.Start();
                        Helpers.TaskDataStore.SetTask(pkgTask.Id.ToString(), pkgTask);
                        Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), pkgTask.Id.ToString());
                    }, TaskContinuationOptions.NotOnCanceled);

                }, tokenSource.Token);

                mainTask.Start();
                Helpers.TaskDataStore.SetTask(jobGuid.ToString(), mainTask);
                mainTask.ContinueWith((t) =>
                {
                    if (mainTask.IsCanceled || mainTask.IsFaulted || tokenSource.IsCancellationRequested)
                    {
                        CleanUpOnFailure();
                        string errMsg = "UEB pacakage could not be created.";
                        logger.Info(errMsg);
                        //UpdatePackageBuildStatusFile(PackageBuildStatus.Error);
                    }
                    else
                    {
                        response.StatusCode = HttpStatusCode.OK;
                        response.Content = new StringContent("UEB package creation is complete for job id:" + jobGuid);
                        Helpers.TaskDataStore.SetTaskResult(jobGuid.ToString(), response);
                        logger.Info(response.Content.ToString());
                        //UpdatePackageBuildStatusFile(PackageBuildStatus.Complete);
                    }
                }, TaskContinuationOptions.NotOnCanceled);
            }
            catch (AggregateException e)
            {
                string responseMsg = string.Empty;
                foreach (var v in e.InnerExceptions)
                {
                    responseMsg += v.Message + "\n";
                }
                logger.Error(responseMsg);
            }
            catch (Exception ex)
            {
                tokenSource.Cancel();
                string errMsg = "UEB package creation failed.\n" + ex.Message;
                logger.Error(errMsg);
            }

            PackageCreationStatus pkgStatus = new PackageCreationStatus();
            pkgStatus.Message = "UEB package creation has started. When done you will be notified.";
            pkgStatus.PackageID = jobGuid.ToString();
            string jsonResponse = JsonConvert.SerializeObject(pkgStatus, Formatting.Indented);
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(jsonResponse);
            return response;
        }
        #region private methods
        private HttpResponseMessage CreateUEBpackageZipFile(string packageID, DateTime startDate, DateTime endDate, byte timeStep, UEBPackageRequest uebPkgRequest)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                if (Directory.Exists(_sourceFilePath) == false)
                {
                    string errMsg = string.Format("UEB package source file path ({0}) doesn't exist.", _sourceFilePath);
                    logger.Error(errMsg);
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.Content = new StringContent(errMsg);
                    return response;
                }
                else if (Directory.GetFiles(_sourceFilePath).Length == 0)
                {
                    string errMsg = string.Format("There are no files to be zipped in the specified directory ({0}).", _sourceFilePath);
                    logger.Error(errMsg);
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.Content = new StringContent(errMsg);
                    return response;
                }

                if (Directory.Exists(_targetPackageDirPath) == false)
                {
                    Directory.CreateDirectory(_targetPackageDirPath);
                }

                if (Directory.Exists(_targetTempPackageFilePath) == false)
                {
                    Directory.CreateDirectory(_targetTempPackageFilePath);
                }
                else
                {
                    DirectoryInfo dir = new DirectoryInfo(_targetTempPackageFilePath);
                    dir.Delete(true);
                    Directory.CreateDirectory(_targetTempPackageFilePath);

                    // create the model output sub directory if necessary
                    SetModelOutputFolder(uebPkgRequest);
                    if (string.IsNullOrEmpty(uebPkgRequest.OutputFolderName) == false)
                    {
                        string modeOutputSubDir = Path.Combine(_targetTempPackageFilePath, uebPkgRequest.OutputFolderName);
                        Directory.CreateDirectory(modeOutputSubDir);
                    }
                }

                // create the control files (.dat files)
                UEBFileManager.GenerateControlFiles(_sourceFilePath, startDate, endDate, timeStep, uebPkgRequest);

                // copy selected files from the source dir to the temp package dir
                DirectoryInfo sourceFilesDir = new DirectoryInfo(_sourceFilePath);

                string[] files = Directory.GetFiles(_sourceFilePath);
                string fileName = string.Empty;
                string destFile = string.Empty;

                foreach (string file in files)
                {
                    if (Path.GetExtension(file) == ".nc" || Path.GetExtension(file) == ".dat")
                    {
                        // Use static Path methods to extract only the file name from the path.
                        fileName = Path.GetFileName(file);
                        destFile = Path.Combine(_targetTempPackageFilePath, fileName);

                        // Copy the file and overwrite destination file if they already exist.
                        File.Copy(file, destFile, true);

                        // delete the file at the source directory
                        File.Delete(file);
                    }
                }

                //create a zip file of all files that are part of the model package
                string zipUEBPackage = Path.Combine(_targetPackageDirPath, _packageZipFileName);

                if (File.Exists(zipUEBPackage))
                {
                    File.Delete(zipUEBPackage);
                }

                ZipFile.CreateFromDirectory(_targetTempPackageFilePath, zipUEBPackage);

            }
            catch (Exception ex)
            {
                logger.Fatal(ex.Message);
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.Content = new StringContent(ex.Message);
                return response;
            }

            string msg = "UEB package creation was successful for job id:" + packageID;
            logger.Info(msg);
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(msg);
            return response;
        }

        private void SetModelOutputFolder(UEBPackageRequest uebPkgRequest)
        {
            // TODO: see PK Weekly progress folder in onenote for date5/23/2013
            // the requirements for retrieving folder (inclding subfolder name) for each of the 
            // file specified in the outputcontrol file
            // The following logic at this point gets the folder name from the first file specfied it ouputcontrol file
            // and is also not getting any subfolders
            string outputControlFile = Path.Combine(_clientPackageRequestDirPath, uebPkgRequest.OutputControlFileName);
            string data = string.Empty;
            int fileLineCount = 0;
            char[] splitChar = new char[] { '\\' };
            using (StreamReader sr = new StreamReader(outputControlFile))
            {
                while (sr.EndOfStream == false)
                {
                    fileLineCount++;
                    data = sr.ReadLine();
                    if (fileLineCount == 3)
                    {
                        string[] splitStrings = data.Split(splitChar);
                        if (splitStrings.Length > 0)
                        {
                            uebPkgRequest.OutputFolderName = splitStrings[0];
                        }
                        break;
                    }
                }
            }
        }

        private HttpResponseMessage GenerateBufferedWatershedFiles(CancellationToken ct, int watershedBufferSize)
        {
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            GenerateBufferedWatershedFilesController ctl = new GenerateBufferedWatershedFilesController();
            return ctl.GetBufferedWatershedFiles(_packageRequestProcessRootDirPath, watershedBufferSize);
        }

        private HttpResponseMessage GenerateWatershedNetCDFFile(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            GenerateWatershedNetCDFFileController ctl = new GenerateWatershedNetCDFFileController();
            return ctl.GetWatershedNetCDFFile(_packageRequestProcessRootDirPath);
        }

        private HttpResponseMessage GenerateWatershedDEMFile(CancellationToken ct, int gridCellSize)
        {
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            CreateWatershedDEMFileController ctl = new CreateWatershedDEMFileController();
            return ctl.GetWatershedDEMFile(gridCellSize, _packageRequestProcessRootDirPath);
        }

        private HttpResponseMessage GetWatershedAtmosphericPressure(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            ComputeWatershedAtmosphericPressureController ctl = new ComputeWatershedAtmosphericPressureController();
            return ctl.GetWatershedAtmosphericPressure(_packageRequestProcessRootDirPath);
        }

        private HttpResponseMessage GetWatershedSlopeNetCDFFile(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }

            GenerateWatershedSlopeNetCdfFileController ctl = new GenerateWatershedSlopeNetCdfFileController();
            return ctl.GetWatershedSlopeNetCDFFile(_packageRequestProcessRootDirPath);
        }

        private HttpResponseMessage GetWatershedAspectNetCDFFile(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            GenerateWatershedAspectNetCdfFileController ctl = new GenerateWatershedAspectNetCdfFileController();
            return ctl.GetWatershedAspectNetCDFFile(_packageRequestProcessRootDirPath);
        }

        private HttpResponseMessage GetWatershedLatLonValues(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            GenerateWatershedLatLonValuesController ctl = new GenerateWatershedLatLonValuesController();
            return ctl.GetWatershedLatLonValues(_packageRequestProcessRootDirPath);
        }

        private HttpResponseMessage GetWatershedLandCoverData(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            GenerateWatershedLandCoverDataController ctl = new GenerateWatershedLandCoverDataController();
            return ctl.GetWatershedLandCoverData(_packageRequestProcessRootDirPath);
        }

        private HttpResponseMessage GetWatershedLandCoverVariablesData(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {               
                return GetCancellationResponse();
            }
            GenerateWatershedLandCoverVariablesDataController ctl = new GenerateWatershedLandCoverVariablesDataController();
            return ctl.GetWatershedLandCoverVariablesData(_packageRequestProcessRootDirPath);
        }

        private HttpResponseMessage GetCancellationResponse()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.BadRequest;
            response.Content = new StringContent("Job has been cancelled.");
            return response;
        }

        private void CleanUpOnFailure()
        {            
            if (Directory.Exists(_packageRequestProcessRootDirPath))
            {
                Directory.Delete(_packageRequestProcessRootDirPath, true);
                logger.Info(string.Format("UEB build package temporary folder: {0} was deleted.", _packageRequestProcessRootDirPath));
            }
        }
        #endregion
    }
}