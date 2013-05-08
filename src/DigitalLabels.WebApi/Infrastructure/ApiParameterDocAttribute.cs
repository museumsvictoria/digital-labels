using System;

namespace DigitalLabels.WebApi.Infrastructure
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ApiParameterDocAttribute : Attribute
    {
        public ApiParameterDocAttribute(string param, string doc)
        {
            Parameter = param;
            Documentation = doc;
        }
        public string Parameter { get; set; }
        public string Documentation { get; set; }
    }
}