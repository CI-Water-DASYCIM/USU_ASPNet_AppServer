using DotSpatial.Data;
using DotSpatial.Topology;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace UWRL.CIWaterNetServer.Models
{
    public class FeatureSetDTO
    {
        public bool AttributesPopulated { get; set; }
        public CoordinateType CoordinateType { get; set; }
        public DataTable DataTable { get; set; }
        public IGeometryFactory FeatureGeometryFactory { get; set; }
        public Dictionary<DataRow, FeatureDTO> FeatureLookup { get; set; }
        //IFeatureList Features { get; set; }
        public FeatureType FeatureType { get; set; }
        public string Filename { get; set; }
        public bool IndexMode { get; set; }
        public double[] M { get; set; }
        public List<ShapeRange> ShapeIndices { get; set; }
        public double[] Vertex { get; set; }
        public bool VerticesAreValid { get; set; }
        public double[] Z { get; set; }
    }
}