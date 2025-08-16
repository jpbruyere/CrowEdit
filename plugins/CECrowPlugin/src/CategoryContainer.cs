// Copyright (c) 2013-2025  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System.Diagnostics;
using CrowEditBase;
using System.Collections.Generic;

namespace CECrowPlugin
{
	[DebuggerDisplay ("{Name}")]
	public class CategoryContainer : CrowEditComponent
	{
		bool _isExpanded = true;

		public readonly IEnumerable<PropertyContainer> Properties;
		public readonly string Name;

		public bool IsExpanded
		{
			get { return _isExpanded; }
			set
			{
				if (value == _isExpanded)
					return;

				_isExpanded = value;

				NotifyValueChanged ("IsExpanded", _isExpanded);
			}
		}

		public CategoryContainer (string categoryName, IEnumerable<PropertyContainer> properties){
			Name = categoryName;
			Properties = properties;
		}
	}
}

