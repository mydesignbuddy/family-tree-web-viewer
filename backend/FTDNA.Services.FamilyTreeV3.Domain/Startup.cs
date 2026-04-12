using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using DataInternal = FTDNA.Services.FamilyTreeV3.Data.Internal;
using DataExternal = FTDNA.Services.FamilyTreeV3.Data.External;

namespace FTDNA.Services.FamilyTreeV3.Domain;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        DataInternal.Startup.ConfigureServices(services);
        DataExternal.Startup.ConfigureServices(services);
    }

    public static void ConfigureWolverine(WolverineOptions options)
    {
        options.Discovery.IncludeAssembly(typeof(DataInternal.Startup).Assembly);
        options.Discovery.IncludeAssembly(typeof(Startup).Assembly);
    }
}
