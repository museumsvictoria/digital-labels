﻿using Ninject.Activation;
using Raven.Client;

namespace DigitalLabels.WebApi.Infrastructure
{
    public class NinjectRavenSessionProvider : Provider<IDocumentSession>
    {
        private readonly IDocumentStore _documentStore;

        public NinjectRavenSessionProvider(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        protected override IDocumentSession CreateInstance(IContext context)
        {
            return _documentStore.OpenSession();
        }
    }
}