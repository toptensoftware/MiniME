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
	// Represents a root level symbol, or a member on the rhs of a member dot.
	class ExprNodeIdentifier : ExprNode
	{
		public ExprNodeIdentifier(Bookmark bookmark) : base(bookmark)
		{

		}
		// Constructor
		public ExprNodeIdentifier(Bookmark bookmark, string name) : base(bookmark)
		{
			Name = name;
		}

		// Constructor
		public ExprNodeIdentifier(Bookmark bookmark, string name, ExprNode lhs) : base(bookmark)
		{
			Name = name;
			Lhs = lhs;
		}

		// Attributes
		public string Name;
		public ExprNode Lhs;

		public override void Dump(int indent)
		{
			if (Lhs == null)
				writeLine(indent, "Variable `{0}`", Name);
			else
			{
				writeLine(indent, "Member `{0}` on:", Name);
				Lhs.Dump(indent + 1);
			}
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.terminal;
		}

		public override bool Render(RenderContext dest)
		{
			if (Lhs != null)
			{
				WrapAndRender(dest, Lhs, false);
				dest.Append(".");
				dest.Append(dest.Members.GetObfuscatedSymbol(Name));
			}
			else
			{
				// Find the symbol and check if it's a constant
				var s = dest.CurrentScope.FindSymbol(Name);
				if (s != null && s.ConstValue != null)
				{
					ExprNodeLiteral.RenderValue(dest, s.ConstValue);
				}
				else
				{
					dest.Append(dest.Symbols.GetObfuscatedSymbol(Name));
				}
			}
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			if (Lhs!=null)
				Lhs.Visit(visitor);
		}


	}
}
