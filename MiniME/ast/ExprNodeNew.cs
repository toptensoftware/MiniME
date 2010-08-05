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
	// Represents object creation through `new` keyword
	class ExprNodeNew : ExprNode
	{
		// Constructor
		public ExprNodeNew(Bookmark bookmark, ExprNode objectType) : base(bookmark)
		{
			ObjectType= objectType;
		}

		// Attributes
		public ExprNode ObjectType;
		public List<ExprNode> Arguments = new List<ExprNode>();

		public override void Dump(int indent)
		{
			writeLine(indent, "New ");
			ObjectType.Dump(indent + 1);
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
			dest.Append("new");
			WrapAndRender(dest, ObjectType, false);
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
			ObjectType.Visit(visitor);
			foreach (var c in Arguments)
				c.Visit(visitor);
		}
	}
}
