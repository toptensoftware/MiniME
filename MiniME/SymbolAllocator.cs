using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	// SymbolAllocator - allocates and maps symbols
	class SymbolAllocator
	{
		// Constructor
		public SymbolAllocator(Compiler c)
		{
			m_Compiler = c;

			// Create the global scope
			m_ScopeStack.Add(new Scope());
			CurrentScope.NextSymbol = "a";
		}

		// Enter a new scope, all newly allocated obfuscated symbols stored in a 
		// new scope that can be cleaned up by LeaveScope.
		// Also returns all currently reserved obfuscated symbols to the list of 
		// available symbols.
		public void EnterScope()
		{
			// Create a new Scope
			var s = new Scope();

			// Make the previously reserved symbols available to the new scope
			s.AvailableSymbols.AddRange(CurrentScope.ReservedSymbols);

			// Copy over the next symbol index
			s.NextSymbol = CurrentScope.NextSymbol;

			// Push the new scope
			m_ScopeStack.Add(s);
		}

		// Leave the current scope, effectively returning all currently 
		// allocated symbols to the set of available symbols
		public void LeaveScope()
		{
			// Pop the last scope
			m_ScopeStack.RemoveAt(m_ScopeStack.Count - 1);
		}

		// Get the obfuscated symbol for a previously allocated symbol
		// Returns the original symbol if not obfuscated
		public string GetObfuscatedSymbol(string originalSymbol)
		{
			// Anonymous functions have a null name
			if (originalSymbol == null)
				return null;

			// Walk the scope stack looking for a matching symbol
			for (int i=m_ScopeStack.Count-1; i>=0; i--)
			{
				var s=m_ScopeStack[i];

				string obfSymbol;
				if (s.SymbolMap.TryGetValue(originalSymbol, out obfSymbol))
				{
					if (m_Compiler.SymbolInfo && obfSymbol!=originalSymbol)
						return String.Format("{0}/*{1}*/", obfSymbol, originalSymbol);
					else
						return obfSymbol;
				}
			}

			// No match, return the original
			return originalSymbol;
		}

		// Claim a symbol so that it's never allocated by the symbol allocator
		// Claimed symbols are also never obfuscated.
		public void ClaimSymbol(string symbol)
		{
			// Map symbol to itself
			CurrentScope.SymbolMap.Add(symbol, symbol);

			// Add to the claimed map
			CurrentScope.ClaimedSymbols.Add(symbol, true);
		}

		// Allocate the next shortest symbol
		public string OnfuscateSymbol(string originalSymbol)
		{
			// Allocate, store, return
			string newSymbol = GetNextObfuscatedSymbol();
			CurrentScope.SymbolMap.Add(originalSymbol, newSymbol);
			return newSymbol;
		}

		// Reserve the next 'count' obfuscated symbols
		public void ReserveObfuscatedSymbols(int count)
		{
			// All we do is allocate symbols and add to the reserve list
			for (int i = 0; i < count; i++)
			{
				CurrentScope.ReservedSymbols.Add(GetNextObfuscatedSymbol());
			}
		}

		// Check if a symbol has been claimed
		public bool IsSymbolClaimed(string str)
		{
			for (int i = m_ScopeStack.Count - 1; i >= 0; i--)
			{
				if (m_ScopeStack[i].ClaimedSymbols.ContainsKey(str))
					return true;
			}

			return false;
		}

		// Work out the next obfuscated symbol, taking care
		// to not allocate something that's already it use
		string GetNextObfuscatedSymbol()
		{
			while (true)
			{
				string str = GetNextObfuscatedSymbolHelper();
				if (!IsSymbolClaimed(str))
					return str;
			}
		}

		// Work out the next obfuscated symbol by first checking the
		// list of available symbols, then by allocating new ones
		string GetNextObfuscatedSymbolHelper()
		{
			// Use available symbol list first
			if (CurrentScope.AvailableSymbols.Count > 0)
			{
				var s = CurrentScope.AvailableSymbols[0];
				CurrentScope.AvailableSymbols.RemoveAt(0);
				return s;
			}

			// Figure out the next available symbol.
			string str = CurrentScope.NextSymbol;
			CurrentScope.NextSymbol = NextSymbol(CurrentScope.NextSymbol);
			return str;
		}

		// Helper to increment a symbol string to get the next
		// Need to ensure that symbol never starts with a digit.
		// Use characters a-z, A-Z and 0-9
		static string NextSymbol(string str)
		{
			// First ever
			if (String.IsNullOrEmpty(str))
				return "a";

			// The approach is to convert the string to a character array
			// and then simply increment the last character and move
			// left as each column overflows.  `letter math`

			char[] array = str.ToCharArray();
			for (int i = array.Length - 1; i >= 0; i--)
			{
				// Easy cases
				if ((array[i] >= 'a' && array[i] < 'z') || (array[i] >= 'A' && array[i] < 'Z') || (array[i] >= '0' && array[i] < '9'))
				{
					array[i]++;
					break;
				}

				// Jump from lower case to upper case (always allowed)
				if (array[i] == 'z')
				{
					array[i] = 'A';
					break;
				}

				// Jump from upper case to digits (only allowed if not first character)
				if (array[i] == 'Z' && i > 0)
				{
					array[i] = '0';
					break;
				}

				// Wrap around
				array[i] = 'a';
				if (i > 0)
					continue;

				// Prepend a new leading character
				return "a" + new String(array);
			}

			// Done!
			return new String(array);
		}


		// Helper to grab to the current scope
		Scope CurrentScope
		{
			get
			{
				return m_ScopeStack[m_ScopeStack.Count-1];
			}
		}

		// Symbol allocate scope - stores state of the currently entered scope
		class Scope
		{
			public Dictionary<string, bool> ClaimedSymbols = new Dictionary<string, bool>();
			public Dictionary<string, string> SymbolMap = new Dictionary<string, string>();
			public List<string> AvailableSymbols=new List<string>();
			public List<string> ReservedSymbols=new List<string>();
			public string NextSymbol;
		};

		Compiler m_Compiler;
		List<Scope> m_ScopeStack = new List<Scope>();
		StringBuilder m_sb = new StringBuilder();
	}
}
