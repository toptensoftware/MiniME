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
	// Represents an array literal (eg: [1,2,3])
	class ExprNodeArrayLiteral : ExprNode
	{
		// Constructor
		public ExprNodeArrayLiteral(Bookmark bookmark) : base(bookmark)
		{

		}

		// Attributes
		public List<ExprNode> Values = new List<ExprNode>();

		public override string ToString()
		{
			return "<array literal>";
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "array literal:");
			foreach (var e in Values)
			{
				if (e == null)
					writeLine(indent + 1, "<undefined>");
				else
					e.Dump(indent + 1);
			}
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.terminal;
		}


		public override bool Render(RenderContext dest)
		{
			dest.Append('[');
			bool bFirst = true;
			foreach (var e in Values)
			{
				if (!bFirst)
					dest.Append(",");
				else
					bFirst = false;
				WrapAndRender(dest, e, false);
			}
			dest.Append(']');
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var e in Values)
				e.Visit(visitor);
		}


		public override ExprNode Simplify()
		{
			SimplifyList(Values);
			return this;
		}


	}
}
