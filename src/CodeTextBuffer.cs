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

namespace Crow
{
	public class CodeTextBuffer : List<string>
	{


		#region CTOR
		public CodeTextBuffer () : base(){}

		public CodeTextBuffer (string rawSource) : this (){
			if (string.IsNullOrEmpty (rawSource))
				return;
			
			this.AddRange (Regex.Split (rawSource, "\r\n|\r|\n|\\\\n"));

			lineBreak = detectLineBreakKind (rawSource);
			findLongestLine ();
		}
		#endregion

		string lineBreak = Interface.LineBreak;

		public int longestLineIdx = 0;
		public int longestLineCharCount = 0;

		void findLongestLine(){
			longestLineCharCount = 0;
			for (int i = 0; i < this.Count; i++) {
				if (this[i].Length > longestLineCharCount) {
					longestLineCharCount = this[i].Length;
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
				return this.Count > 0 ? this.Aggregate((i, j) => i + this.lineBreak + j) : "";
			}
		}
	}
}

