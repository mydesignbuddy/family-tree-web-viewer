using FTDNA.Services.FamilyTreeV3.Data.Internal.Store;
using Microsoft.Extensions.DependencyInjection;

namespace FTDNA.Services.FamilyTreeV3.Data.Internal;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<FamilyTreeStore>();
    }
}
