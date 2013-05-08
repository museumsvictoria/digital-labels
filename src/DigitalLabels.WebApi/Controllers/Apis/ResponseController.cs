using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace DigitalLabels.WebApi.Controllers.Apis
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ResponseController : ApiController
    {
        [HttpGet, HttpPost, HttpPut, HttpDelete]
        public HttpResponseMessage NoResourceFound()
        {
            return Request.CreateErrorResponse(
                HttpStatusCode.NotFound,
                string.Format("No HTTP resource was found that matches the request URI '{0}'", Request.RequestUri));
        }
    }
}