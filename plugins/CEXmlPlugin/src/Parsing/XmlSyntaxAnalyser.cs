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
        /*protected override void Parse(SyntaxNode node)
        {
            throw new NotImplementedException();
        }*/
        public XmlSyntaxAnalyser (XmlDocument source) : base (source) {
			this.source = source;
		}

		/*public virtual SyntaxNode Process (SyntaxNode startingNode) {

		}*/
		public virtual void ProcessAttributeValueSyntax(AttributeSyntax attrib) {
			attrib.valueTok = tokIdx - attrib.TokenIndexBase;
		}
		public override void Process () {
			XmlDocument xmlDoc = source as XmlDocument;
			currentNode = new XMLRootSyntax (xmlDoc);
			currentLine = 0;
			tokIdx = 0;
			tokens = source.Tokens;

			while (tokIdx < tokens.Length) {
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
							tag.close = tokIdx - tag.TokenIndexBase;
							setEndLineForCurrentNode ();
							currentNode.RemoveChild (tag);
							currentNode = currentNode.AddChild (new ElementSyntax (tag));
						} else if (curTok.GetTokenType() == XmlTokenType.EmptyElementClosing) {
							setEndLineForCurrentNode ();
							currentNode.RemoveChild (tag);
							currentNode = currentNode.AddChild (new EmptyElementSyntax (tag));
							setCurrentNodeEndLine (currentLine);
							currentNode = currentNode.Parent;
						} else {
							addException ("Unexpected Token");
							setEndLineForCurrentNode (-1);
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
								addException ("Extra equal sign in attribute syntax");
							else
								attrib.equal = tokIdx - attrib.TokenIndexBase;
						else if (curTok.GetTokenType() == XmlTokenType.AttributeValueOpen)
							attrib.valueOpen = tokIdx - attrib.TokenIndexBase;
						else if (curTok.GetTokenType() == XmlTokenType.AttributeValue)
							ProcessAttributeValueSyntax (attrib);
						else if (curTok.GetTokenType() == XmlTokenType.AttributeValueClose) {
							attrib.valueClose = tokIdx - attrib.TokenIndexBase;
							setEndLineForCurrentNode ();
						} else {
							addException ("Unexpected Token");
							setEndLineForCurrentNode (-1);
							continue;
						}
					} else if (currentNode is ElementEndTagSyntax eltEndTag) {
						if (curTok.GetTokenType() == XmlTokenType.ElementName)
							eltEndTag.name = tokIdx - eltEndTag.TokenIndexBase;
						else if (curTok.GetTokenType() == XmlTokenType.ClosingSign) {
							eltEndTag.close = tokIdx - eltEndTag.TokenIndexBase;
							//go up 2 times
							setEndLineForCurrentNode (); setEndLineForCurrentNode ();
						} else {
							addException ("Unexpected Token");
							setEndLineForCurrentNode (-1);
							setEndLineForCurrentNode (-1);
							continue;
						}
					} else if (currentNode is XMLRootSyntax) {
						switch (curTok.GetTokenType()) {
							case XmlTokenType.ElementOpen:
								currentNode = currentNode.AddChild (new ElementStartTagSyntax (currentLine, tokIdx));
								break;
							case XmlTokenType.PI_Start:
								currentNode = currentNode.AddChild (new ProcessingInstructionSyntax (currentLine, tokIdx));
								break;
							default:
								addException ("Unexpected Token");
								break;
						}
					} else if (currentNode is ProcessingInstructionSyntax pi) {
						if (curTok.GetTokenType() == XmlTokenType.PI_Target)
							pi.name = tokIdx - pi.TokenIndexBase;
						else if (curTok.GetTokenType() == XmlTokenType.PI_End) {
							pi.PIClose = tokIdx - pi.TokenIndexBase;
							setEndLineForCurrentNode ();
						} else if (curTok.GetTokenType() == XmlTokenType.AttributeName) {
							AttributeSyntax attribute = new AttributeSyntax (currentLine, tokIdx);
							attribute.name = 0;
							currentNode = currentNode.AddChild (attribute);
						} else {
							addException ("Unexpected Token");
							setEndLineForCurrentNode (-1);
							continue;
						}
					}
				}
				tokIdx++;
			}
			while (currentNode.Parent != null) {
				if (!currentNode.TokenCount.HasValue)
					setEndLineForCurrentNode (-1);
				else
					currentNode = currentNode.Parent;
			}
			setCurrentNodeEndLine (currentLine);
		}
	}
}