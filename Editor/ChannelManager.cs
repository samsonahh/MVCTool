using Cysharp.Threading.Tasks;
using System;
using UnityEngine.Networking;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MVCTool
{
    public static class ChannelManager
    {
        public static async UniTask CreateChannel(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Channel name is required.");
                return;
            }

            string targetUrl = LoginApi.CreateTargetUrl("createChannel");

            try
            {
                WWWForm form = new WWWForm();

                form.AddField("name", name);

                UnityWebRequest request = await LoginApi.AuthenticatedPost(targetUrl, form);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in CreateChannel: {ex}");
            }
        }

        public static async UniTask DeleteChannel(string uniqueID)
        {
            if (string.IsNullOrEmpty(uniqueID))
            {
                Debug.LogError("Channel ID is required.");
                return;
            }

            string targetUrl = LoginApi.CreateTargetUrl("deleteChannel");

            try
            {
                WWWForm form = new WWWForm();

                form.AddField("uniqueID", uniqueID);

                UnityWebRequest request = await LoginApi.AuthenticatedPost(targetUrl, form);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in DeleteChannel: {ex}");
            }
        }

        public static async UniTask<List<(string uniqueID, string name)>> GetMyChannels()
        {
            string targetUrl = LoginApi.CreateTargetUrl("getMyChannels");

            try
            {
                UnityWebRequest request = await LoginApi.AuthenticatedGet(targetUrl);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to get channels: {request.error}");
                    return new();
                }

                string json = request.downloadHandler.text;
                JArray array = JArray.Parse(json);

                var result = new List<(string uniqueID, string name)>();

                foreach (JToken token in array)
                {
                    string uniqueID = token["uniqueID"]?.ToString();
                    string name = token["name"]?.ToString();

                    if (!string.IsNullOrEmpty(uniqueID) && !string.IsNullOrEmpty(name))
                        result.Add((uniqueID, name));
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in GetMyChannels: {ex}");
                return new();
            }
        }
    }
}
