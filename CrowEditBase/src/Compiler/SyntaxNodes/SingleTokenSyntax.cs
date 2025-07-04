// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
namespace CrowEditBase
{
	public class SingleTokenSyntax : SyntaxNode {
		#region CTOR
        public SingleTokenSyntax(Token tok) {
			token = tok;
			token.syntaxNode = this;
		}
		#endregion

		public readonly Token token;

		#region SyntaxNode implementation
        public override TokenType Type => token.Type;
        public override int SpanStart => token.Start;
		public override int SpanEnd => token.End;
        public override bool IsComplete => token.Type != TokenType.Unknown && token.Length > 0;
        public override bool IsSimilar(object other)
        {
            return other is SingleTokenSyntax sts ?
				sts.Type == Type : other is TokenType tt ? Type == tt : false;

        }
        #endregion

    }
}