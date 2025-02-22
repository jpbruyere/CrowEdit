// Copyright (c) 2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Text;

namespace CrowEdit.Ebnf
{
	public abstract class CharRangeElement {
		public class SingleChar : CharRangeElement {
			public int CodePoint;
			public char AsChar => (char)CodePoint;
			public SingleChar (char c) {
				CodePoint = (int)c;
			}
			public SingleChar (int c) {
				CodePoint = c;
			}
			public SingleChar (ReadOnlySpan<char> ebnfUcsCodePoint) {
				CodePoint = int.Parse (ebnfUcsCodePoint.Slice (2), System.Globalization.NumberStyles.HexNumber);
			}
			public override string ToString() => $"#x{CodePoint:X}";
		}
		public class CharRange : CharRangeElement {
			public SingleChar RangeStart;
			public SingleChar RangeEnd;

			public CharRange (SingleChar rangeStart, SingleChar rangeEnd) {
				RangeStart = rangeStart;
				RangeEnd = rangeEnd;
			}
			public override string ToString() => $"{RangeStart}-{RangeEnd}";
		}
	}
}