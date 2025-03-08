// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using Crow.Text;

namespace CrowEditBase
{
	public abstract class SyntaxRootNode : MultiNodeSyntax {
		public SyntaxRootNode (ReadOnlyTextBuffer buffer, Token[] tokens) {
			this.buffer = buffer;
			this.tokens = tokens;
		}
		protected readonly ReadOnlyTextBuffer buffer;
		protected Token[] tokens;
		public override SyntaxRootNode Root => this;


		public override bool IsFoldable => false;
		public override void UnfoldToTheTop() {}
		public override SyntaxNode NextSiblingOrParentsNextSibling => null;

		public ReadOnlySpan<Token> Tokens => tokens;


		public string GetTokenStringByIndex (int idx) => tokens != null ?
			idx >= 0 && idx < tokens.Length ? GetText(tokens[idx].Span).ToString() : null : null;
		public Token GetTokenByIndex (int idx) => tokens != null ?
			idx >= 0 && idx < tokens.Length ? tokens[idx] : default : default;
		public int FindTokenIndexIncludingPosition (int pos) {
			if (pos == 0 || Tokens.Length == 0)
				return default;
			int idx = Tokens.BinarySearch(new  Token () {Start = pos});
			return idx == 0 ? 0 : idx < 0 ? ~idx - 1 : idx;
		}
		public ReadOnlySpan<char> GetText(TextSpan span) =>
			buffer.Source.Span.Slice(span.Start, span.Length);
		public CharLocation GetLocation(int pos) => buffer.Lines.GetLocation(pos);
			
		public string GetTokenString(Token tok) =>
			GetText(tok.Span).ToString();
	}
}