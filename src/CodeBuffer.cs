//
//  CodeTextBuffer.cs
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
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Crow.Coding
{
	/// <summary>
	/// Code buffer, lines are arranged in a List<string>, new line chars are removed during string.split on '\n...',
	/// </summary>
	public class CodeBuffer
	{
		//those events are handled in SourceEditor to help keeping sync between textbuffer,parser and editor.
		//modified lines are marked for reparse
		#region Events
		public event EventHandler<CodeBufferEventArgs> LineUpadateEvent;
		public event EventHandler<CodeBufferEventArgs> LineRemoveEvent;
		public event EventHandler<CodeBufferEventArgs> LineAdditionEvent;
		public event EventHandler BufferCleared;
		#endregion

		#region CTOR
		public CodeBuffer () {
			this.Add ("");
		}
		#endregion

		string lineBreak = Interface.LineBreak;
		List<string> lines = new List<string>();
		public int longestLineIdx = 0;
		public int longestLineCharCount = 0;
		/// <summary>
		/// real position in char arrays, tab = 1 char
		/// </summary>
		int _currentLine = 0;
		int _currentCol = 0;

		public int LineCount { get { return lines.Count;}}

		/// <summary>
		/// Return line with tabs replaced by spaces
		/// </summary>
		public string GetPrintableLine(int i){
			return string.IsNullOrEmpty(lines[i]) ? "" : lines[i].Replace("\t", new String(' ', Interface.TabSize));
		}

		public string this[int i]
		{
			get { return lines[i]; }
			set {
				if (lines [i] == value)
					return;
				lines[i] = value;
				LineUpadateEvent.Raise (this, new CodeBufferEventArgs (i));
			}
		}

		public void RemoveAt(int i){
			lines.RemoveAt(i);
			LineRemoveEvent.Raise (this, new CodeBufferEventArgs (i));
		}
		public void Insert(int i, string item){
			lines.Insert (i, item);
			LineAdditionEvent.Raise (this, new CodeBufferEventArgs (i));
		}
		public void Add(string item){
			lines.Add (item);
			LineAdditionEvent.Raise (this, new CodeBufferEventArgs (lines.Count - 1));
		}
		public void AddRange (string[] items){
			int start = lines.Count;
			lines.AddRange (items);
			LineAdditionEvent.Raise (this, new CodeBufferEventArgs (start, items.Length));
		}
		public void Clear () {
			lines.Clear();
			BufferCleared.Raise (this, null);
		}

		public void Load(string rawSource) {
			this.Clear();

			if (string.IsNullOrEmpty (rawSource))
				return;

			AddRange (Regex.Split (rawSource, "\r\n|\r|\n|\\\\n"));

			lineBreak = detectLineBreakKind (rawSource);
			FindLongestVisualLine ();
		}

		public void FindLongestVisualLine(){
			longestLineCharCount = 0;
			for (int i = 0; i < this.LineCount; i++) {
				if (this.GetPrintableLine(i).Length > longestLineCharCount) {
					longestLineCharCount = this.GetPrintableLine(i).Length;
					longestLineIdx = i;
				}
			}
			Debug.WriteLine ("Longest line: {0}->{1}", longestLineIdx, longestLineCharCount);
		}
		/// <summary> line break could be '\r' or '\n' or '\r\n' </summary>
		static string detectLineBreakKind(string buffer){
			string strLB = "";

			if (string.IsNullOrEmpty(buffer))
				return Interface.LineBreak;
			int i = 0;
			while ( i < buffer.Length) {
				if (buffer [i] == '\r') {
					strLB += '\r';
					i++;
				}
				if (i < buffer.Length) {
					if (buffer [i] == '\r')
						return "\r";
					if (buffer[i] == '\n')
						strLB += '\n';
				}
				if (!string.IsNullOrEmpty (strLB))
					return strLB;
				i++;
			}
			return Interface.LineBreak;
		}
		/// <summary>
		/// return all lines with linebreaks
		/// </summary>
		public string FullText{
			get {
				return lines.Count > 0 ? lines.Aggregate((i, j) => i + this.lineBreak + j) : "";
			}
		}

		/// <summary>
		/// convert visual position to buffer position
		/// </summary>
		Point getBuffPos (Point visualPos) {
			int i = 0;
			int buffCol = 0;
			while (i < visualPos.X) {
				if (this [visualPos.Y] [buffCol] == '\t')
					i += Interface.TabSize;
				else
					i++;
				buffCol++;
			}
			return new Point (buffCol, visualPos.Y);
		}
		/// <summary>
		/// convert buffer postition to visual position
		/// </summary>
		Point getTabulatedPos (Point buffPos) {
			int vCol = this[buffPos.Y].Substring(0, buffPos.X).Replace("\t", new String(' ', Interface.TabSize)).Length;
			return new Point (vCol, buffPos.Y);
		}
		/// <summary>
		/// Gets visual position computed from actual buffer position
		/// </summary>
		public Point TabulatedPosition {
			get { return getTabulatedPos (new Point (_currentCol, _currentLine)); }
		}
		/// <summary>
		/// set buffer current position from visual position
		/// </summary>
		public void SetBufferPos(Point tabulatedPosition) {
			CurrentPosition = getBuffPos(tabulatedPosition);
		}

		#region Editing and moving cursor
		public string SelectedText {
			get {
				if (selectionIsEmpty)
					return "";
				if (selectionStart.Y == selectionEnd.Y)
					return this [selectionStart.Y].Substring (selectionStart.X, selectionEnd.X - selectionStart.X);
				string tmp = "";
				tmp = this [selectionStart.Y].Substring (selectionStart.X);
				for (int l = selectionStart.Y + 1; l < selectionEnd.Y; l++) {
					tmp += Interface.LineBreak + this [l];
				}
				tmp += Interface.LineBreak + this [selectionEnd.Y].Substring (0, selectionEnd.X);
				return tmp;
			}
		}
		Point selectionStart = -1;
		Point selectionEnd = -1;
		/// <summary>
		/// Set selection in buffer coords from tabulated coords
		/// </summary>
		public void SetSelection (Point tabulatedStart, Point tabulatedEnd) {
			selectionStart = getBuffPos (tabulatedStart);
			selectionEnd = getBuffPos (tabulatedEnd);
		}
		/// <summary>
		/// Set selection in buffer to -1, empty selection
		/// </summary>
		public void ResetSelection () {
			selectionStart = selectionEnd = -1;
		}
		bool selectionIsEmpty {
			get { return selectionStart == selectionEnd; }
		}
		/// <summary>
		/// Current column in buffer coordinate, tabulation = 1 char
		/// </summary>
		public int CurrentColumn{
			get { return _currentCol; }
			set {
				if (value == _currentCol)
					return;
				if (value < 0)
					_currentCol = 0;
				else if (value >  lines [_currentLine].Length)
					_currentCol = lines [_currentLine].Length;
				else
					_currentCol = value;
			}
		}
		/// <summary>
		/// Current row in buffer coordinate, tabulation = 1 char
		/// </summary>
		public int CurrentLine{
			get { return _currentLine; }
			set {
				if (value == _currentLine)
					return;
				if (value >= lines.Count)
					_currentLine = lines.Count-1;
				else if (value < 0)
					_currentLine = 0;
				else
					_currentLine = value;
				//force recheck of currentCol for bounding
				int cc = _currentCol;
				_currentCol = 0;
				CurrentColumn = cc;
			}
		}
		/// <summary>
		/// Current position in buffer coordinate, tabulation = 1 char
		/// </summary>
		public Point CurrentPosition {
			get { return new Point(CurrentColumn, CurrentLine); }
			set {
				_currentCol = value.X;
				_currentLine = value.Y;
			}
		}
		/// <summary>
		/// get char at current position in buffer
		/// </summary>
		protected Char CurrentChar { get { return lines [CurrentLine] [CurrentColumn]; } }

		/// <summary>
		/// Moves cursor one char to the left, move up if cursor reaches start of line
		/// </summary>
		/// <returns><c>true</c> if move succeed</returns>
		public bool MoveLeft(){
			int tmp = _currentCol - 1;
			if (tmp < 0) {
				if (_currentLine == 0)
					return false;
				_currentCol = int.MaxValue;
				CurrentLine--;
			} else
				CurrentColumn = tmp;
			return true;
		}
		/// <summary>
		/// Moves cursor one char to the right, move down if cursor reaches end of line
		/// </summary>
		/// <returns><c>true</c> if move succeed</returns>
		public bool MoveRight(){
			int tmp = _currentCol + 1;
			if (tmp > this [_currentLine].Length){
				if (CurrentLine == this.LineCount - 1)
					return false;
				CurrentLine++;
				CurrentColumn = 0;
			} else
				CurrentColumn = tmp;
			return true;
		}
		public void GotoWordStart(){
			if (this[CurrentLine].Length == 0)
				return;
			CurrentColumn--;
			//skip white spaces
			while (!char.IsLetterOrDigit (this.CurrentChar) && CurrentColumn > 0)
				CurrentColumn--;
			while (char.IsLetterOrDigit (this.CurrentChar) && CurrentColumn > 0)
				CurrentColumn--;
			if (!char.IsLetterOrDigit (this.CurrentChar))
				CurrentColumn++;
		}
		public void GotoWordEnd(){
			//skip white spaces
			if (CurrentColumn >= this [CurrentLine].Length - 1)
				return;
			while (!char.IsLetterOrDigit (this.CurrentChar) && CurrentColumn < this [CurrentLine].Length-1)
				CurrentColumn++;
			while (char.IsLetterOrDigit (this.CurrentChar) && CurrentColumn < this [CurrentLine].Length-1)
				CurrentColumn++;
			if (char.IsLetterOrDigit (this.CurrentChar))
				CurrentColumn++;
		}
		public void DeleteChar()
		{
			if (selectionIsEmpty) {
				if (CurrentColumn == 0) {
					if (CurrentLine == 0 && this.LineCount == 1)
						return;
					CurrentLine--;
					CurrentColumn = this [CurrentLine].Length;
					this [CurrentLine] += this [CurrentLine + 1];
					RemoveAt (CurrentLine + 1);
					return;
				}
				CurrentColumn--;
				this [CurrentLine] = this [CurrentLine].Remove (CurrentColumn, 1);
			} else {
				int linesToRemove = selectionEnd.Y - selectionStart.Y + 1;
				int l = selectionStart.Y;

				if (linesToRemove > 0) {
					this [l] = this [l].Remove (selectionStart.X, this [l].Length - selectionStart.X) +
						this [selectionEnd.Y].Substring (selectionEnd.X, this [selectionEnd.Y].Length - selectionEnd.X);
					l++;
					for (int c = 0; c < linesToRemove-1; c++)
						RemoveAt (l);
					CurrentLine = selectionStart.Y;
					CurrentColumn = selectionStart.X;
				} else
					this [l] = this [l].Remove (selectionStart.X, selectionEnd.X - selectionStart.X);
				CurrentColumn = selectionStart.X;
				ResetSelection ();
			}
		}
		/// <summary>
		/// Insert new string at caret position, should be sure no line break is inside.
		/// </summary>
		/// <param name="str">String.</param>
		public void Insert(string str)
		{
			if (!selectionIsEmpty)
				this.DeleteChar ();
			string[] strLines = Regex.Split (str, "\r\n|\r|\n|" + @"\\n").ToArray();
			this [CurrentLine] = this [CurrentLine].Insert (CurrentColumn, strLines[0]);
			CurrentColumn += strLines[0].Length;
			for (int i = 1; i < strLines.Length; i++) {
				InsertLineBreak ();
				this [CurrentLine] = this [CurrentLine].Insert (CurrentColumn, strLines[i]);
				CurrentColumn += strLines[i].Length;
			}
		}
		/// <summary>
		/// Insert a line break.
		/// </summary>
		public void InsertLineBreak()
		{
			if (CurrentColumn > 0) {
				Insert (CurrentLine + 1, this [CurrentLine].Substring (CurrentColumn));
				this [CurrentLine] = this [CurrentLine].Substring (0, CurrentColumn);
			} else
				Insert(CurrentLine, "");

			CurrentLine++;
			CurrentColumn = 0;
		}
		#endregion
	}
}

