using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace UWRL.CIWaterNetServer.Python
{
    public static class PythonHelper
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static void ExecuteScript(string pyScriptFile, List<string> args)
        {
            string arguments = pyScriptFile + " ";
            foreach (string arg in args)
            {
                arguments += arg + " ";
            }

            arguments.TrimEnd();

            var pythonProc = new Process();
            pythonProc.StartInfo.FileName = @"C:\Python27\ArcGIS10.1\Python.exe";
            pythonProc.StartInfo.Arguments = arguments;
            pythonProc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            pythonProc.StartInfo.CreateNoWindow = false;
            pythonProc.StartInfo.UseShellExecute = false;
            pythonProc.StartInfo.RedirectStandardInput = true;
            pythonProc.StartInfo.RedirectStandardOutput = true;
            pythonProc.StartInfo.RedirectStandardError = true;
            
            logger.Info("Starting python.");                        
            pythonProc.Start();
            pythonProc.WaitForExit();
            
            string errors = pythonProc.StandardError.ReadToEnd();
            string result = pythonProc.StandardOutput.ReadToEnd();
            pythonProc.Close();

            if (result.Contains("Exception"))
            {
                logger.Fatal(result);
                throw new Exception(result);
            }

            if (result.Contains("ERRORS"))
            {
                logger.Fatal(result);
                throw new Exception(result);
            }

            if (string.IsNullOrEmpty(errors) == false)
            {
                logger.Fatal(errors);
                throw new Exception(errors);
            }
        }
        // Executes a shell command synchronously.
        // Example of command parameter value is
        // "python " + @"C:\scripts\geom_input.py".
        //
        public static void ExecuteCommand(object command)
        {
            try
            {
                // Create the ProcessStartInfo using "cmd" as the program to be run,
                // and "/c " as the parameters.
                // "/c" tells cmd that you want it to execute the command that follows,
                // then exit.
                System.Diagnostics.ProcessStartInfo procStartInfo = new
                    System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);

                // The following commands are needed to redirect the standard output.
                // This means that it will be redirected to the Process.StandardOutput StreamReader.
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.RedirectStandardError = true;
                procStartInfo.UseShellExecute = false;

                // Do not create the black window.
                procStartInfo.CreateNoWindow = false;

                // Now you create a process, assign its ProcessStartInfo, and start it.
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                
                // Get the output into a string.
                string result = proc.StandardOutput.ReadToEnd();
                string errors = proc.StandardError.ReadToEnd();

                if (result.Contains("Exception"))
                {
                    logger.Fatal(result);
                    throw new Exception(result);
                }

                if (result.Contains("ERRORS"))
                {
                    logger.Fatal(result);
                    throw new Exception(result);
                }

                if (string.IsNullOrEmpty(errors) == false)
                {
                    logger.Fatal(errors);
                    throw new Exception(errors);
                }
                // Display the command output.
                Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                logger.Fatal(ex.Message);
                throw new Exception(ex.Message);                
            }
        }
    }

}