// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using Crow.Text;

namespace CrowEditBase
{
	public abstract class SyntaxRootNode : SyntaxNode {
		protected readonly SourceDocument source;
		public SyntaxRootNode (SourceDocument source) {
			this.source = source;
		}
		public override int TokenIndexBase => 0;
		public override int? TokenCount { get => Math.Max (0, source.Tokens.Length - 1); internal set {} }
		public override SyntaxRootNode Root => this;
		public override bool IsFoldable => false;
		public override SyntaxNode NextSiblingOrParentsNextSibling => null;
		public override void UnfoldToTheTop() {}
		public string GetTokenStringByIndex (int idx) =>
			idx >= 0 && idx < source.Tokens.Length ? Root.GetText(source.Tokens[idx].Span).ToString() : null;
		public Token GetTokenByIndex (int idx) =>
			idx >= 0 && idx < source.Tokens.Length ? source.Tokens[idx] : default;
		public ReadOnlySpan<char> GetText(TextSpan span) =>
			source.GetText(span);
		public string GetTokenString(Token tok) =>
			source.GetText(tok.Span).ToString();
	}
}