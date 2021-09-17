// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow;
using Crow.Text;
using System.Diagnostics;
using System.Collections;

namespace CrowEditBase
{
	public abstract class SourceDocument : TextDocument {
		public SourceDocument (string fullPath)
			: base (fullPath) {
		}
		protected Token[] tokens;
		protected SyntaxNode RootNode;
		protected Token currentToken;
		protected SyntaxNode currentNode;

		public Token[] Tokens => tokens;
		public Token FindTokenIncludingPosition (int pos) {
			if (pos == 0 || tokens == null || tokens.Length == 0)
				return default;
			int idx = Array.BinarySearch (tokens, 0, tokens.Length, new  Token () {Start = pos});

			return idx == 0 ? tokens[0] : idx < 0 ? tokens[~idx - 1] : tokens[idx - 1];
		}
		public SyntaxNode FindNodeIncludingPosition (int pos) {
			if (RootNode == null)
				return null;
			if (!RootNode.Contains (pos))
				return null;
			return RootNode.FindNodeIncludingPosition (pos);
		}
		public T FindNodeIncludingPosition<T> (int pos) {
			if (RootNode == null)
				return default;
			if (!RootNode.Contains (pos))
				return default;
			return RootNode.FindNodeIncludingPosition<T> (pos);
		}
		protected override void reloadFromFile () {
			base.reloadFromFile ();
			parse ();
		}
		protected override void apply(TextChange change)
		{
			base.apply(change);
			parse ();
		}

		public virtual Crow.Color GetColorForToken (TokenType tokType) {
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
		public abstract IList GetSuggestions (int pos);

		/// <summary>
		/// complete current token with selected item from the suggestion overlay.
		/// It may set a new position or a new selection.
		/// </summary>
		/// <param name="suggestion">selected object of suggestion overlay</param>
		/// <param name="newSelection">new position or selection, null if normal position after text changes</param>
		/// <returns>the TextChange to apply to the source</returns>
		public abstract TextChange? GetCompletionForCurrentToken (object suggestion, out TextSpan? newSelection);
		void parse () {
			Tokenizer tokenizer = CreateTokenizer ();
			tokens = tokenizer.Tokenize (Source);

			SyntaxAnalyser syntaxAnalyser = CreateSyntaxAnalyser ();
			Stopwatch sw = Stopwatch.StartNew ();
			syntaxAnalyser.Process ();
			sw.Stop();
			RootNode = syntaxAnalyser.Root;

			Console.WriteLine ($"Syntax Analysis done in {sw.ElapsedMilliseconds}(ms) {sw.ElapsedTicks}(ticks)");
			foreach (SyntaxException ex in syntaxAnalyser.Exceptions)
				Console.WriteLine ($"{ex}");

				/*foreach (Token t in Tokens)
					Console.WriteLine ($"{t,-40} {Source.AsSpan(t.Start, t.Length).ToString()}");
				syntaxAnalyser.Root.Dump();*/
		}

	}
}