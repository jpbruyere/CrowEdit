// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using Crow.Text;

namespace CrowEditBase
{
	public class TokenizerException : Exception {
		public readonly int Position;
		public TokenizerException (int position, string message) : base (message) {
			Position = position;
		}
	}
	public abstract class Tokenizer {
		protected List<Token> Toks;
		protected int startOfTok;

		public Tokenizer  () {}
		public abstract Token[] Tokenize (string source);
		/// <summary>
		/// First method to call in tokenizers to init parsing variables
		/// </summary>
		/// <returns></returns>
		protected virtual SpanCharReader initParsing (string source) {
			startOfTok = 0;
			Toks = new List<Token>(100);
			return new SpanCharReader(source);
		}
		/// <summary>
		/// Add token delimited from 'startOfTok' to current reader position.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="tokType"></param>
		protected void addTok (ref SpanCharReader reader, Enum tokType) {
			if (reader.CurrentPosition == startOfTok)
				return;
			Toks.Add (new Token((TokenType)tokType, startOfTok, reader.CurrentPosition));
			startOfTok = reader.CurrentPosition;
		}
		protected virtual void skipWhiteSpaces (ref SpanCharReader reader) {
			while(!reader.EndOfSpan) {
				switch (reader.Peak) {
					case '\x85':
					case '\x2028':
					case '\xA':
						reader.Read();
						addTok (ref reader, TokenType.LineBreak);
						break;
					case '\xD':
						reader.Read();
						if (reader.IsNextCharIn ('\xA', '\x85'))
							reader.Read();
						addTok (ref reader, TokenType.LineBreak);
						break;
					case '\x20':
					case '\x9':
						char c = reader.Read();
						while (reader.TryPeak (c))
							reader.Read();
						addTok (ref reader, c == '\x20' ? TokenType.WhiteSpace : TokenType.Tabulation);
						break;
					default:
						return;
				}
			}
		}

	}
}
