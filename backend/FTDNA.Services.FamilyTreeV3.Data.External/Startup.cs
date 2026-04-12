using FTDNA.Services.FamilyTreeV3.Data.External.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FTDNA.Services.FamilyTreeV3.Data.External;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient<WikiTreeApiClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false // We manage cookies manually per-session
            });

        services.AddSingleton<WikiTreeSessionStore>();
    }
}
