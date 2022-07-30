using System.Runtime.InteropServices;

namespace Flappy
{
  internal class PinnedBuff
  {
    private GCHandle _handle;
    public byte[] Data { get; }

    public PinnedBuff(byte[] data)
    {
      Data = data;
      _handle = GCHandle.Alloc(Data, GCHandleType.Pinned);
    }


  }
}
