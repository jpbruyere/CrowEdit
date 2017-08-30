using System;

namespace Crow.Coding
{
	public struct TextFormatting {
		public Color Foreground;
		public Color Background;

		public TextFormatting(Color fg, Color bg){
			Foreground = fg;
			Background = bg;
		}
	}
}

