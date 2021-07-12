// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Runtime.CompilerServices;
using Crow;

namespace CrowEditBase
{
	public class DebuggerObject : IValueChange {
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public void NotifyValueChanged(string MemberName, object _value) 				
			=> ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		
		public void NotifyValueChanged(object _value, [CallerMemberName] string caller = null)
			=> NotifyValueChanged(caller, _value);
		#endregion		
	}	
}