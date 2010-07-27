using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	internal class RenderContext
	{
		public RenderContext(Compiler c)
		{
			m_Compiler = c;
		}

		public Compiler Compiler
		{
			get
			{
				return m_Compiler;
			}
		}


		public void TrackLinePosition(int iCharacters)
		{
			// Don't line break if formatted output, or line breaks disabled
			if (m_Compiler.Formatted || m_Compiler.MaxLineLength==0 || m_iLineBreaksDisabled!=0)
				return;

			if (m_iLinePos == 0)
			{
				m_iLinePos += iCharacters;
				return;
			}

			m_iLinePos += iCharacters;
			if (m_iLinePos > m_Compiler.MaxLineLength)
			{
				m_iLinePos = iCharacters;
				sb.Append("\n");
			}

		}

		public void DisableLineBreaks()
		{
			if (Compiler.MaxLineLength > 0)
			{
				m_iLineBreaksDisabled++;

				if (m_iLineBreaksDisabled == 1)
				{
					// Reset the temp string builder
					sbTemp.Length = 0;

					// Swap string builders
					var t = sb;
					sb = sbTemp;
					sbTemp = t;
				}
			}
		}

		public void EnableLineBreaks()
		{
			if (Compiler.MaxLineLength > 0)
			{
				m_iLineBreaksDisabled--;
				if (m_iLineBreaksDisabled == 0)
				{
					// Swap string builders back again
					var t = sb;
					sb = sbTemp;
					sbTemp = t;

					// Append the unbroken text
					Append(sbTemp.ToString());
				}
			}
		}

		public void Append(string val)
		{
			TrackLinePosition(val.Length);
			sb.Append(val);
		}

		public void Append(char val)
		{
			TrackLinePosition(1);
			sb.Append(val);
		}


		public void AppendFormat(string str, params object[] args)
		{
			Append(string.Format(str, args));
		}

		public string FinalScript()
		{
			return sb.ToString();
		}

		public SymbolAllocator Symbols
		{
			get
			{
				return m_Symbols;
			}
		}

		public void Indent()
		{
			m_iIndent++;
		}
		public void Unindent()
		{
			m_iIndent--;
		}

		public void StartLine()
		{
			if (Compiler.Formatted)
			{
				sb.Append("\n");
				sb.Append(new String(' ', m_iIndent * 4));
			}
		}

		int m_iLinePos = 0;
		int m_iIndent = 0;
		int m_iLineBreaksDisabled = 0;
		Compiler m_Compiler;
		StringBuilder sb = new StringBuilder();
		StringBuilder sbTemp = new StringBuilder();
		SymbolAllocator m_Symbols=new SymbolAllocator();
	}
}
