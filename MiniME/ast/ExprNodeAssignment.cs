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
	// Represents a binary operation (eg: x+y)
	class ExprNodeAssignment : ExprNode
	{
		// Constructor
		public ExprNodeAssignment(Bookmark bookmark, ExprNode lhs, ExprNode rhs, Token op) : base(bookmark)
		{
			Lhs = lhs;
			Rhs = rhs;
			Op = op;
		}

		// Attributes
		public ExprNode Lhs;
		public ExprNode Rhs;
		public Token Op;

		public override string ToString()
		{
			return String.Format("{0}({1}, {2})", Op.ToString(), Lhs.ToString(), Rhs.ToString());
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "{0}", Op.ToString());
			Lhs.Dump(indent + 1);
			Rhs.Dump(indent + 1);
		}

		public override OperatorPrecedence GetPrecedence()
		{
			switch (Op)
			{
				case Token.assign:
				case Token.addAssign:
				case Token.subtractAssign:
				case Token.multiplyAssign:
				case Token.divideAssign:
				case Token.modulusAssign:
				case Token.shlAssign:
				case Token.shrAssign:
				case Token.shrzAssign:
				case Token.bitwiseXorAssign:
				case Token.bitwiseOrAssign:
				case Token.bitwiseAndAssign:
					return OperatorPrecedence.assignment;
				default:
					System.Diagnostics.Debug.Assert(false);
					return OperatorPrecedence.terminal;
			}
		}

		public override bool Render(RenderContext dest)
		{
			// LHS
			WrapAndRender(dest, Lhs, false);

			// Operator
			switch (Op)
			{
				case Token.assign:
				case Token.addAssign:
				case Token.subtractAssign:
				case Token.multiplyAssign:
				case Token.divideAssign:
				case Token.modulusAssign:
				case Token.shlAssign:
				case Token.shrAssign:
				case Token.shrzAssign:
				case Token.bitwiseXorAssign:
				case Token.bitwiseOrAssign:
				case Token.bitwiseAndAssign:
					dest.Append(Tokenizer.FormatToken(Op));
					break;
				default:
					System.Diagnostics.Debug.Assert(false);
					break;
			}

			// RHS
			WrapAndRender(dest, Rhs, true);
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Lhs.Visit(visitor);
			Rhs.Visit(visitor);
		}


		public override ExprNode Simplify()
		{
			Lhs = Lhs.Simplify();
			Rhs = Rhs.Simplify();
			return this;
		}

	}
}
