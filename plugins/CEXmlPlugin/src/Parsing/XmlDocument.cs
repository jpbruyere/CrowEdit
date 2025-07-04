// Copyright (c) 2013-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
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
using System.Diagnostics;

namespace CrowEdit.Xml
{
	public class XmlDocument : SourceDocument {

		public XmlDocument (string fullPath, string editorPath) : base (fullPath, editorPath) {	}
		protected override SyntaxAnalyser CreateSyntaxAnalyser() => new XmlSyntaxAnalyser (ImmutableBufferCopy);
		public override string GetTokenTypeString (TokenType tokenType) => ((XmlTokenType)tokenType).ToString();

		protected virtual IEnumerable<Suggestion> getElementNameSuggestions(string curName, TextChange change) => null;
		protected virtual IEnumerable<Suggestion> getAttributeNameSuggestions(string eltName, string attribName, TextChange change) => null;
		protected virtual IEnumerable<Suggestion> getAttributeValueSuggestions(string eltName, string attribName, string attribValue, TextChange change) => null;
		public override IList GetSuggestions (int absoluteTextPos, int currentTokenIndex, SyntaxNode CurrentNode, CharLocation loc) {
			Console.WriteLine($"absPos:{absoluteTextPos} tokIdx:{currentTokenIndex} node:{CurrentNode} charLoc:{loc}");
			Token tok = GetTokenByIndex(currentTokenIndex);
			XmlTokenType tokType = tok.GetTokenType();
			if (CurrentNode is SingleTokenSyntax sts) {
				XmlTokenType tk = sts.token.GetTokenType();
				if (CurrentNode.Parent is ElementTagSyntax ets) {
					if (ets is ElementEndTagSyntax eets) {
						if (eets.Parent is ElementSyntax es) {
							string name = es.StartTag?.Name;
							if (!string.IsNullOrEmpty(name)){
								Suggestion sug = new Suggestion(name);
								if (tk == XmlTokenType.ElementName && name.StartsWith(sts.AsText(), StringComparison.OrdinalIgnoreCase))
									sug.Change = new TextChange (tok.Start, tok.Length, sug.Caption);
								else if (tk == XmlTokenType.EndElementOpen)
									sug.Change = new TextChange (tok.End, 0, sug.Caption);
								else
									return null;
								if (!eets.HasClosingToken)
									sug.Change.ChangedText += ">";
								return new List<Suggestion>([sug]);
							} 
						}
					} else {
						string txtEnd = ets.HasClosingToken ? "" : ">";
						if (tk == XmlTokenType.ElementOpen) {
							return getElementNameSuggestions("", new TextChange(tok.End, 0, txtEnd)).ToList();
						}
						if (tk == XmlTokenType.ElementName) {
							return getElementNameSuggestions(sts.AsText(), new TextChange(tok.Start, tok.Length, txtEnd)).ToList();
						}
					}
				} 
			}
			/*	
			if (tok.Start != absoluteTextPos //middle of edited tok
				&& currentTokenIndex >= CurrentNode?.Root.TokenCount - 1) //occurs when curTok is last tok of text
			{
				return null;
			}
			Token prevTok = GetTokenByIndex(currentTokenIndex-1);

			if (CurrentNode is ElementEndTagSyntax eltEndTag) {
				//sug = corresponding eltName
				if (!tok.Is(XmlTokenType.ElementName) &&
						eltEndTag.Parent is ElementSyntax elt &&
						elt.StartTag?.Name != null) {

					Suggestion sug = new Suggestion(elt.StartTag.Name);
					string curEndName = null;
					if (prevTok.Is(XmlTokenType.ElementName) && !elt.StartTag.Name.Equals(curEndName, StringComparison.Ordinal)) {
						curEndName = root.GetTokenString(prevTok);
						sug.Change = new TextChange (prevTok.Start, prevTok.Length, sug.Caption);
					} else if (prevTok.Is(XmlTokenType.EndElementOpen)) {
						curEndName = "";
						sug.Change = new TextChange (prevTok.End, 0, sug.Caption);
					} else 
						return null;

					//if (!tok.Is(XmlTokenType.ClosingSign))
					if (!eltEndTag.close.HasValue)
						sug.Change.ChangedText += ">";

					if (elt.StartTag.Name.StartsWith (curEndName, StringComparison.OrdinalIgnoreCase))
						return new List<Suggestion> ([sug]);
				}
			} else if (CurrentNode is ElementStartTagSyntax eltStartTag) {
				if (!tok.Is(XmlTokenType.ElementName)) {
					TextChange change = default;
					
					if (prevTok.Is(XmlTokenType.ElementName))
						change = new TextChange (prevTok.Start, prevTok.Length);
					else if (prevTok.Is(XmlTokenType.ElementOpen))
						change = new TextChange (prevTok.End, 0);
					else if (eltStartTag.name.HasValue) {
						string attribName = "";
						if (prevTok.Type.HasFlag(TokenType.Trivia) ||
							(tok.Type.HasFlag(TokenType.Trivia) && GetTokenByIndex(currentTokenIndex+1).Type.HasFlag(TokenType.Trivia)))//attribute
							change = new TextChange(tok.Start, 0);
						else if (prevTok.Is(XmlTokenType.AttributeName)) {
							change = new TextChange(prevTok.Start, prevTok.Length);
							attribName = prevTok.AsString(source);
						} else
							return null;
						if (!tok.Is(XmlTokenType.EqualSign))
							change.ChangedText += "=\"\"";
						return getAttributeNameSuggestions(eltStartTag.Name, attribName, change).ToList();
					} else
						return null;

					//if (!tok.Is(XmlTokenType.ClosingSign))
					if (!eltStartTag.close.HasValue)
						change.ChangedText = ">";
					
					return getElementNameSuggestions(eltStartTag.Name, change).ToList();

				}
			} else if (CurrentNode is AttributeSyntax attrib &&
						attrib.Parent is ElementStartTagSyntax eltStart &&
						eltStart.name.HasValue) {

				if (prevTok.Is(XmlTokenType.AttributeName)) {
					TextChange change = new TextChange(prevTok.Start, prevTok.Length);
					if (!tok.Is(XmlTokenType.EqualSign))
						change.ChangedText += "=\"\"";
					return getAttributeNameSuggestions(eltStart.Name, attrib.Name, change).ToList();					
				} else if (attrib.name.HasValue) {
					if (prevTok.Is(XmlTokenType.AttributeValueOpen)) {
						return getAttributeValueSuggestions(eltStart.Name, attrib.Name, "",
							tok.Is(XmlTokenType.AttributeValueClose) ?
								new TextChange(prevTok.End, 0)
								: new TextChange(prevTok.End, 0, "\""))?.ToList();
					} else if (prevTok.Is(XmlTokenType.AttributeValue) && attrib.valueTok.HasValue) {
						return getAttributeValueSuggestions(eltStart.Name, attrib.Name, attrib.Value,
							tok.Is(XmlTokenType.AttributeValueClose) ?
								new TextChange(prevTok.Start, prevTok.Length)
								: new TextChange(tok.Start, tok.Length, "\""))?.ToList();
					}
					
				}

				*if (tokType == XmlTokenType.AttributeName) {
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
				}*
			}*/

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

		public override Color GetColorForToken(Token token)
		{
			TokenType tokType = token.Type;
			XmlTokenType xmlTokType = (XmlTokenType)tokType;
			if (xmlTokType.HasFlag (XmlTokenType.Punctuation))
				return Colors.DarkGrey;
			if (tokType.HasFlag (TokenType.WhiteSpace))
				return Colors.Silver;			
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