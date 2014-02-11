using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace UWRL.CIWaterNetServer.Controllers
{
    public class TestCallFromDrupalController : ApiController
    {
        public HttpResponseMessage GetDrupalRequest(string uebPkgRequestNodeID)
        {
            HttpResponseMessage response = new HttpResponseMessage();

            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(string.Format("Response from CIWater App server: Recieved UEB Package build request Node ID:{0}.", uebPkgRequestNodeID));
            return response;

        }        
    }
}
