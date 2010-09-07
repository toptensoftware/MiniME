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
	// Represents a condiditional expression eg: conditions ? true : false
	class ExprNodeTernary : ExprNode
	{
		// Constructor
		public ExprNodeTernary(Bookmark bookmark, ExprNode condition) : base(bookmark)
		{
			Condition = condition;
		}

		// Attributes
		public ExprNode Condition;
		public ExprNode TrueResult;
		public ExprNode FalseResult;

		public override void Dump(int indent)
		{
			writeLine(indent, "if:");
			Condition.Dump(indent + 1);
			writeLine(indent, "then:");
			TrueResult.Dump(indent + 1);
			writeLine(indent, "else:");
			FalseResult.Dump(indent + 1);
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.conditional;
		}

		public override bool Render(RenderContext dest)
		{
			WrapAndRender(dest, Condition, false);
			dest.Append('?');
			WrapAndRender(dest, TrueResult, false);
			dest.Append(':');
			WrapAndRender(dest, FalseResult, false);
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Condition.Visit(visitor);
			TrueResult.Visit(visitor);
			FalseResult.Visit(visitor);
		}


		public override ExprNode Simplify()
		{
			Condition = Condition.Simplify();
			TrueResult = TrueResult.Simplify();
			FalseResult = FalseResult.Simplify();
			return this;
		}

		public override bool HasSideEffects()
		{
			return TrueResult.HasSideEffects() || FalseResult.HasSideEffects();
		}

	}
}
