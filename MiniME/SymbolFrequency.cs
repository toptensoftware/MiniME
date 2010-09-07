// 
//   MiniME - http://www.toptensoftware.com/minime
// 
//   The contents of this file are subject to the license terms as 
//	 specified at the web address above.
//  
//   Software distributed under the License is distributed on an 
//   "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or
//   implied. See the License for the specific language governing
//   rights and limitations under the License.
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	// SymbolFrequency maintains frequency counts for a set of symbols
	class SymbolFrequency : Dictionary<string, Symbol>
	{
		// Constructor
		public SymbolFrequency()
		{
		}

		// Define a new local symbol
		public Symbol DefineSymbol(string str, Bookmark bmk)
		{
			Symbol s;
			if (!FindSymbol(str, Symbol.ScopeType.local, out s))
			{
				s = AddSymbol(new Symbol(str, Symbol.ScopeType.local));
			}

			s.Declarations.Add(bmk);

			return s;
		}

		// Increase the frequency count of a symbol
		public void UseSymbol(string str)
		{
			// Look for local then outer.  If neither, allocate a new 
			// outer scope symbol
			Symbol s;
			if (!FindSymbol(str, Symbol.ScopeType.local, out s))
			{
				if (!FindSymbol(str, Symbol.ScopeType.outer, out s))
				{
					s = AddSymbol(new Symbol(str, Symbol.ScopeType.outer));
				}
			}

			// And bump...
			s.Frequency++;
		}

		// Deep copy all symbols from another symbol frequency map
		public void CopyFrom(SymbolFrequency other)
		{
			this.Clear();

			foreach (var i in other)
			{
				AddSymbol(new Symbol(i.Value));
			}
		}
		
		// Merge symbols from an inner frequency map.
		// Symbols are merged by:
		//   - inner symbols from the inner map are mapped to inner symbols on the outer map
		//   - local symbols from the inner map are mapped to inner symbols on the outer map
		//   - outer symbols from the inner map which are defined in the outer map
		//			are mapped to local symbols on the outer map
		//	 - outer symbols from the inner map which are not defined in the outer map
		//			are mapped to outer symbols on the outer map
		public void MergeSymbols(SymbolFrequency inner)
		{
			foreach (var i in inner)
			{
				// Don't merge inner public symbols
				if (i.Value.Accessibility == Accessibility.Public)
					continue;

				Symbol s;
				switch (i.Value.Scope)
				{
					case Symbol.ScopeType.inner:
					case Symbol.ScopeType.local:
						if (!FindSymbol(i.Value.Name, Symbol.ScopeType.inner, out s))
							s = AddSymbol(new Symbol(i.Value.Name, Symbol.ScopeType.inner));
						break;

					case Symbol.ScopeType.outer:
					default:
						if (!FindSymbol(i.Value.Name, Symbol.ScopeType.local, out s))
						{
							if (!FindSymbol(i.Value.Name, Symbol.ScopeType.outer, out s))
							{
								s=AddSymbol(new Symbol(i.Value.Name, Symbol.ScopeType.outer));
							}
						}
						break;
				}

				s.Frequency += i.Value.Frequency;
			}
		}

		// Sort all symbols by frequency and return in a list
		public List<Symbol> Sort()
		{
			// Build a list
			var l = new List<Symbol>();
			foreach (var i in this)
			{
				// Don't add symbols that can't be obfuscated
				if (i.Value.Accessibility != Accessibility.Public) 
				{
					l.Add(i.Value);
				}
			}

			// Sort by frequency, rank, name and scope
			l.Sort(delegate(Symbol s1, Symbol s2) 
				{
					int Compare=s2.Frequency-s1.Frequency;
					if (Compare == 0)
						Compare = s1.Rank - s2.Rank;
					if (Compare==0)
						Compare=String.Compare(s1.Name, s2.Name);
					if (Compare==0)
						Compare=s2.Scope-s2.Scope;
					return Compare;
				});

			return l;
		}

		// Update the rank level for all local symbols in a frequency map
		//  - Rank is just the order of the symbol in the sorted frequency map
		//  - This function looks at each local symbol and compares its currently
		//    stored rank with it's position in the list.  
		//  - If the current position is further down the list than the currently 
		//    stored rank, increase the stored rank to match.
		public void UpdateRanks(List<Symbol> SymbolsByFreq)
		{
			int Rank = 0;
			foreach (var s in SymbolsByFreq)
			{
				System.Diagnostics.Debug.Assert(s.Accessibility != Accessibility.Public);

				if (s.Scope == Symbol.ScopeType.local)
				{
					Symbol originalSymbol;
					FindSymbol(s.Name, Symbol.ScopeType.local, out originalSymbol);
					if (Rank > originalSymbol.Rank)
						originalSymbol.Rank = Rank;
				}

				if (s.Scope != Symbol.ScopeType.outer)
				{
					Rank++;
				}
			}
		}

		// Dump to console
		public void Dump(int indent)
		{
			foreach (var i in this.Sort())
			{
				Utils.WriteIndentedLine(indent, "{0}: {1}x '{2}' {3}", i.Scope.ToString(), i.Frequency, i.Name, i.Scope==Symbol.ScopeType.local ? "#"+i.Rank.ToString() : "");
			}
		}

		public Symbol FindLocalSymbol(string Name)
		{
			Symbol temp;
			if (!FindSymbol(Name, Symbol.ScopeType.local, out temp))
				return null;

			return temp;
		}

		// Find an existing symbol
		bool FindSymbol(string name, Symbol.ScopeType scope, out Symbol retv)
		{
			return this.TryGetValue(name + "." + scope.ToString(), out retv);
		}

		// Add a new symbol
		Symbol AddSymbol(Symbol s)
		{
			Add(s.Name + "." + s.Scope.ToString(), s);
			return s;
		}
	}
}
