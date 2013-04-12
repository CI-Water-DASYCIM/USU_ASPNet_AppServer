using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace UWRL.CIWaterNetServer.UEB
{
    public class UEBFileNamePathStrings
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static string _uebConfigFileName = "ueb.cfig";

        // TODO: finalize these settings
        // Need to creat a json document with these attributes and then load these properties from that json file.
        //public static readonly string WORKING_DIR_PATH = @"C:\CIWaterData\Working";
        //public static readonly string DAYMET_NETCDF_OUTPUT_ROOT_DIR_PATH = @"C:\CIWaterData\Working\Daymet";
        
        private static readonly string _DAYMET_NETCDF_OUTPUT_TEMP_SUB_DIR_PATH = @"Daymet\TEMP_OUT_NETCDF";
        private static readonly string _DAYMET_NETCDF_OUTPUT_PRECP_SUB_DIR_PATH = @"Daymet\PRECP_OUT_NETCDF";
        private static readonly string _DAYMET_NETCDF_OUTPUT_VP_SUB_DIR_PATH = @"Daymet\VP_OUT_NETCDF";
        private static readonly string _DAYMET_NETCDF_OUTPUT_WIND_SUB_DIR_PATH = @"Daymet\WIND_OUT_NETCDF";
        
        private static readonly string _SHAPE_FILES_SUB_DIR_PATH = "SHAPE_FILES";
        
        //public static readonly string DAYMET_SOURCE_ROOT_DIR_PATH = @"C:\CIWaterData\DataSources\Daymet";
        //public static readonly string DAYMET_SOURCE_TEMP_SUB_DIR_PATH = "TEMP";
        //public static readonly string DAYMET_SOURCE_PRECP_SUB_DIR_PATH = "PRECP";
        //public static readonly string DAYMET_SOURCE_VP_SUB_DIR_PATH = "VP";
        //public static readonly string NLCD_SOURCE_DIR_PATH = @"C:\CIWaterData\DataSources\NLCD";
        //public static readonly string DEM_SOURCE_DIR_PATH = @"C:\CIWaterData\DataSources\DEM";
        //public static readonly string DEM_SOURCE_FILE_NAME = "";
        
        private static readonly string _PACKAGE_SUB_DIR_PATH = "UEB_OUT_Package";
       
        public static readonly string WATERSHED_SHAPE_FILE_NAME = "Watershed.shp";
        public static readonly string WATERSHED_BUFERRED_SHAPE_FILE_NAME = "Buf_Watershed.shp";
        public static readonly string WATERSHED_RASTER_FILE_NAME = "Watershed.tif";
        public static readonly string WATERSHED_NETCDF_FILE_NAME = "Watershed.nc";
        public static readonly string WATERSHED_DEM_RASTER_FILE_NAME = "ws_dem.tif";
        
        //private static readonly string _PYTHON_SCRIPT_DIR_PATH = @"C:\CIWaterPythonScripts";
        
        private static Dictionary<string, string> _configSettings = new Dictionary<string, string>();

        // private constructor to prevent anyone outside this class be able to create an instance of this class
        private UEBFileNamePathStrings()
        {
            // populate the key values in the dictionary collection
            _configSettings.Add("working.dir.path", string.Empty);                        
            _configSettings.Add("daymet.resource.temp.dir.path", string.Empty);
            _configSettings.Add("daymet.resource.precp.dir.path", string.Empty);
            _configSettings.Add("daymet.resource.vp.dir.path", string.Empty);
            _configSettings.Add("nlcd.resource.dir.path", string.Empty);
            _configSettings.Add("nlcd.resource.file.name", string.Empty);
            _configSettings.Add("dem.resource.file.name", string.Empty);
            _configSettings.Add("dem.resource.dir.path", string.Empty);
            _configSettings.Add("python.script.dir.path", string.Empty);

            // read the config file here an populate all the properties of this class
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

                    // TODO: check if this is comment line which if the first character is a # character
                    // if so then no need to do any processing on this line


                    //split the line using delimeter =
                    string[] lineKeyValueSplit = null;
                    lineKeyValueSplit = line.Split(delimeter, 2);
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

        private static UEBFileNamePathStrings _instance = new UEBFileNamePathStrings();

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

        public static string NLCD_RESOURCE_DIR_PATH
        {
            get { return _configSettings["nlcd.resource.dir.path"]; }
        }

        public static string NLCD_RESOURCE_FILE_NAME
        {
            get { return _configSettings["nlcd.resource.file.name"]; }
        }

        public static string DAYMET_NETCDF_OUTPUT_TEMP_DIR_PATH
        {
            get { return Path.Combine(WORKING_DIR_PATH, _DAYMET_NETCDF_OUTPUT_TEMP_SUB_DIR_PATH);}
        }

        public static string DAYMET_NETCDF_OUTPUT_PRECP_DIR_PATH
        {
            get { return Path.Combine(WORKING_DIR_PATH, _DAYMET_NETCDF_OUTPUT_PRECP_SUB_DIR_PATH);}
        }

        public static string DAYMET_NETCDF_OUTPUT_VP_DIR_PATH
        {
            get { return Path.Combine(WORKING_DIR_PATH, _DAYMET_NETCDF_OUTPUT_VP_SUB_DIR_PATH);}
        }

        public static string DAYMET_NETCDF_OUTPUT_WIND_DIR_PATH
        {
            get { return Path.Combine(WORKING_DIR_PATH, _DAYMET_NETCDF_OUTPUT_WIND_SUB_DIR_PATH);}
        }

        public static string SHAPE_FILES_DIR_PATH
        {
            get { return Path.Combine(WORKING_DIR_PATH, _SHAPE_FILES_SUB_DIR_PATH); }
        }

        public static string UEB_PACKAGE_DIR_PATH
        {
            get { return Path.Combine(WORKING_DIR_PATH, _PACKAGE_SUB_DIR_PATH); }
        }
    }
}