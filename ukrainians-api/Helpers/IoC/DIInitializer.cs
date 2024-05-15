using Ukrainians.WebAPI.Configs;
using Ukrainians.Domain.Core.Configs;
using Ukrainians.Infrastructure.Business.Configs;
using Ukrainians.Infrastrusture.Data.Configs;

namespace Ukrainians.WebAPI.Helpers.IoC
{
    public static class DIInitializer
    {
        public static void RegisterAllinjections(this IServiceCollection services)
        {
            services.RegisterWebAPIInjections();
            services.RegisterDomainInjections();
            services.RegisterInfrastructureBusinessInjections();
            services.RegisterInfrastructureDataInjections();
        }
    }
}
