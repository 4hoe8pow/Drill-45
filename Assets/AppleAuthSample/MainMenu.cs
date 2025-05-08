using System;
using System.Text;
using System.Collections;
using UnityEngine;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Interfaces;
using AppleAuth.Native;

public class MainMenu : MonoBehaviour
{
    private const string AppleUserIdKey = "AppleUserId";

    private IAppleAuthManager _appleAuthManager;

    public LoginMenuHandler LoginMenu;
    public GameMenuHandler GameMenu;

    private void Start()
    {
        Debug.Log("Start() called");
        // 1) AppleAuthManager 初期化
        if (AppleAuthManager.IsCurrentPlatformSupported)
        {
            var deserializer = new PayloadDeserializer();
            _appleAuthManager = new AppleAuthManager(deserializer);
        }

        // 2) セッション復元 or Apple Quick Login
        InitializeLoginMenu();
    }

    private void Update()
    {
        _appleAuthManager?.Update();
        LoginMenu.UpdateLoadingMessage(Time.deltaTime);
    }

    public void SignInWithAppleButtonPressed()
    {
        Debug.Log("SignInWithAppleButtonPressed() called");
        SetupLoginMenuForAppleSignIn();
        SignInWithApple();
    }

    private void InitializeLoginMenu()
    {
        Debug.Log("InitializeLoginMenu() called");
        LoginMenu.SetVisible(true);
        GameMenu.SetVisible(false);

        if (_appleAuthManager == null)
        {
            SetupLoginMenuForUnsupportedPlatform();
            return;
        }

        // Apple資格情報がリボークされた通知
        _appleAuthManager.SetCredentialsRevokedCallback(_ =>
        {
            PlayerPrefs.DeleteKey(AppleUserIdKey);
            SessionManager.ClearSession();
            SetupLoginMenuForSignInWithApple();
        });

        // 1) まず Supabase セッション復元を試みる
        var (at, rt) = SessionManager.LoadSession();
        if (!string.IsNullOrEmpty(at))
        {
            // (a) accessToken の有効性チェックが必要ならここで
            HttpClientFactory.SetBearerToken(at);
            SetupGameMenu(PlayerPrefs.GetString(AppleUserIdKey), null);
            return;
        }

        // 2) 既存の AppleUserId があれば Quick Login
        if (PlayerPrefs.HasKey(AppleUserIdKey))
        {
            SetupLoginMenuForQuickLoginAttempt();
            AttemptQuickLogin();
        }
        else
        {
            SetupLoginMenuForSignInWithApple();
        }
    }

    private void SetupLoginMenuForUnsupportedPlatform()
    {
        Debug.Log("SetupLoginMenuForUnsupportedPlatform() called");
        LoginMenu.SetVisible(true);
        GameMenu.SetVisible(false);
        LoginMenu.SetSignInWithAppleButton(false, false);
        LoginMenu.SetLoadingMessage(true, "Unsupported platform");
    }

    private void SetupLoginMenuForSignInWithApple()
    {
        Debug.Log("SetupLoginMenuForSignInWithApple() called");
        LoginMenu.SetVisible(true);
        GameMenu.SetVisible(false);
        LoginMenu.SetSignInWithAppleButton(true, true);
        LoginMenu.SetLoadingMessage(false, string.Empty);
    }

    private void SetupLoginMenuForQuickLoginAttempt()
    {
        Debug.Log("SetupLoginMenuForQuickLoginAttempt() called");
        LoginMenu.SetVisible(true);
        GameMenu.SetVisible(false);
        LoginMenu.SetSignInWithAppleButton(true, false);
        LoginMenu.SetLoadingMessage(true, "Attempting Quick Login");
    }

    private void SetupLoginMenuForAppleSignIn()
    {
        Debug.Log("SetupLoginMenuForAppleSignIn() called");
        LoginMenu.SetVisible(true);
        GameMenu.SetVisible(false);
        LoginMenu.SetSignInWithAppleButton(true, false);
        LoginMenu.SetLoadingMessage(true, "Signing In with Apple");
    }

    private void SetupGameMenu(string appleUserId, ICredential credential)
    {
        Debug.Log("SetupGameMenu() called");
        LoginMenu.SetVisible(false);
        GameMenu.SetVisible(true);
        GameMenu.SetupAppleData(appleUserId, credential);
    }

    private void AttemptQuickLogin()
    {
        Debug.Log("AttemptQuickLogin() called");
        var args = new AppleAuthQuickLoginArgs();
        _appleAuthManager.QuickLogin(args,
            credential =>
            {
                var appleCred = credential as IAppleIDCredential;
                if (appleCred != null)
                {
                    PlayerPrefs.SetString(AppleUserIdKey, appleCred.User);
                    // Supabase 認証も行う
                    var rawNonce = ""; // QuickLogin には nonce 不要
                    StartCoroutine(SignInWithSupabase(Encoding.UTF8.GetString(appleCred.IdentityToken), rawNonce));
                }
            },
            error =>
            {
                Debug.LogError($"AttemptQuickLogin() failed: {error.LocalizedDescription}");
                SetupLoginMenuForSignInWithApple();
            });
    }

    private void SignInWithApple()
    {
        Debug.Log("SignInWithApple() called");
        // 1) Nonce を生成
        var rawNonce = Supabase.Gotrue.Helpers.GenerateNonce();
        var hashedNonce = Supabase.Gotrue.Helpers.GenerateSHA256NonceFromRawNonce(rawNonce);

        var args = new AppleAuthLoginArgs(
            LoginOptions.IncludeEmail | LoginOptions.IncludeFullName,
            hashedNonce
        );

        _appleAuthManager.LoginWithAppleId(args,
            credential =>
            {
                var appleCred = credential as IAppleIDCredential;
                if (appleCred != null)
                {
                    PlayerPrefs.SetString(AppleUserIdKey, appleCred.User);
                    var idToken = Encoding.UTF8.GetString(appleCred.IdentityToken);
                    // 2) Supabase サインイン
                    StartCoroutine(SignInWithSupabase(idToken, rawNonce));
                }
            },
            error =>
            {
                Debug.LogError($"SignInWithApple() failed: {error.LocalizedDescription}");
                SetupLoginMenuForSignInWithApple();
            });
    }

    private IEnumerator SignInWithSupabase(string idToken, string rawNonce)
    {
        Debug.Log("SignInWithSupabase() called");
        var authClient = new SupabaseAuthClient();
        var task = authClient.SignInWithIdTokenAsync("apple", idToken, rawNonce);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception == null && task.Result != null)
        {
            var session = task.Result;
            // 3) トークンを保存
            SessionManager.SaveSession(session.AccessToken, session.RefreshToken);
            SetupGameMenu(PlayerPrefs.GetString(AppleUserIdKey), null);
        }
        else
        {
            Debug.LogError($"SignInWithSupabase() failed: {task.Exception?.Message}");
            SetupLoginMenuForSignInWithApple();
        }
    }
}
