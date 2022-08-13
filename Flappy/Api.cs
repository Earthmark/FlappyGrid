using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using FlatBuffers;
using Noise;

namespace Flappy;

[StructLayout(LayoutKind.Sequential)]
public record struct ExecResult(int Exec, int Code, ulong StartNanos, ulong EndNanos, int StatusCode);

public class Fluppy : IDisposable
{
  private GCHandle _handle;

  public FlatBufferBuilder Builder { get; } = new(1024);

  private InteropByteSpan ToInteropSpan() =>
    new(Builder.DataBuffer.ToSizedArraySegment(), ref _handle);

  protected virtual void Complete(in ExecResult result)
  {
  }

  public Fluppy()
  {
    _handle = GCHandle.Alloc(null, GCHandleType.Pinned);
  }

  ~Fluppy()
  {
    Dispose();
  }

  public void Dispose()
  {
    _handle.Free();
    GC.SuppressFinalize(this);
  }

  private static readonly ConcurrentBag<(int Handle, Fluppy Context)> RunningJobs = new();
  private static readonly Dictionary<int, Fluppy> QueuedJobs = new();

  private static List<ExecResult> _missedJobs = new(128);
  private static List<ExecResult> _missedJobsBackBuffer = new(128);

  public void QueueExec<TCommandBuilder>(ref TCommandBuilder builder)
    where TCommandBuilder : IResBuilder<Offset<Pattern>>
  {
    Builder.Clear();
    Pattern.FinishPatternBuffer(Builder, builder.CreatePattern(Builder));

    var execId = Execute(ToInteropSpan());
    RunningJobs.Add((execId, this));
  }

  public static void PollCompleted()
  {
    while (RunningJobs.TryTake(out var item))
    {
      QueuedJobs.Add(item.Handle, item.Context);
    }

    foreach (var job in _missedJobs)
    {
      if (QueuedJobs.Remove(job.Exec, out var ctx))
      {
        ctx.Complete(in job);
      }

      _missedJobsBackBuffer.Add(job);
    }

    (_missedJobs, _missedJobsBackBuffer) = (_missedJobsBackBuffer, _missedJobs);
    _missedJobsBackBuffer.Clear();

    foreach (ref var job in ExecutionResults().ToSpan())
    {
      if (QueuedJobs.Remove(job.Exec, out var ctx))
      {
        ctx.Complete(in job);
      }
      else
      {
        _missedJobs.Add(job);
      }
    }
  }

  [StructLayout(LayoutKind.Sequential)]
  public readonly struct InteropByteSpan
  {
    public InteropByteSpan(in ArraySegment<byte> data, ref GCHandle handle)
    {
      if (!ReferenceEquals(data.Array, handle.Target))
      {
        handle.Target = data.Array;
      }

      _start = handle.AddrOfPinnedObject() + data.Offset;
      _length = data.Count;
    }

    // ReSharper disable twice PrivateFieldCanBeConvertedToLocalVariable
    private readonly IntPtr _start;
    private readonly int _length;
  }

  [DllImport("Fluppy.dll", EntryPoint = "execute")]
  private static extern int Execute(InteropByteSpan commands);

  [StructLayout(LayoutKind.Sequential)]
  public readonly unsafe struct ExecResultSpan
  {
    private readonly ExecResult* _results;
    private readonly int _length;

    public Span<ExecResult> ToSpan() => new(_results, _length);
  }

  [DllImport("Fluppy.dll", EntryPoint = "execution_results")]
  private static extern ExecResultSpan ExecutionResults();
}
