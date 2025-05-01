using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Very small object-pool for Agent prefabs. </summary>
public class AgentPool : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public GameObject prefab;          // one of your 11 NPC models
        [Min(0)] public int preload = 1;   // how many of this model to warm-up
    }

    [SerializeField] private Entry[] variants;

    readonly List<Queue<GameObject>> pools = new();

    void Awake()
    {
        // one queue per variant
        foreach (var v in variants)
        {
            var q = new Queue<GameObject>();
            for (int i = 0; i < v.preload; i++)
                q.Enqueue(Create(v.prefab));
            pools.Add(q);
        }
    }

    GameObject Create(GameObject prefab)
    {
        var go = Instantiate(prefab, transform);
        go.SetActive(false);
        return go;
    }

    public Agent Get(int variant, Vector3 pos, Quaternion rot)
    {
        if (variant < 0 || variant >= pools.Count)
        { Debug.LogError("variant index out of range"); return null; }

        var q = pools[variant];
        var go = q.Count > 0 ? q.Dequeue() : Create(variants[variant].prefab);
        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);
        return go.GetComponent<Agent>();
    }

    public void Return(int variant, Agent a)
    {
        a.gameObject.SetActive(false);
        pools[variant].Enqueue(a.gameObject);
    }
}