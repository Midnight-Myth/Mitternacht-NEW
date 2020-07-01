using System;
using System.Collections.Generic;
using System.Linq;
using Mitternacht.Common;

namespace Mitternacht.Modules.Gambling.Common {
	public class SlotMachineResult {
		public const int MaxValue = 5;

		static readonly List<Func<int[], int>> WinningCombos = new List<Func<int[], int>> {
            //three flowers
            arr => arr.All(a=>a==MaxValue) ? 30 : 0,
            //three of the same
            arr => !arr.Any(a => a != arr[0]) ? 10 : 0,
            //two flowers
            arr => arr.Count(a => a == MaxValue) == 2 ? 4 : 0,
            //one flower
            arr => arr.Any(a => a == MaxValue) ? 1 : 0
		};

		public static SlotMachineResult Pull() {
			var numbers = new int[3];
			for(var i = 0; i < numbers.Length; i++)
				numbers[i] = new NadekoRandom().Next(0, MaxValue + 1);
			var multi = 0;
			foreach(var t in WinningCombos) {
				multi = t(numbers);
				if(multi != 0)
					break;
			}

			return new SlotMachineResult(numbers, multi);
		}

		public int[] Numbers { get; }
		public int Multiplier { get; }

		public SlotMachineResult(int[] nums, int multi) {
			Numbers = nums;
			Multiplier = multi;
		}
	}
}