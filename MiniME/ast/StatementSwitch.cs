using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class StatementSwitch : Statement
	{
		public StatementSwitch(ExpressionNode testExpression)
		{
			TestExpression = testExpression;
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "switch on:");
			TestExpression.Dump(indent + 1);

			foreach (var c in Cases)
			{
				if (c.Value != null)
				{
					writeLine(indent, "case:");
					c.Value.Dump(indent + 1);
				}
				else
				{
					writeLine(indent, "default:");
				}
				writeLine(indent, "do:");
				c.Code.Dump(indent + 1);
			}
		}

		public override bool Render(RenderContext dest)
		{
			dest.Append("switch(");
			TestExpression.Render(dest);
			dest.Append(')');
			dest.StartLine();
			dest.Append('{');
			dest.Indent();
			dest.StartLine();
			foreach (var c in Cases)
			{
				if (c.Value != null)
				{
					dest.Append("case");
					c.Value.Render(dest);
					dest.Append(":");
				}
				else
				{
					dest.Append("default:");
				}
				dest.Indent();
				dest.StartLine();
				c.Code.Render(dest);
				dest.Unindent();
			}
			dest.Unindent();
			dest.StartLine();
			dest.Append("}");
			return false;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			TestExpression.Visit(visitor);
			foreach (var c in Cases)
			{
				if (c.Value!=null)
					c.Value.Visit(visitor);
				c.Code.Visit(visitor);
			}
		}



		public Case AddCase(ExpressionNode Value)
		{
			var c=new Case(Value);
			Cases.Add(c);
			return c;
		}

		public class Case
		{
			public Case(ExpressionNode value)
			{
				Value = value;
			}
			public ExpressionNode Value;
			public StatementBlock Code = new StatementBlock();
		}

		public ExpressionNode TestExpression;
		public List<Case> Cases = new List<Case>();
	}

	
}
