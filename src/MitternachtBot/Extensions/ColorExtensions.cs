using Discord;
using System;

namespace Mitternacht.Extensions {
	public static class ColorExtensions {
		// Use a simple euclidean metric to compare colors. However, this is not the same as how the human eye perceives color differences. A better suited metric may have to be implemented.
		public static double Difference(this Color color, Color other)
			=> Math.Sqrt(Math.Pow(color.R - other.R, 2) + Math.Pow(color.G - other.G, 2) + Math.Pow(color.B - other.B, 2));
	}
}
