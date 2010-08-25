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
	// Represents a postfix increment or decrement
	class ExprNodeParens : ExprNode
	{
		// Constructor
		public ExprNodeParens(Bookmark bookmark, ExprNode inner) : base(bookmark)
		{
			Inner = inner;
		}

		// Attributes
		public ExprNode Inner;
		
		public override string ToString()
		{
			return String.Format("({0})-postfix-{0}", Inner.ToString());
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "parens");
			Inner.Dump(indent + 1);
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.terminal;
		}

		public override bool Render(RenderContext dest)
		{
			dest.Append("(");
			Inner.Render(dest);
			dest.Append(")");
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Inner.Visit(visitor);
		}

		public override ExprNode Simplify()
		{
			return Inner.Simplify();
		}

		public override bool HasSideEffects()
		{
			return Inner.HasSideEffects();
		}


	}
}
