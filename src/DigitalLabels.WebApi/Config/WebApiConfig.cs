using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.Description;
using DigitalLabels.WebApi.Infrastructure;

namespace DigitalLabels.WebApi.Config
{
    public static class WebApiConfig
    {
        public static void Configure()
        {
            // Detailed WebAPI error messages 
            GlobalConfiguration.Configuration.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;

            // Register documentation provider
            GlobalConfiguration.Configuration.Services.Replace(typeof(IDocumentationProvider), new ApiDocumentationProvider());
            
            // Register alternate ways of controlling media response 
            GlobalConfiguration.Configuration.Formatters.JsonFormatter.MediaTypeMappings.Add(new QueryStringMapping("json", "true", "application/json"));
            GlobalConfiguration.Configuration.Formatters.XmlFormatter.MediaTypeMappings.Add(new QueryStringMapping("xml", "true", "application/xml"));
        }
    }
}