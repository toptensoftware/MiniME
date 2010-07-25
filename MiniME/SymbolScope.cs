using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	class SymbolScope
	{
		public SymbolScope(ast.ExprNodeFunction fn)
		{
			Function = fn;
		}

		public void Dump(int indent)
		{
			Utils.WriteIndentedLine(indent, "Scope: {0}", Function);

			Symbols.Dump(indent + 1);

			foreach (var i in NestedScopes)
			{
				i.Dump(indent + 1);
			}
		}

		public ast.ExprNodeFunction Function;
		public SymbolFrequency Symbols = new SymbolFrequency();
		public List<SymbolScope> NestedScopes = new List<SymbolScope>();
	}
}
