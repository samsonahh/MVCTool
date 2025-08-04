using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using UnityEngine;
using System.Text;

namespace MVCTool
{
    public static class ContentManager
    {
        /// <summary>
        /// Returns a list of content tuples (ID, name) for a specific channel.
        /// </summary>
        /// <param name="channelID"></param>
        /// <returns></returns>
        public static async UniTask<List<(int id, string name)>> ListContentFromChannel(string channelID)
        {
            if (string.IsNullOrEmpty(channelID))
            {
                Debug.LogError("Channel ID is required.");
                return new();
            }

            string targetUrl = LoginApi.CreateTargetUrl($"getAllContentForChannel?uniqueID={UnityWebRequest.EscapeURL(channelID)}");

            try
            {
                UnityWebRequest request = await LoginApi.AuthenticatedGet(targetUrl);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to get content: {request.error}");
                    return new();
                }

                string json = request.downloadHandler.text;
                JArray array = JArray.Parse(json);

                var results = new List<(int id, string name)>();

                foreach (var item in array)
                {
                    int id = item.Value<int>("id");

                    var mediafileToken = item["mediafile"];
                    string name = null;

                    if (mediafileToken != null && mediafileToken.Type == JTokenType.Object)
                    {
                        name = mediafileToken["name"]?.Value<string>();
                    }

                    if (!string.IsNullOrEmpty(name))
                    {
                        results.Add((id, name));
                    }
                }

                return results;
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception in ListContentFromChannel: {e}");
                return new();
            }
        }

        /// <summary>
        /// Uploads a non-Unity file to a specific channel.
        /// Doesn't support web url uploads, only local file paths.
        /// </summary>
        public static async UniTask UploadContentToChannel(string channelID, string filePath)
        {
            if (string.IsNullOrEmpty(channelID))
            {
                Debug.LogError("Channel ID is not set. Please provide a valid channel ID.");
                throw new System.Exception("Channel ID is not set.");
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Debug.LogError($"File path is invalid or file does not exist: {filePath}");
                throw new System.Exception("File path is invalid.");
            }

            string targetUrl = LoginApi.CreateTargetUrl("uploadContentToChannel");

            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                string fileName = Path.GetFileName(filePath);

                WWWForm form = new WWWForm();

                form.AddBinaryData(fileName, fileBytes, fileName);
                form.AddField("uniqueID", channelID);
                form.AddField("published", "true");

                UnityWebRequest request = await LoginApi.AuthenticatedPost(targetUrl, form);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Upload failed: {request.error}");
                    Debug.LogError($"Server response: {request.downloadHandler.text}");
                }
                else
                    Debug.Log($"Successfully uploaded {fileName} to {targetUrl}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"UploadContentToChannel failed: {e}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a previously uploaded file from a channel by content ID.
        /// </summary>
        public static async UniTask DeleteContent(string contentID)
        {
            if (string.IsNullOrEmpty(contentID))
            {
                Debug.LogError("Content ID is not provided.");
                throw new System.Exception("Content ID is required to delete content.");
            }

            string targetUrl = LoginApi.CreateTargetUrl("deleteContent");

            try
            {
                WWWForm form = new WWWForm();
                form.AddField("id", contentID);

                UnityWebRequest request = await LoginApi.AuthenticatedPost(targetUrl, form);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Delete failed: {request.error}");
                    Debug.LogError($"Server response: {request.downloadHandler.text}");
                }
                else
                {
                    Debug.Log($"Successfully deleted content with ID: {contentID}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"DeleteContentFromChannel failed: {e}");
                throw;
            }
        }
    }
}
