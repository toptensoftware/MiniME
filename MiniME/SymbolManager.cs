using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	class SymbolManager
	{
		public SymbolManager()
		{
			m_ScopeStack.Add(m_GlobalScope);
		}

		// Create a new scope (function declaration)
		public void EnterScope()
		{
			var newScope = new SymbolScope();
			CurrentScope.m_ChildScopes.Add(newScope);
			m_ScopeStack.Add(newScope);
		}

		// Leava a scope
		public void LeaveScope()
		{
			// Remove the current scope
			m_ScopeStack.RemoveAt(m_ScopeStack.Count - 1);
		}

		// Called for every variable/function declaration
		public Symbol CreateSymbol(string SymbolName, bool CanObfuscate)
		{
			// Symbols can be defined twice...
			Symbol s;
			if (CurrentScope.m_LocalSymbols.TryGetValue(SymbolName, out s))
			{
				s.Frequency++;
				s.LocalFrequency++;
				return s;
			}
	
			// Create a new symbol
			s = new Symbol();
			s.OriginalName = SymbolName;
			s.ObfuscatedName = null;
			s.CanObfuscate = CanObfuscate;
			s.Frequency = 1;
			s.LocalFrequency = 1;
			s.DefiningScope = CurrentScope;
			CurrentScope.m_LocalSymbols.Add(SymbolName, s);
			return s;
		}

		// Called to resolve variable use
		public Symbol GetSymbol(string SymbolName)
		{
			// Walk scope stack looking for the symbol
			for (int i = m_ScopeStack.Count - 1; i >= 0; i--)
			{
				// Get the scope
				var s = m_ScopeStack[i];

				// Get the symbol
				Symbol symbol;
				if (s.m_LocalSymbols.TryGetValue(SymbolName, out symbol))
				{
					symbol.Frequency++;

					if (i == m_ScopeStack.Count - 1)
						symbol.LocalFrequency++;

					return symbol;
				}
			}

			// Undefined
			return null;
		}

		void ProcessScope(SymbolScope scope)
		{
			foreach (var i in scope.m_ChildScopes)
			{
			}
		}

		public void Finish()
		{
			ProcessScope(m_GlobalScope);
		}

		SymbolScope CurrentScope
		{
			get
			{
				return m_ScopeStack[m_ScopeStack.Count - 1];
			}
		}

		public class Symbol
		{
			public string OriginalName;
			public string ObfuscatedName;
			public bool CanObfuscate;
			public int Frequency;
			public int LocalFrequency;
			public SymbolScope DefiningScope;
		}

		public class SymbolScope
		{
			public List<SymbolScope> m_ChildScopes=new List<SymbolScope>();
			public Dictionary<String, Symbol> m_LocalSymbols = new Dictionary<String, Symbol>();
		}

		SymbolScope m_GlobalScope=new SymbolScope();
		List<SymbolScope> m_ScopeStack = new List<SymbolScope>();
	}
}
