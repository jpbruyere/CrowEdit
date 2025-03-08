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
		public StyleRootSyntax (ReadOnlyTextBuffer source, Token[] tokens) : base (source, tokens) { }
	}
	public class ConstantDefinitionSyntax : SyntaxNode {
	}
	public class StyleIdentifierSyntax : SyntaxNode {
	}
	public class ImlValueSyntax : SyntaxNode {

	}
	public class AttributeSyntax : SyntaxNode {
	}
}