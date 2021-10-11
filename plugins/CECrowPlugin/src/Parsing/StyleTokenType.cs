// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace CECrowPlugin
{
	[Flags]
	public enum StyleTokenType {
		Unknown,
		Trivia					= 0x0100,
		WhiteSpace				= 0x4100,
		Tabulation				= 0x4101,
		LineBreak				= 0x4102,
		LineCommentStart		= 0x0102,
		LineComment				= 0x0103,
		BlockCommentStart		= 0x0104,
		BlockComment			= 0x0105,
		BlockCommentEnd			= 0x0106,
		Name					= 0x0200,
		StyleKey				= 0x0201,//may be a class name or a style name.
		MemberName				= 0x0202,
		ConstantName			= 0x0203,
		Punctuation				= 0x0400,
		OpeningBrace 			= 0x0401,// '{'
		ClosingBrace			= 0x0402,// '}'
		Comma		 			= 0x0403,// ','
		EndOfExpression			= 0x0404,// ';'
		EqualSign 				= 0x0801,
		MemberValuePart				= 0x2000,
		MemberValueOpen			= 0x2401,
		MemberValueClose		= 0x2402,
		ConstantRefOpen			= 0x2403,// '${'
	}
}