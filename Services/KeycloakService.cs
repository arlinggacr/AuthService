using System.Net.Http.Headers;
using System.Threading.Tasks;
using AuthService.Models;
using Newtonsoft.Json;

namespace AuthService.Services
{
    public class KeycloakService
    {
        private readonly HttpClient _httpClient;
        private readonly string _realm;
        private readonly string _adminUsername;
        private readonly string _adminPassword;

        public KeycloakService(
            string keycloakUrl,
            string realm,
            string adminUsername,
            string adminPassword
        )
        {
            _realm = realm;
            _adminUsername = adminUsername;
            _adminPassword = adminPassword;

            _httpClient = new HttpClient { BaseAddress = new Uri(keycloakUrl) };
        }

        private async Task<string?> GetAccessTokenAsync()
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"realms/loc-realm/protocol/openid-connect/token"
                )
                {
                    Content = new FormUrlEncodedContent(
                        new[]
                        {
                            new KeyValuePair<string, string>("grant_type", "password"),
                            new KeyValuePair<string, string>("client_id", "acr212"),
                            new KeyValuePair<string, string>(
                                "client_secret",
                                "ZVJdgKpCqHrsFnX0Oia5oaYKjdykxyLc"
                            ),
                            new KeyValuePair<string, string>("username", _adminUsername),
                            new KeyValuePair<string, string>("password", _adminPassword),
                        }
                    )
                };

                var response = await _httpClient.SendAsync(request);

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Token response content: {content}");

                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(content);
                return tokenResponse?.AccessToken;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP request error while getting access token: {ex.Message}");
                throw; // Re-throw to let the caller handle it
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while getting access token: {ex.Message}");
                throw;
            }
        }

        public async Task<string?> LoginAsync(string username, string password)
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"realms/loc-realm/protocol/openid-connect/token"
                )
                {
                    Content = new FormUrlEncodedContent(
                        new[]
                        {
                            new KeyValuePair<string, string>("grant_type", "password"),
                            new KeyValuePair<string, string>("client_id", "acr212"),
                            new KeyValuePair<string, string>(
                                "client_secret",
                                "ZVJdgKpCqHrsFnX0Oia5oaYKjdykxyLc"
                            ),
                            new KeyValuePair<string, string>("username", username),
                            new KeyValuePair<string, string>("password", password),
                        }
                    )
                };

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(content);
                return tokenResponse?.AccessToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateUserAsync(string username, string email, string password)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                if (accessToken == null)
                {
                    Console.WriteLine("Access token is null. Unable to create user.");
                    return false;
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken
                );

                var keycloakUser = new
                {
                    username,
                    email,
                    enabled = true,
                    credentials = new[]
                    {
                        new
                        {
                            type = "password",
                            value = password,
                            temporary = false
                        }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"admin/realms/loc-realm/users",
                    keycloakUser
                );

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while creating user: {ex.Message}");
                return false;
            }
        }

        public async Task<List<UserDto>> GetUsersAsync()
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();

                if (accessToken == null)
                {
                    Console.WriteLine("Access token is null. Unable to retrieve users.");
                    return new List<UserDto>();
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken
                );

                var response = await _httpClient.GetAsync("admin/realms/loc-realm/users");

                var contentString = await response.Content.ReadAsStringAsync();

                // Console.WriteLine(response);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine(
                        $"Failed to retrieve users. StatusCode: {response.StatusCode}, Content: {contentString}"
                    );
                    return new List<UserDto>();
                }

                // try
                // {
                var users = JsonConvert.DeserializeObject<List<UserDto>>(contentString);
                return users ?? new List<UserDto>();
                // }
                // catch (JsonSerializationException)
                // {
                //     Console.WriteLine("Failed to deserialize JSON response.");
                //     return new List<UserDto>();
                // }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while retrieving users: {ex.Message}");
                return new List<UserDto>();
            }
        }

        public void SetAccessToken(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken
            );
        }
    }
}
