using Mitternacht.Database.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

		public static ModerationPoints FromString(string moderationPointsString) {
			var match = Regex.Match(moderationPointsString.Trim(), "\\A(?:(?<hard>(\\+|-)?\\d+)(?:H|S))?(?:(?<medium>(\\+|-)?\\d+)M)?(?:(?<light>(\\+|-)?\\d+)L)?\\z", RegexOptions.IgnoreCase);

			if(!string.IsNullOrWhiteSpace(moderationPointsString) && match != null && match.Success){
				if(!long.TryParse(match.Groups["hard"  ].Value, out var hard))
					hard   = 0;
				if(!long.TryParse(match.Groups["medium"].Value, out var medium))
					medium = 0;
				if(!long.TryParse(match.Groups["light" ].Value, out var light))
					light  = 0;

				return new ModerationPoints(hard, medium, light);
			} else {
				throw new ArgumentException($"'{moderationPointsString}' is not a valid moderation points string.", nameof(moderationPointsString));
			}
		}

		public static explicit operator ModerationPoints(Warning warning)
			=> new ModerationPoints(warning.PointsHard, warning.PointsMedium, warning.PointsLight);
	}
}
