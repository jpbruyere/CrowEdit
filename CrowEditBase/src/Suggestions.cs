// Copyright (c) 2013-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Text;
using System.Reflection;
using Crow;

namespace CrowEditBase
{
	public class Suggestion {
		public string Caption;
		public TextChange Change;
		public TextSpan? NextSelection;
		public virtual string Icon => "#icons.property.svg";

		public Suggestion(string caption, TextChange change = default, int finalPositionOffset = 0) {
			Caption = caption;
			Change = change;
			NextSelection = finalPositionOffset < 0 ? TextSpan.FromStartAndLength(change.End2 + finalPositionOffset) : null;
		}
	}
	public class MemberInfoSuggestion : Suggestion {
		public MemberInfo MemberInfo;
		public MemberInfoSuggestion(MemberInfo memberInfo, TextChange change = default, int finalPositionOffset = 0) 
			: base(memberInfo.Name, change, finalPositionOffset) {
			MemberInfo = memberInfo;
		}
	}
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
}