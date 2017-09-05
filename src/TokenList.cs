using System;
using System.Collections.Generic;

namespace Crow.Coding
{
	public class TokenList : List<Token>
	{
		/// <summary> The dirty state indicate that this line has changed and should be reparsed </summary>
		public bool Dirty = true;
		/// <summary>
		/// The state of the parser when end of line was reached, used to setup initial state for next line parsing
		/// </summary>
		public int EndingState;
		/// <summary>
		/// Folding state reside here because it's the highest level of abstraction line per line
		/// </summary>
		public bool folded = false;
		public Node SyntacticNode = null;
		/// <summary>
		/// if parsing issue error, exception is not null and tokenlist should contains only one token with line content and type = unknown
		/// </summary>
		public ParsingException exception = null;

		public TokenList () : base ()
		{
			EndingState = 0;
		}
		/// <summary>
		/// Initializes an in  error source line
		/// </summary>
		public TokenList (ParsingException ex, string rawLineTxt){
			exception = ex;
			this.Add (new Token () { Content = rawLineTxt });
		}

		public int FirstNonBlankTokenIndex {
			get {
				for (int i = 0; i < this.Count; i++) {
					if (this [i].Type != Parser.TokenType.WhiteSpace && this [i].Type != Parser.TokenType.Unknown)
						return i;
				}
				return -1;
			}
		}

		//override list.clear to clear additional states of tokenList
		public new void Clear() {
			EndingState = 0;
			folded = false;
			SyntacticNode = null;
			exception = null;
			Dirty = true;
			base.Clear ();
		}
	}
}

