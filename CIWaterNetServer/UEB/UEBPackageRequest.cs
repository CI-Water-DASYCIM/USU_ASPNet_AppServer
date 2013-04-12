using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UWRL.CIWaterNetServer.UEB
{
    // to specify json string request from the client to create a UEB package
    // the client request string will be converted to an object of this class
    internal class UEBPackageRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public byte TimeStep { get; set; }
        public string PackageName { get; set; }

    }
}