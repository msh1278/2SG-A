using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// A thread-safe class which holds a queue of actions to execute on the next Update() on the main thread.
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static bool _isInitialized = false;

    public static UnityMainThreadDispatcher Instance()
    {
        if (!_isInitialized)
        {
            Initialize();
        }
        return _instance;
    }

    public static void Initialize()
    {
        if (_isInitialized) return;

        // Check if an instance already exists
        if (_instance == null)
        {
            // Create a new GameObject with UnityMainThreadDispatcher attached
            var go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }

        _isInitialized = true;
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            _isInitialized = true;
        }
    }

    void Update()
    {
        // Execute all queued actions
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    /// <summary>
    /// Enqueue an action to be executed on the main thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void Enqueue(Action action)
    {
        if (action == null)
        {
            Debug.LogError("Tried to enqueue a null action to the main thread.");
            return;
        }

        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
} 