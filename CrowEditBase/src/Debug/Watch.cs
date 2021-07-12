// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Diagnostics;
using Crow;

namespace CrowEditBase
{
	[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
	public abstract class Watch : CrowEditComponent {

		Debugger dbg;
		bool isExpanded;
		string name;
		string expression;
		string value;
		bool isEditable;
		int numChild;
		string type;
		int threadId;

		ObservableList<Watch> children = new ObservableList<Watch>();

		public CommandGroup Commands => new CommandGroup (
			new Command ("Update Value", () => UpdateValue()),
			new Command ("Delete", () => Delete())
		);

		public bool HasChildren => NumChild > 0;

		public bool IsExpanded {
			get => isExpanded;
			set {
				if (isExpanded == value)
					return;
				isExpanded = value;
				NotifyValueChanged(isExpanded);

				if (isExpanded)
					onExpand();
			}
		}
		protected abstract void onExpand();

		public ObservableList<Watch> Children {
			get => children;
			set {
				if (children == value)
					return;
				children = value;
				NotifyValueChanged (children);				
			}
		}
		public string Name {
			get => name;
			set {
				if (name == value)
					return;
				name = value;
				NotifyValueChanged(name);
			}
		}
		public string Expression {
			get => expression;
			set {
				if (expression == value)
					return;
				expression = value;
				NotifyValueChanged(expression);
			}
		}
		public string Value {
			get => value;
			set {
				if (this.value == value)
					return;
				this.value = value;
				NotifyValueChanged(this.value);
			}
		}
		public bool IsEditable {
			get => isEditable;
			set {
				if (isEditable == value)
					return;
				isEditable = value;
				NotifyValueChanged(isEditable);
			}
		}
		public int NumChild {
			get => numChild;
			set {
				if (numChild == value)
					return;
				numChild = value;
				NotifyValueChanged(numChild);
				NotifyValueChanged ("HasChildren", HasChildren);
			}
		}
		public string Type {
			get => type;
			set {
				if (type == value)
					return;
				type = value;
				NotifyValueChanged(type);
			}
		}
		public int ThreadId {
			get => threadId;
			set {
				if (threadId == value)
					return;
				threadId = value;
				NotifyValueChanged(threadId);
			}
		}

		public abstract void Create();
		public abstract void Delete();
		public abstract void UpdateValue ();


		public override string ToString() => $"{Name}:{Expression} = {Value} [{Type}]";
		string GetDebuggerDisplay() => ToString();
	}
}
