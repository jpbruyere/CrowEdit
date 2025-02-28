// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using Crow.Text;
using System.Collections;
using CrowEditBase;
using Drawing2D;
using System.Collections.Generic;
using System;
using Crow;
using System.Linq;

namespace CrowEdit.Xml
{
	public static class Extensions {
		public static XmlTokenType GetTokenType (this Token tok) {
			return (XmlTokenType)tok.Type;
		}
		public static void SetTokenType (this Token tok, XmlTokenType type) {
			tok.Type = (TokenType)type;
		}
		public static bool Is(this Token tok, XmlTokenType type) => (XmlTokenType)tok.Type == type; 
	}
	public class XmlDocument : SourceDocument {

		public XmlDocument (string fullPath, string editorPath) : base (fullPath, editorPath) {	}
		protected override SyntaxAnalyser CreateSyntaxAnalyser() => new XmlSyntaxAnalyser (this);
		public override string GetTokenTypeString (TokenType tokenType) => ((XmlTokenType)tokenType).ToString();

		protected virtual IEnumerable<Suggestion> getElementNameSuggestions(string curName, TextChange change) {
			
			return null;
		}
		public override IList GetSuggestions (int absoluteTextPos, int currentTokenIndex, SyntaxNode CurrentNode, CharLocation loc) {
			Token tok = GetTokenByIndex(currentTokenIndex);
			Token prevTok = GetTokenByIndex(currentTokenIndex-1);

			if (CurrentNode is ElementEndTagSyntax eltEndTag) {
				if (!prevTok.Is(XmlTokenType.ClosingSign) &&
						eltEndTag.Parent is ElementSyntax elt &&
						elt.StartTag?.Name != null) {

					Suggestion sug = new Suggestion(elt.StartTag.Name);
					string curEndName = null;
					

					if (tok.Is(XmlTokenType.ElementName)) {
						curEndName = root.GetTokenString(tok);
						sug.Change = new TextChange (tok.Start, tok.Length, sug.Caption);
					} else if (prevTok.Is(XmlTokenType.ElementName)) {
						curEndName = root.GetTokenString(prevTok);
						sug.Change = new TextChange (prevTok.Start, prevTok.Length, sug.Caption);
					} else if (prevTok.Is(XmlTokenType.EndElementOpen)) {
						curEndName = "";
						sug.Change = new TextChange (prevTok.End, 0, sug.Caption);
					}

					if (!(tok.Is(XmlTokenType.ClosingSign) || sug.Change.IsEmpty ||
						GetTokenByIndex(currentTokenIndex+1).Is(XmlTokenType.ClosingSign)))
						sug.Change.ChangedText += ">";

					if (curEndName != null && elt.StartTag.Name.StartsWith (
												curEndName, StringComparison.OrdinalIgnoreCase)
											&& !elt.StartTag.Name.Equals(curEndName, StringComparison.Ordinal))
						return new List<Suggestion> ([sug]);
				}
			} else if (CurrentNode is ElementStartTagSyntax eltStartTag) {
				TextChange change = default;
				if (tok.Is(XmlTokenType.ElementName))
					change = new TextChange (tok.Start, tok.Length);
				else if (prevTok.Is(XmlTokenType.ElementName))
					change = new TextChange (prevTok.Start, prevTok.Length);
				else if (tok.Is(XmlTokenType.ElementOpen))
					change = new TextChange (tok.End, 0);
				else if (prevTok.Is(XmlTokenType.ElementOpen))
					change = new TextChange (prevTok.End, 0);
				else
					return null;

				if (!(tok.Is(XmlTokenType.ClosingSign) ||
						GetTokenByIndex(currentTokenIndex+1).Is(XmlTokenType.ClosingSign)))
						change.ChangedText = ">";
				
				return getElementNameSuggestions(eltStartTag.Name, change).ToList();
			}

			return null;
		}
		/*
		public override bool TryCompleteToken (Suggestion suggestion) {
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


			if (tok.GetTokenType() == XmlTokenType.ElementOpen ||
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
			return true;
		}*/

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