using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace GenerationalApp
{
    internal sealed class TrieNode<TKey, TKeyElement, TValue>
        where TKey : IEnumerable<TKeyElement>
    {
        public TrieNode([NotNull] TKeyElement keyElement)
        {
            KeyElement = keyElement;
            Children = new Dictionary<TKeyElement, TrieNode<TKey, TKeyElement, TValue>>();
        }


        public TKeyElement KeyElement { get; }

        public TKey? Key { get; set; }

        public TValue Value { get; set; }

        public IDictionary<TKeyElement, TrieNode<TKey, TKeyElement, TValue>> Children { get; }

        public TrieNode<TKey, TKeyElement, TValue> Parent { get; set; }

        public IEnumerable<KeyValuePair<TKey, TValue>> EnumerateChildren()
        {
            foreach (var child in Children)
            {
                if (child.Value.Key is not null)
                {
                    yield return new(child.Value.Key, child.Value.Value);
                }

                foreach (var item in child.Value.EnumerateChildren())
                    yield return item;
            }
        }
    }
}