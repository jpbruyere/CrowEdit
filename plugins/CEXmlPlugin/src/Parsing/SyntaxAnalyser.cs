// Copyright (c) 2021-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using CrowEditBase;

namespace CrowEdit.Xml
{
	public class XmlSyntaxAnalyser : SyntaxAnalyser {
		public override SyntaxNode Root => currentNode;
		public XmlSyntaxAnalyser (XmlDocument source) : base (source) {
			this.source = source;
		}

		/*public virtual SyntaxNode Process (SyntaxNode startingNode) {

		}*/

		public override void Process () {
			XmlDocument xmlDoc = source as XmlDocument;
			Exceptions = new List<SyntaxException> ();
			currentNode = new IMLRootSyntax (xmlDoc);
			currentLine = 0;
			Span<Token> toks = source.Tokens;
			tokIdx = 0;

			while (tokIdx < toks.Length) {
				Token curTok = toks[tokIdx];
				if (curTok.Type == TokenType.LineBreak)
					currentLine++;
				else if (!curTok.Type.HasFlag (TokenType.Trivia)) {
					if (currentNode is ElementStartTagSyntax tag) {
						if (curTok.GetTokenType() == XmlTokenType.AttributeName) {
							AttributeSyntax attribute = new AttributeSyntax (currentLine, tokIdx);
							attribute.name = 0;
							currentNode = currentNode.AddChild (attribute);
						} else if (curTok.GetTokenType() == XmlTokenType.ElementName)
							tag.name = tokIdx - tag.TokenIndexBase;
						else if (curTok.GetTokenType() == XmlTokenType.ClosingSign) {
							storeCurrentNode ();
							currentNode.RemoveChild (tag);
							currentNode = currentNode.AddChild (new ElementSyntax (tag));
						} else if (curTok.GetTokenType() == XmlTokenType.EmptyElementClosing) {
							storeCurrentNode ();
							currentNode.RemoveChild (tag);
							currentNode = currentNode.AddChild (new EmptyElementSyntax (tag));
							setCurrentNodeEndLine (currentLine);
							currentNode = currentNode.Parent;
						} else {
							Exceptions.Add (new SyntaxException  ("Unexpected Token", curTok));
							storeCurrentNode (-1);
							continue;
						}
					} else if (currentNode is ElementSyntax elt) {
						if (curTok.GetTokenType() == XmlTokenType.ElementOpen)
							currentNode = currentNode.AddChild (new ElementStartTagSyntax (currentLine, tokIdx));
						else if (curTok.GetTokenType() == XmlTokenType.EndElementOpen) {
							elt.EndTag = new ElementEndTagSyntax (currentLine, tokIdx);
							currentNode = elt.AddChild (elt.EndTag);
						}
					} else if (currentNode is AttributeSyntax attrib) {
						if (curTok.GetTokenType() == XmlTokenType.EqualSign)
							if (attrib.equal.HasValue)
								Exceptions.Add (new SyntaxException  ("Extra equal sign in attribute syntax", curTok));
							else
								attrib.equal = tokIdx - attrib.TokenIndexBase;
						else if (curTok.GetTokenType() == XmlTokenType.AttributeValueOpen)
							attrib.valueOpen = tokIdx - attrib.TokenIndexBase;
						else if (curTok.GetTokenType() == XmlTokenType.AttributeValue)
							attrib.valueTok = tokIdx - attrib.TokenIndexBase;
						else if (curTok.GetTokenType() == XmlTokenType.AttributeValueClose) {
							attrib.valueClose = tokIdx - attrib.TokenIndexBase;
							storeCurrentNode ();
						} else {
							Exceptions.Add (new SyntaxException  ("Unexpected Token", curTok));
							storeCurrentNode (-1);
							continue;
						}
					} else if (currentNode is ElementEndTagSyntax eltEndTag) {
						if (curTok.GetTokenType() == XmlTokenType.ElementName)
							eltEndTag.name = tokIdx - eltEndTag.TokenIndexBase;
						else if (curTok.GetTokenType() == XmlTokenType.ClosingSign) {
							//go up 2 times
							storeCurrentNode (); storeCurrentNode ();
						} else {
							Exceptions.Add (new SyntaxException  ("Unexpected Token", curTok));
							storeCurrentNode (-1);
							storeCurrentNode (-1);
							continue;
						}
					} else if (currentNode is IMLRootSyntax) {
						switch (curTok.GetTokenType()) {
							case XmlTokenType.ElementOpen:
								currentNode = currentNode.AddChild (new ElementStartTagSyntax (currentLine, tokIdx));
								break;
							case XmlTokenType.PI_Start:
								currentNode = currentNode.AddChild (new ProcessingInstructionSyntax (currentLine, tokIdx));
								break;
							default:
								Exceptions.Add (new SyntaxException  ("Unexpected Token", curTok));
								break;
						}
					} else if (currentNode is ProcessingInstructionSyntax pi) {
						if (curTok.GetTokenType() == XmlTokenType.PI_Target)
							pi.name = tokIdx - pi.TokenIndexBase;
						else if (curTok.GetTokenType() == XmlTokenType.PI_End) {
							storeCurrentNode ();
						} else if (curTok.GetTokenType() == XmlTokenType.AttributeName) {
							AttributeSyntax attribute = new AttributeSyntax (currentLine, tokIdx);
							attribute.name = 0;
							currentNode = currentNode.AddChild (attribute);
						} else {
							Exceptions.Add (new SyntaxException  ("Unexpected Token", curTok));
							storeCurrentNode (-1);
							continue;
						}
					}
				}
				tokIdx++;
			}
			while (currentNode.Parent != null) {
				if (!currentNode.LastTokenOffset.HasValue)
					storeCurrentNode (-1);
				else
					currentNode = currentNode.Parent;
			}
			setCurrentNodeEndLine (currentLine);
		}
	}
}