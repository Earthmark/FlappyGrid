using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatBuffers;
using Noise;

namespace Flappy;

internal class Tacos
{
  public static byte[] GetProgram()
  {
    FlatBufferBuilder builder = new(1024);

    Pattern.StartPattern(builder);
    var pattern = Pattern.EndPattern(builder);
    Pattern.FinishPatternBuffer(builder, pattern);
    return builder.DataBuffer.ToSizedArray();
  }

  public static void Execute()
  {
    var program = GetProgram();

  }
}
