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
        public int BufferSize { get; set; }
        public int GridCellSize { get; set; }
        public string DomainFileName { get; set; }
        public string DomainGridFileFormat { get; set; }
        public string ModelParametersFileName { get; set; }
        public SiteInitialConditions SiteInitialConditions { get; set; }
        public BristowCambellBValues BristowCambellBValues { get; set; }
        public TimeSeriesInputs TimeSeriesInputs { get; set; }        
        public string OutputControlFileName { get; set; }
        public string AggregatedOutputControlFileName { get; set; }
        public string OutputFolderName { get; set; }        
        public string PackageName { get; set; }

    }

    // TODO: not in use anymore as now these inputs will come from the client in a file
    internal class ModelParameters
    {
        public int irad { get; set; }
        public int ireadalb { get; set; }
        public float tr { get; set; }
        public float ts { get; set; }
        public float ems { get; set; }
        public float cg { get; set; }
        public float z { get; set; }
        public float zo { get; set; }
        public float rho { get; set; }
        public float rhog { get; set; }
        public float lc { get; set; }
        public float ks { get; set; }
        public float de { get; set; }
        public float avo { get; set; }
        public float anirO { get; set; }
        public float lans { get; set; }
        public float lang { get; set; }
        public float wlf { get; set; }
        public float rdl { get; set; }
        public float dnews { get; set; }
        public float emc { get; set; }
        public float alpha { get; set; }
        public float g { get; set; }
        public float uc { get; set; }
        public float _as { get; set; }
        public float bs { get; set; }
        public float lambda { get; set; }
        public float rimax { get; set; }
        public float wcoeff { get; set; }
        public float a { get; set; }
        public float c { get; set; }
    }

    internal class SiteInitialConditions
    {
        public bool is_usic_constant { get; set; }
        public float usic_constant_value { get; set; }
        public string usic_grid_file_name { get; set; }
        public string usic_grid_file_format { get; set; }

        public bool is_wsis_constant { get; set; }
        public float wsis_constant_value { get; set; }
        public string wsis_grid_file_name { get; set; }
        public string wsis_grid_file_format { get; set; }

        public bool is_tic_constant { get; set; }
        public float tic_constant_value { get; set; }
        public string tic_grid_file_name { get; set; }
        public string tic_grid_file_format { get; set; }

        public bool is_wcic_constant { get; set; }
        public float wcic_constant_value { get; set; }
        public string wcic_grid_file_name { get; set; }
        public string wcic_grid_file_format { get; set; }

        public bool is_df_constant { get; set; }
        public float df_constant_value { get; set; }
        public string df_grid_file_name { get; set; }
        public string df_grid_file_format { get; set; }

        public bool is_sbar_constant { get; set; }
        public float sbar_constant_value { get; set; }
        public string sbar_grid_file_name { get; set; }
        public string sbar_grid_file_format { get; set; }

        public bool is_apr_derive_from_elevation { get; set; }
        public bool is_apr_constant { get; set; }
        public float apr_constant_value { get; set; }
        public string apr_grid_file_name { get; set; }
        public string apr_grid_file_format { get; set; }

        public bool is_aep_constant { get; set; }
        public float aep_constant_value { get; set; }
        public string aep_grid_file_name { get; set; }
        public string aep_grid_file_format { get; set; }

        public bool is_cc_constant { get; set; }
        public float? cc_constant_value { get; set; }
        public bool is_cc_derive_from_NLCD { get; set; }       
        public string cc_grid_file_name { get; set; }
        public string cc_grid_file_format { get; set; }

        public bool is_hcan_constant { get; set; }
        public float? hcan_constant_value { get; set; }
        public bool is_hcan_derive_from_NLCD { get; set; }        
        public string hcan_grid_file_name { get; set; }
        public string hcan_grid_file_format { get; set; }

        public bool is_lai_constant { get; set; }
        public float? lai_constant_value { get; set; }
        public bool is_lai_derive_from_NLCD { get; set; }        
        public string lai_grid_file_name { get; set; }
        public string lai_grid_file_format { get; set; }

        public bool is_ycage_constant { get; set; }
        public float? ycage_constant_value { get; set; }
        public bool is_ycage_derive_from_NLCD { get; set; }        
        public string ycage_grid_file_name { get; set; }
        public string ycage_grid_file_format { get; set; }

        public bool is_slope_constant { get; set; }
        public float? slope_constant_value { get; set; }
        public bool is_slope_derive_from_elevation { get; set; }        
        public string slope_grid_file_name { get; set; }
        public string slope_grid_file_format { get; set; }

        public bool is_aspect_constant { get; set; }
        public float? aspect_constant_value { get; set; }
        public bool is_aspect_derive_from_elevation { get; set; }        
        public string aspect_grid_file_name { get; set; }
        public string aspect_grid_file_format { get; set; }

        public bool is_latitude_constant { get; set; }
        public float? latitude_constant_value { get; set; }
        public bool is_latitude_derive_from_projection { get; set; }        
        public string latitude_grid_file_name { get; set; }
        public string latitude_grid_file_format { get; set; }

        public bool is_longitude_constant { get; set; }
        public float? longitude_constant_value { get; set; }
        public bool is_longitude_derive_from_projection { get; set; }       
        public string longitude_grid_file_name { get; set; }
        public string longitude_grid_file_format { get; set; }

        public bool is_subalb_constant { get; set; }
        public float subalb_constant_value { get; set; }
        public string subalb_grid_file_name { get; set; }
        public string subalb_grid_file_format { get; set; }

        public bool is_subtype_constant { get; set; }
        public int subtype_constant_value { get; set; }
        public string subtype_grid_file_name { get; set; }
        public string subtype_grid_file_format { get; set; }

        public bool is_gsurf_constant { get; set; }
        public float gsurf_constant_value { get; set; }
        public string gsurf_grid_file_name { get; set; }
        public string gsurf_grid_file_format { get; set; }

        public bool is_ts_last_constant { get; set; }
        public float ts_last_constant_value { get; set; }
        public string ts_last_grid_file_name { get; set; }
        public string ts_last_grid_file_format { get; set; }

    }

    internal class BristowCambellBValues
	{
        public float b01 { get; set; }
        public float b02 { get; set; }
        public float b03 { get; set; }
        public float b04 { get; set; }
        public float b05 { get; set; }
        public float b06 { get; set; }
        public float b07 { get; set; }
        public float b08 { get; set; }
        public float b09 { get; set; }
        public float b10 { get; set; }
        public float b11 { get; set; }
        public float b12 { get; set; }
	}

    internal class TimeSeriesInputs
    {
        public string data_source_name { get; set; } // daymet for now

        public bool is_ta_compute { get; set; }
        public bool is_ta_constant { get; set; }
        public float? ta_constant_value { get; set; }        
        public string ta_text_file_name { get; set; }
        public string ta_grid_file_name { get; set; }
        public string ta_grid_file_format { get; set; }

        public bool is_prec_compute { get; set; }
        public bool is_prec_constant { get; set; }
        public float? prec_constant_value { get; set; }
        public string prec_text_file_name { get; set; }
        public string prec_grid_file_name { get; set; }
        public string prec_grid_file_format { get; set; }
        
        public bool is_v_compute { get; set; }
        public bool is_v_constant { get; set; }
        public float? v_constant_value { get; set; }
        public string v_text_file_name { get; set; }
        public string v_grid_file_name { get; set; }
        public string v_grid_file_format { get; set; }

        public bool is_rh_compute { get; set; }
        public bool is_rh_constant { get; set; }
        public float? rh_constant_value { get; set; }
        public string rh_text_file_name { get; set; }
        public string rh_grid_file_name { get; set; }
        public string rh_grid_file_format { get; set; }

        public bool is_snowalb_compute { get; set; }
        public bool is_snowalb_constant { get; set; }
        public float snowalb_constant_value { get; set; }
        public string snowalb_text_file_name { get; set; }
        public string snowalb_grid_file_name { get; set; }
        public string snowalb_grid_file_format { get; set; }

        public bool is_qg_constant { get; set; }
        public float qg_constant_value { get; set; }
        public string qg_text_file_name { get; set; }
        public string qg_grid_file_name { get; set; }
        public string qg_grid_file_format { get; set; }
    }

    // TODO: not in use anymore as now these inputs will come from the client in a file
    internal class OutputVariables
    {        
        // variables that are selected will be set to true
        public bool atf { get; set; }
        public bool hri { get; set; }
        public bool Ub { get; set; }
        public bool SWIT { get; set; }        
        
    }

    // TODO: not in use anymore as now these inputs will come from the client in a file
    internal class AggregatedOutputVariables
    {
        public bool P { get; set; }
        public bool SWE { get; set; }
        public bool SWIT { get; set; }        
        

    }

    // TODO: Not used
    internal class OutletLocation
    {
        public float Latitude { get; set; }
        public float Longitude { get; set; }
    }
        
}