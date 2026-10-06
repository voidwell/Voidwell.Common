using System.ComponentModel.DataAnnotations;

namespace Voidwell.Common.Authentication.AuthenticatedHttpClient;

/// <summary>
/// Client-credentials settings used to obtain bearer tokens for an authenticated <see cref="HttpClient"/>.
/// </summary>
public class AuthenticatedHttpClientOptions
{
    /// <summary>
    /// The OAuth2 token endpoint.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "The TokenServiceAddress configuration value is required.")]
    public string TokenServiceAddress { get; set; } = string.Empty;

    /// <summary>
    /// The OAuth2 client id.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "The ClientId configuration value is required.")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The OAuth2 client secret.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "The ClientSecret configuration value is required.")]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The scopes to request.
    /// </summary>
    public List<string> ClientScopes { get; set; } = [];
}
