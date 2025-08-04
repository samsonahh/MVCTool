using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace MVCTool
{
    public static class LoginApi
    {
        public static string BaseUrl { get; private set; }
        public static string BearerToken { get; private set; } = null;
        public static bool HasBearerToken => !string.IsNullOrEmpty(BearerToken);
        public static bool IsLoggedIn => HasBearerToken;

        public static readonly string DefaultBaseUrl = "https://mvcdev.represent.org/";

        public static readonly string BaseUrlEditorPrefsKey = "MVCTool_BaseUrl";
        public static readonly string BearerTokenSessionKey = "MVCTool_LoginBearerToken";

        public enum TokenValidationResult
        {
            Valid,
            InvalidToken,
            RequestFailed
        }

        public static async UniTask Login(string baseUrl, string username, string password)
        {
            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Debug.LogError($"Login failed: Missing required fields (URL, Username, Password).");
                throw new System.Exception("Please fill in all fields!");
            }

            baseUrl = baseUrl.TrimEnd('/');
            string targetUrl = $"{baseUrl}/strapi/api/auth/local";

            WWWForm form = new WWWForm();
            form.AddField("identifier", username);
            form.AddField("password", password);
            Debug.Log($"Attempting to login as {username}");

            UnityWebRequest w = UnityWebRequest.Post(targetUrl, form);
            await w.SendWebRequest();

            if (w.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(w.downloadHandler?.text); // print JSON error message
                throw new System.Exception($"Login failed: {w.error}");
            }

            JObject json = JObject.Parse(w.downloadHandler.text);
            string jwt = (string)json["jwt"];
            Debug.Log($"Successfully logged in as {username}!");

            SetBearerToken(jwt);
            SetBaseUrl(baseUrl);
        }

        public static void SetBaseUrl(string url)
        {
            BaseUrl = url;
            EditorPrefs.SetString(BaseUrlEditorPrefsKey, url);
        }

        public static void ClearBaseUrl()
        {
            BaseUrl = DefaultBaseUrl;
            EditorPrefs.DeleteKey(BaseUrlEditorPrefsKey);
        }

        public static void LoadStoredBaseUrl()
        {
            BaseUrl = EditorPrefs.GetString(BaseUrlEditorPrefsKey, DefaultBaseUrl);
            if (string.IsNullOrEmpty(BaseUrl))
                BaseUrl = DefaultBaseUrl;
        }

        public static void SetBearerToken(string token)
        {
            BearerToken = token;
            SessionState.SetString(BearerTokenSessionKey, token);
            // EditorPrefs.SetString(EditorPrefsKeys.BearerToken, token);
        }

        /// <summary>
        /// Logs the user out.
        /// </summary>
        public static void ClearBearerToken()
        {
            BearerToken = null;
            SessionState.EraseString(BearerTokenSessionKey);
            // EditorPrefs.DeleteKey(EditorPrefsKeys.BearerToken);
        }

        public static void LoadStoredBearerToken()
        {
            BearerToken = SessionState.GetString(BearerTokenSessionKey, null);
            // BearerToken = EditorPrefs.GetString(EditorPrefsKeys.BearerToken, null);
            if (string.IsNullOrEmpty(BearerToken))
                BearerToken = null;
        }

        public static async UniTask<UnityWebRequest> AuthenticatedGet(string url)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            if (HasBearerToken)
                request.SetRequestHeader("Authorization", $"Bearer {BearerToken}");

            await request.SendWebRequest();

            if (ValidateTokenFromRequest(request) == TokenValidationResult.InvalidToken)
                HandleInvalidTokenResponse();

            return request;
        }

        public static async UniTask<UnityWebRequest> AuthenticatedPost(string url, WWWForm form)
        {
            UnityWebRequest request = UnityWebRequest.Post(url, form);
            if (HasBearerToken)
                request.SetRequestHeader("Authorization", $"Bearer {BearerToken}");

            await request.SendWebRequest();

            if (ValidateTokenFromRequest(request) == TokenValidationResult.InvalidToken)
                HandleInvalidTokenResponse();

            return request;
        }

        public static TokenValidationResult ValidateTokenFromRequest(UnityWebRequest request)
        {
            if (request == null)
                return TokenValidationResult.RequestFailed;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Request error: {request.error}");
                return TokenValidationResult.RequestFailed;
            }

            if (request.responseCode == 401 || request.responseCode == 403)
            {
                Debug.LogWarning("Token invalid or expired.");
                return TokenValidationResult.InvalidToken;
            }

            return TokenValidationResult.Valid;
        }

        private static void HandleInvalidTokenResponse()
        {
            Debug.LogWarning("Invalid Bearer Token response received. Clearing token.");
            ClearBearerToken();
        }

        /// <summary>
        /// Helper method to create a target URL for API requests.
        /// </summary>
        public static string CreateTargetUrl(string endpoint)
        {
            if (string.IsNullOrEmpty(BaseUrl))
            {
                Debug.LogError("Base URL is not set. Please log in first.");
                throw new System.Exception("Base URL is required to create target URL.");
            }

            return $"{BaseUrl.TrimEnd('/')}/strapi/api/{endpoint.TrimStart('/')}";
        }
    }
}