using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    [Header("NPC Settings")]
    [SerializeField] private List<GameObject> npcPrefabs; // List of NPC prefabs
    [SerializeField] private float spacing = 0.1f;

    [Header("Canvas Settings")]
    [SerializeField] private float canvasWidth = 600f;
    [SerializeField] private float canvasHeight = 960f;

    [Header("Hover Settings")]
    [SerializeField] private float hoverAmplitude = 10f;
    [SerializeField] private float hoverFrequency = 1f;

    [Header("Camera Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Vector3 cameraOffset;

    private List<GameObject> npcInstances = new List<GameObject>();

    private GoogleSheetsHandler googleSheetsHandler;

    private void Start()
    {
        // Find or assign the GoogleSheetsHandler
        googleSheetsHandler = FindObjectOfType<GoogleSheetsHandler>();
        if (googleSheetsHandler == null)
        {
            Debug.LogError("GoogleSheetsHandler not found in the scene.");
            return;
        }

        // Subscribe to the OnDataRetrieved event
        googleSheetsHandler.OnDataRetrieved += OnDataRetrieved;

        // Start retrieving data
        googleSheetsHandler.GetAllVideoSelections();
    }

    private void OnDataRetrieved(List<GoogleSheetsHandler.VideoSelection> videoSelections)
    {
        // Instantiate and position NPCs based on the data
        InstantiateAndPositionNPCs(videoSelections);
    }

    private void InstantiateAndPositionNPCs(List<GoogleSheetsHandler.VideoSelection> videoSelections)
    {
        // Ensure we have NPC prefabs assigned
        if (npcPrefabs == null || npcPrefabs.Count == 0)
        {
            Debug.LogError("NPC Prefabs list is empty.");
            return;
        }

        int npcCount = npcPrefabs.Count;

        // Calculate starting x position to center the NPCs
        float totalWidth = (npcCount - 1) * spacing;
        float startX = -totalWidth / 2f;

        // Instantiate NPCs
        for (int i = 0; i < npcCount; i++)
        {
            GameObject npcPrefab = npcPrefabs[i];
            if (npcPrefab == null)
            {
                Debug.LogWarning($"NPC prefab at index {i} is null.");
                continue;
            }

            GameObject npc = Instantiate(npcPrefab);
            npcInstances.Add(npc);

            // Position the NPCs in a row
            npc.transform.localScale *= 0.75f;
            float xPos = startX + i * spacing;
            npc.transform.position = new Vector3(xPos, 0f, 0f);

            // Set the NPC's height based on the click data
            int selectionCount = GetSelectionCountForNPC(videoSelections, i);
            float height = CalculateHeight(selectionCount);
            npc.transform.position += new Vector3(0f, height, 0f);

            // Add a Hovering script to make it hover
            Hovering hovering = npc.AddComponent<Hovering>();
            float upwardSpeed = CalculateUpwardSpeed(selectionCount);
            hovering.SetHoverParameters(hoverAmplitude, hoverFrequency, upwardSpeed);
        }
    }

    private int GetSelectionCountForNPC(List<GoogleSheetsHandler.VideoSelection> videoSelections, int npcIndex)
    {
        // Find the selection count for the NPC with the matching index
        foreach (var selection in videoSelections)
        {
            if (int.TryParse(selection.NPCIndex, out int index))
            {
                if (index == npcIndex)
                {
                    if (int.TryParse(selection.SelectionCount, out int count))
                    {
                        return count;
                    }
                }
            }
        }
        return 0; // Default to 0 if not found
    }

    private float CalculateHeight(int selectionCount)
    {
        // Map the selection count to a height value
        // Higher selection count means higher height
        float height = selectionCount * 1f; // Adjust the multiplier as needed
        return height;
    }

    private float CalculateUpwardSpeed(int selectionCount)
    {
        // Map the selection count to an upward speed value
        // Higher selection count means higher speed
        float speed = selectionCount * 0.1f; // Adjust the multiplier as needed
        return speed;
    }

    private void Update()
    {
       // UpdateCameraPosition();
    }

    private void UpdateCameraPosition()
    {
        if (npcInstances.Count == 0 || mainCamera == null)
            return;

        Vector3 averagePosition = Vector3.zero;
        foreach (var npc in npcInstances)
        {
            averagePosition += npc.transform.position;
        }
        averagePosition /= npcInstances.Count;

        Vector3 targetPosition = averagePosition + cameraOffset;
        Vector3 currentPosition = mainCamera.transform.position;
        mainCamera.transform.position = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime);
    }
}
