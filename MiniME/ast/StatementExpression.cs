using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Wraps an expression as a statement
	class StatementExpression : Statement
	{
		// Constructor
		public StatementExpression(Bookmark bookmark, Expression expression) : base(bookmark)
		{
			Expression = expression;
		}

		// Attributes
		public Expression Expression;

		public override void Dump(int indent)
		{
			writeLine(indent, "Expression:");
			Expression.Dump(indent + 1);
		}
		public override bool Render(RenderContext dest)
		{
			if (Bookmark.warnings && !Expression.RootNode.HasSideEffects())
			{
				Console.WriteLine("{0}: warning: expression has no side effects", Bookmark);
			}

			return Expression.Render(dest);
		}
		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Expression.Visit(visitor);
		}



	}
}
