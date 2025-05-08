using System;
using UnityEngine;
using System.Net.Http;
using System.Net.Http.Headers;

public static class HttpClientFactory
{
    private static HttpClient _client;

    public static HttpClient GetClient()
    {
        Debug.Log("GetClient() called");
        if (_client == null)
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://bkljejfkirbknyhzalba.supabase.co")
            };
            _client.DefaultRequestHeaders.Accept
                   .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!_client.DefaultRequestHeaders.Contains("apikey"))
            {
                _client.DefaultRequestHeaders.Add("apikey", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImJrbGplamZraXJia255aHphbGJhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDY2MjY1MzksImV4cCI6MjA2MjIwMjUzOX0.p4-ltGDchU-jWtUPX9wd2fQYBa9YQ1NFJDGd082hii4");
            }
        }
        return _client;
    }

    // Bearer トークンを設定するヘルパー
    public static void SetBearerToken(string accessToken)
    {
        Debug.Log("SetBearerToken() called");
        var client = GetClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
    }

    // Bearer トークンをクリアするヘルパー
    public static void ClearBearerToken()
    {
        Debug.Log("ClearBearerToken() called");
        var client = GetClient();
        client.DefaultRequestHeaders.Authorization = null;
    }
}
