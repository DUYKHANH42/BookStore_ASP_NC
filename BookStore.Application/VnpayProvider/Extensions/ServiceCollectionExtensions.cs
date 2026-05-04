using Microsoft.Extensions.DependencyInjection;
using System;
using BookStore.Application.VnpayProvider.Extensions.Options;

namespace BookStore.Application.VnpayProvider.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddVnpayClient(this IServiceCollection services, Action<VnpayConfiguration> config)
        {
            services.Configure(config);
            services.AddScoped<IVnpayClient, VnpayClient>();
            return services;
        }
    }
}
