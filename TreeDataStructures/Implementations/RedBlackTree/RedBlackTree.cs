using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
{
    public RedBlackTree() : base() { }
    public RedBlackTree(IComparer<TKey> comparer) : base(comparer) { }

    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        // Новый узел всегда красный
        return new RbNode<TKey, TValue>(key, value) { Color = RbColor.Red };
    }

    // ------------------------------------------------------------------
    //  Балансировка после вставки (вызывается из OnNodeAdded)
    // ------------------------------------------------------------------
    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        FixInsert(newNode);
    }

    // ------------------------------------------------------------------
    //  Балансировка после удаления (вызывается из OnNodeRemoved)
    // ------------------------------------------------------------------
    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
        // При удалении узла с двумя детьми мы передаём родителя и ребёнка преемника.
        // Балансировку нужно начинать с того места, где произошло фактическое удаление.
        // parent — это родитель удалённого узла (или преемника),
        // child — это ребёнок, который занял место удалённого (может быть null).
        // Начинаем балансировку с child (если он есть) или с parent.
        RbNode<TKey, TValue>? startNode = child ?? parent;
        if (startNode != null)
            FixDelete(startNode);
    }

    // ------------------------------------------------------------------
    //  Остальные хуки для RB-дерева не нужны
    // ------------------------------------------------------------------
    protected override void OnNodeUpdated(RbNode<TKey, TValue> node) { /* не требуется */ }
    protected override void OnNodeAccessed(RbNode<TKey, TValue> node) { /* не требуется */ }

    // ------------------------------------------------------------------
    //  Вспомогательные методы для балансировки
    // ------------------------------------------------------------------
    private void FixInsert(RbNode<TKey, TValue> node)
    {
        // Пока узел не корень и родитель красный
        while (node.Parent != null && node.Parent.Color == RbColor.Red)
        {
            var parent = node.Parent;
            var grand = parent.Parent;
            if (grand == null) break; // защиты

            var isParentLeft = parent.IsLeftChild;
            var uncle = isParentLeft ? grand.Right : grand.Left;

            // Случай 1: дядя красный → перекраска
            if (uncle != null && uncle.Color == RbColor.Red)
            {
                parent.Color = RbColor.Black;
                uncle.Color = RbColor.Black;
                grand.Color = RbColor.Red;
                node = grand;           // проблема поднимается к деду
                continue;
            }

            // Случай 2: дядя чёрный → повороты
            if (isParentLeft)
            {
                if (!node.IsLeftChild) // внутренний узел (правый ребёнок)
                {
                    node = parent;
                    RotateLeft(node);
                    parent = node.Parent; // обновляем parent после поворота
                }
                // внешний узел (левый ребёнок)
                parent!.Color = RbColor.Black;
                grand.Color = RbColor.Red;
                RotateRight(grand);
            }
            else // parent — правый ребёнок
            {
                if (node.IsLeftChild) // внутренний узел (левый ребёнок)
                {
                    node = parent;
                    RotateRight(node);
                    parent = node.Parent;
                }
                parent!.Color = RbColor.Black;
                grand.Color = RbColor.Red;
                RotateLeft(grand);
            }
            // после поворотов дерево сбалансировано, выходим
            break;
        }

        // Корень всегда чёрный
        if (Root != null) Root.Color = RbColor.Black;
    }

    private void FixDelete(RbNode<TKey, TValue> node)
    {
        // Пока узел не корень и он чёрный (или стал двойным чёрным)
        while (node != Root && (node.Color == RbColor.Black))
        {
            var parent = node.Parent;
            if (parent == null) break;

            var isLeft = node.IsLeftChild;
            var sibling = isLeft ? parent.Right : parent.Left;

            // Случай 1: брат красный
            if (sibling != null && sibling.Color == RbColor.Red)
            {
                parent.Color = RbColor.Red;
                sibling.Color = RbColor.Black;
                if (isLeft)
                    RotateLeft(parent);
                else
                    RotateRight(parent);
                sibling = isLeft ? parent.Right : parent.Left;
            }

            // Если брата нет, поднимаемся вверх (не должно быть, но на всякий случай)
            if (sibling == null)
            {
                node = parent;
                continue;
            }

            var leftNephew = sibling.Left;
            var rightNephew = sibling.Right;

            // Случай 2: оба племянника чёрные
            if ((leftNephew == null || leftNephew.Color == RbColor.Black) &&
                (rightNephew == null || rightNephew.Color == RbColor.Black))
            {
                sibling.Color = RbColor.Red;
                node = parent;          // проблема поднимается к родителю
                continue;
            }

            // Случай 3: один из племянников красный, но не дальний
            if (isLeft)
            {
                if (rightNephew == null || rightNephew.Color == RbColor.Black)
                {
                    if (leftNephew != null) leftNephew.Color = RbColor.Black;
                    sibling.Color = RbColor.Red;
                    RotateRight(sibling);
                    sibling = parent.Right;
                    rightNephew = sibling?.Right;
                }
                // Случай 4: дальний племянник красный
                sibling!.Color = parent.Color;
                parent.Color = RbColor.Black;
                if (rightNephew != null) rightNephew.Color = RbColor.Black;
                RotateLeft(parent);
            }
            else // симметрично для правого ребёнка
            {
                if (leftNephew == null || leftNephew.Color == RbColor.Black)
                {
                    if (rightNephew != null) rightNephew.Color = RbColor.Black;
                    sibling.Color = RbColor.Red;
                    RotateLeft(sibling);
                    sibling = parent.Left;
                    leftNephew = sibling?.Left;
                }
                sibling!.Color = parent.Color;
                parent.Color = RbColor.Black;
                if (leftNephew != null) leftNephew.Color = RbColor.Black;
                RotateRight(parent);
            }

            // После исправления выходим
            break;
        }

        // Убеждаемся, что текущий узел (ставший корнем) чёрный
        if (node != null) node.Color = RbColor.Black;
        if (Root != null) Root.Color = RbColor.Black;
    }
}