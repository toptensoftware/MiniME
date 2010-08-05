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
	// Represents a unary operator (eg: ++, !, ~ etc...)
	class ExprNodeUnary : ExprNode
	{
		// Constrctor
		public ExprNodeUnary(Bookmark bookmark, ExprNode rhs, Token op) : base(bookmark)
		{
			Rhs = rhs;
			Op = op;
		}

		// Attributes
		public ExprNode Rhs;
		public Token Op;

		public override string ToString()
		{
			return String.Format("{0}({1})", Op.ToString(), Rhs.ToString());
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "{0}", Op.ToString());
			Rhs.Dump(indent+1);
		}

		public override OperatorPrecedence GetPrecedence()
		{
			switch (Op)
			{
				case Token.bitwiseNot:
				case Token.logicalNot:
				case Token.add:
				case Token.subtract:
				case Token.increment:
				case Token.decrement:
				case Token.kw_typeof:
				case Token.kw_void:
				case Token.kw_delete:
					return OperatorPrecedence.negation;

				default:
					System.Diagnostics.Debug.Assert(false);
					return OperatorPrecedence.terminal;
			}

		}

		public override bool Render(RenderContext dest)
		{
			switch (Op)
			{
				case Token.kw_typeof:
				case Token.kw_void:
				case Token.kw_delete:
					dest.Append(Op.ToString().Substring(3));
					break;

				case Token.add:
					break;

				case Token.subtract:
					dest.Append('-');
					dest.NeedSpaceIf('-');
					break;

				case Token.bitwiseNot:
				case Token.logicalNot:
				case Token.increment:
				case Token.decrement:
					dest.Append(Tokenizer.FormatToken(Op));
					break;

				default:
					System.Diagnostics.Debug.Assert(false);
					break;
			}

			// RHS
			WrapAndRender(dest, Rhs, false);
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Rhs.Visit(visitor);
		}

		public static object Eval(double rhs, Token Op)
		{
			switch (Op)
			{
				case Token.add: return rhs;
				case Token.subtract: return -rhs;
			}

			return null;
		}

		public static object Eval(long rhs, Token Op)
		{
			switch (Op)
			{
				case Token.bitwiseNot: return ~rhs;
				case Token.logicalNot: return !(rhs != 0);
				case Token.add: return rhs;
				case Token.subtract: return -rhs;
			}
			return null;
		}

		public static object Eval(bool rhs, Token Op)
		{
			switch (Op)
			{
				case Token.logicalNot: return !rhs;
			}
			return null;
		}

		public override object EvalConstLiteral()
		{
			// Eval right, quit if cant
			var rhs = Rhs.EvalConstLiteral();
			if (rhs == null)
				return null;

			// Double?
			if (rhs.GetType() == typeof(DoubleLiteral))
			{
				return Eval(((DoubleLiteral)rhs).Value, Op);
			}

			// Long?
			if (rhs.GetType() == typeof(long))
			{
				return Eval((long)rhs, Op);
			}

			return null;
		}


		public override ExprNode Simplify()
		{
			Rhs = Rhs.Simplify();
			return this;
		}

	}
}
