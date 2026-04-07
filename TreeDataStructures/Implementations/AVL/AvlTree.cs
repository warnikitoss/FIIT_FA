using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
{
    public AvlTree() : base() { }
    public AvlTree(IComparer<TKey> comparer) : base(comparer) { }

    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    // ------------------------------------------------------------------
    //  Балансировка после вставки (поднимаемся от newNode до корня)
    // ------------------------------------------------------------------
    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        // Начинаем с добавленного узла и поднимаемся вверх, балансируя каждый узел
        var current = newNode;
        while (current != null)
        {
            UpdateHeight(current);
            current = Balance(current);
            current = current.Parent;
        }
    }

    // ------------------------------------------------------------------
    //  Балансировка после удаления (поднимаемся от места удаления до корня)
    // ------------------------------------------------------------------
    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        // При удалении узла с двумя детьми мы передаём родителя и ребёнка преемника.
        // Начинаем балансировку с parent (или с child, если parent нет).
        var start = parent ?? child;
        var current = start;
        while (current != null)
        {
            UpdateHeight(current);
            current = Balance(current);
            current = current.Parent;
        }
    }

    // Остальные хуки не нужны для AVL
    protected override void OnNodeUpdated(AvlNode<TKey, TValue> node) { }
    protected override void OnNodeAccessed(AvlNode<TKey, TValue> node) { }

    // ------------------------------------------------------------------
    //  Вспомогательные методы балансировки
    // ------------------------------------------------------------------
    private int GetBalanceFactor(AvlNode<TKey, TValue>? node)
        => node == null ? 0 : GetHeight(node.Left) - GetHeight(node.Right);

    private AvlNode<TKey, TValue> Balance(AvlNode<TKey, TValue> node)
    {
        if (node == null) return null!;

        int bf = GetBalanceFactor(node);

        // Левое поддерево выше правого
        if (bf > 1)
        {
            if (GetBalanceFactor(node.Left) < 0)
            {
                // Левый-правый поворот (двойной)
                node = RotateLeftRight(node);
            }
            else
            {
                // Правый поворот
                node = RotateRight(node);
            }
        }
        // Правое поддерево выше левого
        else if (bf < -1)
        {
            if (GetBalanceFactor(node.Right) > 0)
            {
                // Правый-левый поворот (двойной)
                node = RotateRightLeft(node);
            }
            else
            {
                // Левый поворот
                node = RotateLeft(node);
            }
        }

        // После поворотов высоты обновятся внутри методов поворота,
        // но для страховки ещё раз обновим высоту текущего узла
        UpdateHeight(node);
        return node;
    }

    // ------------------------------------------------------------------
    //  Повороты с обновлением высот и возвратом нового корня поддерева
    // ------------------------------------------------------------------
    private new AvlNode<TKey, TValue> RotateLeft(AvlNode<TKey, TValue> x)
    {
        base.RotateLeft(x);
        // После поворота новый корень — бывший правый ребёнок x
        var newRoot = x.Parent as AvlNode<TKey, TValue> ?? x;
        UpdateHeight(x);
        UpdateHeight(newRoot);
        return newRoot;
    }

    private new AvlNode<TKey, TValue> RotateRight(AvlNode<TKey, TValue> y)
    {
        base.RotateRight(y);
        var newRoot = y.Parent as AvlNode<TKey, TValue> ?? y;
        UpdateHeight(y);
        UpdateHeight(newRoot);
        return newRoot;
    }

    private AvlNode<TKey, TValue> RotateLeftRight(AvlNode<TKey, TValue> x)
    {
        // Сначала левый поворот левого ребёнка
        var leftChild = x.Left;
        if (leftChild != null)
        {
            base.RotateLeft(leftChild);
            UpdateHeight(leftChild);
            UpdateHeight(x);
        }
        // Затем правый поворот вокруг x
        base.RotateRight(x);
        var newRoot = x.Parent as AvlNode<TKey, TValue> ?? x;
        UpdateHeight(x);
        UpdateHeight(newRoot);
        return newRoot;
    }

    private AvlNode<TKey, TValue> RotateRightLeft(AvlNode<TKey, TValue> x)
    {
        var rightChild = x.Right;
        if (rightChild != null)
        {
            base.RotateRight(rightChild);
            UpdateHeight(rightChild);
            UpdateHeight(x);
        }
        base.RotateLeft(x);
        var newRoot = x.Parent as AvlNode<TKey, TValue> ?? x;
        UpdateHeight(x);
        UpdateHeight(newRoot);
        return newRoot;
    }

    // Переопределим UpdateHeight, чтобы использовать типизированный узел (необязательно, но для ясности)
    private new void UpdateHeight(AvlNode<TKey, TValue> node)
    {
        node.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));
    }
}