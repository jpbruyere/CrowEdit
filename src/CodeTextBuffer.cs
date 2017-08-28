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
	public class CodeTextBuffer : List<SourceLine>
	{
		public CodeTextBuffer () : base()
		{
		}

		public int longestLineIdx = 0;
		public int longestLineCharCount = 0;

		public CodeTextBuffer (string rawSource) : this (){
			if (string.IsNullOrEmpty (rawSource))
				return;
			string[] lines = Regex.Split (rawSource, "\r\n|\r|\n|\\\\n");
			for (int i = 0; i < lines.Length; i++) {
				if (lines [i].Length > longestLineCharCount) {
					longestLineCharCount = lines [i].Length;
					longestLineIdx = i;
				}
				this.Add (new SourceLine ( lines [i] ));
			}
		}

		/// <summary>
		/// return all lines with linebreaks
		/// </summary>
		public string FullText{
			get {
				string tmp = "";
				foreach (SourceLine sl in this)
					tmp += sl.RawText + Interface.LineBreak;
				return tmp;
			}
		}			

		public void Tokenize (int lineIndex) {
			//handle multiline block comments
			if (lineIndex > 0){
				if (this [lineIndex - 1].Tokens?.LastOrDefault ().Type == TokenType.BlockComment)
					this [lineIndex].PresetCurrentToken (TokenType.BlockComment);
			}
			this [lineIndex].Tokenize ();
		}

		public void InsertLine(int index, SourceLine line){
			base.Insert (index, line);
		}
		public void RemoveLine (int index) {
			base.RemoveAt (index);
		}

		//public void Tokenize (int lineIndex) {
		//	//handle multiline block comments
		//	if (lineIndex > 0){
		//		if (this [lineIndex - 1].Tokens?.LastOrDefault ().Type == TokenType.BlockComment)
		//			this [lineIndex].PresetCurrentToken (TokenType.BlockComment);
		//	}
		//	this [lineIndex].Tokenize ();
		//}
	}
}

