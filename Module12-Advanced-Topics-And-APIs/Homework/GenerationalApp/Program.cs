using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Spectre.Console;

namespace GenerationalApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await ProcessAsync();
        }

        private static async Task ProcessAsync()
        {
            Trie<int> stringTrie = new();
            HttpClient client = new HttpClient();
            var url = "http://gutendex.com//books?languages=en&mime_type=text%2Fplain";
            int index = 0;
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var mainTask = ctx.AddTask("[green]Processing books [/]");
                    while (true)
                    {
                        HttpResponseMessage response = await client.GetAsync(url);
                        if (!response.IsSuccessStatusCode)
                            break;
                        var page = await response.Content.ReadFromJsonAsync<ResultsPage>();
                        if (page is null)
                            break;
                        mainTask.MaxValue = page.Count;

                        int pageIndex = 1;
                        var pageTask = ctx.AddTask($"[darkgreen]Processing page {pageIndex}[/]");
                        pageTask.MaxValue = page.Results.Length;
                        foreach (var pageResult in page.Results)
                        {
                            if (pageResult.Formats.TryGetValue("text/plain; charset=utf-8", out var bookUrl) &&
                                bookUrl.EndsWith(".txt"))
                            {
                                // TODO: Replace result string processing to avoid allocations by:
                                // - avoiding using heavy-allocating Split and use ReadOnlySpan-based solutions.
                                //   Unfortunatelly currently Regex.Matches/Split does not accept/produce ReadOnlySpan.
                                //   Use custom enumerator and Span.Split extension method included at the end of this file.
                                // - avoiding unneccessary string allocations in TryNormalize - this method
                                //   needs to allocate string only at the very end, when adding it to Trie.
                                //   All other checks/transformations/trimming may be done by slicing and
                                //   stackallocated buffers.
                                var result = await client.GetStringAsync(bookUrl);
                                var words = result.Split(new[] {' ', '\r', '\n'},
                                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                foreach (var word in words)
                                {
                                    if (TryNormalize(word, out var newWord))
                                    {
                                        var newValue = 0;
                                        if (stringTrie.TryGetItem(newWord, out var counter))
                                            newValue = ++counter;
                                        stringTrie.Add(newWord, newValue);
                                    }
                                }

                                index++;
                                mainTask.Value = index;
                                pageTask.Value++;
                                AnsiConsole.WriteLine(
                                    $"After parsing '{pageResult.Title}' trie size is {stringTrie.Count()}");
                            }
                        }

                        if (page.Next is null)
                            break;
                        url = page.Next;
                        pageIndex++;
                    }
                });
        }

        private static char[] trimChars = new char[] {'.', ',', ';', '!', '?', '"', ':', '(', ')', '_', '[', ']'};
        private static bool TryNormalize(string word, out string result)
        {
            result = word.ToLowerInvariant()
                .Trim(trimChars);
            foreach (var c in result)
            {
                if (!char.IsLetter(c))
                    return false;
            }
            return true;
        }
    }

    class Trie<TValue>
    {
        private readonly TrieNode<TValue> _root;

        public Trie()
        {
            _root = new TrieNode<TValue>(default);
        }

        public void Add(string key, TValue value)
        {
            var node = _root;
            foreach (var element in key)
            {
                node = AddElement(node, element);
            }
            node.Key = key;
            node.Value = value;
        }

        public bool TryGetItem(string key, out TValue value)
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

        private bool TryGetNode(string key, out TrieNode<TValue>? node)
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

        public int Count()
        {
            int result = 0;
            foreach (var element in _root.Children)
                result += element.Value.Count();
            return result;
        } 

        public IEnumerable<KeyValuePair<string, TValue>> EnumerateNodes()
        {
            foreach (var element in _root.EnumerateChildren())
                yield return element;
        }

        private TrieNode<TValue> AddElement(TrieNode<TValue> node,
            char keyElement)
        {
            if (!node.Children.TryGetValue(keyElement, out var childNode))
            {
                childNode = new TrieNode<TValue>(keyElement)
                {
                    Parent = node
                };
                node.Children.Add(keyElement, childNode);
            }

            return childNode;
        }
    }

    internal sealed class TrieNode<TValue>
    {
        public TrieNode([NotNull] char keyElement)
        {
            KeyElement = keyElement;
            Children = new Dictionary<char, TrieNode<TValue>>();
        }


        public char KeyElement { get; }

        public string? Key { get; set; }

        public TValue Value { get; set; }

        public Dictionary<char, TrieNode<TValue>> Children { get; }

        public TrieNode<TValue> Parent { get; set; }

        public IEnumerable<KeyValuePair<string, TValue>> EnumerateChildren()
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

        public int Count()
        {
            int result = Key is not null ? 1 : 0;
            foreach (var (_, childNode) in Children)
            {
                if (childNode.Key is not null)
                    result++;
                foreach (var (_, grandchildNode) in childNode.Children)
                    result += grandchildNode.Count();
            }
            return result;
        }
    }


    public class ResultsPage
    {
        public int Count { get; set; }
        public string? Next { get; set; }
        public string? Previous { get; set; }
        public Result[] Results { get; set; }
    }

    public class Result
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public Dictionary<string, string> Formats { get; set; }
    }
}

