using Godot;
using System;

public static class NodeExtensions
{
    /// <summary>
    /// Recursively searches this node's subtree for the first child
    /// of the specified type.
    /// Returns null if no matching node is found.
    /// </summary>
    public static T FindChildOfType<T>(this Node root) where T : class
    {
        foreach (var childObj in root.GetChildren())
        {
            if (childObj is T match)
                return match;

            if (childObj is Node child)
            {
                var nested = child.FindChildOfType<T>();
                if (nested != null)
                    return nested;
            }
        }

        return null;
    }

    /// <summary>
    /// Recursively searches this node's ancestors for the first parent of the given type.
    /// </summary>
    /// <typeparam name="T"> The type we want to search for</typeparam>
    /// <param name="root"> Where we ant to start our search.</param>
    /// <returns> The node of the given type if it was found, NULL otherwise.</returns>
    public static T FindParentOfType<T>(this Node root) where T : class
    {
        while(root.GetParent() != null)
        {
            root = root.GetParent();
            if (root is T match)
                return match;
        }

        return null;
    }

    /// <summary>
    /// Searches the current SceneTree instance for the first node of the given type.
    /// This starts from the current scene root (if available), falling back to the
    /// tree's root viewport if no current scene is set.
    /// Returns null if no matching node is found.
    /// </summary>
    public static T FindInSceneTreeOfType<T>(this Node node) where T : class
    {
        var tree = node.GetTree();
        if (tree == null)
            return null;

        // Walk up until we are at the root of the PackedScene instance this node belongs to.
        Node instanceRoot = node;
        var owner = node.Owner;

        while (instanceRoot.GetParent() != null &&
               instanceRoot.GetParent().Owner == owner)
        {
            instanceRoot = instanceRoot.GetParent();
        }

        return instanceRoot.FindChildOfType<T>();
    }

    /// <summary>
    /// Searches the entire SceneTree for the first node whose type matches
    /// <typeparamref name="T"/>.
    /// Returns null if no matching node is found.
    /// </summary>
    /// <typeparam name="T">The type of node to search for.</typeparam>
    /// <param name="node">Any node that is part of the active SceneTree.</param>
    /// <returns>
    /// The first node of type <typeparamref name="T"/> in the SceneTree,
    /// or null if no matching node exists.
    /// </returns>
    public static T FindInEntireSceneTreeOfType<T>(this Node node) where T : class
    {
        var tree = node.GetTree();
        if (tree == null)
            return null;

        // Root is the viewport root and will contain both autoloads and the current scene.
        var root = tree.Root;
        if (root == null)
            return null;

        // If the root itself matches, return it immediately.
        if (root is T rootMatch)
            return rootMatch;

        // Otherwise, reuse the existing recursive child search starting at the root.
        return root.FindChildOfType<T>();
    }
}
