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
using static CrowEditBase.CrowEditBase;

using CrowEdit.Xml;

using AttributeSyntax = CrowEdit.Xml.AttributeSyntax;
using Drawing2D;

namespace CECrowPlugin
{
	public class ImlDocument : XmlDocument {


		public ImlDocument (string fullPath, string editorPath) : base (fullPath, editorPath) {
			App.GetService<CrowService> ()?.Start ();

			/*if (project is MSBuildProject msbp) {
				if (msbp.IsCrowProject)
			}*/
		}
		protected override Tokenizer CreateTokenizer() => new ImlTokenizer ();

		string[] allWidgetNames = typeof (Widget).Assembly.GetExportedTypes ().Where(t=>typeof(Widget).IsAssignableFrom (t))
					.Select (s => s.Name).ToArray ();


		IEnumerable<MemberInfo> getAllCrowTypeMembers (string crowTypeName) {
			Type crowType = IML.Instantiator.GetWidgetTypeFromName (crowTypeName);
			return crowType.GetMembers (BindingFlags.Public | BindingFlags.Instance).
				Where (m=>((m is PropertyInfo pi && pi.CanWrite) || (m is EventInfo)) &&
						m.GetCustomAttribute<XmlIgnoreAttribute>() == null);
		}
		MemberInfo getCrowTypeMember (string crowTypeName, string memberName) {
			Type crowType = IML.Instantiator.GetWidgetTypeFromName (crowTypeName);
			return crowType.GetMember (memberName, BindingFlags.Public | BindingFlags.Instance).FirstOrDefault ();
		}
		public override IList GetSuggestions (CharLocation loc) {
			if (tokens.Length == 0)
				return null;
			IList sugs = base.GetSuggestions (loc);
			if (sugs != null)
				return sugs;

#if DEBUG
			Console.WriteLine ($"Current Token: {CurrentTokenString} Current Node: {CurrentNode}");
#endif

			if (currentToken.GetTokenType() == XmlTokenType.ElementOpen)
				return new List<string> (allWidgetNames);
			if (currentToken.GetTokenType() == XmlTokenType.ElementName)
				return allWidgetNames.Where (s => s.StartsWith (currentToken.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
			if (currentToken.GetTokenType() == XmlTokenType.WhiteSpace && CurrentNode is ElementStartTagSyntax eltStartTag2)
				return getAllCrowTypeMembers (eltStartTag2.Name).ToList();
			if (CurrentNode is CrowEdit.Xml.AttributeSyntax attribNode) {
				if (CurrentNode.Parent is ElementTagSyntax eltTag) {
					if (!string.IsNullOrEmpty (eltTag.Name)) {
						if (currentToken.GetTokenType() == XmlTokenType.AttributeName) {
							return getAllCrowTypeMembers (eltTag.Name)
								.Where (s => s.Name.StartsWith (currentToken.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
						} else if (!string.IsNullOrEmpty (attribNode.Name)) {
							if (currentToken.GetTokenType() == XmlTokenType.AttributeValue) {
								MemberInfo mi = getCrowTypeMember (
									eltTag.Name, attribNode.Name);
								if (mi is PropertyInfo pi) {
									if (pi.Name == "Style")
										return App.Styling.Keys
											.Where (s => s.StartsWith (currentToken.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
									if (pi.PropertyType.IsEnum)
										return Enum.GetNames (pi.PropertyType)
											.Where (s => s.StartsWith (currentToken.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
									if (pi.PropertyType == typeof(bool))
										return  (new string[] {"true", "false"}).
											Where (s => s.StartsWith (currentToken.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
									if (pi.PropertyType == typeof (Measure))
										return (new string[] {"Stretched", "Fit"}).
											Where (s => s.StartsWith (currentToken.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
									if (pi.PropertyType == typeof (Fill))
										return  EnumsNET.Enums.GetValues<Colors> ()
											.Where (s => s.ToString().StartsWith (currentToken.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
								}
							} else if (currentToken.GetTokenType() == XmlTokenType.AttributeValueOpen) {
								MemberInfo mi = getCrowTypeMember (
									eltTag.Name, attribNode.Name);
								if (mi is PropertyInfo pi) {
									if (pi.Name == "Style")
										return App.Styling.Keys.ToList ();
									if (pi.PropertyType.IsEnum)
										return  Enum.GetNames (pi.PropertyType).ToList ();
									if (pi.PropertyType == typeof(bool))
										return  new List<string> (new string[] {"true", "false"});
									if (pi.PropertyType == typeof (Fill))
										return  EnumsNET.Enums.GetValues<Colors> ().ToList ();
									if (pi.PropertyType == typeof (Measure))
										return  new List<string> (new string[] {"Stretched", "Fit"});
								}
							}
						}
					}
				}
			} else if (currentToken.GetTokenType() != XmlTokenType.AttributeValueClose &&
					currentToken.GetTokenType() != XmlTokenType.EmptyElementClosing &&
					currentToken.GetTokenType() != XmlTokenType.ClosingSign &&
					CurrentNode is ElementStartTagSyntax eltStartTag) {
				if (currentToken.GetTokenType() == XmlTokenType.AttributeName)
					return getAllCrowTypeMembers (eltStartTag.Name)
						.Where (s => s.Name.StartsWith (currentToken.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
				//else if (currentToken.Type == TokenType.ElementName)
				//	Suggestions = getAllCrowTypeMembers (eltStartTag.NameToken.Value.AsString (Source)).ToList ();
			} else {
			}
			return null;
		}
		public override bool TryGetCompletionForCurrentToken (object suggestion, out TextChange change, out TextSpan? newSelection) {
			return base.TryGetCompletionForCurrentToken (suggestion is MemberInfo mi ? mi.Name : suggestion, out change, out newSelection);
		}

		public override Color GetColorForToken(TokenType tokType)
		{
			return base.GetColorForToken (tokType);
		}
	}
}