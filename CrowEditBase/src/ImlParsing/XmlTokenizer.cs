// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Text;
using System.Collections.Generic;
using CrowEditBase;

namespace CrowEdit.Xml
{
	public class XmlTokenizer : Tokenizer {
		enum States
		{
			Init,//first statement of prolog, xmldecl should only apear in this state
			prolog,//misc before doctypedecl
			ProcessingInstrucitons,
			DTD,
			DTDObject,//doctype finished				
			Xml,
			StartTag,//inside start tag
			Content,//after start tag with no closing slash
			EndTag
		}

		States curState;
		int startOfTok;


		public XmlTokenizer  () {}
		bool readName (ref SpanCharReader reader) {
			if (reader.EndOfSpan)
				return false;
			char c = reader.Peak;					
			if (char.IsLetter(c) || c == '_' || c == ':') {
				reader.Advance ();
				while (reader.TryPeak (ref c)) {									
					if (!(char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '\xB7'))
						return true;
					reader.Advance ();
				}
				return true;
			}
			return false;
		}
		protected List<Token> Toks;

		void skipWhiteSpaces (ref SpanCharReader reader) {
			while(!reader.EndOfSpan) {
				switch (reader.Peak) {
					case '\x85':
					case '\x2028':
					case '\xA':
						reader.Read();
						addTok (ref reader, XmlTokenType.LineBreak);
						break;
					case '\xD':
						reader.Read();
						if (reader.IsNextCharIn ('\xA', '\x85'))
							reader.Read();
						addTok (ref reader, XmlTokenType.LineBreak);														
						break;
					case '\x20':
					case '\x9':
						char c = reader.Read();									
						while (reader.TryPeak (c))
							reader.Read();
						addTok (ref reader, c == '\x20' ? XmlTokenType.WhiteSpace : XmlTokenType.Tabulation);
						break;
					default:
						return;
				}
			}
		}
		void addTok (ref SpanCharReader reader, XmlTokenType tokType) {
			if (reader.CurrentPosition == startOfTok)
				return;
			Toks.Add (new Token((TokenType)tokType, startOfTok, reader.CurrentPosition));
			startOfTok = reader.CurrentPosition;
		}
		public override Token[] Tokenize (string source) {
			SpanCharReader reader = new SpanCharReader(source);
			
			startOfTok = 0;
			int curObjectLevel = 0;
			curState = States.Init;
			Toks = new List<Token>(100);

			while(!reader.EndOfSpan) {

				skipWhiteSpaces (ref reader);

				if (reader.EndOfSpan)
					break;

				switch (reader.Peak) {				
				case '<':
					reader.Advance ();
					if (reader.TryPeak ('?')) {								
						reader.Advance ();
						addTok (ref reader, XmlTokenType.PI_Start);
						readName (ref reader);
						addTok (ref reader, XmlTokenType.PI_Target);
						curState = States.ProcessingInstrucitons;
					} else if (reader.TryPeak ('!')) {
						reader.Advance ();
						if (reader.TryPeak ("--")) {
							reader.Advance (2);
							addTok (ref reader, XmlTokenType.BlockCommentStart);										
							if (reader.TryReadUntil ("-->")) {
								addTok (ref reader, XmlTokenType.BlockComment);
								reader.Advance (3);											
								addTok (ref reader, XmlTokenType.BlockCommentEnd);
							} else if (reader.TryPeak ("-->")) {
								reader.Advance (3);											
								addTok (ref reader, XmlTokenType.BlockCommentEnd);
							}
						} else {
							addTok (ref reader, XmlTokenType.DTDObjectOpen);
							if (readName (ref reader)) {
								addTok (ref reader, XmlTokenType.Keyword);
								curState = States.DTDObject;
							}								
						}								
					} else if (reader.TryPeak('/')) {
						reader.Advance ();
						addTok (ref reader, XmlTokenType.EndElementOpen);
						if (readName (ref reader)) {
							addTok (ref reader, XmlTokenType.ElementName);
							if (reader.TryPeak('>')) {
								reader.Advance ();
								addTok (ref reader, XmlTokenType.ClosingSign);

								if (--curObjectLevel > 0)
									curState = States.Content;
								else
									curState = States.Xml;
							} 
						}
					}else{							
						addTok (ref reader, XmlTokenType.ElementOpen);							
						if (readName (ref reader)) {
							addTok (ref reader, XmlTokenType.ElementName);								
							curState = States.StartTag;
						}
					}
					break;
				case '?':
					reader.Advance ();
					if (reader.TryPeak ('>')){
						reader.Advance ();
						addTok (ref reader, XmlTokenType.PI_End);
					}else
						addTok (ref reader, XmlTokenType.Unknown);						
					curState = States.prolog;						
					break;
				case '\'':
				case '"':
					char q = reader.Read();
					addTok (ref reader, XmlTokenType.AttributeValueOpen);
					if (reader.TryReadUntil (q)) {
						addTok (ref reader, XmlTokenType.AttributeValue);
						reader.Advance ();
						addTok (ref reader, XmlTokenType.AttributeValueClose);
					} else
						addTok (ref reader, XmlTokenType.AttributeValue);
					break;
				case '=':
					reader.Advance();
					addTok (ref reader, XmlTokenType.EqualSign);
					break;
				case '>':
					reader.Advance();
					addTok (ref reader, XmlTokenType.ClosingSign);
					curObjectLevel++;
					curState = States.Content;
					break;
				case '/':
					reader.Advance();
					if (reader.TryRead ('>')) {
						addTok (ref reader, XmlTokenType.EmptyElementClosing);
						if (--curObjectLevel > 0)
							curState = States.Content;
						else
							curState = States.Xml;
					}else
						addTok (ref reader, XmlTokenType.Unknown);
					break;
				default:
					if (curState == States.StartTag || curState == States.ProcessingInstrucitons) {
						if (readName(ref reader))
							addTok (ref reader, XmlTokenType.AttributeName);
						else if (reader.TryAdvance())
							addTok (ref reader, XmlTokenType.Unknown);
					} else {
						reader.TryReadUntil ('<');
						addTok (ref reader, XmlTokenType.Content);
					}
					break;
				}
			}

			return Toks.ToArray();
		}
		
	}
}
