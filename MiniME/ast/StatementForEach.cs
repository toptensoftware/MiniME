using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class StatementForEach : Statement
	{
		public StatementForEach()
		{
		}

		public override void Dump(int indent)
		{
			if (VariableDeclaration != null)
			{
				writeLine(indent, "for each loop, with new variable:");
			}
			else
			{
				writeLine(indent, "for each loop, with existing variable");
			}

			Iterator.Dump(indent + 1);
			writeLine(indent, "collection:");
			Collection.Dump(indent + 1);
			writeLine(indent, "do:");
			CodeBlock.Dump(indent + 1);
		}

		public override bool Render(RenderContext dest)
		{
			dest.Append("for(");
			if (VariableDeclaration!=null)
				VariableDeclaration.Render(dest);
			else
				Iterator.Render(dest);
			dest.Append(" in ");
			Collection.Render(dest);
			dest.Append(")");
			return CodeBlock.RenderIndented(dest);
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			if (VariableDeclaration != null)
				VariableDeclaration.Visit(visitor);
			else
				Iterator.Visit(visitor);
			Collection.Visit(visitor);
			CodeBlock.Visit(visitor);
		}


		public Statement VariableDeclaration;		// for (var x in ...)
		public ExpressionNode Iterator;				// for (x in ...)
		public ExpressionNode Collection;
		public Statement CodeBlock;
	}
}
