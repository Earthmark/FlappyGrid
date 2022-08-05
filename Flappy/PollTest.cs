using BenchmarkDotNet.Attributes;

namespace Flappy;

public class PollTest
{
  [Benchmark]
  public void Test()
  {
    EmptyPatternBuilder empty;
    using var jobTask = ExecContext.Execute(ref empty);
    ExecContext.PollCompleted();

    jobTask.Result.GetAwaiter().GetResult();
  }
}
