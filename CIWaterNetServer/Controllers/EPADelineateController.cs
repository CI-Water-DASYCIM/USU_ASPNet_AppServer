using DotSpatial.Data;
using DotSpatial.Projections;
using DotSpatial.Topology;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using UWRL.CIWaterNetServer.Models;
using System.Net.Http.Headers;
using NLog;
using UWRL.CIWaterNetServer.Helpers;

namespace UWRL.CIWaterNetServer.Controllers
{
    public class EPADelineateController : ApiController
    {
        #region Variables
        private static Logger logger = LogManager.GetCurrentClassLogger();        
        
        private string _targetShapeFilesDirPath = string.Empty;
        private string _targetTempShapeFilesDirPath = string.Empty; 
        private string _targetShapeFileZipDirPath = string.Empty;  //@"C:\temp\shapefiles\zip";
        
        /// <summary>
        /// watershed outlet point file name
        /// </summary>
        private string _wshedpointFile = UEB.UEBSettings.WATERSHED_POINT_SHAPE_FILE_NAME; // "Watershedpoint.shp";
                
        /// <summary>
        /// watershed boundary (in JSON format) file name
        /// </summary>
        private string _wshedFile = UEB.UEBSettings.WATERSHED_SHAPE_FILE_NAME; // "Watershed.shp";
                
        /// <summary>
        /// stream identifier file name
        /// </summary>
        private string _streamFile = UEB.UEBSettings.WATERSHED_STREAM_SHAPE_FILE_NAME; // "Stream.shp";
               
        private readonly ProjectionInfo WGS84 = KnownCoordinateSystems.Geographic.World.WGS1984;
        private IList<IFeatureSet> _featureSets = null;
        
        #endregion Variables

        #region constructor
        public EPADelineateController()
        {
           
            // >>> TODO: remove the magic strings as done in other classes
            
            //if (EnvironmentSettings.IsLocalHost)
            //{
            //    _targetShapeFilesDirPath = @"E:\CIWaterData\Temp";
            //    _targetTempShapeFilesDirPath = @"E:\CIWaterData\Temp\ShapeFiles";
            //    _targetShapeFileZipDirPath = @"E:\CIWaterData\Temp\zip";
            //}
            //else
            //{
            //    _targetShapeFilesDirPath = @"C:\CIWaterData\Temp";
            //    _targetTempShapeFilesDirPath = @"C:\CIWaterData\Temp\ShapeFiles";
            //    _targetShapeFileZipDirPath = @"C:\CIWaterData\Temp\zip";
            //}

            // >> new code begin
            _targetShapeFilesDirPath = UEB.UEBSettings.WORKING_DIR_PATH; // @"C:\CIWaterData\Temp";
            _targetTempShapeFilesDirPath = Path.Combine(UEB.UEBSettings.WORKING_DIR_PATH, "ShapeFiles"); // @"C:\CIWaterData\Temp\ShapeFiles";
            _targetShapeFileZipDirPath = Path.Combine(UEB.UEBSettings.WORKING_DIR_PATH, "ShapeFilesZip"); // @"C:\CIWaterData\Temp\zip";

            // create directories if they do not already exist
            if (!Directory.Exists(_targetShapeFilesDirPath))
            {
                Directory.CreateDirectory(_targetShapeFilesDirPath);
            }

            if (!Directory.Exists(_targetTempShapeFilesDirPath))
            {
                Directory.CreateDirectory(_targetTempShapeFilesDirPath);
            }

            if (!Directory.Exists(_targetShapeFileZipDirPath))
            {
                Directory.CreateDirectory(_targetShapeFileZipDirPath);
            }
            // >> new code end
        }

        #endregion

        #region HTTP public methods

        /// <summary>
        /// To test if the web service is up/running or not
        /// </summary>
        /// <returns></returns>
        private string GetEPADelineate()
        {
            return "Testing WEB API";
        }
                
