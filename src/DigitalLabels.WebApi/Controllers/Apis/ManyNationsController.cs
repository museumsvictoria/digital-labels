using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using AttributeRouting;
using AttributeRouting.Web.Http;
using DigitalLabels.Core.DomainModels;
using DigitalLabels.Core.Indexes;
using DigitalLabels.WebApi.Infrastructure;
using Raven.Client;
using DigitalLabels.Core.Extensions;

namespace DigitalLabels.WebApi.Controllers.Apis
{
    [RoutePrefix("api/manynations")]
    public class ManyNationsController : ApiController
    {
        private readonly IDocumentSession _documentSession;

        public ManyNationsController(
            IDocumentSession documentSession)
        {
            _documentSession = documentSession;
        }

        [GET("")]
        [ApiDoc("Returns the entire set of Many Nations Labels.")]
        public IEnumerable<ManyNationsLabel> GetAll()
        {
            // TODO: add paging and remove allresults when working on digital labels again.

            return _documentSession
                .Query<ManyNationsLabel, ManyNationsLabel_All>()
                .GetAllResultsWithPaging()
                .ToList();
        }

        [GET("{irn:long}")]
        [ApiDoc("Return a single Many Nations Label by Irn.")]
        [ApiParameterDoc("irn", "The Irn of the Label. [integer]")]
        public ManyNationsLabel GetByIrn(long irn)
        {
            return _documentSession
                .Load<ManyNationsLabel>(irn);
        }
    }
}