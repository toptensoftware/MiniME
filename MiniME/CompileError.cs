using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	public class CompileError : Exception
	{
		internal CompileError(string message, Tokenizer t)
		{
			m_strMessage = message;
			m_strFileName = t.FileName;
			m_lineNumber = t.currentLine;
			m_linePosition = t.currentLinePosition;
		}

		public override string Message
		{
			get
			{
				return string.Format("{0}({1},{2}): {3}", m_strFileName, m_lineNumber+1, m_linePosition+1, m_strMessage);
			}
		}

		string m_strMessage;
		string m_strFileName;
		int m_lineNumber;
		int m_linePosition;
	}
}
