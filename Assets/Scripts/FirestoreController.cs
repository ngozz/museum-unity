using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using System.IO;

public class FirestoreController : MonoBehaviour
{
    public static event Action<string> OnNewImageDownloaded; // Define the event

    private string databaseURL = "https://firestore.googleapis.com/v1/projects/museum-d3ac7/databases/(default)/documents/images?orderBy=timestamp asc";
    [SerializeField]
    private float timeInterval = 30.0f; // Time interval in seconds
    private DateTime lastTimestamp = DateTime.MinValue;
    [SerializeField]
    private bool clearLastTimestamp = false;

    void Start()
    {
        if (clearLastTimestamp)
        {
            PlayerPrefs.DeleteKey("LastTimestamp");
        }
        else if (PlayerPrefs.HasKey("LastTimestamp"))
        {
            lastTimestamp = DateTime.FromBinary(Convert.ToInt64(PlayerPrefs.GetString("LastTimestamp")));
        }

        InvokeRepeating("CallAPI", 0.0f, timeInterval);
    }

    void CallAPI()
    {
        StartCoroutine(GetRequest(databaseURL));
    }

    IEnumerator GetRequest(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(": Error: " + webRequest.error);
            }
            else
            {
                Debug.Log(":\nReceived: " + webRequest.downloadHandler.text);

                var documents = JSON.Parse(webRequest.downloadHandler.text)["documents"].AsArray;

                foreach (JSONNode document in documents)
                {
                    string timestampStr = document["fields"]["timestamp"]["timestampValue"];
                    if (!string.IsNullOrEmpty(timestampStr))
                    {
                        DateTime timestamp = DateTime.Parse(timestampStr);

                        if (DateTime.Compare(timestamp, lastTimestamp) > 0)
                        {
                            string imageUrl = document["fields"]["imageUrl"]["stringValue"];
                            string qrUrl = document["fields"]["qrUrl"]["stringValue"];

                            // Save images locally
                            string formattedTimestamp = timestamp.ToString("yyyy-MM-dd_HH-mm-ss");
                            StartCoroutine(DownloadAndSaveImage(imageUrl, "image_" + formattedTimestamp + ".png"));
                            StartCoroutine(DownloadAndSaveImage(qrUrl, "qr_" + formattedTimestamp + ".png"));

                            lastTimestamp = timestamp;

                            PlayerPrefs.SetString("LastTimestamp", lastTimestamp.ToBinary().ToString());
                        }
                        else
                        {
                            Debug.Log("No new images");
                        }
                    }
                }
            }
        }
    }

    IEnumerator DownloadAndSaveImage(string url, string fileName)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.ConnectionError)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                byte[] bytes = texture.EncodeToPNG();

                // Replace invalid characters
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }

                string filePath = Application.persistentDataPath + "/" + fileName;
                File.WriteAllBytes(filePath, bytes);
                Debug.Log("Saved image to: " + filePath);

                OnNewImageDownloaded?.Invoke(filePath); // Trigger the event
            }
        }
    }
}
