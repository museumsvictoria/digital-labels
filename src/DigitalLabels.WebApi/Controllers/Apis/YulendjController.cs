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
    [RoutePrefix("api/yulendj")]
    public class YulendjController : ApiController
    {
        private readonly IDocumentSession _documentSession;

        public YulendjController(
            IDocumentSession documentSession)
        {
            _documentSession = documentSession;
        }

        [GET("")]
        [ApiDoc("Returns the entire set of Yulendj Labels.")]
        public IEnumerable<YulendjLabel> GetAll()
        {
            // TODO: add paging and remove allresults when working on digital labels again.

            return _documentSession
                .Query<YulendjLabel, YulendjLabel_All>()
                .GetAllResultsWithPaging()
                .ToList();
        }

        [GET("{irn:long}")]
        [ApiDoc("Return a single Yulendj Label by primary image Irn.")]
        [ApiParameterDoc("irn", "The Irn of the primary image. [integer]")]
        public YulendjLabel GetByIrn(long irn)
        {
            return _documentSession
                .Load<YulendjLabel>(irn);
        }
    }
}