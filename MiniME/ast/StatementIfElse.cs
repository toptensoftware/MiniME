using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Represents an if-else statement
	class StatementIfElse : Statement
	{
		// Constructor
		public StatementIfElse(ExpressionNode condition)
		{
			Condition = condition;
		}

		// Attributes
		public ExpressionNode Condition;
		public Statement TrueStatement;
		public Statement FalseStatement;

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

		// Resolve the hanging-else problem

		// If 
		//	we have an else clause and...
		//  the inner statement is a an if statement and...
		//  it doesn't have an else clause
		// Then
		//  wrap the inner statement in braces
		public bool CheckForHangingElse()
		{

			if (FalseStatement == null)
				return false;

			if (TrueStatement.GetType() != typeof(StatementIfElse))
				return false;

			StatementIfElse InnerIf = (StatementIfElse)TrueStatement;

			if (InnerIf.FalseStatement == null)
				return true;

			return false;
		}

		public override bool Render(RenderContext dest)
		{
			// Statement
			dest.Append("if(");
			Condition.Render(dest);
			dest.Append(")");

			// True block
			bool retv;
			if (CheckForHangingElse())
			{
				dest.StartLine();
				dest.Append("{");
				TrueStatement.RenderIndented(dest);
				dest.StartLine();
				dest.Append("}");
				retv = false;
			}
			else
			{
				retv = TrueStatement.RenderIndented(dest);
			}

			// False block
			if (FalseStatement != null)
			{
				if (retv)
					dest.Append(';');

				dest.StartLine();

				dest.Append("else");
				if (FalseStatement.GetType() != typeof(StatementBlock))
					dest.Append(' ');

				retv = FalseStatement.RenderIndented(dest);
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

	}
}
