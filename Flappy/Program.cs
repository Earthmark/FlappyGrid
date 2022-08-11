using Flappy;

EmptyPatternBuilder empty;

var j1 = ExecContext.Execute(empty);
var j2 = ExecContext.Execute(empty);
var j3 = ExecContext.Execute(empty);
var j4 = ExecContext.Execute(empty);
var j5 = ExecContext.Execute(empty);
var j6 = ExecContext.Execute(empty);

ExecContext.PollCompleted();

var j7 = ExecContext.Execute(empty);
var j8 = ExecContext.Execute(empty);

ExecContext.PollCompleted();

await j1;
await j2;
await j3;
await j4;
await j5;
await j6;
await j7;
await j8;

BenchmarkDotNet.Running.BenchmarkRunner.Run(typeof(Program).Assembly);
