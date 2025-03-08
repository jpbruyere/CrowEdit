// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;

using CrowEditBase;

namespace CECrowPlugin.Style
{

	public class StyleRootSyntax : SyntaxRootNode {
		public StyleRootSyntax (ReadOnlyMemory<char> source, Token[] tokens) : base (source, tokens) { }
	}
	public class ConstantDefinitionSyntax : SyntaxNode {
		internal int? equal;
		public readonly StyleIdentifierSyntax Identifier;
		public readonly ImlValueSyntax Value;
		public override bool IsComplete => base.IsComplete & Identifier != null & equal.HasValue;
		public ConstantDefinitionSyntax (StyleIdentifierSyntax id)
			: base (id.StartLine, id.TokenIndexBase) {
			Identifier = id;
		}
	}
	public class StyleIdentifierSyntax : SyntaxNode {
		public StyleIdentifierSyntax (int startLine, int tokenBase)
			: base (startLine, tokenBase, tokenBase) {
			EndLine = startLine;
		}
	}
	public class ImlValueSyntax : SyntaxNode {
		internal int? valueOpen, value, valueClose;
		public override bool IsComplete => base.IsComplete & value.HasValue &
			valueOpen.HasValue & valueClose.HasValue;
		public ImlValueSyntax (int startLine, int tokenBase)
			: base (startLine, tokenBase) {
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