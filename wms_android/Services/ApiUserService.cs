using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using wms_android.shared.Interfaces;
using wms_android.shared.Models;
using wms_android.shared.Models.Auth;
using System.Diagnostics;

namespace wms_android.Services
{
    /// <summary>
    /// Client-side implementation of IUserService that interacts with the Web API.
    /// </summary>
    public class ApiUserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _serializerOptions;

        public ApiUserService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Assuming API uses camelCase
            };
            Debug.WriteLine($"ApiUserService initialized with HttpClient BaseAddress: {_httpClient.BaseAddress}");
        }

        // Note: API endpoints assumed based on REST conventions. Verify with actual API implementation.

        public async Task<User> AuthenticateAsync(string username, string password)
        {
            try
            {
                Debug.WriteLine($"ApiUserService: Verifying password for {username} via API at {_httpClient.BaseAddress}api/auth/verify-password");
                var credentials = new { Username = username, Password = password, ClientType = "Android" };
                var response = await _httpClient.PostAsJsonAsync("api/auth/verify-password", credentials);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"ApiUserService: Login response content: {content}");
                    
                    // Parse the login response which has structure: { Token, Username, Role, UserId }
                    var loginResponse = JsonSerializer.Deserialize<JsonElement>(content, _serializerOptions);
                    
                    if (loginResponse.TryGetProperty("userId", out var userIdElement) && 
                        loginResponse.TryGetProperty("username", out var usernameElement) &&
                        loginResponse.TryGetProperty("role", out var roleElement))
                    {
                        // Create a User object from the login response
                        var user = new User
                        {
                            Id = userIdElement.GetInt32(),
                            Username = usernameElement.GetString() ?? "",
                            Role = new Role
                            {
                                Name = roleElement.GetString() ?? ""
                            }
                        };
                        
                        Debug.WriteLine($"ApiUserService: Authentication successful for {username}, UserId: {user.Id}");
                        return user;
                    }
                    else
                    {
                        Debug.WriteLine($"ApiUserService: Unexpected login response format: {content}");
                        return null;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"ApiUserService: Authentication failed. Status: {response.StatusCode}, Reason: {response.ReasonPhrase}, Content: {errorContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiUserService: Exception during AuthenticateAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            try
            {
                Debug.WriteLine($"ApiUserService: Getting user by ID {id} from {_httpClient.BaseAddress}api/users/{id}");
                var response = await _httpClient.GetAsync($"api/users/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<User>(_serializerOptions);
                }
                 Debug.WriteLine($"ApiUserService: GetUserByIdAsync failed. Status: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                return null;
            }
            catch (Exception ex)
            {
                 Debug.WriteLine($"ApiUserService: Exception during GetUserByIdAsync: {ex.Message}");
                 return null;
            }
        }

        public async Task<IEnumerable<User>> GetUsersAsync()
        {
             try
            {
                Debug.WriteLine($"ApiUserService: Getting all users from {_httpClient.BaseAddress}api/users");
                var response = await _httpClient.GetAsync("api/users");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<IEnumerable<User>>(_serializerOptions);
                }
                Debug.WriteLine($"ApiUserService: GetUsersAsync failed. Status: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                return Enumerable.Empty<User>();
            }
            catch (Exception ex)
            {
                 Debug.WriteLine($"ApiUserService: Exception during GetUsersAsync: {ex.Message}");
                 return Enumerable.Empty<User>();
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                Debug.WriteLine($"ApiUserService: Getting all users from {_httpClient.BaseAddress}api/users");
                Debug.WriteLine($"ApiUserService: HttpClient timeout: {_httpClient.Timeout}");
                Debug.WriteLine($"ApiUserService: Request headers: {string.Join(", ", _httpClient.DefaultRequestHeaders.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
                
                var response = await _httpClient.GetAsync("api/users");
                
                Debug.WriteLine($"ApiUserService: Response status: {response.StatusCode}");
                Debug.WriteLine($"ApiUserService: Response reason: {response.ReasonPhrase}");
                Debug.WriteLine($"ApiUserService: Response headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
                
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"ApiUserService: Raw response content length: {content?.Length ?? 0}");
                    Debug.WriteLine($"ApiUserService: Raw response content: {content}");
                    
                    if (string.IsNullOrEmpty(content))
                    {
                        Debug.WriteLine("ApiUserService: Empty response content");
                        return Enumerable.Empty<User>();
                    }
                    
                    // Handle .NET reference handling format: {"$id":"1","$values":[...]}
                    JsonElement[] dynamicUsers;
                    if (content.TrimStart().StartsWith("{") && content.Contains("$values"))
                    {
                        Debug.WriteLine("ApiUserService: Detected .NET reference handling format");
                        var wrapper = JsonSerializer.Deserialize<JsonElement>(content, _serializerOptions);
                        if (wrapper.TryGetProperty("$values", out var valuesElement))
                        {
                            dynamicUsers = JsonSerializer.Deserialize<JsonElement[]>(valuesElement.GetRawText(), _serializerOptions);
                            Debug.WriteLine($"ApiUserService: Extracted {dynamicUsers?.Length ?? 0} users from $values array");
                        }
                        else
                        {
                            Debug.WriteLine("ApiUserService: No $values property found in wrapper object");
                            return Enumerable.Empty<User>();
                        }
                    }
                    else if (content.TrimStart().StartsWith("["))
                    {
                        Debug.WriteLine("ApiUserService: Standard array format detected");
                        dynamicUsers = JsonSerializer.Deserialize<JsonElement[]>(content, _serializerOptions);
                    }
                    else
                    {
                        Debug.WriteLine($"ApiUserService: Unexpected JSON format, content starts with: {content.Substring(0, Math.Min(100, content.Length))}");
                        return Enumerable.Empty<User>();
                    }
                    Debug.WriteLine($"ApiUserService: Deserialized {dynamicUsers?.Length ?? 0} users");
                    
                    var users = new List<User>();
                    
                    if (dynamicUsers != null)
                    {
                        foreach (var dynamicUser in dynamicUsers)
                        {
                            try
                            {
                                var user = new User
                                {
                                    Id = dynamicUser.GetProperty("id").GetInt32(),
                                    Username = dynamicUser.GetProperty("username").GetString() ?? "",
                                    Email = dynamicUser.GetProperty("email").GetString() ?? "",
                                    FirstName = dynamicUser.TryGetProperty("firstName", out var firstName) ? firstName.GetString() : null,
                                    LastName = dynamicUser.TryGetProperty("lastName", out var lastName) ? lastName.GetString() : null,
                                    CreatedAt = dynamicUser.GetProperty("createdAt").GetDateTime()
                                };
                                
                                // Handle the nested Role object
                                if (dynamicUser.TryGetProperty("role", out var roleElement) && roleElement.ValueKind != JsonValueKind.Null)
                                {
                                    user.Role = new Role
                                    {
                                        Id = roleElement.GetProperty("id").GetInt32(),
                                        Name = roleElement.GetProperty("name").GetString() ?? "",
                                        Description = roleElement.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                                        CanConfirm = roleElement.TryGetProperty("canConfirm", out var roleCanConfirm) && roleCanConfirm.GetBoolean(),
                                        CanDispatch = roleElement.TryGetProperty("canDispatch", out var roleCanDispatch) && roleCanDispatch.GetBoolean()
                                    };
                                    user.RoleId = user.Role.Id;
                                }
                                
                                if (dynamicUser.TryGetProperty("canConfirm", out var userCanConfirm))
                                    user.CanConfirm = userCanConfirm.GetBoolean();
                                if (dynamicUser.TryGetProperty("canDispatch", out var userCanDispatch))
                                    user.CanDispatch = userCanDispatch.GetBoolean();
                                
                                users.Add(user);
                                Debug.WriteLine($"Parsed user: {user.Username}, Role: {user.Role?.Name ?? "null"}");
                            }
                            catch (Exception userEx)
                            {
                                Debug.WriteLine($"Error parsing individual user: {userEx.Message}");
                            }
                        }
                    }
                    
                    Debug.WriteLine($"ApiUserService: Successfully parsed {users.Count} users");
                    return users;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"ApiUserService: GetAllUsersAsync failed. Status: {response.StatusCode}, Reason: {response.ReasonPhrase}, Content: {errorContent}");
                    return Enumerable.Empty<User>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiUserService: Exception during GetAllUsersAsync: {ex.Message}");
                Debug.WriteLine($"ApiUserService: Exception stack trace: {ex.StackTrace}");
                return Enumerable.Empty<User>();
            }
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName)
        {
            try
            {
                Debug.WriteLine($"ApiUserService: Getting users by role '{roleName}' from {_httpClient.BaseAddress}api/users/byRole/{roleName}");
                var response = await _httpClient.GetAsync($"api/users/byRole/{Uri.EscapeDataString(roleName)}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"ApiUserService: Raw response: {content}");
                    
                    // Deserialize to dynamic objects first
                    var dynamicUsers = JsonSerializer.Deserialize<JsonElement[]>(content, _serializerOptions);
                    var users = new List<User>();
                    
                    foreach (var dynamicUser in dynamicUsers)
                    {
                        var user = new User
                        {
                            Id = dynamicUser.GetProperty("id").GetInt32(),
                            Username = dynamicUser.GetProperty("username").GetString() ?? "",
                            Email = dynamicUser.GetProperty("email").GetString() ?? "",
                            FirstName = dynamicUser.TryGetProperty("firstName", out var firstName) ? firstName.GetString() : null,
                            LastName = dynamicUser.TryGetProperty("lastName", out var lastName) ? lastName.GetString() : null,
                            CreatedAt = dynamicUser.GetProperty("createdAt").GetDateTime()
                        };
                        
                        // Handle the nested Role object
                        if (dynamicUser.TryGetProperty("role", out var roleElement) && roleElement.ValueKind != JsonValueKind.Null)
                        {
                            user.Role = new Role
                            {
                                Id = roleElement.GetProperty("id").GetInt32(),
                                Name = roleElement.GetProperty("name").GetString() ?? "",
                                Description = roleElement.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : ""
                            };
                            user.RoleId = user.Role.Id;
                        }
                        
                        users.Add(user);
                        Debug.WriteLine($"Parsed user: {user.Username}, Role: {user.Role?.Name ?? "null"}");
                    }
                    
                    return users;
                }
                Debug.WriteLine($"ApiUserService: GetUsersByRoleAsync failed. Status: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                return Enumerable.Empty<User>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiUserService: Exception during GetUsersByRoleAsync: {ex.Message}");
                return Enumerable.Empty<User>();
            }
        }

        public async Task<User> CreateUserAsync(User user)
        {
            // Usually client apps might not create users directly, but implement if needed
            Debug.WriteLine("ApiUserService: CreateUserAsync called (potentially needs API endpoint: POST api/users)");
            await Task.CompletedTask; // Placeholder
            // Example:
            // var response = await _httpClient.PostAsJsonAsync("api/users", user);
            // response.EnsureSuccessStatusCode();
            // return await response.Content.ReadFromJsonAsync<User>(_serializerOptions);
            throw new NotImplementedException("Client-side CreateUserAsync via API not fully implemented.");
        }

        public async Task<User> LoginAsync(Credentials credentials)
        {
            // This method is often redundant if AuthenticateAsync exists, but implement if needed
            Debug.WriteLine("ApiUserService: LoginAsync called, delegating to AuthenticateAsync");
            return await AuthenticateAsync(credentials.Username, credentials.Password);
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            // Client apps might update their own profile, but perhaps not arbitrary users
            Debug.WriteLine($"ApiUserService: UpdateUserAsync called (potentially needs API endpoint: PUT api/users/{user.Id})");
            await Task.CompletedTask; // Placeholder
            // Example:
            // var response = await _httpClient.PutAsJsonAsync($"api/users/{user.Id}", user);
            // response.EnsureSuccessStatusCode();
            // return await response.Content.ReadFromJsonAsync<User>(_serializerOptions);
            throw new NotImplementedException("Client-side UpdateUserAsync via API not fully implemented.");
        }

        public async Task DeleteUserAsync(int id)
        {
            Debug.WriteLine($"ApiUserService: DeleteUserAsync called (potentially needs API endpoint: DELETE api/users/{id})");
            await Task.CompletedTask; // Placeholder
            // Example:
            // var response = await _httpClient.DeleteAsync($"api/users/{id}");
            // response.EnsureSuccessStatusCode();
             throw new NotImplementedException("Client-side DeleteUserAsync via API not fully implemented.");
        }

        public async Task<int> GetNewClientCountForDateAsync(DateTime date)
        {
             Debug.WriteLine($"ApiUserService: GetNewClientCountForDateAsync called (potentially needs API endpoint: GET api/users/stats/new-clients?date={date:yyyy-MM-dd})");
            // This might be more of an admin/reporting function, less common for client
            await Task.CompletedTask; // Placeholder
            // Example:
            // var response = await _httpClient.GetAsync($"api/users/stats/new-clients?date={date:yyyy-MM-dd}");
            // response.EnsureSuccessStatusCode();
            // var content = await response.Content.ReadAsStringAsync();
            // return int.Parse(content);
             throw new NotImplementedException("Client-side GetNewClientCountForDateAsync via API not fully implemented.");
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            try
            {
                Debug.WriteLine($"ApiUserService: Getting user by username {username} from {_httpClient.BaseAddress}api/users/username/{username}");
                // Note: Ensure API has an endpoint like GET api/users/username/{username}
                var response = await _httpClient.GetAsync($"api/users/username/{Uri.EscapeDataString(username)}"); 
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<User>(_serializerOptions);
                }
                Debug.WriteLine($"ApiUserService: GetUserByUsernameAsync failed. Status: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApiUserService: Exception during GetUserByUsernameAsync: {ex.Message}");
                return null;
            }
        }
    }
} 