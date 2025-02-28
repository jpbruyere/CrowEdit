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
using Drawing2D;

//using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static CrowEditBase.CrowEditBase;
using CrowEdit.Xml;

namespace CERoslynPlugin
{
	/*public static class Extensions {
		public static CSTokenType GetTokenType (this Token tok) => (XmlTokenType)tok.Type;
		public static void SetTokenType (this Token tok, CSTokenType type) => tok.Type = (TokenType)type;
	}*/
	public class CSDocument : SourceDocument {

		static CSDocument () {
			App.GetService<RoslynService> ()?.Start ();
		}

		CSharpSyntaxTree tree;
		public CSDocument (string fullPath, string editorPath)	: base (fullPath, editorPath) {

			tree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText (source.ToString(), CSharpParseOptions.Default);
			var root = tree.GetRoot();
			/*foreach (SyntaxKind v in Enum.GetValues<SyntaxKind>().OrderBy(k=>(uint)k)) {
				Console.WriteLine($"{v,50} {(((uint)v) ).ToString("B16") } {(((uint)v) ).ToString("X4") }");
			}*/
		}

		#region SourceDocument abstract class implementation
		protected override SyntaxAnalyser CreateSyntaxAnalyser() => new CSSyntaxAnalyser (this);

		public override IList GetSuggestions (int absoluteTextPos, int currentTokenIndex, SyntaxNode CurrentNode, CharLocation loc)
		{
			Token currentToken = GetTokenByIndex(currentTokenIndex);
			throw new NotImplementedException();
		}
		#endregion

		public override Color GetColorForToken (TokenType tokType) {
			uint rawkind = (uint)tokType;
			uint tokCat = rawkind & 0xFF;
			CSTokenType cat = (CSTokenType)tokCat;

			SyntaxKind k = (SyntaxKind)tokType;

			//Console.WriteLine($"{k,50} {(((uint)tokType) ).ToString("B16") } {cat}");
			
			switch (cat) {
				case CSTokenType.Trivia: return Colors.Grey;
				case CSTokenType.Keyword: return Colors.DarkSlateBlue;
				default: return Colors.Black;
			}
		}

	}
}