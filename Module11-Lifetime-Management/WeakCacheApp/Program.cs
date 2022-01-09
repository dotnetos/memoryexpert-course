// Create a weak evition cache, evicting entries to weak references after 4 seconds
var cache = new WeakEvictionCache<string, object>(TimeSpan.FromSeconds(4));

// Add some data
var data1 = new Data(1);
cache.Add("1", data1);
var data2 = new Data(2);
cache.Add("2", data2);

// Promote everything to gen2
GC.Collect();
GC.Collect();
// Both entries should be now in gen2:
Console.WriteLine($"gen{GC.GetGeneration(data1)} & gen{GC.GetGeneration(data2)}");

// Wait more than weak eviction time
Thread.Sleep(6_000);

// We should still be able to get entries from cache - they are weak references but no gen2 GCs yet
if (cache.TryGet("1", out var rdata1))
    Console.WriteLine(rdata1);
else
    Console.WriteLine("No 1!");

if (cache.TryGet("2", out var rdata2))
    Console.WriteLine(rdata2);
else
    Console.WriteLine("No 2!");

// Trigger Full GC, weak evicted items should be garbage collected afterwards
GC.Collect();

// We should not be able to get those entries now
if (cache.TryGet("1", out var rdata3))
    Console.WriteLine(rdata3);
else
    Console.WriteLine("No 1!");

if (cache.TryGet("2", out var rdata4))
    Console.WriteLine(rdata4);
else
    Console.WriteLine("No 2!");

record Data(int value);

internal sealed class StrongToWeakReference<T> : WeakReference where T : class
{
    private T? _strongRef;
    public StrongToWeakReference(T obj) : base(obj)
    {
        _strongRef = obj;
    }

    public void MakeWeak() => _strongRef = null;
    public new T? Target => _strongRef ?? (base.Target as T);
    public bool IsStrong => _strongRef != null;   
}

public class WeakEvictionCache<TKey, TValue>
    where TKey : notnull
    where TValue : class
{
    private readonly Timer evictionTimer;
    private readonly TimeSpan weakEvictionThreshold;
    private readonly Dictionary<TKey, CacheEntry> items;
    public WeakEvictionCache(TimeSpan weakEvictionThreshold)
    {
        this.weakEvictionThreshold = weakEvictionThreshold;
        items = new();
        evictionTimer = new Timer(DoWeakEviction, null, 1_000, 1_000);
    }

    public void Add(TKey key, TValue value)
        => items.Add(key, new CacheEntry()
        {
            Reference = new StrongToWeakReference<TValue>(value),
            LastAccess = DateTime.UtcNow
        });

    public bool TryGet(TKey key, out TValue? result)
    {
        // TODO: implement
        return false;
    }

    public void DoWeakEviction(object? state)
    {
        // TODO: implement - using StrongToWeakReference.MakeWeak on items that are not accessed longer
        // than a configured eviction threshold.
        // Suggestion: print something to the console while calling MakeWeak, to have "diagnostic insight"
        
    }

    struct CacheEntry
    {
        public StrongToWeakReference<TValue> Reference;
        public DateTime LastAccess;
    }
}
