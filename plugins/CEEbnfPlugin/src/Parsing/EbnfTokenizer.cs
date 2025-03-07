// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Text;
using System.Collections.Generic;
using CrowEditBase;

namespace CrowEdit.Ebnf
{
	public class EbnfTokenizer : Tokenizer {
		public EbnfTokenizer  () {}
		bool readName (ref SpanCharReader reader) {
			if (reader.EndOfSpan)
				return false;
			char c = reader.Peek;
			if (char.IsLetter(c) || c == '_') {
				reader.Advance ();
				while (reader.TryPeek (ref c)) {
					if (!(char.IsLetterOrDigit(c)|| c == '_'))
						return true;
					reader.Advance ();
				}
				return true;
			}
			return false;
		}
		//first '#' character must have been read
		void readUcsCodePoint (ref SpanCharReader reader) {
			if (reader.TryRead ('x')) {
				if (readHexNumber (ref reader)) {
					addTok (ref reader, EbnfTokenType.CodePointMatch);
					return;
				}
			}
			throw new EbnfParserException ("malform ucs character codepoint, expecting '#xN'.");
		}
		bool readHexNumber (ref SpanCharReader reader) {
			if (reader.EndOfSpan)
				return false;
			if (IsValidHexDigit (reader.Peek)) {
				reader.Advance ();
				while (IsValidHexDigit (reader.Peek))
					reader.Advance ();
				return true;
			}
			return false;
		}

		public static bool IsValidHexDigit (char c) =>
			char.IsDigit (c) || (c > 64 && c < 71) || (c > 96 && c < 103);

		public override Token[] Tokenize (ReadOnlySpan<char> source) {
			SpanCharReader reader = new SpanCharReader(source);

			startOfTok = 0;
			Toks = new List<Token>(100);

			while(!reader.EndOfSpan) {

				skipWhiteSpaces (ref reader);

				if (reader.EndOfSpan)
					break;

				switch (reader.Peek) {
				case '/':
					reader.Advance ();
					if (reader.TryPeek ('*')) {
						reader.Advance ();
						addTok (ref reader, EbnfTokenType.BlockCommentStart);
						while (!reader.EndOfSpan) {
							if (reader.Eol()) {
								addTok (ref reader, EbnfTokenType.BlockComment);
								reader.ReadEol();
								addTok (ref reader, EbnfTokenType.LineBreak);
								continue;
							}
							if (reader.TryPeek ("*/")) {
								addTok (ref reader, EbnfTokenType.BlockComment);
								reader.Advance (2);
								addTok (ref reader, EbnfTokenType.BlockCommentEnd);
								break;
							} else
								reader.Read ();
						}
						break;					
					}
					addTok (ref reader, EbnfTokenType.Unknown);
					break;
				case '"':
				case '\'':
					char q = reader.Read();
					addTok (ref reader, EbnfTokenType.StringDelimiter);
					while (!reader.EndOfSpan) {
						if (reader.Eol()) {
							addTok (ref reader, EbnfTokenType.StringLiteral);
							break;
						} else if (reader.Peek == q) {
							addTok (ref reader, EbnfTokenType.StringLiteral);
							reader.Advance ();
							addTok (ref reader, EbnfTokenType.StringDelimiter);
							break;
						}
						reader.Advance();
					}
					break;
				case ':':
					reader.Advance();
					if (!reader.TryRead (":="))
						throw new EbnfParserException ("malform symbol declaration, expecting '::='.");
					addTok (ref reader, EbnfTokenType.SymbolAffectation);
					break;
				case '<':
					reader.Advance();
					if (!reader.TryRead ("?TOKENS?>"))
						throw new EbnfParserException ("malform token section start, expecting '<?TOKENS?>'.");
					addTok (ref reader, EbnfTokenType.TokenSectionStart);
					break;
				case '(':
					reader.Advance();
					addTok (ref reader, EbnfTokenType.OpenRoundBracket);
					break;
				case ')':
					reader.Advance();
					addTok (ref reader, EbnfTokenType.ClosingRoundBracket);
					break;
				case '|':
					reader.Advance();
					addTok (ref reader, EbnfTokenType.ChoiceOp);
					break;
				case '-':
					reader.Advance();
					addTok (ref reader, EbnfTokenType.ExclusionOp);
					break;
				case '?':
				case '+':
				case '*':
					reader.Advance();
					addTok (ref reader, EbnfTokenType.CardinalityOp);
					break;
				case '[':
					reader.Advance();
					addTok (ref reader, EbnfTokenType.OpenBracket);
					if (reader.TryPeek ('^')) {
						reader.Advance();
						addTok (ref reader, EbnfTokenType.CharMatchNegation);
					}
					while(!reader.Eol()) {
						char c = reader.Read ();
						if (c == ']') {
							addTok (ref reader, EbnfTokenType.ClosingBracket);
							break;
						} else if (c == '-')
							addTok (ref reader, EbnfTokenType.CharMatchRangeOperator);
						else if (c == '#' && reader.TryPeek ('x'))
							readUcsCodePoint (ref reader);
						else
							addTok (ref reader, EbnfTokenType.CharMatch);
					}
					/*if (reader.TryReadUntil ("]")) {
						addTok (ref reader, TokenType.CharMatch);
						reader.Advance (1);
						addTok (ref reader, TokenType.CharMatchClose);
						break;
					}*/
					break;
				case '#':
					reader.Advance ();
					readUcsCodePoint (ref reader);
					break;
				case '$':
					reader.Advance ();
					addTok (ref reader, EbnfTokenType.EndOfFile);
					break;
				case '.':
					reader.Advance ();
					addTok (ref reader, EbnfTokenType.CodePointMatch); // '.' is any char
					break;
				default:
					if (readName(ref reader))
						addTok (ref reader, EbnfTokenType.SymbolName);
					else if (reader.TryAdvance())
						addTok (ref reader, EbnfTokenType.Unknown);
					break;
				}
			}

			return Toks.ToArray();
		}

	}
}
