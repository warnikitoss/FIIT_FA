using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;
using System.Linq;

namespace TreeDataStructures.Core;

/// <summary>
/// Базовый класс для всех деревьев поиска (BST, AVL, RB, Splay, Treap).
/// Реализует итеративные алгоритмы вставки и удаления с хуками для балансировки.
/// </summary>
public abstract class BinarySearchTreeBase<TKey, TValue, TNode> : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; }
    public int Count { get; protected set; }
    public bool IsReadOnly => false;
    public ICollection<TKey> Keys => InOrder().Select(e => e.Key).ToList();
    public ICollection<TValue> Values => InOrder().Select(e => e.Value).ToList();

    protected BinarySearchTreeBase(IComparer<TKey>? comparer = null)
    {
        Comparer = comparer ?? Comparer<TKey>.Default;
    }

    // ------------------------------------------------------------------
    //  Публичные методы (шаблонные)
    // ------------------------------------------------------------------
    public virtual void Add(TKey key, TValue value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        TNode? parent = null;
        TNode? current = Root;
        int cmp = 0;

        // Поиск места вставки
        while (current != null)
        {
            parent = current;
            cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0)
            {
                // Ключ уже существует – обновляем значение
                current.Value = value;
                OnNodeUpdated(current);
                return;
            }
            current = cmp < 0 ? current.Left : current.Right;
        }

        // Создание нового узла
        TNode newNode = CreateNode(key, value);
        newNode.Parent = parent;

        if (parent == null)
            Root = newNode;
        else if (cmp < 0)
            parent.Left = newNode;
        else
            parent.Right = newNode;

        Count++;
        OnNodeAdded(newNode);   // хук после успешной вставки
    }

    public virtual bool Remove(TKey key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        TNode? nodeToRemove = FindNode(key);
        if (nodeToRemove == null) return false;

        TNode? parentOfRemoved = nodeToRemove.Parent;
        TNode? childOfRemoved = null;

        // Случай 1: нет детей
        if (nodeToRemove.Left == null && nodeToRemove.Right == null)
        {
            ReplaceNode(nodeToRemove, null);
        }
        // Случай 2: только правый ребёнок
        else if (nodeToRemove.Left == null)
        {
            childOfRemoved = nodeToRemove.Right;
            ReplaceNode(nodeToRemove, nodeToRemove.Right);
        }
        // Случай 3: только левый ребёнок
        else if (nodeToRemove.Right == null)
        {
            childOfRemoved = nodeToRemove.Left;
            ReplaceNode(nodeToRemove, nodeToRemove.Left);
        }
        // Случай 4: два ребёнка – ищем преемника (минимальный в правом поддереве)
        else
        {
            TNode successor = FindMin(nodeToRemove.Right);
            TKey successorKey = successor.Key;
            TValue successorValue = successor.Value;

            TNode? successorParent = successor.Parent;
            TNode? successorChild = successor.Right; // у преемника нет левого ребёнка

            // Удаляем преемника (он не имеет левого ребёнка)
            ReplaceNode(successor, successor.Right);

            // Заменяем данные удаляемого узла данными преемника
            nodeToRemove.Key = successorKey;
            nodeToRemove.Value = successorValue;

            parentOfRemoved = successorParent;
            childOfRemoved = successorChild;
        }

        Count--;
        OnNodeRemoved(parentOfRemoved, childOfRemoved);
        return true;
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;

    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            OnNodeAccessed(node);
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    public void Clear()
    {
        Root = null;
        Count = 0;
    }

    // ------------------------------------------------------------------
    //  Защищённые вспомогательные методы
    // ------------------------------------------------------------------
    protected abstract TNode CreateNode(TKey key, TValue value);

    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) return current;
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected TNode FindMin(TNode node)
    {
        while (node.Left != null) node = node.Left;
        return node;
    }

    protected TNode FindMax(TNode node)
    {
        while (node.Right != null) node = node.Right;
        return node;
    }

    protected void ReplaceNode(TNode oldNode, TNode? newNode)
    {
        if (oldNode.Parent == null)
            Root = newNode;
        else if (oldNode.IsLeftChild)
            oldNode.Parent.Left = newNode;
        else
            oldNode.Parent.Right = newNode;

        if (newNode != null)
            newNode.Parent = oldNode.Parent;
    }

    // ------------------------------------------------------------------
    //  Хуки для наследников (балансировка, splay и т.д.)
    // ------------------------------------------------------------------
    protected virtual void OnNodeAdded(TNode newNode) { }
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }
    protected virtual void OnNodeUpdated(TNode node) { }
    protected virtual void OnNodeAccessed(TNode node) { }

    // ------------------------------------------------------------------
    //  Повороты (используются в AVL, RB, Splay)
    // ------------------------------------------------------------------
    protected void RotateLeft(TNode x)
    {
        if (x.Right == null) throw new InvalidOperationException("RotateLeft requires right child.");
        TNode y = x.Right;
        x.Right = y.Left;
        if (y.Left != null) y.Left.Parent = x;
        y.Parent = x.Parent;
        if (x.Parent == null)
            Root = y;
        else if (x.IsLeftChild)
            x.Parent.Left = y;
        else
            x.Parent.Right = y;
        y.Left = x;
        x.Parent = y;
        UpdateHeight(x);
        UpdateHeight(y);
    }

    protected void RotateRight(TNode y)
    {
        if (y.Left == null) throw new InvalidOperationException("RotateRight requires left child.");
        TNode x = y.Left;
        y.Left = x.Right;
        if (x.Right != null) x.Right.Parent = y;
        x.Parent = y.Parent;
        if (y.Parent == null)
            Root = x;
        else if (y.IsLeftChild)
            y.Parent.Left = x;
        else
            y.Parent.Right = x;
        x.Right = y;
        y.Parent = x;
        UpdateHeight(y);
        UpdateHeight(x);
    }

    protected void RotateDoubleLeft(TNode x) => RotateLeft(x.Left!);
    protected void RotateDoubleRight(TNode x) => RotateRight(x.Right!);
    protected void RotateBigLeft(TNode x) => RotateDoubleLeft(x);
    protected void RotateBigRight(TNode x) => RotateDoubleRight(x);

    // Высота (нужна для AVL и для отображения в итераторах)
    protected virtual void UpdateHeight(TNode node)
    {
        node.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));
    }

    protected int GetHeight(TNode? node) => node?.Height ?? 0;

    // ------------------------------------------------------------------
    //  Итераторы (полностью из старой версии, без изменений)
    // ------------------------------------------------------------------
    private enum TraversalStrategy
    {
        InOrder, PreOrder, PostOrder,
        InOrderReverse, PreOrderReverse, PostOrderReverse
    }

    private struct TreeIterator : IEnumerable<TreeEntry<TKey, TValue>>,
                                  IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TNode? _root;
        private readonly TraversalStrategy _strategy;
        private Stack<TNode>? _stack;
        private TNode? _currentNode;
        private TNode? _lastVisited;
        private TreeEntry<TKey, TValue> _current;

        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _stack = null;
            _currentNode = null;
            _lastVisited = null;
            _current = default;
            Reset();
        }

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        public TreeEntry<TKey, TValue> Current => _current;
        object IEnumerator.Current => Current;
        public void Dispose() { }

        public void Reset()
        {
            _stack = new Stack<TNode>();
            _currentNode = _root;
            _lastVisited = null;
            _current = default;
        }

        public bool MoveNext()
        {
            switch (_strategy)
            {
                case TraversalStrategy.InOrder: return MoveNextInOrder();
                case TraversalStrategy.PreOrder: return MoveNextPreOrder();
                case TraversalStrategy.PostOrder: return MoveNextPostOrder();
                case TraversalStrategy.InOrderReverse: return MoveNextInOrderReverse();
                case TraversalStrategy.PreOrderReverse: return MoveNextPreOrderReverse();
                case TraversalStrategy.PostOrderReverse: return MoveNextPostOrderReverse();
                default: return false;
            }
        }

        private bool MoveNextInOrder()
        {
            while (_stack?.Count > 0 || _currentNode != null)
            {
                if (_currentNode != null)
                {
                    _stack!.Push(_currentNode);
                    _currentNode = _currentNode.Left;
                }
                else
                {
                    TNode node = _stack!.Pop();
                    _current = new TreeEntry<TKey, TValue>(node.Key, node.Value, node.Height);
                    _currentNode = node.Right;
                    return true;
                }
            }
            return false;
        }

        private bool MoveNextPreOrder()
        {
            if (_stack?.Count == 0 && _currentNode == null) return false;
            if (_currentNode != null)
            {
                _current = new TreeEntry<TKey, TValue>(_currentNode.Key, _currentNode.Value, _currentNode.Height);
                if (_currentNode.Right != null) _stack!.Push(_currentNode.Right);
                _currentNode = _currentNode.Left;
                return true;
            }
            _currentNode = _stack!.Pop();
            return MoveNextPreOrder();
        }

        private bool MoveNextPostOrder()
        {
            while (_stack?.Count > 0 || _currentNode != null)
            {
                if (_currentNode != null)
                {
                    _stack!.Push(_currentNode);
                    _currentNode = _currentNode.Left;
                }
                else
                {
                    TNode peek = _stack!.Peek();
                    if (peek.Right != null && _lastVisited != peek.Right)
                    {
                        _currentNode = peek.Right;
                    }
                    else
                    {
                        _lastVisited = _stack.Pop();
                        _current = new TreeEntry<TKey, TValue>(_lastVisited.Key, _lastVisited.Value, _lastVisited.Height);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool MoveNextInOrderReverse()
        {
            while (_stack?.Count > 0 || _currentNode != null)
            {
                if (_currentNode != null)
                {
                    _stack!.Push(_currentNode);
                    _currentNode = _currentNode.Right;
                }
                else
                {
                    TNode node = _stack!.Pop();
                    _current = new TreeEntry<TKey, TValue>(node.Key, node.Value, node.Height);
                    _currentNode = node.Left;
                    return true;
                }
            }
            return false;
        }

        private bool MoveNextPreOrderReverse()
        {
            if (_stack?.Count == 0 && _currentNode == null) return false;
            if (_currentNode != null)
            {
                _current = new TreeEntry<TKey, TValue>(_currentNode.Key, _currentNode.Value, _currentNode.Height);
                if (_currentNode.Left != null) _stack!.Push(_currentNode.Left);
                _currentNode = _currentNode.Right;
                return true;
            }
            _currentNode = _stack!.Pop();
            return MoveNextPreOrderReverse();
        }

        private bool MoveNextPostOrderReverse()
        {
            while (_stack?.Count > 0 || _currentNode != null)
            {
                if (_currentNode != null)
                {
                    _stack!.Push(_currentNode);
                    _currentNode = _currentNode.Right;
                }
                else
                {
                    TNode peek = _stack!.Peek();
                    if (peek.Left != null && _lastVisited != peek.Left)
                    {
                        _currentNode = peek.Left;
                    }
                    else
                    {
                        _lastVisited = _stack.Pop();
                        _current = new TreeEntry<TKey, TValue>(_lastVisited.Key, _lastVisited.Value, _lastVisited.Height);
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() =>
        new TreeIterator(Root, TraversalStrategy.InOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() =>
        new TreeIterator(Root, TraversalStrategy.PreOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() =>
        new TreeIterator(Root, TraversalStrategy.PostOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() =>
        new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() => PreOrder().Reverse(); 
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() => PostOrder().Reverse();

    // ------------------------------------------------------------------
    //  Реализация ICollection<KeyValuePair<TKey, TValue>>
    // ------------------------------------------------------------------
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var entry in InOrder())
            yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
        throw new NotImplementedException();
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}