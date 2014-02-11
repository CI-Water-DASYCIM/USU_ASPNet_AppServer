using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UWRL.CIWaterNetServer.Models
{
    public class Service
    {
        public int ServiceID { get; set; }
        public string APIName { get; set; }
        public bool IsAllowConcurrentRun { get; set; }        
    }
}