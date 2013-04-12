using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UWRL.CIWaterNetServer.Models
{
    public class ShapeDataSet
    {
        public int ShapeSequenceNumber { get; set; }
        public List<ShapeData> LatlonValues { get; set; }
    }
}