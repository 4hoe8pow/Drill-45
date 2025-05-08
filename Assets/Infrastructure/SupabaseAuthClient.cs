using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class SupabaseAuthClient
{
    private readonly HttpClient _http;
    private const string TokenEndpoint = "/auth/v1/token";

    // ここにあなたの ANON KEY を設定
    private const string AnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImJrbGplamZraXJia255aHphbGJhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDY2MjY1MzksImV4cCI6MjA2MjIwMjUzOX0.p4-ltGDchU-jWtUPX9wd2fQYBa9YQ1NFJDGd082hii4";

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
        _http = new HttpClient
        {
            BaseAddress = new Uri("https://bkljejfkirbknyhzalba.supabase.co")
        };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!_http.DefaultRequestHeaders.Contains("apikey"))
            _http.DefaultRequestHeaders.Add("apikey", AnonKey);
    }

    // Apple ID トークンでサインイン
    public async Task<Session> SignInWithIdTokenAsync(string idToken, string nonce)
    {
        Debug.Log("SignInWithIdTokenAsync() called");
        try
        {
            // ペイロード組み立て
            var payload = new
            {
                provider = "apple",
                id_token = idToken,
                nonce    = nonce
            };
            var content = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json"
            );

            // エンドポイント呼び出し
            var response = await _http.PostAsync($"{TokenEndpoint}?grant_type=id_token", content);

            // 401 の場合は詳細をログに出す
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
