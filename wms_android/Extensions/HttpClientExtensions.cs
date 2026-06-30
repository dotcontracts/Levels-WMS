using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace wms_android.Extensions
{
    public static class HttpClientExtensions
    {
        /// <summary>
        /// Adds session header to HTTP requests if a session exists
        /// </summary>
        /// <param name="httpClient">The HttpClient instance</param>
        /// <returns>The same HttpClient instance for chaining</returns>
        public static async Task<HttpClient> AddSessionHeaderAsync(this HttpClient httpClient)
        {
            try
            {
                var sessionId = await SecureStorage.GetAsync("SessionId");
                if (!string.IsNullOrEmpty(sessionId))
                {
                    // Remove existing session header if present
                    if (httpClient.DefaultRequestHeaders.Contains("X-Session-ID"))
                    {
                        httpClient.DefaultRequestHeaders.Remove("X-Session-ID");
                    }
                    
                    // Add session header
                    httpClient.DefaultRequestHeaders.Add("X-Session-ID", sessionId);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the request
                System.Diagnostics.Debug.WriteLine($"Error adding session header: {ex.Message}");
            }
            
            return httpClient;
        }

        /// <summary>
        /// Removes session header from HTTP requests
        /// </summary>
        /// <param name="httpClient">The HttpClient instance</param>
        /// <returns>The same HttpClient instance for chaining</returns>
        public static HttpClient RemoveSessionHeader(this HttpClient httpClient)
        {
            try
            {
                if (httpClient.DefaultRequestHeaders.Contains("X-Session-ID"))
                {
                    httpClient.DefaultRequestHeaders.Remove("X-Session-ID");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing session header: {ex.Message}");
            }
            
            return httpClient;
        }
    }
}