// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

namespace CrowEditBase
{
	public abstract class Tokenizer {

		public Tokenizer  () {}
		public abstract Token[] Tokenize (string source);
	}
}
