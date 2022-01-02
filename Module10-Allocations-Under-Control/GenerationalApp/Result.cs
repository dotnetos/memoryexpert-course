using System.Collections.Generic;

namespace GenerationalApp
{
    public class Result
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public Dictionary<string, string> Formats { get; set; }
    }
}