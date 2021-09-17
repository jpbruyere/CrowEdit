// Copyright (c) 2021-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;

namespace CrowEditBase
{
	public class SyntaxNode {
		public SyntaxNode Parent { get; private set; }
		List<SyntaxNode> children = new List<SyntaxNode> ();

		public readonly Token StartToken;
		public Token? EndToken { get; set; }
		public SyntaxNode (Token tokStart, Token? tokEnd = null) {
			StartToken = tokStart;
			EndToken = tokEnd;
		}

		public virtual bool IsComplete => EndToken.HasValue;

		public SyntaxNode AddChild (SyntaxNode child) {
			children.Add (child);
			child.Parent = this;
			return child;
		}
		public void RemoveChild (SyntaxNode child) {
			children.Remove (child);
			child.Parent = null;
		}
		public T GetChild<T> () => children.OfType<T> ().FirstOrDefault ();
		public SyntaxNode FindNodeIncludingPosition (int pos) {
			foreach (SyntaxNode node in children) {
				if (node.Contains (pos))
					return node.FindNodeIncludingPosition (pos);
			}
			return this;
		}
		public T FindNodeIncludingPosition<T> (int pos) {
			foreach (SyntaxNode node in children) {
				if (node.Contains (pos))
					return node.FindNodeIncludingPosition<T> (pos);
			}

			return this is T tt ? tt : default;
		}
		public virtual SyntaxNode Root => Parent.Root;
		public bool Contains (int pos) =>
			EndToken.HasValue ?
				StartToken.Start <= pos && EndToken.Value.End >= pos : false;

		public void Dump (int level = 0) {
			Console.WriteLine ($"{new string('\t', level)}{this}");
			foreach (SyntaxNode node in children)
				node.Dump (level + 1);
		}
		public override string ToString() => $"{this.GetType().Name}: {StartToken} -> {EndToken}";
	}
}