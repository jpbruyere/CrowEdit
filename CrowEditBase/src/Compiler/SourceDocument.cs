// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow;
using Crow.Text;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using Drawing2D;

namespace CrowEditBase
{
	public abstract class SourceDocument : TextDocument {
		public SourceDocument (string fullPath, string editorPath = "#ui.sourceEditor.itmp")
			: base (fullPath, editorPath) {
		}
		protected Token[] tokens;
		protected SyntaxNode RootNode;
		protected Token currentToken => currentTokenIndex < 0 ? default : tokens[currentTokenIndex];
		SyntaxNode currentNode;
		public SyntaxNode CurrentNode {
			get => currentNode;
			set {
				if (currentNode == value)
					return;
				currentNode = value;
				NotifyValueChanged ("CurrentNode", currentNode);
			}
		}
		public string CurrentTokenString => RootNode?.Root.GetTokenStringByIndex (currentTokenIndex);
		public Token CurrentToken => currentToken;

		//public SyntaxNode EditedNode { get; protected set; }
		protected int currentTokenIndex;

		public Token[] Tokens => tokens;
		public SyntaxNode SyntaxRootNode => RootNode;
		public IEnumerable<SyntaxNode> SyntaxRootChildNodes => RootNode?.children;
		public LineCollection Lines => lines;
		public Token FindTokenIncludingPosition (int pos) {
			if (pos == 0 || tokens == null || tokens.Length == 0)
				return default;
			int idx = Array.BinarySearch (tokens, 0, tokens.Length, new  Token () {Start = pos});

			return idx == 0 ? tokens[0] : idx < 0 ? tokens[~idx - 1] : tokens[idx - 1];
		}
		public int FindTokenIndexIncludingPosition (int pos) {
			if (pos == 0 || tokens == null || tokens.Length == 0)
				return default;
			int idx = Array.BinarySearch (tokens, 0, tokens.Length, new  Token () {Start = pos});

			return idx == 0 ? 0 : idx < 0 ? ~idx - 1 : idx - 1;
		}
		/// <summary>
		/// if outermost is true, return oldest ancestor exept root node, useful for folding.
		/// </summary>
		public SyntaxNode FindNodeIncludingPosition (int pos, bool outerMost = false) {
			if (RootNode == null)
				return null;
			if (!RootNode.Contains (pos))
				return null;
			SyntaxNode sn = RootNode.FindNodeIncludingPosition (pos);
			if (outerMost) {
				while (sn.Parent != RootNode && sn.Span.Start == sn.Parent.Span.Start)
					sn = sn.Parent;
			}
			return sn;
		}
		public T FindNodeIncludingPosition<T> (int pos) {
			if (RootNode == null)
				return default;
			if (!RootNode.Contains (pos))
				return default;
			return RootNode.FindNodeIncludingPosition<T> (pos);
		}
		public SyntaxNode FindNodeIncludingSpan (TextSpan span) {
			if (RootNode == null)
				return null;
			if (!RootNode.Contains (span))
				return null;
			return RootNode.FindNodeIncludingSpan (span);
		}
		protected override void reloadFromFile () {
			base.reloadFromFile ();
			parse ();
		}
		protected override void apply(TextChange change)
		{
			SyntaxNode editedNode = FindNodeIncludingSpan (new TextSpan (change.Start, change.End));

			base.apply(change);

			Tokenizer tokenizer = CreateTokenizer ();
			tokens = tokenizer.Tokenize (Source);
			SyntaxAnalyser syntaxAnalyser = CreateSyntaxAnalyser ();
			syntaxAnalyser.Process ();

			SyntaxNode newNode = syntaxAnalyser.Root.FindNodeIncludingSpan (TextSpan.FromStartAndLength (change.Start, change.ChangedText.Length));

			if (editedNode == null) {
				//System.Diagnostics.Debugger.Break ();
				RootNode = syntaxAnalyser.Root;
			} else if (newNode.IsSimilar (editedNode)) {
				if (!tryReplaceNode (editedNode, newNode))
					RootNode = syntaxAnalyser.Root;
			} else if (newNode.Parent != null && newNode.Parent.IsSimilar (editedNode)) {
				if (!tryReplaceNode (editedNode, newNode.Parent))
					RootNode = syntaxAnalyser.Root;
			} else if (editedNode.Parent != null && newNode.IsSimilar (editedNode.Parent)) {
				if (!tryReplaceNode (editedNode.Parent, newNode))
					RootNode = syntaxAnalyser.Root;
			} else if (newNode.Parent != null && editedNode.Parent != null && newNode.Parent.IsSimilar (editedNode.Parent)) {
				if (!tryReplaceNode (editedNode.Parent, newNode.Parent))
					RootNode = syntaxAnalyser.Root;
			} else {
				//System.Diagnostics.Debugger.Break ();
				RootNode = syntaxAnalyser.Root;
			}

			//updateCurrentTokAndNode (change.End2);
			//EditedNode = editedNode;

			//Console.WriteLine ($"CurrentToken: idx({currentTokenIndex}) {currentToken} {RootNode.Root.GetTokenStringByIndex(currentTokenIndex)}");
		}
		static bool tryReplaceNode (SyntaxNode editedNode, SyntaxNode newNode) {
			if (newNode is SyntaxRootNode || editedNode is SyntaxRootNode)
				return false;
			editedNode.Replace (newNode);
			return true;
		}

