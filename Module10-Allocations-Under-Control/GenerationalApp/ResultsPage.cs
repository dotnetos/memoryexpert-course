namespace GenerationalApp
{
    public class ResultsPage
    {
        public int Count { get; set; }
        public string? Next { get; set; }
        public string? Previous { get; set; }
        public Result[] Results { get; set; }
    }
}