using DotSpatial.Data;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using UWRL.CIWaterNetServer.Helpers;
using UWRL.CIWaterNetServer.Models;

namespace UWRL.CIWaterNetServer.Controllers
{
    // This is not needed for creating UEB package. This service allows
    // client to get the lat lon values for a given shape file to display the shape file
    // on a map
    public class ShapeLatLonValuesController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
                
        // NOTE: Client need to use the post method and no this GET method
        // The ciwater hubzero based portal will not be able to show watershed
        // since it using the following web service (the GET version) which tries to find the shapefile
        // in a specfic fixed foldername where the delination service used to store the
        // files. Now the delineation service code has been changed to dynamically create
        // a folder based on guid for generated shape file storage.
        public HttpResponseMessage GetShapeLatLonValues(string shapeFileName)
        {
            HttpResponseMessage response = new HttpResponseMessage();
                        
            string inputWatershedShapeFilePath = UEB.UEBSettings.WORKING_DIR_PATH;

            if(string.IsNullOrEmpty(shapeFileName))
            {               
                string errMsg = "No shape file name was provided";
                logger.Error(errMsg);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
            }

            string shapeFileExt = Path.GetExtension(Path.Combine(inputWatershedShapeFilePath, shapeFileName));
            if (shapeFileExt == string.Empty)
            {
                shapeFileName += ".shp";
            }
            else if (shapeFileExt != ".shp")
            {
                shapeFileName.Replace(shapeFileExt, ".shp");
            }
            
            // check file exists
            if (!File.Exists(Path.Combine(inputWatershedShapeFilePath, shapeFileName)))
            {                
                string errMsg = shapeFileName + " was not found.";
                logger.Error(errMsg);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
            }

            FeatureSet fs;

            try
            {
                fs = FeatureSet.Open(Path.Combine(inputWatershedShapeFilePath, shapeFileName)) as FeatureSet;
            }
            catch (Exception ex)
            {
                logger.Fatal(ex.Message);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message);
            }            
            
            // fill the attributes table
            fs.FillAttributes();
            string result = string.Empty;           
            List<ShapeDataSet> ShapeDataSetList = new List<ShapeDataSet>();
            int seqNo = 1;

            foreach (IFeature f in fs.Features)
            {
                Shape shp = f.ToShape();
                ShapeDataSet sds = new ShapeDataSet();
                sds.ShapeSequenceNumber = seqNo;
                seqNo++;
                List<ShapeData> shapeDataList = new List<ShapeData>();

                for (int i = 0; i < shp.Vertices.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        ShapeData sd = new ShapeData { Lat = shp.Vertices[i+1], Lon = shp.Vertices[i] };
                        shapeDataList.Add(sd);
                    }
                }
                sds.LatlonValues = shapeDataList;
                ShapeDataSetList.Add(sds);
            }

            string jsonString = JsonConvert.SerializeObject(ShapeDataSetList, Formatting.Indented);
            response.Content = new StringContent(jsonString);
            response.StatusCode = HttpStatusCode.OK;            
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            
            //Ref: http://weblogs.asp.net/cibrax/archive/2011/04/25/implementing-caching-in-your-wcf-web-apis.aspx
            //set the browser to cache this response for 10 secs only
            response.Content.Headers.Expires = DateTime.Now.AddSeconds(10);

            return response;
        }

        /// <summary>
        /// The caller (client) needs to send a zip file containing all shape related files. All files
        /// need to have the same file name with different extensions
        /// </summary>
        /// <returns></returns>
        public HttpResponseMessage PostShapeLatLonValues()
        {
            HttpResponseMessage response = new HttpResponseMessage();            
            Stream shapeFileToProcess = null;

            var t_stream = this.Request.Content.ReadAsStreamAsync().ContinueWith(s =>
            {
                shapeFileToProcess = s.Result;
            });
            t_stream.Wait();

            if (shapeFileToProcess == null)
            {
                string errMsg = "No shape file was provided in the request.";
                logger.Error(errMsg);
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, errMsg);
            }

            // generate a guid to pass on to the client as a job ID
            string folderBasedOnGuid = Guid.NewGuid().ToString();

            string inputShapeFilePath = Path.Combine(UEB.UEBSettings.WORKING_DIR_PATH, folderBasedOnGuid);
            string inputShapeFile = Path.Combine(inputShapeFilePath, "shapefile.zip");
            string unzipDirPath = Path.Combine(inputShapeFilePath, "ShapeFiles");
            string fileWithSHPExtension = string.Empty;

            try
            {
                // create temporary directory if it does not already exist
                if (!Directory.Exists(inputShapeFilePath))
                {
                    Directory.CreateDirectory(inputShapeFilePath);
                }

                if (File.Exists(inputShapeFile))
                {
                    File.Delete(inputShapeFile);
                }
                if (Directory.Exists(unzipDirPath))
                {
                    Directory.Delete(unzipDirPath);
                }

                Directory.CreateDirectory(unzipDirPath);

                using (var fileStream = File.Create(inputShapeFile))
                {
                    shapeFileToProcess.CopyTo(fileStream);
                }

                // unzip the zipped shape file
                ZipFile.ExtractToDirectory(inputShapeFile, unzipDirPath);

                // get the name of the file with .shp extension
                string[] filesWithSHPExtension = Directory.GetFiles(unzipDirPath, "*.shp");
                if (filesWithSHPExtension.Length != 1)
                {
                    string errMsg = "Either no file with .shp extension was provided as part of the zip file or there are multiple files with .shp extension.";
                    logger.Error(errMsg);
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Content = new StringContent(errMsg);
                    return response;
                }

                fileWithSHPExtension = filesWithSHPExtension[0];
            }
            catch (Exception ex)
            {
                logger.Fatal(ex.Message);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
                       
            FeatureSet fs;

            try
            {
                fs = FeatureSet.Open(fileWithSHPExtension) as FeatureSet;
            }
            catch (Exception ex)
            {
                logger.Fatal(ex.Message);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message);
            }

            // fill the attributes table
            fs.FillAttributes();
            string result = string.Empty;
            List<ShapeDataSet> ShapeDataSetList = new List<ShapeDataSet>();
            int seqNo = 1;

            foreach (IFeature f in fs.Features)
            {
                Shape shp = f.ToShape();
                ShapeDataSet sds = new ShapeDataSet();
                sds.ShapeSequenceNumber = seqNo;
                seqNo++;
                List<ShapeData> shapeDataList = new List<ShapeData>();

                for (int i = 0; i < shp.Vertices.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        ShapeData sd = new ShapeData { Lat = shp.Vertices[i + 1], Lon = shp.Vertices[i] };
                        shapeDataList.Add(sd);
                    }
                }
                sds.LatlonValues = shapeDataList;
                ShapeDataSetList.Add(sds);
            }

            
            string jsonString = JsonConvert.SerializeObject(ShapeDataSetList, Formatting.Indented);
            response.Content = new StringContent(jsonString);
            response.StatusCode = HttpStatusCode.OK;
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            //Ref: http://weblogs.asp.net/cibrax/archive/2011/04/25/implementing-caching-in-your-wcf-web-apis.aspx
            //set the browser to cache this response for 10 secs only
            response.Content.Headers.Expires = DateTime.Now.AddSeconds(10);

            // clean up the temporary folders for shape files
            DirectoryInfo dir = new DirectoryInfo(inputShapeFilePath);
            dir.Delete(true);
            logger.Info(string.Format("Lat/lon values in json format were successfully created for shape file:{0}", fileWithSHPExtension));            
            return response;
        }
    }
}
