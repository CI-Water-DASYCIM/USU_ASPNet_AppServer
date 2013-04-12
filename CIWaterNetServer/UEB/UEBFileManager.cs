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

        public static void GenerateControlFiles(string destinationFilePath, DateTime startDate, DateTime endDate, byte timeStep)
        {
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
                CreateOverallControlFile(destinationFilePath);

                //create the param.dat file            
                CreateParameterFile(destinationFilePath);

                //create the siteinital.dat file
                CreateSiteInitialFile(destinationFilePath);

                //create the inputcontrol.dat file
                CreateInputControlFile(destinationFilePath, startDate, endDate, timeStep);

                //create the netCDFFileList.dat file
                //CreateNetCCDFFileListFile(destinationFilePath); // due to format change as suggested by Tseganeh

                // create dat files listing each of the timeseries netcdf files
                CreateNetCDFTimeSeriesListFiles(destinationFilePath);

                //create the outputcontrol.dat file
                CreateOutputControlFile(destinationFilePath);

                //create the aggregatedoutputcontrol.dat file
                CreateAggregatedOutputControlFile(destinationFilePath);
            }
            catch (Exception ex)
            {
                string errMsg = "Failed to create UEB control files.\n";
                errMsg += ex.Message;
                _logger.Fatal(errMsg);
                throw new Exception(errMsg);
            }
        }

        private static void CreateOverallControlFile(string destFiePath)
        {
            string fileName = "Overallcontrol.dat";
            string fileToWriteTo = destFiePath + @"\" + fileName;
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine("UEBGrid Model Driver");
                sw.WriteLine("param.dat"); 
                sw.WriteLine("siteinitial.dat"); 
                sw.WriteLine("inputcontrol.dat"); 
                sw.WriteLine("outputcontrol.dat"); 
                sw.WriteLine("watershed.nc;D:watershed"); // this file is part of the UEB pacakge creation
                //sw.WriteLine("Id"); // variable name used in creating the watershed.nc (see py script "WatershedFeaturesToNetCDfConversion.py"
                sw.WriteLine("Aggregatedoutputcontrol.dat"); 
                sw.WriteLine(@"outputs\AggregatedOutput.dat"); // this is where the UEB output will be written. no need for us to generate this file
            }

            _logger.Info(fileName + " file was created.");
        }

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

        private static void CreateSiteInitialFile(string destFiePath)
        {
            //first read the atmos pressure value from the text file
            string fileName = "ws_atom_pres.txt";
            float atomPresValue;
            string fileToReadFrom = destFiePath + @"\" + fileName;
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
            string fileToWriteTo = destFiePath + @"\" + fileName;
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine("Site and Initial Condition Input Variables");

                sw.WriteLine("USci: Energy content initial condition (kg m-3)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("0"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("WSis: Snow water equivalent initial condition (m)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("0"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("Tic: Canopy Snow Water Equivalent (m) relative to T = 0 C solid phase");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("0"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("WCic: Snow water equivalent dimensionless age initial condition (m) ");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("0"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("df: Drift factor multiplier");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("1.0"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("apr: Snow water equivalent initial condition (m)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine(atomPresValue);

                sw.WriteLine("Aep: Albedo extinction coefficient");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("0.1"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("cc: Canopy coverage fraction");
                sw.WriteLine("1"); // variable flag value for SVTC type variable
                sw.WriteLine("cc_nlcd_.nc"); //netcdf file taht contains data for cc
                sw.WriteLine("cc"); // name of the data varaible in the above netcdf file

                sw.WriteLine("hcan: Canopy height");
                sw.WriteLine("1"); // variable flag value for SVTC type variable
                sw.WriteLine("hc_nlcd_.nc"); //netcdf file taht contains data for hcan
                sw.WriteLine("hcan"); // name of the data varaible in the above netcdf file

                sw.WriteLine("lai: Leaf area index");
                sw.WriteLine("1"); // variable flag value for SVTC type variable
                sw.WriteLine("lai_nlcd_.nc"); //netcdf file taht contains data for lai
                sw.WriteLine("lai"); // name of the data varaible in the above netcdf file

                sw.WriteLine("ycage: Forest age flag for wind speed profile parameterization");
                sw.WriteLine("1"); // variable flag value for SVTC type variable
                sw.WriteLine("ycage_nlcd_.nc"); //netcdf file taht contains data for ycage
                sw.WriteLine("ycage"); // name of the data varaible in the above netcdf file

                sw.WriteLine("Sbar: Maximum snow load held per unit branch area");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("6.6"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("slope: A 2-D grid that contains the slope at each grid point");
                sw.WriteLine("1"); // variable flag value for SVTC type variable
                sw.WriteLine("slope.nc"); //netcdf file taht contains data for slope
                sw.WriteLine("slope"); // name of the data varaible in the above netcdf file

                sw.WriteLine("aspect: A 2-D grid that contains the aspect at each grid point");
                sw.WriteLine("1"); // variable flag value for SVTC type variable
                sw.WriteLine("aspect.nc"); //netcdf file taht contains data for aspect
                sw.WriteLine("aspect"); // name of the data varaible in the above netcdf file

                // There may not be lat gridded file in which case there
                // would be ws-lat.txt file that contains a single value for the whole ws grid
                // so we need to process that and write to the file in three line format with
                // variable type = 0
                fileName = "lat.nc";
                string fileToCheck = destFiePath + @"\" + fileName;
                if (File.Exists(fileToCheck))
                {
                    sw.WriteLine("latitude: A 2-D grid that contains the latitude at each grid point");
                    sw.WriteLine("1"); // variable flag value for SVTC type variable
                    sw.WriteLine("lat.nc"); //netcdf file that contains data for latitude
                    sw.WriteLine("lat"); // name of the data varaible in the above netcdf file
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
                                
                sw.WriteLine("subalb: Albedo (fraction 0-1) of the substrate beneath the snow (ground, or glacier)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("0.25"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("subtype: Type of beneath snow substrate encoded as (0 = Ground/Non Glacier, 1=Clean Ice/glacier, 2= Debris covered ice/glacier, 3= Glacier snow accumulation zone)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("0"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("gsurf: The fraction of surface melt that runs off (e.g. from a glacier)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("0"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("b01: Bristow-Campbell B for January (1)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("6.743"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("b02: Bristow-Campbell B for Feb (2)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("7.927"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("b03: Bristow-Campbell B for Mar (3)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("8.055"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("b04: Bristow-Campbell B for Apr (4)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("8.602"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("b05: Bristow-Campbell B for May (5)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("8.43"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("b06: Bristow-Campbell B for June (6)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("9.76"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("b07: Bristow-Campbell B for July (7)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("0.00"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("b08: Bristow-Campbell B for Aug (8)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("0.00"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("b09: Bristow-Campbell B for Sep (9)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("0.00"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("b10: Bristow-Campbell B for Oct (10)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("7.4"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("b11: Bristow-Campbell B for Nov (11)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("9.14"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("b12: Bristow-Campbell B for Dec (12)");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("6.67"); //default value, later need to get this value form a user uploaded siteinitial input file

                sw.WriteLine("ts_last: degree celcius");
                sw.WriteLine("0"); // variable flag value for SCTC type variable
                sw.WriteLine("-9999"); //default value, later need to get this value form a user uploaded siteinitial input file

                // there may not be lon gridded file in which case there
                // would be ws-lon.txt file that contains a single value for the whole ws grid
                // so we need to process that and write to the file in three line format with
                // variable type = 0
                fileName = "lon.nc";
                fileToCheck = destFiePath + @"\" + fileName;
                if (File.Exists(fileToCheck))
                {
                    sw.WriteLine("longitude: A 2-D grid that contains the longitude at each grid point");
                    sw.WriteLine("1"); // variable flag value for SVTC type variable
                    sw.WriteLine("lon.nc"); //netcdf file that contains data for lon
                    sw.WriteLine("lon"); // name of the data varaible in the above netcdf file
                }
                else
                {
                    fileName = "lon.txt";
                    fileToReadFrom = destFiePath + @"\" + fileName;
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
        }

        //create the inputcontrol.dat file
        private static void CreateInputControlFile(string destFiePath, DateTime startDate, DateTime endDate, byte timeStep)
        {
            //create the param.dat file            
            string fileName = "inputcontrol.dat";
            string fileToWriteTo = destFiePath + @"\" + fileName;
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine("Input Control file");
                string dateFormat = "yyyy MM dd hh:mm";                
                sw.WriteLine(startDate.ToString(dateFormat)); // yyyy mm dd hh.hh (starting Date)
                sw.WriteLine(endDate.ToString(dateFormat)); // yyyy mm dd hh.hh (ending Date)
                sw.WriteLine(timeStep); // time step
                sw.WriteLine("6.00"); // UTC time offset

                sw.WriteLine("Ta: Air temperature  (always required)");
                sw.WriteLine("1"); // variable flag value for SCTC type variable
                sw.WriteLine("Talist.dat;X:x;Y:y;time:time;D:T"); // this is the new format suggested by Tseganeh
                //sw.WriteLine("netCDFFileList.dat"); //netcdf file that the name of all time series netcdf files
                //sw.WriteLine("T"); // name of the variable as in the netcdf file

                sw.WriteLine("Prec: Precipitation  (always required)");
                sw.WriteLine("1"); // variable flag value for SCTC type variable
                sw.WriteLine("Preclist.dat;X:x;Y:y;time:time;D:Prec"); // this is the new format suggested by Tseganeh
                //sw.WriteLine("netCDFFileList.dat"); //netcdf file that the name of all time series netcdf files
                //sw.WriteLine("Prec"); // name of the variable as in the netcdf file

                sw.WriteLine("v: Wind speed (always required)");
                sw.WriteLine("1"); // variable flag value for SCTC type variable
                sw.WriteLine("Vlist.dat;X:x;Y:y;time:time;D:V"); // this is the new format suggested by Tseganeh
                //sw.WriteLine("netCDFFileList.dat"); //netcdf file that the name of all time series netcdf files
                //sw.WriteLine("V"); // name of the variable as in the netcdf file

                sw.WriteLine("RH: Relative humidity (always required)");
                sw.WriteLine("1"); // variable flag value for SCTC type variable
                sw.WriteLine("RHlist.dat;X:x;Y:y;time:time;D:rh"); // this is the new format suggested by Tseganeh
                //sw.WriteLine("netCDFFileList.dat"); //netcdf file that the name of all time series netcdf files
                //sw.WriteLine("rh"); // name of the variable as in the netcdf file

                //sw.WriteLine("Qsi: Incoming shortwave(kJ/m2/hr)   (only required if irad=1 or 2)");
                //sw.WriteLine("0"); // variable flag value for SCTC type variable
                //sw.WriteLine("IncomingShortwaveTS.dat"); //TODO: this file is required for irad=2, for this protype we are using irad=0

                //sw.WriteLine("Qli: Long wave radiation(kJ/m2/hr)   (only required if irad=1 or 2)");
                //sw.WriteLine("0"); // variable flag value for SCTC type variable
                //sw.WriteLine("LongwaveRadiationTS.dat"); //TODO: this file is required for irad=2, for this protype we are using irad=0

                //sw.WriteLine("Qnet: Net radiation(kJ/m2/hr)   (only required if irad=3)");
                //sw.WriteLine("0"); // variable flag value for SCTC type variable
                //sw.WriteLine("NetRadiationTS.dat"); //TODO: this file is required for irad=3, for this protype we are using irad=0

                //sw.WriteLine("Snowalb: Snow albedo (0-1).  (only required if ireadalb=1) The albedo of the snow surface to be used when the internal albedo calculations are to be overridden");
                //sw.WriteLine("0"); // variable flag value for SCTC type variable
                //sw.WriteLine("snoalbmaps.dat"); //TODO: this file is required for ireadalb=1, for this protype we are using ireadalb=0

                sw.WriteLine("Qg: : Ground heat flux   (kJ/m2/hr)");
                sw.WriteLine("2"); // variable flag value for SCTC type variable
                sw.WriteLine("0"); // variable value
            }

            _logger.Info(fileName + " file was created.");
        }

        //create the netCDFFileList.dat file
        private static void CreateNetCDFFileListFile(string destFiePath)
        {
            string fileName = "netCDFFileList.dat";
            string fileToWriteTo = destFiePath + @"\" + fileName;
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine("ta_daily_multiple_data.nc");
                sw.WriteLine("precp_daily_multiple_data.nc");
                sw.WriteLine("wind_daily_multiple_data.nc");
                sw.WriteLine("rh_daily_multiple_data.nc");
            }
            _logger.Info(fileName + " file was created.");

        }

        private static void CreateNetCDFTimeSeriesListFiles(string destFiePath)
        {
            string fileName = "Talist.dat";
            string fileToWriteTo = destFiePath + @"\" + fileName;
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine("ta_daily_multiple_data.nc");                
            }

            _logger.Info(fileName + " file was created.");
            
            fileName = "Preclist.dat";
            fileToWriteTo = destFiePath + @"\" + fileName;
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine("precp_daily_multiple_data.nc");                
            }

            _logger.Info(fileName + " file was created.");

            fileName = "Vlist.dat";
            fileToWriteTo = destFiePath + @"\" + fileName;
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {               
                sw.WriteLine("wind_daily_multiple_data.nc");                
            }

            _logger.Info(fileName + " file was created.");

            fileName = "RHlist.dat";
            fileToWriteTo = destFiePath + @"\" + fileName;
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine("rh_daily_multiple_data.nc");
            }

            _logger.Info(fileName + " file was created.");
        }

        //create the outputcontrol.dat file
        private static void CreateOutputControlFile(string destFiePath)
        {
            string fileName = "outputcontrol.dat";
            string fileToWriteTo = destFiePath + @"\" + fileName;
            using (StreamWriter sw = new StreamWriter(fileToWriteTo))
            {
                sw.WriteLine("List of output variables");

                // TODO: These values (varialble names should come as user inputs and thus not to be hard coded
                // need to think how such a long list of user input values would come from the client side
                // to this server side code
                sw.WriteLine("atf: Atmospheric transmission factor"); // name of the variable for which output data is needed
                sw.WriteLine("outputs" + @"\" + "atf.nc"); // output file name with path

                sw.WriteLine("hri: radiation index"); // name of the variable for which output data is needed
                sw.WriteLine("outputs" + @"\" + "hri.nc"); // output file name with path

                //there could be more variables
            }

            _logger.Info(fileName + " file was created.");
        }

        //create the aggregatedoutputcontrol.dat file
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
}