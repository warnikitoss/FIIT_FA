namespace TreeDataStructures.Core
{
    public abstract class Node<TKey, TValue, TNode>
        where TNode : Node<TKey, TValue, TNode>
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }
        public TNode? Left { get; set; }
        public TNode? Right { get; set; }
        public TNode? Parent { get; set; }
        public int Height { get; set; }

        public bool IsLeftChild => Parent?.Left == this;
        public bool IsRightChild => Parent?.Right == this;

        protected Node(TKey key, TValue value)
        {
            Key = key;
            Value = value;
            Height = 1;
        }
    }
}