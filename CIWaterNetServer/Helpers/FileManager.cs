using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace UWRL.CIWaterNetServer.Helpers
{
    public static class FileManager
    {
        public static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return false;
        }
    }
}