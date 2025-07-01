using Cysharp.Threading.Tasks;
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

        public static readonly string DefaultBaseUrl = "https://mvcdev.represent.org/";

        public static class EditorPrefsKeys
        {
            public static readonly string BaseUrl = "MVCTool_BaseUrl";
            public static readonly string BearerToken = "MVCTool_LoginBearerToken";
        }

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

            WWWForm form = new WWWForm();
            form.AddField("identifier", username);
            form.AddField("password", password);
            Debug.Log($"Attempting to login as {username}");

            UnityWebRequest w = UnityWebRequest.Post($"{baseUrl}/strapi/api/auth/local", form);
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
            EditorPrefs.SetString(EditorPrefsKeys.BaseUrl, url);
        }

        public static void ClearBaseUrl()
        {
            BaseUrl = DefaultBaseUrl;
            EditorPrefs.DeleteKey(EditorPrefsKeys.BaseUrl);
        }

        public static void LoadStoredBaseUrl()
        {
            BaseUrl = EditorPrefs.GetString(EditorPrefsKeys.BaseUrl, DefaultBaseUrl);
            if (string.IsNullOrEmpty(BaseUrl))
                BaseUrl = DefaultBaseUrl;
        }

        public static void SetBearerToken(string token)
        {
            BearerToken = token;
            EditorPrefs.SetString(EditorPrefsKeys.BearerToken, token);
        }

        /// <summary>
        /// Logs the user out.
        /// </summary>
        public static void ClearBearerToken()
        {
            BearerToken = null;
            EditorPrefs.DeleteKey(EditorPrefsKeys.BearerToken);
        }

        public static void LoadStoredBearerToken()
        {
            BearerToken = EditorPrefs.GetString(EditorPrefsKeys.BearerToken, null);
            if (string.IsNullOrEmpty(BearerToken))
                BearerToken = null;
        }

        public static bool HasBearerToken => !string.IsNullOrEmpty(BearerToken);

        public static async UniTask<UnityWebRequest> AuthenticatedGet(string url)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            if (HasBearerToken)
                request.SetRequestHeader("Authorization", $"Bearer {BearerToken}");

            await request.SendWebRequest();

            if (ValidateTokenFromRequest(request) == TokenValidationResult.InvalidToken)
            {
                HandleInvalidTokenResponse();
            }

            return request;
        }

        public static async UniTask<UnityWebRequest> AuthenticatedPost(string url, WWWForm form)
        {
            UnityWebRequest request = UnityWebRequest.Post(url, form);
            if (HasBearerToken)
                request.SetRequestHeader("Authorization", $"Bearer {BearerToken}");

            await request.SendWebRequest();

            if (ValidateTokenFromRequest(request) == TokenValidationResult.InvalidToken)
            {
                HandleInvalidTokenResponse();
            }

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
    }
}