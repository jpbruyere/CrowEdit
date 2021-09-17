// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;

using CrowEditBase;

namespace CECrowPlugin
{

	public class StyleRootSyntax : SyntaxNode {
		internal readonly StyleDocument source;
		public override SyntaxNode Root => this;
		public StyleRootSyntax (StyleDocument source)
			: base (source.Tokens.FirstOrDefault (), source.Tokens.LastOrDefault ()) {
			this.source = source;
		}
	}

	public class AttributeSyntax : SyntaxNode {
		public Token? NameToken { get; internal set; }
		public Token? EqualToken { get; internal set; }
		public Token? ValueOpenToken { get; internal set; }
		public Token? ValueCloseToken { get; internal set; }
		public Token? ValueToken { get; internal set; }
		public AttributeSyntax (Token startTok) : base  (startTok) {}
		public override bool IsComplete => base.IsComplete & NameToken.HasValue & EqualToken.HasValue & ValueToken.HasValue & ValueOpenToken.HasValue & ValueCloseToken.HasValue;
	}
}