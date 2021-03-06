using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Artsoftheinsane.Extensions.ServiceCollectionDecorator
{
    public static class ServiceCollectionDecoratorExtensions
    {
        public static void Decorate<TInterface, TDecorator>(this IServiceCollection services)
          where TInterface : class
          where TDecorator : class, TInterface
        {
            // grab the existing registration
            var wrappedDescriptor = services.FirstOrDefault(
              s => s.ServiceType == typeof(TInterface));

            if (wrappedDescriptor == null)
            {
                throw new InvalidOperationException($"{typeof(TInterface).Name} is not registered");
            }

            // create the object factory for our decorator type,
            // specifying that we will supply TInterface explicitly
            var objectFactory = ActivatorUtilities.CreateFactory(
              typeof(TDecorator),
              new[] { typeof(TInterface) });

            // replace the existing registration with one
            // that passes an instance of the existing registration
            // to the object factory for the decorator
            services.
                Replace(
                    ServiceDescriptor.Describe(
                        typeof(TInterface),
                        s => (TInterface)objectFactory(s, new[] { s.CreateInstance(wrappedDescriptor) }), wrappedDescriptor.Lifetime)
            );
        }

        private static object CreateInstance(this IServiceProvider services, ServiceDescriptor descriptor)
        {
            // this covers the scenario when you register a singleton with an instance
            // ex. service.AddSingleton<IFoo>(new Foo());
            if (descriptor.ImplementationInstance != null)
            {
                return descriptor.ImplementationInstance;
            }

            // this covers the scenario when a factory has been specified at registration
            // ex. services.AddScoped<IFoo>(s => new Foo());
            if (descriptor.ImplementationFactory != null)
            {
                return descriptor.ImplementationFactory(services);
            }

            // Basic sanity check
            if (descriptor.ImplementationType == null)
            {
                throw new ArgumentNullException($"{nameof(descriptor.ImplementationType)}");
            }

            return ActivatorUtilities.GetServiceOrCreateInstance(services, descriptor.ImplementationType);
        }
    }
}