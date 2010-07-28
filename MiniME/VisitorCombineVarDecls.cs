using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	// Visitor to look at all statement blocks, find consecutive variable
	// declarations and combine into a single statement
	class VisitorCombineVarDecl: ast.IVisitor
	{
		public VisitorCombineVarDecl(SymbolScope rootScope)
		{
		}

		public void OnEnterNode(MiniME.ast.Node n)
		{
			if (n.GetType() == typeof(ast.StatementBlock))
			{
				var block = (ast.StatementBlock)n;
				block.CombineVarDecls();
			}
		}

		public void OnLeaveNode(MiniME.ast.Node n)
		{
		}
	}
}
