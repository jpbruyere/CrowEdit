// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System.Diagnostics;

namespace CrowEditBase
{
	[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
	public class ThreadInfo
	{
		public int Id;
		public string Name;
		public bool IsStopped;
		public bool IsRunning => !IsStopped;

		public override string ToString() => $"{Id}:{Name} Running:{IsRunning})";
		string GetDebuggerDisplay() => ToString();
	}
}
