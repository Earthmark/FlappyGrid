using Flappy;

EmptyPatternBuilder empty;

using (Api.Execute(ref empty))
using (Api.Execute(ref empty))
using (Api.Execute(ref empty))
using (Api.Execute(ref empty))
using (Api.Execute(ref empty))
using (var jobTask = Api.Execute(ref empty))
{
  Api.PollCompleted();

  using (Api.Execute(ref empty))
  using (Api.Execute(ref empty))
  using (Api.Execute(ref empty))
  using (Api.Execute(ref empty))
  using (Api.Execute(ref empty))
  {
    Api.PollCompleted();
  }

  var job = await jobTask.Result;
}

BenchmarkDotNet.Running.BenchmarkRunner.Run(typeof(Program).Assembly);
