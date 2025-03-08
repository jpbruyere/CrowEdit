// Copyright (c) 2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
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
	public class ConstantDefinitionSyntax : MultiNodeSyntax {
		public ConstantDefinitionSyntax(ConstantNameSyntax nameNode) {
			AddChild(nameNode);
		}
        public override bool IsComplete => ChildSequenceIs
		(
			typeof(ConstantNameSyntax),
			StyleTokenType.EqualSign,
			StyleTokenType.MemberValueOpen,
			StyleTokenType.MemberValuePart,
			StyleTokenType.MemberValueClose,
			StyleTokenType.EndOfExpression
		);
	}
	public class StyleDefinitionSyntax : MultiNodeSyntax {
		public StyleDefinitionSyntax(StyleIdentifierSyntax nameNode) {
			AddChild(nameNode);
		}
        /*public override bool IsComplete => ChildSequenceIs
		(
			typeof(ConstantNameSyntax),
			StyleTokenType.EqualSign,
			StyleTokenType.MemberValueOpen,
			StyleTokenType.MemberValuePart,
			StyleTokenType.MemberValueClose,
			StyleTokenType.EndOfExpression
		);*/		
	}	

	public class ConstantNameSyntax : SingleTokenSyntax {
		public ConstantNameSyntax(Token name) : base(name) {}
        public override bool IsComplete => token.GetTokenType() == StyleTokenType.Name;
    }
	public class StyleIdentifierSyntax : SingleTokenSyntax {
		public StyleIdentifierSyntax(Token name) : base(name) {}
        public override bool IsComplete => token.GetTokenType() == StyleTokenType.Name;
	}
	public class MemberIdentifierSyntax : SingleTokenSyntax {
		public MemberIdentifierSyntax(Token name) : base(name) {}
        public override bool IsComplete => token.GetTokenType() == StyleTokenType.Name;
	}
	public class MemberListSyntax : MultiNodeSyntax {

	}
	public class MemberSyntax : MultiNodeSyntax {
		public MemberSyntax(MemberIdentifierSyntax name) {
			AddChild(name);
		}
	}


	public class ImlValueSyntax : MultiNodeSyntax {

	}
	public class ConstanteReferenceSyntax : MultiNodeSyntax {
	}
}