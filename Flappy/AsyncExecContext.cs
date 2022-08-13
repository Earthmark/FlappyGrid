using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;
using FlatBuffers;
using Noise;

namespace Flappy;

public class AsyncExecContext : Fluppy, IValueTaskSource<ExecResult>
{
  private static readonly ConcurrentBag<AsyncExecContext> ContextCache = new();

  private ManualResetValueTaskSourceCore<ExecResult> _resultSource;

  ExecResult IValueTaskSource<ExecResult>.GetResult(short token) => _resultSource.GetResult(token);
  ValueTaskSourceStatus IValueTaskSource<ExecResult>.GetStatus(short token) => _resultSource.GetStatus(token);
  void IValueTaskSource<ExecResult>.OnCompleted(Action<object?> continuation, object? state, short token,
    ValueTaskSourceOnCompletedFlags flags) =>
    _resultSource.OnCompleted(continuation, state, token, flags);

  [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
  public static async ValueTask<ExecResult> ExecuteAsync<TCommandBuilder>(TCommandBuilder builder)
    where TCommandBuilder : IResBuilder<Offset<Pattern>>
  {
    if (!ContextCache.TryTake(out var ctx))
    {
      ctx = new AsyncExecContext();
    }

    ctx._resultSource.Reset();
    ctx.QueueExec(ref builder);

    var result = await new ValueTask<ExecResult>(ctx, ctx._resultSource.Version);

    builder.DestroyPattern();

    ContextCache.Add(ctx);

    return result;
  }

  protected override void Complete(in ExecResult result)
  {
    _resultSource.SetResult(result);
  }
}