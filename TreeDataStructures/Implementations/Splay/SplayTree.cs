using TreeDataStructures.Core;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, BstNode<TKey, TValue>>
{
    public SplayTree() : base() { }
    public SplayTree(IComparer<TKey> comparer) : base(comparer) { }

    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode) => Splay(newNode);
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
        if (parent != null)
            Splay(parent);
        else if (child != null)
            Splay(child);
    }
    protected override void OnNodeUpdated(BstNode<TKey, TValue> node) => Splay(node);
    protected override void OnNodeAccessed(BstNode<TKey, TValue> node) => Splay(node);

    public override bool ContainsKey(TKey key) => TryGetValue(key, out _);

    private void Splay(BstNode<TKey, TValue> node)
    {
        while (node.Parent != null)
        {
            var parent = node.Parent;
            var grandParent = parent.Parent;

            if (grandParent == null)
            {
                // Zig
                if (parent.Left == node)
                    RotateRight(parent);
                else
                    RotateLeft(parent);
            }
            else
            {
                bool isNodeLeft = parent.Left == node;
                bool isParentLeft = grandParent.Left == parent;

                if (isNodeLeft && isParentLeft)
                {
                    // Zig-Zig (LL)
                    RotateRight(grandParent);
                    RotateRight(parent);
                }
                else if (!isNodeLeft && !isParentLeft)
                {
                    // Zig-Zig (RR)
                    RotateLeft(grandParent);
                    RotateLeft(parent);
                }
                else if (isNodeLeft && !isParentLeft)
                {
                    // Zig-Zag (RL)
                    RotateRight(parent);
                    RotateLeft(grandParent);
                }
                else
                {
                    // Zig-Zag (LR)
                    RotateLeft(parent);
                    RotateRight(grandParent);
                }
            }
        }
    }
}