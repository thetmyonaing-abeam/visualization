namespace Visualization.API.Services;

public interface IPowerBIService
{
    Task<PowerBIEmbedConfig> GetEmbedConfigAsync(string reportId);
    PowerBISettings GetSettings();
}

public class PowerBIEmbedConfig
{
    public string ReportId { get; set; } = string.Empty;
    public string EmbedUrl { get; set; } = string.Empty;
    public string EmbedToken { get; set; } = string.Empty;
    public DateTime TokenExpiry { get; set; }
}

public class PowerBISettings
{
    public string WorkspaceId { get; set; } = string.Empty;
    public string GeospatialReportId { get; set; } = string.Empty;
    public string TemporalReportId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AuthorityUrl { get; set; } = "https://login.microsoftonline.com/";
    public string ResourceUrl { get; set; } = "https://analysis.windows.net/powerbi/api";
    public string ApiUrl { get; set; } = "https://api.powerbi.com/";
}

public class PowerBIService : IPowerBIService
{
    private readonly PowerBISettings _settings;
    private readonly ILogger<PowerBIService> _logger;
    private readonly HttpClient _httpClient;

    public PowerBIService(IConfiguration configuration, ILogger<PowerBIService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("PowerBI");
        _settings = new PowerBISettings();
        configuration.GetSection("PowerBI").Bind(_settings);
    }

    public PowerBISettings GetSettings()
    {
        return _settings;
    }

    public async Task<PowerBIEmbedConfig> GetEmbedConfigAsync(string reportId)
    {
        // In production, this would authenticate with Azure AD and get an embed token
        // For now, return a placeholder configuration
        _logger.LogInformation("Getting Power BI embed config for report {ReportId}", reportId);

        try
        {
            // Step 1: Get Azure AD token (placeholder - requires actual Azure AD app registration)
            var accessToken = await GetAzureAdTokenAsync();

            if (string.IsNullOrEmpty(accessToken))
            {
                // Return demo config when not configured
                return new PowerBIEmbedConfig
                {
                    ReportId = reportId,
                    EmbedUrl = $"https://app.powerbi.com/reportEmbed?reportId={reportId}&groupId={_settings.WorkspaceId}",
                    EmbedToken = "demo-token-configure-azure-ad",
                    TokenExpiry = DateTime.UtcNow.AddHours(1)
                };
            }

            // Step 2: Generate embed token using Power BI REST API
            var embedToken = await GenerateEmbedTokenAsync(reportId, accessToken);

            return new PowerBIEmbedConfig
            {
                ReportId = reportId,
                EmbedUrl = $"https://app.powerbi.com/reportEmbed?reportId={reportId}&groupId={_settings.WorkspaceId}",
                EmbedToken = embedToken,
                TokenExpiry = DateTime.UtcNow.AddHours(1)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Power BI embed config");
            return new PowerBIEmbedConfig
            {
                ReportId = reportId,
                EmbedUrl = "",
                EmbedToken = "",
                TokenExpiry = DateTime.UtcNow
            };
        }
    }

    private async Task<string?> GetAzureAdTokenAsync()
    {
        if (string.IsNullOrEmpty(_settings.TenantId) || string.IsNullOrEmpty(_settings.ClientId))
        {
            _logger.LogWarning("Power BI Azure AD credentials not configured");
            return null;
        }

        var tokenEndpoint = $"{_settings.AuthorityUrl}{_settings.TenantId}/oauth2/v2.0/token";

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _settings.ClientId),
            new KeyValuePair<string, string>("client_secret", _settings.ClientSecret),
            new KeyValuePair<string, string>("scope", $"{_settings.ResourceUrl}/.default")
        });

        var response = await _httpClient.PostAsync(tokenEndpoint, content);
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            // Parse access_token from response
            var tokenDoc = System.Text.Json.JsonDocument.Parse(json);
            return tokenDoc.RootElement.GetProperty("access_token").GetString();
        }

        return null;
    }

    private async Task<string> GenerateEmbedTokenAsync(string reportId, string accessToken)
    {
        var url = $"{_settings.ApiUrl}v1.0/myorg/groups/{_settings.WorkspaceId}/reports/{reportId}/GenerateToken";

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(
            "{\"accessLevel\":\"View\"}",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var tokenDoc = System.Text.Json.JsonDocument.Parse(json);
            return tokenDoc.RootElement.GetProperty("token").GetString() ?? "";
        }

        return "";
    }
}
