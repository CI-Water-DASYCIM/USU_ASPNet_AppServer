using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace UWRL.CIWaterNetServer.Controllers
{
    /// <summary>
    /// Any client that needs to use the
    /// service to create a UEB model package can call this service to find out the json format of the 
    /// input query string needed for the service to create UEB model package.
    /// </summary>
    public class GetUEBPackageCreateRequestJSONFormatController : ApiController
    {
        public HttpResponseMessage GetUEBPkgRequestJSONFormat()
        {
            HttpResponseMessage response = new HttpResponseMessage();

            UEB.UEBPackageRequest pkRequest = new UEB.UEBPackageRequest();
            //pkRequest.OutletLocation = new UEB.OutletLocation(); 
            //pkRequest.ModelParameters = new UEB.ModelParameters(); 
                                            
            pkRequest.SiteInitialConditions = new UEB.SiteInitialConditions();
            pkRequest.BristowCambellBValues = new UEB.BristowCambellBValues();
            pkRequest.TimeSeriesInputs = new UEB.TimeSeriesInputs();
            //pkRequest.OutputVariables = new UEB.OutputVariables();
            //pkRequest.AggregatedOutputVariables = new UEB.AggregatedOutputVariables();

            string pkgReqInjsonFormat = JsonConvert.SerializeObject(pkRequest);
            //pkRequest = null;
            //pkRequest = JsonConvert.DeserializeObject<UEB.UEBPackageRequest>(pkgReqInjsonFormat);
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(pkgReqInjsonFormat);
            return response;
        }
    }
}
