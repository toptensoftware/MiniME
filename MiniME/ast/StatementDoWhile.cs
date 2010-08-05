using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Represents a do-while statement
	class StatementDoWhile : Statement
	{
		// Constructor
		public StatementDoWhile(Bookmark bookmark) : base(bookmark)
		{
		}

		// Attributes
		public Expression Condition;
		public CodeBlock Code;

		public override void Dump(int indent)
		{
			writeLine(indent, "do:");
			Code.Dump(indent + 1);
			writeLine(indent, "while:");
			Condition.Dump(indent + 1);
		}

		public override bool Render(RenderContext dest)
		{
			// Render the statement, need special handling to insert
			// a space if don't have a braced statement block
			dest.Append("do");
			if (Code.RenderIndented(dest))
				dest.Append(';');
			dest.StartLine();
			dest.Append("while(");
			Condition.Render(dest);
			dest.Append(')');
			return false;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Condition.Visit(visitor);
			Code.Visit(visitor);
		}



	}
}
