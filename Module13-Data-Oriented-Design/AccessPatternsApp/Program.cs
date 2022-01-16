using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;

// TODO:
// - just run one or another benchmark, observe results!
// - try to use commented HardwareCounters attribute in FalseSharingBenchmarks.
//   They are based on PMC (Precise Machine Counter) events.
//    - LLC_MISSES - Last level cache demand requests from this core that missed the LLC
//    - LLC_REFS - Last level cache demand requests from this core
// NOTE: To use PMC hardware counters you have to DISABLE virtualization om your machine!

//BenchmarkRunner.Run<AccessPatternsBenchmarks>();
//BenchmarkRunner.Run<FalseSharingBenchmarks>();

public class AccessPatternsBenchmarks
{
    [Params(50, 500, 5000)]
    public int Size { get; set; }

    [Benchmark]
    public void IJAccess()
    {
        int n = Size;
        int m = Size;
        int[,] tab = new int[n, m];
        for (int i = 0; i < n; ++i)
        {
            for (int j = 0; j < m; ++j)
                tab[i, j] = 1;
        }
    }

    [Benchmark]
    public void JIAccess()
    {
        int n = Size;
        int m = Size;
        int[,] tab = new int[n, m];
        for (int i = 0; i < n; ++i)
        {
            for (int j = 0; j < m; ++j)
                tab[j, i] = 1;
        }
    }
}

[MemoryDiagnoser]
//[HardwareCounters(HardwareCounter.CacheMisses, HardwareCounter.LlcMisses)]
public class FalseSharingBenchmarks
{
    [Params(100_000_000)]
    public int Size { get; set; }

    public int[] sharedData = new int[4];
    public int[] sharedData2 = new int[4 * 16];
    public int[] sharedData3 = new int[4 * 16 + 16];

    [Benchmark]
    public void DoSharingTest()
    {
        Thread[] workers = new Thread[4];
        for (int i = 0; i < 4; ++i)
        {
            workers[i] = new Thread(new ParameterizedThreadStart(idx =>
            {
                int index = (int)idx;
                for (int j = 0; j < Size; ++j)
                    sharedData[index] = sharedData[index] + 1;
            }));
        }
        for (int i = 0; i < 4; ++i) workers[i].Start(i);
        for (int i = 0; i < 4; ++i) workers[i].Join();
    }

    [Benchmark]
    public void DoSharingTest2()
    {
        Thread[] workers = new Thread[4];
        for (int i = 0; i < 4; ++i)
        {
            workers[i] = new Thread(new ParameterizedThreadStart(idx =>
            {
                int index = (int)idx;
                for (int j = 0; j < Size; ++j)
                    sharedData2[index * 16] = sharedData2[index * 16] + 1;
            }));
        }
        for (int i = 0; i < 4; ++i) workers[i].Start(i);
        for (int i = 0; i < 4; ++i) workers[i].Join();
    }

    [Benchmark]
    public void DoSharingTest3()
    {
        Thread[] workers = new Thread[4];
        for (int i = 0; i < 4; ++i)
        {
            workers[i] = new Thread(new ParameterizedThreadStart(idx =>
            {
                int index = (int)idx + 1;
                for (int j = 0; j < Size; ++j)
                    sharedData3[index * 16] = sharedData3[index * 16] + 1;
            }));
        }
        for (int i = 0; i < 4; ++i) workers[i].Start(i);
        for (int i = 0; i < 4; ++i) workers[i].Join();
    }
}