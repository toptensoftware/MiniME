using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class StatementReturnThrow : Statement
	{
		public StatementReturnThrow(Token op)
		{
			Op = op;
		}

		public StatementReturnThrow(Token op, ExpressionNode value)
		{
			Op = op;
			Value = value;
		}

		public override void Dump(int indent)
		{
			writeLine(indent, Op.ToString());
			if (Value != null)
			{
				Value.Dump(indent + 1);
			}
		}

		public override bool Render(RenderContext dest)
		{
			if (Value == null)
			{
				dest.Append(Op.ToString().Substring(3));
				return true;
			}

	
			dest.DisableLineBreaks();
			dest.Append(Op.ToString().Substring(3));
			dest.Append(' ');
			dest.EnableLineBreaksAfterNextWrite();
			Value.Render(dest);
			return true;
		}
		public override void OnVisitChildNodes(IVisitor visitor)
		{
			if (Value!=null)
				Value.Visit(visitor);
		}



		public Token Op;
		public ExpressionNode Value;
	}
}
