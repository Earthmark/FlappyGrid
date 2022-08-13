using System.Collections.Concurrent;
using FlatBuffers;
using Noise;

namespace Flappy;

public class ExecContext : Fluppy
{
  private static readonly ConcurrentBag<ExecContext> ContextCache = new();

  public delegate void OnComplete(object? context, in ExecResult result);

  private OnComplete _onComplete = Identity;
  private object? _state;
  
  private static void Identity(object? context, in ExecResult result)
  {
  }

  public static void Execute<TCommandBuilder>(ref TCommandBuilder builder, OnComplete? onComplete = null,
    object? state = null)
    where TCommandBuilder : IResBuilder<Offset<Pattern>>
  {
    if (!ContextCache.TryTake(out var ctx))
    {
      ctx = new ExecContext();
    }

    ctx._onComplete = onComplete ?? Identity;
    ctx._state = state;

    ctx.QueueExec(ref builder);
  }

  protected override void Complete(in ExecResult result)
  {
    _onComplete(_state, in result);

    ContextCache.Add(this);
  }
}