        /// <summary>
        /// Get a zip file containing all shape files
        /// </summary>
        /// <remarks>
        /// HTTP GET call format:http://{server}/api/EPADelineate?watershedOutletLat=latValue&watershedOutletLon=lonValue
        /// </remarks>
        /// <param name="watershedOutletLat">Latidue of the watershed outlet location based on WGS84 datum</param>
        /// <param name="watershedOutletLon">Longitude of the watershed outlet location based on WGS84 datum</param>
        /// <returns>A zip file of all shape files, shape files data based on WGS84 datum</returns>
        public HttpResponseMessage GetShapeFiles(double watershedOutletLat, double watershedOutletLon)
        {
            HttpResponseMessage response = new HttpResponseMessage();
                       
            try
            {
                Delineate(watershedOutletLat, watershedOutletLon);

                // see the link below to continue implementing this method
                //http://stackoverflow.com/questions/12145390/how-to-set-downloading-file-name-in-asp-net-mvc-web-api
                // still need to figure out how all files associated with a shapefile (file names are same with diff extenions)
                // can be zipped and temporarily saved and then read to MemoryStream object as shown in the above link
                // and then retunred as part of the HttpresponseMessage

                if (Directory.Exists(_targetTempShapeFilesDirPath) == false)
                {
                    Directory.CreateDirectory(_targetTempShapeFilesDirPath);
                }
                else
                {
                    DirectoryInfo dir = new DirectoryInfo(_targetTempShapeFilesDirPath);
                    dir.Delete(true);
                    Directory.CreateDirectory(_targetTempShapeFilesDirPath);
                }
                           
                if (Directory.Exists(_targetShapeFileZipDirPath) == false)
                {
                    Directory.CreateDirectory(_targetShapeFileZipDirPath);
                }

                // copy all shape files from the Temp directory to _targetTempShapeFilesDirPath
                string[] shapeFiles = Directory.GetFiles(_targetShapeFilesDirPath, "*.shp");

                string fileName = string.Empty;
                string destFile = string.Empty;

                foreach (string file in shapeFiles)
                {
                    string shapeFileName = Path.GetFileNameWithoutExtension(file);
                                        
                    // get all files matching the above file name
                    string[] machingShapeFiles = Directory.GetFiles(_targetShapeFilesDirPath, shapeFileName + ".*");

                    foreach (string msFile in machingShapeFiles)
                    {
                        if (Path.GetExtension(msFile) != ".nc")
                        {
                            // Use static Path methods to extract only the file name from the path.
                            fileName = Path.GetFileName(msFile);
                            destFile = Path.Combine(_targetTempShapeFilesDirPath, fileName);

                            // Copy the files and overwrite destination files if they already exist.
                            File.Copy(file, destFile, true);
                        }
                    }
                    
                }

                // create a zip file of all shapes files
                string zipFilePath = Path.Combine(_targetShapeFileZipDirPath, "shapefiles.zip");

                if (File.Exists(zipFilePath))
                {
                    File.Delete(zipFilePath);
                }

                ZipFile.CreateFromDirectory(_targetTempShapeFilesDirPath, zipFilePath);
                
                // load the zip file to memory
                MemoryStream ms = new MemoryStream();

                using (FileStream file = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read))
                {                    
                    file.CopyTo(ms);                    
                    file.Close();
                    if (ms.Length == 0)
                    {                        
                        string errMsg = "No watershed exists for the selected outlet location.";
                        logger.Error(errMsg);
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
                    }
                    response.StatusCode = HttpStatusCode.OK;
                    ms.Position = 0;
                    response.Content = new StreamContent(ms);
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    //CacheControlHeaderValue cacheControlHeaderValue = new CacheControlHeaderValue();
                    //cacheControlHeaderValue.NoCache = true;
                    //response.Headers.CacheControl = cacheControlHeaderValue;

                    // Ref: http://weblogs.asp.net/cibrax/archive/2011/04/25/implementing-caching-in-your-wcf-web-apis.aspx
                    // set the browser to cache this response for 10 secs only
                    response.Content.Headers.Expires = DateTime.Now.AddSeconds(10);
                    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = UEB.UEBSettings.WATERSHED_SHAPE_ZIP_FILE_NAME // "shapefiles.zip"
                    };

                    logger.Info("All shape files were zipped.");
                }
            }
            catch (Exception ex)
            {                
                logger.Fatal(ex.Message);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }            
            
