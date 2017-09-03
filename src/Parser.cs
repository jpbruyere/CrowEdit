using System;
using System.IO;
using Crow;
using System.Collections.Generic;
using System.Diagnostics;

namespace Crow.Coding
{
	/// <summary>
	/// base class for tokenizing sources
	/// </summary>
	public abstract class Parser
	{
		/// <summary>
		/// Default tokens, this enum may be overriden in derived parser with the new keyword,
		/// see XMLParser for example.
		/// </summary>
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
			if (buffer.LineCount > 0)
				eof = false;
		}

		#endregion

		CodeBuffer buffer;

		internal int currentLine = 0;
		internal int currentColumn = 0;
		protected Token currentTok;
		protected bool eof = true;

		public List<TokenList> Tokens;
		protected TokenList TokensLine;

		public Point CurrentPosition {
			get { return new Point (currentLine, currentColumn); } 
			set {
				currentLine = value.Y;
				currentColumn = value.X;
			}
		}

		public abstract void Parse(int line);
		public virtual void SetLineInError(ParsingException ex) {
			currentTok = default(Token);
			Tokens [ex.Line] = new TokenList (ex, buffer [ex.Line]);
		}

		#region low level parsing
		/// <summary>
		/// Read one char from current position in buffer and store it into the current token
		/// </summary>
		/// <param name="startOfTok">if true, set the Start position of the current token to the current position</param>
		protected void readToCurrTok(bool startOfTok = false){
			if (startOfTok)
				currentTok.Start = CurrentPosition;
			currentTok += Read();
		}
		/// <summary>
		/// read n char from the buffer and store it into the current token
		/// </summary>
		protected void readToCurrTok(int length) {
			for (int i = 0; i < length; i++)
				currentTok += Read ();
		}
		/// <summary>
		/// Save current token into current TokensLine and raz current token
		/// </summary>
		protected void saveAndResetCurrentTok() {
			currentTok.End = CurrentPosition;
			TokensLine.Add (currentTok);
			currentTok = default(Token);
		}
		/// <summary>
		/// read one char and add current token to current TokensLine, current token is reset
		/// </summary>
		/// <param name="type">Type of the token</param>
		/// <param name="startToc">set start of token to current position</param>
		protected void readAndResetCurrentTok(System.Enum type, bool startToc = false) {
			readToCurrTok ();
			saveAndResetCurrentTok (type);
		}
		/// <summary>
		/// Save current tok
		/// </summary>
		/// <param name="type">set the type of the tok</param>
		protected void saveAndResetCurrentTok(System.Enum type) {
			currentTok.Type = (TokenType)type;
			saveAndResetCurrentTok ();
		}
		/// <summary>
		/// Peek next char, emit '\n' if current column > buffer's line length
		/// Throw error if eof is true
		/// </summary>
		protected virtual char Peek() {
			if (eof)
				throw new ParsingException (this, "Unexpected End of File");
			return currentColumn < buffer [currentLine].Length ?
				buffer [currentLine] [currentColumn] : '\n';
		}
		/// <summary>
		/// Peek n char from buffer or less if remaining char in buffer's line is less than requested
		/// if end of line is reached, no '\n' will be emitted, instead, empty string is returned. '\n' should be checked only
		/// with single char Peek().
		/// Throw error is eof is true
		/// </summary>
		/// <param name="length">Length.</param>
		protected virtual string Peek(int length) {
			if (eof)
				throw new ParsingException (this, "Unexpected End of File");
			int lg = Math.Min(length, Math.Max (buffer [currentLine].Length - currentColumn, buffer [currentLine].Length - currentColumn - length));
			if (lg == 0)
				return "";
			return buffer [currentLine].Substring (currentColumn, lg);
		}
		/// <summary>
		/// read one char from buffer at current position, if '\n' is read, current line is incremented
		/// and column is reset to 0
		/// </summary>
		protected virtual char Read() {
			char c = Peek ();
			//TODO: the parsing is done line by line, we should be able to remove the next line handling from read
			if (c == '\n') {
				currentLine++;
				if (currentLine >= buffer.LineCount)
					eof = true;
				currentColumn = 0;
			} else
				currentColumn++;
			return c;
		}
		/// <summary>
		/// read until end of line is reached
		/// </summary>
		/// <returns>string read</returns>
		protected virtual string ReadLine () {
			string tmp = "";
			while (!eof) {
				if (Peek () == '\n')
					return tmp;
				tmp += Read ();
			}
			return tmp;
		}
		/// <summary>
		/// read until end expression is reached or end of line.
		/// </summary>
		/// <returns>string read minus the ending expression that has to be read after</returns>
		/// <param name="endExp">Expression to search for</param>
		protected virtual string ReadLineUntil (string endExp){
			string tmp = "";

			while (!eof) {
				if (buffer [currentLine].Length - currentColumn - endExp.Length < 0) {
					tmp += ReadLine();
					break;
				}
				if (string.Equals (Peek (endExp.Length), endExp))
					return tmp;
				tmp += Read();
			}
			return tmp;
		}
		/// <summary>
		/// skip white spaces, but not line break. Save spaces in a WhiteSpace token.
		/// </summary>
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