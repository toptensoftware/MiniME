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
	// Represents a call to a method or global function.
	class ExprNodeCall : ExprNode
	{
		// Constructor
		public ExprNodeCall(Bookmark bookmark, ExprNode lhs) : base(bookmark)
		{
			Lhs = lhs;
		}

		// Attributes
		public ExprNode Lhs;
		public List<ExprNode> Arguments = new List<ExprNode>();

		public override void Dump(int indent)
		{
			writeLine(indent, "Call");
			Lhs.Dump(indent + 1);
			writeLine(indent, "with args:");
			foreach (var a in Arguments)
			{
				a.Dump(indent + 1);
			}
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.terminal;
		}

		public override bool Render(RenderContext dest)
		{
			WrapAndRender(dest, Lhs, false);
			dest.Append("(");
			bool first = true;
			foreach (var a in Arguments)
			{
				if (!first)
					dest.Append(",");
				a.Render(dest);
				first = false;
			}
			dest.Append(")");
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Lhs.Visit(visitor);
			foreach (var c in Arguments)
				c.Visit(visitor);
		}

		public override ExprNode Simplify()
		{
			Lhs = Lhs.Simplify();
			SimplifyList(Arguments);
			return this;
		}

		public override bool HasSideEffects()
		{
			return true;
		}

	}

}
