using DotSpatial.Data;
using Newtonsoft.Json;
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
using UWRL.CIWaterNetServer.Models;

namespace UWRL.CIWaterNetServer.Controllers
{
    // This is not needed for creating UEB package. This service allows
    // client to get the lat lon values for a given shape file to display the shape file
    // on a map
    public class ShapeLatLonValuesController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private string _inputWatershedShapeFilePath = string.Empty; 

        public HttpResponseMessage GetShapeLatLonValues(string shapeFileName)
        {
            HttpResponseMessage response = new HttpResponseMessage();

            if (EnvironmentSettings.IsLocalHost)
            {                
                _inputWatershedShapeFilePath = @"E:\CIWaterData\Temp";                
            }
            else
            {                
                _inputWatershedShapeFilePath = @"C:\CIWaterData\Temp";                
            }

            if(string.IsNullOrEmpty(shapeFileName))
            {               
                string errMsg = "No shape file name was provided";
                logger.Error(errMsg);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
            }

            string shapeFileExt = Path.GetExtension(Path.Combine(_inputWatershedShapeFilePath, shapeFileName));
            if (shapeFileExt == string.Empty)
            {
                shapeFileName += ".shp";
            }
            else if (shapeFileExt != ".shp")
            {
                shapeFileName.Replace(shapeFileExt, ".shp");
            }
            
            //check file exists
            if (!File.Exists(Path.Combine(_inputWatershedShapeFilePath, shapeFileName)))
            {                
                string errMsg = shapeFileName + " was not found.";
                logger.Error(errMsg);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, errMsg);
            }

            FeatureSet fs;

            try
            {
                fs = FeatureSet.Open(Path.Combine(_inputWatershedShapeFilePath, shapeFileName)) as FeatureSet;
            }
            catch (Exception ex)
            {
                logger.Fatal(ex.Message);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message);
            }            
            
            //fill the attributes table
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
    }
}
