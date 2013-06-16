using NLog;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UWRL.CIWaterNetServer.UEB
{
    /// <summary>
    /// The purpose of the class is to generate final input/output controls files
    /// as per the UEB interface design specifications
    /// that needs to be part of a UEB model package
    /// </summary>
    public static class UEBFileManager
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        internal static void GenerateControlFiles(string destinationFilePath, DateTime startDate, DateTime endDate, byte timeStep, UEBPackageRequest uebPkgRequest)
        {
            TimeSeriesFiles timeSeriesFiles = new TimeSeriesFiles();
            //TODO: check the destinationFilePath exists
            // destinationFilePath value should be  E:\CIWaterData\Temp when testing on localhost
            if (Directory.Exists(destinationFilePath) == false)
            {
                string errMsg = destinationFilePath + " was not found for creating UEB control files.";
                _logger.Error(errMsg);
                throw new Exception(errMsg);
            }
            try
            {
                //create the Overallcontrol.dat file
                CreateOverallControlFile(destinationFilePath, uebPkgRequest);

                //create the param.dat file            
                //CreateParameterFile(destinationFilePath);

                //create the siteinital.dat file
                CreateSiteInitialFile(destinationFilePath, uebPkgRequest);

                //create the inputcontrol.dat file
                CreateInputControlFile(destinationFilePath, uebPkgRequest, timeSeriesFiles);

                //create the netCDFFileList.dat file
                //CreateNetCCDFFileListFile(destinationFilePath); // due to format change as suggested by Tseganeh

                // create dat files listing each of the timeseries netcdf files
                CreateNetCDFTimeSeriesListFiles(destinationFilePath, uebPkgRequest, timeSeriesFiles);

                //create the outputcontrol.dat file
                //CreateOutputControlFile(destinationFilePath);

                //create the aggregatedoutputcontrol.dat file
                //CreateAggregatedOutputControlFile(destinationFilePath);
            }
            catch (Exception ex)
            {
                string errMsg = "Failed to create UEB control files.\n";
                errMsg += ex.Message;
                _logger.Fatal(errMsg);
                throw new Exception(errMsg);
            }
        }

        private static void CreateOverallControlFile(string destFiePath, UEBPackageRequest uebPkgrequest)
        {
            // TODO: remove magic strings used in thei method
            string fileName = "Overallcontrol.dat";
            string fileToWriteTo = Path.Combine(destFiePath, fileName);
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine("UEBGrid Model Driver");                
                sw.WriteLine(uebPkgrequest.ModelParametersFileName); 
                sw.WriteLine("siteinitial.dat"); 
                sw.WriteLine("inputcontrol.dat"); 
                sw.WriteLine(uebPkgrequest.OutputControlFileName);
                if (uebPkgrequest.DomainFileName.EndsWith(".nc"))
                {
                    sw.WriteLine(uebPkgrequest.DomainFileName + ";" + uebPkgrequest.DomainGridFileFormat);
                }
                else
                {
                    sw.WriteLine(UEB.UEBSettings.WATERSHED_NETCDF_FILE_NAME + ";X:x;Y:y;D:watershed"); // this file is part of the UEB pacakge creation
                }                
                
                sw.WriteLine(uebPkgrequest.AggregatedOutputControlFileName);
                sw.WriteLine(UEB.UEBSettings.MODEL_OUTPUT_FOLDER_NAME + @"\AggregatedOutput.dat"); // this is where the UEB output will be written. no need for us to generate this file
            }

            _logger.Info(fileName + " file was created.");
        }

        // TODO: not used antmore as the parameters file will be provided by the client in its request to build a package
        private static void CreateParameterFile(string destFiePath)
        {
            string fileName = "param.dat";
            string fileToWriteTo = destFiePath + @"\" + fileName;

            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine("Model Parameters");
                sw.WriteLine("irad: Radiation control flag (0=from ta, 1= input qsi, 2= input qsi,qli 3= input qnet)");
                sw.WriteLine("0"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("ireadalb: Albedo reading control flag (0=albedo is computed internally, 1 albedo is read)");
                sw.WriteLine("0"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("tr: Temperature above which all is rain (3 C)");
                sw.WriteLine("3"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("ts: Temperature below which all is snow (-1 C)");
                sw.WriteLine("-1"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("ems: Emissivity of snow (nominally 0.99)");
                sw.WriteLine("0.99"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("cg: Ground heat capacity (nominally 2.09 KJ/kg/C)");
                sw.WriteLine("2.09"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("z: Nominal meas. heights for air temp. and humidity (2m)");
                sw.WriteLine("2"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("zo: Surface aerodynamic roughness (m)");
                sw.WriteLine("0.010"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("rho: Snow Density (Nominally 450 kg/m^3)");
                sw.WriteLine("337"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("rhog: Soil Density (nominally 1700 kg/m^3)");
                sw.WriteLine("1700"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("lc: Liquid holding capacity of snow (0.05)");
                sw.WriteLine("0.05"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("ks: Snow Saturated hydraulic conductivity (20 m/hr)");
                sw.WriteLine("20"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("de: Thermally active depth of soil (0.1 m)");
                sw.WriteLine("0.1"); //default value, later need to get this value form a user uploaded moddel param input file
                
                sw.WriteLine("avo: Visual new snow albedo (0.95)");
                sw.WriteLine("0.85"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("anir0: NIR new snow albedo (0.65)");
                sw.WriteLine("0.65"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("lans: The thermal conductivity of fresh (dry) snow");
                sw.WriteLine("1.0"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("lang: the thermal conductivity of soil");
                sw.WriteLine("4.0"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("wlf: Low frequency fluctuation in deep snow/soil layer ");
                sw.WriteLine("0.0654"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("rdl: Amplitude correction coefficient of heat conduction (1)");
                sw.WriteLine("1"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("dnews: The threshold depth of for new snow (0.001 m)");
                sw.WriteLine("0.001"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("emc: Emissivity of canopy");
                sw.WriteLine("0.98"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("fstab: Stability correction control parameter 0 = no corrections");
                sw.WriteLine("1.0"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("tref: Reference temperature of soil layer in ground heat calculation input");
                sw.WriteLine("-0.45"); //default value, later need to get this value form a user uploaded moddel param input file
                
                sw.WriteLine("alpha: Scattering coefficient for solar radiation");
                sw.WriteLine("0.5"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("alphal: Scattering coefficient for long wave radiation");
                sw.WriteLine("0.0"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("g: leaf orientation with respect to zenith angle");
                sw.WriteLine("0.5"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("uc: leaf orientation with respect to zenith angle");
                sw.WriteLine("0.004626286"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("as: Fraction of extraterrestrial radiation on cloudy day, Shuttleworth (1993)");
                sw.WriteLine("0.25"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("Bs: (as+bs):Fraction of extraterrestrial radiation on clear day, Shuttleworth");
                sw.WriteLine("0.5"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("lambda: Ratio of direct atm radiation to diffuse, worked out from Dingman");
                sw.WriteLine("0.857143"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("rimax: Maximum value of Richardson number for stability correction");
                sw.WriteLine("0.16"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("wcoeff: Wind decay coefficient for the forest");
                sw.WriteLine("0.5"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("a: A in Bristow-Campbell formula for atmospheric transmittance");
                sw.WriteLine("0.8"); //default value, later need to get this value form a user uploaded moddel param input file

                sw.WriteLine("c: C in Bristow-Campbell formula for atmospheric transmittance");
                sw.WriteLine("2.4"); //default value, later need to get this value form a user uploaded moddel param input file
            }

            _logger.Info(fileName + " file was created.");
        }

        private static void CreateSiteInitialFile(string destFiePath, UEBPackageRequest uebPkgRequest)
        {
            // TODO: remove magic strings used in this method

            //first read the atmos pressure value from the text file
            string fileName = UEB.UEBSettings.WATERSHED_ATMOSPHERIC_PRESSURE_FILE_NAME; // "ws_atom_pres.txt";
            float atomPresValue;
            string fileToReadFrom =Path.Combine(destFiePath, fileName);
            using (StreamReader sr = new StreamReader(fileToReadFrom))
            {
                //read the one line of text in this file                
                string data = sr.ReadLine();
                bool success = float.TryParse(data, out atomPresValue);

                if (success == false)
                {
                    atomPresValue = 74051; // default value
                }
            }

            fileName = "siteinitial.dat";
            string fileToWriteTo = Path.Combine(destFiePath, fileName);
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine("Site and Initial Condition Input Variables");

                sw.WriteLine("USic: Energy content initial condition (kg m-3)");
                if (uebPkgRequest.SiteInitialConditions.is_usic_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.usic_constant_value);
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.usic_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.usic_grid_file_format); 
                }
               

                sw.WriteLine("WSis: Snow water equivalent initial condition (m)");
                if (uebPkgRequest.SiteInitialConditions.is_wsis_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable                
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.wsis_constant_value); 
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.wsis_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.wsis_grid_file_format);
                }

                sw.WriteLine("Tic: Canopy Snow Water Equivalent (m) relative to T = 0 C solid phase");
                if (uebPkgRequest.SiteInitialConditions.is_tic_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable                    
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.tic_constant_value); 
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.tic_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.tic_grid_file_format);
                }
               
                sw.WriteLine("WCic: Snow water equivalent dimensionless age initial condition (m) ");
                if (uebPkgRequest.SiteInitialConditions.is_wcic_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable                    
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.wcic_constant_value); 
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.wcic_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.wcic_grid_file_format);
                }
                
                sw.WriteLine("df: Drift factor multiplier");
                if (uebPkgRequest.SiteInitialConditions.is_df_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable                    
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.df_constant_value); 
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.df_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.df_grid_file_format);
                }

                sw.WriteLine("apr: Average atmospheric pressure");                
                if (uebPkgRequest.SiteInitialConditions.is_apr_derive_from_elevation)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable
                    sw.WriteLine(atomPresValue);
                }
                else if(uebPkgRequest.SiteInitialConditions.is_apr_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.apr_constant_value);
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.apr_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.apr_grid_file_format);
                }
                
                sw.WriteLine("Aep: Albedo extinction coefficient");
                if (uebPkgRequest.SiteInitialConditions.is_aep_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable                    
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.aep_constant_value); 
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.aep_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.aep_grid_file_format);
                }
                
                sw.WriteLine("cc: Canopy coverage fraction");                
                if (uebPkgRequest.SiteInitialConditions.is_cc_derive_from_NLCD)
                {
                    sw.WriteLine("1"); // variable flag value for SVTC type variable
                    sw.WriteLine(UEB.UEBSettings.WATERSHED_NLCD_CC_NETCDF_FILE_NAME + ";X:x;Y:y;D:cc"); //netcdf file taht contains data for cc
                }
                else if (uebPkgRequest.SiteInitialConditions.is_cc_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.cc_constant_value);
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SVTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.cc_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.cc_grid_file_format); 
                }
                                
                sw.WriteLine("hcan: Canopy height");                
                if (uebPkgRequest.SiteInitialConditions.is_hcan_derive_from_NLCD)
                {
                    sw.WriteLine("1"); // variable flag value for SVTC type variable
                    sw.WriteLine(UEB.UEBSettings.WATERSHED_NLCD_HC_NETCDF_FILE_NAME + ";X:x;Y:y;D:hcan"); //netcdf file that contains data for hcan
                }
                else if (uebPkgRequest.SiteInitialConditions.is_hcan_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.hcan_constant_value);
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SVTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.hcan_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.hcan_grid_file_format);
                }
                 
                sw.WriteLine("lai: Leaf area index");                
                if (uebPkgRequest.SiteInitialConditions.is_lai_derive_from_NLCD)
                {
                    sw.WriteLine("1"); // variable flag value for SVTC type variable
                    sw.WriteLine(UEB.UEBSettings.WATERSHED_NLCD_LAI_NETCDF_FILE_NAME + ";X:x;Y:y;D:lai"); //netcdf file that contains data for lai
                }
                else if (uebPkgRequest.SiteInitialConditions.is_lai_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.lai_constant_value);
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SVTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.lai_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.lai_grid_file_format);
                }
                
                sw.WriteLine("ycage: Forest age flag for wind speed profile parameterization");                
                if (uebPkgRequest.SiteInitialConditions.is_ycage_derive_from_NLCD)
                {
                    sw.WriteLine("1"); // variable flag value for SVTC type variable
                    sw.WriteLine(UEB.UEBSettings.WATERSHED_NLCD_YCAGE_NETCDF_FILE_NAME + ";X:x;Y:y;D:ycage"); //netcdf file that contains data for ycage
                }
                else if (uebPkgRequest.SiteInitialConditions.is_ycage_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.ycage_constant_value);
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SVTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.ycage_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.ycage_grid_file_format);
                }
                                
                sw.WriteLine("Sbar: Maximum snow load held per unit branch area");
                if (uebPkgRequest.SiteInitialConditions.is_sbar_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable                    
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.sbar_constant_value); 
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SVTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.sbar_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.sbar_grid_file_format);
                }
                
                sw.WriteLine("slope: A 2-D grid that contains the slope at each grid point");               
                if (uebPkgRequest.SiteInitialConditions.is_slope_derive_from_elevation)
                {
                    sw.WriteLine("1"); // variable flag value for SVTC type variable
                    sw.WriteLine(UEB.UEBSettings.WATERSHED_SLOPE_NETCDF_FILE_NAME + ";X:x;Y:y;D:slope"); //netcdf file that contains data for slope
                }
                else if (uebPkgRequest.SiteInitialConditions.is_slope_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.slope_constant_value);
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SVTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.slope_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.slope_grid_file_format);
                }
                
                sw.WriteLine("aspect: A 2-D grid that contains the aspect at each grid point");                
                if (uebPkgRequest.SiteInitialConditions.is_aspect_derive_from_elevation)
                {
                    sw.WriteLine("1"); // variable flag value for SVTC type variable
                    sw.WriteLine(UEB.UEBSettings.WATERSHED_ASPECT_NETCDF_FILE_NAME + ";X:x;Y:y;D:aspect"); //netcdf file that contains data for aspect
                }
                else if (uebPkgRequest.SiteInitialConditions.is_aspect_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.aspect_constant_value);
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SVTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.aspect_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.aspect_grid_file_format);
                }

                if (uebPkgRequest.SiteInitialConditions.is_latitude_derive_from_projection)
                {
                    // There may not be lat gridded file in which case there
                    // would be ws-lat.txt file that contains a single value for the whole ws grid
                    // so we need to process that and write to the file in three line format with
                    // variable type = 0
                    fileName = UEB.UEBSettings.WATERSHED_LATITUDE_FILE_NAME_WITHOUT_EXTENSION + ".nc";
                    string fileToCheck = destFiePath + @"\" + fileName;
                    if (File.Exists(fileToCheck))
                    {
                        sw.WriteLine("latitude: A 2-D grid that contains the latitude at each grid point");
                        sw.WriteLine("1"); // variable flag value for SVTC type variable
                        sw.WriteLine(fileName + ";X:x;Y:y;D:latitude"); //netcdf file that contains data for latitude                        
                    }
                    else
                    {
                        fileName = "lat.txt";
                        fileToReadFrom = destFiePath + @"\" + fileName;
                        string data = GetSingleDataValueInFile(fileToReadFrom);
                        float latitude;
                        bool success = float.TryParse(data, out latitude);
                        if (!success)
                        {
                            string errMsg = "No valid latitude data for the watershed was found: " + data;
                            _logger.Fatal("errMsg");
                            throw new Exception(errMsg);
                        }
                        else
                        {
                            sw.WriteLine("latitude: Latidue of the watershed at grid mid point");
                            sw.WriteLine("0"); // variable flag value for SCTC type variable
                            sw.WriteLine(latitude); // value of latitude variable
                        }
                    }
                }
                else if (uebPkgRequest.SiteInitialConditions.is_latitude_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.latitude_constant_value);
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SVTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.latitude_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.latitude_grid_file_format);
                }
                                
                sw.WriteLine("subalb: Albedo (fraction 0-1) of the substrate beneath the snow (ground, or glacier)");
                if (uebPkgRequest.SiteInitialConditions.is_subalb_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable                    
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.subalb_constant_value); 
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.subalb_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.subalb_grid_file_format);
                }

                sw.WriteLine("subtype: Type of beneath snow substrate encoded as (0 = Ground/Non Glacier, 1=Clean Ice/glacier, 2= Debris covered ice/glacier, 3= Glacier snow accumulation zone)");
                if (uebPkgRequest.SiteInitialConditions.is_subtype_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable
                    //sw.WriteLine("0"); //default value, later need to get this value form a user uploaded siteinitial input file
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.subtype_constant_value); 
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.subtype_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.subtype_grid_file_format);
                }

                sw.WriteLine("gsurf: The fraction of surface melt that runs off (e.g. from a glacier)");
                if (uebPkgRequest.SiteInitialConditions.is_gsurf_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable                    
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.gsurf_constant_value); 
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.gsurf_grid_file_name + ";" + uebPkgRequest.SiteInitialConditions.gsurf_grid_file_format);
                }

                sw.WriteLine("b01: Bristow-Campbell B for January (1)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable                
                sw.WriteLine(uebPkgRequest.BristowCambellBValues.b01); 

                sw.WriteLine("b02: Bristow-Campbell B for Feb (2)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable                
                sw.WriteLine(uebPkgRequest.BristowCambellBValues.b02); 

                sw.WriteLine("b03: Bristow-Campbell B for Mar (3)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable                
                sw.WriteLine(uebPkgRequest.BristowCambellBValues.b03); 
                
                sw.WriteLine("b04: Bristow-Campbell B for Apr (4)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable                
                sw.WriteLine(uebPkgRequest.BristowCambellBValues.b04); 

                sw.WriteLine("b05: Bristow-Campbell B for May (5)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable                
                sw.WriteLine(uebPkgRequest.BristowCambellBValues.b05); 

                sw.WriteLine("b06: Bristow-Campbell B for June (6)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable                
                sw.WriteLine(uebPkgRequest.BristowCambellBValues.b06); 

                sw.WriteLine("b07: Bristow-Campbell B for July (7)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable                
                sw.WriteLine(uebPkgRequest.BristowCambellBValues.b07); 

                sw.WriteLine("b08: Bristow-Campbell B for Aug (8)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable               
                sw.WriteLine(uebPkgRequest.BristowCambellBValues.b08); 

                sw.WriteLine("b09: Bristow-Campbell B for Sep (9)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable                
                sw.WriteLine(uebPkgRequest.BristowCambellBValues.b09); 

                sw.WriteLine("b10: Bristow-Campbell B for Oct (10)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable                
                sw.WriteLine(uebPkgRequest.BristowCambellBValues.b10); 

                sw.WriteLine("b11: Bristow-Campbell B for Nov (11)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable                
                sw.WriteLine(uebPkgRequest.BristowCambellBValues.b11); 

                sw.WriteLine("b12: Bristow-Campbell B for Dec (12)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable                
                sw.WriteLine(uebPkgRequest.BristowCambellBValues.b12); 

                sw.WriteLine("ts_last: degree celcius");
                sw.WriteLine("0"); // variable flag value for SCTC type variable                
                sw.WriteLine(uebPkgRequest.SiteInitialConditions.ts_last_constant_value);

                if (uebPkgRequest.SiteInitialConditions.is_longitude_derive_from_projection)
                {
                    // there may not be lon gridded file in which case there
                    // would be ws-lon.txt file that contains a single value for the whole ws grid
                    // so we need to process that and write to the file in three line format with
                    // variable type = 0
                    fileName = UEB.UEBSettings.WATERSHED_LONGITIDUE_FILE_NAME_WITHOUT_EXTENSION + ".nc";
                    string fileToCheck = Path.Combine(destFiePath, fileName);
                    if (File.Exists(fileToCheck))
                    {
                        sw.WriteLine("longitude: A 2-D grid that contains the longitude at each grid point");
                        sw.WriteLine("1"); // variable flag value for SVTC type variable
                        sw.WriteLine(fileName + ";X:x;Y:y;D:longitude"); //netcdf file that contains data for lon                        
                    }
                    else
                    {
                        fileName = "lon.txt";
                        fileToReadFrom = Path.Combine(destFiePath, fileName);
                        string data = GetSingleDataValueInFile(fileToReadFrom);
                        float longitidue;
                        bool success = float.TryParse(data, out longitidue);
                        if (!success)
                        {
                            string errMsg = "No valid longitidue data for the watershed was found: " + data;
                            _logger.Fatal("errMsg");
                            throw new Exception(errMsg);
                        }
                        else
                        {
                            sw.WriteLine("longitidue: Longitidue of the watershed at grid mid point");
                            sw.WriteLine("0"); // variable flag value for SCTC type variable
                            sw.WriteLine(longitidue); // value of latitude variable
                        }
                    }
                }
                else if (uebPkgRequest.SiteInitialConditions.is_longitude_constant)
                {
                    sw.WriteLine("0"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.longitude_constant_value);
                }
                else
                {
                    sw.WriteLine("1"); // variable flag value for SVTC type variable
                    sw.WriteLine(uebPkgRequest.SiteInitialConditions.longitude_grid_file_name + uebPkgRequest.SiteInitialConditions.longitude_grid_file_format);
                }
            }
            _logger.Info(fileName + " file was created.");
        }

        private static void CreateInputControlFile(string destFiePath, UEBPackageRequest uebPkgRequest, TimeSeriesFiles timeSeriesFiles) //string destFiePath, DateTime startDate, DateTime endDate, byte timeStep, UEBPackageRequest uebPkgRequest
        {
            // TODO: remove magic strings used in this method
            //create the inputcontrol.dat file       
            string fileName = "inputcontrol.dat";
            string fileToWriteTo = Path.Combine(destFiePath, fileName);
            DateTime startDate = uebPkgRequest.StartDate;
            DateTime endDate = uebPkgRequest.EndDate;
            byte timeStep = uebPkgRequest.TimeStep;
                            
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine("Input Control file");
                string dateFormat = "yyyy MM dd hh:mm";                
                sw.WriteLine(startDate.ToString(dateFormat)); // yyyy mm dd hh.hh (starting Date)
                sw.WriteLine(endDate.ToString(dateFormat)); // yyyy mm dd hh.hh (ending Date)
                sw.WriteLine(timeStep); // time step
                
                //TODO: Check how can we avoid hard coding this UTC time offset value
                sw.WriteLine("6.00"); // UTC time offset

                sw.WriteLine("Ta: Air temperature  (always required)");
                if (uebPkgRequest.TimeSeriesInputs.is_ta_compute)
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine("Talist.dat;X:x;Y:y;time:time;D:T"); // this is the new format suggested by Tseganeh
                    timeSeriesFiles.TaFileName = "Talist.dat";
                }
                else if(uebPkgRequest.TimeSeriesInputs.is_ta_constant)
                {
                    sw.WriteLine("2"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.TimeSeriesInputs.ta_constant_value); // value of ta variable
                }
                else if (uebPkgRequest.TimeSeriesInputs.ta_grid_file_name.EndsWith(".nc"))
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine("Talist.dat;" + uebPkgRequest.TimeSeriesInputs.ta_grid_file_format);
                    timeSeriesFiles.TaFileName = "Talist.dat";
                }
                else if(string.IsNullOrEmpty(uebPkgRequest.TimeSeriesInputs.ta_text_file_name) == false)
                {                    
                    sw.WriteLine("0"); // variable flag value for SCTC type variable
                    sw.Write(uebPkgRequest.TimeSeriesInputs.ta_text_file_name);
                }
                
                sw.WriteLine("Prec: Precipitation  (always required)");
                if (uebPkgRequest.TimeSeriesInputs.is_prec_compute)
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine("Preclist.dat;X:x;Y:y;time:time;D:Prec"); // this is the new format suggested by Tseganeh
                    timeSeriesFiles.PrecFileName = "Preclist.dat";
                }
                else if (uebPkgRequest.TimeSeriesInputs.is_prec_constant)
                {
                    sw.WriteLine("2"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.TimeSeriesInputs.prec_constant_value); // value of prec variable
                }
                else if (uebPkgRequest.TimeSeriesInputs.prec_grid_file_name.EndsWith(".nc"))
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine("Preclist.dat;" + uebPkgRequest.TimeSeriesInputs.prec_grid_file_format);
                    timeSeriesFiles.PrecFileName = "Preclist.dat";
                }
                else if (string.IsNullOrEmpty(uebPkgRequest.TimeSeriesInputs.prec_text_file_name) == false)
                {                    
                    sw.WriteLine("0"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.TimeSeriesInputs.prec_text_file_name);
                }
                                                
                sw.WriteLine("v: Wind speed (always required)");
                if (uebPkgRequest.TimeSeriesInputs.is_v_compute)
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine("Vlist.dat;X:x;Y:y;time:time;D:V"); // this is the new format suggested by Tseganeh
                    timeSeriesFiles.VFileName = "Vlist.dat";
                }
                else if (uebPkgRequest.TimeSeriesInputs.is_v_constant)
                {
                    sw.WriteLine("2"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.TimeSeriesInputs.v_constant_value); // value of v variable
                }
                else if (uebPkgRequest.TimeSeriesInputs.v_grid_file_name.EndsWith(".nc"))
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine("Vlist.dat;" + uebPkgRequest.TimeSeriesInputs.v_grid_file_format);
                    timeSeriesFiles.VFileName = "Vlist.dat";
                }
                else if (string.IsNullOrEmpty(uebPkgRequest.TimeSeriesInputs.v_text_file_name) == false)
                {                    
                    sw.WriteLine("0"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.TimeSeriesInputs.v_text_file_name);
                }
                
                sw.WriteLine("RH: Relative humidity (always required)");
                if (uebPkgRequest.TimeSeriesInputs.is_rh_compute)
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine("RHlist.dat;X:x;Y:y;time:time;D:rh"); // this is the new format suggested by Tseganeh
                    timeSeriesFiles.RhFileName = "RHlist.dat";
                }
                else if (uebPkgRequest.TimeSeriesInputs.is_rh_constant)
                {
                    sw.WriteLine("2"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.TimeSeriesInputs.rh_constant_value); // value of rh variable
                }
                else if (uebPkgRequest.TimeSeriesInputs.rh_grid_file_name.EndsWith(".nc"))
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine("RHlist.dat;" + uebPkgRequest.TimeSeriesInputs.rh_grid_file_format); // this is the new format suggested by Tseganeh
                    timeSeriesFiles.RhFileName = "RHlist.dat";
                }
                else if (string.IsNullOrEmpty(uebPkgRequest.TimeSeriesInputs.rh_text_file_name) == false)
                {                    
                    sw.WriteLine("0"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.TimeSeriesInputs.rh_text_file_name);
                }

                if (uebPkgRequest.TimeSeriesInputs.is_snowalb_compute == false)
                {
                    if (uebPkgRequest.TimeSeriesInputs.is_snowalb_constant)
                    {
                        sw.WriteLine("Snowalb: Snow albedo (0-1).  (only required if ireadalb=1) The albedo of the snow surface to be used when the internal albedo calculations are to be overridden");
                        sw.WriteLine("2"); // variable flag value for SCTC type variable                    
                        sw.WriteLine(uebPkgRequest.TimeSeriesInputs.snowalb_constant_value);
                    }
                    else if (uebPkgRequest.TimeSeriesInputs.snowalb_grid_file_name.EndsWith(".nc"))
                    {
                        sw.WriteLine("1"); // variable flag value for SCTC type variable
                        sw.WriteLine("Snowalblist.dat;" + uebPkgRequest.TimeSeriesInputs.snowalb_grid_file_format); // this is the new format suggested by Tseganeh
                        timeSeriesFiles.SnowalbFileName = "Snowalblist.dat";
                    }
                    else if (string.IsNullOrEmpty(uebPkgRequest.TimeSeriesInputs.snowalb_text_file_name) == false)
                    {                        
                        sw.WriteLine("0"); // variable flag value for SCTC type variable
                        sw.WriteLine(uebPkgRequest.TimeSeriesInputs.snowalb_text_file_name);
                    }
                }
                

                if (uebPkgRequest.TimeSeriesInputs.is_qg_constant)
                {
                    sw.WriteLine("Qg: : Ground heat flux   (kJ/m2/hr)");
                    sw.WriteLine("2"); // variable flag value for SCTC type variable                
                    sw.WriteLine(uebPkgRequest.TimeSeriesInputs.qg_constant_value);
                }
                else if (uebPkgRequest.TimeSeriesInputs.qg_grid_file_name.EndsWith(".nc"))
                {
                    sw.WriteLine("1"); // variable flag value for SCTC type variable
                    sw.WriteLine("Qglist.dat;" + uebPkgRequest.TimeSeriesInputs.snowalb_grid_file_format); // this is the new format suggested by Tseganeh
                    timeSeriesFiles.QgFileName = "Qglist.dat";
                }
                else if (string.IsNullOrEmpty(uebPkgRequest.TimeSeriesInputs.qg_text_file_name) == false)
                {                    
                    sw.WriteLine("0"); // variable flag value for SCTC type variable
                    sw.WriteLine(uebPkgRequest.TimeSeriesInputs.qg_text_file_name);
                }
                
            }

            _logger.Info(fileName + " file was created.");
        }

        //TODO: create the netCDFFileList.dat file - not used
        private static void CreateNetCDFFileListFile(string destFiePath)
        {
            string fileName = "netCDFFileList.dat";
            string fileToWriteTo = destFiePath + @"\" + fileName;
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine(UEB.UEBSettings.WATERSHED_MULTIPLE_TEMP_NETCDF_FILE_NAME);
                sw.WriteLine(UEB.UEBSettings.WATERSHED_MULTIPLE_PRECP_NETCDF_FILE_NAME);
                sw.WriteLine(UEB.UEBSettings.WATERSHED_MULTIPLE_WIND_NETCDF_FILE_NAME);
                sw.WriteLine(UEB.UEBSettings.WATERSHED_MULTIPLE_RH_NETCDF_FILE_NAME);
            }
            _logger.Info(fileName + " file was created.");

        }
                
        private static void CreateNetCDFTimeSeriesListFiles(string destFiePath, UEBPackageRequest uebPkgRequest, TimeSeriesFiles timeSeriesFiles)
        {            
            string fileToWriteTo = string.Empty;

            if (string.IsNullOrEmpty(timeSeriesFiles.TaFileName) == false)
            {
                fileToWriteTo = Path.Combine(destFiePath, timeSeriesFiles.TaFileName);
                using (StreamWriter sw = new StreamWriter(fileToWriteTo))
                {
                    if (uebPkgRequest.TimeSeriesInputs.is_ta_compute)
                    {
                        sw.WriteLine(UEB.UEBSettings.WATERSHED_MULTIPLE_TEMP_NETCDF_FILE_NAME);
                    }
                    else if (uebPkgRequest.TimeSeriesInputs.ta_grid_file_name.EndsWith(".nc"))
                    {
                        sw.WriteLine(uebPkgRequest.TimeSeriesInputs.ta_grid_file_name);
                    }
                    else if (string.IsNullOrEmpty(uebPkgRequest.TimeSeriesInputs.ta_text_file_name) == false)
                    {                        
                        sw.WriteLine(uebPkgRequest.TimeSeriesInputs.ta_text_file_name);
                    }
                }

                _logger.Info(timeSeriesFiles.TaFileName + " file was created.");
            }

            if (string.IsNullOrEmpty(timeSeriesFiles.PrecFileName) == false)
            {
                fileToWriteTo = Path.Combine(destFiePath, timeSeriesFiles.PrecFileName);
                using (StreamWriter sw = new StreamWriter(fileToWriteTo))
                {
                    if (uebPkgRequest.TimeSeriesInputs.is_prec_compute)
                    {
                        sw.WriteLine(UEB.UEBSettings.WATERSHED_MULTIPLE_PRECP_NETCDF_FILE_NAME);
                    }
                    else if (uebPkgRequest.TimeSeriesInputs.prec_grid_file_name.EndsWith(".nc"))
                    {
                        sw.WriteLine(uebPkgRequest.TimeSeriesInputs.prec_grid_file_name);
                    }
                    else if (string.IsNullOrEmpty(uebPkgRequest.TimeSeriesInputs.prec_text_file_name) == false)
                    {
                        sw.WriteLine(uebPkgRequest.TimeSeriesInputs.prec_text_file_name);
                    }
                }

                _logger.Info(timeSeriesFiles.PrecFileName + " file was created.");
            }

            if (string.IsNullOrEmpty(timeSeriesFiles.VFileName) == false)
            {
                fileToWriteTo = Path.Combine(destFiePath, timeSeriesFiles.VFileName);
                using (StreamWriter sw = new StreamWriter(fileToWriteTo))
                {
                    if (uebPkgRequest.TimeSeriesInputs.is_v_compute)
                    {
                        sw.WriteLine(UEB.UEBSettings.WATERSHED_MULTIPLE_WIND_NETCDF_FILE_NAME);
                    }
                    else if (uebPkgRequest.TimeSeriesInputs.v_grid_file_name.EndsWith(".nc"))
                    {
                        sw.WriteLine(uebPkgRequest.TimeSeriesInputs.v_grid_file_name);
                    }
                    else if (string.IsNullOrEmpty(uebPkgRequest.TimeSeriesInputs.v_text_file_name) == false)
                    {
                        sw.WriteLine(uebPkgRequest.TimeSeriesInputs.v_text_file_name);
                    }
                }

                _logger.Info(timeSeriesFiles.VFileName + " file was created.");
            }

            if (string.IsNullOrEmpty(timeSeriesFiles.RhFileName) == false)
            {
                fileToWriteTo = Path.Combine(destFiePath, timeSeriesFiles.RhFileName);
                using (StreamWriter sw = new StreamWriter(fileToWriteTo))
                {
                    if (uebPkgRequest.TimeSeriesInputs.is_rh_compute)
                    {
                        sw.WriteLine(UEB.UEBSettings.WATERSHED_MULTIPLE_RH_NETCDF_FILE_NAME);
                    }
                    else if (uebPkgRequest.TimeSeriesInputs.rh_grid_file_name.EndsWith(".nc"))
                    {
                        sw.WriteLine(uebPkgRequest.TimeSeriesInputs.rh_grid_file_name);
                    }
                    else if (string.IsNullOrEmpty(uebPkgRequest.TimeSeriesInputs.rh_text_file_name) == false)
                    {
                        sw.WriteLine(uebPkgRequest.TimeSeriesInputs.rh_text_file_name);
                    }
                }

                _logger.Info(timeSeriesFiles.RhFileName + " file was created.");
            }

            if (string.IsNullOrEmpty(timeSeriesFiles.SnowalbFileName) == false)
            {
                fileToWriteTo = Path.Combine(destFiePath, timeSeriesFiles.SnowalbFileName);
                using (StreamWriter sw = new StreamWriter(fileToWriteTo))
                {
                    if (uebPkgRequest.TimeSeriesInputs.snowalb_grid_file_name.EndsWith(".nc"))
                    {
                        sw.WriteLine(uebPkgRequest.TimeSeriesInputs.snowalb_grid_file_name);
                    }
                    else if (string.IsNullOrEmpty(uebPkgRequest.TimeSeriesInputs.snowalb_text_file_name) == false)
                    {
                        sw.WriteLine(uebPkgRequest.TimeSeriesInputs.snowalb_text_file_name);
                    }
                }

                _logger.Info(timeSeriesFiles.SnowalbFileName + " file was created.");
            }

            if (string.IsNullOrEmpty(timeSeriesFiles.QgFileName) == false)
            {
                fileToWriteTo = Path.Combine(destFiePath, timeSeriesFiles.QgFileName);
                using (StreamWriter sw = new StreamWriter(fileToWriteTo))
                {
                    if (uebPkgRequest.TimeSeriesInputs.qg_grid_file_name.EndsWith(".nc"))
                    {
                        sw.WriteLine(uebPkgRequest.TimeSeriesInputs.qg_grid_file_name);
                    }
                    else if (string.IsNullOrEmpty(uebPkgRequest.TimeSeriesInputs.qg_text_file_name) == false)
                    {
                        sw.WriteLine(uebPkgRequest.TimeSeriesInputs.qg_text_file_name);
                    }
                }

                _logger.Info(timeSeriesFiles.QgFileName + " file was created.");
            }
        }

        //TODO: create the outputcontrol.dat file - not used
        private static void CreateOutputControlFile(string destFiePath)
        {
            string fileName = "outputcontrol.dat";
            string fileToWriteTo = Path.Combine(destFiePath, fileName);
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine("List of output variables");

                // TODO: These values (varialble names should come as user inputs and thus not to be hard coded
                // need to think how such a long list of user input values would come from the client side
                // to this server side code
                sw.WriteLine("atf: Atmospheric transmission factor"); // name of the variable for which output data is needed
                sw.WriteLine(UEB.UEBSettings.MODEL_OUTPUT_FOLDER_NAME + @"\" + "atf.nc"); // output file name with path

                sw.WriteLine("hri: radiation index"); // name of the variable for which output data is needed
                sw.WriteLine(UEB.UEBSettings.MODEL_OUTPUT_FOLDER_NAME + @"\" + "hri.nc"); // output file name with path

                //there could be more variables
            }

            _logger.Info(fileName + " file was created.");
        }

        //TODO: create the aggregatedoutputcontrol.dat file - not used
        private static void CreateAggregatedOutputControlFile(string destFiePath)
        {
            string fileName = "Aggregatedoutputcontrol.dat";
            string fileToWriteTo = destFiePath + @"\" + fileName;
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine("List of Aggregated output variables");

                // TODO: These values (varialble names should come as user inputs and thus not to be hard coded
                sw.WriteLine("P: Precipitation (m/hr)");
                sw.WriteLine("SWE: Surface snow water equivalent  (m)");
                sw.WriteLine("SWIT: Total outflow  (m/hr)");

                // there could be more of these variables
            }

            _logger.Info(fileName + " file was created.");
        }

        private static string GetSingleDataValueInFile(string fileToRead)
        {
            string data = string.Empty;
            using (StreamReader sr = new StreamReader(fileToRead))
            {
                //read the one line of text in this file                
                data = sr.ReadLine();                
            }

            return data;
        }        
    }
    internal class TimeSeriesFiles
    {
        public string TaFileName {get; set;}
        public string PrecFileName {get; set;}
        public string RhFileName {get; set;}
        public string VFileName {get; set;}
        public string SnowalbFileName {get; set;}
        public string QgFileName {get; set;}

        public TimeSeriesFiles()
        {
            TaFileName = string.Empty;
            PrecFileName = string.Empty;
            RhFileName = string.Empty;
            VFileName = string.Empty;
            SnowalbFileName = string.Empty;
            QgFileName = string.Empty;
        }
    }
}