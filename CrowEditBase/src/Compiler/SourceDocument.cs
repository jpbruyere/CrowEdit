// Copyright (c) 2021-2025  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
		protected SyntaxRootNode root;

		public Command CMDRefreshSyntaxTree;

        protected override void initCommands()
        {
            base.initCommands();

			CMDRefreshSyntaxTree = new ActionCommand ("Reparse", parse, "#icons.refresh.svg", true);
        }
        public SyntaxRootNode Root => root;
		public bool IsParsed => root != null && Tokens.Length > 0;
		public ReadOnlySpan<Token> Tokens => root.Tokens;
		public IEnumerable<SyntaxNode> SyntaxRootChildNodes => root?.children;

		public Token FindTokenIncludingPosition (int pos) {
			if (!IsParsed || pos == 0 || Tokens.Length == 0)
				return default;
			int idx = Tokens.BinarySearch(new  Token () {Start = pos});
			return idx == 0 ? Tokens[0] : idx < 0 ? Tokens[~idx - 1] : Tokens[idx];
		}
		public Token GetTokenByIndex(int tokIdx) => IsParsed && tokIdx >= 0 ?
						Tokens[Math.Min(Tokens.Length - 1, tokIdx)] : default;
		public int FindTokenIndexIncludingPosition (int pos) {
			if (!IsParsed || pos == 0 || Tokens.Length == 0)
				return default;
			int idx = Tokens.BinarySearch(new  Token () {Start = pos});
			return idx == 0 ? 0 : idx < 0 ? ~idx - 1 : idx;
		}
		/// <summary>
		/// if outermost is true, return oldest ancestor exept root node, useful for folding.
		/// </summary>
		public SyntaxNode FindNodeIncludingPosition (int pos, bool outerMost = false) {
			if (root == null)
				return null;
			if (!root.Contains (pos))
				return null;
			SyntaxNode sn = root.FindNodeIncludingPosition (pos);
			if (outerMost) {
				while (sn.Parent != root && sn.Span.Start == sn.Parent.Span.Start)
					sn = sn.Parent;
			}
			return sn;
		}
		/*public T FindNodeIncludingPosition<T> (int pos) {
			if (root == null)
				return default;
			if (!root.Contains (pos))
				return default;
			return root.FindNodeIncludingPosition<T> (pos);
		}
		public SyntaxNode FindNodeIncludingSpan (TextSpan span) {
			if (root == null)
				return null;
			if (!root.Contains (span))
				return null;
			return root.FindNodeIncludingSpan (span);
		}*/
		
		protected override void reloadFromFile () {
			base.reloadFromFile ();
			parse ();
		}
		protected override void apply(TextChange change)
		{
			SyntaxNode editedNode = root?.FindNodeIncludingSpan (new TextSpan (change.Start, change.End));

			base.apply(change);

			SyntaxAnalyser syntaxAnalyser = CreateSyntaxAnalyser ();
			root = syntaxAnalyser?.Process ();

			NotifyValueChanged("Exceptions", syntaxAnalyser?.Exceptions);

			//SyntaxNode changedNode = root.FindNodeIncludingSpan (TextSpan.FromStartAndLength (change.Start, change.ChangedText.Length));			
			
			
			/*
			SyntaxNode newNode = syntaxAnalyser.Root.FindNodeIncludingSpan (TextSpan.FromStartAndLength (change.Start, change.ChangedText.Length));

			if (editedNode == null) {
				//System.Diagnostics.Debugger.Break ();
				root = syntaxAnalyser.Root;
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
			*/

			//updateCurrentTokAndNode (change.End2);
			//EditedNode = editedNode;

			//Console.WriteLine ($"CurrentToken: idx({currentTokenIndex}) {currentToken} {RootNode.Root.GetTokenStringByIndex(currentTokenIndex)}");
		}


		public virtual Color GetColorForToken (TokenType tokType) {
			if (tokType.HasFlag (TokenType.Punctuation))
				return Colors.DarkGrey;
			if (tokType.HasFlag (TokenType.WhiteSpace))
				return Colors.Gainsboro;
			if (tokType.HasFlag (TokenType.Trivia))
				return Colors.Silver;
			if (tokType == TokenType.Keyword)
				return Colors.DarkSlateBlue;
			return Colors.Red;
		}
		public virtual string GetTokenTypeString (TokenType tokenType) => tokenType.ToString();
		//protected abstract Tokenizer CreateTokenizer ();
		protected abstract SyntaxAnalyser CreateSyntaxAnalyser ();
		public abstract IList GetSuggestions (int absoluteTextPos, int currentTokenIndex, SyntaxNode currentNode, CharLocation loc);

		void parse () {
			SyntaxAnalyser syntaxAnalyser = CreateSyntaxAnalyser ();
			root = syntaxAnalyser?.Process ();

			NotifyValueChanged("Exceptions", syntaxAnalyser?.Exceptions);
			NotifyValueChanged ("SyntaxRootChildNodes", (object)null);
			NotifyValueChanged ("SyntaxRootChildNodes", SyntaxRootChildNodes);
			
			//CurrentNode?.ExpandToTheTop();
			
			//CrowEditBase.App.Log (LogType.Low, $"Syntax Analysis done in {sw.ElapsedMilliseconds}(ms) {sw.ElapsedTicks}(ticks)");
		}

		public SyntaxException CurrentException {
			get => CrowEditBase.App.CurrentException;
			set {
				CrowEditBase.App.CurrentException = value;
				SetLocation(value.Location);
			} 
		}

	}
}