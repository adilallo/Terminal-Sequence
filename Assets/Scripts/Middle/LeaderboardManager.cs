using System.Collections.Generic;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    // Video names that can be set in the Unity Inspector
    [SerializeField] private List<string> videoNames;

    // Dictionary to track the number of times each video is selected
    private Dictionary<int, int> videoSelections = new Dictionary<int, int>();

    void Awake()
    {
        // Implementing singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Method to record video selection based on the video index
    public void RecordVideoSelection(int videoIndex)
    {
        if (videoIndex < 0 || videoIndex >= videoNames.Count)
        {
            Debug.LogWarning($"Invalid video index: {videoIndex}. Cannot record selection.");
            return;
        }

        if (videoSelections.ContainsKey(videoIndex))
        {
            videoSelections[videoIndex]++;
        }
        else
        {
            videoSelections[videoIndex] = 1;
        }

        Debug.Log($"Video \"{videoNames[videoIndex]}\" selected. Total selections: {videoSelections[videoIndex]}");
    }

    // Method to get all video selections (index -> selection count)
    public Dictionary<int, int> GetAllVideoSelections()
    {
        return new Dictionary<int, int>(videoSelections);
    }

    // Method to get the name of a video based on its index
    public string GetVideoName(int videoIndex)
    {
        if (videoIndex < 0 || videoIndex >= videoNames.Count)
        {
            return "Invalid Video Index";
        }
        return videoNames[videoIndex];
    }
}