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

namespace Crow.Coding
{
	public class CodeBufferEventArgs : EventArgs {
		public int LineStart;
		public int LineCount;

		public CodeBufferEventArgs(int lineNumber) {
			LineStart = lineNumber;
			LineCount = 1;
		}
		public CodeBufferEventArgs(int lineStart, int lineCount) {
			LineStart = lineStart;
			LineCount = lineCount;
		}
	}

	public class CodeTextBuffer
	{
		#region Events
		public event EventHandler<CodeBufferEventArgs> LineUpadateEvent;
		public event EventHandler<CodeBufferEventArgs> LineRemoveEvent;
		public event EventHandler<CodeBufferEventArgs> LineAdditionEvent;
		public event EventHandler BufferCleared;
		#endregion

		#region CTOR
		public CodeTextBuffer () : base() {}
		#endregion


		List<string> lines = new List<string>();

		public int Length { get { return lines.Count;}}

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
			findLongestLine ();
		}
		string lineBreak = Interface.LineBreak;

		public int longestLineIdx = 0;
		public int longestLineCharCount = 0;

		void findLongestLine(){
			longestLineCharCount = 0;
			for (int i = 0; i < lines.Count; i++) {
				if (lines[i].Length > longestLineCharCount) {
					longestLineCharCount = lines[i].Length;
					longestLineIdx = i;
				}
			}
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
	}
}

