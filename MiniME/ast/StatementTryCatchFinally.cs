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
				cc.Dump(indent);
			}

		}

		public override bool Render(RenderContext dest)
		{
			dest.Append("try");
			Code.Render(dest);
			foreach (var cc in CatchClauses)
			{
				cc.Render(dest);
			}

			if (FinallyClause != null)
			{
				dest.Append("finally");
				FinallyClause.RenderIndented(dest);
			}
			return false;

		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Code.Visit(visitor);
			foreach (var cc in CatchClauses)
			{
				cc.Visit(visitor);
			}
			if (FinallyClause != null)
				FinallyClause.Visit(visitor);
		}



		public Statement Code;
		public List<CatchClause> CatchClauses =new List<CatchClause>();
		public Statement FinallyClause;
	}

	class CatchClause : Statement
	{
		public override void Dump(int indent)
		{
			if (Condition != null)
			{
				writeLine(indent, "catch `{0}` if:", ExceptionVariable);
				Condition.Dump(indent + 1);
				writeLine(indent, "do:");
			}
			else
			{
				writeLine(indent, "catch `{0}` do:", ExceptionVariable);
			}

			Code.Dump(indent + 1);
		}

		public override bool Render(RenderContext dest)
		{
			// Enter a new symbol scope
			dest.Symbols.EnterScope();

			// Render the function
			Scope.ObfuscateSymbols(dest);

			dest.StartLine();
			dest.Append("catch(");
			dest.Append(dest.Symbols.GetObfuscatedSymbol(ExceptionVariable));
			if (Condition != null)
			{
				dest.Append(" if ");
				Condition.Render(dest);
			}
			dest.Append(')');
			Code.Render(dest);

			dest.Symbols.LeaveScope();
			return false;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			if (Condition != null)
				Condition.Visit(visitor);
			if (Code != null)
				Code.Visit(visitor);
		}
		public string ExceptionVariable;
		public ExpressionNode Condition;
		public Statement Code;
	}


}
