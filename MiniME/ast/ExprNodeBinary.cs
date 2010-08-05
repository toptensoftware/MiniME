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
	class ExprNodeBinary : ExprNode
	{
		// Constructor
		public ExprNodeBinary(Bookmark bookmark, ExprNode lhs, ExprNode rhs, Token op) : base(bookmark)
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
				case Token.add:
				case Token.subtract:
					return OperatorPrecedence.add;

				case Token.multiply:
				case Token.divide:
				case Token.modulus:
					return OperatorPrecedence.multiply;

				case Token.shl:
				case Token.shr:
				case Token.shrz:
					return OperatorPrecedence.bitshift;

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

				case Token.compareEQ:
				case Token.compareNE:
				case Token.compareEQStrict:
				case Token.compareNEStrict:
					return OperatorPrecedence.equality;

				case Token.compareLT:
				case Token.compareLE:
				case Token.compareGT:
				case Token.compareGE:
				case Token.kw_in:
				case Token.kw_instanceof:
					return OperatorPrecedence.relational;

				case Token.bitwiseXor:
					return OperatorPrecedence.bitxor;

				case Token.bitwiseOr:
					return OperatorPrecedence.bitor;

				case Token.bitwiseAnd:
					return OperatorPrecedence.bitand;

				case Token.logicalNot:
					return OperatorPrecedence.logand;

				case Token.logicalOr:
					return OperatorPrecedence.logor;

				case Token.logicalAnd:
					return OperatorPrecedence.logand;

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
				case Token.add:				
					dest.Append("+");

					// Prevent `lhs + ++rhs` from becoming `lhs+++rhs` (which would incorrectly be interpreted as `lhs++ + rhs`)
					dest.NeedSpaceIf('+');
					break;

				case Token.subtract:		
					dest.Append("-");

					// Prevent `lhs - --rhs` from becoming `lhs---rhs` (which would incorrectly be interpreted as `lhs-- - rhs`)
					// Also prevent `lhs- -rhs` from become `lhs--rhs` (which would incorrectly be interpreted as `lhs-- rhs`)
					dest.NeedSpaceIf('-');
					break;

				case Token.multiply:
				case Token.divide:
				case Token.modulus:
				case Token.shl:
				case Token.shr:
				case Token.assign:
				case Token.shrz:
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
				case Token.compareEQ:
				case Token.compareNE:
				case Token.compareLT:
				case Token.compareLE:
				case Token.compareGT:
				case Token.compareGE:
				case Token.compareEQStrict:
				case Token.compareNEStrict:
				case Token.bitwiseXor:
				case Token.bitwiseOr:
				case Token.bitwiseAnd:
				case Token.logicalNot:
				case Token.logicalOr:
				case Token.logicalAnd:
					dest.Append(Tokenizer.FormatToken(Op));
					break;

				case Token.kw_in:
				case Token.kw_instanceof:
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

		public static object Eval(double lhs, double rhs, Token Op)
		{
			switch (Op)
			{
				case Token.add: return lhs + rhs;
				case Token.subtract: return lhs - rhs;
				case Token.multiply: return lhs * rhs;
				case Token.divide: return lhs / rhs;
				case Token.modulus: return lhs % rhs;
				case Token.compareEQ: return lhs == rhs;
				case Token.compareEQStrict: return lhs == rhs;
				case Token.compareNE: return lhs != rhs;
				case Token.compareNEStrict: return lhs != rhs;
				case Token.compareLT: return lhs < rhs;
				case Token.compareLE: return lhs <= rhs;
				case Token.compareGT: return lhs > rhs;
				case Token.compareGE: return lhs >= rhs;
			}

			return null;
		}

		public static object Eval(long lhs, long rhs, Token Op)
		{
			switch (Op)
			{
				case Token.add: return lhs + rhs;
				case Token.subtract: return lhs - rhs;
				case Token.multiply: return lhs * rhs;
				case Token.divide: return lhs / rhs;
				case Token.modulus: return lhs % rhs;
				case Token.bitwiseXor: return lhs ^ rhs;
				case Token.bitwiseOr: return lhs | rhs;
				case Token.bitwiseAnd: return lhs & rhs;
				case Token.shl: return lhs << (int)rhs;
				case Token.shr: return lhs >> (int)rhs;
				case Token.shrz: return (long)(((ulong)lhs) >> (int)rhs);
				case Token.compareEQ: return lhs == rhs;
				case Token.compareEQStrict: return lhs == rhs;
				case Token.compareNE: return lhs != rhs;
				case Token.compareNEStrict: return lhs != rhs;
				case Token.compareLT: return lhs < rhs;
				case Token.compareLE: return lhs <= rhs;
				case Token.compareGT: return lhs > rhs;
				case Token.compareGE: return lhs >= rhs;
			}
			return null;
		}

		public static object Eval(bool lhs, bool rhs, Token Op)
		{
			switch (Op)
			{
				case Token.compareEQ: return lhs == rhs;
				case Token.compareEQStrict: return lhs == rhs;
				case Token.compareNE: return lhs != rhs;
				case Token.compareNEStrict: return lhs != rhs;
				case Token.logicalAnd: return lhs && rhs;
				case Token.logicalOr: return lhs || rhs;
			}

			return null;
		}


		public override object EvalConstLiteral()
		{
			// Eval left, quit if cant
			var lhs = Lhs.EvalConstLiteral();
			if (lhs == null)
				return null;

			// Eval right, quit if cant
			var rhs = Rhs.EvalConstLiteral();
			if (rhs == null)
				return null;

			// We don't like strings
			if (lhs.GetType() == typeof(string) ||
				rhs.GetType() == typeof(string))
			{
				return null;
			}

			// Double?
			if (lhs.GetType() == typeof(DoubleLiteral) &&
				rhs.GetType() == typeof(DoubleLiteral))
			{
				return Eval(((DoubleLiteral)lhs).Value, ((DoubleLiteral)rhs).Value, Op);
			}

			// Long?
			if (lhs.GetType()==typeof(long) && 
				rhs.GetType()==typeof(long))
			{
				return Eval((long)lhs, (long)rhs, Op);
			}

			// Bool
			if (lhs.GetType() == typeof(bool) &&
				rhs.GetType() == typeof(bool))
			{
				return Eval((bool)lhs, (bool)rhs, Op);
			}

			return null;
		}

	}
}
