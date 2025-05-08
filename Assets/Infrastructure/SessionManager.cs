using UnityEngine;
using System.Threading.Tasks;

public static class SessionManager
{
    private const string AccessTokenKey  = "supabase_access_token";
    private const string RefreshTokenKey = "supabase_refresh_token";

    // トークンを永続化
    public static void SaveSession(string accessToken, string refreshToken)
    {
        PlayerPrefs.SetString(AccessTokenKey, accessToken);
        PlayerPrefs.SetString(RefreshTokenKey, refreshToken);
        PlayerPrefs.Save();

        // HttpClient のデフォルトヘッダーにも設定しておく
        HttpClientFactory.SetBearerToken(accessToken);
    }

    // 保存済みトークンを読み込み
    public static (string accessToken, string refreshToken) LoadSession()
    {
        var at = PlayerPrefs.GetString(AccessTokenKey, null);
        var rt = PlayerPrefs.GetString(RefreshTokenKey, null);
        if (!string.IsNullOrEmpty(at))
            HttpClientFactory.SetBearerToken(at);
        return (at, rt);
    }

    // リフレッシュトークンでセッション更新
    public static async Task<bool> RefreshIfNeededAsync()
    {
        var (at, rt) = LoadSession();
        if (string.IsNullOrEmpty(rt))
            return false;

        // ここで期限切れチェックを行ってもよい
        var authClient = new SupabaseAuthClient();
        var session = await authClient.RefreshSessionAsync(rt);
        if (session != null)
        {
            SaveSession(session.AccessToken, session.RefreshToken);
            return true;
        }

        return false;
    }
}
