using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class SupabaseAuthClient
{
    private readonly HttpClient _http;
    private const string TokenEndpoint = "/auth/v1/token";

    public class Session
    {
        [JsonProperty("access_token")]
        public string AccessToken;
        [JsonProperty("refresh_token")]
        public string RefreshToken;
        [JsonProperty("expires_in")]
        public int ExpiresIn;
    }

    public SupabaseAuthClient()
    {
        _http = HttpClientFactory.GetClient();
    }

    // Apple ID トークンでサインイン
    public async Task<Session> SignInWithIdTokenAsync(string idToken)
    {
        Debug.Log("SignInWithIdTokenAsync() called");
        try
        {
            var payload = new
            {
                provider = "apple",
                id_token = idToken,
            };
            var content = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _http.PostAsync($"{TokenEndpoint}?grant_type=id_token", content);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var body = await response.Content.ReadAsStringAsync();
                Debug.LogError($"401 Unauthorized from Supabase Auth: {body}");
                throw new UnauthorizedAccessException("Supabase Auth returned 401");
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Session>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SignInWithIdTokenAsync() failed: {ex}");
            throw;
        }
    }

    // リフレッシュトークンで更新
    public async Task<Session> RefreshSessionAsync(string refreshToken)
    {
        Debug.Log("RefreshSessionAsync() called");
        try
        {
            var payload = new { refresh_token = refreshToken };
            var content = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _http.PostAsync($"{TokenEndpoint}?grant_type=refresh_token", content);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var body = await response.Content.ReadAsStringAsync();
                Debug.LogError($"401 Unauthorized on refresh: {body}");
                throw new UnauthorizedAccessException("Supabase Auth refresh returned 401");
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Session>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"RefreshSessionAsync() failed: {ex}");
            throw;
        }
    }
}
