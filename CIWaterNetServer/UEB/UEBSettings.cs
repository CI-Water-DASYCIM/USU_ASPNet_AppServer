using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace UWRL.CIWaterNetServer.UEB
{
    public class UEBSettings
    {
        #region public fields
        public static readonly string WATERSHED_SHAPE_ZIP_FILE_NAME = "shapefiles.zip";
        public static readonly string WATERSHED_SHAPE_FILE_NAME = "Watershed.shp";
        public static readonly string WATERSHED_POINT_SHAPE_FILE_NAME = "Watershedpoint.shp";
        public static readonly string WATERSHED_STREAM_SHAPE_FILE_NAME = "Stream.shp";
        public static readonly string WATERSHED_BUFERRED_SHAPE_FILE_NAME = "Watershed_buffered.shp";
        public static readonly string WATERSHED_BUFERRED_RASTER_FILE_NAME = "Watershed_buffered.tif";
        public static readonly string WATERSHED_NETCDF_FILE_NAME = "Watershed.nc";
        public static readonly string WATERSHED_DEM_RASTER_FILE_NAME = "ws_dem.tif";
        public static readonly string WATERSHED_SINGLE_TEMP_MIN_NETCDF_FILE_NAME = "tmin_daily_one_data.nc";
        public static readonly string WATERSHED_SINGLE_TEMP_MAX_NETCDF_FILE_NAME = "tmax_daily_one_data.nc";
        public static readonly string WATERSHED_SINGLE_PRECP_NETCDF_FILE_NAME = "precp_daily_one_data.nc";
        public static readonly string WATERSHED_SINGLE_VP_NETCDF_FILE_NAME = "vp_daily_one_data.nc";
        public static readonly string WATERSHED_MULTIPLE_TEMP_NETCDF_FILE_NAME = "ta_daily_multiple_data.nc";
        public static readonly string WATERSHED_MULTIPLE_PRECP_NETCDF_FILE_NAME = "precp_daily_multiple_data.nc";
        public static readonly string WATERSHED_MULTIPLE_VP_NETCDF_FILE_NAME = "vp_daily_multiple_data.nc";
        public static readonly string WATERSHED_MULTIPLE_RH_NETCDF_FILE_NAME = "rh_daily_multiple_data.nc";
        public static readonly string WATERSHED_MULTIPLE_WIND_NETCDF_FILE_NAME = "wind_daily_multiple_data.nc";
        public static readonly string WATERSHED_SLOPE_NETCDF_FILE_NAME = "slope.nc";
        public static readonly string WATERSHED_ASPECT_NETCDF_FILE_NAME = "aspect.nc";
        public static readonly string WATERSHED_NLCD_RASTER_FILE_NAME = "ws_nlcd.img";
        public static readonly string WATERSHED_NLCD_CC_NETCDF_FILE_NAME = "cc_nlcd.nc";
        public static readonly string WATERSHED_NLCD_HC_NETCDF_FILE_NAME = "hc_nlcd.nc";
        public static readonly string WATERSHED_NLCD_LAI_NETCDF_FILE_NAME = "lai_nlcd.nc";
        public static readonly string WATERSHED_NLCD_YCAGE_NETCDF_FILE_NAME = "ycage_nlcd.nc";
        public static readonly string WATERSHED_LATITUDE_FILE_NAME_WITHOUT_EXTENSION = "lat";
        public static readonly string WATERSHED_LONGITIDUE_FILE_NAME_WITHOUT_EXTENSION = "lon";
        public static readonly string WATERSHED_SINGLE_TEMP_MIN_NETCDF_VARIABLE_NAME = "tmin";
        public static readonly string WATERSHED_SINGLE_TEMP_MAX_NETCDF_VARIABLE_NAME = "tmax";
        public static readonly string WATERSHED_MULTIPLE_TEMP_NETCDF_VARIABLE_NAME = "T";
        public static readonly string WATERSHED_MULTIPLE_RH_NETCDF_VARIABLE_NAME = "rh";
        public static readonly string UEB_PACKAGE_FILE_NAME = "UEBPackage.zip";
        public static readonly string WATERSHED_ATMOSPHERIC_PRESSURE_FILE_NAME = "ws_atom_pres.txt";
        public static readonly string PACKAGE_BUILD_STATUS_FILE_NAME = "package_build_status.txt";
        public static readonly string PACKAGE_OUTPUT_SUB_DIR_PATH = "UEBPackage";
        public static readonly string PACKAGE_FILES_OUTPUT_SUB_DIR_PATH = "UEBPackageFiles";
        public static readonly string PACKAGE_BUILD_REQUEST_SUB_DIR_PATH = "UEBPackageRequest";
        public static readonly string PACKAGE_BUILD_REQUEST_ZIP_FILE_NAME = "uebPkgRequest.zip";
        public static readonly string DAYMET_NETCDF_OUTPUT_TEMP_SUB_DIR_PATH = @"Daymet\TEMP_OUT_NETCDF";
        public static readonly string DAYMET_RASTER_OUTPUT_TEMP_SUB_DIR_PATH = @"Daymet\TEMP_OUT_RASTER";
        public static readonly string DAYMET_NETCDF_OUTPUT_VP_SUB_DIR_PATH = @"Daymet\VP_OUT_NETCDF";
        public static readonly string DAYMET_RASTER_OUTPUT_VP_SUB_DIR_PATH = @"Daymet\VP_OUT_RASTER";
        public static readonly string DAYMET_NETCDF_OUTPUT_RH_SUB_DIR_PATH = @"Daymet\RH_OUT_NETCDF";
        public static readonly string DAYMET_NETCDF_OUTPUT_PRECP_SUB_DIR_PATH = @"Daymet\PRECP_OUT_NETCDF";
        public static readonly string DAYMET_RASTER_OUTPUT_PRECP_SUB_DIR_PATH = @"Daymet\PRECP_OUT_RASTER";
        public static readonly string DAYMET_NETCDF_OUTPUT_WIND_SUB_DIR_PATH = @"Daymet\WIND_OUT_NETCDF";
        #endregion public fields

        #region private fields
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static string _uebConfigFileName = "ueb.cfig";        
        private static readonly string _SHAPE_FILES_SUB_DIR_PATH = "SHAPE_FILES";        
        private static Dictionary<string, string> _configSettings = new Dictionary<string, string>();
        private static UEBSettings _instance = new UEBSettings();
        #endregion private fields

        #region private constructor        
        // private constructor to prevent anyone outside this class be able to create an instance of this class
        private UEBSettings()
        {
            // populate the key values in the dictionary collection
            _configSettings.Add("working.dir.path", string.Empty);                        
            _configSettings.Add("daymet.resource.temp.dir.path", string.Empty);
            _configSettings.Add("daymet.resource.precp.dir.path", string.Empty);
            _configSettings.Add("daymet.resource.vp.dir.path", string.Empty);
            _configSettings.Add("daymet.resource.temp.min.file.name.pattern", string.Empty);
            _configSettings.Add("daymet.resource.temp.max.file.name.pattern", string.Empty);
            _configSettings.Add("daymet.resource.precp.file.name.pattern", string.Empty);
            _configSettings.Add("daymet.resource.vp.file.name.pattern", string.Empty);
            _configSettings.Add("nlcd.resource.dir.path", string.Empty);
            _configSettings.Add("nlcd.resource.file.name", string.Empty);
            _configSettings.Add("dem.resource.file.name", string.Empty);
            _configSettings.Add("dem.resource.dir.path", string.Empty);
            _configSettings.Add("python.script.dir.path", string.Empty);
            _configSettings.Add("watershed.constant.wind.speed", string.Empty);
            _configSettings.Add("model.output.folder.name", string.Empty);

            // read the config file here and populate all the properties of this class
            string customConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomConfig");
            //string currentPath = Directory.GetCurrentDirectory();
            string uebConfigFile = Path.Combine(customConfigPath, _uebConfigFileName);

            if (File.Exists(uebConfigFile) == false)
            {
                string errMsg = string.Format("{0} file was not found in the folder - {1}.", _uebConfigFileName, customConfigPath);
                _logger.Error(errMsg);
                throw new Exception(errMsg);
            }

            using (StreamReader sw = new StreamReader(uebConfigFile))
            {
                string line;
                string delimeterStr = "=";
                char [] delimeter = delimeterStr.ToCharArray();

                while ((line = sw.ReadLine()) != null)
                {
                    line = line.Trim();

                    // check if the line is a blank line and if so then skip this line
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    
                    // check if this is comment line which if the first character is a # character
                    // if so then no need to do any processing on this line
                    if (line.Substring(0, 1) == "#")
                    {
                        continue;
                    }

                    //split the line using delimeter '='
                    string[] lineKeyValueSplit = null;
                    lineKeyValueSplit = line.Split(delimeter, 2);

                    // if the character '=' is missing from the line then skip it
                    if (lineKeyValueSplit.Length != 2)
                    {
                        continue;
                    }
                    string configKey = lineKeyValueSplit[0].Trim();
                    string configValue = lineKeyValueSplit[1].Trim();

                    if (_configSettings.ContainsKey(configKey) && string.IsNullOrEmpty(configValue) == false)
                    {
                        _configSettings[configKey] = configValue;
                    }
                }
            }

            // check that the configSettings dictionary has a non-empty value for each of the keys
            if (_configSettings.Values.ToList().Any(value => value == string.Empty))
            {  
               var keysWithNoValues  =  _configSettings.ToList().FindAll(kvp => kvp.Value == string.Empty);
               foreach (KeyValuePair<string, string> kvp in keysWithNoValues)
               {
                   string errMsg = string.Format("No setting was found for UEB configuration parameter '{0}'.", kvp.Key);
                   _logger.Error(errMsg);
               }
               
                throw new  Exception(string.Format("Invalid config settings were found in file '{0}'.", _uebConfigFileName));
            }
                        
        }

        #endregion private constructor

        #region public properties

        public static string WORKING_DIR_PATH
        {
            get { return _configSettings["working.dir.path"]; }
        }

        public static string DEM_RESOURCE_DIR_PATH
        {
            get { return _configSettings["dem.resource.dir.path"]; }
        }

        public static string DEM_RESOURCE_FILE_NAME
        {
            get { return _configSettings["dem.resource.file.name"]; }
        }

        public static string PYTHON_SCRIPT_DIR_PATH
        {
            get { return _configSettings["python.script.dir.path"]; }
        }

        public static string DAYMET_RESOURCE_TEMP_DIR_PATH
        {
            get { return _configSettings["daymet.resource.temp.dir.path"]; }
        }

        public static string DAYMET_RESOURCE_PRECP_DIR_PATH
        {
            get { return _configSettings["daymet.resource.precp.dir.path"]; }
        }

        public static string DAYMET_RESOURCE_VP_DIR_PATH
        {
            get { return _configSettings["daymet.resource.vp.dir.path"]; }
        }

        public static string DAYMET_RESOURCE_TEMP_MIN_FILE_NAME_PATTERN
        {
            get { return _configSettings["daymet.resource.temp.min.file.name.pattern"]; }
        }

        public static string DAYMET_RESOURCE_TEMP_MAX_FILE_NAME_PATTERN
        {
            get { return _configSettings["daymet.resource.temp.max.file.name.pattern"]; }
        }

        public static string DAYMET_RESOURCE_PRECP_FILE_NAME_PATTERN
        {
            get { return _configSettings["daymet.resource.precp.file.name.pattern"]; }
        }

        public static string DAYMET_RESOURCE_VP_FILE_NAME_PATTERN
        {
            get { return _configSettings["daymet.resource.vp.file.name.pattern"]; }
        }
                
        public static string NLCD_RESOURCE_DIR_PATH
        {
            get { return _configSettings["nlcd.resource.dir.path"]; }
        }

        public static string NLCD_RESOURCE_FILE_NAME
        {
            get { return _configSettings["nlcd.resource.file.name"]; }
        }

        public static float WATERSHED_CONSTANT_WIND_SPEED
        {
            get 
            {
                float windSpeed;
                bool isSuccess = float.TryParse(_configSettings["watershed.constant.wind.speed"], out windSpeed);
                if(isSuccess)
                {
                    if (windSpeed < 0)
                    {
                        windSpeed = Math.Abs(windSpeed);
                    }
                    return windSpeed;
                }

                // set a default speed of 2 m/sec
                windSpeed = 2.0f;
                return windSpeed;
            }
        }

        public static string MODEL_OUTPUT_FOLDER_NAME
        {
            get { return _configSettings["model.output.folder.name"]; }
        }

        //public static string DAYMET_NETCDF_OUTPUT_TEMP_DIR_PATH
        //{
        //    get { return Path.Combine(WORKING_DIR_PATH, _DAYMET_NETCDF_OUTPUT_TEMP_SUB_DIR_PATH);}
        //}

        //public static string DAYMET_NETCDF_OUTPUT_PRECP_DIR_PATH
        //{
        //    get { return Path.Combine(WORKING_DIR_PATH, _DAYMET_NETCDF_OUTPUT_PRECP_SUB_DIR_PATH);}
        //}

        //public static string DAYMET_NETCDF_OUTPUT_VP_DIR_PATH
        //{
        //    get { return Path.Combine(WORKING_DIR_PATH, _DAYMET_NETCDF_OUTPUT_VP_SUB_DIR_PATH);}
        //}

        //public static string DAYMET_NETCDF_OUTPUT_RH_DIR_PATH
        //{
        //    get { return Path.Combine(WORKING_DIR_PATH, _DAYMET_NETCDF_OUTPUT_RH_SUB_DIR_PATH); }
        //}
        //public static string DAYMET_NETCDF_OUTPUT_WIND_DIR_PATH
        //{
        //    get { return Path.Combine(WORKING_DIR_PATH, _DAYMET_NETCDF_OUTPUT_WIND_SUB_DIR_PATH);}
        //}


        //public static string DAYMET_RASTER_OUTPUT_TEMP_DIR_PATH
        //{
        //    get { return Path.Combine(WORKING_DIR_PATH, _DAYMET_RASTER_OUTPUT_TEMP_SUB_DIR_PATH); }
        //}

        //public static string DAYMET_RASTER_OUTPUT_PRECP_DIR_PATH
        //{
        //    get { return Path.Combine(WORKING_DIR_PATH, _DAYMET_RASTER_OUTPUT_PRECP_SUB_DIR_PATH); }
        //}

        //public static string DAYMET_RASTER_OUTPUT_VP_DIR_PATH
        //{
        //    get { return Path.Combine(WORKING_DIR_PATH, _DAYMET_RASTER_OUTPUT_VP_SUB_DIR_PATH); }
        //}

        //public static string DAYMET_RASTER_OUTPUT_WIND_DIR_PATH
        //{
        //    get { return Path.Combine(WORKING_DIR_PATH, _DAYMET_RASTER_OUTPUT_WIND_SUB_DIR_PATH); }
        //}
        
        public static string SHAPE_FILES_DIR_PATH
        {
            get { return Path.Combine(WORKING_DIR_PATH, _SHAPE_FILES_SUB_DIR_PATH); }
        }

        //public static string UEB_PACKAGE_DIR_PATH
        //{
        //    get { return Path.Combine(WORKING_DIR_PATH, _PACKAGE_SUB_DIR_PATH); }
        //}

        #endregion public properties
    }
}