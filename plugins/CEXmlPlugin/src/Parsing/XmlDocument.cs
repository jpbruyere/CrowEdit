// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Linq;
using Crow.Text;
using System.Collections.Generic;
using System.Diagnostics;
using Crow;
using IML = Crow.IML;
using System.Collections;
using System.Reflection;
using CrowEditBase;
using static CrowEditBase.CrowEditBase;

namespace CrowEdit.Xml
{
	public static class Extensions {
		public static XmlTokenType GetTokenType (this Token tok) {
			return (XmlTokenType)tok.Type;
		}
		public static void SetTokenType (this Token tok, XmlTokenType type) {
			tok.Type = (TokenType)type;
		}
	}
	public class XmlDocument : SourceDocument {

		public XmlDocument (string fullPath, string editorPath) : base (fullPath, editorPath) {

		}
		protected override Tokenizer CreateTokenizer() => new XmlTokenizer ();
		protected override SyntaxAnalyser CreateSyntaxAnalyser() => new XmlSyntaxAnalyser (this);

		public override IList GetSuggestions (int pos) {
			currentToken = FindTokenIncludingPosition (pos);
			currentNode = FindNodeIncludingPosition (pos);
			return null;
		}
		public override TextChange? GetCompletionForCurrentToken (object suggestion, out TextSpan? newSelection) {
			newSelection = null;
			return null;
		}

		public override Color GetColorForToken(TokenType tokType)
		{
			XmlTokenType xmlTokType = (XmlTokenType)tokType;
			if (xmlTokType.HasFlag (XmlTokenType.Punctuation))
				return Colors.DarkGrey;
			if (xmlTokType.HasFlag (XmlTokenType.Trivia))
				return Colors.DimGrey;
			else if (xmlTokType == XmlTokenType.ElementName)
				return Colors.Green;
			if (xmlTokType == XmlTokenType.AttributeName)
				return Colors.Blue;
			if (xmlTokType == XmlTokenType.AttributeValue)
				return Colors.OrangeRed;
			if (xmlTokType == XmlTokenType.EqualSign)
				return Colors.Black;
			if (xmlTokType == XmlTokenType.PI_Target)
				return Colors.DarkSlateBlue;
			return Colors.Red;

		}
	}
}