// 
//   MiniME - http://www.toptensoftware.com/minime
// 
//   The contents of this file are subject to the license terms as 
//	 specified at the web address above.
//  
//   Software distributed under the License is distributed on an 
//   "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or
//   implied. See the License for the specific language governing
//   rights and limitations under the License.
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Operator precedence is used to determine how parentheses must
	// be generated when regenerating the Javascript code from the 
	// abstract syntax tree.
	enum OperatorPrecedence
	{
		comma,
		assignment,
		conditional,
		logor,
		logand,
		bitor,
		bitxor,
		bitand,
		equality,
		relational,
		bitshift,
		add,
		multiply,
		unary,
		function,
		terminal,
	}

	// Base class for all expression nodes
	abstract class ExprNode : Node
	{
		// Constructor
		public ExprNode(Bookmark bookmark) : base(bookmark)
		{

		}

		// Must be overridden in all node types to return the precedence
		public abstract OperatorPrecedence GetPrecedence();

		// Render an child node, wrapping it in parentheses if necessary
		public void WrapAndRender(RenderContext dest, ExprNode other, bool bWrapEqualPrecedence)
		{
			var precOther=other.GetPrecedence();
			var precThis = this.GetPrecedence();
			if (precOther < precThis || (precOther==precThis && bWrapEqualPrecedence))
			{
				dest.Append("(");
				other.Render(dest);
				dest.Append(")");
			}
			else
			{
				other.Render(dest);
			}
		}

		// Return the constant value of this expression, or null if can't
		public virtual object EvalConstLiteral()
		{
			return null;
		}

		// Override in expression nodes that can simplify themselves.
		public abstract ExprNode Simplify();

		// Simplify a list of nodes
		public static void SimplifyList(List<ExprNode> nodes)
		{
			// Simplify all nodes
			for (int i = 0; i < nodes.Count; i++)
			{
				var v = nodes[i];
				if (v != null)
				{
					var vNew = v.Simplify();
					if (v != vNew)
					{
						nodes.RemoveAt(i);
						nodes.Insert(i, vNew);
					}
				}
			}
		}

		public abstract bool HasSideEffects();
	}
}
