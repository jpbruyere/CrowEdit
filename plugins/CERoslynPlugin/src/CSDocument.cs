// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Linq;
using Crow.Text;
using System.Collections.Generic;
using System.Diagnostics;
using Crow;
using IML = Crow.IML;
using System.Collections;
using System.Reflection;
using CrowEditBase;

//using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static CrowEditBase.CrowEditBase;

namespace CERoslynPlugin
{
	/*public static class Extensions {
		public static CSTokenType GetTokenType (this Token tok) => (XmlTokenType)tok.Type;
		public static void SetTokenType (this Token tok, CSTokenType type) => tok.Type = (TokenType)type;
	}*/
	public class CSDocument : TextDocument {
		
		static CSDocument () {
			App.GetService<RoslynService> ()?.Start ();
		}
		CSharpSyntaxTree tree;
		
		public CSDocument (string fullPath)	: base (fullPath) {

			//tree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText (Source, CSharpParseOptions.Default);			
		}

		/*ProjectCollection tree;
		public CSDocument (string fullPath)	: base (fullPath) {
			tree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText (Source, CSharpParseOptions.Default);			
		}*/
		
	}	
}