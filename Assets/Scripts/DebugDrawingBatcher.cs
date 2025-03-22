using System;
using System.Collections.Generic;
using UnityEngine;

public class DebugDrawingBatcher : MonoBehaviour
{
    private static DebugDrawingBatcher _instance = null;
    private readonly Dictionary<string, List<Action>> _batchedCalls = new();
    private HashSet<string> _pausedGroups = new HashSet<string>();
    public static void BatchCall(string group, Action action)
    {
        var instance = GetInstance();
        if(instance == null)
            return;

        if (instance._batchedCalls.ContainsKey(group))
            instance._batchedCalls[group].Add(action);
        else
            instance._batchedCalls.Add(group, new List<Action>(){action});
    }

    public static void ClearCalls(string group = "All")
    {
        if(group == "All")
            GetInstance()._batchedCalls.Clear();
        else
            GetInstance()._batchedCalls.Remove(group);
    }

    public static void PauseGroup(string group) => GetInstance()._pausedGroups.Add(group);
    public static void UnPauseGroup(string group) => GetInstance()._pausedGroups.Remove(group);
    public static void ReversePauseGroup(string group)
    {
        if (GetInstance()._pausedGroups.Contains(group))
            UnPauseGroup(group);
        else
            PauseGroup(group);
    }
    

    private static DebugDrawingBatcher GetInstance()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("DebugDrawingBatcher");
            go.hideFlags = HideFlags.HideAndDontSave;
            _instance = go.AddComponent<DebugDrawingBatcher>();
        }

        return _instance;
    }

    private void Update()
    {
        foreach (var group in _batchedCalls.Keys)
        {
            if (_pausedGroups.Contains(group))
                continue;
            foreach (var call in _batchedCalls[group])
            {
                call.Invoke();
            }
        }
    }
}