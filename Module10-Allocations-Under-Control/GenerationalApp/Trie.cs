using System.Collections.Generic;

namespace GenerationalApp
{
    class Trie<TKey, TKeyElement, TValue>
        where TKey : IEnumerable<TKeyElement>
    {
        private readonly TrieNode<TKey, TKeyElement, TValue> _root;

        public Trie()
        {
            _root = new TrieNode<TKey, TKeyElement, TValue>(default);
        }

        public void Add(TKey key, TValue value)
        {
            var node = _root;
            foreach (var element in key)
            {
                node = AddElement(node, element);
            }
            node.Key = key;
            node.Value = value;
        }

        public bool TryGetItem(TKey key, out TValue value)
        {
            if (TryGetNode(key, out var node))
            {
                if (node is not null && node.Key is not null)
                {
                    value = node.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private bool TryGetNode(TKey key, out TrieNode<TKey, TKeyElement, TValue>? node)
        {
            var currentNode = _root;
            foreach (var keyElement in key)
            {
                if (!currentNode.Children.TryGetValue(keyElement, out currentNode))
                {
                    node = null;
                    return false;
                }
            }

            node = currentNode;
            return true;
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> EnumerateNodes()
        {
            return _root.EnumerateChildren();
        }

        private TrieNode<TKey, TKeyElement, TValue> AddElement(TrieNode<TKey, TKeyElement, TValue> node,
            TKeyElement keyElement)
        {
            if (!node.Children.TryGetValue(keyElement, out var childNode))
            {
                childNode = new TrieNode<TKey, TKeyElement, TValue>(keyElement)
                {
                    Parent = node
                };
                node.Children.Add(keyElement, childNode);
            }

            return childNode;
        }
    }
}