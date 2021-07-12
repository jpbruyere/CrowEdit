// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System.Diagnostics;

namespace CrowEditBase
{
	[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
	public abstract class StackFrame
	{
		public int Level;		
		public string FileFullPath;
		public int Line;
		public int Column;
		public int LineEnd;
		public int ColumnEnd;
		public string Function;
		public string Address;

		public bool IsDefined => !string.IsNullOrEmpty(FileFullPath);

		public override string ToString() => $"{Level}:{FileFullPath}({Line},{Column} {Function})";
		string GetDebuggerDisplay() => ToString();
	}			
}
