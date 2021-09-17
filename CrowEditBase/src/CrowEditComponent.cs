// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow;
using System.Runtime.CompilerServices;

namespace CrowEditBase
{
	public class CrowEditComponent : IValueChange, ISelectable {
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public void NotifyValueChanged (string MemberName, object _value)
		{
			//Debug.WriteLine ("Value changed: {0}->{1} = {2}", this, MemberName, _value);
			ValueChanged.Raise (this, new ValueChangeEventArgs (MemberName, _value));
		}
		public void NotifyValueChanged (object _value, [CallerMemberName] string caller = null)
		{
			NotifyValueChanged (caller, _value);
		}
		#endregion

		#region ISelectable implementation
		bool isSelected;
		public event EventHandler Selected;
		public event EventHandler Unselected;

		public virtual bool IsSelected {
			get => isSelected;
			set {
				if (value == isSelected)
					return;

				isSelected = value;

				NotifyValueChanged (isSelected);
			}
		}
		#endregion
	}
}