using System;
using System.Net.Http;
using System.Net.Http.Headers;

public static class HttpClientFactory
{
    private static HttpClient _client;

    public static HttpClient GetClient()
    {
        if (_client == null)
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://bkljejfkirbknyhzalba.supabase.co")
            };
            _client.DefaultRequestHeaders.Accept
                   .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        return _client;
    }

    // Bearer トークンを設定するヘルパー
    public static void SetBearerToken(string accessToken)
    {
        var client = GetClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
