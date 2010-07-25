using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class StatementTryCatchFinally : Statement
	{
		public StatementTryCatchFinally()
		{
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "try:");
			Code.Dump(indent + 1);

			foreach (var cc in CatchClauses)
			{
				if (cc.Condition != null)
				{
					writeLine(indent, "catch `{0}` if:", cc.ExceptionVariable);
					cc.Condition.Dump(indent + 1);
					writeLine(indent, "do:");
				}
				else
				{
					writeLine(indent, "catch `{0}` do:", cc.ExceptionVariable);
				}

				cc.Code.Dump(indent+1);
			}

		}

		public override bool Render(StringBuilder dest)
		{
			dest.Append("try");
			Code.Render(dest);
			foreach (var cc in CatchClauses)
			{
				dest.Append("catch(");
				dest.Append(cc.ExceptionVariable);
				if (cc.Condition != null)
				{
					dest.Append(" if ");
					cc.Condition.Render(dest);
				}
				dest.Append(')');
				cc.Code.Render(dest);
			}

			if (FinallyClause != null)
			{
				dest.Append("finally{");
				Code.Render(dest);
				dest.Append('}');
			}
			return false;

		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Code.Visit(visitor);
			foreach (var cc in CatchClauses)
			{
				cc.Condition.Visit(visitor);
				Code.Visit(visitor);
			}
			if (FinallyClause != null)
				FinallyClause.Visit(visitor);
		}



		public class CatchClause
		{
			public string ExceptionVariable;
			public ExpressionNode Condition;
			public Statement Code;
		}

		public Statement Code;
		public List<CatchClause> CatchClauses =new List<CatchClause>();
		public Statement FinallyClause;
	}
}
