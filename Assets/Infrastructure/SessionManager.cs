using UnityEngine;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

public static class SessionManager
{
    private const string AccessTokenKey = "supabase_access_token";
    private const string RefreshTokenKey = "supabase_refresh_token";

    // トークンを永続化
    public static void SaveSession(string accessToken, string refreshToken)
    {
        Debug.Log("SaveSession() called");
        PlayerPrefs.SetString(AccessTokenKey, accessToken);
        PlayerPrefs.SetString(RefreshTokenKey, refreshToken);
        PlayerPrefs.Save();

        // HttpClient のデフォルトヘッダーにも設定しておく
        HttpClientFactory.SetBearerToken(accessToken);
    }

    // 保存済みトークンを読み込み
    public static (string accessToken, string refreshToken) LoadSession()
    {
        Debug.Log("LoadSession() called");
        var at = PlayerPrefs.GetString(AccessTokenKey, null);
        var rt = PlayerPrefs.GetString(RefreshTokenKey, null);
        if (!string.IsNullOrEmpty(at))
            HttpClientFactory.SetBearerToken(at);
        return (at, rt);
    }

    // セッションデータをクリア
    public static void ClearSession()
    {
        Debug.Log("ClearSession() called");
        PlayerPrefs.DeleteKey(AccessTokenKey);
        PlayerPrefs.DeleteKey(RefreshTokenKey);
        PlayerPrefs.Save();

        // HttpClient のデフォルトヘッダーをクリア
        HttpClientFactory.ClearBearerToken();
    }

    // アクセストークンの期限切れチェック
    public static bool IsAccessTokenExpired(string token)
    {
        Debug.Log("IsAccessTokenExpired() called");
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return true;

            var payload = parts[1];
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64(payload)));
            var payloadData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (payloadData.TryGetValue("exp", out var exp))
            {
                var expiry = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(exp));
                return DateTimeOffset.UtcNow > expiry;
            }
        }
        catch { }
        return true;
    }

    private static string PadBase64(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: return base64 + "==";
            case 3: return base64 + "=";
            default: return base64;
        }
    }

    // リフレッシュトークンでセッション更新
    public static async Task<bool> RefreshIfNeededAsync()
    {
        Debug.Log("RefreshIfNeededAsync() called");
        try
        {
            var (at, rt) = LoadSession();
            if (string.IsNullOrEmpty(rt) || IsAccessTokenExpired(at))
                return false;

            var authClient = new SupabaseAuthClient();
            var session = await authClient.RefreshSessionAsync(rt);
            if (session != null)
            {
                SaveSession(session.AccessToken, session.RefreshToken);
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"RefreshIfNeededAsync() failed: {ex.Message}");
        }
        return false;
    }
}
