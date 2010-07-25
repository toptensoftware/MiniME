using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class StatementDoWhile : Statement
	{
		public StatementDoWhile()
		{
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "do:");
			Code.Dump(indent + 1);
			writeLine(indent, "while:");
			Condition.Dump(indent + 1);
		}
		public override bool Render(StringBuilder dest)
		{
			dest.Append("do ");
			Code.Render(dest);
			dest.Append(" while(");
			Condition.Render(dest);
			dest.Append(')');
			return false;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Condition.Visit(visitor);
			Code.Visit(visitor);
		}



		public ExpressionNode Condition;
		public Statement Code;
	}
}
