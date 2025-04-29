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

    [Header("Swarm Behavior")]
    [SerializeField] private float cohesionRadius = 5f;  // Fixed radius for cohesion
    [SerializeField] private float cohesionStrength = 1f;  // Fixed strength for cohesion

    [Header("Agent Management")]
    [SerializeField] private List<Agent> allAgents = new List<Agent>();

    private Vector3 mouseWorldPosition;

    void Start()
    {
        if (camRig == null)
        {
            Debug.LogError("Camera Rig is not assigned! Please assign it in the Inspector.");
        }

        if (agentPrefabs == null || agentPrefabs.Count == 0)
        {
            Debug.LogError("Agent Prefabs list is empty! Please assign prefabs in the Inspector.");
        }
    }

    private void OnDisable()
    {
        ClearSwarm();
    }

    void Update()
    {
        UpdateMousePosition();
        UpdateCameraPosition();
        RotateAgents();
        UpdateAgentTarget();
    }

    #region Initialization

    public void InitializeSwarm()
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject selectedPrefab = agentPrefabs[i % agentPrefabs.Count];
            Vector3 spawnPosition = Random.insideUnitSphere * spread;
            GameObject newAgent = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);

            MeshRenderer meshRenderer = newAgent.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.UseProxyVolume;
            }
            else
            {
                // Try to find inside children
                meshRenderer = newAgent.GetComponentInChildren<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.UseProxyVolume;
                }
            }

            Agent agentScript = newAgent.GetComponent<Agent>();
            if (agentScript != null)
            {
                allAgents.Add(agentScript);
            }
            else
            {
                Debug.LogWarning($"Agent prefab at index {i % agentPrefabs.Count} does not have an Agent component.");
            }
        }

        foreach (Agent agent in allAgents)
        {
            agent.SetAllAgents(allAgents);
            agent.CohesionRadius = cohesionRadius;  // Set fixed radius
            agent.CohesionStrength = cohesionStrength;  // Set fixed strength
        }
    }

    private void ClearSwarm()
    {
        foreach (Agent agent in allAgents)
        {
            if (agent != null)
            {
                Destroy(agent.gameObject);
            }
        }
        allAgents.Clear();
    }

    #endregion

    #region Update Methods

    private void UpdateMousePosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(ray, out float distance))
        {
            mouseWorldPosition = ray.GetPoint(distance);
        }
    }

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

        camRig.transform.position = Vector3.MoveTowards(camRig.transform.position, averagePosition, 0.1f);
    }

    private void RotateAgents()
    {
        if (allAgents.Count == 0)
            return;

        float rotationAmount = rotationSpeed * Time.deltaTime;
        foreach (Agent agent in allAgents)
        {
            agent.transform.Rotate(Vector3.up, rotationAmount, Space.Self);
        }
    }

    private void UpdateAgentTarget()
    {
        foreach (Agent agent in allAgents)
        {
            agent.TargetPosition = mouseWorldPosition;  // Pass the mouse position as a target
        }
    }

    #endregion
}
