using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	class SymbolScope
	{
		public SymbolScope(ast.Node node)
		{
			Node = node;
		}

		public static void UpdateRanks(SymbolFrequency Symbols, List<Symbol> SymbolsByFreq)
		{
			int Rank = 0;
			foreach (var s in SymbolsByFreq)
			{
				if (s.Scope == Symbol.ScopeType.local)
				{
					Symbol originalSymbol;
					Symbols.FindSymbol(s.Name, Symbol.ScopeType.local, out originalSymbol);
					if (Rank > originalSymbol.Rank)
						originalSymbol.Rank = Rank;
				}

				if (s.Scope != Symbol.ScopeType.outer)
				{
					Rank++;
				}
			}
		}

		public void PrepareSymbolRanks()
		{
			// Quit if no point
			if (ContainsEvil)
				return;

			// Calculate base rank based on local frequency
			UpdateRanks(Symbols, Symbols.Sort());

			// Update ranks when merged with each child scope
			foreach (var i in NestedScopes)
			{
				var merged = new SymbolFrequency();
				merged.CopyFrom(Symbols);
				merged.MergeSymbols(i.AllSymbols);

				UpdateRanks(Symbols, merged.Sort());
			}

			foreach (var i in NestedScopes)
			{
				i.PrepareSymbolRanks();
			}
		}

		public void ClaimSymbols(RenderContext ctx)
		{
			foreach (var i in Symbols.Sort())
			{
				if (i.Scope == Symbol.ScopeType.local)
				{
					ctx.Symbols.ClaimSymbol(i.Name);
				}
			}
		}

		public void ObfuscateSymbols(RenderContext ctx)
		{
			// Quit if obfuscation prevented by evil
			if (ContainsEvil)
			{
				if (ctx.DebugInfo)
				{
					ctx.StartLine();
					ctx.AppendFormat("// Obfuscation prevented by evil");
				}
				return;
			}

			int rankPos = -1;
			foreach (var i in Symbols.Sort())
			{
				if (i.Scope == Symbol.ScopeType.local)
				{
					// Reserve space for inner, higher frequency symbols
					if (i.Rank > rankPos+1)
					{
						if (ctx.DebugInfo)
						{
							for (int j = rankPos+1; j < i.Rank; j++)
							{
								ctx.StartLine();
								ctx.AppendFormat("// #{0} Reserved", j);
							}
						}
						ctx.Symbols.ReserveObfuscatedSymbols(i.Rank - rankPos);
					}

					string newSymbol=ctx.Symbols.OnfuscateSymbol(i.Name);

					// Create this symbol
					if (ctx.DebugInfo)
					{
						ctx.StartLine();
						ctx.AppendFormat("// #{0} {1} -> {2}", i.Rank, i.Name, newSymbol);
					}
					rankPos = i.Rank;
				}
			}
		}

		public void Dump(int indent)
		{
			Utils.WriteIndentedLine(indent, "Scope: {0}", Node);

			Utils.WriteIndentedLine(indent + 1, "Local:");
			Symbols.Dump(indent + 2);

			foreach (var i in NestedScopes)
			{
				var merged = new SymbolFrequency();
				merged.CopyFrom(Symbols);
				merged.MergeSymbols(i.AllSymbols);

				Utils.WriteIndentedLine(indent + 1, "Merged for {0}", i.Node);
				merged.Dump(indent + 2);
			}

			foreach (var i in NestedScopes)
			{
				i.Dump(indent + 1);
			}
		}

		public SymbolFrequency AllSymbols
		{
			get
			{
				if (m_AllSymbols == null)
				{
					m_AllSymbols = new SymbolFrequency();
					m_AllSymbols.CopyFrom(Symbols);
					foreach (var s in NestedScopes)
					{
						m_AllSymbols.MergeSymbols(s.AllSymbols);
					}
				}
				return m_AllSymbols;
			}
		}

		public ast.Node Node;
		public SymbolFrequency Symbols = new SymbolFrequency();
		public List<SymbolScope> NestedScopes = new List<SymbolScope>();
		public SymbolFrequency m_AllSymbols;
		public bool ContainsEvil;		// Contains a 'with' or 'eval'
	}
}
