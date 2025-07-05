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
using System.Xml;
using System.Text;

namespace CrowEdit.Xml
{
	public class XmlDocument : SourceDocument {

		public XmlDocument (string fullPath, string editorPath) : base (fullPath, editorPath) {	}
		protected override SyntaxAnalyser CreateSyntaxAnalyser() => new XmlSyntaxAnalyser (ImmutableBufferCopy);
		public override string GetTokenTypeString (TokenType tokenType) => ((XmlTokenType)tokenType).ToString();

		protected virtual IEnumerable<Suggestion> getElementNameSuggestions(string curName, TextChange change, int finalPositionOffset = 0) => null;
		protected virtual IEnumerable<Suggestion> getAttributeNameSuggestions(string eltName, string attribName, TextChange change, int finalPositionOffset = 0) => null;
		protected virtual IEnumerable<Suggestion> getAttributeValueSuggestions(string eltName, string attribName, string attribValue, TextChange change, int finalPositionOffset = 0) => null;
		public override IList GetSuggestions (int absoluteTextPos, int currentTokenIndex, SyntaxNode CurrentNode, CharLocation loc) {
			Token tok = GetTokenByIndex(currentTokenIndex);
			Token nextTok = currentTokenIndex < Tokens.Length - 1 ? GetTokenByIndex(currentTokenIndex + 1) : null; 
			//Console.WriteLine($"absPos:{absoluteTextPos} {tok.GetTokenType()} tokIdx:{currentTokenIndex} node:{CurrentNode} parent:{CurrentNode?.Parent} charLoc:{loc}");
			XmlTokenType tokType = tok.GetTokenType();
			if (CurrentNode is SingleTokenSyntax sts) {
				XmlTokenType tk = sts.token.GetTokenType();

				if (CurrentNode.Parent is ElementTagSyntax ets) {
					if (ets is ElementEndTagSyntax eets) {
						if (eets.Parent is ElementSyntax es) {
							string name = es.StartTag?.Name;
							string eltName = sts.AsText();
							if (!string.IsNullOrEmpty(name) && !string.Equals(name, eltName)){
								Suggestion sug = new Suggestion(name);
								if (tk == XmlTokenType.ElementName && name.StartsWith(eltName, StringComparison.OrdinalIgnoreCase))
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
					} else {//startTag or empty element
						StringBuilder txtEnd = new StringBuilder(10);
						int offset = 0;
						if (nextTok?.GetTokenType() != XmlTokenType.WhiteSpace)
							txtEnd.Append(" ");
						if (!ets.HasClosingToken) {
							txtEnd.Append(">");
							offset = -1;
						}
						List<Suggestion> sugs;
						if (tk == XmlTokenType.ElementOpen) {
							sugs = getElementNameSuggestions("", new TextChange(tok.End, 0, txtEnd.ToString()), offset).ToList();
						}else if (tk == XmlTokenType.ElementName) {
							sugs = getElementNameSuggestions(sts.AsText(), new TextChange(tok.Start, tok.Length, txtEnd.ToString()), offset).ToList();
						} else {
							return null;
						}
						if (sugs.Count == 1 && sugs[0].Caption == sts.AsText())
							return null;
						return sugs;
					}
				} else if (CurrentNode.Parent is AttributeSyntax atts) {
					
					if (tokType == XmlTokenType.WhiteSpace) {
						Console.WriteLine($"*** {tk}");
						//return getAttributeNameSuggestions(atts.Name, "", new TextChange(tok.End, 0)).ToList();
					} else {
						if (atts.Parent is ElementTagSyntax et) {
							if (tokType == XmlTokenType.AttributeName) {
								string txtEnd = sts.NextSiblingIs(XmlTokenType.EqualSign) ? "" : "=\"\"";
								List<Suggestion> sugs = getAttributeNameSuggestions(et.Name, atts.Name, new TextChange(tok.Start, tok.Length, txtEnd), txtEnd.Length > 0 ? -1 : 0).ToList();
								if (sugs.Count == 0 || (sugs.Count == 1 && sugs[0].Caption == sts.AsText()))
									return null;

								return sugs;
							} else if (atts.HasName) {
								if(tokType == XmlTokenType.AttributeValueOpen) {
									return getAttributeValueSuggestions(et.Name, atts.Name, "", new TextChange(tok.End, 0))?.ToList();
								} else if (tokType == XmlTokenType.AttributeValue) {
									return getAttributeValueSuggestions(et.Name, atts.Name, CurrentNode.AsText(), new TextChange(tok.Start, tok.Length))?.ToList();
								}
							}
						} else {
							System.Diagnostics.Debugger.Break();
						}
					}
				}
			} else if (CurrentNode is ElementTagSyntax ts) {
				string txtEnd = nextTok?.GetTokenType() == XmlTokenType.EqualSign ? "" : "=\"\"";
				return getAttributeNameSuggestions(ts.Name, "", new TextChange(tok.End, 0, txtEnd), txtEnd.Length > 0 ? -1 : 0).ToList();
			}
			return null;
		}

		public override Color GetColorForToken(Token token)
		{
			TokenType tokType = token.Type;
			XmlTokenType xmlTokType = (XmlTokenType)tokType;
			if (xmlTokType == XmlTokenType.ElementName)
				return Colors.Green;
			if (xmlTokType == XmlTokenType.AttributeName)
				return Colors.Blue;
			if (xmlTokType == XmlTokenType.AttributeValue)
				return Colors.OrangeRed;
			if (xmlTokType == XmlTokenType.EqualSign)
				return Colors.Black;
			if (xmlTokType == XmlTokenType.PI_Target)
				return Colors.DarkSlateBlue;
			return base.GetColorForToken(token);

		}
	}
}