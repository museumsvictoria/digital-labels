using System.Web.Http;
using System.Web.Mvc;
using AttributeRouting.Web.Mvc;
using DigitalLabels.WebApi.ViewModels;
using Raven.Client;

namespace DigitalLabels.WebApi.Controllers
{
    public class KioskController : Controller
    {
        [GET("standingstrong")]
        public ActionResult StandingStrong()
        {
            return View();
        }
    }
}