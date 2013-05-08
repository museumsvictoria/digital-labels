using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DigitalLabels.Core.DomainModels;
using Raven.Client;

namespace DigitalLabels.Core.Config
{
    public class ApplicationManager : IApplicationManager
    {
        private readonly IDocumentSession _documentSession;

        private Application _application;

        public ApplicationManager(
            IDocumentSession documentSession)
        {
            _documentSession = documentSession;
        }

        public void SetupApplication()
        {
            _application = _documentSession.Load<Application>(Constants.ApplicationId);

            // Initial Setup
            if (_application == null)
            {
                AddApplication();
            }
            else
            {
                if (_application.DataImportRunning)
                    _application.DataImportFinished();
            }

            _documentSession.SaveChanges();
        }

        private void AddApplication()
        {
            _application = new Application();
            _documentSession.Store(_application);
        }
    }
}
