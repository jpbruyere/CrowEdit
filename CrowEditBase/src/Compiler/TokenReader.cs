// Copyright (c) 2021-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using Crow.Text;
using Crow;

namespace CrowEditBase
{
    public struct TokenReader
    {
        int curPos;
        Token[] buffer;

        public TokenReader (Token[] span) {
            buffer = span;
            curPos = 0;
        }

        public int CurrentPosition => curPos;
		/// <summary>
        /// Current reader position is further the end of the buffer.
        /// </summary>
		public bool EndOfSpan => curPos >= buffer.Length;

        public void Seek (int position) => curPos = position;

        public Token Peek => buffer[curPos];
        public Token Read () => buffer[curPos++];
		public bool TryRead (out Token c) {
			if (EndOfSpan) {
				c = default;
				return false;
			}
			c = Read();
			return true;
		}
		public bool TryRead (Token c) => EndOfSpan ? false : EqualityComparer<Token>.Default.Equals(Read(), c);

		public ReadOnlySpan<Token> Read (int length) => buffer.AsSpan().Slice (curPos += length, length);
		public void Advance (int increment = 1) => curPos += increment;
		public bool TryAdvance (int increment = 1) {
			curPos += increment;
			return curPos < buffer.Length;
		}
		/// <summary>
		/// Retrieve a span of that buffer from provided starting position to the current reader position.
		/// </summary>
		/// <param name="fromPosition"></param>
		/// <returns></returns>
        public ReadOnlySpan<Token> Get (int fromPosition) => buffer.AsSpan().Slice (fromPosition, curPos - fromPosition);
		public bool TryPeek (Token c) => !EndOfSpan && EqualityComparer<Token>.Default.Equals(Peek, c);
		/// <summary>
		/// Try peak one char, return false if end of span, true otherwise.
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public bool TryPeek (out Token c) {
			c = default;
			if (EndOfSpan)
				return false;
			c = buffer[curPos];
			return true;
		}
	}	
}