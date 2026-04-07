using TreeDataStructures.Core;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.BST
{
    public class BinarySearchTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, BstNode<TKey, TValue>>
    {
        public BinarySearchTree() : base() { }
        public BinarySearchTree(IComparer<TKey> comparer) : base(comparer) { }

        protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        {
            return new BstNode<TKey, TValue>(key, value);
        }
    }
}