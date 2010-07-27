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
				if (c.Code != null)
				{
					c.Code.Dump(indent + 1);
				}
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
			bool bNeedSemicolon=false;
			foreach (var c in Cases)
			{
				if (bNeedSemicolon)
					dest.Append(';');

				dest.StartLine();
				if (c.Value != null)
				{
					dest.Append("case ");
					c.Value.Render(dest);
					dest.Append(":");
				}
				else
				{
					dest.Append("default:");
				}
				if (c.Code != null)
				{
					bNeedSemicolon = c.Code.RenderIndented(dest);
				}
				else
				{
					bNeedSemicolon = false;
				}
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
				if (c.Code!=null)
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
			public StatementBlock Code;
		}

		public ExpressionNode TestExpression;
		public List<Case> Cases = new List<Case>();
	}

	
}

