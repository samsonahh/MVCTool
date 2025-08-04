using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using static Codice.CM.WorkspaceServer.WorkspaceTreeDataStore;

namespace MVCTool
{
    public static class AssetManager
    {
        public static readonly Dictionary<BuildTarget, BuildTargetData> SupportedBuildTargets = new Dictionary<BuildTarget, BuildTargetData>
        {
            { 
                BuildTarget.Android,
                new BuildTargetData { 
                    Directory = $"{Application.streamingAssetsPath}/Android", 
                    DisplayName = "Android",
                    EditorPrefsKey = "MVCTool_AndroidBuildSelected",
                } 
            },
            {
                BuildTarget.StandaloneWindows,
                new BuildTargetData {
                    Directory = $"{Application.streamingAssetsPath}/PC",
                    DisplayName = "PC",
                    EditorPrefsKey = "MVCTool_PCBuildSelected",
                }
            },
            {
                BuildTarget.WebGL,
                new BuildTargetData {
                    Directory = $"{Application.streamingAssetsPath}/WebGL",
                    DisplayName = "WebGL",
                    EditorPrefsKey = "MVCTool_WebGLBuildSelected",
                }
            },
            {
                BuildTarget.StandaloneOSX,
                new BuildTargetData {
                    Directory = $"{Application.streamingAssetsPath}/Mac",
                    DisplayName = "Mac",
                    EditorPrefsKey = "MVCTool_MacBuildSelected",
                }
            },
        };
        
        private static Dictionary<PrefabBuildTargetPair, string> _builtPrefabManifests = new Dictionary<PrefabBuildTargetPair, string>();
        public static ReadOnlyDictionary<PrefabBuildTargetPair, string> BuiltPrefabManifests => new ReadOnlyDictionary<PrefabBuildTargetPair, string>(_builtPrefabManifests);
        public static PrefabBuildOutputData BuiltAvatarPrefabData { get; private set; } = null;

        public static void CreateMissingBuildTargetDirectories()
        {
            foreach (var target in SupportedBuildTargets)
            {
                if (!Directory.Exists(target.Value.Directory))
                {
                    Directory.CreateDirectory(target.Value.Directory);
                    Debug.Log($"Created missing build target directory: {target.Value.Directory}");
                }
            }
        }

        /// <summary>
        /// Gets the currently selected build targets based on the build modules installed in the editor.
        /// </summary>
        public static HashSet<BuildTarget> GetAvailableBuildTargets()
        {
            HashSet<BuildTarget> availableTargets = new HashSet<BuildTarget>();
            foreach (BuildTarget target in SupportedBuildTargets.Keys)
            {
                BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);

                if (BuildPipeline.IsBuildTargetSupported(group, target))
                {
                    availableTargets.Add(target);
                }
            }
            return availableTargets;
        }

        private static AssetBundleManifest BuildAsset(string assetPath, BuildTarget buildTarget)
        {
            if(!SupportedBuildTargets.ContainsKey(buildTarget))
                throw new System.Exception($"Build target {buildTarget} is not supported by MVCTool for asset bundling.");

            if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
                throw new System.Exception($"Asset path is invalid or does not exist: {assetPath}");

            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            if(importer == null)
                throw new System.Exception($"AssetImporter for path {assetPath} is null.");

            string tempBundleName = $"{Path.GetFileNameWithoutExtension(assetPath).ToLower()}.bin";
            importer.assetBundleName = tempBundleName;
            importer.SaveAndReimport();

            AssetBundleBuild build = new AssetBundleBuild
            {
                assetBundleName = tempBundleName,
                assetNames = new string[] { assetPath }
            };

            CreateMissingBuildTargetDirectories();

            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                SupportedBuildTargets[buildTarget].Directory,
                new AssetBundleBuild[] { build },
                BuildAssetBundleOptions.ChunkBasedCompression,
                buildTarget
            );

            importer.assetBundleName = null;
            importer.SaveAndReimport();

            Debug.Log($"Finished building asset bundle for {assetPath} as {tempBundleName} on {buildTarget}.");

