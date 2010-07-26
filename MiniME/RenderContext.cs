using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	internal class RenderContext
	{
		public RenderContext()
		{

		}

		public void Append<T>(T val)
		{
			sb.Append(val);
		}

		public void AppendFormat(string str, params object[] args)
		{
			sb.AppendFormat(str, args);
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
			if (Formatted)
			{
				sb.Append("\n");
				sb.Append(new String(' ', m_iIndent * 4));
			}
		}

		public bool Formatted
		{
			get;
			set;
		}
		public bool DebugInfo
		{
			get;
			set;
		}

		int m_iIndent = 0;
		StringBuilder sb = new StringBuilder();
		SymbolAllocator m_Symbols=new SymbolAllocator();
	}
}
