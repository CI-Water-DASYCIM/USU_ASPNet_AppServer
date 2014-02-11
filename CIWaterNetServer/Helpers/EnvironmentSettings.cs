using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UWRL.CIWaterNetServer.Helpers
{
    public static class EnvironmentSettings
    {
        // variable to control runing this code in localhost or remote host mode
        public static bool IsLocalHost = true;
        public static string PythonExecutableFile = @"C:\Python27\ArcGIS10.1\Python.exe";
    }
}