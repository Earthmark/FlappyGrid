using Flappy;

EmptyPatternBuilder empty;

using (ExecContext.Execute(ref empty))
using (ExecContext.Execute(ref empty))
using (ExecContext.Execute(ref empty))
using (ExecContext.Execute(ref empty))
using (ExecContext.Execute(ref empty))
using (var jobTask = ExecContext.Execute(ref empty))
{
  ExecContext.PollCompleted();

  using (ExecContext.Execute(ref empty))
  using (ExecContext.Execute(ref empty))
  using (ExecContext.Execute(ref empty))
  using (ExecContext.Execute(ref empty))
  using (ExecContext.Execute(ref empty))
  {
    ExecContext.PollCompleted();
  }

  var job = await jobTask.Result;
}

BenchmarkDotNet.Running.BenchmarkRunner.Run(typeof(Program).Assembly);
