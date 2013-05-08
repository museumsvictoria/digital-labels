using System;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using AttributeRouting.Web.Mvc;
using DigitalLabels.Core.Config;
using DigitalLabels.Core.DomainModels;
using DigitalLabels.WebApi.Config;
using DigitalLabels.WebApi.Infrastructure;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Ninject;
using Ninject.Web.Common;
using Ninject.Extensions.Conventions;
using NinjectAdapter;
using Raven.Client;
using NinjectDependencyResolver = DigitalLabels.WebApi.Infrastructure.NinjectDependencyResolver;

[assembly: WebActivator.PreApplicationStartMethod(typeof(DigitalLabels.WebApi.WebsiteBootstrapper), "PreStart")]
[assembly: WebActivator.PostApplicationStartMethod(typeof(DigitalLabels.WebApi.WebsiteBootstrapper), "PostStart")]
[assembly: WebActivator.ApplicationShutdownMethodAttribute(typeof(DigitalLabels.WebApi.WebsiteBootstrapper), "Stop")]

namespace DigitalLabels.WebApi
{
    public static class WebsiteBootstrapper
    {
        private static readonly Bootstrapper bootstrapper = new Bootstrapper();
        private static IKernel _kernal;

        /// <summary>
        /// Starts the application
        /// </summary>
        public static void PreStart()
        {
            DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
            DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
            bootstrapper.Initialize(CreateKernel);

            // Register Web Routes
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        /// <summary>
        /// Stops the application.
        /// </summary>
        public static void Stop()
        {
            bootstrapper.ShutDown();
        }

        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        private static IKernel CreateKernel()
        {
            _kernal = new StandardKernel();

            _kernal.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
            _kernal.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

            RegisterServices(_kernal);

            // Register WebAPI dependency resolver
            GlobalConfiguration.Configuration.DependencyResolver = new NinjectDependencyResolver(_kernal);

            return _kernal;
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(IKernel kernel)
        {
            // Raven Db Bindings
            kernel.Bind<IDocumentStore>().ToProvider<NinjectRavenDocumentStoreProvider>().InSingletonScope();
            kernel.Bind<IDocumentSession>().ToProvider<NinjectRavenSessionProvider>();

            // The rest of our bindings
            kernel.Bind(x => x
                .FromAssemblyContaining(typeof(DomainModel), typeof(WebsiteBootstrapper))
                .SelectAllClasses()
                .BindAllInterfaces());

            kernel.Bind<IServiceLocator>().ToMethod(x => ServiceLocator.Current);

            ServiceLocator.SetLocatorProvider(() => new NinjectServiceLocator(kernel));
        }

        public static void PostStart()
        {
            AreaRegistration.RegisterAllAreas();

            // Register Global Filters
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);            

            // Configure Web Api
            WebApiConfig.Configure();

            // Perform Application setup
            ServiceLocator.Current.GetInstance<IApplicationManager>().SetupApplication();
        }
    }
}