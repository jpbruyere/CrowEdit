// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;

using CrowEditBase;

namespace CECrowPlugin
{

	public class StyleRootSyntax : SyntaxRootNode {
		public StyleRootSyntax (StyleDocument source)
			: base (source) {
		}
	}

	public class AttributeSyntax : SyntaxNode {
		public Token? NameToken { get; internal set; }
		public Token? EqualToken { get; internal set; }
		public Token? ValueOpenToken { get; internal set; }
		public Token? ValueCloseToken { get; internal set; }
		public Token? ValueToken { get; internal set; }
		public AttributeSyntax (int startLine, int startTok) : base  (startLine, startTok) {}
		public override bool IsComplete => base.IsComplete & NameToken.HasValue & EqualToken.HasValue &
			ValueToken.HasValue & ValueOpenToken.HasValue & ValueCloseToken.HasValue;
	}
}