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
		public EbnfRootSyntax (ReadOnlyMemory<char> source, Token[] tokens) : base (source, tokens) { }
    }
	public class EbnfSyntaxNode : SyntaxNode {
		public EbnfSyntaxNode(int startLine, int tokenBase)
			: base (startLine, tokenBase) {
			}
	}

	public class ProductionSyntax : SyntaxNode {
		public int? ncname, equal;
		public ExpressionSyntax expression;
		public ProductionSyntax(int startLine, int tokenBase)
			: base (startLine, tokenBase) {
			}
		public ProductionSyntax(int name, int startLine, int tokenBase)
			: base (startLine, tokenBase) {
				ncname = name;
			}
        public override bool IsComplete => base.IsComplete;
    }	
	public class ExpressionSyntax : SyntaxNode {
		public ExpressionSyntax(int startLine, int tokenBase)
			: base (startLine, tokenBase) {	}
	}
	public class LinkSyntax : ExpressionSyntax {
		public int? openBracket, closingBracket;
		public LinkSyntax(int openBracket, int startLine, int tokenBase)
			: base (startLine, tokenBase) {
				this.openBracket = openBracket;
			}
	}
	public class ChoiceSyntax : ExpressionSyntax {
		public ChoiceSyntax(int startLine, int tokenBase)
			: base (startLine, tokenBase) {
			}
	}
	// (Item ( '-' Item | Item* ))?
	public class SequenceOrDifferenceSyntax : SyntaxNode {
		public SequenceOrDifferenceSyntax(int startLine, int tokenBase)
			: base (startLine, tokenBase) {	}
	}
	// Item ::=  Primary ( '?' | '*' | '+' )?   */
	public class ItemSyntax : SyntaxNode {
		public ItemSyntax(int startLine, int tokenBase)
			: base (startLine, tokenBase) {	}
	}
	/* NCName | StringLiteral | CharCode | CharClass | '(' Choice ')'    */
	public class PrimarySyntax : SyntaxNode {
		public PrimarySyntax(int startLine, int tokenBase)
			: base (startLine, tokenBase) {
			}
	}
	// StringLiteral ::= '"' [^"]* '"' | "'" [^']* "'"	
	public class StringLiteralSyntax : SyntaxNode {
		public StringLiteralSyntax(int startLine, int tokenBase)
			: base (startLine, tokenBase) {
				
			}
	}
	// CharCode ::= '#x' [0-9a-fA-F]+
	public class CharCodeSyntax : SyntaxNode {
		public CharCodeSyntax(int startLine, int tokenBase)
			: base (startLine, tokenBase) {
			}
	}
	// CharClass ::= '[' '^'? ( Char | CharCode | CharRange | CharCodeRange )+ ']'
	public class CharClassSyntax : SyntaxNode {
		public int? closingBracket;
		public CharClassSyntax(int startLine, int tokenBase)
			: base (startLine, tokenBase) {}
	}
	// Char ::= #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF]	 any Unicode character, excluding the surrogate blocks, FFFE, and FFFF.
	public class CharSyntax : SyntaxNode {
		public CharSyntax(int startLine, int tokenBase)
			: base (startLine, tokenBase) {
			}
	}


}