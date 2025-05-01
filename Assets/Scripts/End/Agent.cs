using System.Collections.Generic;
using UnityEngine;

/// <summary> Same behaviour, but NO Update; called from ManageSwarmOptimized.Tick(). </summary>
[RequireComponent(typeof(MeshRenderer))]
public class Agent : MonoBehaviour
{
    /* --- tunables (public for editor) --- */
    [Range(0f, 20f)] public float cohesionRadius = 5f;
    [Range(0f, 5f)] public float cohesionStrength = 1f;
    [Range(0f, 20f)] public float separationRadius = 3f;
    [Range(0f, 10f)] public float separationStrength = 1f;
    [Range(0f, 20f)] public float alignmentRadius = 6f;
    [Range(0f, 10f)] public float alignmentStrength = 1f;
    [Range(0f, 1f)] public float agility = 0.5f;
    [Range(0f, 20f)] public float maxSpeed = 4f;

    /* --- runtime fields --- */
    [HideInInspector] public Vector3 velocity;
    [HideInInspector] public Vector3 target;
    [HideInInspector] public Vector3 mouse;
    [HideInInspector] public float mStr = 4f;

    Transform tf; List<Agent> all;

    public void Init(List<Agent> list) { tf = transform; all = list; velocity = Vector3.zero; }
    public void DeInit() { all = null; }          // called when returned to pool

    /* ------------------------------------------------------- */
    public void Tick(float dtSqr, float dt)
    {
        Vector3 accel = (Cohesion() + Separation() + Alignment()) / 3f;
        velocity += accel * agility;

        float maxSqr = maxSpeed * maxSpeed;
        if (velocity.sqrMagnitude > maxSqr)
            velocity = velocity.normalized * maxSpeed;

        tf.position += velocity * dt;
        tf.Rotate(Vector3.up, 30f * dt, Space.Self);   // constant spin
    }

    Vector3 Cohesion()
    {
        Vector3 dir = mouse - tf.position;
        float d2 = dir.sqrMagnitude + 0.001f;
        return (dir / d2) * mStr;
    }

    Vector3 Separation()
    {
        Vector3 force = Vector3.zero; int count = 0;
        float radSqr = separationRadius * separationRadius;

        foreach (var a in all)
        {
            if (a == this) continue;
            Vector3 diff = tf.position - a.tf.position;
            float d2 = diff.sqrMagnitude;
            if (d2 < radSqr && d2 > 0f)
            { force += diff.normalized / Mathf.Sqrt(d2); count++; }
        }
        if (count == 0) return Vector3.zero;
        force = force / count * separationStrength;
        return force;
    }

    Vector3 Alignment()
    {
        Vector3 avgV = Vector3.zero; int count = 0;
        float radSqr = alignmentRadius * alignmentRadius;

        foreach (var a in all)
        {
            if (a == this) continue;
            Vector3 diff = a.tf.position - tf.position;
            if (diff.sqrMagnitude < radSqr)
            { avgV += a.velocity; count++; }
        }
        if (count == 0) return Vector3.zero;
        avgV /= count;
        return avgV.normalized * alignmentStrength;
    }
}
