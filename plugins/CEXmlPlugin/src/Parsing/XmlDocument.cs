// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using Crow.Text;
using System.Collections;
using CrowEditBase;
using Drawing2D;
using System.Collections.Generic;
using System;

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

		public XmlDocument (string fullPath, string editorPath) : base (fullPath, editorPath) {	}
		protected override SyntaxAnalyser CreateSyntaxAnalyser() => new XmlSyntaxAnalyser (this);
		public override string GetTokenTypeString (TokenType tokenType) => ((XmlTokenType)tokenType).ToString();
		public override IList GetSuggestions (Token currentToken, SyntaxNode CurrentNode, CharLocation loc) {
			/*currentToken = FindTokenIncludingPosition (pos);
			currentNode = FindNodeIncludingPosition (pos);*/
			if (currentToken.GetTokenType() == XmlTokenType.EndElementOpen &&
				CurrentNode is ElementEndTagSyntax eltEndTag && !eltEndTag.IsComplete) {
				ElementSyntax es = eltEndTag.Parent as ElementSyntax;
				if (es?.StartTag.name != null)
					return new List<string> (new string[] {es.StartTag.Name});
			}			
			return null;
		}
		public override bool TryCompleteToken (Token tok, SyntaxNode node, object suggestion, out TextChange change, out TextSpan? newSelection) {
			newSelection = null;
			change = default;

			string selectedSugg = suggestion?.ToString ();

			if (selectedSugg == null)
				return false;

			XmlTokenType tokType = tok.GetTokenType();

			if (tokType.HasFlag(XmlTokenType.WhiteSpace)) {
				
				if (typeof(ElementTagSyntax).IsAssignableFrom(node?.GetType())) {
					ElementTagSyntax ets = node as ElementTagSyntax;
					if (ets.name.HasValue) {
						change = new TextChange (tok.End, 0, selectedSugg + "=\"\"");
						newSelection = TextSpan.FromStartAndLength(change.End2 - 1);
					} else
						change = new TextChange (tok.End, 0, selectedSugg + " ");
				} else {
					change = new TextChange (tok.End, 0, selectedSugg);
				}
			} else if (tokType == XmlTokenType.EndElementOpen) {
				change = new TextChange (tok.End, 0, selectedSugg + ">");
			} else if (tokType == XmlTokenType.ElementName) {
				if (node is ElementEndTagSyntax)
					change = new TextChange (tok.Start, tok.Length, selectedSugg + ">");
				else {
					change = new TextChange (tok.Start, tok.Length, selectedSugg + ">");
					newSelection = TextSpan.FromStartAndLength (change.End2 - 1);
				}
			} else if (node is AttributeSyntax attrib) {
				if (tokType == XmlTokenType.AttributeName) {
					if (attrib.ValueToken.HasValue) {
						change = new TextChange (tok.Start, tok.Length, selectedSugg);
						newSelection = new TextSpan(
							attrib.ValueToken.Value.Start + change.CharDiff + 1,
							attrib.ValueToken.Value.End + change.CharDiff - 1
						);
					} else {
						change = new TextChange (tok.Start, tok.Length, selectedSugg + "=\"\"");
						newSelection = TextSpan.FromStartAndLength (tok.Start + selectedSugg.Length + 2);
					}
				} else {
					int offset = 1;
					if (!attrib.valueClose.HasValue) {
						selectedSugg += root.GetTokenStringByIndex(attrib.valueClose.Value);
						offset = 0;
					}
					if (tokType == XmlTokenType.AttributeValueOpen)
						change = new TextChange (tok.End, 0, selectedSugg);
					else if (tokType == XmlTokenType.AttributeValue)
						change = new TextChange (tok.Start, tok.Length, selectedSugg);
					newSelection = TextSpan.FromStartAndLength (change.End2 + offset);
				}
			} else if (tokType == XmlTokenType.ElementOpen) {
				change = new TextChange (tok.End, 0, selectedSugg + " ");
			} else
				change =  new TextChange (tok.Start, tok.Length, selectedSugg);

			return true;
/***********************************************/

			/*if (tok.GetTokenType() == XmlTokenType.ElementOpen ||
				tok.GetTokenType() == XmlTokenType.WhiteSpace ||
				tok.GetTokenType() == XmlTokenType.AttributeValueOpen) {
				change = new TextChange (tok.End, 0, selectedSugg);
				return true;
			}
			if (tok.GetTokenType() == XmlTokenType.AttributeName && CurrentNode is AttributeSyntax attrib) {
				if (attrib.ValueToken.HasValue) {
					change = new TextChange (tok.Start, tok.Length, selectedSugg);
					newSelection = new TextSpan(
						attrib.ValueToken.Value.Start + change.CharDiff + 1,
						attrib.ValueToken.Value.End + change.CharDiff - 1
					);
				} else {
					change = new TextChange (tok.Start, tok.Length, selectedSugg + "=\"\"");
					newSelection = TextSpan.FromStartAndLength (tok.Start + selectedSugg.Length + 2);
				}
				return true;
			}

			change = new TextChange (tok.Start, tok.Length, selectedSugg);
			return true;*/
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
		//protected bool previousTokHasFlag(XmlTokenType flag) => previousToken.HasValue && previousToken.Value.Type.HasFlag(flag);
	}
}