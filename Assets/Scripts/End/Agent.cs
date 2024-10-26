using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
    [Header("Movement Parameters")]
    [SerializeField] private float cohesionRadius = 0.5f;
    [SerializeField] private float cohesionStrength = 0.5f;
    [SerializeField] private float separationRadius = 0.5f;
    [SerializeField] private float separationStrength = 0.5f;
    [SerializeField] private float alignmentRadius = 0.5f;
    [SerializeField] private float alignmentStrength = 0.5f;
    [SerializeField] private float agility = 0.5f;
    [SerializeField] private float maxSpeed = 3f;

    // Public properties to allow ManageSwarm to set these values
    public float CohesionRadius
    {
        get => cohesionRadius;
        set => cohesionRadius = Mathf.Max(0f, value); // Ensure non-negative
    }

    public float CohesionStrength
    {
        get => cohesionStrength;
        set => cohesionStrength = Mathf.Max(0f, value);
    }

    public float SeparationRadius
    {
        get => separationRadius;
        set => separationRadius = Mathf.Max(0f, value);
    }

    public float SeparationStrength
    {
        get => separationStrength;
        set => separationStrength = Mathf.Max(0f, value);
    }

    public float AlignmentRadius
    {
        get => alignmentRadius;
        set => alignmentRadius = Mathf.Max(0f, value);
    }

    public float AlignmentStrength
    {
        get => alignmentStrength;
        set => alignmentStrength = Mathf.Max(0f, value);
    }

    public float Agility
    {
        get => agility;
        set => agility = Mathf.Clamp(value, 0f, 1f); // Assuming agility is between 0 and 1
    }

    public float MaxSpeed
    {
        get => maxSpeed;
        set => maxSpeed = Mathf.Max(0f, value);
    }

    // Cached references
    private Transform cachedTransform;

    // Movement variables
    private Vector3 velocity;
    private Vector3 acceleration;

    // Reference to all agents
    private List<Agent> allAgents = new List<Agent>();

    void Awake()
    {
        // Cache the transform component for performance
        cachedTransform = transform;

        // Initialize velocity
        velocity = Vector3.zero;

        // Set initial rotation to 180 degrees around Y-axis using Euler angles
        cachedTransform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    void Update()
    {
        CalculateAcceleration();
        UpdateVelocity();
        UpdatePosition();
    }

    /// <summary>
    /// Sets the reference to all agents.
    /// Should be called once after all agents are instantiated.
    /// </summary>
    /// <param name="agents">List of all Agent instances.</param>
    public void SetAllAgents(List<Agent> agents)
    {
        if (agents == null || agents.Count == 0)
        {
            Debug.LogError("SetAllAgents called with null or empty list.");
            return;
        }

        allAgents = agents;
    }

    /// <summary>
    /// Cohesion behavior: Steer towards the average position of agents outside a certain radius.
    /// </summary>
    /// <param name="radiusSquared">Squared detection radius.</param>
    /// <param name="strength">Strength of the cohesion force.</param>
    /// <returns>Acceleration vector for cohesion.</returns>
    private Vector3 Cohesion(float radiusSquared, float strength)
    {
        Vector3 averagePosition = Vector3.zero;
        int count = 0;

        for (int i = 0; i < allAgents.Count; i++)
        {
            Agent agent = allAgents[i];
            if (agent == this) continue;

            Vector3 diff = agent.cachedTransform.position - cachedTransform.position;
            float distanceSquared = diff.sqrMagnitude;

            if (distanceSquared > radiusSquared)
            {
                averagePosition += diff;
                count++;
            }
        }

        if (count > 0)
        {
            averagePosition /= count;
            Vector3 direction = averagePosition.normalized * strength;
            return direction;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Separation behavior: Steer to avoid crowding agents within a certain radius.
    /// </summary>
    /// <param name="radiusSquared">Squared detection radius.</param>
    /// <param name="strength">Strength of the separation force.</param>
    /// <returns>Acceleration vector for separation.</returns>
    private Vector3 Separation(float radiusSquared, float strength)
    {
        Vector3 force = Vector3.zero;
        int count = 0;

        for (int i = 0; i < allAgents.Count; i++)
        {
            Agent agent = allAgents[i];
            if (agent == this) continue;

            Vector3 diff = cachedTransform.position - agent.cachedTransform.position;
            float distanceSquared = diff.sqrMagnitude;

            if (distanceSquared < radiusSquared && distanceSquared > 0f)
            {
                // Weight the force by the inverse of distance to prioritize closer agents
                force += diff.normalized / Mathf.Sqrt(distanceSquared);
                count++;
            }
        }

        if (count > 0)
        {
            force /= count;
            force *= strength;
        }

        return force;
    }

    /// <summary>
    /// Alignment behavior: Steer towards the average velocity of nearby agents within a certain radius.
    /// </summary>
    /// <param name="radiusSquared">Squared detection radius.</param>
    /// <param name="strength">Strength of the alignment force.</param>
    /// <returns>Acceleration vector for alignment.</returns>
    private Vector3 Alignment(float radiusSquared, float strength)
    {
        Vector3 averageVelocity = Vector3.zero;
        int count = 0;

        for (int i = 0; i < allAgents.Count; i++)
        {
            Agent agent = allAgents[i];
            if (agent == this) continue;

            Vector3 diff = agent.cachedTransform.position - cachedTransform.position;
            float distanceSquared = diff.sqrMagnitude;

            if (distanceSquared < radiusSquared)
            {
                averageVelocity += agent.velocity;
                count++;
            }
        }

        if (count > 0)
        {
            averageVelocity /= count;
            Vector3 desiredVelocity = averageVelocity.normalized * strength;
            return desiredVelocity;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Calculates the acceleration based on cohesion, separation, and alignment behaviors.
    /// </summary>
    private void CalculateAcceleration()
    {
        Vector3 cohesionForce = Cohesion(cohesionRadius * cohesionRadius, cohesionStrength);
        Vector3 separationForce = Separation(separationRadius * separationRadius, separationStrength);
        Vector3 alignmentForce = Alignment(alignmentRadius * alignmentRadius, alignmentStrength);

        // Combine the forces
        acceleration = (cohesionForce + separationForce + alignmentForce) / 3f;
    }

    /// <summary>
    /// Updates the velocity based on acceleration and agility.
    /// </summary>
    private void UpdateVelocity()
    {
        velocity += acceleration * agility;

        // Limit the velocity to maxSpeed
        if (velocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            velocity = velocity.normalized * maxSpeed;
        }
    }

    /// <summary>
    /// Updates the agent's position based on velocity.
    /// </summary>
    private void UpdatePosition()
    {
        cachedTransform.position += velocity * Time.deltaTime;
    }
}
