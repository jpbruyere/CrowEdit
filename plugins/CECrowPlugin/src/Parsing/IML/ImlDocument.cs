// Copyright (c) 2013-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Linq;
using Crow.Text;
using System.Collections.Generic;
using Crow;
using IML = Crow.IML;
using System.Reflection;
using CrowEditBase;
using static CrowEditBase.CrowEditBase;

using CrowEdit.Xml;
using Drawing2D;
using System.Diagnostics;

namespace CECrowPlugin
{
	public class ImlDocument : XmlDocument {


		public ImlDocument (string fullPath, string editorPath) : base (fullPath, editorPath) {
			App.GetService<CrowService> ()?.Start ();

			/*if (project is MSBuildProject msbp) {
				if (msbp.IsCrowProject)
			}*/
		}
		protected override SyntaxAnalyser CreateSyntaxAnalyser() => new ImlSyntaxAnalyser (this);
		public override string GetTokenTypeString (TokenType tokenType) => ((ImlTokenType)tokenType).ToString();


		IEnumerable<MemberInfo> getAllCrowTypeMembers (string crowTypeName) {
			Type crowType = App.GetService<CrowService>()?.GetWidgetTypeFromeName(crowTypeName);
			return crowType?.GetMembers (BindingFlags.Public | BindingFlags.Instance).
				Where (m=>((m is PropertyInfo pi && pi.CanWrite) || (m is EventInfo)) &&
						m.GetCustomAttribute<XmlIgnoreAttribute>() == null);
		}
		/*MemberInfo getCrowTypeMember (string crowTypeName, string memberName) {
			Type crowType = App.GetService<CrowService>()?.GetWidgetTypeFromeName(crowTypeName);
			return crowType.GetMember (memberName, BindingFlags.Public | BindingFlags.Instance).FirstOrDefault ();
		}*/

