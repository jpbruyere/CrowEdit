// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Diagnostics;
//using static Crow.Coding.NetcoredbgDebugger;
using Crow;

namespace NetcoreDbgPlugin
{	
	public class Watch : CrowEditBase.Watch {
		static int curId;

		NetcoredbgDebugger dbg;
	
		protected override void onExpand() {
			if (HasChildren && Children.Count == 0)
				dbg.WatchChildrenRequest (this);
		}


		public override void Create()
		{
		}
		public override void Delete()
		{			
			dbg.CreateNewRequest (new NetcoredbgDebugger.Request<Watch> (this, $"-var-delete {Name}"));
			dbg.Watches.Remove (this);
		}
		public override void UpdateValue () {
			string strThread = dbg.CurrentThread == null ? "" : $"--thread {dbg.CurrentThread.Id}";
			string strLevel = dbg.CurrentFrame == null ? "" : $"--frame {dbg.CurrentFrame.Level}";
			dbg.CreateNewRequest (new NetcoredbgDebugger.Request<Watch> (this, $"-var-evaluate-expression {Name} {strThread} {strLevel}"));
			foreach (Watch w in Children)
				w.UpdateValue ();
		}
		public Watch(NetcoredbgDebugger debugger, string expression)
		{
			dbg = debugger;
			Name = $"watch_{curId++}";
			Expression = expression;
		}
		public Watch(NetcoredbgDebugger debugger, MITupple variable)
		{
			dbg = debugger;
			Update (variable);
		}
		public void Update (MITupple variable)
		{
			Name = variable.GetAttributeValue("name");
			Expression = variable.GetAttributeValue("exp");
			Value = variable.GetAttributeValue("value");
			IsEditable = variable.GetAttributeValue("attributes") == "editable";
			Type = variable.GetAttributeValue("type");
			NumChild = int.Parse(variable.GetAttributeValue("numchild"));
			ThreadId = int.Parse(variable.GetAttributeValue("thread-id"));
			NotifyValueChanged ("HasChildren", HasChildren);
		}
	}
}
