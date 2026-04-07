using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
{
    private readonly Random _random = new();

    public Treap() : base() { }
    public Treap(IComparer<TKey> comparer) : base(comparer) { }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new TreapNode<TKey, TValue>(key, value)
        {
            Priority = _random.Next()
        };
    }

    // Хуки не требуются для Treap
    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode) { }
    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child) { }

    /// <summary>
    /// Разрезает дерево с корнем root на два поддерева:
    /// Left: все ключи <= key
    /// Right: все ключи > key
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null)
            return (null, null);

        int cmp = Comparer.Compare(key, root.Key);
        if (cmp < 0)
        {
            var (leftSub, rightSub) = Split(root.Left, key);
            root.Left = rightSub;
            if (rightSub != null) rightSub.Parent = root;
            UpdateHeight(root);
            return (leftSub, root);
        }
        else
        {
            var (leftSub, rightSub) = Split(root.Right, key);
            root.Right = leftSub;
            if (leftSub != null) leftSub.Parent = root;
            UpdateHeight(root);
            return (root, rightSub);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Все ключи в left должны быть меньше всех ключей в right.
    /// Слияние происходит на основе приоритетов (max-heap).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null) return right;
        if (right == null) return left;

        if (left.Priority > right.Priority)
        {
            left.Right = Merge(left.Right, right);
            if (left.Right != null) left.Right.Parent = left;
            UpdateHeight(left);
            return left;
        }
        else
        {
            right.Left = Merge(left, right.Left);
            if (right.Left != null) right.Left.Parent = right;
            UpdateHeight(right);
            return right;
        }
    }

    public override void Add(TKey key, TValue value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        // Если ключ уже существует, обновляем значение
        var existing = FindNode(key);
        if (existing != null)
        {
            existing.Value = value;
            return;
        }

        var (left, right) = Split(Root, key);
        var newNode = CreateNode(key, value);
        Root = Merge(Merge(left, newNode), right);
        Count++;
    }

    public override bool Remove(TKey key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var node = FindNode(key);
        if (node == null) return false;

        var parent = node.Parent;
        var merged = Merge(node.Left, node.Right);

        if (parent == null)
            Root = merged;
        else if (node.IsLeftChild)
            parent.Left = merged;
        else
            parent.Right = merged;

        if (merged != null) merged.Parent = parent;

        // Обновляем высоты от parent до корня
        var current = parent;
        while (current != null)
        {
            UpdateHeight(current);
            current = current.Parent;
        }

        Count--;
        return true;
    }
}