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
	// Represents a call to a method or global function.
	class ExprNodeCall : ExprNode
	{
		// Constructor
		public ExprNodeCall(Bookmark bookmark, ExprNode lhs) : base(bookmark)
		{
			Lhs = lhs;
		}

		// Attributes
		public ExprNode Lhs;
		public List<ExprNode> Arguments = new List<ExprNode>();

		public override void Dump(int indent)
		{
			writeLine(indent, "Call");
			Lhs.Dump(indent + 1);
			writeLine(indent, "with args:");
			foreach (var a in Arguments)
			{
				a.Dump(indent + 1);
			}
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.terminal;
		}

		public override bool Render(RenderContext dest)
		{
			WrapAndRender(dest, Lhs, false);
			dest.Append("(");
			bool first = true;
			foreach (var a in Arguments)
			{
				if (!first)
					dest.Append(",");
				a.Render(dest);
				first = false;
			}
			dest.Append(")");
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Lhs.Visit(visitor);
			foreach (var c in Arguments)
				c.Visit(visitor);
		}
	}

}
