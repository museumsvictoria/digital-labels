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
    [RoutePrefix("api/standingstrong")]
    public class StandingStrongController : ApiController
    {
        private readonly IDocumentSession _documentSession;

        public StandingStrongController(
            IDocumentSession documentSession)
        {
            _documentSession = documentSession;
        }

        [GET("")]
        [ApiDoc("Returns the entire set of Standing Strong labels.")]
        public IEnumerable<StandingStrongLabel> GetAll()
        {
            // TODO: add paging and remove allresults when working on digital labels again.

            return _documentSession
                .Query<StandingStrongLabel, StandingStrongLabel_All>()
                .GetAllResultsWithPaging()
                .OrderBy(x => x.Order)
                .ToList();
        }

        [GET("{irn:long}")]
        [ApiDoc("Return a single Standing Strong Label by primary image Irn.")]
        [ApiParameterDoc("irn", "The Irn of the label. [integer]")]
        public StandingStrongLabel GetByIrn(long irn)
        {
            return _documentSession
                .Load<StandingStrongLabel>(irn);
        }
    }
}