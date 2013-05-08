using System.Web.Http;
using System.Web.Mvc;
using AttributeRouting.Web.Mvc;
using DigitalLabels.WebApi.ViewModels;
using Raven.Client;

namespace DigitalLabels.WebApi.Controllers
{
    public class HomeController : Controller
    {
        [GET("")]
        public ActionResult Help()
        {
            return View(new HelpViewModel(GlobalConfiguration.Configuration.Services.GetApiExplorer()));
        }
    }
}