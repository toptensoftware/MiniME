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
	class ExprNodeLtr : ExprNode
	{
		public class Term
		{
			public Term(ExprNode rhs, Token op)
			{
				Rhs = rhs;
				Op = op;
			}
			public ExprNode Rhs;
			public Token Op;
		}

		// Constructor
		public ExprNodeLtr(Bookmark bookmark, ExprNode lhs) : base(bookmark)
		{
			Lhs = lhs;
		}

		public void AddTerm(Token Op, ExprNode Rhs)
		{
			Terms.Add(new Term(Rhs, Op));
		}

		// Attributes
		public ExprNode Lhs;
		public List<Term> Terms=new List<Term>();

		public override string ToString()
		{
			var buf = new StringBuilder();
			buf.Append("(");
			buf.Append(Lhs.ToString());
			foreach (var t in Terms)
			{
				buf.Append(" ");
				buf.Append(t.Op.ToString());
				buf.Append(" ");
				buf.Append(t.Rhs.ToString());
			}
			buf.Append(")");
			return buf.ToString();
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "(");
			Lhs.Dump(indent + 1);
			foreach (var t in Terms)
			{
				writeLine(indent, t.Op.ToString());
				t.Rhs.Dump(indent + 1);
			}
			writeLine(indent, ")");
		}

		public static OperatorPrecedence PrecedenceOfToken(Token op)
		{
			switch (op)
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

				case Token.logicalOr:
					return OperatorPrecedence.logor;

				case Token.logicalAnd:
					return OperatorPrecedence.logand;

				default:
					System.Diagnostics.Debug.Assert(false);
					return OperatorPrecedence.terminal;
			}
		}

		public override OperatorPrecedence GetPrecedence()
		{
			OperatorPrecedence precedence = PrecedenceOfToken(Terms[0].Op);

			// Check all terms have the same precedence
			foreach (var t in Terms)
			{
				System.Diagnostics.Debug.Assert(PrecedenceOfToken(t.Op) == precedence);
			}

			return precedence;
		}

		public override bool Render(RenderContext dest)
		{
			// LHS
			WrapAndRender(dest, Lhs, false);

			foreach (var t in Terms)
			{
				// Operator
				switch (t.Op)
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
					case Token.shrz:
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
					case Token.logicalOr:
					case Token.logicalAnd:
					case Token.kw_in:
					case Token.kw_instanceof:
						dest.Append(Tokenizer.FormatToken(t.Op));
						break;

					default:
						System.Diagnostics.Debug.Assert(false);
						break;
				}

				// RHS
				WrapAndRender(dest, t.Rhs, true);
			}

			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Lhs.Visit(visitor);
			foreach (var t in Terms)
				t.Rhs.Visit(visitor);
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

			foreach (var t in Terms)
			{
				// Eval right, quit if cant
				var rhs = t.Rhs.EvalConstLiteral();
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
					lhs = Eval(((DoubleLiteral)lhs).Value, ((DoubleLiteral)rhs).Value, t.Op);
				}

				// Long?
				if (lhs.GetType() == typeof(long) &&
					rhs.GetType() == typeof(long))
				{
					lhs=Eval((long)lhs, (long)rhs, t.Op);
				}

				// Bool
				if (lhs.GetType() == typeof(bool) &&
					rhs.GetType() == typeof(bool))
				{
					lhs=Eval((bool)lhs, (bool)rhs, t.Op);
				}

				if (lhs==null)
					return null;
			}

			return lhs;
		}

		public Token InverseOp(Token op)
		{
			switch (op)
			{
				case Token.add:
					return Token.subtract;
				case Token.subtract:
					return Token.add;
				case Token.multiply:
					return Token.divide;
				case Token.divide:
					return Token.multiply;
			}

			System.Diagnostics.Debug.Assert(false);
			return Token.eof;
		}

		public int IndexOfLastModulusOp()
		{
			for (int i = Terms.Count - 1; i >= 0; i--)
			{
				if (Terms[i].Op == Token.modulus)
					return i;
			}

			return -1;
		}

		public override ExprNode Simplify()
		{
			// First, simplify all our terms
			Lhs = Lhs.Simplify();
			foreach (var t in Terms)
			{
				t.Rhs = t.Rhs.Simplify();
			}

			// Combine add/subtract subterms
			if (GetPrecedence() == OperatorPrecedence.add)
			{
				for (int i=0; i<Terms.Count; i++)
				{
					var t = Terms[i];

					// Negative term, swap our own operator
					// eg: a - -b -> a+b
					var unaryTerm = t.Rhs as ExprNodeUnary;
					if (unaryTerm != null)
					{
						if (unaryTerm.Op == Token.subtract)
						{
							t.Op = InverseOp(t.Op);
							t.Rhs = unaryTerm.Rhs;
						}
					}

					// Nested LTR add operation
					// eg: x-(a+b) => x-a-b
					var ltrTerm = t.Rhs as ExprNodeLtr;
					if (ltrTerm != null && ltrTerm.GetPrecedence()==OperatorPrecedence.add)
					{
						// Move the first child term to self
						t.Rhs=ltrTerm.Lhs;

						// Negate the other child terms
						if (t.Op == Token.subtract)
						{
							// Swap the operator of other terms
							foreach (var nestedTerm in ltrTerm.Terms)
							{
								nestedTerm.Op = InverseOp(nestedTerm.Op);
							}
						}

						// Insert the inner terms
						Terms.InsertRange(i + 1, ltrTerm.Terms);

						// Continue with self again to catch new subtract of negative
						//  eg: we've now done this: x-(-a+b) => x- -a + b
						//      need to reprocess this term to get		x+a+b
						i--;
					}
				}
			}

			// Combine add/subtract subterms
			if (GetPrecedence() == OperatorPrecedence.multiply)
			{
				// Remove negatives on any modulus ops  eg: a%-b => a%b
				foreach (var t in Terms)
				{
					if (t.Op == Token.modulus)
					{
						var unaryTerm = t.Rhs as ExprNodeUnary;
						if (unaryTerm != null && unaryTerm.Op == Token.subtract)
						{
							t.Rhs = unaryTerm.Rhs;
						}
					}
				}

				// Nested LTR multiply operation
				// eg: x*(a*b) => x*a*b
				for (int i = 0; i < Terms.Count; i++)
				{
					var t = Terms[i];

					// nb: we don't do x/(a*b)=>x/a/b on the (possibly flawed) assumption div is slower
					if (t.Op != Token.multiply)
						continue;

					var ltrTerm = t.Rhs as ExprNodeLtr;
					if (ltrTerm != null && ltrTerm.GetPrecedence() == OperatorPrecedence.multiply)
					{
						int iLastMod = ltrTerm.IndexOfLastModulusOp();

						if (iLastMod < 0)
						{
							// Move the first child term to self
							t.Rhs = ltrTerm.Lhs;

							// Insert the inner terms
							Terms.InsertRange(i + 1, ltrTerm.Terms);
						}
						else
						{
							// Move the trailing multiply/divs to self
							// ie: a*(b%c*d) => a*(b%c)*d
							int iInsertPos = i + 1;
							while (iLastMod + 1 < ltrTerm.Terms.Count)
							{
								Terms.Insert(iInsertPos++, ltrTerm.Terms[iLastMod+1]);
								ltrTerm.Terms.RemoveAt(iLastMod + 1);
							}
						}
					}
				}

				// Remove -ve * -ve    eg: -a * -b => a*b

				// Step 1 - make all negated terms, positive.
				//			and count how many
				int negateCount = 0;
				foreach (var t in Terms)
				{
					var unaryTerm = t.Rhs as ExprNodeUnary;
					if (unaryTerm != null && unaryTerm.Op == Token.subtract)
					{
						// Remove the negate
						t.Rhs = unaryTerm.Rhs;
						negateCount++;
					}
				}

				// Step 2 - if there was an odd number of negates in the 
				//			other terms, negate the Lhs term
				if ((negateCount % 2) == 1)
				{
					var temp = new ExprNodeUnary(Lhs.Bookmark, Lhs, Token.subtract);
					Lhs = temp.Simplify();
				}


			}

			return this;
		}

	}
}
