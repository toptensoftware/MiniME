using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	enum Token
	{
		eof,			// End of file
		comment,		// C or C++ style comment
		literal,

		identifier,

		assign,

		add,
		subtract,
		multiply,
		divide,
		modulus,
		shl,
		shr,
		shrz,

		addAssign,
		subtractAssign,
		multiplyAssign,
		divideAssign,
		modulusAssign,
		shlAssign,
		shrAssign,
		shrzAssign,

		increment,
		decrement,

		compareEQ,
		compareNE,
		compareLT,
		compareLE,
		compareGT,
		compareGE,
		compareEQStrict,
		compareNEStrict,

		openRound,
		closeRound,
		openBrace,
		closeBrace,
		openSquare,
		closeSquare,

		bitwiseXor,
		bitwiseOr,
		bitwiseAnd,
		bitwiseNot,

		bitwiseXorAssign,
		bitwiseOrAssign,
		bitwiseAndAssign,

		logicalNot,
		logicalOr,
		logicalAnd,

		ternary,			// ?
		colon,				// :
		semicolon,			// ;
		comma,				// ,

		memberDot,

		kw_void,
		kw_delete,
		kw_new,
		kw_in,
		kw_instanceof,
		kw_typeof,
		kw_var,
		kw_return,
		kw_if,
		kw_switch,
		kw_case,
		kw_default,
		kw_break,
		kw_continue,
		kw_else,
		kw_for,
		kw_do,
		kw_while,
		kw_with,
		kw_throw,
		kw_try,
		kw_catch,
		kw_finally,
		kw_function,
	}

	internal class DoubleLiteral
	{
		public double Value;
		public string Original;
	}

	internal class Tokenizer
	{
		internal Tokenizer(string str, string strFileName)
		{
			m_strFileName = strFileName;
			BuildKeywordMap();
			p = new StringScanner();
			p.Reset(str);
			Next();
		}

		internal string FileName
		{
			get
			{
				return m_strFileName;
			}
		}

		internal Token token
		{
			get
			{
				return m_currentToken;
			}
		}

		internal bool more
		{
			get
			{
				return m_currentToken != Token.eof;
			}
		}

		internal int currentOffset
		{
			get
			{
				return p.position;
			}
		}

		internal int currentLine
		{
			get
			{
				int unused;
				return p.LineNumberFromOffset(p.position, out unused);
			}
		}

		internal int currentLinePosition
		{
			get
			{
				int chpos;
				p.LineNumberFromOffset(p.position, out chpos);
				return chpos;
			}
		}

		internal string RawToken
		{
			get
			{
				return p.Substring(m_tokenStart, m_tokenEnd - m_tokenStart);
			}
		}

		internal object literal
		{
			get
			{
				return m_literal;
			}
		}

		internal string identifier
		{
			get
			{
				return m_strIdentifier;
			}
		}

		internal bool IsAutoSemicolon()
		{
			if (p.eof)
				return true;

			// Scan backwards from the current token and look for a linebreak
			var pos = m_tokenStart;
			while (pos > 0)
			{
				// Ignore line space
				if (StringScanner.IsLineSpace(p.input[pos - 1]))
				{
					pos--;
					continue;
				}

				// Is it a line end?
				if (StringScanner.IsLineEnd(p.input[pos - 1]))
				{
					return true;
				}

				return false;
			}

			return true;
		}

		internal bool SkipOptional(Token t)
		{
			if (token != t)
			{
				// Automatic semicolons?
				if (t == Token.semicolon && IsAutoSemicolon())
					return true;

				return false;
			}

			Next();
			return true;
		}

		internal void SkipRequired(Token t)
		{
			// Automatic semicolons?
			if (t == Token.semicolon && token != Token.semicolon)
			{
				if (IsAutoSemicolon())
					return;
			}

			Require(t);
			Next();
		}

		internal void Require(Token t)
		{
			if (token != t)
			{
				throw new CompileError(string.Format("Syntax error, expected {0}", t.ToString()), this);
			}
		}

		internal string ParseRegEx()
		{
			// Rewind to start of the expression
			p.position = m_tokenStart;

			System.Diagnostics.Debug.Assert(p.current=='/');

			p.Mark();

			// Skip opening slash
			p.SkipForward(1);

			while (!p.eol)
			{
				if (p.current == '/')
					break;

				if (p.current=='\\')
					p.SkipForward(2);
				else
					p.SkipForward(1);
			}

			// Check end found
			if (p.current != '/')
			{
				p.position=m_tokenStart;
				throw new CompileError("Syntax error - unterminated regular expression", this);
			}

			p.SkipForward(1);

			// Flags
			while (p.current=='g' || p.current=='i' || p.current=='m' || p.current=='y')
				p.SkipForward(1);

			// Check no more characters
			if (IsIdentifierChar(p.current))
			{
				p.position = m_tokenStart;
				throw new CompileError("Syntax error - unexpected characters after regular expression", this);
			}

			// Get the regex verbatim
			string regex = p.Extract();

			// Scan the next token
			Next();

			// Done
			return regex;
		}

		internal Token Next()
		{
			// Grab next non-comment token
			do
			{
				m_currentToken = ParseToken();
			} while (m_currentToken == Token.comment);

			m_tokenEnd = p.position;
			return m_currentToken;
		}

		internal Token ParseToken()
		{
			p.SkipWhitespace();

			// check for eof
			if (p.eof)
				return Token.eof;

			// Save current position
			m_tokenStart = p.position;

			char ch=p.current;

			// Digit?
			if (ch >= '0' && ch <= '9')
			{
				return ParseNumber();
			}

			// Identifier
			if (IsIdentifierLeadChar(ch))
			{
				ParseIdentifier();
				return MapIdentifierToKeyword();
			}


			// C style comment
			switch (ch)
			{
				case '@':
					if (IsIdentifierLeadChar(p.CharAtOffset(1)))
					{
						p.SkipForward(1);
						ParseIdentifier();
						return Token.identifier;
					}
					if (p.CharAtOffset(1) == '\"')
					{
						return ParseRawString();
					}
					break;

				case '.':
					p.SkipForward(1);
					if (p.current >= '0' && p.current <= '9')
					{
						p.SkipForward(-1);
						return ParseNumber();
					}
					return Token.memberDot;

				case '/':
					p.SkipForward(1);

					switch (p.current)
					{
						case '*':
							if (!p.Find("*/"))
							{
								p.position = m_tokenStart;
								throw new CompileError("Syntax error - unterminated C-style comment", this);
							}

							p.SkipForward(2);
							return Token.comment;

						case '/':
							p.SkipToEol();
							return Token.comment;

						case '=':
							p.SkipForward(1);
							return Token.divideAssign;
					}

					return Token.divide;

				case '+':
					p.SkipForward(1);
					if (p.current == '=')
					{
						p.SkipForward(1);
						return Token.addAssign;
					}
					if (p.current == '+')
					{
						p.SkipForward(1);
						return Token.increment;
					}
					return Token.add;

				case '-':
					p.SkipForward(1);
					if (p.current == '=')
					{
						p.SkipForward(1);
						return Token.subtractAssign;
					}
					if (p.current == '-')
					{
						p.SkipForward(1);
						return Token.decrement;
					}
					return Token.subtract;

				case '*':
					p.SkipForward(1);
					if (p.current == '=')
					{
						p.SkipForward(1);
						return Token.multiplyAssign;
					}
					return Token.multiply;

				case '%':
					p.SkipForward(1);
					if (p.current == '=')
					{
						p.SkipForward(1);
						return Token.modulusAssign;
					}
					return Token.modulus;

				case '<':
					p.SkipForward(1);
					if (p.current == '<')
					{
						// <<
						p.SkipForward(1);
						if (p.current == '=')
						{
							// <<=
							p.SkipForward(1);
							return Token.shlAssign;
						}

						return Token.shl;
					}
					if (p.current == '=')
					{
						// <=
						p.SkipForward(1);
						return Token.compareLE;
					}

					// <
					return Token.compareLT;

				case '>':
					p.SkipForward(1);
					if (p.current == '>')
					{
						// >>
						p.SkipForward(1);
						if (p.current == '>')
						{
							// >>>
							p.SkipForward(1);
							if (p.current == '=')
							{
								// >>>=
								p.SkipForward(1);
								return Token.shrzAssign;
							}
							return Token.shrz;
						}

						if (p.current == '=')
						{
							p.SkipForward(1);
							return Token.shrAssign;
						}

						return Token.shr;
					}

					if (p.current == '=')
					{
						// >=
						p.SkipForward(1);
						return Token.compareGE;
					}

					return Token.compareGT;

				case '{':
					p.SkipForward(1);
					return Token.openBrace;

				case '}':
					p.SkipForward(1);
					return Token.closeBrace;

				case '[':
					p.SkipForward(1);
					return Token.openSquare;

				case ']':
					p.SkipForward(1);
					return Token.closeSquare;

				case '(':
					p.SkipForward(1);
					return Token.openRound;

				case ')':
					p.SkipForward(1);
					return Token.closeRound;

				case '&':
					p.SkipForward(1);
					if (p.current == '&')
					{
						p.SkipForward(1);
						return Token.logicalAnd;
					}
					if (p.current == '=')
					{
						p.SkipForward(1);
						return Token.bitwiseAndAssign;
					}
					return Token.bitwiseAnd;

				case '|':
					p.SkipForward(1);
					if (p.current == '|')
					{
						p.SkipForward(1);
						return Token.logicalOr;
					}
					if (p.current == '=')
					{
						p.SkipForward(1);
						return Token.bitwiseOrAssign;
					}
					return Token.bitwiseOr;

				case '~':
					p.SkipForward(1);
					return Token.bitwiseNot;

				case '!':
					p.SkipForward(1);
					if (p.current == '=')
					{
						p.SkipForward(1);
						if (p.current == '=')
						{
							p.SkipForward(1);
							return Token.compareNEStrict;
						}
						return Token.compareNE;
					}
					return Token.logicalNot;

				case '^':
					p.SkipForward(1);
					if (p.current == '=')
					{
						p.SkipForward(1);
						return Token.bitwiseXorAssign;
					}
					return Token.bitwiseXor;

				case '=':
					p.SkipForward(1);
					if (p.current == '=')
					{
						p.SkipForward(1);
						if (p.current == '=')
						{
							p.SkipForward(1);
							return Token.compareEQStrict;
						}
						return Token.compareEQ;
					}
					return Token.assign;

				case '?':
					p.SkipForward(1);
					return Token.ternary;

				case ':':
					p.SkipForward(1);
					return Token.colon;

				case ',':
					p.SkipForward(1);
					return Token.comma;

				case ';':
					p.SkipForward(1);
					return Token.semicolon;

				case '\'':
				case '\"':
					return ParseString();
			}

			throw new CompileError(string.Format("Syntax error, unrecognized character: '{0}'", p.current), this);
		}

		Token ParseRawString()
		{
			sb.Length = 0;
			p.SkipForward(2);		// The opening @"
			while (!p.eof)
			{
				if (p.current == '\"')
				{
					if (p.CharAtOffset(1) == '\"')
					{
						sb.Append('\"');
						p.SkipForward(2);
					}
					else
					{
						p.SkipForward(1);
						m_literal = sb.ToString();
						return Token.literal;
					}
				}
				else
				{
					sb.Append(p.current);
					p.SkipForward(1);
				}
			}

			p.position = m_tokenStart;
			throw new CompileError("Syntax error - unterminated string literal", this);
		}

		Token ParseString()
		{
			char chTerminator = p.current;
			p.SkipForward(1);

			sb.Length = 0;
			while (!p.eol)
			{
				var ch = p.current;

				if (ch == chTerminator)
					break;

				if (ch == '\\')
				{
					p.SkipForward(1);

					if (OctalDigit(p.current) >= 0)
					{
						// Up to three octal digits
						int val = 0;
						for (int i = 0; i < 3; i++)
						{
							int digit = OctalDigit(p.current);
							if (digit < 0)
								break;
							val = val * 8 + digit;
							p.SkipForward(1);
						}
						sb.Append((char)val);
					}
					else
					{
						switch (p.current)
						{
							case 'b':
								sb.Append('\b');
								break;

							case 'f':
								sb.Append('\f');
								break;

							case 'n':
								sb.Append('\n');
								break;

							case 'r':
								sb.Append('\r');
								break;

							case 't':
								sb.Append('\t');
								break;

							case '\'':
								sb.Append('\'');
								break;

							case '\"':
								sb.Append('\"');
								break;

							case '\\':
								sb.Append('\\');
								break;

							case 'x':
								{
									p.SkipForward(1);

									// Need two hex digits
									int val = 0;
									for (int i = 0; i < 2; i++)
									{
										int digit = HexDigit(p.current);
										if (digit < 0)
										{
											throw new CompileError(string.Format("Syntax error - invalid character in string literal - {0}", p.current), this);
										}
										val = val * 16 + digit;
										p.SkipForward(1);
									}
									sb.Append((char)val);
									continue;
								}

							case 'u':
								{
									p.SkipForward(1);

									// Need four hex digits
									int val = 0;
									for (int i = 0; i < 4; i++)
									{
										int digit = HexDigit(p.current);
										if (digit < 0)
										{
											throw new CompileError(string.Format("Syntax error - invalid character in string literal - {0}", p.current), this);
										}
										val = val * 16 + digit;
										p.SkipForward(1);
									}
									sb.Append((char)val);
									continue;
								}

							default:
								throw new CompileError(string.Format("Syntax error - unrecognised string escape character: '{0}'", p.current), this);
						}
						p.SkipForward(1);
					}
				}
				else
				{
					sb.Append(ch);
					p.SkipForward(1);
				}
			}

			if (p.current == chTerminator)
			{
				p.SkipForward(1);
				m_literal = sb.ToString();
				return Token.literal;
			}

			p.position = m_tokenStart;
			throw new CompileError("Syntax error - unterminated string literal", this);
		}

		internal static bool IsIdentifier(string str)
		{
			if (String.IsNullOrEmpty(str))
				return false;

			if (!IsIdentifierLeadChar(str[0]))
				return false;

			for (int i = 1; i < str.Length; i++)
			{
				if (!IsIdentifierChar(str[i]))
					return false;
			}

			return true;
		}

		internal static bool IsIdentifierLeadChar(char ch)
		{
			return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch == '_') || (ch == '$');
		}

		internal static bool IsIdentifierChar(char ch)
		{
			return IsIdentifierLeadChar(ch) || (ch >= '0' && ch <= '9');
		}

		internal bool ParseIdentifier()
		{
			p.Mark();
			if (!IsIdentifierLeadChar(p.current))
				return false;

			p.SkipForward(1);

			while (IsIdentifierChar(p.current))
				p.SkipForward(1);

			m_strIdentifier = p.Extract();

			return true;
		}

		internal Dictionary<string, Token> m_mapKeywords;

		void BuildKeywordMap()
		{
			m_mapKeywords=new Dictionary<string,Token>();
			var names=Enum.GetNames(typeof(Token));
			var vals=Enum.GetValues(typeof(Token));
			for (var i=0; i<names.Length; i++)
			{
				if (names[i].StartsWith("kw_"))
				{
					m_mapKeywords.Add(names[i].Substring(3), (Token)vals.GetValue(i));
				}
			}
		}

		Token MapIdentifierToKeyword()
		{
			if (m_strIdentifier == "true")
			{
				m_literal = true;
				return Token.literal;
			}
			if (m_strIdentifier == "false")
			{
				m_literal = false;
				return Token.literal;
			}

			Token t;
			if (m_mapKeywords.TryGetValue(m_strIdentifier, out t))
				return t;
			else
				return Token.identifier;
		}

		internal Token ParseNumber()
		{
			// Base 10 by default
			int b=10;
			int startPos = p.position;

			if (p.current == '0')
			{
				if (p.CharAtOffset(1) == 'x' || p.CharAtOffset(1) == 'X')
				{
					p.SkipForward(2);
					b = 16;
					startPos = p.position;
					while (HexDigit(p.current) >= 0)
						p.SkipForward(1);
				}
				else
				{
					while (p.current == '0')
						p.SkipForward(1);

					if (p.current != '.' && p.current != 'e' && p.current != 'E')
					{
						b = 8;
						while (OctalDigit(p.current) >= 0)
							p.SkipForward(1);
					}
				}
			}

			if (b==10)
			{
				// Leading digits
				while (DecimalDigit(p.current) >= 0)
					p.SkipForward(1);

				// Decimal point
				if (p.current == '.' && DecimalDigit(p.CharAtOffset(1)) >= 0)
				{
					b = 0;
					p.SkipForward(2);
					while (DecimalDigit(p.current) >= 0)
						p.SkipForward(1);
				}

				// Exponent
				if (p.current == 'E' || p.current == 'e')
				{
					b = 0;
					p.SkipForward(1);
					if (p.current == '+' || p.current == '-')
						p.SkipForward(1);

					while (DecimalDigit(p.current) >= 0)
					{
						p.SkipForward(1);
					}
				}
			}


			string str = p.Substring(startPos, p.position - startPos);
			if (b == 0)
			{
				double temp;
				if (!double.TryParse(str, out temp))
				{
					p.position = m_tokenStart;
					throw new CompileError("Syntax error - incorrectly formatted decimal literal", this);
				}

				// Need to store both the parsed value and the original value.
				// Original value is used to re-write on render without losing/changing
				// the value
				DoubleLiteral l = new DoubleLiteral();
				l.Value = temp;
				l.Original = str;

				m_literal = l;
				return Token.literal;
			}
			else
			{
				try
				{
					m_literal = Convert.ToInt64(str, b);
					return Token.literal;
				}
				catch (Exception)
				{
					p.position = m_tokenStart;
					throw new CompileError("Syntax error - incorrectly formatted integer literal", this);
				}
			}
		}

		static int HexDigit(char ch)
		{
			if (ch >= '0' && ch <= '9')
				return 0 + ch - '0';
			if (ch >= 'A' && ch <= 'F')
				return 0xA + ch - 'A';
			if (ch >= 'a' && ch <= 'f')
				return 0xA + ch - 'a';
			return -1;
		}

		static int OctalDigit(char ch)
		{
			if (ch >= '0' && ch <= '7')
				return 0 + ch - '0';
			return -1;
		}

		static int DecimalDigit(char ch)
		{
			if (ch >= '0' && ch <= '9')
				return 0 + ch - '0';
			return -1;
		}

		public int Mark()
		{
			return m_tokenStart;
		}

		public void Rewind(int mark)
		{
			// Rewind and reparse
			p.position = mark;
			Next();
		}

		String m_strFileName;
		StringScanner p;
		StringBuilder sb = new StringBuilder();
		Token m_currentToken;
		string m_strIdentifier;
		int m_tokenStart;
		int m_tokenEnd;
		object m_literal;
	}
}


/*
				while (t.more)
				{
					Console.WriteLine("{0} - `{1}`", t.token.ToString(), t.RawToken);
					switch (t.token)
					{
						case Token.literalString:
							Console.WriteLine("\tString=`{0}`", t.literalString);
							break;

						case Token.literalDouble:
							Console.WriteLine("\tDouble={0}", t.literalDouble);
							break;				

						case Token.literalInteger:
							Console.WriteLine("\tInteger={0}", t.literalInteger);
							break;

						case Token.identifier:
							Console.WriteLine("\tIdenfifier={0}", t.literalString);
							break;
					}
					t.Next();
				}
*/