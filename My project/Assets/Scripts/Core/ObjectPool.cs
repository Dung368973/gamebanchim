using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional interface that pooled objects can implement to receive spawn/despawn lifecycle callbacks.
/// </summary>
public interface IPooledObject
{
    void OnSpawnFromPool();
    void OnReturnToPool();
}

/// <summary>
/// High-performance, zero-allocation generic object pool for Unity Components.
/// Prewarms instances, eliminates GC allocations during gameplay, and handles lifecycle events.
/// </summary>
/// <typeparam name="T">Component type to pool</typeparam>
public class ObjectPool<T> where T : Component
{
    private readonly T _prefab;
    private readonly Transform _parent;
    private readonly Stack<T> _pool;
    private readonly HashSet<T> _inPoolSet;
    private int _totalCreated;

    public int AvailableCount => _pool.Count;
    public int TotalCreated => _totalCreated;
    public int ActiveCount => _totalCreated - _pool.Count;

    /// <summary>
    /// Constructs and prewarms an object pool.
    /// </summary>
    /// <param name="prefab">The template prefab component</param>
    /// <param name="initialCapacity">Number of instances to prewarm</param>
    /// <param name="parent">Optional parent transform in hierarchy</param>
    public ObjectPool(T prefab, int initialCapacity, Transform parent = null)
    {
        if (prefab == null)
        {
            throw new ArgumentNullException(nameof(prefab), "Cannot create an ObjectPool with a null prefab.");
        }

        _prefab = prefab;
        _parent = parent;
        _pool = new Stack<T>(Mathf.Max(initialCapacity, 4));
        _inPoolSet = new HashSet<T>();
        _totalCreated = 0;

        Prewarm(initialCapacity);
    }

    /// <summary>
    /// Prewarms the pool by instantiating instances and deactivating them.
    /// </summary>
    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            T instance = CreateNewInstance();
            instance.gameObject.SetActive(false);
            _pool.Push(instance);
            _inPoolSet.Add(instance);
        }
    }

    /// <summary>
    /// Retrieves an available object from the pool, or creates one if pool is depleted.
    /// Activates the GameObject and invokes IPooledObject.OnSpawnFromPool if implemented.
    /// </summary>
    public T Get()
    {
        T instance;

        if (_pool.Count > 0)
        {
            instance = _pool.Pop();
            _inPoolSet.Remove(instance);
        }
        else
        {
            instance = CreateNewInstance();
        }

        if (instance == null)
        {
            // Safeguard against externally destroyed objects
            instance = CreateNewInstance();
        }

        instance.gameObject.SetActive(true);

        if (instance is IPooledObject pooledObj)
        {
            pooledObj.OnSpawnFromPool();
        }

        return instance;
    }

    /// <summary>
    /// Retrieves an object, sets its position and rotation, and activates it.
    /// </summary>
    public T Get(Vector3 position, Quaternion rotation)
    {
        T instance = Get();
        instance.transform.SetPositionAndRotation(position, rotation);
        return instance;
    }

    /// <summary>
    /// Returns an object to the pool, deactivating it and notifying IPooledObject.OnReturnToPool.
    /// Prevents duplicate returns.
    /// </summary>
    public void Return(T obj)
    {
        if (obj == null) return;

        // Prevent returning an object that is already in the pool
        if (_inPoolSet.Contains(obj))
        {
            return;
        }

        if (obj is IPooledObject pooledObj)
        {
            pooledObj.OnReturnToPool();
        }

        obj.gameObject.SetActive(false);

        if (_parent != null && obj.transform.parent != _parent)
        {
            obj.transform.SetParent(_parent);
        }

        _pool.Push(obj);
        _inPoolSet.Add(obj);
    }

    /// <summary>
    /// Destroys all pooled instances and clears the pool.
    /// </summary>
    public void Clear()
    {
        while (_pool.Count > 0)
        {
            T obj = _pool.Pop();
            if (obj != null && obj.gameObject != null)
            {
                UnityEngine.Object.Destroy(obj.gameObject);
            }
        }
        _inPoolSet.Clear();
        _totalCreated = 0;
    }

    private T CreateNewInstance()
    {
        T instance = UnityEngine.Object.Instantiate(_prefab, _parent);
        _totalCreated++;
        return instance;
    }
}
