using BenchmarkDotNet.Attributes;

namespace Flappy;

public class PollTest
{
  [Benchmark]
  public void Test()
  {
    EmptyPatternBuilder empty;
    using var jobTask = Api.Execute(ref empty);

    Api.PollCompleted();

    jobTask.Result.GetAwaiter().GetResult();
  }
}
