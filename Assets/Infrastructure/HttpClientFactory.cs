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
