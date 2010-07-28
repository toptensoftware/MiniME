using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	// Render context holds everything required for the rendering process, including
	// the output string builder, the symbol allocator and a reference back to the compile
	// from where options can be retrieved.
	internal class RenderContext
	{
		// Constructor
		public RenderContext(Compiler c, SymbolAllocator SymbolAllocator)
		{
			m_Compiler = c;
			m_SymbolAllocator = SymbolAllocator;
		}

		// Get the owning compiler
		public Compiler Compiler
		{
			get
			{
				return m_Compiler;
			}
		}

		// Get the symbol allocator
		public SymbolAllocator Symbols
		{
			get
			{
				return m_SymbolAllocator;
			}
		}


		// Track the current line position and insert breaks as necessary
		//  - iCharacters=the number of characters about to be written
		void TrackLinePosition(int iCharacters)
		{
			// Don't insert breaks if we're doing formatted output, or line breaks disabled
			// by the user
			if (m_Compiler.Formatted || m_Compiler.MaxLineLength==0)
				return;

			// Are line breaks currently disabled?
			if (m_iLineBreaksDisabled != 0)
				return;

			// If the line is currently empty, we just need to accept the new length
			// regardless of whether it will fit.
			if (m_iLinePos == 0)
			{
				m_iLinePos += iCharacters;
				return;
			}

			// Update position, insert breaks as necessary
			m_iLinePos += iCharacters;
			if (m_iLinePos > m_Compiler.MaxLineLength)
			{
				m_iLinePos = iCharacters;
				sb.Append("\n");
			}

		}

		// Called by the rendering code to temporarily disable line breaks
		// while rendering structures that would change meaning if there was 
		// a line break between.  eg: strings and `return <value>` statements
		//
		// When disabled, we accumulate generated text into a secondary string
		// buffer and append the whole lot when line breaks are re-enabled
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

		// Re-enable line breaks
		public void EnableLineBreaks()
		{
			if (Compiler.MaxLineLength > 0)
			{
				m_iLineBreaksDisabled--;
				System.Diagnostics.Debug.Assert(m_iLineBreaksDisabled >= 0);
				if (m_iLineBreaksDisabled == 0)
				{
					// Swap string builders back again
					var t = sb;
					sb = sbTemp;
					sbTemp = t;

					// Append the temporarily accumulated text
					Append(sbTemp.ToString());
				}
			}
		}

		// Automatically enable line breaks immediately after the next write
		public void EnableLineBreaksAfterNextWrite()
		{
			System.Diagnostics.Debug.Assert(m_bEnableLineBreaksAfterNextWrite == false);
			m_bEnableLineBreaksAfterNextWrite = true;
		}

		// Force insertion of a space if the next character is chNext
		// Used to insert spaces between + and ++ operators
		public void NeedSpaceIf(char chNext)
		{
			System.Diagnostics.Debug.Assert(m_chNeedSpaceIf == '\0');
			m_chNeedSpaceIf = chNext;
		}

		// Append a string to the output buffer
		public void Append(string val)
		{
			// Check if we need to insert a space for NeedSpaceIf()
			if (m_chNeedSpaceIf != '\0')
			{
				if (val.Length > 0 && val[0] == m_chNeedSpaceIf)
				{
					m_chNeedSpaceIf = '\0';
					Append(" ");
				}
				else
				{
					m_chNeedSpaceIf = '\0';
				}
			}

			// Track the line position and append the text
			TrackLinePosition(val.Length);
			sb.Append(val);

			// Auto enable line breaks?
			if (m_bEnableLineBreaksAfterNextWrite)
			{
				m_bEnableLineBreaksAfterNextWrite = false;
				EnableLineBreaks();
			}
		}

		// Append a single character
		public void Append(char ch)
		{
			Append(new String(ch, 1));
		}

		// Append a formatted string
		public void AppendFormat(string str, params object[] args)
		{
			Append(string.Format(str, args));
		}

		// Get the generated output
		public string GetGeneratedOutput()
		{
			return sb.ToString();
		}

		// Increase indent level
		public void Indent()
		{
			m_iIndent++;
		}
		// Decrease the indent level
		public void Unindent()
		{
			m_iIndent--;
		}

		// Start a new line and insert the current indent level
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
		bool m_bEnableLineBreaksAfterNextWrite = false;
		char m_chNeedSpaceIf = '\0';
		Compiler m_Compiler;
		StringBuilder sb = new StringBuilder();
		StringBuilder sbTemp = new StringBuilder();
		SymbolAllocator m_SymbolAllocator;
	}
}
