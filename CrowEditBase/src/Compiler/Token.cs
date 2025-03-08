// Copyright (c) 2021-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Text;
using System.Diagnostics.CodeAnalysis;

namespace CrowEditBase
{
	public class Token : IComparable<Token>, IEquatable<Token> {
		public TokenType Type;
		public SyntaxNode syntaxNode;
		public int Start;
		public int Length;
		public int End => Start + Length;
		public TextSpan Span => new TextSpan (Start, End);
		public string AsString (ReadOnlySpan<char> source)
			=> source.Slice (Start, Length).ToString();
		public char GetChar (ReadOnlySpan<char> source, int charIndex = 0)
			=> source[Start + charIndex];
		public Token (TokenType type, int pos) {
			Type = type;
			Start = pos;
			Length = 1;
		}
		public Token (TokenType type, int start, int end) {
			Type = type;
			Start = start;
			Length = end - start;
		}
		public Token (int start, int length, TokenType type) {
			Type = type;
			Start = start;
			Length = length;
		}
		public Token (int start) {
			Start = start;
		}

		public int CompareTo([AllowNull] Token other)
			=> Start - other.Start;
		public bool Equals([AllowNull] Token other)
			=> Type == other.Type && Start == other.Start && Length == other.Length;
		public override bool Equals(object obj)
			=> obj is Token other ? Equals (other) : false;
		public override int GetHashCode() => HashCode.Combine (Type, Start, Length);
		public override string ToString() => $"{Type}:{Start},{Length};";
	}
}