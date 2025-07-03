// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace CrowEdit.Ebnf
{
	[Flags]
	public enum EbnfTokenType {
		Unknown,
		Trivia					= 0x0100,
		WhiteSpace				= 0x4100,
		Tabulation				= 0x4101,
		LineBreak				= 0x4102,
		EndOfFile				= 0x4103,
		LineComment				= 0x0103,
		BlockCommentStart		= 0x0104,
		BlockCommentPart		= 0x0105,
		BlockCommentEnd			= 0x0106,

		Name					= 0x0200,
		SymbolName				= 0x0201,

		Punctuation				= 0x0400,
		OpenBrace				= 0x0401,// '('
		ClosingBrace			= 0x0402,// ')'
		OpenBracket 			= 0x0403,// '['
		ClosingBracket			= 0x0404,// ']'
		DoubleQuote				= 0x0405,
		SingleQuote				= 0x0405,

		StringLiteral			= 0x0406,
		CharMatchNegation		= 0x0407,// '^'
		CharMatchRangeOperator	= 0x0C01,// '-'
		//CharMatch				= 0x0C01,// '-'

		Operator 				= 0x0800,
		DefiningSymbol	 		= 0x0801,
		ChoiceOp		 		= 0x0802,
		ExclusionOp		 		= 0x0803,
		SequenceOp		 		= 0x0804, //never parsed (it's only a white space) but use in syntaxAnalyser
		CardinalityOp	 		= 0x0805,

		Match					= 0x2000,
		StringMatch				= 0x2001,
		CharMatch				= 0x2002,
		CodePointMatch			= 0x2003,
		TokenSectionStart,
	}
}