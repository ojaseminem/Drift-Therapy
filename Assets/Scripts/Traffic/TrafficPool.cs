using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TrafficPool : MonoBehaviour
{
    [SerializeField] TrafficAgent agentPrefab;
    [SerializeField] int initialSize = 6;
    [SerializeField] Transform container;

    readonly Stack<TrafficAgent> freeAgents = new Stack<TrafficAgent>();
    readonly HashSet<TrafficAgent> activeAgents = new HashSet<TrafficAgent>();

    public int FreeCount => freeAgents.Count;
    public int ActiveCount => activeAgents.Count;

    void Awake()
    {
        Prewarm();
    }

    public void Prewarm()
    {
        if (!agentPrefab)
            return;

        while (freeAgents.Count + activeAgents.Count < initialSize)
            freeAgents.Push(CreateInstance());
    }

    public TrafficAgent Acquire()
    {
        if (!agentPrefab)
            return null;

        if (freeAgents.Count == 0)
            freeAgents.Push(CreateInstance());

        var agent = freeAgents.Pop();
        activeAgents.Add(agent);
        agent.gameObject.SetActive(true);
        return agent;
    }

    public void Release(TrafficAgent agent)
    {
        if (!agent || !activeAgents.Remove(agent))
            return;

        agent.gameObject.SetActive(false);
        agent.transform.SetParent(container ? container : transform, false);
        freeAgents.Push(agent);
    }

    TrafficAgent CreateInstance()
    {
        var parent = container ? container : transform;
        var instance = Instantiate(agentPrefab, parent);
        instance.gameObject.SetActive(false);
        return instance;
    }
}