            return manifest;
        }

        public static void BuildPrefabs(List<GameObject> prefabs, HashSet<BuildTarget> buildTargets)
        {
            Debug.Log($"Building {prefabs.Count} prefabs for targets: {string.Join(", ", buildTargets)}");

            foreach (GameObject prefab in prefabs)
            {
                string prefabPath = AssetDatabase.GetAssetPath(prefab);

                int successfullyBuiltCount = 0;
                foreach (BuildTarget target in buildTargets)
                {
                    AssetBundleManifest manifest = BuildAsset(prefabPath, target);
                    if (manifest == null)
                    {
                        Debug.LogError($"Failed to build asset bundle for {prefabPath} on {target}.");
                        continue;
                    }

                    PrefabBuildTargetPair key = new PrefabBuildTargetPair
                    {
                        Prefab = prefab,
                        BuildTarget = target
                    };

                    string bundleName = manifest.GetAllAssetBundles().FirstOrDefault();

                    if(_builtPrefabManifests.ContainsKey(key))
                    {
                        Debug.LogWarning($"Asset bundle for {prefabPath} on {target} already exists. Overwriting manifest." +
                                         $" Previous manifest: {_builtPrefabManifests[key]}");
                        _builtPrefabManifests[key] = bundleName;
                    }
                    else
                    {
                        _builtPrefabManifests.Add(key, bundleName);
                    }

                    successfullyBuiltCount++;
                }

                if (successfullyBuiltCount == 0)
                {
                    Debug.LogError($"Failed to build any asset bundles for prefab: {prefabPath}");
                    continue;
                }
            }
        }

        public static void RemoveBuiltPrefab(GameObject prefab)
        {
            // Find and remove all matching keys from the manifest dictionary
            var keysToRemove = _builtPrefabManifests
                .Where(kv => kv.Key.Prefab == prefab)
                .Select(kv => kv.Key)
                .ToList(); // avoid modifying the collection while iterating

            foreach (var key in keysToRemove)
            {
                _builtPrefabManifests.Remove(key);
            }

            if (keysToRemove.Count > 0)
            {
                Debug.Log($"Removed prefab '{prefab.name}' from {_builtPrefabManifests} for {keysToRemove.Count} target(s).");
            }
            else
            {
                Debug.LogWarning($"Prefab '{prefab.name}' was not found in built prefab manifests.");
            }
        }

        public static void ClearBuiltPrefabs()
        {
            _builtPrefabManifests.Clear();
        }

        public static List<GameObject> GetBuiltPrefabs()
        {
            return _builtPrefabManifests.Keys
                .Select(kv => kv.Prefab)
                .Distinct()
                .ToList();
        }

        public static void BuildAvatarPrefab(GameObject avatarPrefab, HashSet<BuildTarget> buildTargets)
        {
            if(avatarPrefab == null)
            {
                Debug.LogError("Avatar prefab is null. Cannot build.");
                return;
            }

            if (!avatarPrefab.GetComponent<MVCAvatar>())
            {
                Debug.LogError("The provided prefab does not contain an MVCAvatar component.");
                return;
            }

            Debug.Log($"Building avatar prefab: {avatarPrefab.name}");

            List<BuildOutputData> buildOutputDataList = new List<BuildOutputData>();

            string prefabPath = AssetDatabase.GetAssetPath(avatarPrefab);

            foreach (BuildTarget target in buildTargets)
            {
                AssetBundleManifest manifest = BuildAsset(prefabPath, target);
                if (manifest == null)
                {
                    Debug.LogError($"Failed to build avatar asset bundle for {prefabPath} on {target}.");
                    continue;
                }

                BuildOutputData manifestData = new BuildOutputData
                {
                    BundleName = manifest.GetAllAssetBundles().FirstOrDefault(), // Only 1 asset per bundle so get first one
                    BuildTarget = target,
                };
                buildOutputDataList.Add(manifestData);
            }

            BuiltAvatarPrefabData = new PrefabBuildOutputData(avatarPrefab, buildOutputDataList);
        }

        public static void ClearBuiltAvatarPrefabData()
        {
            BuiltAvatarPrefabData = null;
        }

        private static string ValidateUpload()
        {
            if (string.IsNullOrEmpty(LoginApi.BaseUrl))
                return "Base URL is not set. Please set the base URL in the Login tab.";

            if (string.IsNullOrEmpty(LoginApi.BearerToken))
                return "Please login first.";

            return null;
        }

        public static async UniTask UploadBuiltAvatarPrefab()
        {
            string validationError = ValidateUpload();
            if (validationError != null)
            {
                Debug.LogError(validationError);
                throw new System.Exception(validationError);
            }

            string targetUrl = LoginApi.CreateTargetUrl("uploadAvatar");

            try
            {
                foreach (BuildOutputData buildOutputData in BuiltAvatarPrefabData.BuildOutputDataList)
                {
                    BuildTargetData buildTargetData = SupportedBuildTargets[buildOutputData.BuildTarget];

                    string bundleName = buildOutputData.BundleName;
                    string path = $"{buildTargetData.Directory}/{bundleName}";
                    if (!File.Exists(path))
                    {
                        Debug.LogError($"Asset bundle file does not exist at path: {path}, skipping build target {buildTargetData.DisplayName}");
                        continue;
                    }

                    string platform = buildTargetData.DisplayName;

                    WWWForm form = new WWWForm();
                    form.AddField("platform", platform);
                    byte[] bytes = File.ReadAllBytes(path);
                    form.AddBinaryData("bundle", bytes, bundleName);
                    form.AddField("maxContentLength", "Infinity");
                    form.AddField("maxBodyLength", "Infinity");

                    UnityWebRequest request = await LoginApi.AuthenticatedPost(targetUrl, form);

                    if (request.result != UnityWebRequest.Result.Success)
                        Debug.LogError(request.error);
                    else
                        Debug.Log($"Finished uploading avatar {bundleName} for {platform} to {targetUrl}");
                }
                Debug.Log($"Finished uploading avatar asset bundle to {targetUrl}.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"UploadAssetBundles failed: {e}");
                throw;
            }
        }

        public static async UniTask UploadBuiltPrefabsToChannel(string channelID)
        {
            string validationError = ValidateUpload();
            if (validationError != null)
            {
                Debug.LogError(validationError);
                throw new System.Exception(validationError);
            }

            if (string.IsNullOrEmpty(channelID))
            {
                Debug.LogError("Channel ID is not set. Please provide a valid channel ID.");
                throw new System.Exception("Channel ID is not set.");
            }

            string targetUrl = LoginApi.CreateTargetUrl("uploadAssetToChannel");

            try
            {
                foreach(var kvp in _builtPrefabManifests)
                {
                    BuildTarget buildTarget = kvp.Key.BuildTarget;
                    BuildTargetData buildTargetData = SupportedBuildTargets[buildTarget];

                    string bundleName = kvp.Value;
                    string path = $"{buildTargetData.Directory}/{bundleName}";
                    if (!File.Exists(path))
                    {
                        Debug.LogError($"Asset bundle file does not exist at path: {path}, skipping build target {buildTargetData.DisplayName}");
                        continue;
                    }

                    string platform = buildTargetData.DisplayName;

                    WWWForm form = new WWWForm();
                    form.AddField("platform", platform);
                    byte[] bytes = File.ReadAllBytes(path);
                    form.AddBinaryData("bundle", bytes, bundleName);
                    form.AddField("name", bundleName);
                    form.AddField("uniqueID", channelID);
                    form.AddField("maxContentLength", "Infinity");
                    form.AddField("maxBodyLength", "Infinity");

                    UnityWebRequest request = await LoginApi.AuthenticatedPost(targetUrl, form);

                    if (request.result != UnityWebRequest.Result.Success)
                        Debug.LogError(request.error);
                    else
                        Debug.Log($"Finished uploading {bundleName} for {platform} to {targetUrl}");
                }
                Debug.Log($"Finished uploading built asset bundles to {LoginApi.BaseUrl}.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"UploadAssetBundles failed: {e}");
                throw;
            }
        }

        public static async UniTask<List<(int id, string name)>> ListAssetsFromChannel(string channelID)
        {
            if (string.IsNullOrEmpty(channelID))
            {
                Debug.LogError("Channel ID is required.");
                return new();
            }

            string targetUrl = LoginApi.CreateTargetUrl($"getAssetsForChannel?uniqueID={UnityWebRequest.EscapeURL(channelID)}");

            try
            {
                UnityWebRequest request = await LoginApi.AuthenticatedGet(targetUrl);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to get assets: {request.error}");
                    return new();
                }

                string json = request.downloadHandler.text;
                JArray array = JArray.Parse(json);

                var results = new List<(int id, string name)>();

                foreach (var item in array)
                {
                    int id = item.Value<int>("id");
                    string name = item.Value<string>("name");

                    if (!string.IsNullOrEmpty(name))
                        results.Add((id, name));
                }

                return results;
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception in ListContentFromChannel: {e}");
                return new();
            }
        }

        public static async UniTask DeleteAsset(string assetID)
        {
            if (string.IsNullOrEmpty(assetID))
            {
                Debug.LogError("Asset ID is not provided.");
                throw new System.Exception("Asset ID is required to delete content.");
            }

            string targetUrl = LoginApi.CreateTargetUrl("deleteAsset");

            try
            {
                WWWForm form = new WWWForm();
                form.AddField("id", assetID);

                UnityWebRequest request = await LoginApi.AuthenticatedPost(targetUrl, form);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Delete failed: {request.error}");
                    Debug.LogError($"Server response: {request.downloadHandler.text}");
                }
                else
                {
                    Debug.Log($"Successfully deleted asset with ID: {assetID}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"DeleteAssetFromChannel failed: {e}");
                throw;
            }
        }
    }

    public struct BuildTargetData
    {
        public string Directory;
        public string DisplayName;
        public string EditorPrefsKey;
    }

    public struct PrefabBuildTargetPair : IEquatable<PrefabBuildTargetPair>
    {
        public GameObject Prefab;
        public BuildTarget BuildTarget;

        public bool Equals(PrefabBuildTargetPair other)
        {
            return Prefab == other.Prefab && BuildTarget == other.BuildTarget;
        }

        // Object level equality
        public override bool Equals(object obj)
        {
            return obj is PrefabBuildTargetPair other && Equals(other);
        }

        // For dictionary hashing
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Prefab != null ? Prefab.GetHashCode() : 0) * 397) ^ (int)BuildTarget;
            }
        }

        public override string ToString() => $"{Prefab?.name} ({BuildTarget})";
    }

    public class PrefabBuildOutputData
    {
        public GameObject Prefab;
        public List<BuildOutputData> BuildOutputDataList;

        public PrefabBuildOutputData(GameObject prefab, List<BuildOutputData> buildOutputDataList)
        {
            Prefab = prefab;
            BuildOutputDataList = buildOutputDataList;
        }
    }

    public struct BuildOutputData
    {
        public string BundleName;
        public BuildTarget BuildTarget;
    }
}
