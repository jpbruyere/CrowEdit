// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using CrowEditBase;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CERoslynPlugin
{
	public enum TriviaPos { none, leading, trailing };
	public class CSRootSyntax : SyntaxRootNode {
		public CSRootSyntax (ReadOnlyTextBuffer source) : base (source, null) {	}
		internal void SetTokens(Token[] tokens) {
			this.tokens = tokens;
		}
	}
	public class CSToken : SingleTokenSyntax {
		SyntaxToken cstoken;
		public CSToken(SyntaxToken token, Token tok) : base (tok) {
			cstoken = token;
		}
		public override string ToString() => $"TOK: {cstoken.Kind()}";
	}
	public class CSTriviaSyntax : MultiNodeSyntax {
		public TriviaPos TriviaPos;
		public SyntaxTrivia Trivia;
		public CSTriviaSyntax(SyntaxTrivia trivia, TriviaPos triviaPos) {
			Trivia = trivia;
			TriviaPos = triviaPos;
		}
		public override string ToString() => $"TriviaSyntax({TriviaPos}): {Trivia}";
	}
	public class CSTrivia : SingleTokenSyntax {
		SyntaxTrivia cstrivia;
		public CSTrivia(SyntaxTrivia token, Token tok) : base (tok) {
			cstrivia = token;
		}
		public override string ToString() => $"Trivia: {cstrivia.Kind()}";
	}	
	public class CSSyntaxNode : MultiNodeSyntax {
		protected Microsoft.CodeAnalysis.SyntaxNode node;
		public CSSyntaxNode(Microsoft.CodeAnalysis.SyntaxNode node) {
			this.node = node;
		}
		public override string ToString() => $"{node.Kind()}";
    }
	public class CSUsingDirectiveSyntax : CSSyntaxNode {
		public CSUsingDirectiveSyntax(Microsoft.CodeAnalysis.SyntaxNode node) : base(node) { }
        /*public override int FoldedLineCount => base.FoldedLineCount;
        public override bool IsFoldable => (PreviousSibling == null || !PreviousSibling.GetType().IsAssignableFrom(typeof(UsingDirectiveSyntax)))
			&& NextSibling != null && NextSibling.GetType().IsAssignableFrom(typeof(UsingDirectiveSyntax));*/
	}
}