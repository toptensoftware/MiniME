using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	class Bookmark
	{
		public Bookmark(StringScanner file, int position, Token token, bool warnings)
		{
			this.file = file;
			this.position = position;
			this.token = token;
			this.warnings = warnings;
		}

		public StringScanner file;
		public int position;
		public Token token;
		public bool warnings;

		public override string ToString()
		{
			int offset;
			int line = file.LineNumberFromOffset(position, out offset);
			return String.Format("{0}({1},{2})", file.FileName, line + 1, offset + 1);
		}
	}

	enum Token
	{
		eof,			// End of file
		comment,		// C or C++ style comment (filtered out internally to tokenizer)
		literal,
		identifier,

		directive_private,			// Comment of form /* private */
		directive_public,			// Comment of form /* public */
		directive_comment,			// Comment of form //! or /*!

		// Operators
		assign,
		addAssign,
		subtractAssign,
		multiplyAssign,
		divideAssign,
		modulusAssign,
		shlAssign,
		shrAssign,
		shrzAssign,
		bitwiseXorAssign,
		bitwiseOrAssign,
		bitwiseAndAssign,
		increment,
		decrement,
		add,
		subtract,
		multiply,
		divide,
		modulus,
		shl,
		shr,
		shrz,
		compareEQ,
		compareNE,
		compareLT,
		compareLE,
		compareGT,
		compareGE,
		compareEQStrict,
		compareNEStrict,
		bitwiseXor,
		bitwiseOr,
		bitwiseAnd,
		bitwiseNot,
		logicalNot,
		logicalOr,
		logicalAnd,

		// Special symbols
		question,			// ?
		colon,				// :
		semicolon,			// ;
		comma,				// ,
		openRound,
		closeRound,
		openBrace,
		closeBrace,
		openSquare,
		closeSquare,
		period,

		// Keywords
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
		kw_fallthrough,
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
		kw_class,
		kw_debugger,
	}

	// Simple class to store a double value and it's original
	// string representation.  Used to pass through doubles without
	// changing precision.
	public class DoubleLiteral
	{
		public double Value;
		public string Original;
	}

	// Tokenizer
	//  - a fairly standard approach to input stream tokenization
	//  - tokenizes all Javascript tokens
	//  - special case for regex:
	//     - parser must know when a regex is allowed, check for Token.divide
	//       and then call ParseRegex, which simply returns the entire regex 
	//       as a string
	//  - uses a StringScanner for maintaining position in input
	class Tokenizer
	{
		static Tokenizer()
		{
			// Build a map of keyword identifiers to token enums
			BuildKeywordMap();
		}

		// Constructor
		public Tokenizer(string str, string strFileName, bool bWarnings)
		{
			// Prep the string scanner
			p = new StringScanner();
			p.Reset(str);
			p.FileName = strFileName;

			m_bWarnings = bWarnings;

			// Used to detect line breaks between tokens for automatic
			// semicolon insertion
			m_bPreceededByLineBreak = true;

			// Queue up the first token
			m_prevToken = Token.eof;
			Next();
		}


		public void OpenIncludeFile(string text, string fileName)
		{
			// Save the current string parser
			m_IncludeStack.Push(p);

			// Prep the string scanner
			p = new StringScanner();
			p.Reset(text);
			p.FileName = fileName;

			// Used to detect line breaks between tokens for automatic
			// semicolon insertion
			m_bPreceededByLineBreak = true;
			m_prevTokenEnd = 0;
		}

		public Bookmark GetBookmark()
		{
			return new Bookmark(p, m_tokenStart, this.token, m_bWarnings);
		}

		public bool Warnings
		{
			get
			{
				return m_bWarnings;
			}
		}

		// Rewind the tokenizer to a previously marked position
		public void Rewind(Bookmark bmk)
		{
			System.Diagnostics.Debug.Assert(bmk.file == this.p);

			p.position = bmk.position;
			Next();
		}

		// Helper to convert a token back to it's original form
		public static string FormatToken(Token token)
		{
			switch (token)
			{
				case Token.eof: return "end-of-file";
				case Token.assign: return "=";
				case Token.add: return "+";
				case Token.subtract: return "-";
				case Token.multiply: return "*";
				case Token.divide: return "/";
				case Token.modulus: return "%";
				case Token.shl: return "<<";
				case Token.shr: return ">>";
				case Token.shrz: return ">>>";
				case Token.addAssign: return "+=";
				case Token.subtractAssign: return "-=";
				case Token.multiplyAssign: return "*=";
				case Token.divideAssign: return "/=";
				case Token.modulusAssign: return "*=";
				case Token.shlAssign: return "<<=";
				case Token.shrAssign: return ">>=";
				case Token.shrzAssign: return ">>>=";
				case Token.increment:return "++";
				case Token.decrement: return "--";
				case Token.compareEQ: return "==";
				case Token.compareNE: return "!=";
				case Token.compareLT: return "<";
				case Token.compareLE: return "<=";
				case Token.compareGT: return ">";
				case Token.compareGE:return ">=";
				case Token.compareEQStrict:return "===";
				case Token.compareNEStrict:return "!==";
				case Token.bitwiseXor:return "^";
				case Token.bitwiseOr:return "|";
				case Token.bitwiseAnd:return "&";
				case Token.bitwiseNot:return "~";
				case Token.bitwiseXorAssign: return "^=";
				case Token.bitwiseOrAssign:return "|=";
				case Token.bitwiseAndAssign:return "&=";
				case Token.logicalNot:return "!";
				case Token.logicalOr:return "||";
				case Token.logicalAnd:return "&&";
				case Token.question:return "?";
				case Token.colon:return ":";
				case Token.semicolon: return ";";
				case Token.comma:return ",";
				case Token.openRound:return "(";
				case Token.closeRound:return ")";
				case Token.openBrace:return "{";
				case Token.closeBrace:return "}";
				case Token.openSquare:return "[";
				case Token.closeSquare:return "]";
				case Token.period:return ".";
			}

			var s=token.ToString();
			if (s.StartsWith("kw_"))
				return s.Substring(3);

			return s;
		}

		// Get a text representation of the current token 
		//  - typically used for descriptive error messages
		public string DescribeCurrentToken()
		{
			switch (token)
			{
				case Token.identifier:
				case Token.literal:
					return string.Format("{0} - `{1}`", FormatToken(token), RawToken);
			}
			return string.Format("`{0}`", FormatToken(token));
		}

		// The current token
		public Token token
		{
			get
			{
				return m_currentToken;
			}
		}

		// Check if more input is available
		public bool more
		{
			get
			{
				return m_currentToken != Token.eof && m_IncludeStack.Count==0;
			}
		}

		// Get the offset of the current token
		public int currentOffset
		{
			get
			{
				return m_tokenStart;
			}
		}

		// Get the line number of the current token
		public int currentLine
		{
			get
			{
				int unused;
				return p.LineNumberFromOffset(m_tokenStart, out unused);
			}
		}

		// Get the line offset of the current token
		public int currentLinePosition
		{
			get
			{
				int chpos;
				p.LineNumberFromOffset(m_tokenStart, out chpos);
				return chpos;
			}
		}

		public int LineNumberFromOffset(int position, out int lineoffset)
		{
			return p.LineNumberFromOffset(position, out lineoffset);
		}

		// Get the raw text from which the current token was parsed
		public string RawToken
		{
			get
			{
				return p.Substring(m_tokenStart, m_tokenEnd - m_tokenStart);
			}
		}

		// Get the value of a literal token
		public object literal
		{
			get
			{
				System.Diagnostics.Debug.Assert(token == Token.literal);
				return m_literal;
			}
		}

		// Get the name of an identifier token
		public string identifier
		{
			get
			{
				return m_strIdentifier;
			}
		}

		// Check for cases where a semicolon is optional
		//  - end of file
		//  - immediately before a brace
		//  - before a line break
		public bool IsAutoSemicolon()
		{
			if (p.eof || token == Token.closeBrace || m_bPreceededByLineBreak)
			{
				if (m_prevToken!=Token.closeBrace)
				{
					if (m_bWarnings)
					{
						var b = new Bookmark(p, m_prevTokenEnd, token, m_bWarnings);
						b.file = this.p;
						b.position = this.m_prevTokenEnd;
						b.token = this.token;
						Console.WriteLine("{0}: warning: missing semicolon", b);
					}
				}
				return true;
			}

			return false;
		}

		// Check for an optional token, skipping it if found
		public bool SkipOptional(Token t)
		{
			if (token != t)
			{
				// Automatic semicolons?
				if (t == Token.semicolon && IsAutoSemicolon())
				{
					return true;
				}

				return false;
			}

			Next();
			return true;
		}

		// Check for a required token, skipping it if found, raising an
		// error if not found
		public void SkipRequired(Token t)
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

		// Raise an error if the current token isn't of the specified type
		public void Require(Token t)
		{
			if (token != t)
			{
				throw new CompileError(string.Format("Syntax error - expected {0} but found {1}", FormatToken(t), DescribeCurrentToken()), this);
			}
		}

		// Parse a regex string
		public string ParseRegEx()
		{
			// Rewind to start of the current token
			p.position = m_tokenStart;

			// Must start with a slash
			System.Diagnostics.Debug.Assert(p.current=='/');

			// Mark the position and skip the opening slash
			p.Mark();
			p.SkipForward(1);

			// Find the end
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

			// Closing slash
			p.SkipForward(1);

			// Flags
			while (p.current=='g' || p.current=='i' || p.current=='m' || p.current=='y')
				p.SkipForward(1);

			// Check no more characters
			if (IsIdentifierChar(p.current))
			{
				p.position = m_tokenStart;
				throw new CompileError(string.Format("Syntax error - unexpected character `{0}` after regular expression", p.current), this);
			}

			// Get the regex verbatim
			string regex = p.Extract();

			// Scan the next token
			Next();

			// Done
			return regex;
		}

		// Get the next token, skipping comments as we go
		public Token Next()
		{
			m_prevToken = m_currentToken;
			do
			{
				m_currentToken = ParseToken();

				if (m_currentToken == Token.eof && m_IncludeStack.Count > 0)
				{
					// Pop include stack
					p = m_IncludeStack.Pop();
					m_prevTokenEnd = p.position;
					return Next();
				}


				// Check for directive comments
				if (m_currentToken == Token.comment)
				{
					if (m_strIdentifier.StartsWith("!"))
					{
						m_currentToken = Token.directive_comment;
						break;
					}

					string str = m_strIdentifier.Trim();

					if (str=="fall through")
					{
						m_currentToken=Token.kw_fallthrough;
						break;
					}

					if (str.StartsWith("private:"))
					{
						m_currentToken = Token.directive_private;
						m_strIdentifier = str.Substring(8);
						break;
					}
					if (str.StartsWith("public:"))
					{
						m_currentToken = Token.directive_public;
						m_strIdentifier = str.Substring(7);
						break;
					}

					if (str.StartsWith("include:"))
					{
						// Get file name
						string strFile = str.Substring(8).Trim();

						// Work out fully qualified name, relative to current file being processed
						string strDir = System.IO.Path.GetDirectoryName(p.FileName);
						string strOldDir = System.IO.Directory.GetCurrentDirectory();
						System.IO.Directory.SetCurrentDirectory(strDir);
						strFile = System.IO.Path.GetFullPath(strFile);
						System.IO.Directory.SetCurrentDirectory(strOldDir);

						// Open the include file
						OpenIncludeFile(System.IO.File.ReadAllText(strFile), strFile);

						// Recurse
						return Next();
					}
				}

			} while (m_currentToken == Token.comment);

			m_prevTokenEnd = m_tokenEnd;
			m_tokenEnd = p.position;
			return m_currentToken;
		}

		// Main token parser
		public Token ParseToken()
		{
			// Skip whitespace, but remember if there were any line breaks
			bool bOldPreceededByLineBreak = m_bPreceededByLineBreak;
			m_bPreceededByLineBreak = false;
			while (char.IsWhiteSpace(p.current))
			{
				if (p.eol)
					m_bPreceededByLineBreak = true;
				p.SkipForward(1);
			}

			// check for eof
			if (p.eof)
			{
				return Token.eof;
			}

			// Save current position
			m_tokenStart = p.position;

			char ch=p.current;

			// Digit?
			if (ch >= '0' && ch <= '9')
			{
				return ParseNumber();
			}

			// Identifier?
			if (IsIdentifierLeadChar(ch))
			{
				ParseIdentifier();
				return MapIdentifierToKeyword();
			}

			// Characters...
			switch (ch)
			{
				case '.':
					// Decimal point without leading digits
					p.SkipForward(1);
					if (p.current >= '0' && p.current <= '9')
					{
						p.SkipForward(-1);
						return ParseNumber();
					}
					return Token.period;

				case '/':
					p.SkipForward(1);
					switch (p.current)
					{
						case '*':
							p.SkipForward(1);
							p.Mark();
							if (!p.Find("*/"))
							{
								p.position = m_tokenStart;
								throw new CompileError("Syntax error - unterminated C-style comment", this);
							}
							m_strIdentifier = p.Extract();

							p.SkipForward(2);
							m_bPreceededByLineBreak = bOldPreceededByLineBreak;
							return Token.comment;

						case '/':
							p.SkipForward(1);
							p.Mark();
							p.SkipToEol();
							m_strIdentifier = p.Extract();
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
					return Token.question;

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

			throw new CompileError(string.Format("Syntax error - unrecognized character: `{0}`", p.current), this);
		}

		// Parse a string literal
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
											throw new CompileError(string.Format("Syntax error - invalid character `{0}` in string literal", p.current), this);
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
											throw new CompileError(string.Format("Syntax error - invalid character `{0}` in string literal", p.current), this);
										}
										val = val * 16 + digit;
										p.SkipForward(1);
									}
									sb.Append((char)val);
									continue;
								}

							default:
								sb.Append(p.current);
								break;
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

		// Check if a string conforms to identifier requirements
		public static bool IsIdentifier(string str)
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

		// Is character valid as the first character in an identifier
		public static bool IsIdentifierLeadChar(char ch)
		{
			return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch == '_') || (ch == '$');
		}

		// Is character valid elsewhere in an identifier
		public static bool IsIdentifierChar(char ch)
		{
			return IsIdentifierLeadChar(ch) || (ch >= '0' && ch <= '9');
		}

		// Parse an indentifier
		public bool ParseIdentifier()
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

		// Build a map of keyword to token
		public static Dictionary<string, Token> m_mapKeywords;
		static void BuildKeywordMap()
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

		// Convert an keyword identifier into a token
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

		public static bool IsKeyword(string str)
		{
			return str=="true" || str=="false" || m_mapKeywords.ContainsKey(str);
		}

		// Parse a number - either double, hex integer, decimal interer or octal integer
		// Don't need to handle negatives as these are handled by Token.subtract unary operator
		public Token ParseNumber()
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
					throw new CompileError(string.Format("Syntax error - incorrectly formatted decimal literal `{0}`", str), this);
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
					throw new CompileError(string.Format("Syntax error - incorrectly formatted integer literal `{0}`", str), this);
				}
			}
		}

		// Hex character types
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

		// Octal character types
		static int OctalDigit(char ch)
		{
			if (ch >= '0' && ch <= '7')
				return 0 + ch - '0';
			return -1;
		}

		// Decimal character types
		static int DecimalDigit(char ch)
		{
			if (ch >= '0' && ch <= '9')
				return 0 + ch - '0';
			return -1;
		}


		StringBuilder sb = new StringBuilder();
		StringScanner p;
		Stack<StringScanner> m_IncludeStack=new Stack<StringScanner>();
		Token m_currentToken;
		bool m_bWarnings;
		bool m_bPreceededByLineBreak;
		string m_strIdentifier;
		int m_tokenStart;
		int m_tokenEnd;
		Token m_prevToken;
		int m_prevTokenEnd;
		object m_literal;
	}
}

