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
	// Represents a literal string, integer or double
	class ExprNodeLiteral : ExprNode
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
}
