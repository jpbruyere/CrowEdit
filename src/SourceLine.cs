//
//  SourceLine.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2017 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;

namespace Crow
{
	/// <summary>
	/// basic structure for line of source code
	/// </summary>
	public class SourceLine
	{
		public string RawText;
		public List<Token> Tokens = null;

		public int Length {
			get { return string.IsNullOrEmpty (RawText)? 0 : RawText.Length; }
		}
		public char this[int index]{
			get { return RawText [index]; }

		} 
		int ptr;	//character pointer in the source string
		Token tok;	//current token parsed before addition to the token list

		public SourceLine ()
		{
		}
		public SourceLine (string rawText){
			RawText = rawText;
		}

		/// <summary>
		/// Tokenize this instance.
		/// This tokenization step is used for display mainly, so literals are not interpreted
		/// </summary>
		public bool Tokenize(){
			Tokens = new List<Token>();
			ptr = 0;

			while (!eol) {
				Char c = readChar ();

				//block comments
				if (tok?.Type == TokenType.BlockComment) {
					tok.Content += c;
					if (c == '*') {
						if (peekChar () == '/') {
							tok.Content += readChar ();
							saveCurTok ();
						}
					}
					continue;
				} else if (tok?.Type == TokenType.StringLiteral) {
					tok.Content += c;
					if (c == '\\')//may escape " char, so next char is read;
						tok.Content += readChar ();
					else if (c == '"')
						saveCurTok ();					
					continue;
				} else if (tok?.Type == TokenType.CharacterLiteral) {
					tok.Content += c;
					if (c == '\\')//may escape ' char, so next char is read;
						tok.Content += readChar ();
					else if (c == '\'')
						saveCurTok ();					
					continue;
				} else if (tok?.Type == TokenType.WhiteSpace) {
					if (char.IsWhiteSpace (c)) {
						tok.Content += c;
						continue;
					}
					saveCurTok ();
					//if (char.IsLetter (c))
						
				}

				//single char tokens
				if (c == '{')
					tok.Type = TokenType.OpenBlock;
				else if (c == '}')
					tok.Type = TokenType.CloseBlock;
				else if (c == '(')
					tok.Type = TokenType.OpenParenth;
				else if (c == ')')
					tok.Type = TokenType.CloseParenth;
				

				if (tok == null) {
					tok = new Token () { Content = new string (c, 1) };

					if (char.IsWhiteSpace (c))
						tok.Type = TokenType.WhiteSpace;
					else if (char.IsDigit (c))
						tok.Type = TokenType.DigitalLiteral;
					else if (char.IsLetter (c))
						tok.Type = TokenType.Unknown;
					else if (c == '"')
						tok.Type = TokenType.StringLiteral;
					else {//put here all single step parsing token, reseting tok directely
						saveCurTok ();
					}
				}


					
				ptr++;
			}
			return true;
		}
		/// <summary> add tok to token list and reset it to null </summary>
		void saveCurTok(){
			Tokens.Add (tok);
			tok = null;
		}
		public void PresetCurrentToken (TokenType tokType, string content = null){
			tok = new Token (tokType,content);
		}

		bool eol { get { return ptr < RawText.Length; }}
		char readChar() {			
			char c = RawText [ptr];
			ptr++;
			return c;
		}
		char peekChar() {
			return RawText [ptr];
		}

//		public static implicit operator SourceLine(string rawText){
//			return new SourceLine() { RawText = rawText };
//		}
//		public static implicit operator string(SourceLine sl){
//			return sl?.RawText;
//		}
	}
}

