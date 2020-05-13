using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using NLog;

namespace Mitternacht.Services.Impl
{
    public class ImagesService : IImagesService
    {
        private readonly Logger _log;

        private const string BasePath = "data/images/";

        private const string HeadsPath = BasePath + "coins/heads.png";
        private const string TailsPath = BasePath + "coins/tails.png";

        private const string CurrencyImagesPath = BasePath + "currency";
        private const string DiceImagesPath = BasePath + "dice";

        private const string WifeMatrixPath = BasePath + "rategirl/wifematrix.png";
        private const string RategirlDotPath = BasePath + "rategirl/dot.png";


        public ImmutableArray<byte> Heads { get; private set; }
        public ImmutableArray<byte> Tails { get; private set; }
        
        public ImmutableArray<(string, ImmutableArray<byte>)> Currency { get; private set; }

        public ImmutableArray<ImmutableArray<byte>> Dice { get; private set; }

        public ImmutableArray<byte> WifeMatrix { get; private set; }
        public ImmutableArray<byte> RategirlDot { get; private set; }

        public ImagesService()
        {
            _log = LogManager.GetCurrentClassLogger();
            Reload();
        }

        public void Reload()
        {
            try
            {
                Heads = File.ReadAllBytes(HeadsPath).ToImmutableArray();
                Tails = File.ReadAllBytes(TailsPath).ToImmutableArray();

                Currency = Directory.GetFiles(CurrencyImagesPath)
                    .Select(x => (Path.GetFileName(x), File.ReadAllBytes(x).ToImmutableArray()))
                    .ToImmutableArray();

                Dice = Directory.GetFiles(DiceImagesPath)
                                .OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x)))
                                .Select(x => File.ReadAllBytes(x).ToImmutableArray())
                                .ToImmutableArray();

                WifeMatrix = File.ReadAllBytes(WifeMatrixPath).ToImmutableArray();
                RategirlDot = File.ReadAllBytes(RategirlDotPath).ToImmutableArray();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw;
            }
        }
    }
}