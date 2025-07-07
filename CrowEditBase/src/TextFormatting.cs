// Copyright (c) 2013-2025  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Runtime.CompilerServices;
using Crow;
using Drawing2D;

namespace CrowEditBase
{
	public class TextFormatting : IValueChange {
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged (string MemberName, object _value) {
			//Debug.WriteLine ("Value changed: {0}->{1} = {2}", this, MemberName, _value);
			if (ValueChanged != null)
				ValueChanged.Invoke (this, new ValueChangeEventArgs (MemberName, _value));
		}
		public void NotifyValueChanged (object _value, [CallerMemberName] string caller = null) {
			if (ValueChanged != null)
				NotifyValueChanged (caller, _value);
		}
        #endregion
        Color foreground, background;
        bool bold, italic;
        public Color Foreground {
			get => foreground;
			set {
				if (foreground == value)
					return;
				foreground = value;
				NotifyValueChanged (foreground);
			}
		}
		public Color Background {
			get => background;
			set {
				if (background == value)
					return;
				background = value;
				NotifyValueChanged (background);
			}
		}
		public bool Bold {
			get => bold;
			set {
				if (bold == value)
					return;
				bold = value;
				NotifyValueChanged (bold);
			}
		}
		public bool Italic {
			get => italic;
			set {
				if (italic == value)
					return;
				italic = value;
				NotifyValueChanged (italic);
			}
		}

		public TextFormatting(Color fg, Color bg, bool _bold = false, bool _italic = false){
			Foreground = fg;
			Background = bg;
			Bold = _bold;
			Italic = _italic;
		}

		public override string ToString ()
			=> $"{Foreground};{Background};{Bold};{Italic}";

		public static TextFormatting Parse (string str) {
			string[] tmp = str.Split (';');
			return new TextFormatting (Color.Parse (tmp[0]), Color.Parse (tmp[1]), bool.Parse (tmp[2]), bool.Parse (tmp[3]));
		}
        
    }
}

