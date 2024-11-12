using System.Collections.Generic;
using UnityEngine;

public class ManageSwarm : MonoBehaviour
{
    [Header("Agent Configuration")]
    [SerializeField] private List<GameObject> agentPrefabs = new List<GameObject>();
    [SerializeField] private int spread = 50;
    [SerializeField] private int amount = 100;
    [SerializeField] private float rotationSpeed = 30f;

    [Header("Camera")]
    [SerializeField] private GameObject camRig;

    [Header("Agent Management")]
    [SerializeField] private List<Agent> allAgents = new List<Agent>();

    // Cached components
    private int prefabCount;

    // Cached input values
    private float normalizedMouseX;
    private float normalizedMouseY;

    void Start()
    {
        InitializeSwarm();
        prefabCount = agentPrefabs.Count;

        if (camRig == null)
        {
            Debug.LogError("Camera Rig is not assigned! Please assign it in the Inspector.");
        }

        if (agentPrefabs == null || agentPrefabs.Count == 0)
        {
            Debug.LogError("Agent Prefabs list is empty! Please assign prefabs in the Inspector.");
        }

        Debug.Log($"Total Agents Initialized: {allAgents.Count}");
    }

    void Update()
    {
        UpdateMouseInputs();
        UpdateCameraPosition();
        RotateAgents();
        UpdateAgentProperties();
    }

    #region Initialization

    /// <summary>
    /// Initializes the swarm by instantiating agents.
    /// </summary>
    private void InitializeSwarm()
    {
        if (agentPrefabs == null || agentPrefabs.Count == 0)
        {
            Debug.LogError("No agent prefabs assigned. Please assign at least one prefab.");
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            GameObject selectedPrefab = agentPrefabs[i % agentPrefabs.Count];
            Vector3 spawnPosition = Random.insideUnitSphere * spread;
            GameObject newAgent = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);

            Agent agentScript = newAgent.GetComponent<Agent>();
            if (agentScript != null)
            {
                allAgents.Add(agentScript);
                // Assuming SetAllAgents is necessary only once after all agents are instantiated
            }
            else
            {
                Debug.LogWarning($"Agent prefab at index {i % prefabCount} does not have an Agent component.");
            }
        }

        // After all agents are instantiated, assign the list to each agent
        foreach (Agent agent in allAgents)
        {
            agent.SetAllAgents(allAgents);
        }
    }

    #endregion

    #region Update Methods

    /// <summary>
    /// Updates normalized mouse input values.
    /// </summary>
    private void UpdateMouseInputs()
    {
        normalizedMouseX = Mathf.Clamp01(Input.mousePosition.x / Screen.width);
        normalizedMouseY = Mathf.Clamp01(Input.mousePosition.y / Screen.height);
    }

    /// <summary>
    /// Updates the camera position based on the average position of all agents.
    /// </summary>
    private void UpdateCameraPosition()
    {
        if (camRig == null || allAgents.Count == 0)
            return;

        Vector3 averagePosition = Vector3.zero;
        for (int i = 0; i < allAgents.Count; i++)
        {
            averagePosition += allAgents[i].transform.position;
        }
        averagePosition /= allAgents.Count;

        // Smoothly move the camera towards the average position
        camRig.transform.position = Vector3.MoveTowards(camRig.transform.position, averagePosition, 0.1f);
    }

    /// <summary>
    /// Rotates all agents around the Y-axis.
    /// </summary>
    private void RotateAgents()
    {
        if (allAgents.Count == 0)
            return;

        float rotationAmount = rotationSpeed * Time.deltaTime;
        for (int i = 0; i < allAgents.Count; i++)
        {
            allAgents[i].transform.Rotate(Vector3.up, rotationAmount, Space.Self);
        }
    }

    /// <summary>
    /// Updates cohesion, separation, and alignment properties of all agents based on mouse input.
    /// </summary>
    private void UpdateAgentProperties()
    {
        if (allAgents.Count == 0)
            return;

        float cohesionRadius = normalizedMouseX;
        float cohesionStrength = normalizedMouseY;
        float separationRadius = normalizedMouseX;
        float separationStrength = normalizedMouseY;
        float alignmentRadius = normalizedMouseX;
        float alignmentStrength = normalizedMouseY;

        for (int i = 0; i < allAgents.Count; i++)
        {
            Agent agent = allAgents[i];
            agent.CohesionRadius = cohesionRadius;
            agent.CohesionStrength = cohesionStrength;
           // agent.SeparationRadius = separationRadius;
            //agent.SeparationStrength = separationStrength;
            //agent.AlignmentRadius = alignmentRadius;
            //agent.AlignmentStrength = alignmentStrength;
        }
    }

    #endregion
}