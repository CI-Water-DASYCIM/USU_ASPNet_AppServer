﻿using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using UWRL.CIWaterNetServer.Daymet;
using UWRL.CIWaterNetServer.Helpers;
using UWRL.CIWaterNetServer.UEB;

namespace UWRL.CIWaterNetServer.Controllers
{
    public class GenerateUEBPackageController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
                
        private string _sourceFilePath = string.Empty;
        private string _targetTempPackageFilePath = string.Empty;
        private string _targetPackageDirPath = string.Empty;
        private const string _packageZipFileName = "UEBPackage.zip";

        public GenerateUEBPackageController()
        {
            if (EnvironmentSettings.IsLocalHost)
            {
                _sourceFilePath = @"E:\CIWaterData\Temp";
                _targetTempPackageFilePath = @"E:\CIWaterData\Temp\UEBPackageFiles";
                _targetPackageDirPath = @"E:\CIWaterData\Temp\UEBPackageZip";
            }
            else
            {
                _sourceFilePath = @"C:\CIWaterData\Temp";
                _targetTempPackageFilePath = @"C:\CIWaterData\Temp\UEBPackageFiles";
                _targetPackageDirPath = @"C:\CIWaterData\Temp\UEBPackageZip";
            }
        }

        //This one to test if we can create the pacakge zip file
        //This method assumes that all files need to be zipped are at the _sourceFilePath
        //public HttpResponseMessage GetUEBPackage(string test)
        //{
        //    return CreateUEBpackageZipFile("123");

        //}

        public HttpResponseMessage GetUEBPackageCreate(DateTime startDate, DateTime endDate, byte timeStep)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            ResponseMessage daymetResponse;
            List<byte> validTimeStepValues = new List<byte>{1, 2, 3, 4, 6};
            var tokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = tokenSource.Token;

            Stopwatch stopWatch = new Stopwatch();

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
            if(startDate >= endDate)
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
                    stopWatch = new Stopwatch();
                    //ResponseMessage daymetResponse;
                    //int timeStep = 6; //TODO: this value needs to be passed as a parameter in GetUEBPackage method 
                    const float constantWindSpeed = 2.0f;
                    GenerateBufferedWatershedFiles(cancellationToken);
                    GenerateWatershedDEMFile(cancellationToken);
                    GenerateWatershedNetCDFFile(cancellationToken);                    
                    GetWatershedAtmosphericPressure(cancellationToken);
                    GetWatershedSlopeNetCDFFile(cancellationToken);
                    GetWatershedAspectNetCDFFile(cancellationToken);
                    GetWatershedLatLonValues(cancellationToken);
                    GetWatershedLandCoverData(cancellationToken);
                    GetWatershedLandCoverVariablesData(cancellationToken);

