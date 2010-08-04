using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	// All tokenize/parse/render errors thrown through this
	public class CompileError : Exception
	{
		// Constructor
		internal CompileError(string message, Tokenizer t)
		{
			m_strMessage = message;
			m_Bookmark = t.GetBookmark();
		}

		// Attributes
		string m_strMessage;
		Bookmark m_Bookmark;

		public override string Message
		{
			get
			{
				return string.Format("{0}: {1}", m_Bookmark.ToString(), m_strMessage);
			}
		}

	}
}
