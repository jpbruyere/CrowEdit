// Copyright (c) 2013-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace CERoslynPlugin
{
	[Flags]
	public enum CSTokenType {
		Unknown,

		Trivia					= 0x0100,
		WhiteSpace				= 0x4100,
		Tabulation				= 0x4101,
		LineBreak				= 0x4102,
		LineComment				= 0x0103,
		BlockCommentStart		= 0x0104,
		BlockComment			= 0x0105,
		BlockCommentEnd			= 0x0106,
		Name					= 0x0200,
		Punctuation				= 0x0400,
		OpenBrace				= 0x0401,
		CloseBrace				= 0x0402,
		OpenBracket				= 0x0403,
		CloseBracket			= 0x0404,
		OpenParen				= 0x0405,
		CloseParenToken			= 0x0406,
		DoubleQuote				= 0x0407,
		SingleQuote				= 0x0408,
		
		Operator 				= 0x0800,
		Keyword 				= 0x1000,
		TypeKeyword				= 0x1001,
		TryKeyword				= 0x1002,
		VisibilityKeyword		= 0x1003,
		NewKeyword				= 0x1004,
		Region					= 0x1005,
		Directive				= 0x1006,

	}
}