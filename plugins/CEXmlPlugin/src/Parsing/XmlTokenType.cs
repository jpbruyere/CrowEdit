// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace CrowEdit.Xml
{
	[Flags]
	public enum XmlTokenType {
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
		ElementName				= 0x8201,
		AttributeName			= 0x8202,
		PI_Target				= 0x8203,
		Punctuation				= 0x0400,

		PI_Start				= 0x8401,// '<?'
		PI_End					= 0x8402,// '?>'
		ElementOpen 			= 0x8403,// '<'
		EndElementOpen			= 0x8404,// '</'
		EmptyElementClosing		= 0x8405,// '/>'
		ClosingSign				= 0x8406,// '>'
		DTDObjectOpen			= 0x84A0,// '<!'
		Operator 				= 0x0800,
		EqualSign 				= 0x0801,
		Keyword 				= 0x1000,
		AttributeValue			= 0x8000,
		AttributeValueOpen		= 0x8401,
		AttributeValueClose		= 0x8402,


		Content,
	}
}