using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WanderSpire.Scripting;

namespace SceneEditor.Models;

/// <summary>
/// Represents a node in the scene hierarchy
/// </summary>
public class SceneNode : ReactiveObject
{
    private string _name = "Entity";
    private bool _isVisible = true;
    private bool _isLocked = false;
    private bool _isExpanded = true;
    private bool _isSelected = false;
    private SceneNode? _parent;

    public Entity Entity { get; }

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    public bool IsLocked
    {
        get => _isLocked;
        set => this.RaiseAndSetIfChanged(ref _isLocked, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    public SceneNode? Parent
    {
        get => _parent;
        set => this.RaiseAndSetIfChanged(ref _parent, value);
    }

    public ObservableCollection<SceneNode> Children { get; } = new();

    public bool HasChildren => Children.Count > 0;

    public SceneNode(Entity entity)
    {
        Entity = entity;

        // Initialize name based on entity data
        if (entity.IsValid)
        {
            try
            {
                // Try to get a meaningful name from components
                var uuid = entity.Uuid;
                Name = uuid != 0 ? $"Entity_{uuid:X8}" : $"Entity_{entity.Id}";
            }
            catch
            {
                Name = $"Entity_{entity.Id}";
            }
        }
    }

    public override string ToString() => Name;

    /// <summary>
    /// Get all descendant nodes (recursive)
    /// </summary>
    public IEnumerable<SceneNode> GetDescendants()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var descendant in child.GetDescendants())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Get all ancestor nodes up to root
    /// </summary>
    public IEnumerable<SceneNode> GetAncestors()
    {
        var current = Parent;
        while (current != null)
        {
            yield return current;
            current = current.Parent;
        }
    }

    /// <summary>
    /// Check if this node is an ancestor of another node
    /// </summary>
    public bool IsAncestorOf(SceneNode other)
    {
        return other.GetAncestors().Contains(this);
    }

    /// <summary>
    /// Check if this node is a descendant of another node
    /// </summary>
    public bool IsDescendantOf(SceneNode other)
    {
        return GetAncestors().Contains(other);
    }

    /// <summary>
    /// Get the depth level in the hierarchy (0 = root)
    /// </summary>
    public int GetDepth()
    {
        int depth = 0;
        var current = Parent;
        while (current != null)
        {
            depth++;
            current = current.Parent;
        }
        return depth;
    }

    /// <summary>
    /// Find a child by name
    /// </summary>
    public SceneNode? FindChild(string name)
    {
        return Children.FirstOrDefault(c => c.Name == name);
    }

    /// <summary>
    /// Find a descendant by name (recursive)
    /// </summary>
    public SceneNode? FindDescendant(string name)
    {
        var direct = FindChild(name);
        if (direct != null)
            return direct;

        foreach (var child in Children)
        {
            var found = child.FindDescendant(name);
            if (found != null)
                return found;
        }

        return null;
    }

    /// <summary>
    /// Get sibling index
    /// </summary>
    public int GetSiblingIndex()
    {
        if (Parent == null)
        {
            // Would need access to root collection to determine this
            return 0;
        }

        return Parent.Children.IndexOf(this);
    }

    /// <summary>
    /// Move this node to a different sibling index
    /// </summary>
    public void SetSiblingIndex(int index)
    {
        if (Parent == null)
            return;

        var siblings = Parent.Children;
        int currentIndex = siblings.IndexOf(this);
        if (currentIndex == -1 || currentIndex == index)
            return;

        siblings.RemoveAt(currentIndex);
        siblings.Insert(Math.Clamp(index, 0, siblings.Count), this);
    }
}