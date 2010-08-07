using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	// SymbolScope stores symbol information for a scope.
	//  - this object is attached to a node in the AST - typically a function declaration
	//    with references both ways between the two objects
	class SymbolScope
	{
		// Constructor
		public SymbolScope(ast.Node node, Accessibility defaultAccessibility)
		{
			Node = node;
			DefaultAccessibility = defaultAccessibility;
		}

		// Get/set default accessibility
		public Accessibility DefaultAccessibility
		{
			get;
			set;
		}

		// Prepare symbol ranks of all local symbols
		//  - For each inner scope, merge this symbol frequency map
		//    of this scope with that of the inner scope
		//  - Update the rank of each symbol
		//  - Repeat for each inner scope
		public void Prepare()
		{
			// Recurse through all scopes
			foreach (var i in InnerScopes)
			{
				i.Prepare();
			}

			// Apply default accessibility to own local symbols
			foreach (var i in Symbols)
			{
				if (i.Value.Accessibility == Accessibility.Default)
				{
					i.Value.Accessibility = DefaultAccessibility;
				}
			}

			// Calculate base rank based on frequency of our own
			// local symbols
			Symbols.UpdateRanks(Symbols.Sort());

			// Merge each inner scope and update ranks
			foreach (var i in InnerScopes)
			{
				var merged = new SymbolFrequency();
				merged.CopyFrom(Symbols);
				merged.MergeSymbols(i.AllSymbols);

				Symbols.UpdateRanks(merged.Sort());
			}

			// Member symbol
			Members.UpdateRanks(Members.Sort());
			foreach (var i in InnerScopes)
			{
				var merged = new SymbolFrequency();
				merged.CopyFrom(Members);
				merged.MergeSymbols(i.AllMembers);

				Members.UpdateRanks(merged.Sort());
			}

		}

		// Claim all local symbols in this scope, preventing use
		// by the symbol allocator.  Used on the global scope to
		// reserve global symbols
		public void ClaimSymbols(SymbolAllocator symbols)
		{
			foreach (var i in Symbols)
			{
				var s = i.Value;
				if (s.Scope == Symbol.ScopeType.local && s.Accessibility==Accessibility.Public)
				{
					symbols.ClaimSymbol(s.Name);
				}
			}
		}

		// Obfuscate all local symbols
		//  - enumerate all local symbols and tell the symbol allocator
		//    that it can be obfuscated.
		//	- where there are `holes` in the rank mapping, tell the symbol
		//    allocator to reserve those symbols.  These holes are to be
		//    filled by higher frequency symbols on the inner scopes.
		public void ObfuscateSymbols(RenderContext ctx, SymbolFrequency SymbolFrequency, SymbolAllocator Allocator, string prefix)
		{
			// Walk through local symbols
			int expectedRank = 0;
			foreach (var symbol in SymbolFrequency.Sort())
			{
				// Ignore public symbols
				if (symbol.Accessibility != Accessibility.Private)
					continue;

				// Ignore non-local symbols
				if (symbol.Scope != Symbol.ScopeType.local)
					continue;

				// Reserve space for inner, higher frequency symbols
				if (symbol.Rank > expectedRank)
				{
					if (ctx.Compiler.Formatted && ctx.Compiler.SymbolInfo)
					{
						for (int r = expectedRank; r < symbol.Rank; r++)
						{
							ctx.StartLine();
							ctx.AppendFormat("// #{0} reserved", r);
						}
					}
					Allocator.ReserveObfuscatedSymbols(symbol.Rank - expectedRank);
				}

				string newSymbol = Allocator.OnfuscateSymbol(symbol.Name);

				// Show info
				if (ctx.Compiler.Formatted && ctx.Compiler.SymbolInfo)
				{
					ctx.StartLine();
					ctx.AppendFormat("// #{0} {3}{1} -> {3}{2}", symbol.Rank, symbol.Name, newSymbol, prefix);
				}

				expectedRank = symbol.Rank + 1;
			}
		}

		public void ObfuscateSymbols(RenderContext ctx)
		{
			// Obfuscate all symbols
			ObfuscateSymbols(ctx, Symbols, ctx.Symbols, "");
			ObfuscateSymbols(ctx, Members, ctx.Members, ".");

			// Dump const eliminated variables
			foreach (var i in Symbols.Where(x=>x.Value.ConstValue!=null))
			{
				// Show info
				if (ctx.Compiler.Formatted && ctx.Compiler.SymbolInfo)
				{
					ctx.StartLine();
					ctx.AppendFormat("// {0} -> optimized away (const)", i.Value.Name);
				}
			}

		}

		// Dump to stdout
		public void Dump(int indent)
		{
			Utils.WriteIndentedLine(indent, "Scope: {0}", Node);

			if (Symbols.Sort().Count>0)
			{
				Utils.WriteIndentedLine(indent + 1, "Local Symbols:");
				Symbols.Dump(indent + 2);
			}

			foreach (var i in InnerScopes)
			{
				var merged = new SymbolFrequency();
				merged.CopyFrom(Symbols);
				merged.MergeSymbols(i.AllSymbols);

				if (merged.Sort().Count > 0)
				{
					Utils.WriteIndentedLine(indent + 1, "Symbols after merge with {0}:", i.Node);
					merged.Dump(indent + 2);
				}
			}

			foreach (var i in InnerScopes)
			{
				i.Dump(indent + 1);
			}
		}

		// Get all symbols, including all symbols from inner scopes
		// in a single frequency map
		public SymbolFrequency AllSymbols
		{
			get
			{
				if (m_AllSymbols == null)
				{
					m_AllSymbols = new SymbolFrequency();
					m_AllSymbols.CopyFrom(Symbols);
					foreach (var s in InnerScopes)
					{
						m_AllSymbols.MergeSymbols(s.AllSymbols);
					}
				}
				return m_AllSymbols;
			}
		}

		// Get all symbols, including all symbols from inner scopes
		// in a single frequency map
		public SymbolFrequency AllMembers
		{
			get
			{
				if (m_AllMembers == null)
				{
					m_AllMembers = new SymbolFrequency();
					m_AllMembers.CopyFrom(Members);
					foreach (var s in InnerScopes)
					{
						m_AllMembers.MergeSymbols(s.AllMembers);
					}
				}
				return m_AllMembers;
			}
		}

		public Symbol FindSymbol(string Name)
		{
			// Check self
			var s = Symbols.FindLocalSymbol(Name);
			if (s!=null)
				return s;

			// Check outer scope
			if (OuterScope != null)
				return OuterScope.FindSymbol(Name);

			// Not defined
			return null;
		}

		public void AddAccessibilitySpec(Bookmark bmk, AccessibilitySpec spec)
		{
			// Just store wildcards for now
			if (spec.IsWildcard())
			{
				m_AccessibilitySpecs.Add(spec);
				return;
			}


			Symbol s;
			if (spec.IsMemberSpec())
			{
				s = Members.DefineSymbol(spec.GetExplicitName());
			}
			else
			{
				s = Symbols.DefineSymbol(spec.GetExplicitName());
			}

			// Mark obfuscation for this symbol
			s.Accessibility = spec.GetAccessibility();
		}

		public void ProcessAccessibilitySpecs(string identifier)
		{
			// Check accessibility specs
			foreach (var spec in m_AccessibilitySpecs)
			{
				if (spec.IsWildcard() && spec.DoesMatch(identifier))
				{
					var symbol=Symbols.DefineSymbol(identifier);
					if (symbol.Accessibility==Accessibility.Default)
						symbol.Accessibility = spec.GetAccessibility();
					return;
				}
			}

			// Pass to outer scope
			if (OuterScope!=null)
				OuterScope.ProcessAccessibilitySpecs(identifier);
		}

		public void ProcessAccessibilitySpecs(ast.ExprNodeIdentifier target, string identifier)
		{
			// Check accessibility specs
			foreach (var spec in m_AccessibilitySpecs)
			{
				if (spec.IsWildcard() && spec.DoesMatch(target, identifier))
				{
					var symbol=Members.DefineSymbol(identifier);
					if (symbol.Accessibility == Accessibility.Default)
						symbol.Accessibility = spec.GetAccessibility();
					return;
				}
			}

			// Pass to outer scope
			if (OuterScope!=null)
				OuterScope.ProcessAccessibilitySpecs(identifier);
		}

		public ast.Node Node;
		public SymbolScope OuterScope = null;
		public List<SymbolScope> InnerScopes = new List<SymbolScope>();
		public SymbolFrequency Symbols = new SymbolFrequency();
		public SymbolFrequency m_AllSymbols;
		public SymbolFrequency Members = new SymbolFrequency();
		public SymbolFrequency m_AllMembers;
		public List<AccessibilitySpec> m_AccessibilitySpecs=new List<AccessibilitySpec>();
	}
}

