using BenchmarkDotNet.Attributes;

namespace Flappy;

[MemoryDiagnoser]
public class ExecContextBenchmark
{
  [Benchmark]
  public void RunSingleJob()
  {
    EmptyPatternBuilder empty;
    ExecContext.Execute(ref empty);
    Fluppy.PollCompleted();
  }

  private static void RunMultipleInternal()
  {
    EmptyPatternBuilder empty;
    ExecContext.Execute(ref empty);
    ExecContext.Execute(ref empty);
    ExecContext.Execute(ref empty);
    ExecContext.Execute(ref empty);
    ExecContext.Execute(ref empty);
  }

  [Benchmark]
  public void RunMultipleJobs()
  {
    RunMultipleInternal();
    Fluppy.PollCompleted();
  }
}