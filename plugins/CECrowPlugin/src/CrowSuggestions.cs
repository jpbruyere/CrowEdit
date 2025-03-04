// Copyright (c) 2013-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Text;
using System.Reflection;
using Crow;
using CrowEditBase;

namespace CECrowPlugin
{
	public class WidgetSuggestion : Suggestion {
		public Type Type;
        public override string Icon => $"#icons.{Type.FullName}.svg";
        public WidgetSuggestion(Type type, TextChange change = default, int finalPositionOffset = 0)
			: base(type.Name, change, finalPositionOffset) {
			Type = type;
		}
	}
	public class ColorSuggestion : Suggestion {
		public Fill Fill;
        public override string Icon => $"#icons.fill.svg";
        public ColorSuggestion(Fill fill, TextChange change = default, int finalPositionOffset = 0)
			: base(fill.ToString(), change, finalPositionOffset) {
			Fill = fill;
		}
	}
	public class CrowPropertySuggestion : MemberInfoSuggestion {
		DesignCategory category;
		public string Category => category == null ? "Divers" : category.Name;
		public override string Icon => $"#icons.{Category}.svg";
		public CrowPropertySuggestion(PropertyInfo pi, TextChange change, int finalPositionOffset = 0)
		: base (pi, change, finalPositionOffset) {
			category = (DesignCategory)MemberInfo.GetCustomAttribute (typeof(DesignCategory));
		}
	}
}