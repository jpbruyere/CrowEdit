// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Text;
using System.Collections.Generic;
using CrowEditBase;
using System.Globalization;
using Crow.Coding;

namespace CECrowPlugin {
	public class StyleTokenizer : Tokenizer {
		enum States	{
			classNames, members, value, endOfStatement
		}

		States curState;
		int startOfTok;


		public StyleTokenizer  () {}

		bool readName (ref SpanCharReader reader) {
			if (reader.EndOfSpan)
				return false;
			char c = reader.Peak;
			if (char.IsLetter(c) || c == '_' ) {
				reader.Advance ();
				while (reader.TryPeak (ref c)) {
					if (!char.IsLetterOrDigit(c)) {
						UnicodeCategory uc = Char.GetUnicodeCategory (c);
						if (uc != UnicodeCategory.NonSpacingMark &&
							uc != UnicodeCategory.SpacingCombiningMark &&
							uc != UnicodeCategory.ConnectorPunctuation &&
							uc != UnicodeCategory.Format)
							return true;
					}
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
						addTok (ref reader, StyleTokenType.LineBreak);
						break;
					case '\xD':
						reader.Read();
						if (reader.IsNextCharIn ('\xA', '\x85'))
							reader.Read();
						addTok (ref reader, StyleTokenType.LineBreak);
						break;
					case '\x20':
					case '\x9':
						char c = reader.Read();
						while (reader.TryPeak (c))
							reader.Read();
						addTok (ref reader, c == '\x20' ? StyleTokenType.WhiteSpace : StyleTokenType.Tabulation);
						break;
					default:
						return;
				}
			}
		}
		void addTok (ref SpanCharReader reader, Enum tokType) {
			if (reader.CurrentPosition == startOfTok)
				return;
			Toks.Add (new Token((TokenType)tokType, startOfTok, reader.CurrentPosition));
			startOfTok = reader.CurrentPosition;
		}
		public override Token[] Tokenize (string source) {
			SpanCharReader reader = new SpanCharReader(source);

			startOfTok = 0;
			int curObjectLevel = 0;
			curState = States.classNames;
			Toks = new List<Token>(100);

			while(!reader.EndOfSpan) {

				skipWhiteSpaces (ref reader);

				if (reader.EndOfSpan)
					break;

				switch (reader.Peak) {
				case '/':
					reader.Advance ();
					if (reader.TryPeak ('/')) {
						reader.Advance ();
						addTok (ref reader, StyleTokenType.LineCommentStart);
						reader.AdvanceUntilEol ();
						addTok (ref reader, StyleTokenType.LineComment);
					} else if (reader.TryPeak ('*')) {
						reader.Advance ();
						addTok (ref reader, StyleTokenType.BlockCommentStart);
						if (reader.TryReadUntil ("*/")) {
							addTok (ref reader, StyleTokenType.BlockComment);
							reader.Advance (2);
							addTok (ref reader, StyleTokenType.BlockCommentEnd);
						}
					}
					break;
				case ',':
					reader.Advance ();
					addTok (ref reader, StyleTokenType.Comma);
					curState = States.classNames;
					break;
				case '{':
					reader.Advance ();
					addTok (ref reader, StyleTokenType.OpeningBrace);
					curState = States.members;
					break;
				case '}':
					reader.Advance ();
					addTok (ref reader, StyleTokenType.ClosingBrace);
					curState = States.classNames;
					break;
				case '=':
					reader.Advance ();
					addTok (ref reader, StyleTokenType.EqualSign);
					curState = States.value;
					break;
				case '"':
					reader.Advance ();
					addTok (ref reader, StyleTokenType.MemberValueOpen);

					while (!reader.EndOfSpan) {
						if (reader.TryPeak ("${")) {
							addTok (ref reader, StyleTokenType.MemberValuePart);
							reader.Advance (2);
							addTok (ref reader, StyleTokenType.ConstantRefOpen);

							while (!reader.EndOfSpan) {
								if (reader.TryPeak ('}')) {
									addTok (ref reader, StyleTokenType.ConstantName);
									reader.Read ();
									addTok (ref reader, StyleTokenType.ClosingBrace);
									break;
								}
								reader.Advance ();
							}
							continue;
						} else if (reader.TryPeak ('\"')) {
							addTok (ref reader, StyleTokenType.MemberValuePart);
							reader.Advance ();
							addTok (ref reader, StyleTokenType.MemberValueClose);
							break;
						}
						reader.Advance ();
					}
					curState = States.endOfStatement;
					break;
				case ';':
					reader.Advance();
					addTok (ref reader, StyleTokenType.EndOfExpression);
					curState = States.members;
					break;
				default:
					if (readName (ref reader)) {
						addTok (ref reader, StyleTokenType.Name);
						break;
					}
					reader.Advance ();
					addTok (ref reader, StyleTokenType.Unknown);
					break;
				}

			}

			return Toks.ToArray();
		}
	}
}
