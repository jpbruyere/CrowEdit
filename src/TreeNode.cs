// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Crow;

namespace CrowEditBase
{
	public abstract class TreeNode : IValueChange, ISelectable
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;

        public virtual void NotifyValueChanged (string MemberName, object _value)
		{
			ValueChanged.Raise (this, new ValueChangeEventArgs (MemberName, _value));
		}
		#endregion

		#region ISelectable implementation
		public event EventHandler Selected;
		public event EventHandler Unselected;
		public virtual bool IsSelected {
			get { return isSelected; }
			set {
				if (value == isSelected)
					return;

				Console.WriteLine ($"TreeNode({this}).IsSelected: {isSelected} -> {value}");

				isSelected = value;

				NotifyValueChanged ("IsSelected", isSelected);
			}
		}
		#endregion

		ObservableList<TreeNode> childs = new ObservableList<TreeNode> ();
		
		public TreeNode () {}
		public TreeNode (TreeNode parent) {
			Parent?.AddChild (this);
		}
		
		protected bool isSelected, isExpanded;

		public TreeNode Parent { get; protected set; }

		public abstract string Name { get; }
		public ObservableList<TreeNode> Childs {
			get => childs;
			set {
				if (childs == value)
					return;
				childs = value;
				NotifyValueChanged ("Childs", childs);
			}
		}
		public abstract CommandGroup Commands { get; }

		public void AddChild (TreeNode pn)
		{
			childs.Add (pn);
			pn.Parent = this;
		}
		public void RemoveChild (TreeNode pn)
		{
			pn.Parent = null;
			childs.Remove (pn);
		}

		public virtual bool IsExpanded {
			get { return isExpanded; }
			set {
				if (value == isExpanded)
					return;
				isExpanded = value;
				NotifyValueChanged ("IsExpanded", isExpanded);
				NotifyValueChanged ("IconSub", IconSub);
			}
		}
		public virtual Picture Icon => new SvgPicture ("#Icons.Question.svg");
		public virtual string IconSub => null;
		
		public override string ToString () => Name;

		public IEnumerable<TreeNode> Flatten {
			get {
				yield return this;
				foreach (var node in childs.SelectMany (child => child.Flatten))
					yield return node;
			}
		}

		public virtual void SortChilds ()
		{
			foreach (TreeNode pn in Childs)
				pn.SortChilds ();
			Childs = new ObservableList<TreeNode> (Childs.OrderBy (c => c, new NodeComparer()));
		}

		public class NodeComparer : IComparer<TreeNode>
		{
			public int Compare (TreeNode x, TreeNode y)
			{
				return string.Compare (x.Name, y.Name);
			}
		}
	}


}
