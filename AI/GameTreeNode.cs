using Godot;
using System;
using System.Collections.Generic;

public class GameTreeNode
{
    public AmoeballState GameState { get; private set; }
    public GameTreeNode Parent { get; private set; }
    public List<GameTreeNode> Children { get; private set; }

    public GameTreeNode(AmoeballState gameState, GameTreeNode parent = null)
    {
        GameState = gameState;
        Parent = parent;
        Children = new List<GameTreeNode>();
    }

    // Helper method to add a child node
    public GameTreeNode AddChild(AmoeballState childState)
    {
        var childNode = new GameTreeNode(childState, this);
        Children.Add(childNode);
        return childNode;
    }

    // Helper method to remove a child node
    public bool RemoveChild(GameTreeNode child)
    {
        return Children.Remove(child);
    }

    // Helper method to check if this is a leaf node
    public bool IsLeaf()
    {
        return Children.Count == 0;
    }

    // Helper method to check if this is the root node
    public bool IsRoot()
    {
        return Parent == null;
    }

    // Helper method to get the depth of this node in the tree
    public int GetDepth()
    {
        int depth = 0;
        var current = this;
        while (current.Parent != null)
        {
            depth++;
            current = current.Parent;
        }
        return depth;
    }

    // Generates child nodes for all possible legal moves from the current game state
    public void Expand()
    {
        // Clear any existing children first
        Children.Clear();

        // Get all possible next states from the current game state
        foreach (var nextState in GameState.GetNextStates())
        {
            // Create a new child node for each possible next state
            AddChild(nextState);
        }
    }
}
