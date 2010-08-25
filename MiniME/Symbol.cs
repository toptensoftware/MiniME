using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	public enum Accessibility
	{
		Default,
		Private,
		Public,
	}

	// Stores information about a single symbol, including
	// it's name, scope, frequency and rank
	class Symbol
	{
		public enum ScopeType
		{
			inner,		// Symbol defined in an inner scope
			outer,		// Symbol unknown, must be defined in an outer scope
			local,		// Symbol defined in current scope
		}

		// Constructor
		public Symbol(string name, ScopeType scope)
		{
			Name = name;
			Scope = scope;
			Frequency = 0;
			ConstAllowed = true;
			Accessibility= Accessibility.Default;
		}

		// Copy constructor
		public Symbol(Symbol other)
		{
			Name = other.Name;
			Scope = other.Scope;
			Frequency = other.Frequency;
			Rank = other.Rank;
			Accessibility = other.Accessibility;
			ConstAllowed = false;
		}

		// Attributes
		public String Name;
		public ScopeType Scope;
		public int Frequency;
		public int Rank;
		public object ConstValue;
		public bool ConstAllowed;
		public Accessibility Accessibility;
		public List<Bookmark> Declarations = new List<Bookmark>();
	}
}
