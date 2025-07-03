// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;

using CrowEditBase;

namespace CrowEdit.Ebnf
{

	public class EbnfRootSyntax : SyntaxRootNode {
		public EbnfRootSyntax (ReadOnlyTextBuffer source, Token[] tokens) : base (source, tokens) { }
    }
	public class EbnfSyntaxNode : SyntaxNode {
	}

	public class ProductionSyntax : MultiNodeSyntax {
    }	
	public class ExpressionSyntax : MultiNodeSyntax {
	}
	public class LinkSyntax : ExpressionSyntax {
	}
	public class ChoiceSyntax : MultiNodeSyntax {
	}
	// (Item ( '-' Item | Item* ))?
	public class SequenceOrDifferenceSyntax : MultiNodeSyntax {
	}
	// Item ::=  Primary ( '?' | '*' | '+' )?   */
	public class ItemSyntax : MultiNodeSyntax {
	}
	/* NCName | StringLiteral | CharCode | CharClass | '(' Choice ')'    */
	public class PrimarySyntax : SyntaxNode {}
	
	// StringLiteral ::= '"' [^"]* '"' | "'" [^']* "'"	
	public class StringLiteralSyntax : SyntaxNode {
	}
	// CharCode ::= '#x' [0-9a-fA-F]+
	public class CharCodeSyntax : SyntaxNode {
	}
	// CharClass ::= '[' '^'? ( Char | CharCode | CharRange | CharCodeRange )+ ']'
	public class CharClassSyntax : SyntaxNode {
	}
	// Char ::= #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF]	 any Unicode character, excluding the surrogate blocks, FFFE, and FFFF.
	public class CharSyntax : SyntaxNode {
	}


}