public ref struct SpanSplitEnumerator<T> where T : IEquatable<T>
{
    private readonly ReadOnlySpan<T> _buffer;

    private readonly ReadOnlySpan<T> _separators;
    private readonly T _separator;

    private readonly int _separatorLength;
    private readonly bool _splitOnSingleToken;

    private readonly bool _isInitialized;

    private int _startCurrent;
    private int _endCurrent;
    private int _startNext;

    /// <summary>
    /// Returns an enumerator that allows for iteration over the split span.
    /// </summary>
    /// <returns>Returns a <see cref="SpanSplitEnumerator{T}"/> that can be used to iterate over the split span.</returns>
    public SpanSplitEnumerator<T> GetEnumerator() => this;

    /// <summary>
    /// Returns the current element of the enumeration.
    /// </summary>
    /// <returns>Returns a <see cref="System.Range"/> instance that indicates the bounds of the current element withing the source span.</returns>
    public Range Current => new Range(_startCurrent, _endCurrent);

    internal SpanSplitEnumerator(ReadOnlySpan<T> span, ReadOnlySpan<T> separators)
    {
        _isInitialized = true;
        _buffer = span;
        _separators = separators;
        _separator = default!;
        _splitOnSingleToken = false;
        _separatorLength = _separators.Length != 0 ? _separators.Length : 1;
        _startCurrent = 0;
        _endCurrent = 0;
        _startNext = 0;
    }

    internal SpanSplitEnumerator(ReadOnlySpan<T> span, T separator)
    {
        _isInitialized = true;
        _buffer = span;
        _separator = separator;
        _separators = default;
        _splitOnSingleToken = true;
        _separatorLength = 1;
        _startCurrent = 0;
        _endCurrent = 0;
        _startNext = 0;
    }

    /// <summary>
    /// Advances the enumerator to the next element of the enumeration.
    /// </summary>
    /// <returns><see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the enumeration.</returns>
    public bool MoveNext()
    {
        if (!_isInitialized || _startNext > _buffer.Length)
        {
            return false;
        }

        ReadOnlySpan<T> slice = _buffer.Slice(_startNext);
        _startCurrent = _startNext;

        int separatorIndex = _splitOnSingleToken ? slice.IndexOf(_separator) : slice.IndexOf(_separators);
        int elementLength = (separatorIndex != -1 ? separatorIndex : slice.Length);

        _endCurrent = _startCurrent + elementLength;
        _startNext = _endCurrent + _separatorLength;
        return true;
    }
}

public static partial class MemoryExtensions
{
    /// <summary>
    /// Returns a type that allows for enumeration of each element within a split span
    /// using a single space as a separator character.
    /// </summary>
    /// <param name="span">The source span to be enumerated.</param>
    /// <returns>Returns a <see cref="SpanSplitEnumerator{T}"/>.</returns>
    public static SpanSplitEnumerator<char> Split(this ReadOnlySpan<char> span)
        => new SpanSplitEnumerator<char>(span, ' ');

    /// <summary>
    /// Returns a type that allows for enumeration of each element within a split span
    /// using the provided separator character.
    /// </summary>
    /// <param name="span">The source span to be enumerated.</param>
    /// <param name="separator">The separator character to be used to split the provided span.</param>
    /// <returns>Returns a <see cref="SpanSplitEnumerator{T}"/>.</returns>
    public static SpanSplitEnumerator<char> Split(this ReadOnlySpan<char> span, char separator)
        => new SpanSplitEnumerator<char>(span, separator);

    /// <summary>
    /// Returns a type that allows for enumeration of each element within a split span
    /// using the provided separator string.
    /// </summary>
    /// <param name="span">The source span to be enumerated.</param>
    /// <param name="separator">The separator string to be used to split the provided span.</param>
    /// <returns>Returns a <see cref="SpanSplitEnumerator{T}"/>.</returns>
    public static SpanSplitEnumerator<char> Split(this ReadOnlySpan<char> span, string separator)
        => new SpanSplitEnumerator<char>(span, separator ?? string.Empty);
}
