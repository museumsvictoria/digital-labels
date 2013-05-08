using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using AttributeRouting;
using AttributeRouting.Web.Http;
using DigitalLabels.Core.DomainModels;
using DigitalLabels.Core.Indexes;
using DigitalLabels.WebApi.Infrastructure;
using Raven.Client;
using Raven.Client.Linq;
using DigitalLabels.Core.Extensions;

namespace DigitalLabels.WebApi.Controllers.Apis
{
    [RoutePrefix("api/generations")]
    public class GenerationsController : ApiController
    {
        private readonly IDocumentSession _documentSession;

        public GenerationsController(
            IDocumentSession documentSession)
        {
            _documentSession = documentSession;
        }

        [GET("")]
        [ApiDoc("Returns the entire set of Generations Labels.")]
        public IEnumerable<GenerationsLabel> GetAll()
        {
            // TODO: add paging and remove allresults when working on digital labels again.

            return _documentSession
                .Query<GenerationsLabel, GenerationsLabel_All>()
                .GetAllResultsWithPaging()
                .ToList();
        }

        [GET("{irn:long}")]
        [ApiDoc("Return a single Generations Label by primary image Irn.")]
        [ApiParameterDoc("irn", "The Irn of the primary image. [integer]")]
        public GenerationsLabel GetByIrn(long irn)
        {
            return _documentSession
                .Load<GenerationsLabel>(irn);
        }

        [GET("theme")]
        [ApiDoc("Returns all of the Generations themes.")]
        public IEnumerable<string> GetAllThemes()
        {
            var stuff = _documentSession
                .Query<GenerationsLabel, GenerationsLabel_All>()
                .GetAllResultsWithPaging()
                .Select(x => x.Theme)
                .Distinct()
                .ToList();

            return stuff;
        }

        [GET("theme/{theme}")]
        [ApiDoc("Returns a collection of Generations Labels by theme.")]
        [ApiParameterDoc("theme", "The name of the theme. [string]")]
        public IEnumerable<GenerationsLabel> GetByTheme(string theme)
        {
            return _documentSession
                .Query<GenerationsLabel, GenerationsLabel_ByTheme>()
                .Where(x => x.Theme == theme.ToLower())
                .GetAllResultsWithPaging()
                .ToList();
        }
    }
}