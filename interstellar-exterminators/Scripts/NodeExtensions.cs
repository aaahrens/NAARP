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

        // This is the root of the running scene (e.g., your level).
        var sceneRoot = tree.CurrentScene ?? tree.Root;
        if (sceneRoot == null)
            return null;

        // Walk up until our parent is the sceneRoot (or we hit the root).
        Node instanceRoot = node;
        while (instanceRoot.GetParent() != null &&
               instanceRoot.GetParent() != sceneRoot)
        {
            instanceRoot = instanceRoot.GetParent();
        }

        // Now search only within this instance's subtree.
        return instanceRoot.FindChildOfType<T>();
    }
}
