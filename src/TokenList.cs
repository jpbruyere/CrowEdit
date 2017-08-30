using System;
using System.Collections.Generic;

namespace Crow.Coding
{
	public class TokenList : List<Token>
	{
		public bool Dirty = true;
		public int EndingState;

		public TokenList () : base ()
		{
			EndingState = 0;
		}

		public new void Clear() {
			EndingState = 0;
			Dirty = true;
			base.Clear ();
		}
	}
}

