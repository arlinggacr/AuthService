using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AuthService.Models;

[Keyless]
public class TokenResponse
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public int RefreshExpiresIn { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string SessionState { get; set; } = string.Empty;
}
