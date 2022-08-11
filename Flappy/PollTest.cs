using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Flappy;

[MemoryDiagnoser]
public class PollTest
{
  [Benchmark]
  public void RunSingleJob()
  {
    EmptyPatternBuilder empty;
    var jobTask = ExecContext.Execute(empty);
    ExecContext.PollCompleted();

    jobTask.GetAwaiter().GetResult();
  }

  [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
  private static async ValueTask RunMultipleInternal()
  {
    EmptyPatternBuilder empty;
    var j1 = ExecContext.Execute(empty);
    var j2 = ExecContext.Execute(empty);
    var j3 = ExecContext.Execute(empty);
    var j4 = ExecContext.Execute(empty);
    var j5 = ExecContext.Execute(empty);

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
    ExecContext.PollCompleted();
    job.GetAwaiter().GetResult();
  }

  [Benchmark]
  public void PollCompleted()
  {
    ExecContext.PollCompleted();
  }
}
