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
	abstract class ExpressionNode : Node
	{
		// Constructor
		public ExpressionNode(Bookmark bookmark) : base(bookmark)
		{

		}

		// Must be overridden in all node types to return the precedence
		public abstract OperatorPrecedence GetPrecedence();

		// Render an child node, wrapping it in parentheses if necessary
		public void WrapAndRender(RenderContext dest, ExpressionNode other)
		{
			if (other.GetPrecedence() < this.GetPrecedence())
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

	// Represents a root level symbol, or a member on the rhs of a member dot.
	class ExprNodeIdentifier : ExpressionNode
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
		public ExprNodeIdentifier(Bookmark bookmark, string name, ExpressionNode lhs) : base(bookmark)
		{
			Name = name;
			Lhs = lhs;
		}

		// Attributes
		public string Name;
		public ExpressionNode Lhs;

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
				WrapAndRender(dest, Lhs);
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

	// Represents a call to a method or global function.
	class ExprNodeCall : ExpressionNode
	{
		// Constructor
		public ExprNodeCall(Bookmark bookmark, ExpressionNode lhs) : base(bookmark)
		{
			Lhs = lhs;
		}

		// Attributes
		public ExpressionNode Lhs;
		public List<ExpressionNode> Arguments = new List<ExpressionNode>();

		public override void Dump(int indent)
		{
			writeLine(indent, "Call");
			Lhs.Dump(indent + 1);
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
			WrapAndRender(dest, Lhs);
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
			Lhs.Visit(visitor);
			foreach (var c in Arguments)
				c.Visit(visitor);
		}
	}

	// Represents object creation through `new` keyword
	class ExprNodeNew : ExpressionNode
	{
		// Constructor
		public ExprNodeNew(Bookmark bookmark, ExpressionNode objectType) : base(bookmark)
		{
			ObjectType= objectType;
		}

		// Attributes
		public ExpressionNode ObjectType;
		public List<ExpressionNode> Arguments = new List<ExpressionNode>();

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
			WrapAndRender(dest, ObjectType);
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

	// Represents array indexer (ie: square brackets)
	class ExprNodeIndexer : ExpressionNode
	{
		// Constructor
		public ExprNodeIndexer(Bookmark bookmark, ExpressionNode lhs, ExpressionNode index) : base(bookmark)
		{
			Lhs = lhs;
			Index = index; 
		}

		// Attributes
		public ExpressionNode Lhs;
		public ExpressionNode Index;

		public override void Dump(int indent)
		{
			writeLine(indent, "Index");
			Lhs.Dump(indent + 1);
			writeLine(indent, "with:");
			Index.Dump(indent + 1);
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.terminal;
		}

		string GetIdentifier()
		{
			if (Index.GetType() != typeof(ast.ExprNodeLiteral))
				return null;

			object literal = ((ast.ExprNodeLiteral)Index).Value;
			if (literal.GetType() != typeof(string))
				return null;

			string identifier=(string)literal;
			if (!Tokenizer.IsIdentifier(identifier) && !Tokenizer.IsKeyword(identifier))
				return null;

			return identifier;
		}

		public override bool Render(RenderContext dest)
		{
			WrapAndRender(dest, Lhs);

			string str = GetIdentifier();
			if (str != null)
			{
				dest.Append('.');
				dest.Append(str);
			}
			else
			{
				dest.Append("[");
				Index.Render(dest);
				dest.Append("]");
			}
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Lhs.Visit(visitor);
			Index.Visit(visitor);
		}

	}

	// Represents a literal string, integer or double
	class ExprNodeLiteral : ExpressionNode
	{
		// Constructor
		public ExprNodeLiteral(Bookmark bookmark, object value) : base(bookmark)
		{
			Value = value;
		}

		// Attributes
		public object Value;

		public override string ToString()
		{
			return String.Format("literal({0} - {1})", Value.GetType(), Value);
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "literal - {0}", Value.GetType().ToString());
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.terminal;
		}

		// Helper to render a literal value with appropriate escapting etc...
		public static void RenderValue(RenderContext dest, object Value)
		{
			// Is it a string?
			if (Value.GetType() == typeof(string))
			{
				string str = (string)Value;

				// Don't do line breaks
				var buf = new StringBuilder();

				// Count quotes and double quotes and use the less frequent as the
				// string delimiter
				int quotes = 0;
				int dquotes = 0;
				foreach (char ch in str)
				{
					if (ch == '\'')
						quotes++;
					if (ch == '\"')
						dquotes++;
				}
				char chDelim = dquotes > quotes ? '\'' : '\"';

				// Opening quote
				buf.Append(chDelim);

				// Encode the string
				foreach (char ch in str)
				{
					if (ch < 127)
					{
						switch (ch)
						{
							case '\b':
								buf.Append("\\b");
								break;

							case '\f':
								buf.Append("\\f");
								break;

							case '\n':
								buf.Append("\\n");
								break;

							case '\r':
								buf.Append("\\r");
								break;

							case '\t':
								buf.Append("\\t");
								break;

							case '\'':
								if (chDelim == '\'')
									buf.Append("\\\'");
								else
									buf.Append('\'');
								break;

							case '\"':
								if (chDelim == '\"')
									buf.Append("\\\"");
								else
									buf.Append('\"');
								break;

							case '\\':
								buf.Append("\\\\");
								break;

							default:
								if (char.IsControl(ch))
								{
									buf.AppendFormat("\\x{0:X2}", (int)ch);
								}
								else
								{
									buf.Append(ch);
								}
								break;
						}
					}
					else if (ch <= 255)
					{
						buf.AppendFormat("\\x{0:X2}", (int)ch);
					}
					else
					{
						buf.AppendFormat("\\u{0:X4}", (int)ch);
					}
				}

				// Closing quote
				buf.Append(chDelim);

				// Done
				dest.Append(buf.ToString());
				return;
			}

			// Is it an integer
			if (Value.GetType() == typeof(long))
			{
				// Try encoding as both hex and decimal and use the shorted
				long lVal = (long)Value;
				string strHex = "0x" + lVal.ToString("X");
				string strDec = lVal.ToString();
				if (strHex.Length < strDec.Length)
					dest.Append(strHex);
				else
					dest.Append(strDec);
				return;
			}

			// Is it a double
			if (Value.GetType() == typeof(DoubleLiteral))
			{
				// Use the original string
				dest.Append(((DoubleLiteral)Value).Original);
				return;
			}

			// Is it a bool
			if (Value.GetType() == typeof(bool))
			{
				dest.Append(((bool)Value) ? "true" : "false");
				return;
			}

		}

		public override bool Render(RenderContext dest)
		{
			RenderValue(dest, Value);
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
		}

		public override object EvalConstLiteral()
		{
			return Value;
		}

	}

	// Represents a regular expression eg: /regex/gim
	class ExprNodeRegEx : ExpressionNode
	{
		// Constructor
		public ExprNodeRegEx(Bookmark bookmark, string re) : base(bookmark)
		{
			RegEx = re;
		}

		// Attributes
		string RegEx;

		public override string ToString()
		{
			return String.Format("regular expression : {0}", RegEx);
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "regular expression: {0}", RegEx);
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.terminal;
		}

		public override bool Render(RenderContext dest)
		{
			dest.Append(RegEx);
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
		}

	}

	// Represents a binary operation (eg: x+y)
	class ExprNodeBinary : ExpressionNode
	{
		// Constructor
		public ExprNodeBinary(Bookmark bookmark, ExpressionNode lhs, ExpressionNode rhs, Token op) : base(bookmark)
		{
			Lhs = lhs;
			Rhs = rhs;
			Op = op;
		}

		// Attributes
		public ExpressionNode Lhs;
		public ExpressionNode Rhs;
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
			WrapAndRender(dest, Lhs);

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
			WrapAndRender(dest, Rhs);
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

	// Represents a unary operator (eg: ++, !, ~ etc...)
	class ExprNodeUnary : ExpressionNode
	{
		// Constrctor
		public ExprNodeUnary(Bookmark bookmark, ExpressionNode rhs, Token op) : base(bookmark)
		{
			Rhs = rhs;
			Op = op;
		}

		// Attributes
		public ExpressionNode Rhs;
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
			WrapAndRender(dest, Rhs);
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

	}

	// Represents a postfix increment or decrement
	class ExprNodePostfix : ExpressionNode
	{
		// Constructor
		public ExprNodePostfix(Bookmark bookmark, ExpressionNode lhs, Token op) : base(bookmark)
		{
			Lhs = lhs;
			Op = op;
		}

		// Attributes
		public ExpressionNode Lhs;
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
			return OperatorPrecedence.negation;
		}

		public override bool Render(RenderContext dest)
		{
			dest.DisableLineBreaks();

			WrapAndRender(dest, Lhs);
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

	}

	// Represents an array literal (eg: [1,2,3])
	class ExprNodeArrayLiteral : ExpressionNode
	{
		// Constructor
		public ExprNodeArrayLiteral(Bookmark bookmark) : base(bookmark)
		{

		}

		// Attributes
		public List<ExpressionNode> Values = new List<ExpressionNode>();

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
				WrapAndRender(dest, e);
			}
			dest.Append(']');
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var e in Values)
				e.Visit(visitor);
		}


	}

	// Key/value pair for object literal 
	class KeyExpressionPair
	{
		public KeyExpressionPair(object key, ExpressionNode value)
		{
			Key = key;
			Value = value;
		}
		public object Key;
		public ExpressionNode Value;
	}

	// Represents an object literal (eg: {a:1,b:2,c:3})
	class ExprNodeObjectLiteral : ExpressionNode
	{
		// Constructor
		public ExprNodeObjectLiteral(Bookmark bookmark) : base(bookmark)
		{

		}

		// List of values (NB: don't use a dictionary as we need to maintain order)
		public List<KeyExpressionPair> Values = new List<KeyExpressionPair>();

		public override string ToString()
		{
			return "<object literal>";
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "object literal:");
			foreach (var e in Values)
			{
				writeLine(indent + 1, e.Key.ToString());
				e.Value.Dump(indent + 1);
			}
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.terminal;
		}

		public override bool Render(RenderContext dest)
		{
			if (Values.Count == 0)
			{
				dest.Append("{}");
				return true;
			}

			dest.Append('{');
			dest.Indent();
			for (var i = 0; i < Values.Count; i++)
			{
				if (i > 0)
					dest.Append(',');

				dest.StartLine();

				// Key - if key is a valid identifier, don't quote it
				var kp = Values[i];
				if (kp.Key.GetType() == typeof(String) && Tokenizer.IsIdentifier((string)kp.Key) && !Tokenizer.IsKeyword((string)kp.Key))
				{
					dest.Append((string)kp.Key);
				}
				else
				{
					ExprNodeLiteral.RenderValue(dest, kp.Key);
				}

				// Value
				dest.Append(':');
				kp.Value.Render(dest);
			}
			dest.Unindent();
			dest.StartLine();
			dest.Append('}');
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var kp in Values)
			{
				kp.Value.Visit(visitor);
			}
		}

	}

	// Represents a condiditional expression eg: conditions ? true : false
	class ExprNodeConditional : ExpressionNode
	{
		// Constructor
		public ExprNodeConditional(Bookmark bookmark, ExpressionNode condition) : base(bookmark)
		{
			Condition = condition;
		}

		// Attributes
		public ExpressionNode Condition;
		public ExpressionNode TrueResult;
		public ExpressionNode FalseResult;

		public override void Dump(int indent)
		{
			writeLine(indent, "if:");
			Condition.Dump(indent + 1);
			writeLine(indent, "then:");
			TrueResult.Dump(indent + 1);
			writeLine(indent, "else:");
			FalseResult.Dump(indent + 1);
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.conditional;
		}

		public override bool Render(RenderContext dest)
		{
			WrapAndRender(dest, Condition);
			dest.Append('?');
			WrapAndRender(dest, TrueResult);
			dest.Append(':');
			WrapAndRender(dest, FalseResult);
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Condition.Visit(visitor);
			TrueResult.Visit(visitor);
			FalseResult.Visit(visitor);
		}

	}

	// Represents a comma separated composite expression
	class ExprNodeComposite : ExpressionNode
	{
		// Constrictor
		public ExprNodeComposite(Bookmark bookmark) : base(bookmark)
		{
		}

		// Attributes
		public List<ExpressionNode> Expressions = new List<ExpressionNode>();

		public override void Dump(int indent)
		{
			writeLine(indent, "composite expression:");
			foreach (var e in Expressions)
			{
				e.Dump(indent + 1);
			}
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.comma;
		}

		public override bool Render(RenderContext dest)
		{
			for (int i = 0; i < Expressions.Count; i++)
			{
				if (i > 0)
					dest.Append(',');

				WrapAndRender(dest, Expressions[i]);
			}
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var e in Expressions)
			{
				e.Visit(visitor);
			}
		}


	}

	// Represents a function declaration
	class ExprNodeFunction : ExpressionNode
	{
		// Constructor
		public ExprNodeFunction(Bookmark bookmark) : base(bookmark)
		{
		}

		// Attributes
		public string Name;
		public List<Parameter> Parameters = new List<Parameter>();
		public CodeBlock Code;


		public override string ToString()
		{
			return String.Format("function {0}", Name ?? "<anonymous>");
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "function `{0}`:", Name);
			foreach (var p in Parameters)
			{
				writeLine(indent + 1, p.ToString());
			}
			writeLine(indent, "body:");
			if (Code != null)
				Code.Dump(indent + 1);
			else
				writeLine(indent + 1, "<no implementation>");
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.function;
		}

		public override bool Render(RenderContext dest)
		{
			// Get obfuscated name before we enter our own scope
			string strObfuscatedName = dest.Symbols.GetObfuscatedSymbol(Name);

			// Enter a new symbol scope and tell symbol allocator
			// about our local symbols
			dest.EnterScope(Scope);

			// `function`
			dest.Append("function");

			// Function name not present for anonymous functions
			if (Name != null)
			{
				dest.Append(strObfuscatedName);
			}

			// Parameters
			dest.Append('(');
			for (int i = 0; i < Parameters.Count; i++)
			{
				if (i > 0)
					dest.Append(',');
				Parameters[i].Render(dest);
			}
			dest.Append(")");

			// Body of the function
			Code.Render(dest);

			// Clean up scope and we're finished
			dest.LeaveScope();
			return false;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var p in Parameters)
			{
				p.Visit(visitor);
			}
			Code.Visit(visitor);
		}


	}
}
