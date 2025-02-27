// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using Crow.Text;

namespace CrowEditBase
{
	public abstract class SyntaxRootNode : SyntaxNode {
		public SyntaxRootNode (ReadOnlyMemory<char> source, Token[] tokens) {
			this.source = source;
			this.tokens = tokens;
		}
		protected readonly ReadOnlyMemory<char> source;
		protected Token[] tokens;
		public override int TokenIndexBase => 0;
		public override int TokenCount => tokens == null ? 0 : Math.Max (0, tokens.Length - 1);
		public override SyntaxRootNode Root => this;
		public override bool IsFoldable => false;
		public override SyntaxNode NextSiblingOrParentsNextSibling => null;
		public override void UnfoldToTheTop() {}

		public ReadOnlySpan<Token> Tokens => tokens;
		public string GetTokenStringByIndex (int idx) => tokens != null ?
			idx >= 0 && idx < tokens.Length ? GetText(tokens[idx].Span).ToString() : null : null;
		public Token GetTokenByIndex (int idx) => tokens != null ?
			idx >= 0 && idx < tokens.Length ? tokens[idx] : default : default;
		public ReadOnlySpan<char> GetText(TextSpan span) =>
			source.Span.Slice(span.Start, span.Length);
		public string GetTokenString(Token tok) =>
			GetText(tok.Span).ToString();
	}
}