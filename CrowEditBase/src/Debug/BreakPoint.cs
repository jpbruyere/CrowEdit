// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System.Diagnostics;

namespace CrowEditBase
{
	[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
	public class BreakPoint : CrowEditComponent
	{
		int index = -1;
		public string Function;		
		string fileFullPath;
		int line;
		bool isEnabled;

		string type;
		string disp;
		string warning;
		
		public int Index {
			get => index;
			set {
				if (index == value)
					return;
				index = value;
				NotifyValueChanged(index);
			}
		}
		public int Line {
			get => line;
			set {
				if (line == value)
					return;
				line = value;
				NotifyValueChanged (line);
			}
		}
		public bool IsEnabled {
			get => isEnabled;
			set {
				if (isEnabled == value)
					return;
				isEnabled = value;
				NotifyValueChanged (isEnabled);
			}
		}
		public string Type {
			get => type;
			set {
				if (type == value)
					return;
				type = value;
				NotifyValueChanged (type);
			}
		}
		public string Disp {
			get => disp;
			set {
				if (disp == value)
					return;
				disp = value;
				NotifyValueChanged (disp);
			}
		}
		public string Warning {
			get => warning;
			set {
				if (warning == value)
					return;
				warning = value;
				NotifyValueChanged (warning);
			}
		}		
		public string FileFullPath {
			get => fileFullPath;
			set {
				if (fileFullPath == value)
					return;
				fileFullPath = value;
				NotifyValueChanged (fileFullPath);
			}
		}

		protected BreakPoint(string fileFullPath, int line, bool isEnabled = true)
		{
			FileFullPath = fileFullPath;
			Line = line;
			IsEnabled = isEnabled;
		}

		public void UpdateLocation (StackFrame frame) {
			//FileName = frame.File;
			FileFullPath = frame.FileFullPath;
			Function = frame.Function;
			Line = frame.Line - 1;
		}
		public override string ToString() => $"{Index}:{Type} {FileFullPath}:{Line} enabled:{IsEnabled}";
		private string GetDebuggerDisplay() => ToString();
	}
}
