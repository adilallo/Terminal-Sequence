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

    // Public properties for ManageSwarm to set values
    public float CohesionRadius
    {
        get => cohesionRadius;
        set => cohesionRadius = Mathf.Max(0f, value);
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
        set => agility = Mathf.Clamp(value, 0f, 1f);
    }

    public float MaxSpeed
    {
        get => maxSpeed;
        set => maxSpeed = Mathf.Max(0f, value);
    }

    // Target position for cohesion behavior (set by ManageSwarm)
    public Vector3 TargetPosition { get; set; }

    // Cached references
    private Transform cachedTransform;

    // Movement variables
    private Vector3 velocity;
    private Vector3 acceleration;

    // Reference to all agents
    private List<Agent> allAgents = new List<Agent>();

    void Awake()
    {
        cachedTransform = transform;
        velocity = Vector3.zero;
        cachedTransform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    void Update()
    {
        CalculateAcceleration();
        UpdateVelocity();
        UpdatePosition();
    }

    public void SetAllAgents(List<Agent> agents)
    {
        if (agents == null || agents.Count == 0)
        {
            Debug.LogError("SetAllAgents called with null or empty list.");
            return;
        }

        allAgents = agents;
    }

    private Vector3 Cohesion(float radiusSquared, float strength)
    {
        Vector3 targetDirection = TargetPosition - cachedTransform.position;
        float distanceSquared = targetDirection.sqrMagnitude;

        if (distanceSquared > radiusSquared)
        {
            return targetDirection.normalized * strength;
        }

        return Vector3.zero;
    }

    private Vector3 Separation(float radiusSquared, float strength)
    {
        Vector3 force = Vector3.zero;
        int count = 0;

        foreach (var agent in allAgents)
        {
            if (agent == this) continue;

            Vector3 diff = cachedTransform.position - agent.cachedTransform.position;
            float distanceSquared = diff.sqrMagnitude;

            if (distanceSquared < radiusSquared && distanceSquared > 0f)
            {
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

    private Vector3 Alignment(float radiusSquared, float strength)
    {
        Vector3 averageVelocity = Vector3.zero;
        int count = 0;

        foreach (var agent in allAgents)
        {
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
            return (averageVelocity.normalized * strength);
        }

        return Vector3.zero;
    }

    private void CalculateAcceleration()
    {
        Vector3 cohesionForce = Cohesion(cohesionRadius * cohesionRadius, cohesionStrength);
        Vector3 separationForce = Separation(separationRadius * separationRadius, separationStrength);
        Vector3 alignmentForce = Alignment(alignmentRadius * alignmentRadius, alignmentStrength);

        acceleration = (cohesionForce + separationForce + alignmentForce) / 3f;
    }

    private void UpdateVelocity()
    {
        velocity += acceleration * agility;

        if (velocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            velocity = velocity.normalized * maxSpeed;
        }
    }

    private void UpdatePosition()
    {
        cachedTransform.position += velocity * Time.deltaTime;
    }
}
