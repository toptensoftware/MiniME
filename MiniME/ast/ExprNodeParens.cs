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
	// Represents a postfix increment or decrement
	class ExprNodeParens : ExprNode
	{
		// Constructor
		public ExprNodeParens(Bookmark bookmark, ExprNode inner) : base(bookmark)
		{
			Inner = inner;
		}

		// Attributes
		public ExprNode Inner;
		
		public override string ToString()
		{
			return String.Format("({0})-postfix-{0}", Inner.ToString());
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "parens");
			Inner.Dump(indent + 1);
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.terminal;
		}

		public override bool Render(RenderContext dest)
		{
			dest.Append("(");
			Inner.Render(dest);
			dest.Append(")");
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Inner.Visit(visitor);
		}

		public override ExprNode Simplify()
		{
			return Inner.Simplify();
		}

		public override bool HasSideEffects()
		{
			return Inner.HasSideEffects();
		}


	}
}
