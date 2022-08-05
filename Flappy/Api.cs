using FlatBuffers;
using Noise;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Sources;

using RefHandle = System.Int32;
using ExecHandle = System.Int32;

namespace Flappy;

public interface IResBuilder<out TResource>
{
  TResource Write(ExecContext ctx);
}

public struct EmptyPatternBuilder : IResBuilder<Offset<Pattern>>
{
  public Offset<Pattern> Write(ExecContext ctx)
  {
    Pattern.StartPattern(ctx.Builder);
    return Pattern.EndPattern(ctx.Builder);
  }
}

public class ExecContext : IValueTaskSource<ExecContext.ExecResult>, IDisposable
{
  private readonly List<GCHandle> _resourceHandles = new(16);
  private ManualResetValueTaskSourceCore<ExecResult> _resultSource;

  private readonly FlatBufferBuilder _builder = new(1024);

  private ExecHandle? _executing;

  public FlatBufferBuilder Builder =>
    !_executing.HasValue ? _builder : throw new InvalidOperationException("Context is currently executing");

  public ValueTask<ExecResult> Result => _executing.HasValue
    ? new ValueTask<ExecResult>(this, _resultSource.Version)
    : throw new InvalidOperationException("Context is currently being populated and is not running.");

  private void SetResult(in ExecResult result)
  {
    ClearHandles();
    _resultSource.SetResult(result);
  }

  private void SetException(Exception exception)
  {
    ClearHandles();
    _resultSource.SetException(exception);
  }

  private ExecHandle? ExecuteContext<TCommandBuilder>(ref TCommandBuilder builder)
    where TCommandBuilder : IResBuilder<Offset<Pattern>>
  {
    Pattern.FinishPatternBuffer(Builder, builder.Write(this));

    var span = new InteropByteSpan(_builder.DataBuffer.ToSizedArraySegment(), out var handle);
    _resourceHandles.Add(handle);

    var execId = Execute(span);
    _executing = execId;

    if (execId == -1)
    {
      SetException(new InvalidOperationException("Buffer failed to load allocated resource"));
      return null;
    }

    if (execId == -2)
    {
      SetException(new InvalidOperationException("Buffer failed to validate"));
      return null;
    }

    return execId;
  }

  private void ClearHandles()
  {
    foreach (var handle in _resourceHandles)
    {
      handle.Free();
    }

    _resourceHandles.Clear();
  }

  ~ExecContext()
  {
    // If the context was lost, we don't know if we can release the resources or not.
    // To prevent referencing unallocated memory don't free handles,
    // but at least report an error.
    if (_resourceHandles.Count > 0)
    {
      Console.WriteLine("Resource handle retention dropped, memory is probably being leaked.");
    }
  }

  // This uses dispose as a way to return the context to the pool.
  // If it was finalized nothing needs to be removed here.
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
  public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
  {
    _builder.Clear();
    _resultSource.Reset();

    // This might not be needed as this should only be disposed after the value task has completed.
    ClearHandles();

    if (_executing.HasValue)
    {
      RunningJobs.TryUpdate(_executing.Value, null, this);
    }

    _executing = null;

    Builders.Add(this);
  }

  ExecResult IValueTaskSource<ExecResult>.GetResult(short token) => _resultSource.GetResult(token);

  ValueTaskSourceStatus IValueTaskSource<ExecResult>.GetStatus(short token) => _resultSource.GetStatus(token);

  void IValueTaskSource<ExecResult>.OnCompleted(Action<object?> continuation, object? state, short token,
    ValueTaskSourceOnCompletedFlags flags) =>
    _resultSource.OnCompleted(continuation, state, token, flags);

  private static readonly ConcurrentDictionary<ExecHandle, ExecContext?> RunningJobs = new();

  private static readonly ConcurrentBag<ExecContext> Builders = new();

  static ExecContext()
  {
    for (int i = 0; i < 20; i++)
    {
      Builders.Add(new ExecContext());
    }
  }

  public static ExecContext Execute<TCommandBuilder>(ref TCommandBuilder builder)
    where TCommandBuilder : IResBuilder<Offset<Pattern>>
  {
    if (!Builders.TryTake(out var ctx))
    {
      ctx = new ExecContext();
    }

    var execId = ctx.ExecuteContext(ref builder);

    if (execId != null)
    {
      RunningJobs.TryAdd(execId.Value, ctx);
    }

    return ctx;
  }

  private static List<ExecResult> _missedJobs = new(128);
  private static List<ExecResult> _missedJobsBackBuffer = new(128);

  public static void PollCompleted()
  {
    foreach (var job in _missedJobs)
    {
      if (RunningJobs.TryRemove(job.Exec, out var tcs))
      {
        tcs?.SetResult(job);
      }

      _missedJobsBackBuffer.Add(job);
    }

    (_missedJobs, _missedJobsBackBuffer) = (_missedJobsBackBuffer, _missedJobs);
    _missedJobsBackBuffer.Clear();

    foreach (ref var job in ExecutionResults().ToSpan())
    {
      if (RunningJobs.TryRemove(job.Exec, out var tcs))
      {
        tcs?.SetResult(job);
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
  };

  [StructLayout(LayoutKind.Sequential)]
  public record struct ExecResult(ExecHandle Exec, RefHandle Code, ulong StartNanos, ulong EndNanos, int StatusCode);

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
  private static extern ExecHandle Execute(InteropByteSpan commands);

  [DllImport("Fluppy.dll", EntryPoint = "execution_results")]
  private static extern ExecResultSpan ExecutionResults();
}
