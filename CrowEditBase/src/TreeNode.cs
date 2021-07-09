// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using Crow;
using CrowEditBase;
using static CrowEditBase.CrowEditBase;

namespace CrowEditBase
{
	public enum NodeType {
		Unknown,
		ReferenceGroup,
		Reference,
		PackageReference,
		ProjectReference,
		VirtualGroup,
		Folder,
		None,
		Compile,
		EmbeddedResource,
	}	
	public abstract class TreeNode : CrowEditComponent
	{
		#region CTOR
		protected TreeNode () { }
		#endregion

		ObservableList<TreeNode> children = new ObservableList<TreeNode> ();		

		protected bool isSelected, isExpanded;

		public TreeNode Parent { get; private set; }

		public abstract string Caption { get; }
		public abstract NodeType NodeType { get; }
		public T GetRoot<T> () where T : TreeNode {
			TreeNode n = this;
			while (n.Parent != null)
				n = n.Parent;
			return (T)n;
		}

		public ObservableList<TreeNode> Childs {
			get => children;
			set {
				if (children == value)
					return;
				children = value;
				NotifyValueChanged (children);
			}
		}
		public virtual CommandGroup Commands => null;

		public void AddChild (TreeNode pn)
		{
			children.Add (pn);
			pn.Parent = this;
		}
		public void RemoveChild (TreeNode pn)
		{
			pn.Parent = null;
			children.Remove (pn);
		}

		public virtual bool IsExpanded {
			get { return isExpanded; }
			set {
				if (value == isExpanded)
					return;
				isExpanded = value;
				NotifyValueChanged (isExpanded);
				NotifyValueChanged ("IconSub", (object)IconSub);
			}
		}
		public bool HasChildren => children?.Count > 0;
		public abstract string Icon { get; }
		public virtual string IconSub => null;				

		public IEnumerable<TreeNode> Flatten {
			get {
				yield return this;
				foreach (var node in children.SelectMany (child => child.Flatten))
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
				int typeCompare = x.NodeType.CompareTo (y.NodeType);
				return typeCompare != 0 ? typeCompare : string.Compare (x.Caption, y.Caption);
			}
		}
	}


}
