using System.Text.Json.Serialization;

namespace Voidwell.Common.Authentication.AuthenticatedHttpClient;

internal sealed class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
