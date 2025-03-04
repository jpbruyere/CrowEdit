// Copyright (c) 2013-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using CrowEditBase;
using Crow;

using static CrowEditBase.CrowEditBase;

namespace CECrowPlugin
{
	public class ForeignWidgetContainer : CrowEditComponent {
		internal static Type typeWidget;//, typeGroup, typeContainer, typeTemplatedContainer, typeTemplatedGroup;
		//design mode members, present only if crow compiled with DESIGN_MODE enabled
		internal static FieldInfo fiWidget_design_id, fiWidget_design_style_values,	fiWidget_design_iml_values, fiWidget_design_style_locations;
		Func<string> delGetName;

		bool isExpanded;
		Type type;
		object instance;
		public ForeignWidgetContainer(Type widgetType, object instance) {
			type = widgetType;
			this.instance = instance;

			delGetName = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), instance, type.GetProperty("Name").GetGetMethod());
		}

		public string Icon => $"#icons.{type.FullName}.svg";

		public IEnumerable<MemberInfo> Members => type.GetMembers (BindingFlags.Public | BindingFlags.Instance).
				Where (m=>((m is PropertyInfo pi && pi.CanWrite) || (m is EventInfo)) &&
						m.GetCustomAttribute<XmlIgnoreAttribute>() == null);

		public IEnumerable<PropertyContainer> Properties => Members.Where(m=>m.MemberType == MemberTypes.Property).Select(p=> new PropertyContainer(this, p as PropertyInfo));

		public string Name => delGetName();

		public IEnumerable<ForeignWidgetContainer> Children  {
			get {
				var srv = App.GetService<CrowService> ();
				if (srv == null || !srv.IsRunning)
					return null;
				return srv.GetWidgetChilren(instance)?.Select(c=>new ForeignWidgetContainer(c.GetType(),c));
			}
			
		} 
			
				
		
		public object Instance => instance;

		public Dictionary<string,string> ImlValues => fiWidget_design_iml_values.GetValue(instance) as Dictionary<string,string>;
		public Dictionary<string,string> StyleValues => fiWidget_design_style_values.GetValue(instance) as Dictionary<string,string>;
		public Dictionary<string,FileLocation> StyleLocation => fiWidget_design_style_locations.GetValue(instance) as Dictionary<string,FileLocation>;

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

	}
}
