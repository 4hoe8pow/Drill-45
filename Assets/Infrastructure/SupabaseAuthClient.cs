using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class SupabaseAuthClient
{
    private readonly HttpClient _http = HttpClientFactory.GetClient();
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

    // Apple ID トークンでサインイン
    public async Task<Session> SignInWithIdTokenAsync(string provider, string idToken, string nonce)
    {
        Debug.Log("SignInWithIdTokenAsync() called");
        try
        {
            var payload = new
            {
                provider = provider,
                token = idToken,
                nonce = nonce
            };
            var content = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json"
            );
            var response = await _http.PostAsync($"{TokenEndpoint}?grant_type=id_token", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Session>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SignInWithIdTokenAsync() failed: {ex.Message}");
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
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Session>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"RefreshSessionAsync() failed: {ex.Message}");
            throw;
        }
    }
}