		internal void updateCurrentTokAndNode (CharLocation loc) {
			int pos = lines.GetAbsolutePosition(loc);
			if (tokens.Length > 0) {
				currentTokenIndex = FindTokenIndexIncludingPosition (pos);
				CurrentNode = FindNodeIncludingSpan (currentToken.Span);
				NotifyValueChanged ("CurrentTokenString", (object)CurrentTokenString);
			}else {
				currentTokenIndex = -1;
				CurrentNode = null;
				NotifyValueChanged ("CurrentTokenString", (object)"no token");
			}
		}

		public virtual Color GetColorForToken (TokenType tokType) {
			if (tokType.HasFlag (TokenType.Punctuation))
				return Colors.DarkGrey;
			if (tokType.HasFlag (TokenType.Trivia))
				return Colors.DimGrey;
			if (tokType == TokenType.Keyword)
				return Colors.DarkSlateBlue;
			return Colors.Red;
		}
		protected abstract Tokenizer CreateTokenizer ();
		protected abstract SyntaxAnalyser CreateSyntaxAnalyser ();
		public abstract IList GetSuggestions (CharLocation loc);

		/// <summary>
		/// complete current token with selected item from the suggestion overlay.
		/// It may set a new position or a new selection.
		/// </summary>
		/// <param name="suggestion">selected object of suggestion overlay</param>
		/// /// <param name="change">the text change to apply</param>
		/// <param name="newSelection">new position or selection, null if normal position after text changes</param>
		/// <returns>true if successed</returns>
		public abstract bool TryGetCompletionForCurrentToken (object suggestion, out TextChange change, out TextSpan? newSelection);
		void parse () {
			Tokenizer tokenizer = CreateTokenizer ();
			tokens = tokenizer.Tokenize (Source);

			SyntaxAnalyser syntaxAnalyser = CreateSyntaxAnalyser ();
			//Stopwatch sw = Stopwatch.StartNew ();
			syntaxAnalyser.Process ();
			//sw.Stop();
			RootNode = syntaxAnalyser.Root;

			/*Console.WriteLine ($"Syntax Analysis done in {sw.ElapsedMilliseconds}(ms) {sw.ElapsedTicks}(ticks)");
			foreach (SyntaxException ex in syntaxAnalyser.Exceptions)
				Console.WriteLine ($"{ex}");*/

				/*foreach (Token t in Tokens)
					Console.WriteLine ($"{t,-40} {Source.AsSpan(t.Start, t.Length).ToString()}");
				syntaxAnalyser.Root.Dump();*/
		}

	}
}