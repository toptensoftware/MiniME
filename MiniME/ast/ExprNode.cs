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
	// Operator precedence is used to determine how parentheses must
	// be generated when regenerating the Javascript code from the 
	// abstract syntax tree.
	enum OperatorPrecedence
	{
		comma,
		assignment,
		conditional,
		logor,
		logand,
		bitor,
		bitxor,
		bitand,
		equality,
		relational,
		bitshift,
		add,
		multiply,
		negation,
		function,
		terminal,
	}

	// Base class for all expression nodes
	abstract class ExprNode : Node
	{
		// Constructor
		public ExprNode(Bookmark bookmark) : base(bookmark)
		{

		}

		// Must be overridden in all node types to return the precedence
		public abstract OperatorPrecedence GetPrecedence();

		// Render an child node, wrapping it in parentheses if necessary
		public void WrapAndRender(RenderContext dest, ExprNode other, bool bWrapEqualPrecedence)
		{
			var precOther=other.GetPrecedence();
			var precThis = this.GetPrecedence();
			if (precOther < precThis || (precOther==precThis && bWrapEqualPrecedence))
			{
				dest.Append("(");
				other.Render(dest);
				dest.Append(")");
			}
			else
			{
				other.Render(dest);
			}
		}

		// Return the constant value of this expression, or null if can't
		public virtual object EvalConstLiteral()
		{
			return null;
		}

	}

}
