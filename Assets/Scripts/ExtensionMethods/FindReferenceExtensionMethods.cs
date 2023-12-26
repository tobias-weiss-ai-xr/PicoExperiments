using System;
using System.Collections.Generic;
using UnityEngine;

public static class FindReferenceExtensionMethods 
{
    /// <summary>
    /// Overwrites the refence with GameObject.Find(expectedName) if the reference was null
    /// </summary>
    /// <param name="mb">Self-ref</param>
    /// <param name="reference">The object to be ensured</param>
    /// <param name="expectedName">The expected name of the Object if its null</param>
    public static void EnsureObjectReference(this MonoBehaviour mb, ref GameObject reference, string expectedName)
    {
        if(reference == null)
        {          
            reference = GameObject.Find(expectedName);
            if(reference == null)
            {
                Debug.LogWarning("Could not find object with expected name: " + expectedName);
            }
        }
    }
    /// <summary>
    /// Overwrites the refence with a child with the given name if the reference was null
    /// </summary>
    /// <param name="mb">Self-ref</param>
    /// <param name="reference">The object to be ensured</param>
    /// <param name="expectedName">The expected name of the Object</param>
    public static void EnsureObjectReferenceInChildren(this MonoBehaviour mb, ref GameObject reference, string expectedName)
    {
        if (reference == null)
        {
            reference = mb.transform.FindRecursive(expectedName).gameObject;
            if (reference == null)
            {
                Debug.LogWarning("Could not find object with expected name: " + expectedName);
            }
        }
    }

    /// <summary>
    /// Overwrites the refence with a child with the given name if the reference was null
    /// </summary>
    /// <param name="mb">Self-ref</param>
    /// <param name="reference">The object to be ensured</param>
    /// <param name="expectedName">The expected name of the Object if its null</param>
    public static void EnsureComponentReferenceInChildren<T>(this MonoBehaviour mb, ref T reference, string expectedName) where T : Component
    {
        if (reference == null)
        {
            reference = mb.transform.FindRecursive(expectedName).GetComponent<T>();
            if (reference == null)
            {
                Debug.LogWarning("Could not find object with expected name: " + expectedName);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="mb"></param>
    /// <param name="reference"></param>
    public static void EnsureComponentReference<T>(this MonoBehaviour mb, ref T reference) where T: Component
    {
        if (reference == null)
        {
            reference = mb.GetComponent<T>();
            if (reference == null)
            {
                Debug.LogWarning("Could not find component with type " + typeof(T) + "on object: " + mb.gameObject.name);
            }
        }
    }

    /// <summary>
    /// If the reference is null, sets it to the Component of the same type attached to the GameObject with name expectedName
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="mb"></param>
    /// <param name="reference"></param>
    /// <param name="expectedName"></param>
    public static void EnsureComponentReference<T>(this MonoBehaviour mb, ref T reference, string expectedName) where T : Component
    {
        if (reference == null)
        {
            reference = GameObject.Find(expectedName).GetComponent<T>();
            if (reference == null)
            {
                Debug.LogWarning("Could not find component with type " + typeof(T) + "on object: " + mb.gameObject.name);
            }
        }
    }
}
