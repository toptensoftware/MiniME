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
	// Represents a comma separated composite expression
	class ExprNodeComposite : ExprNode
	{
		// Constrictor
		public ExprNodeComposite(Bookmark bookmark) : base(bookmark)
		{
		}

		// Attributes
		public List<ExprNode> Expressions = new List<ExprNode>();

		public override void Dump(int indent)
		{
			writeLine(indent, "composite expression:");
			foreach (var e in Expressions)
			{
				e.Dump(indent + 1);
			}
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.comma;
		}

		public override bool Render(RenderContext dest)
		{
			for (int i = 0; i < Expressions.Count; i++)
			{
				if (i > 0)
					dest.Append(',');

				WrapAndRender(dest, Expressions[i], false);
			}
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var e in Expressions)
			{
				e.Visit(visitor);
			}
		}


		public override ExprNode Simplify()
		{
			SimplifyList(Expressions);
			return this;
		}

		public override bool HasSideEffects()
		{
			foreach (var n in Expressions)
			{
				if (!n.HasSideEffects())
					return false;
			}
			return true;
		}
	}
}