                    string outputTminDataVarName = "tmin";
                    string inputDaymetTminFileNamePattern = "tmin*.nc";
                    daymetResponse =  DataProcessor.GetWatershedSingleTempDataPointNetCDFFile(cancellationToken, outputTminDataVarName, inputDaymetTminFileNamePattern);
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                    }

                    string outputTmaxDataVarName = "tmax";
                    string inputDaymetTmaxFileNamePattern = "tmax*.nc";
                    daymetResponse = DataProcessor.GetWatershedSingleTempDataPointNetCDFFile(cancellationToken, outputTmaxDataVarName, inputDaymetTmaxFileNamePattern);
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                    }

                    daymetResponse =  DataProcessor.GetWatershedMultipleTempDataPointsNetCDFFile(cancellationToken, timeStep);
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                    }

                    string inputDaymetVpFileNamePattern = "vp*.nc";
                    daymetResponse = DataProcessor.GetWatershedSingleVaporPresDataPointNetCDFFile(cancellationToken, inputDaymetVpFileNamePattern);
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                    }

                    daymetResponse = DataProcessor.GetWatershedMultipleVaporPresDataPointsNetCDFFile(cancellationToken, timeStep);
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                    }

                    string inputDaymetPrcpFileNamePattern = "prcp*.nc";
                    daymetResponse = DataProcessor.GetWatershedSinglePrecpDataPointNetCDFFile(cancellationToken, inputDaymetPrcpFileNamePattern);
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                    }

                    daymetResponse = DataProcessor.GetWatershedMultiplePrecpDataPointsNetCDFFile(cancellationToken, timeStep);
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                    }

                    daymetResponse = DataProcessor.GetWatershedMultipleWindDataPointsNetCDFFile(cancellationToken, constantWindSpeed);
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                    }

                    daymetResponse = DataProcessor.GetWatershedMultipleRHDataPointsNetCDFFile(cancellationToken, timeStep);
                    if (daymetResponse.StatusCode != ResponseStatus.OK)
                    {
                        tokenSource.Cancel();
                    }

                    if (tokenSource.IsCancellationRequested == false)
                    {
                        CreateUEBpackageZipFile(jobGuid.ToString(), startDate, endDate, timeStep);
                    }
                    
                }, tokenSource.Token);

                mainTask.Start();
                stopWatch.Start();
                Helpers.TaskDataStore.SetTask(jobGuid.ToString(), mainTask);
                mainTask.ContinueWith((t) =>
                {
                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;

                    // Format and display the TimeSpan value. 
                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);

                    if (mainTask.IsCanceled || mainTask.IsFaulted)
                    {
                        CleanUpOnFailure();
                        string errMsg = "UEB pacakage could not be created.";
                        logger.Info(errMsg);
                    }
                    else
                    {
                        HttpResponseMessage mainTaskResponse = new HttpResponseMessage();
                        mainTaskResponse.StatusCode = HttpStatusCode.OK;
                        mainTaskResponse.Content = new StringContent("UEB package creation is complete for job id:" + jobGuid);
                        Helpers.TaskDataStore.SetTaskResult(jobGuid.ToString(), mainTaskResponse);
                        logger.Info(mainTaskResponse.Content.ToString());
                    }

                    logger.Info("UEB Package run time:" + elapsedTime);
                });      
            }
            catch(Exception ex)
            {
                HttpResponseMessage mainTaskResponse = new HttpResponseMessage();
                mainTaskResponse.StatusCode = HttpStatusCode.Forbidden;
                mainTaskResponse.Content = new StringContent("UEB package creation was unscuccessful for job id:" + jobGuid + "\n" + ex.Message);
                Helpers.TaskDataStore.SetTaskResult(jobGuid.ToString(), mainTaskResponse);
                logger.Info(mainTaskResponse.Content.ToString());
                CleanUpOnFailure();
            }

            PackageCreationStatus pkgStatus = new PackageCreationStatus();
            pkgStatus.Message = "UEB package creation has started. When done you will be notified.";
            pkgStatus.PackageID = jobGuid.ToString();
            string jsonResponse = JsonConvert.SerializeObject(pkgStatus, Formatting.Indented);
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(jsonResponse);
            return response;   

        }
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
            List<byte> validTimeStepValues = new List<byte>{1, 2, 3, 4, 6};

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
            if(startDate >= endDate)
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
                    const float constantWindSpeed = 2.0f;

                    // create a task array to monitor all tasks related to daymet data processing                    
                    List<Task> daymetTaskList = new List<Task>();

                    // start a new task to generate netcdf file for the watershed which would also generate watershed buffered shape file
                    Task<HttpResponseMessage> taskCreateWsNetCDFFile = Task<HttpResponseMessage>.Factory.StartNew((t) =>
                        GenerateWatershedNetCDFFile(cancellationToken), tokenSource.Token, TaskCreationOptions.AttachedToParent);

                    Helpers.TaskDataStore.SetTask(taskCreateWsNetCDFFile.Id.ToString(), taskCreateWsNetCDFFile);
                    Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskCreateWsNetCDFFile.Id.ToString());

                    // create a task to generate watershed DEM file
                    Task<HttpResponseMessage> taskCreateWsDEMFile = new Task<HttpResponseMessage>(() =>
                        GenerateWatershedDEMFile(cancellationToken), tokenSource.Token, TaskCreationOptions.AttachedToParent);

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
                        return DataProcessor.GetWatershedSingleTempDataPointNetCDFFile(cancellationToken, outputTminDataVarName, inputDaymetTminFileNamePattern);
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
                        return DataProcessor.GetWatershedSingleTempDataPointNetCDFFile(cancellationToken, outputTmaxDataVarName, inputDaymetTmaxFileNamePattern);
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
                        DataProcessor.GetWatershedMultipleTempDataPointsNetCDFFile(cancellationToken, timeStep), tokenSource.Token, TaskCreationOptions.AttachedToParent);
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
                        DataProcessor.GetWatershedMultipleVaporPresDataPointsNetCDFFile(cancellationToken, timeStep), tokenSource.Token, TaskCreationOptions.AttachedToParent);
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
                        return DataProcessor.GetWatershedSingleVaporPresDataPointNetCDFFile(cancellationToken, inputDaymetVpFileNamePattern);
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
                        return DataProcessor.GetWatershedSinglePrecpDataPointNetCDFFile(cancellationToken, inputDaymetPrcpFileNamePattern);
                    }, tokenSource.Token, TaskCreationOptions.AttachedToParent);

                    Helpers.TaskDataStore.SetTask(taskPrecp.Id.ToString(), taskPrecp);
                    Helpers.TaskDataStore.SetChildTask(jobGuid.ToString(), taskPrecp.Id.ToString());
                    daymetTaskList.Add(taskPrecp);

                    // set a task to generate daily multiple prec data netcdf file for waterhed      
                    Task<ResponseMessage> taskMultiplePrecp = new Task<ResponseMessage>(() => DataProcessor.GetWatershedMultiplePrecpDataPointsNetCDFFile(cancellationToken, timeStep), tokenSource.Token, TaskCreationOptions.AttachedToParent);
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
                    Task<ResponseMessage> taskMultipleWind = new Task<ResponseMessage>(() => DataProcessor.GetWatershedMultipleWindDataPointsNetCDFFile(cancellationToken, constantWindSpeed), tokenSource.Token, TaskCreationOptions.AttachedToParent);
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
                        DataProcessor.GetWatershedMultipleRHDataPointsNetCDFFile(cancellationToken, timeStep), tokenSource.Token, TaskCreationOptions.AttachedToParent);
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
                    Task<HttpResponseMessage> pkgTask = new Task<HttpResponseMessage>(() => CreateUEBpackageZipFile(jobGuid.ToString(), startDate, endDate, timeStep), tokenSource.Token, TaskCreationOptions.AttachedToParent);

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
                    }
                    else
                    {
                        response.StatusCode = HttpStatusCode.OK;
                        response.Content = new StringContent("UEB package creation is complete for job id:" + jobGuid);
                        Helpers.TaskDataStore.SetTaskResult(jobGuid.ToString(), response);
                        logger.Info(response.Content.ToString());
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

        private HttpResponseMessage CreateUEBpackageZipFile(string packageID, DateTime startDate, DateTime endDate, byte timeStep)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                if (Directory.Exists(_sourceFilePath) == false)
                {
                    string errMsg = "Internal error: Source file path doesn't exist.";
                    logger.Error(errMsg);
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);                    
                }
                else if (Directory.GetFiles(_sourceFilePath).Length == 0)
                {
                    string errMsg = "Internal error: No source files can be found to create an UEB package.";
                    logger.Error(errMsg);
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
                }

                _targetPackageDirPath += @"\" + packageID;

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
                }

                //create the control files (.dat files)
                UEBFileManager.GenerateControlFiles(_sourceFilePath, startDate, endDate, timeStep);

                // copy selected files from the source dir to the temp package dir
                DirectoryInfo sourceFilesDir = new DirectoryInfo(_sourceFilePath);

                string[] files = Directory.GetFiles(_sourceFilePath);

                string fileName = string.Empty;
                string destFile = string.Empty;

                foreach (string file in files)
                {
                    // TODO: Don't copy the .txt files as the data in these text files is extracted
                    // and put into control files (.dat files). Remove the condition: Path.GetExtension(file) == ".txt" from the if statement
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

                //create a zip file of all shapes files
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
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

            logger.Info("UEB package creation was successful.");
            return response;
        }

        private HttpResponseMessage GenerateBufferedWatershedFiles(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            GenerateBufferedWatershedFilesController ctl = new GenerateBufferedWatershedFilesController();
            return ctl.GetBufferedWatershedFiles();
        }
        private HttpResponseMessage GenerateWatershedNetCDFFile(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            GenerateWatershedNetCDFFileController ctl = new GenerateWatershedNetCDFFileController();
            return ctl.GetWatershedNetCDFFile();
        }

        private HttpResponseMessage GenerateWatershedDEMFile(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            CreateWatershedDEMFileController ctl = new CreateWatershedDEMFileController();
            return ctl.GetWatershedDEMFile();
        }

        private HttpResponseMessage GetWatershedAtmosphericPressure(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            ComputeWatershedAtmosphericPressureController ctl = new ComputeWatershedAtmosphericPressureController();
            return ctl.GetWatershedAtmosphericPressure();
        }

        private HttpResponseMessage GetWatershedSlopeNetCDFFile(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }

            GenerateWatershedSlopeNetCdfFileController ctl = new GenerateWatershedSlopeNetCdfFileController();
            return ctl.GetWatershedSlopeNetCDFFile();
        }

        private HttpResponseMessage GetWatershedAspectNetCDFFile(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            GenerateWatershedAspectNetCdfFileController ctl = new GenerateWatershedAspectNetCdfFileController();
            return ctl.GetWatershedAspectNetCDFFile();
        }

        private HttpResponseMessage GetWatershedLatLonValues(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            GenerateWatershedLatLonValuesController ctl = new GenerateWatershedLatLonValuesController();
            return ctl.GetWatershedLatLonValues();
        }

        private HttpResponseMessage GetWatershedLandCoverData(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return GetCancellationResponse();
            }
            GenerateWatershedLandCoverDataController ctl = new GenerateWatershedLandCoverDataController();
            return ctl.GetWatershedLandCoverData();
        }

        private HttpResponseMessage GetWatershedLandCoverVariablesData(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {               
                return GetCancellationResponse();
            }
            GenerateWatershedLandCoverVariablesDataController ctl = new GenerateWatershedLandCoverVariablesDataController();
            return ctl.GetWatershedLandCoverVariablesData();
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
            string[] files = Directory.GetFiles(_sourceFilePath, "*.nc");
            files.ToList().ForEach(f => File.Delete(f));

        }
        #endregion
    }

    public class PackageCreationStatus
    {
        public string Message { get; set; }
        public string PackageID { get; set; }
    }
}