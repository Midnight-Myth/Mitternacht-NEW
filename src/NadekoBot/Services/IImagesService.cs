﻿using System.Collections.Immutable;

namespace NadekoBot.Services
{
    public interface IImagesService : INService
    {
        ImmutableArray<byte> Heads { get; }
        ImmutableArray<byte> Tails { get; }

        ImmutableArray<(string, ImmutableArray<byte>)> Currency { get; }
        ImmutableArray<ImmutableArray<byte>> Dice { get; }

        ImmutableArray<byte> SlotBackground { get; }
        ImmutableArray<ImmutableArray<byte>> SlotEmojis { get; }
        ImmutableArray<ImmutableArray<byte>> SlotNumbers { get; }

        ImmutableArray<byte> WifeMatrix { get; }
        ImmutableArray<byte> RategirlDot { get; }

        void Reload();
    }
}
