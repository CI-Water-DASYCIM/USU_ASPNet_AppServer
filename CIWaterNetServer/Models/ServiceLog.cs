using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UWRL.CIWaterNetServer.Models
{    
    public static class RunStatus
    {
        public static string InQueue = "In Queue";
        public static string Processing = "Processing";
        public static string Success = "Success";
        public static string Error = "Error";
    }

    public class ServiceLog
    {
        public int ID { get; set; }
        public int ServiceID { get; set; } // this will be the FK to Service entity
        public string JobID { get; set; }
        public DateTime? CallTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? FinishTime { get; set; }
        public string RunStatus { get; set; }        
        public string Error { get; set; }

        //navigation property
        public virtual Service Service { get; set; }
    }

   
}