using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    private const string AppleUserIdKey = "AppleUserId";
    private IAppleAuthManager _appleAuthManager;
    private string _rawNonce;

    public LoginMenuHandler LoginMenu;
    public GameMenuHandler   GameMenu;

    private async void Start()
    {
        // Supabase セッションの自動復元を試みる
        var (at, rt) = SessionManager.LoadSession();
        if (!string.IsNullOrEmpty(at) && !string.IsNullOrEmpty(rt))
        {
            Debug.Log("Supabase セッション復元");
            SetupGameMenu(PlayerPrefs.GetString(AppleUserIdKey), null);
            return;
        }

        // AppleAuthManager の初期化…
        if (AppleAuthManager.IsCurrentPlatformSupported)
        {
            _appleAuthManager = new AppleAuthManager(new PayloadDeserializer());
            _appleAuthManager.SetCredentialsRevokedCallback(_ =>
            {
                PlayerPrefs.DeleteKey(AppleUserIdKey);
                SessionManager.SaveSession("", "");
                InitializeLoginMenu();
            });
        }

        InitializeLoginMenu();
    }

    private void InitializeLoginMenu()
    {
        LoginMenu.SetVisible(true);
        GameMenu.SetVisible(false);

        if (_appleAuthManager == null)
        {
            LoginMenu.SetLoadingMessage(true, "Unsupported platform");
            return;
        }

        if (PlayerPrefs.HasKey(AppleUserIdKey))
        {
            AttemptQuickLogin();
        }
        else
        {
            LoginMenu.SetSignInWithAppleButton(true, true);
        }
    }

    public void SignInWithAppleButtonPressed()
    {
        LoginMenu.SetLoadingMessage(true, "Signing In with Apple");
        SignInWithApple();
    }

    private void AttemptQuickLogin()
    {
        LoginMenu.SetLoadingMessage(true, "Attempting Quick Login");
        _appleAuthManager.QuickLogin(new AppleAuthQuickLoginArgs(),
            credential =>
            {
                OnAppleCredential(credential as IAppleIDCredential);
            },
            error =>
            {
                Debug.LogWarning("Quick Login failed");
                LoginMenu.SetSignInWithAppleButton(true, true);
            });
    }

    private void SignInWithApple()
    {
        // rawNonce を生成
        _rawNonce = Supabase.Gotrue.Helpers.GenerateNonce();
        var sha256Nonce = Supabase.Gotrue.Helpers.GenerateSHA256NonceFromRawNonce(_rawNonce);

        var loginArgs = new AppleAuthLoginArgs(
            LoginOptions.IncludeEmail | LoginOptions.IncludeFullName,
            sha256Nonce
        );

        _appleAuthManager.LoginWithAppleId(
            loginArgs,
            credential =>
            {
                OnAppleCredential(credential as IAppleIDCredential);
            },
            error =>
            {
                Debug.LogWarning("Sign in with Apple failed");
                LoginMenu.SetSignInWithAppleButton(true, true);
            });
    }

    private void OnAppleCredential(IAppleIDCredential appleCred)
    {
        // Apple User ID 保存
        PlayerPrefs.SetString(AppleUserIdKey, appleCred.User);

        // Supabase サインイン
        _ = SignInWithSupabaseAsync(appleCred).ContinueWith(_ =>
        {
            SetupGameMenu(appleCred.User, appleCred);
        });
    }

    private async Task SignInWithSupabaseAsync(IAppleIDCredential appleCred)
    {
        var idToken = Encoding.UTF8.GetString(appleCred.IdentityToken);
        var authClient = new SupabaseAuthClient();
        var session = await authClient.SignInWithIdTokenAsync(
            provider: "apple",
            idToken: idToken,
            nonce: _rawNonce
        );

        // セッション永続化
        SessionManager.SaveSession(session.AccessToken, session.RefreshToken);
        Debug.Log("Supabase ログイン成功");
    }

    private void SetupGameMenu(string appleUserId, ICredential credential)
    {
        LoginMenu.SetVisible(false);
        GameMenu.SetVisible(true);
        GameMenu.SetupAppleData(appleUserId, credential);
    }
}
