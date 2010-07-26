using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class StatementExpression : Statement
	{
		public StatementExpression(ExpressionNode expression)
		{
			Expression = expression;
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "Expression:");
			Expression.Dump(indent + 1);
		}
		public override bool Render(RenderContext dest)
		{
			return Expression.Render(dest);
		}
		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Expression.Visit(visitor);
		}


		public ExpressionNode Expression;

	}
}
