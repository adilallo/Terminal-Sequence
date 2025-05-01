using System.Collections.Generic;
using UnityEngine;

public class ManageSwarm : MonoBehaviour
{
    [SerializeField] AgentPool pool;                 // drag the pool here
    [SerializeField] int spread = 40;
    [SerializeField, Range(0.1f, 10f)]
    float mouseStrength = 4f;
    [SerializeField] GameObject camRig;

    readonly List<Agent> agents = new();
    Plane ground = new Plane(Vector3.up, Vector3.zero);
    Vector3 target;

    void OnEnable()
    {
        // spawn exactly one of each prefab variant in the pool
        for (int v = 0; v < pool.transform.childCount; v++)
        {
            Vector3 pos = Random.insideUnitSphere * spread;
            Agent a = pool.Get(v, pos, Quaternion.identity);
            a.Init(agents);
            agents.Add(a);
        }
    }

    void OnDisable()
    {
        // return all live agents to their variant queue
        for (int i = 0; i < agents.Count; i++)
        {
            var a = agents[i];
            int variant = i;                     // 1-to-1 ordering
            a.DeInit();
            pool.Return(variant, a);
        }
        agents.Clear();
    }

    void Update()
    {
        Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (ground.Raycast(r, out float d))
            target = r.GetPoint(d);

        float dt = Time.deltaTime;
        float dtSq = dt * dt;

        foreach (var a in agents)
        {
            a.mouse = target;
            a.mStr = mouseStrength;
            a.Tick(dtSq, dt);
        }

        if (camRig && agents.Count > 0)
        {
            Vector3 c = Vector3.zero;
            foreach (var a in agents) c += a.transform.position;
            camRig.transform.position = Vector3.Lerp(camRig.transform.position, c / agents.Count, 0.1f);
        }
    }
}
