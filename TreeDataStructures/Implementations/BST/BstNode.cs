using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.BST
{
    public class BstNode<TKey, TValue> : Node<TKey, TValue, BstNode<TKey, TValue>>
    {
        public BstNode(TKey key, TValue value) : base(key, value) { }
    }
}