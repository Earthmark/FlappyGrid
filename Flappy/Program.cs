using Flappy;

EmptyPatternBuilder empty;

ExecContext.Execute(ref empty);
ExecContext.Execute(ref empty);
ExecContext.Execute(ref empty);
ExecContext.Execute(ref empty);
ExecContext.Execute(ref empty);
ExecContext.Execute(ref empty);

Fluppy.PollCompleted();

ExecContext.Execute(ref empty);
ExecContext.Execute(ref empty);

Fluppy.PollCompleted();

BenchmarkDotNet.Running.BenchmarkRunner.Run(typeof(Program).Assembly);
