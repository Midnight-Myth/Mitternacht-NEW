using Mitternacht.Database.Models;
using System.Collections.Generic;

namespace Mitternacht.Common {
	public class ModerationPoints : IModerationPoints {
		public long PointsHard   { get; set; }
		public long PointsMedium { get; set; }
		public long PointsLight  { get; set; }

		public ModerationPoints(long hard, long medium, long light) {
			PointsHard   = hard;
			PointsMedium = medium;
			PointsLight  = light;
		}

		public override string ToString()
			=> PointsHard == 0 && PointsMedium == 0 && PointsLight == 0 ? "0L" : $"{(PointsHard != 0 ? $"{PointsHard}H" : "")}{(PointsMedium != 0 ? $"{PointsMedium}M" : "")}{(PointsLight != 0 ? $"{PointsLight}L" : "")}";

		public static ModerationPoints operator +(ModerationPoints a, IModerationPoints b)
			=> new ModerationPoints(a.PointsHard + b.PointsHard, a.PointsMedium + b.PointsMedium, a.PointsLight + b.PointsLight);

		public static ModerationPoints FromList(IEnumerable<IModerationPoints> moderationPoints) {
			var hard   = 0l;
			var medium = 0l;
			var light  = 0l;
			
			foreach(var mp in moderationPoints) {
				hard   += mp.PointsHard;
				medium += mp.PointsMedium;
				light  += mp.PointsLight;
			}

			return new ModerationPoints(hard, medium, light);
		}

		public static explicit operator ModerationPoints(Warning warning)
			=> new ModerationPoints(warning.PointsHard, warning.PointsMedium, warning.PointsLight);
	}
}
