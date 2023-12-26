using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransformExtensions
{
    /// <summary>
    /// Find a transform by name in the transforms child hierarchy
    /// </summary>
    /// <param name="self">self-ref</param>
    /// <param name="exactName">The name of the child transform</param>
    /// <returns></returns>
    public static Transform FindRecursive(this Transform self, string exactName) => self.FindRecursive(child => child.name.Equals(exactName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Find a transform child that fulfills selector by depth-first search
    /// </summary>
    /// <param name="self">self-ref</param>
    /// <param name="selector">The selector function</param>
    /// <returns></returns>
    public static Transform FindRecursive(this Transform self, Func<Transform, bool> selector)
    {
        foreach (Transform child in self)
        {
            if (selector(child))
            {
                return child;
            }

            var finding = child.FindRecursive(selector);

            if (finding != null)
            {
                return finding;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns a list of all children and childrens children, depth first, including this transform
    /// </summary>
    /// <param name="self">self-ref</param>
    /// <returns></returns>
    public static List<Transform> GetRecursiveChildren(this Transform self)
    {
        List<Transform> foundChildren = new()
        {
            self
        };
        foreach (Transform t in self)
        {
            foundChildren.AddRange(t.GetRecursiveChildren());
        }
        return foundChildren;
    }
}
