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
	class ExprNodePostfix : ExprNode
	{
		// Constructor
		public ExprNodePostfix(Bookmark bookmark, ExprNode lhs, Token op) : base(bookmark)
		{
			Lhs = lhs;
			Op = op;
		}

		// Attributes
		public ExprNode Lhs;
		public Token Op;
		
		public override string ToString()
		{
			return String.Format("({1})-postfix-{0}", Op.ToString(), Lhs.ToString());
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "postfix {0}", Op.ToString());
			Lhs.Dump(indent + 1);
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.unary;
		}

		public override bool Render(RenderContext dest)
		{
			dest.DisableLineBreaks();

			WrapAndRender(dest, Lhs, false);
			switch (Op)
			{
				case Token.increment:
				case Token.decrement:
					dest.Append(Tokenizer.FormatToken(Op));
					break;

				default:
					System.Diagnostics.Debug.Assert(false);
					break;
			}
			dest.EnableLineBreaks();
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Lhs.Visit(visitor);
		}

		public override ExprNode Simplify()
		{
			Lhs = Lhs.Simplify();
			return this;
		}

		public override bool HasSideEffects()
		{
			return true;
		}


	}
}
