using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
namespace MVCTool
{
    public class LoginTab : EditorTab
    {
        public override string TabName => "Login";

        private const string RememberUsernameEditorPrefsKey = "MVCTool_LoginRememberUser";
        private const string UsernameEditorPrefsKey = "MVCTool_LoginUsername";

        private string _baseUrl = LoginApi.DefaultBaseUrl;
        private string _username = "";
        private string _password = "";

        private bool _rememberUsername = false;

        private bool _isLoggingIn = false;

        private string _errorMessage = null;

        public override void Draw()
        {
            bool isLoggedIn = !string.IsNullOrEmpty(LoginApi.BearerToken);
            if (!isLoggedIn)
            {
                EditorGUI.BeginDisabledGroup(_isLoggingIn);
                DrawLoginFields();
                EditorGUI.EndDisabledGroup();

                if (!string.IsNullOrEmpty(_errorMessage))
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox($"Error: {_errorMessage}", MessageType.Error);
                }
            }
            else
            {
                GUILayout.Label($"Logged in as {_username}!", new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter
                });

                if (GUILayout.Button("Logout", GUILayout.Height(30)))
                    Logout();
            }
        }

        public override void OnEnter()
        {

        }

        public override void OnExit()
        {

        }

        private protected override void Load()
        {
            LoginApi.LoadStoredBaseUrl();
            _baseUrl = LoginApi.BaseUrl;

            _rememberUsername = EditorPrefs.GetBool(RememberUsernameEditorPrefsKey, false);
            _username = EditorPrefs.GetString(UsernameEditorPrefsKey, "");

            LoginApi.LoadStoredBearerToken();
        }

        public override void Reset()
        {
            LoginApi.ClearBearerToken();

            LoginApi.ClearBaseUrl();
            _baseUrl = LoginApi.DefaultBaseUrl;

            EditorPrefs.DeleteKey(UsernameEditorPrefsKey);
            EditorPrefs.DeleteKey(RememberUsernameEditorPrefsKey);

            Load();
        }

        private void DrawLoginFields()
        {
            _baseUrl = EditorGUILayout.TextField("Base Url", _baseUrl);
            _username = EditorGUILayout.TextField("Username", _username);
            _password = EditorGUILayout.PasswordField("Password", _password);

           _rememberUsername = EditorGUILayout.Toggle("Remember Username", _rememberUsername);
            EditorPrefs.SetBool(RememberUsernameEditorPrefsKey, _rememberUsername);
            if (!_rememberUsername)
                EditorPrefs.DeleteKey(UsernameEditorPrefsKey);

            if (GUILayout.Button("Login"))
            {
                Login(_baseUrl, _username, _password).Forget();
            }

            if (GUILayout.Button("Register") )
            {
                string registrationUrl = $"{_baseUrl.Trim('/')}/register";
                Application.OpenURL(registrationUrl);
            }
        }

        private async UniTask Login(string baseUrl, string username, string password)
        {
            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Debug.LogError($"Login failed: Missing required fields (URL, Username, Password).");
                _errorMessage = "Please fill in all fields!";
                ForceDraw(); // Force redraw to show the error message
                return;
            }

            _isLoggingIn = true;
            _errorMessage = null;

            try
            {
                await LoginApi.Login(baseUrl, username, password);

                if (_rememberUsername)
                    EditorPrefs.SetString(UsernameEditorPrefsKey, _username);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Login exception: {ex.Message}");
                _errorMessage = $"Login failed!";
                ForceDraw(); // Force redraw to show the error message
            }
            finally
            {
                _isLoggingIn = false;
            }
        }

        private void Logout()
        {
            GUI.FocusControl(null);

            LoginApi.ClearBearerToken();

            if (!_rememberUsername)
                _username = "";
            _password = "";
        }
    }
}