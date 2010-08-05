using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Represents a switch-case statement
	class StatementSwitch : Statement
	{
		// Constructor
		public StatementSwitch(Bookmark bookmark, Expression testExpression) : base(bookmark)
		{
			TestExpression = testExpression;
		}

		// Attributes
		public Expression TestExpression;
		public List<Case> Cases = new List<Case>();

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
			// Statement
			dest.Append("switch(");
			TestExpression.Render(dest);
			dest.Append(')');
			dest.StartLine();

			// Opening brace
			dest.Append('{');

			// Cases
			dest.Indent();
			bool bNeedSemicolon=false;
			foreach (var c in Cases)
			{
				// Separator
				if (bNeedSemicolon)
					dest.Append(';');

				// `case` or `default`
				dest.StartLine();
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

				// Is there any code associated with this case?
				if (c.Code != null)
				{
					bNeedSemicolon = c.Code.RenderIndented(dest);
				}
				else
				{
					bNeedSemicolon = false;
				}
			}

			// Close brace
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



		// Create a new case clause
		public Case AddCase(Expression Value)
		{
			var c=new Case(Value);
			Cases.Add(c);
			return c;
		}

		// Represents a single case in a switch statement
		public class Case
		{
			// Constructor
			public Case(Expression value)
			{
				Value = value;
			}

			// Attributes
			public Expression Value;
			public CodeBlock Code;

			// Add code to this case clause.
			public void AddCode(ast.Statement statement)
			{
				// First time?
				if (Code == null)
				{
					Code = new ast.CodeBlock(statement.Bookmark, TriState.No);
				}

				Code.AddStatement(statement);
			}
		}
	}
}

