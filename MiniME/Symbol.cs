using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	class Symbol
	{
		public enum ScopeType
		{
			inner,		// Symbol defined in an inner scope
			outer,		// Symbol unknown, must be defined in an outer scope
			local,		// Symbol defined in current scope
		}

		public Symbol(string name, ScopeType scope)
		{
			Name = name;
			Scope = scope;
			Frequency = 0;
		}
		public Symbol(Symbol other)
		{
			Name = other.Name;
			Scope = other.Scope;
			Frequency = other.Frequency;
			Rank = other.Rank;
		}

		public String Name;
		public ScopeType Scope;
		public int Frequency;
		public int Rank;
	}
}
