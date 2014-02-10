using System;
using System.Net.Http;
using NLog;
using System.Configuration;

namespace JobRunner
{
    class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            //HttpClient client = new HttpClient();

            try
            {
                RunJobs();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
            finally
            {
                // this is needed for some reason for this program to be run on a scheduler
                System.Threading.Thread.Sleep(1000);
            }
        }
        
        private static async void RunJobs()
        {
            string appServerURL = string.Empty;
            string runJobWebAPI = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                //appServerURL = "http://localhost:20040"; // for running on local server for testing
                //client.BaseAddress = new Uri(appServerURL);

                // read the url of the app server from configuration file
                appServerURL = System.Configuration.ConfigurationManager.AppSettings["AppServerURL"]; // for running on production server
                client.BaseAddress = new Uri(appServerURL); 
                logger.Info("App server scheduled jobs started:{0}", DateTime.Now);

                runJobWebAPI = System.Configuration.ConfigurationManager.AppSettings["RunJobWebAPI"];
                var response = await client.GetAsync(runJobWebAPI);
                
                // Check that response was successful or throw exception 
                response.EnsureSuccessStatusCode(); 

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger.Error("All App server scheduled jobs started successfully:{0}", DateTime.Now);
                    Console.WriteLine("All App server scheduled jobs started successfully:{0}", DateTime.Now);
                }
                else
                {
                    logger.Info("Not all App server scheduled jobs started successfully:{0}", DateTime.Now);
                    Console.WriteLine("Not all App server scheduled jobs started successfully:{0}", DateTime.Now);
                }
            }
        }        
    }
}
