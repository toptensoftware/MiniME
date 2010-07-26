using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	class SymbolFrequency : Dictionary<string, Symbol>
	{
		public bool FindSymbol(string name, Symbol.ScopeType scope, out Symbol retv)
		{
			return this.TryGetValue(name + "." + scope.ToString(), out retv);
		}

		public Symbol AddSymbol(Symbol s)
		{
			Add(s.Name + "." + s.Scope.ToString(), s);
			return s;
		}

		public void DefineSymbol(string str)
		{
			Symbol s;
			if (!FindSymbol(str, Symbol.ScopeType.local, out s))
			{
				s = AddSymbol(new Symbol(str, Symbol.ScopeType.local));
			}
			s.Frequency++;
		}

		public void UseSymbol(string str)
		{
			Symbol s;
			if (!FindSymbol(str, Symbol.ScopeType.local, out s))
			{
				if (!FindSymbol(str, Symbol.ScopeType.outer, out s))
				{
					s = AddSymbol(new Symbol(str, Symbol.ScopeType.outer));
				}
			}
			s.Frequency++;
		}

		public void CopyFrom(SymbolFrequency other)
		{
			this.Clear();

			foreach (var i in other)
			{
				AddSymbol(new Symbol(i.Value));
			}
		}
		public void MergeSymbols(SymbolFrequency other)
		{
			foreach (var i in other)
			{
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
		public List<Symbol> Sort()
		{
			var l = new List<Symbol>();
			foreach (var i in this)
			{
				l.Add(i.Value);
			}

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

		public void Dump(int indent)
		{
			foreach (var i in this.Sort())
			{
				Utils.WriteIndentedLine(indent, "{0}: {1}x '{2}' {3}", i.Scope.ToString(), i.Frequency, i.Name, i.Scope==Symbol.ScopeType.local ? "#"+i.Rank.ToString() : "");
			}
		}
	}
}
