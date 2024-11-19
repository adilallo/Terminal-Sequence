using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class LocalSelectionTracker : MonoBehaviour
{
    [SerializeField] private string fileName = "videoSelections.txt";

    private string filePath;

    public event System.Action<List<VideoSelection>> OnDataRetrieved;

    void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, fileName);

        // Create the file if it doesn't exist
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "");
        }

        Debug.Log("Persistent Data Path: " + Application.persistentDataPath);
    }

    // Method to record video selection
    public void RecordVideoSelection(int npcIndex)
    {
        Dictionary<int, int> selections = LoadSelections();

        if (selections.ContainsKey(npcIndex))
        {
            selections[npcIndex]++;
        }
        else
        {
            selections[npcIndex] = 1;
        }

        SaveSelections(selections);
    }

    // Method to get all video selections
    public void GetAllVideoSelections()
    {
        Dictionary<int, int> selections = LoadSelections();
        List<VideoSelection> videoSelections = new List<VideoSelection>();

        foreach (KeyValuePair<int, int> entry in selections)
        {
            videoSelections.Add(new VideoSelection
            {
                NPCIndex = entry.Key.ToString(),
                SelectionCount = entry.Value.ToString()
            });
        }

        Debug.Log("GetAllVideoSelections called, videoSelections count: " + videoSelections.Count);
        foreach (var selection in videoSelections)
        {
            Debug.Log($"NPCIndex: {selection.NPCIndex}, SelectionCount: {selection.SelectionCount}");
        }

        OnDataRetrieved?.Invoke(videoSelections);
    }

    // Load selections from the text file
    private Dictionary<int, int> LoadSelections()
    {
        Dictionary<int, int> selections = new Dictionary<int, int>();

        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    string[] parts = line.Split(',');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int npcIndex) && int.TryParse(parts[1], out int selectionCount))
                    {
                        selections[npcIndex] = selectionCount;
                    }
                }
            }
        }

        return selections;
    }

    // Save selections to the text file
    private void SaveSelections(Dictionary<int, int> selections)
    {
        List<string> lines = new List<string>();

        foreach (KeyValuePair<int, int> entry in selections)
        {
            lines.Add($"{entry.Key},{entry.Value}");
        }

        File.WriteAllLines(filePath, lines);
    }

    [System.Serializable]
    public class VideoSelection
    {
        public string NPCIndex;
        public string SelectionCount;
    }
}
