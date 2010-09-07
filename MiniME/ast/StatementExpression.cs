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
	// Wraps an expression as a statement
	class StatementExpression : Statement
	{
		// Constructor
		public StatementExpression(Bookmark bookmark, Expression expression) : base(bookmark)
		{
			Expression = expression;
		}

		// Attributes
		public Expression Expression;

		public override void Dump(int indent)
		{
			writeLine(indent, "Expression:");
			Expression.Dump(indent + 1);
		}
		public override bool Render(RenderContext dest)
		{
			if (!Expression.RootNode.HasSideEffects())
			{
				dest.Compiler.RecordWarning(Bookmark, "expression has no side effects");
			}

			return Expression.Render(dest);
		}
		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Expression.Visit(visitor);
		}

		public override bool IsDeclarationOnly()
		{
			if ((Expression.RootNode as ExprNodeFunction)!=null)
				return true;
			else
				return false;
		}


	}
}
