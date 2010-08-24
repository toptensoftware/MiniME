/*
 * MiniME
 * 
 * Copyright (C) 2010 Topten Software. Some Rights Reserved.
 * See http://toptensoftware.com/minime for licensing terms.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Represents a comma separated composite expression
	class ExprNodeComposite : ExprNode
	{
		// Constrictor
		public ExprNodeComposite(Bookmark bookmark) : base(bookmark)
		{
		}

		// Attributes
		public List<ExprNode> Expressions = new List<ExprNode>();

		public override void Dump(int indent)
		{
			writeLine(indent, "composite expression:");
			foreach (var e in Expressions)
			{
				e.Dump(indent + 1);
			}
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.comma;
		}

		public override bool Render(RenderContext dest)
		{
			for (int i = 0; i < Expressions.Count; i++)
			{
				if (i > 0)
					dest.Append(',');

				WrapAndRender(dest, Expressions[i], false);
			}
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var e in Expressions)
			{
				e.Visit(visitor);
			}
		}


		public override ExprNode Simplify()
		{
			SimplifyList(Expressions);
			return this;
		}

		public override bool HasSideEffects()
		{
			foreach (var n in Expressions)
			{
				if (!n.HasSideEffects())
					return false;
			}
			return true;
		}
	}
}
