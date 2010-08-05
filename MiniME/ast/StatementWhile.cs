using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Represents a while statement
	class StatementWhile : Statement
	{
		// Constructor
		public StatementWhile(Bookmark bookmark) : base(bookmark)
		{
		}

		// Attributes
		public Expression Condition;
		public CodeBlock Code;


		public override void Dump(int indent)
		{
			writeLine(indent, "while:");
			Condition.Dump(indent + 1);
			writeLine(indent, "do:");
			Code.Dump(indent + 1);
		}

		public override bool Render(RenderContext dest)
		{
			dest.Append("while(");
			Condition.Render(dest);
			dest.Append(")");
			return Code.RenderIndented(dest);
		}
		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Condition.Visit(visitor);
			Code.Visit(visitor);
		}



	}
}
