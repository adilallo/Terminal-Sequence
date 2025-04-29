using System;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    [Serializable]
    public class PoolPrefab
    {
        public string key;           // e.g. "Line", "NPCImage"
        public GameObject prefab;
        [Min(0)] public int preload = 0;
    }

    [SerializeField] private PoolPrefab[] prefabs;
    private readonly Dictionary<string, Queue<GameObject>> _pool = new();
    private readonly Dictionary<string, PoolPrefab> _prefabLookup = new();

    void Awake()
    {
        foreach (var p in prefabs)
        {
            _prefabLookup[p.key] = p;
            var q = new Queue<GameObject>();
            for (int i = 0; i < p.preload; i++)
            {
                var go = Instantiate(p.prefab, transform);
                go.SetActive(false);
                q.Enqueue(go);
            }
            _pool[p.key] = q;
        }
    }

    public GameObject Get(string key, Transform parent)
    {
        if (!_pool.TryGetValue(key, out var q) || !_prefabLookup.TryGetValue(key, out var def))
        {
            Debug.LogError($"Pool key '{key}' not found");
            return null;
        }

        var go = q.Count > 0 ? q.Dequeue() : Instantiate(def.prefab);
        go.transform.SetParent(parent, false);
        go.SetActive(true);
        return go;
    }

    public void Return(string key, GameObject go)
    {
        if (!_pool.TryGetValue(key, out var q))
        {
            Destroy(go); // no pool defined – just destroy
            return;
        }
        go.SetActive(false);
        q.Enqueue(go);
    }
}