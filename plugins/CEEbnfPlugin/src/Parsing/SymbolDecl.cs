// Copyright (c) 2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System.Collections.Generic;
using CrowEditBase;

namespace CrowEdit.Ebnf
{
	public class SymbolDecl {
		public readonly string Name;
		public Expression Expression;
		public SymbolDecl(string name) {
			Name = name;
		}
		
        public override string ToString() => $"{Name} ::= {Expression.ToString()}";

		public IEnumerable<Expression> FlattenedExpressions {
			get {
				foreach (Expression e in Expression.Flatten)
					yield return e;
			}
		}

		/*public virtual void SortChilds ()
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
		}*/
	}
}