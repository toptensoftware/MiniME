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
		public StatementExpression(ExpressionNode expression)
		{
			Expression = expression;
		}

		// Attributes
		public ExpressionNode Expression;

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



	}
}
