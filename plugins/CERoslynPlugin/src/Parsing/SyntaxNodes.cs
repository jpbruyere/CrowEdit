// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using CrowEditBase;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CERoslynPlugin
{
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
	public class CSTrivia : SingleTokenSyntax {
		SyntaxTrivia cstrivia;
		public CSTrivia(SyntaxTrivia token, Token tok) : base (tok) {
			cstrivia = token;
		}
		public override string ToString() => $"Trivia: {cstrivia.Kind()}";
	}	
	public class CSSyntaxNode : MultiNodeSyntax {
		Microsoft.CodeAnalysis.SyntaxNode node;
		public CSSyntaxNode(Microsoft.CodeAnalysis.SyntaxNode node) {
			this.node = node;
		}
		public override string ToString() => $"{node.Kind()}";

    }
}