            return response;
        }

        #endregion HTTP public methods

        #region private methods

        private void Delineate(double watershedOutletLat, double watershedOutletLon)
        {
            Coordinate wsOutletLocation = new Coordinate();
            wsOutletLocation.X = watershedOutletLon;
            wsOutletLocation.Y = watershedOutletLat;

            _featureSets = GetShapes(wsOutletLocation);

            if (_featureSets == null)
            {
                string msg = "Invalid outlet location.";
                logger.Error(msg);
                throw new Exception(msg);
            }

            // save featuresets as shape files
            SaveShapeFiles(_featureSets);
            logger.Info("All shape files created and saved.");
        }
                
        /// <summary>
        /// Call EPAWebServiceHelper Method to get delineated watershed, and also return the start point.
        /// </summary>
        /// <param name="param">Arguments for backgroundworkers</param>
        /// <returns>Return a list of featureset including both point and polygon</returns>
        private IList<IFeatureSet> GetShapes(Coordinate watershedOutletLocation)
        {
            var projCor = watershedOutletLocation;
            
            // declare a new EPAWebServiceHelper Client
            var trigger = new EPAWebServiceHelper(projCor);

            // get Start Point Information
            object[] startpt = trigger.GetStartPoint();

            // check if start point successful
            if (startpt == null)
            {
                //progress.closeForm();
                return null;
            }
            
            // get delineated watershed
            object[] WshedObj = trigger.GetWsheds(startpt);

            if (WshedObj == null)
            {                               

                return null;
            }

            IFeatureSet fsWshed = new FeatureSet();

            // delete small marginal polygons if any
            try
            {
                var fsCatchment = (IFeatureSet)WshedObj[0];
                int count = fsCatchment.Features.Count;
                if (count > 1)
                {
                    // the last one is the main watershed
                    for (int i = 0; i < count - 1; i++)
                    {
                        fsCatchment.Features.RemoveAt(0);
                    }

                    // Object process could be dangerous to lose Projection info
                    WshedObj[0] = fsCatchment;
                }

                fsWshed = SetAttribute(WshedObj);
            }

            catch (Exception ex)
            {
                // As a bare minimum we should probably log these errors
                logger.Fatal(ex.Message);
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }

            // get Upstream flowlines
            var StreamObj = trigger.GetLines(startpt);
            var fsStream = SetAttribute(StreamObj);

            // create the start point shapefile
            var point = new Feature(projCor);
            IFeatureSet fsPoint = new FeatureSet(point.FeatureType);  
            
            // PK: added this following one line
            fsPoint.Projection = WGS84;

            fsPoint.AddFeature(point);

            IList<IFeatureSet> EPAShapes = new List<IFeatureSet>();
            EPAShapes.Add(fsWshed);
            EPAShapes.Add(fsStream);
            EPAShapes.Add(fsPoint);
            logger.Info("All shapes were generated.");
            return EPAShapes;
        }
                
        /// <summary>
        /// Created for setting attribute table for shapefiles.
        /// </summary>
        /// <param name="attri">object[] Attributes including necessary information</param>
        /// <returns>Returns the IFeatureSet with attribute table filled</returns>
        private IFeatureSet SetAttribute(object[] attri)
        {
            logger.Info("Setting attribute table fo shape files.");

            if (attri == null) return null;

            var Ifs = attri[0] as IFeatureSet;
            var fs = Ifs as FeatureSet;

            //Fill Streamlines' attribute table
            if (Ifs.FeatureType == FeatureType.Line)
            {
                var comid = attri[1] as List<string>;
                var reachcode = attri[2] as List<string>;
                var totdist = attri[3] as List<string>;

                var Id = new DataColumn("Id");
                var Comid = new DataColumn("Comid");
                var Reachcode = new DataColumn("ReachCode");
                var Totdist = new DataColumn("Length(km)");

                fs.DataTable.Columns.Add(Id);
                fs.DataTable.Columns.Add(Comid);
                fs.DataTable.Columns.Add(Reachcode);
                fs.DataTable.Columns.Add(Totdist);

                for (int i = 0; i < fs.Features.Count; i++)
                {
                    fs.Features[i].DataRow["Id"] = (i + 1);
                    fs.Features[i].DataRow["Comid"] = comid[i];
                    fs.Features[i].DataRow["ReachCode"] = reachcode[i];
                    fs.Features[i].DataRow["Length(km)"] = totdist[i];
                }
            }

            else
            {
                var wshedarea = attri[1] as string;

                var Area = new DataColumn("Area(sq_km)");
                var Id = new DataColumn("Id");

                fs.DataTable.Columns.Add(Id);
                fs.DataTable.Columns.Add(Area);

                if (fs.Features.Count == 1)
                {
                    fs.Features[0].DataRow["Id"] = 1;
                    fs.Features[0].DataRow["Area(sq_km)"] = wshedarea;
                }
                else
                {
                    int count = fs.Features.Count;
                    try
                    {
                        for (int i = 0; i < count - 1; i++)
                        {
                            fs.Features[i].DataRow.Delete();
                        }
                    }
                    catch (Exception ex)
                    {
                        var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        {
                            Content = new StringContent(ex.Message),
                            ReasonPhrase = "Error in creating attribute table for shape files."
                        };

                        logger.Fatal(ex.Message);
                        throw new HttpResponseException(resp);   
                    }

                    fs.Features[0].DataRow["Id"] = 1;
                    fs.Features[0].DataRow["Area(sq_km)"] = wshedarea;
                }
            }

            logger.Info("Atrribute table was created for shape file.");
            return fs;
        }

        private void SaveShapeFiles(IEnumerable<IFeatureSet> pointpolygon)
        {
            logger.Info("Saving shape files.");

            if (pointpolygon == null) return;
           
            if (Directory.Exists(_targetShapeFilesDirPath) == false)
            {
                Directory.CreateDirectory(_targetShapeFilesDirPath);
            }

            foreach (IFeatureSet fsset in pointpolygon)
            {                
                //PK:Since EPA service return data is based on WSG1984 datum
                //set the projection of the featureset to that projection before 
                //saving feature to a shape file. 
                //TODO: FeatureSet projection might have been set previously in other parts 
                //of this project which kind of not necessary to do
                fsset.Projection = WGS84;

                string fileSavePath = string.Empty;
                if (fsset.FeatureType == FeatureType.Point)
                {
                    try
                    {                        
                        //Save featureset as a MapPointLayer
                        fileSavePath = Path.Combine(_targetShapeFilesDirPath, _wshedpointFile);
                        fsset.SaveAs(fileSavePath, true);                        
                    }
                    catch (Exception ex)
                    {
                        var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        {
                            Content = new StringContent(ex.Message),
                            ReasonPhrase = "Failed to save the point shape file."
                        };

                        logger.Fatal(ex.Message);
                        throw new HttpResponseException(resp);
                    }
                }

                if (fsset.FeatureType == FeatureType.Line)
                {
                    try
                    {
                        // save featureset as a MapLineLayer
                        fileSavePath = Path.Combine(_targetShapeFilesDirPath, _streamFile);
                        fsset.SaveAs(fileSavePath, true);
                        
                    }
                    catch (Exception ex)
                    {
                        var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        {
                            Content = new StringContent(ex.Message),
                            ReasonPhrase = "Failed to save the streamline shape file."
                        };

                        logger.Fatal(ex.Message);
                        throw new HttpResponseException(resp);
                    }
                }

                if (fsset.FeatureType == FeatureType.Polygon)
                {
                    try
                    {
                        fileSavePath = Path.Combine(_targetShapeFilesDirPath, _wshedFile);
                        fsset.SaveAs(fileSavePath, true);                       
                    }
                    catch (Exception ex)
                    {
                        var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        {
                            Content = new StringContent(ex.Message),
                            ReasonPhrase = "Failed to save the the watershed shape file."
                        };

                        logger.Fatal(ex.Message);
                        throw new HttpResponseException(resp);
                    }
                }

                logger.Info("Shape files were saved.");
            }
        }
        
        # endregion
    }
}
