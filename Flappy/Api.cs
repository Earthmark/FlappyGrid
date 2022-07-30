using FlatBuffers;
using Noise;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Sources;

namespace Flappy;

public interface IResBuilder<out TResource>
{
  TResource Write(FlatBufferBuilder builder);
}

public struct EmptyPatternBuilder : IResBuilder<Offset<Pattern>>
{
  public Offset<Pattern> Write(FlatBufferBuilder builder)
  {
    Pattern.StartPattern(builder);
    return Pattern.EndPattern(builder);
  }
}

public static unsafe class Api
{
  public class ExecContext : IValueTaskSource<ExecResult>, IDisposable
  {
    private readonly List<GCHandle> _resourceHandles = new();
    private ManualResetValueTaskSourceCore<ExecResult> _resultSource;

    private readonly FlatBufferBuilder _builder = new(1024);

    private ExecHandle? _executing;

    public FlatBufferBuilder Builder =>
      !_executing.HasValue ? _builder : throw new InvalidOperationException("Context is currently executing");

    public ValueTask<ExecResult> Result => _executing.HasValue
      ? new ValueTask<ExecResult>(this, _resultSource.Version)
      : throw new InvalidOperationException("Context is currently being populated and is not running.");

    public void SetResult(in ExecResult result)
    {
      ClearHandles();
      _resultSource.SetResult(result);
    }

    public void SetException(Exception exception)
    {
      ClearHandles();
      _resultSource.SetException(exception);
    }

    public RefHandle AddResource(byte[] data) => AddResource(new ArraySegment<byte>(data));

    public RefHandle AddResource(in ArraySegment<byte> data)
    {
      fixed (byte* d = data.Array)
      {
        _resourceHandles.Add(GCHandle.Alloc(data.Array, GCHandleType.Pinned));
        var r = AddRef(d + data.Offset, data.Count);
        return r;
      }
    }

    public ExecHandle? ExecuteContext<TCommandBuilder>(ref TCommandBuilder builder) where TCommandBuilder : IResBuilder<Offset<Pattern>>
    {
      Pattern.FinishPatternBuffer(Builder, builder.Write(Builder));

      var patternResource = AddResource(Builder.DataBuffer.ToSizedArraySegment());

      var execId = Api.Execute(patternResource);
      _executing = execId;

      if (execId.Handle == -1)
      {
        SetException(new InvalidOperationException("Buffer failed to load allocated resource"));
        return null;
      }
      if (execId.Handle == -2)
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
  }

  private static readonly ConcurrentDictionary<ExecHandle, ExecContext?> RunningJobs = new();

  private static readonly ConcurrentBag<ExecContext> Builders = new();

  public static ExecContext Execute<TCommandBuilder>(ref TCommandBuilder builder) where TCommandBuilder : IResBuilder<Offset<Pattern>>
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

    foreach (ref var job in SafeExecResults())
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

  private static Span<ExecResult> SafeExecResults()
  {
    ExecutionResults(out var foundResults, out var length);
    return new Span<ExecResult>(foundResults, length);
  }

  [StructLayout(LayoutKind.Sequential)]
  public record struct RefHandle(int Handle);

  [StructLayout(LayoutKind.Sequential)]
  public record struct ExecHandle(int Handle);

  [StructLayout(LayoutKind.Sequential)]
  public record struct ExecResult(ExecHandle Exec, RefHandle Code, ulong StartNanos, ulong EndNanos, int StatusCode);


  [DllImport("Fluppy.dll", EntryPoint = "add_ref")]
  private static extern RefHandle AddRef(byte* data, int length);

  [DllImport("Fluppy.dll", EntryPoint = "execute")]
  private static extern ExecHandle Execute(RefHandle commands);

  [DllImport("Fluppy.dll", EntryPoint = "execution_results")]
  private static extern void ExecutionResults(out ExecResult* data, out int length);
}