        protected override IEnumerable<Suggestion> getElementNameSuggestions(string curName, TextChange change)
        {
			CrowService srv = App.GetService<CrowService>();
			if (srv == null || !srv.IsRunning)
				return null;
			Type widgetType = srv.GetWidgetTypeFromeName("Widget");
			if (widgetType == null)
				return null;

			IEnumerable<Type> widgetTypes = widgetType.Assembly.GetExportedTypes ().Where(t=>
				widgetType.IsAssignableFrom (t) && !t.IsAbstract);
			int curNameLength = 0;
			if (!string.IsNullOrEmpty(curName)) {
				widgetTypes = widgetTypes.Where(t=>t.Name.StartsWith(curName, StringComparison.OrdinalIgnoreCase));
				curNameLength = curName.Length;
			}
			int endPosOffset = change.HasNewText ? -change.ChangedText.Length : 0;
            return widgetTypes.Select (t
				=> new WidgetSuggestion(t,
					new TextChange(change.Start, change.Length, t.Name + change.ChangedText), endPosOffset));
        }
        protected override IEnumerable<Suggestion> getAttributeNameSuggestions(string eltName, string curName, TextChange change) {
			int endPosOffset = change.HasNewText ? -1 : 0;
			var members = getAllCrowTypeMembers(eltName);
			if (members == null)
				return null;
			if (!string.IsNullOrEmpty(curName))
				members = members.Where(m =>
					m.Name.StartsWith (curName, StringComparison.OrdinalIgnoreCase));
			return members?.Select(m
				=> new MemberInfoSuggestion(m,
					new TextChange(change.Start, change.Length, m.Name + change.ChangedText), endPosOffset));
		}
		protected override IEnumerable<Suggestion> getAttributeValueSuggestions(string eltName, string attribName, string attribValue, TextChange change) {
			CrowService srv = App.GetService<CrowService>();
			if (srv == null || !srv.IsRunning)
				return null;
			MemberInfo mi = getAllCrowTypeMembers(eltName)?.Where(m=>m.Name.Equals(attribName, StringComparison.Ordinal)).FirstOrDefault();
			if (mi is PropertyInfo pi) {
				if (pi.Name == "Style")
					return App.Styling.Keys
						.Where (s => s.StartsWith (attribValue, StringComparison.OrdinalIgnoreCase))
						.Select(s=>new Suggestion(s,
							new TextChange(change.Start, change.Length, s + change.ChangedText)));
				if (pi.PropertyType.IsEnum)
					return Enum.GetNames (pi.PropertyType)
						.Where (s => s.StartsWith (attribValue, StringComparison.OrdinalIgnoreCase))
						.Select(s=>new Suggestion(s,
							new TextChange(change.Start, change.Length, s + change.ChangedText)));
				if (pi.PropertyType == typeof(bool))
					return  (new string[] {"true", "false"}).
						Where (s => s.StartsWith (attribValue, StringComparison.OrdinalIgnoreCase))
						.Select(s=>new Suggestion(s,
							new TextChange(change.Start, change.Length, s + change.ChangedText)));
				if (pi.PropertyType.Name == "Measure")
					return (new string[] {"Stretched", "Fit"}).
						Where (s => s.StartsWith (attribValue, StringComparison.OrdinalIgnoreCase))
						.Select(s=>new Suggestion(s,
							new TextChange(change.Start, change.Length, s + change.ChangedText)));
				if (pi.PropertyType.Name == "Fill")
					return  EnumsNET.Enums.GetValues<Colors> ()
						.Where (s => s.ToString().StartsWith (attribValue, StringComparison.OrdinalIgnoreCase))
						.Select(c=>new ColorSuggestion(c,
							new TextChange(change.Start, change.Length, c + change.ChangedText)));
			}
			return null;
		}
        /*public override IList GetSuggestions (int absoluteTextPos, int currentTokenIndex, SyntaxNode CurrentNode, CharLocation loc) {
			IList sugs = base.GetSuggestions (absoluteTextPos, currentTokenIndex, CurrentNode, loc);
			if (sugs != null)
				return sugs;

			
			Token tok = GetTokenByIndex(currentTokenIndex);

			if (tok.GetTokenType() == XmlTokenType.ElementOpen)
				return new List<string> (allWidgetNames);
			if (tok.GetTokenType() == XmlTokenType.ElementName)
				return allWidgetNames.Where (s => s.StartsWith (root.GetTokenString(tok), StringComparison.OrdinalIgnoreCase)).ToList ();
			if (tok.Type.HasFlag(TokenType.WhiteSpace) && CurrentNode.TryCast(out ElementTagSyntax ets)) {
				if (ets.name.HasValue)
					return getAllCrowTypeMembers (ets.Name).ToList();
				return null;
			}
						
			if (CurrentNode is CrowEdit.Xml.AttributeSyntax attribNode) {
				if (CurrentNode.Parent is ElementTagSyntax eltTag) {
					if (!string.IsNullOrEmpty (eltTag.Name)) {
						if (tok.GetTokenType() == XmlTokenType.AttributeName) {
							return getAllCrowTypeMembers (eltTag.Name)
								.Where (s => s.Name.StartsWith (root.GetTokenString(tok), StringComparison.OrdinalIgnoreCase)).ToList ();
						} else if (!string.IsNullOrEmpty (attribNode.Name)) {
							if (tok.GetTokenType() == XmlTokenType.AttributeValue) {
								MemberInfo mi = getCrowTypeMember (
									eltTag.Name, attribNode.Name);
								if (mi is PropertyInfo pi) {
									if (pi.Name == "Style")
										return App.Styling.Keys
											.Where (s => s.StartsWith (root.GetTokenString(tok), StringComparison.OrdinalIgnoreCase)).ToList ();
									if (pi.PropertyType.IsEnum)
										return Enum.GetNames (pi.PropertyType)
											.Where (s => s.StartsWith (root.GetTokenString(tok), StringComparison.OrdinalIgnoreCase)).ToList ();
									if (pi.PropertyType == typeof(bool))
										return  (new string[] {"true", "false"}).
											Where (s => s.StartsWith (root.GetTokenString(tok), StringComparison.OrdinalIgnoreCase)).ToList ();
									if (pi.PropertyType == typeof (Measure))
										return (new string[] {"Stretched", "Fit"}).
											Where (s => s.StartsWith (root.GetTokenString(tok), StringComparison.OrdinalIgnoreCase)).ToList ();
									if (pi.PropertyType == typeof (Fill))
										return  EnumsNET.Enums.GetValues<Colors> ()
											.Where (s => s.ToString().StartsWith (root.GetTokenString(tok), StringComparison.OrdinalIgnoreCase)).ToList ();
								}
							} else if (tok.GetTokenType() == XmlTokenType.AttributeValueOpen) {
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
			} *//*else if (tok.GetTokenType() != XmlTokenType.AttributeValueClose &&
					tok.GetTokenType() != XmlTokenType.EmptyElementClosing &&
					tok.GetTokenType() != XmlTokenType.ClosingSign &&
					CurrentNode is ElementStartTagSyntax eltStartTag) {
				if (tok.GetTokenType() == XmlTokenType.AttributeName)
					return getAllCrowTypeMembers (eltStartTag.Name)
						.Where (s => s.Name.StartsWith (tok.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
				//else if (tok.Type == TokenType.ElementName)
				//	Suggestions = getAllCrowTypeMembers (eltStartTag.NameToken.Value.AsString (Source)).ToList ();
			} else {
			}
			return null;
		}*/

		public override Color GetColorForToken(TokenType tokType)
		{
			switch ((ImlTokenType)tokType) {
				case ImlTokenType.BindingOpen:
				case ImlTokenType.BindingClose:
					return Colors.DarkGreen;
				case ImlTokenType.BindingName: return Colors.RoyalBlue;
				case ImlTokenType.BindingDot: 
				case ImlTokenType.BindingDoubleDot: 
				case ImlTokenType.BindingLevel: 
					return Colors.MediumVioletRed;
				case ImlTokenType.ConstantName:
				case ImlTokenType.ConstantRefOpen:
				case ImlTokenType.ConstantRefClose:
					return Colors.Brown;
			}
			return base.GetColorForToken (tokType);
		}
	}
}