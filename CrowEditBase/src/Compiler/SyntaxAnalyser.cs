// Copyright (c) 2021-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;

namespace CrowEditBase
{
	public class SyntaxException : Exception {
		public readonly Token Token;
		public SyntaxException(string message, Token token = default, Exception innerException = null)
				: base (message, innerException) {
			Token = token;
		}
	}
	public abstract class SyntaxAnalyser {
		protected SourceDocument source;
		public abstract SyntaxNode Root { get; }
		public List<SyntaxException> Exceptions { get; protected set; }
		public SyntaxAnalyser (SourceDocument source) {
			this.source = source;
		}
		public abstract void Process ();
	}
}