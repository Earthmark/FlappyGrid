using FlatBuffers;
using Noise;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Sources;

namespace Flappy;

public interface IResBuilder<out TResource>
{
  TResource CreatePattern(ExecContext ctx);
  void DestroyPattern(ExecContext ctx);
}

public struct EmptyPatternBuilder : IResBuilder<Offset<Pattern>>
{
  public Offset<Pattern> CreatePattern(ExecContext ctx)
  {
    Pattern.StartPattern(ctx.Builder);
    return Pattern.EndPattern(ctx.Builder);
  }

  public void DestroyPattern(ExecContext ctx)
  {

  }
}

public class ExecContext : IValueTaskSource<ExecContext.ExecResult>, IDisposable
{
  private ManualResetValueTaskSourceCore<ExecResult> _resultSource;

  private readonly FlatBufferBuilder _builder = new(1024);

  private int? _executing;

  private GCHandle _handle;
  private InteropByteSpan _nativeSpan;

  public FlatBufferBuilder Builder =>
    !_executing.HasValue ? _builder : throw new InvalidOperationException("Context is currently executing");

  public ValueTask<ExecResult> Result => _executing.HasValue
    ? new ValueTask<ExecResult>(this, _resultSource.Version)
    : throw new InvalidOperationException("Context is currently being populated and is not running.");

  public ExecContext()
  {
    _nativeSpan = new InteropByteSpan(_builder.DataBuffer.ToSizedArraySegment(), out _handle);
  }

  private ref InteropByteSpan ToInteropSpan()
  {
    var sizedSegment = _builder.DataBuffer.ToSizedArraySegment();
    if (!ReferenceEquals(sizedSegment.Array, _handle.Target))
    {
      _handle.Free();
      _nativeSpan = new InteropByteSpan(sizedSegment, out _handle);
    }

    return ref _nativeSpan;
  }

  private void SetResult(in ExecResult result)
  {
    _resultSource.SetResult(result);
  }

  // This uses dispose as a way to return the context to the pool.
  // If it was finalized nothing needs to be removed here.
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
  public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
  {
    _builder.Clear();
    _resultSource.Reset();

    if (_executing.HasValue)
    {
      // don't care about early dispose.
      //RunningJobs.TryUpdate(_executing.Value, null, this);
    }

    _executing = null;

    Builders.Add(this);
  }

  ExecResult IValueTaskSource<ExecResult>.GetResult(short token) => _resultSource.GetResult(token);

  ValueTaskSourceStatus IValueTaskSource<ExecResult>.GetStatus(short token) => _resultSource.GetStatus(token);

  void IValueTaskSource<ExecResult>.OnCompleted(Action<object?> continuation, object? state, short token,
    ValueTaskSourceOnCompletedFlags flags) =>
    _resultSource.OnCompleted(continuation, state, token, flags);

  private static readonly ConcurrentBag<(int Handle, ExecContext Context)> RunningJobs = new();

  private static readonly ConcurrentBag<ExecContext> Builders = new();

  static ExecContext()
  {
    for (var i = 0; i < 20; i++)
    {
      Builders.Add(new ExecContext());
    }
  }

  [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
  public static async ValueTask<ExecResult> Execute<TCommandBuilder>(TCommandBuilder builder)
    where TCommandBuilder : IResBuilder<Offset<Pattern>>
  {
    if (!Builders.TryTake(out var ctx))
    {
      ctx = new ExecContext();
    }

    var execId = ctx.ExecuteContext(ref builder);

    if (execId != null)
    {
      RunningJobs.Add((execId.Value, ctx));
    }

    var result = await ctx.Result;

    builder.DestroyPattern(ctx);

    return result;
  }

  private int? ExecuteContext<TCommandBuilder>(ref TCommandBuilder builder)
    where TCommandBuilder : IResBuilder<Offset<Pattern>>
  {
    Pattern.FinishPatternBuffer(Builder, builder.CreatePattern(this));
    return _executing = Execute(ToInteropSpan());
  }

  private static readonly Dictionary<int, ExecContext> QueuedJobs = new();

  private static List<ExecResult> _missedJobs = new(128);
  private static List<ExecResult> _missedJobsBackBuffer = new(128);

  public static void PollCompleted()
  {
    while (RunningJobs.TryTake(out var item))
    {
      QueuedJobs.Add(item.Handle, item.Context);
    }

    foreach (var job in _missedJobs)
    {
      if (QueuedJobs.Remove(job.Exec, out var tcs))
      {
        tcs.SetResult(job);
      }

      _missedJobsBackBuffer.Add(job);
    }

    (_missedJobs, _missedJobsBackBuffer) = (_missedJobsBackBuffer, _missedJobs);
    _missedJobsBackBuffer.Clear();

    foreach (ref var job in ExecutionResults().ToSpan())
    {
      if (QueuedJobs.Remove(job.Exec, out var ctx))
      {
        ctx.SetResult(job);
      }
      else
      {
        _missedJobs.Add(job);
      }
    }
  }

  [StructLayout(LayoutKind.Sequential)]
  private readonly struct InteropByteSpan
  {
    public InteropByteSpan(in ArraySegment<byte> data, out GCHandle handle)
    {
      handle = GCHandle.Alloc(data.Array, GCHandleType.Pinned);

      _start = handle.AddrOfPinnedObject() + data.Offset;
      _length = data.Count;
    }

    // ReSharper disable twice PrivateFieldCanBeConvertedToLocalVariable
    private readonly IntPtr _start;
    private readonly int _length;
  }

  [StructLayout(LayoutKind.Sequential)]
  public record struct ExecResult(int Exec, int Code, ulong StartNanos, ulong EndNanos, int StatusCode);

  [StructLayout(LayoutKind.Sequential)]
  private readonly unsafe struct ExecResultSpan
  {
    private readonly ExecResult* _results;
    private readonly int _length;

    public Span<ExecResult> ToSpan()
    {
      return new Span<ExecResult>(_results, _length);
    }
  }

  [DllImport("Fluppy.dll", EntryPoint = "execute")]
  private static extern int Execute(InteropByteSpan commands);

  [DllImport("Fluppy.dll", EntryPoint = "execution_results")]
  private static extern ExecResultSpan ExecutionResults();
}
