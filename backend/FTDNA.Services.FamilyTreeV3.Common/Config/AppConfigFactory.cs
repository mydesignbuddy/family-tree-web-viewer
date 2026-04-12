using Microsoft.Extensions.Configuration;

namespace FTDNA.Services.FamilyTreeV3.Common.Config;

public static class AppConfigFactory
{
    public static AppConfig Build(IConfiguration configuration)
    {
        var config = new AppConfig();
        configuration.GetSection("App").Bind(config);
        return config;
    }
}
