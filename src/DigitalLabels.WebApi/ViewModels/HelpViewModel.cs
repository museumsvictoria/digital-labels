using System.Linq;
using System.Web.Http.Description;

namespace DigitalLabels.WebApi.ViewModels
{
    public class HelpViewModel
    {
        public ILookup<string, ApiDescription> Apis { get; set; }

        public string Method { get; set; }

        public HelpViewModel(IApiExplorer apiExplorer)
        {
            Apis = apiExplorer.ApiDescriptions.ToLookup(x => x.ActionDescriptor.ControllerDescriptor.ControllerName);
        }
    }
}