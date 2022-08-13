using FlatBuffers;
using Noise;

namespace Flappy;

public interface IResBuilder<out TResource>
{
  TResource CreatePattern(FlatBufferBuilder ctx);
  void DestroyPattern();
}

public struct EmptyPatternBuilder : IResBuilder<Offset<Pattern>>
{
  public Offset<Pattern> CreatePattern(FlatBufferBuilder ctx)
  {
    Pattern.StartPattern(ctx);
    return Pattern.EndPattern(ctx);
  }

  public void DestroyPattern()
  {

  }
}
