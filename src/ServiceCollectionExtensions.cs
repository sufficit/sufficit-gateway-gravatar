using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Sufficit.Gateway.Gravatar
{
    /// <summary>
    ///     Dependency-injection extensions for the Gravatar gateway.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     Registers <see cref="GravatarOptions"/> bound to "Sufficit:Gateway:Gravatar"
        ///     and a typed <see cref="IGravatarClient"/> over the shared HttpClient
        ///     named <see cref="GravatarClient.HttpClientName"/>.
        ///     Safe to call multiple times: the typed client is registered once.
        /// </summary>
        public static IServiceCollection AddSufficitGatewayGravatar(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<GravatarOptions>()
                .Bind(configuration.GetSection(GravatarOptions.SectionName));

            if (services.All(d => d.ServiceType != typeof(IGravatarClient)))
            {
                services.AddHttpClient(GravatarClient.HttpClientName)
                    .AddTypedClient<IGravatarClient, GravatarClient>();
            }

            return services;
        }
    }
}
