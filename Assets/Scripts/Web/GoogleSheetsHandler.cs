using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class GoogleSheetsHandler : MonoBehaviour
{
    [SerializeField] private string webAppUrl = "https://script.google.com/macros/s/AKfycbwgty4GsgJ1WubDogn9jJjNTjj2WZI6LJiw3ZchtKF-gZf1t2uJLoXHTaJ6VVsQx3K5/exec"; // Replace with your web app URL

    public event System.Action<List<VideoSelection>> OnDataRetrieved;

    // Method to record video selection
    public void RecordVideoSelection(int npcIndex)
    {
        StartCoroutine(PostDataCoroutine(npcIndex));
    }

    private IEnumerator PostDataCoroutine(int npcIndex)
    {
        // Create the form data
        WWWForm form = new WWWForm();
        form.AddField("npcIndex", npcIndex.ToString());
        form.AddField("selectionCount", "1");

        UnityWebRequest request = UnityWebRequest.Post(webAppUrl, form);
        // No need to set the content type; UnityWebRequest.Post handles it

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error posting data: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
        else
        {
            Debug.Log("Data posted successfully: " + request.downloadHandler.text);
        }
    }

    // Method to get all video selections
    public void GetAllVideoSelections()
    {
        StartCoroutine(GetDataCoroutine());
    }

    private IEnumerator GetDataCoroutine()
    {
        UnityWebRequest request = UnityWebRequest.Get(webAppUrl);
        request.timeout = 30;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error getting data: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
        else
        {
            string jsonData = request.downloadHandler.text;
            Debug.Log("Data retrieved: " + jsonData);
            // Parse and process the data
            ProcessRetrievedData(jsonData);
        }
    }

    private void ProcessRetrievedData(string jsonData)
    {
        // Deserialize JSON data
        List<VideoSelection> videoSelections = JsonUtility.FromJson<VideoSelectionList>("{\"items\":" + jsonData + "}").items;

        OnDataRetrieved?.Invoke(videoSelections);
    }

    [System.Serializable]
    public class VideoSelection
    {
        public string NPCIndex;
        public string NPCName;
        public string SelectionCount;
    }

    [System.Serializable]
    public class VideoSelectionList
    {
        public List<VideoSelection> items;
    }
}