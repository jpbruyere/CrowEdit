﻿using System;
using System.IO;
using Crow;
using System.Collections.Generic;
using System.Diagnostics;

namespace Crow.Coding
{
	public abstract class Parser
	{
		public enum TokenType {
			Unknown,
			WhiteSpace,
			NewLine,
			LineComment,
			BlockComment,
			Type,
			Identifier,
			Indexer,
			OpenBlock,
			CloseBlock,
			StatementEnding,
			UnaryOp,
			BinaryOp,
			Affectation,
			StringLitteralOpening,
			StringLitteralClosing,
			StringLitteral,
			NumericLitteral,
			Preprocessor,
		}

		#region CTOR
		public Parser (CodeBuffer _buffer)
		{
			buffer = _buffer;
			Tokens = new List<TokenList> ();
			if (buffer.Length > 0)
				eof = false;
		}

		#endregion

		CodeBuffer buffer;

		internal int currentLine = 0;
		internal int currentColumn = 0;
		protected Token currentTok;
		protected bool eof = true;

		public List<TokenList> Tokens;

		public Point CurrentPosition { get { return new Point (currentLine, currentColumn); } }

		public abstract void Parse(int line);
		public virtual void SetLineInError(ParsingException ex) {
			currentTok = default(Token);
			Tokens [ex.Line] = new TokenList () {new Token () { Content = buffer [ex.Line] }};
		}

		#region low level parsing
		protected void readToCurrTok(bool startOfTok = false){
			if (startOfTok)
				currentTok.Start = CurrentPosition;
			currentTok += Read();
		}
		protected void readAndResetCurrentTok(System.Enum type, bool startToc = false) {
			readToCurrTok ();
			saveAndResetCurrentTok (type);
		}
		protected void saveAndResetCurrentTok() { this.saveAndResetCurrentTok (currentTok.Type); }
		protected void saveAndResetCurrentTok(System.Enum type) {
			currentTok.Type = (TokenType)type;
			currentTok.End = CurrentPosition;
			Tokens[currentLine].Add (currentTok);
			currentTok = default(Token);
		}
		protected virtual char Peek() {
			if (eof)
				throw new ParsingException (this, "Unexpected End of File");
			return currentColumn < buffer [currentLine].Length ?
				buffer [currentLine] [currentColumn] : '\n';
		}
		protected virtual string Peek(int length) {
			if (eof)
				throw new ParsingException (this, "Unexpected End of File");
			if (buffer[currentLine].Length - currentColumn - length < 0)
				throw new ParsingException (this, "Unexpected End of line");
			return buffer [currentLine].Substring (currentColumn, length);
		}
		protected virtual char Read() {
			char c = Peek ();

			if (c == '\n') {
				currentLine++;
				if (currentLine >= buffer.Length)
					eof = true;
				currentColumn = 0;
			} else
				currentColumn++;
			return c;
		}
		protected virtual string ReadUntil (string endExp){
			string tmp = "";

			while (!eof) {
				if (buffer [currentLine].Length - currentColumn - endExp.Length < 0) {
					currentLine++;
					if (currentLine >= buffer.Length)
						eof = true;
					currentColumn = 0;
					continue;
				}
				if (string.Equals (Peek (endExp.Length), endExp))
					return tmp;
				tmp += Read();
			}
			throw new ParsingException (this, string.Format("Expectign '{0}'", endExp));
		}
		protected void SkipWhiteSpaces () {
			if (currentTok.Type != TokenType.Unknown)
				throw new ParsingException (this, "current token should be reset to unknown (0) before skiping white spaces");
			while (!eof) {
				if (!char.IsWhiteSpace (Peek ())||Peek()=='\n')
					break;
				readToCurrTok (currentTok.Type == TokenType.Unknown);
				currentTok.Type = TokenType.WhiteSpace;
			}
			if (currentTok.Type != TokenType.Unknown)
				saveAndResetCurrentTok ();
		}
		#endregion
	}
}