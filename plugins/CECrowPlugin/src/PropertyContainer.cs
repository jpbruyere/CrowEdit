// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using CrowEditBase;
using Drawing2D;
using Crow;

using static CrowEditBase.CrowEditBase;
using Crow.Text;

namespace CECrowPlugin
{
	[DebuggerDisplay ("{Type}:{Name}")]
	public class PropertyContainer : CrowEditComponent
	{
		ForeignWidgetContainer host;
		PropertyInfo pi;

		Command cmdReset, cmdGoToStyle;
		public CommandGroup Commands;

		#region CTOR
		public PropertyContainer(ForeignWidgetContainer host, PropertyInfo prop){
			this.host = host;
			pi = prop;

			cmdReset = new ActionCommand ("Reset to default", Reset, "", HasStyling | IsSetByIML);
			cmdGoToStyle = new ActionCommand ("Goto style", GotoStyle, "#icons.edit.svg", HasStyling);

			Commands = new CommandGroup (cmdReset, cmdGoToStyle);
		}
		#endregion

		public string DesignCategory {
			get {
				DesignCategory dca = (DesignCategory)pi.GetCustomAttribute (typeof(DesignCategory));
				return dca == null ? "Divers" : dca.Name;					
			}
		}
		public string Name => pi.Name;
		public object Value {
			get => pi.GetValue(host.Instance);
			set {
				
					
			}
		}
		/// <summary>
		/// for style attribute which is a string, return Style as type
		/// </summary>
		public string Type => pi.PropertyType.IsEnum ? "System.Enum"
					: pi.Name == "Style" ? "Style" : pi.PropertyType.FullName;
		
		public object[] Choices {
			get {
				return pi.PropertyType.IsEnum ?
					Enum.GetValues (pi.PropertyType).Cast<object>().ToArray() : null;
			}
		}

		public object DefaultValue => ((DefaultValueAttribute)pi.GetCustomAttribute (typeof (DefaultValueAttribute))).Value;
		public bool HasDefaultValue => pi.GetCustomAttribute (typeof(DefaultValueAttribute))!=null; 
		/// <summary>
		/// return true if current value comes from IML attributes
		/// </summary>
		public bool IsSetByIML => host.ImlValues.ContainsKey (Name);
		/// <summary>
		/// return true if member default value comes from style
		/// </summary>
		public bool HasStyling => host.StyleLocation.ContainsKey(Name);
		/// <summary>
		/// Return true if current value comes from styling
		/// </summary>
		public bool IsSetByStyling => IsSetByIML ? false : HasStyling;


		public Color LabForeground => IsSetByIML ? Colors.Blue : HasStyling ? Colors.Black : Colors.Silver;

		/// <summary>
		/// reset to default value
		/// </summary>
		public void Reset () {
			/*Widget inst = imlProjItem.SelectedItem as Widget;
			if (!inst.design_iml_values.ContainsKey (Name))
				return;
			inst.design_iml_values.Remove (Name);

			NotifyValueChanged ("LabForeground", LabForeground);
			imlProjItem.UpdateSource(this, imlProjItem.Instance.GetIML());*/
			//mview.ProjectNode.Instance.design_HasChanged = true;
			//should reinstantiate to get default
		}
		public void GotoStyle(){
			if (!HasStyling)
				return;
			FileLocation fl = host.StyleLocation[Name];

			CrowService srv = App.GetService<CrowService> ();
			if (srv?.CurrentSolution == null)
				return;
			if (srv.CurrentSolution.TryGetFile(fl.FilePath, out IFileNode node)) {
				if (App.OpenFile(node.FullPath) is TextDocument doc) {
					doc.IsSelected = true;
					doc.SetLocation(new CharLocation(fl.Line, fl.Column));
				}
			}
			

/*			if (!mview.ProjectNode.Project.TryGetProjectFileFromPath ("#" + fl.FilePath, out pf))
				return;

			if (!pf.IsOpened)
				pf.Open ();

			pf.CurrentLine = fl.Line;
			pf.CurrentColumn = fl.Column;

			pf.IsSelected = true;*/

		}

		public override string ToString () => $"{Name} = {Value}";
	}
}

