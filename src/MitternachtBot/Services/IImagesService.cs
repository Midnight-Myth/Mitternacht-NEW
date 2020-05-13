using System.Collections.Immutable;

namespace Mitternacht.Services
{
    public interface IImagesService : IMService
    {
        ImmutableArray<byte> Heads { get; }
        ImmutableArray<byte> Tails { get; }

        ImmutableArray<(string, ImmutableArray<byte>)> Currency { get; }
        ImmutableArray<ImmutableArray<byte>> Dice { get; }

        ImmutableArray<byte> WifeMatrix { get; }
        ImmutableArray<byte> RategirlDot { get; }

        void Reload();
    }
}
