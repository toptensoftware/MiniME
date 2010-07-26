using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	class VisitorSymbolUsage : ast.IVisitor
	{
		public VisitorSymbolUsage(SymbolScope rootScope)
		{
			m_Scopes.Push(rootScope);
		}

		public void OnEnterNode(MiniME.ast.Node n)
		{
			// Is it a function?
			if (n.GetType() == typeof(ast.ExprNodeFunction))
			{
				// Enter scope
				m_Scopes.Push(((ast.ExprNodeFunction)n).Scope);
			}

			if (n.GetType() == typeof(ast.ExprNodeMember))
			{
				var m = (ast.ExprNodeMember)n;
				if (m.Lhs == null)
				{
					UseSymbol(m.Name);
				}
			}
		}

		public void OnLeaveNode(MiniME.ast.Node n)
		{
			if (n.GetType()==typeof(ast.ExprNodeFunction))
			{
				m_Scopes.Pop();
			}
		}

		void UseSymbol(string str)
		{
			var s = m_Scopes.Peek();
			s.Symbols.UseSymbol(str);
		}

		public Stack<SymbolScope> m_Scopes=new Stack<SymbolScope>();
	}
}
