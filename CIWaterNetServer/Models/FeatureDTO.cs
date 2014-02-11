using DotSpatial.Data;
using DotSpatial.Topology;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace UWRL.CIWaterNetServer.Models
{
    public class FeatureDTO
    {
        public IBasicGeometry BasicGeometry { get; set; }
        public int ContentLength { get; set; }
        public DataRow DataRow { get; set; }
        public CacheTypes EnvelopeSource { get; set; }
        public int Fid { get; set; }
        public int RecordNumber { get; set; }
        public ShapeRange ShapeIndex { get; set; }
        public ShapeType ShapeType { get; set; }
    }
}