using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class StatementWith : Statement
	{
		public StatementWith()
		{
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "with:");
			Expression.Dump(indent + 1);
			writeLine(indent, "do:");
			Code.Dump(indent + 1);
		}

		public override bool Render(RenderContext dest)
		{
			dest.Append("with(");
			Expression.Render(dest);
			dest.Append(")");
			return Code.RenderIndented(dest);
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Expression.Visit(visitor);
			Code.Visit(visitor);
		}



		public ExpressionNode Expression;
		public Statement Code;
	}
}
