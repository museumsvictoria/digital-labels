using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Description;

namespace DigitalLabels.WebApi.Infrastructure
{
    public class ApiDocumentationProvider : IDocumentationProvider
    {       
        public string GetDocumentation(HttpParameterDescriptor parameterDescriptor)
        {
            string doc = "";
            
            var attr = parameterDescriptor
                .ActionDescriptor
                .GetCustomAttributes<ApiParameterDocAttribute>()
                .FirstOrDefault(p => p.Parameter == parameterDescriptor.ParameterName);

            if (attr != null)
            {
                doc = attr.Documentation;
            }

            return doc;
        }

        public string GetDocumentation(HttpActionDescriptor actionDescriptor)
        {
            string doc = "";

            var attr = actionDescriptor
                .GetCustomAttributes<ApiDocAttribute>()
                .FirstOrDefault();

            if (attr != null)
            {
                doc = attr.Documentation;
            }

            return doc;
        }
    }
}