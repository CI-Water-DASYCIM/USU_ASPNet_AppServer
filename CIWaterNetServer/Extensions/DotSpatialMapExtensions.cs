using DotSpatial.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web;
using UWRL.CIWaterNetServer.Properties;

namespace UWRL.CIWaterNetServer.Extensions
{
    public static class DotSpatialMapExtensions
    {
        /// <summary>
        /// Get Data Sites Layer for given map
        /// </summary>
        /// <param name="map">Map</param>
        /// <param name="createIfNotExists">Create data sites layer, if it not exists.</param>
        /// <returns>Data Sites Layer.</returns>
        public static IMapGroup GetDataSitesLayer(this IMap map, bool createIfNotExists = false)
        {
            if (map == null) throw new ArgumentNullException("map");
            Contract.EndContractBlock();

            var layerName = Resources.SearchGroupName;
            var layer = FindGroupLayerByName(map, layerName);
            if (layer == null && createIfNotExists)
            {
                layer = new MapGroup(map, layerName);
            }
            return layer;
        }

        private static IMapGroup FindGroupLayerByName(IMap map, string layerName)
        {
            return map.Layers
                .OfType<IMapGroup>()
                .FirstOrDefault(group => group.LegendText.ToLower() == layerName.ToLower());
        }
    }
}