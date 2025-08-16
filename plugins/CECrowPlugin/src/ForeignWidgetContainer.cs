// Copyright (c) 2013-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using CrowEditBase;
using Crow;
using Drawing2D;

using static CrowEditBase.CrowEditBase;
using System.Diagnostics;


namespace CECrowPlugin
{
	public class ForeignWidgetContainer : CrowEditComponent {
		internal static Type typeWidget;//, typeGroup, typeContainer, typeTemplatedContainer, typeTemplatedGroup;
		//design mode members, present only if crow compiled with DESIGN_MODE enabled
		internal static FieldInfo fiWidget_design_id,
									fiWidget_design_style_values, fiWidget_design_iml_values,
									fiWidget_design_style_locations, fiWidget_design_iml_locations,
									fiWidget_design_line, fiWidget_design_column, fiWidget_design_imlPath,
									fiWidget_slot;
		Func<string> delGetName;
		Func<Rectangle,Rectangle> delGetScreenCoordinates;

		bool isExpanded;
		Type type;
		object instance;
		ForeignWidgetContainer parent;
		string designId, designImlPath;
		int designLine, designColumn;
		CrowService srv;
		public ForeignWidgetContainer(Type widgetType, object instance, ForeignWidgetContainer parent = null) {
			type = widgetType;
			this.instance = instance;
			this.parent = parent;

			delGetName = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), instance, type.GetProperty("Name").GetGetMethod());
			delGetScreenCoordinates = (Func<Rectangle,Rectangle>)Delegate.CreateDelegate(typeof(Func<Rectangle,Rectangle>), instance, type.GetMethod("ScreenCoordinates"));

			designId = (string)fiWidget_design_id?.GetValue(instance);
			designLine = (int)fiWidget_design_line?.GetValue(instance);
			designColumn = (int)fiWidget_design_column?.GetValue(instance);
			designImlPath = (string)fiWidget_design_imlPath?.GetValue(instance);

			//onsole.WriteLine($"new ForeignWidgetContainer: {this} {parent} {designImlPath}");
			srv = App.GetService<CrowService>();
		}


		public IEnumerable<MemberInfo> Members => type.GetMembers (BindingFlags.Public | BindingFlags.Instance).
				Where (m=>((m is PropertyInfo pi && pi.CanWrite) || (m is EventInfo)) &&
						m.GetCustomAttribute<XmlIgnoreAttribute>() == null);

		public IEnumerable<CategoryContainer> Properties {
			get {
				if (!srv.crowTypesMembersCache.ContainsKey(type.FullName)) {
					srv.crowTypesMembersCache.Add (type.FullName, Members.Where(m=>m.MemberType == MemberTypes.Property)
						.Select(p=> new PropertyContainer(this, p as PropertyInfo)).GroupBy(pc=>pc.DesignCategory).Select(g=>new CategoryContainer(g.Key, g.AsEnumerable())));
				}
				return srv.crowTypesMembersCache[type.FullName];
			}
		}

		public string Icon => $"#icons.{type.FullName}.svg";
		public string Name => delGetName();
		public string DesignId => designId;
		public string DesignPath => designImlPath;
		public int DesignLine => designLine;
		public int DesignColumn => designColumn;
		public Rectangle GetScreenCoordinate() => delGetScreenCoordinates(Slot);
		public Rectangle Slot => (Rectangle)fiWidget_slot?.GetValue(instance);

		public string TypeName => type.FullName;

		volatile bool childrenFetched = false;
		IList<ForeignWidgetContainer> children;
		public IList<ForeignWidgetContainer> Children  {
			get {
				var srv = App.GetService<CrowService> ();
				if (srv == null || !srv.IsRunning)
					return null;
				if (!childrenFetched) {
					children = srv.GetWidgetChilren(instance)?.Select(c => new ForeignWidgetContainer(c.GetType(),c,this)).ToList();
					childrenFetched = true;
				}
					
				return children;
			}			
		} 
		public bool TryFindWidgetById (string designId, out ForeignWidgetContainer widgetContainer) {
			widgetContainer = null;
			if (designId == DesignId) {
				widgetContainer = this;
				return true;
			}
			foreach (var child in Children) {
				if (child.TryFindWidgetById(designId, out ForeignWidgetContainer childContainer)) {
					widgetContainer = childContainer;
					return true;
				}
			}
			return false;
		}

		
		public object Instance => instance;

		public Dictionary<string,string> ImlValues => fiWidget_design_iml_values.GetValue(instance) as Dictionary<string,string>;
		public Dictionary<string,string> StyleValues => fiWidget_design_style_values.GetValue(instance) as Dictionary<string,string>;
		public Dictionary<string,FileLocation> StyleLocation => fiWidget_design_style_locations.GetValue(instance) as Dictionary<string,FileLocation>;
		public Dictionary<string,FileLocation> ImlLocation => fiWidget_design_iml_locations.GetValue(instance) as Dictionary<string,FileLocation>;

		public virtual bool IsExpanded {
			get => isExpanded;
			set {
				if (value == isExpanded)
					return;
				isExpanded = value;
				NotifyValueChanged (isExpanded);
			}
		}
		public bool HasChildren => Children?.Count() > 0;		
		public void ExpandToTheTop() {
			ForeignWidgetContainer p = parent;
			while(p != null) {
				p.IsExpanded = true;
				p = p.parent;
			}
		}

        public override string ToString() => $"{DesignId}:{Name}";
    }
}
