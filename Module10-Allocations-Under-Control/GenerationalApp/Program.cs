using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Spectre.Console;

namespace GenerationalApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Trie<string, char, int> stringTrie = new();
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
                                var result = await client.GetStringAsync(bookUrl);
                                var words = result.Split(new[] { ' ', '\r', '\n' },
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
                                AnsiConsole.MarkupLine(
                                    $"After parsing '{pageResult.Title}' trie size is {stringTrie.EnumerateNodes().Count()}");
                            }
                        }
                        if (page.Next is null)
                            break;
                        url = page.Next;
                        pageIndex++;
                    }
                });
           }

        private static bool TryNormalize(string word, out string result)
        {
            result = word.ToLowerInvariant()
                .Trim('.', ',', ';', '!', '?', '"', ':', '(', ')', '_', '[', ']');
            if (result.Any(c => !char.IsLetter(c)))
            {
                return false;
            }
            return true;
        }
    }
}
