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
				case Token.add:
				case Token.subtract:
					return OperatorPrecedence.unary;

				case Token.bitwiseNot:
				case Token.logicalNot:
				case Token.increment:
				case Token.decrement:
				case Token.kw_typeof:
				case Token.kw_void:
				case Token.kw_delete:
					return OperatorPrecedence.unary;

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
			// Simplify the RHS
			Rhs = Rhs.Simplify();

			// Redundant positive operator?
			if (Op == Token.add)
			{
				return Rhs;
			}

			// Negatives?
			if (Op == Token.subtract)
			{
				// Double negative
				var rhsUnary = Rhs as ExprNodeUnary;
				if (rhsUnary!=null && rhsUnary.Op == Token.subtract)
				{
					return rhsUnary.Rhs;
				}

				// Negative Add/Subtract
				var rhsLtr = Rhs as ExprNodeLtr;
				if (rhsLtr != null && rhsLtr.GetPrecedence() == OperatorPrecedence.add)
				{
					//eg: convert -(a+b) to -a-b

					// Swap all operators

					// Wrap the LHS in a unary negative, then simplify it again
					rhsLtr.Lhs = new ast.ExprNodeUnary(Bookmark, rhsLtr.Lhs, Token.subtract);
					rhsLtr.Lhs = rhsLtr.Lhs.Simplify();

					// Swap the add/subtract on all other terms
					foreach (var t in rhsLtr.Terms)
					{
						switch (t.Op)
						{
							case Token.add:
								t.Op = Token.subtract;
								break;

							case Token.subtract:
								t.Op = Token.add;
								break;

							default:
								System.Diagnostics.Debug.Assert(false);
								break;
						}
					}

					// Return the simplified LTR expression
					return rhsLtr;
				}

				// Negative of multiply terms
				if (rhsLtr != null && rhsLtr.GetPrecedence() == OperatorPrecedence.multiply)
				{
					// eg: convert -(a*b) to -a*b

					// Wrap the first term in a unary negative, then simplify it again
					rhsLtr.Lhs = new ast.ExprNodeUnary(Bookmark, rhsLtr.Lhs, Token.subtract);
					return rhsLtr.Simplify();
				}
			}

			if (Op == Token.bitwiseNot || Op == Token.logicalNot)
			{
				/*
				// Double negative  eg: !!x   or ~~x
				var rhsUnary = Rhs as ExprNodeUnary;
				if (rhsUnary != null && rhsUnary.Op == Op)
				{
					return rhsUnary.Rhs;
				}
				 */

				// Actually, don't do the above cause 
				//		!! converts to bool
				//		~~ converts bool to integer

			}

			return this;
		}

		public override bool HasSideEffects()
		{
			return false;
		}

	}
}
