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
	public abstract class SyntaxRootNode : SyntaxNode {
		internal readonly SourceDocument source;
		public SyntaxRootNode (SourceDocument source) {
			this.source = source;
		}
		public override int TokenIndexBase => 0;
		public override int? LastTokenOffset { get => Math.Max (0, source.Tokens.Length - 1); internal set {} }
		public override SyntaxRootNode Root => this;
		public override bool IsFoldable => false;
		public override SyntaxNode NextSiblingOrParentsNextSibling => null;
		public override void UnfoldToTheTop() {}
		public string GetTokenStringByIndex (int idx) =>
			idx >= 0 && idx < Root.source.Tokens.Length ? Root.source.Tokens[idx].AsString (Root.source.Source) : null;
		public Token? GetTokenByIndex (int idx) =>
			idx >= 0 && idx < Root.source.Tokens.Length ? Root.source.Tokens[idx] : default;
	}
	public class SyntaxNode : CrowEditComponent {
		bool _isExpanded;
		public bool isExpanded {
			get => _isExpanded;
			set {
				if  (_isExpanded == value)
					return;
				_isExpanded = value;
				NotifyValueChanged (_isExpanded);
			}
		}
		public void ExpandToTheTop () {
			isExpanded = true;
			Parent?.ExpandToTheTop ();
		}
		public SyntaxNode Parent { get; private set; }
		public int StartLine { get; private set; }
		public virtual int LineCount => lineCount;
		public virtual bool IsComplete => LastTokenOffset.HasValue;
		public virtual bool IsFoldable => Parent.StartLine != StartLine && lineCount > 1;
		public virtual SyntaxRootNode Root => Parent.Root;
		public virtual void UnfoldToTheTop () {
			isFolded = false;
			Parent.UnfoldToTheTop ();
		}
		protected Token getTokenByIndex (int idx) => idx < Root.source.Tokens.Length ? Root.source.Tokens[idx] : default;
		internal List<SyntaxNode> children = new List<SyntaxNode> ();
		public IEnumerable<SyntaxNode> Children => children;
		//public int IndexOf (SyntaxNode node) => children.IndexOf (node);
		public bool HasChilds => children.Count > 0;
		public SyntaxNode NextSibling {
			get {
				if (Parent != null) {
					int idx  = Parent.children.IndexOf (this);
					if (idx < Parent.children.Count - 1)
						return Parent.children[idx + 1];
				}
				return null;
			}
		}
		public SyntaxNode PreviousSibling {
			get {
				if (Parent != null) {
					int idx  = Parent.children.IndexOf (this);
					if (idx > 0)
						return Parent.children[idx - 1];
				}
				return null;
			}
		}
		public virtual SyntaxNode NextSiblingOrParentsNextSibling
			=> NextSibling ?? Parent.NextSiblingOrParentsNextSibling;
		public IEnumerable<SyntaxNode> FoldableNodes {
			get {
				if (IsFoldable)
					yield return this;
				foreach	(SyntaxNode n in Children) {
					foreach (SyntaxNode folds in n.FoldableNodes)
						yield return folds;
				}
			}
		}
		public virtual int FoldedLineCount {
			get {
				if (isFolded)
					return lineCount;
				int tmp = 0;
				if (HasChilds) {
					foreach (SyntaxNode n in children.Where (c => c.IsFoldable))
						tmp += n.FoldedLineCount;
				}
				return tmp;
			}
		}

		public virtual int TokenIndexBase { get; private set; }
		public virtual int? LastTokenOffset { get; internal set; }
		public int? LastTokenIndex => TokenIndexBase + LastTokenOffset;
		internal SyntaxNode () {}
		public SyntaxNode (int startLine, int tokenBase, int? lastTokenIdx = null) {
			StartLine = startLine;
			TokenIndexBase = tokenBase;
			if (lastTokenIdx.HasValue)
				LastTokenOffset = lastTokenIdx - tokenBase;
		}
		internal bool isFolded;
		internal int lineCount;

		public int EndLine {
			internal set {
				lineCount = value - StartLine + 1;
			}
			get => StartLine + lineCount - 1;
		}
		public TextSpan Span {
			get {
				/*if (HasChilds) {
					return new TextSpan (children.First().Span.Start, children.Last().Span.End)
				}*/
				try {
				Token startTok = getTokenByIndex(TokenIndexBase);
				Token endTok = LastTokenOffset.HasValue ? getTokenByIndex (TokenIndexBase+LastTokenOffset.Value) : startTok;
				return new TextSpan (startTok.Start, endTok.End);
				}catch{
					System.Diagnostics.Debugger.Break ();
				}
				return default;

			}
		}
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
		public void Replace (SyntaxNode newNode) {
			Parent.replaceChild (this, newNode);
		}
		void replaceChild (SyntaxNode oldNode, SyntaxNode newNode) {
			int idx = children.IndexOf (oldNode);
			children[idx] = newNode;
			newNode.Parent = this;
			int tokIdxDiff = newNode.LastTokenOffset.Value - oldNode.LastTokenOffset.Value;
			int lineDiff = newNode.EndLine - oldNode.EndLine;
			if (tokIdxDiff == 0 && lineDiff == 0)
				return;

			SyntaxNode curNode = this;
			while (curNode != null) {
				curNode.lineCount += lineDiff;
				curNode.LastTokenOffset += tokIdxDiff;
				if (curNode is SyntaxRootNode)
					break;
				while (++idx < curNode.children.Count)
					curNode.children[idx].offset (tokIdxDiff, lineDiff);
				idx = curNode.Parent.children.IndexOf (curNode);
				curNode = curNode.Parent;
			}
		}
		void offset (int tokenOffset, int lineOffset) {
			TokenIndexBase += tokenOffset;
			StartLine += lineOffset;
			foreach (SyntaxNode child in children) {
				child.offset (tokenOffset, lineOffset);
			}
		}
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
		public SyntaxNode FindNodeIncludingSpan (TextSpan span) {
			foreach (SyntaxNode node in children) {
				if (node.Contains (span))
					return node.FindNodeIncludingSpan (span);
			}
			return this;
		}
		public bool Contains (int pos) => Span.Contains (pos);
		public bool Contains (TextSpan span) => Span.Contains (span);
		public void Dump (int level = 0) {
			Console.WriteLine ($"{new string('\t', level)}{this}");
			foreach (SyntaxNode node in children)
				node.Dump (level + 1);
		}
		public override string ToString() => $"l:({StartLine,3},{LineCount,3}) tks:{TokenIndexBase},{LastTokenOffset} {this.GetType().Name}";
		public string AsText() {
			TextSpan span = Span;
			return Root.source.Source.Substring (span.Start, span.Length);
		}
		public bool IsSimilar (SyntaxNode other) => this.GetType() == other?.GetType();
	}
}