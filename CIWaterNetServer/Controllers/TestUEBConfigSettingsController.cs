using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using UWRL.CIWaterNetServer.UEB;

namespace UWRL.CIWaterNetServer.Controllers
{
    public class TestUEBConfigSettingsController : ApiController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private HttpResponseMessage GetUEBConfigSettings()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            string uebSettings = string.Empty;
            uebSettings += "working dir path:" + UEBSettings.WORKING_DIR_PATH + "\n";
            uebSettings += "python script dir path:" + UEBSettings.PYTHON_SCRIPT_DIR_PATH;

            response.Content = new StringContent(uebSettings);
            response.StatusCode = HttpStatusCode.OK;
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");
            return response;
        }
    }
}
