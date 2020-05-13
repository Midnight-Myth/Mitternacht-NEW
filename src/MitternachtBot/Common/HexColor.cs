using System.Globalization;
using System.Text.RegularExpressions;
using Discord;

namespace Mitternacht.Common {
	public class HexColor {
		public byte Red   { get; }
		public byte Green { get; }
		public byte Blue  { get; }

		public HexColor(byte red, byte green, byte blue) {
			Red   = red;
			Green = green;
			Blue  = blue;
		}

		public static bool TryParse(string s, out HexColor hcolor) {
			s      = s.Trim().ToLowerInvariant();
			hcolor = null;
			if(string.IsNullOrWhiteSpace(s)) return false;

			var regex = new Regex("#?([a-f\\d]{2})([a-f\\d]{2})([a-f\\d]{2})");
			if(!regex.IsMatch(s)) return false;

			var match = regex.Match(s);
			if(!byte.TryParse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var red)
				|| !byte.TryParse(match.Groups[2].Value, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var green)
				|| !byte.TryParse(match.Groups[3].Value, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var blue))
				return false;

			hcolor = new HexColor(red, green, blue);
			return true;
		}

		public Color ToColor()
			=> new Color(Red, Green, Blue);

		public static implicit operator Color(HexColor hc)
			=> hc.ToColor();

		public static implicit operator Optional<Color>(HexColor hc)
			=> new Optional<Color>(hc);

		public override string ToString()
			=> $"#{Red:X2}{Green:X2}{Blue:X2}";
	}
}