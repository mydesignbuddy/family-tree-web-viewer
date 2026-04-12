namespace FTDNA.Services.FamilyTreeV3.Common.Config;

public class AppConfig
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public long MaxUploadSizeBytes { get; set; } = 50 * 1024 * 1024;
}
