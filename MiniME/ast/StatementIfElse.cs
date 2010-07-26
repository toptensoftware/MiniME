using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class StatementIfElse : Statement
	{
		public StatementIfElse(ExpressionNode condition)
		{
			Condition = condition;
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "if condition:");
			Condition.Dump(indent + 1);
			writeLine(indent, "true:");
			TrueStatement.Dump(indent + 1);
			if (FalseStatement != null)
			{
				writeLine(indent, "else:");
				FalseStatement.Dump(indent + 1);
			}

		}

		public override bool Render(RenderContext dest)
		{
			dest.Append("if(");
			Condition.Render(dest);
			dest.Append(")");
			bool retv=TrueStatement.Render(dest);
			if (FalseStatement != null)
			{
				if (retv)
					dest.Append(';');

				dest.Append("else");
				if (FalseStatement.GetType() != typeof(StatementBlock))
					dest.Append(' ');
				retv=FalseStatement.Render(dest);
			}
			return retv;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Condition.Visit(visitor);
			TrueStatement.Visit(visitor);
			if (FalseStatement != null)
				FalseStatement.Visit(visitor);
		}

		public ExpressionNode Condition;
		public Statement TrueStatement;
		public Statement FalseStatement;
	}
}
