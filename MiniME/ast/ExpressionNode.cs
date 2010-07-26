using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	enum OperatorPrecedence
	{
		terminal,
		member,
		call,
		negation,
		multiply,
		add,
		bitshift,
		relational,
		equality,
		bitand,
		bitxor,
		bitor,
		logand,
		logor,
		conditional,
		assignment,
		comma,
	}

	abstract class ExpressionNode : Node
	{
		public abstract OperatorPrecedence GetPrecedence();

		public void WrapAndRender(RenderContext dest, ExpressionNode other)
		{
			if (other.GetPrecedence() > this.GetPrecedence())
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
	}

	// Represents a method/property/field name
	class ExprNodeMember : ExpressionNode
	{
		public ExprNodeMember(string name)
		{
			Name = name;
		}

		public ExprNodeMember(string name, ExpressionNode lhs)
		{
			Name = name;
			Lhs = lhs;
		}

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
			return OperatorPrecedence.member;
		}

		public override bool Render(RenderContext dest)
		{
			if (Lhs != null)
			{
				WrapAndRender(dest, Lhs);
				dest.Append(".");
				dest.Append(Name);
			}
			else
			{
				dest.Append(dest.Symbols.GetObfuscatedSymbol(Name));
			}
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			if (Lhs!=null)
				Lhs.Visit(visitor);
		}


	}

	class ExprNodeCall : ExpressionNode
	{
		public ExprNodeCall(ExpressionNode lhs)
		{
			Lhs = lhs;
		}

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
			return OperatorPrecedence.call;
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

	class ExprNodeIndexer : ExpressionNode
	{
		public ExprNodeIndexer(ExpressionNode lhs, ExpressionNode index)
		{
			Lhs = lhs;
			Index = index; 
		}

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
			return OperatorPrecedence.member;
		}

		public override bool Render(RenderContext dest)
		{
			WrapAndRender(dest, Lhs);
			dest.Append("[");
			Index.Render(dest);
			dest.Append("]");
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Lhs.Visit(visitor);
			Index.Visit(visitor);
		}

	}

	class ExprNodeLiteral : ExpressionNode
	{
		public ExprNodeLiteral(object value)
		{
			Value = value;
		}

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

		public static void RenderValue(RenderContext dest, object Value)
		{
			if (Value.GetType() == typeof(string))
			{
				// encode string
				string str = (string)Value;

				// Count quotes and double quotes
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

				dest.Append(chDelim);



				foreach (char ch in str)
				{
					if (ch < 127)
					{
						switch (ch)
						{
							case '\b':
								dest.Append("\\b");
								break;

							case '\f':
								dest.Append("\\f");
								break;

							case '\n':
								dest.Append("\\n");
								break;

							case '\r':
								dest.Append("\\r");
								break;

							case '\t':
								dest.Append("\\t");
								break;

							case '\'':
								if (chDelim == '\'')
									dest.Append("\\\'");
								else
									dest.Append('\'');
								break;

							case '\"':
								if (chDelim == '\"')
									dest.Append("\\\"");
								else
									dest.Append('\"');
								break;

							case '\\':
								dest.Append("\\\\");
								break;

							default:
								if (char.IsControl(ch))
								{
									dest.AppendFormat("\\x{0:X2}", (int)ch);
								}
								else
								{
									dest.Append(ch);
								}
								break;
						}
					}
					else if (ch <= 255)
					{
						dest.AppendFormat("\\x{0:X2}", (int)ch);
					}
					else
					{
						dest.AppendFormat("\\u{0:X4}", (int)ch);
					}
				}
				dest.Append(chDelim);
			}

			if (Value.GetType() == typeof(long))
			{
				long lVal = (long)Value;

				string strHex = "0x" + lVal.ToString("X");
				string strDec = lVal.ToString();

				if (strHex.Length < strDec.Length)
					dest.Append(strHex);
				else
					dest.Append(strDec);
			}

			if (Value.GetType() == typeof(double))
			{
				double dblVal = (double)Value;
				dest.Append(dblVal);
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


		object Value;
	}

	class ExprNodeRegEx : ExpressionNode
	{
		public ExprNodeRegEx(string re)
		{
			RegEx = re;
		}

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

		string RegEx;
	}

	class ExprNodeBinary : ExpressionNode
	{
		public ExprNodeBinary(ExpressionNode lhs, ExpressionNode rhs, Token op)
		{
			Lhs = lhs;
			Rhs = rhs;
			Op = op;
		}

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
					return OperatorPrecedence.equality;

				case Token.compareLT:
				case Token.compareLE:
				case Token.compareGT:
				case Token.compareGE:
				case Token.compareEQStrict:
				case Token.compareNEStrict:
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
			WrapAndRender(dest, Lhs);

			switch (Op)
			{
				case Token.add:				dest.Append("+"); break;
				case Token.subtract:		dest.Append("-"); break;
				case Token.multiply:		dest.Append("*"); break;
				case Token.divide:			dest.Append("/"); break;
				case Token.modulus: dest.Append("%"); break;
				case Token.shl: dest.Append("<<"); break;
				case Token.shr: dest.Append(">>"); break;
				case Token.assign: dest.Append("="); break;
				case Token.shrz: dest.Append(">>>"); break;
				case Token.addAssign: dest.Append("+="); break;
				case Token.subtractAssign: dest.Append("-="); break;
				case Token.multiplyAssign: dest.Append("*="); break;
				case Token.divideAssign: dest.Append("/="); break;
				case Token.modulusAssign: dest.Append("%="); break;
				case Token.shlAssign: dest.Append("<<="); break;
				case Token.shrAssign: dest.Append(">>="); break;
				case Token.shrzAssign: dest.Append(">>>="); break;
				case Token.bitwiseXorAssign: dest.Append("^="); break;
				case Token.bitwiseOrAssign: dest.Append("|="); break;
				case Token.bitwiseAndAssign: dest.Append("&="); break;
				case Token.compareEQ: dest.Append("=="); break;
				case Token.compareNE: dest.Append("!="); break;
				case Token.compareLT: dest.Append("<"); break;
				case Token.compareLE: dest.Append("<="); break;
				case Token.compareGT: dest.Append(">"); break;
				case Token.compareGE: dest.Append(">="); break;
				case Token.compareEQStrict: dest.Append("==="); break;
				case Token.compareNEStrict: dest.Append("!=="); break;
				case Token.bitwiseXor: dest.Append("^"); break;
				case Token.bitwiseOr: dest.Append("|"); break;
				case Token.bitwiseAnd: dest.Append("&"); break;
				case Token.logicalNot: dest.Append("!"); break;
				case Token.logicalOr: dest.Append("||"); break;
				case Token.logicalAnd: dest.Append("&&"); break;
				default:
					System.Diagnostics.Debug.Assert(false);
					break;
			}

			WrapAndRender(dest, Rhs);
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Lhs.Visit(visitor);
			Rhs.Visit(visitor);
		}

		ExpressionNode Lhs;
		ExpressionNode Rhs;
		Token Op;
	}

	class ExprNodeUnary : ExpressionNode
	{
		public ExprNodeUnary(ExpressionNode rhs, Token op)
		{
			Rhs = rhs;
			Op = op;
		}
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
				case Token.kw_new:
					return OperatorPrecedence.call;

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
				case Token.kw_new:
				case Token.kw_typeof:
				case Token.kw_void:
				case Token.kw_delete:
					dest.Append(Op.ToString().Substring(3));
					dest.Append(' ');
					break;

				case Token.bitwiseNot:
					dest.Append('~');
					break;

				case Token.logicalNot:
					dest.Append('!');
					break;

				case Token.add:
					dest.Append('+');
					break;

				case Token.subtract:
					dest.Append('-');
					break;

				case Token.increment:
					dest.Append("++");
					break;

				case Token.decrement:
					dest.Append("--");
					break;

				default:
					System.Diagnostics.Debug.Assert(false);
					break;
			}
			WrapAndRender(dest, Rhs);
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Rhs.Visit(visitor);
		}

		ExpressionNode Rhs;
		Token Op;
	}

	class ExprNodePostfix : ExpressionNode
	{
		public ExprNodePostfix(ExpressionNode lhs, Token op)
		{
			Lhs = lhs;
			Op = op;
		}
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
			WrapAndRender(dest, Lhs);
			switch (Op)
			{
				case Token.increment:
					dest.Append("++");
					break;

				case Token.decrement:
					dest.Append("--");
					break;

				default:
					System.Diagnostics.Debug.Assert(false);
					break;
			}
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Lhs.Visit(visitor);
		}

		ExpressionNode Lhs;
		Token Op;
	}

	class ExprNodeArrayLiteral : ExpressionNode
	{
		public ExprNodeArrayLiteral()
		{

		}

		public override string ToString()
		{
			return "<array literal>";
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "array literal:");
			foreach (var e in Expressions)
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

		public List<ExpressionNode> Expressions = new List<ExpressionNode>();


		public override bool Render(RenderContext dest)
		{
			dest.Append('[');
			foreach (var e in Expressions)
			{
				WrapAndRender(dest, e);
			}
			dest.Append(']');
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var e in Expressions)
				e.Visit(visitor);
		}


	}

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

	class ExprNodeObjectLiteral : ExpressionNode
	{
		public ExprNodeObjectLiteral()
		{

		}

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
			dest.Append('{');
			for (var i = 0; i < Values.Count; i++)
			{
				if (i > 0)
					dest.Append(',');

				var kp = Values[i];

				if (kp.Key.GetType() == typeof(String) && Tokenizer.IsIdentifier((string)kp.Key))
				{
					dest.Append((string)kp.Key);
				}
				else
				{
					ExprNodeLiteral.RenderValue(dest, kp.Key);
				}
				dest.Append(':');
				kp.Value.Render(dest);
			}
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

		public List<KeyExpressionPair> Values = new List<KeyExpressionPair>();
	}

	class ExprNodeConditional : ExpressionNode
	{
		public ExprNodeConditional(ExpressionNode condition)
		{
			Condition = condition;
		}
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

	class ExprNodeComposite : ExpressionNode
	{
		public ExprNodeComposite()
		{
		}
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

	class ExprNodeFunction : ExpressionNode
	{
		public ExprNodeFunction()
		{
		}

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
			if (Body != null)
				Body.Dump(indent + 1);
			else
				writeLine(indent + 1, "<no implementation>");
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.terminal;
		}

		public override bool Render(RenderContext dest)
		{
			// Enter a new symbol scope
			dest.Symbols.EnterScope();

			// Render the function
			Scope.ObfuscateSymbols(dest);

			if (dest.Formatted)
			{
				dest.StartLine();
			}

			dest.Append("function");
			if (Name != null)
			{
				dest.Append(' ');
				dest.Append(dest.Symbols.GetObfuscatedSymbol(Name));
			}
			dest.Append('(');
			for (int i = 0; i < Parameters.Count; i++)
			{
				if (i > 0)
					dest.Append(',');
				Parameters[i].Render(dest);
			}
			dest.Append(")");
			Body.Render(dest);


			// Leave the symbol scope
			dest.Symbols.LeaveScope();
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var p in Parameters)
			{
				p.Visit(visitor);
			}
			Body.Visit(visitor);
		}


		public string Name;
		public List<Parameter> Parameters = new List<Parameter>();
		public Statement Body;
	}
}
