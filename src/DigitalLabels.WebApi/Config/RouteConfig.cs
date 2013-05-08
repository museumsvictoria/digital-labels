using System.Web.Http;
using System.Web.Routing;
using AttributeRouting.Web.Http.WebHost;
using AttributeRouting.Web.Mvc;

namespace DigitalLabels.WebApi.Config
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapAttributeRoutes();

            GlobalConfiguration.Configuration.Routes.MapHttpAttributeRoutes();
        }
    }
}