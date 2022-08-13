using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Flappy;

[MemoryDiagnoser]
public class AsyncExecContextBenchmark
{
  [Benchmark]
  public void RunSingleJob()
  {
    EmptyPatternBuilder empty;
    var j1 = AsyncExecContext.ExecuteAsync(empty);
    Fluppy.PollCompleted();
    j1.AsTask().GetAwaiter().GetResult();
  }

  [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
  private static async ValueTask RunMultipleInternal()
  {
    EmptyPatternBuilder empty;
    var j1 = AsyncExecContext.ExecuteAsync(empty);
    var j2 = AsyncExecContext.ExecuteAsync(empty);
    var j3 = AsyncExecContext.ExecuteAsync(empty);
    var j4 = AsyncExecContext.ExecuteAsync(empty);
    var j5 = AsyncExecContext.ExecuteAsync(empty);

    await j1;
    await j2;
    await j3;
    await j4;
    await j5;
  }

  [Benchmark]
  public void RunMultipleJobs()
  {
    var job = RunMultipleInternal();
    Fluppy.PollCompleted();
    job.AsTask().GetAwaiter().GetResult();
  }

  [Benchmark]
  public void PollCompleted()
  {
    Fluppy.PollCompleted();
  }
}
