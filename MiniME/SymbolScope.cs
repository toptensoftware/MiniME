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
		public SymbolScope(ast.Node node)
		{
			Node = node;
		}

		// Prepare symbol ranks of all local symbols
		//  - For each inner scope, merge this symbol frequency map
		//    of this scope with that of the inner scope
		//  - Update the rank of each symbol
		//  - Repeat for each inner scope
		public void PrepareSymbolRanks()
		{
			// Quit if there's no point
			if (ContainsEvil)
				return;

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

			// Recurse through all scopes
			foreach (var i in InnerScopes)
			{
				i.PrepareSymbolRanks();
			}
		}

		// Claim all local symbols in this scope, preventing use
		// by the symbol allocator.  Used on the global scope to
		// reserve global symbols
		public void ClaimSymbols(SymbolAllocator symbols)
		{
			foreach (var i in Symbols.Sort())
			{
				if (i.Scope == Symbol.ScopeType.local)
				{
					symbols.ClaimSymbol(i.Name);
				}
			}
		}

		public void ObfuscateSymbols(RenderContext ctx, SymbolFrequency SymbolFrequency, SymbolAllocator Allocator, string prefix)
		{
			// Walk through local symbols
			int expectedRank = 0;
			foreach (var i in SymbolFrequency.Sort())
			{
				if (i.Scope == Symbol.ScopeType.local)
				{
					// Reserve space for inner, higher frequency symbols
					if (i.Rank > expectedRank)
					{
						if (ctx.Compiler.Formatted && ctx.Compiler.SymbolInfo)
						{
							for (int r = expectedRank; r < i.Rank; r++)
							{
								ctx.StartLine();
								ctx.AppendFormat("// #{0} reserved", r);
							}
						}
						Allocator.ReserveObfuscatedSymbols(i.Rank - expectedRank);
					}

					string newSymbol = Allocator.OnfuscateSymbol(i.Name);

					// Show info
					if (ctx.Compiler.Formatted && ctx.Compiler.SymbolInfo)
					{
						ctx.StartLine();
						ctx.AppendFormat("// #{0} {3}{1} -> {3}{2}", i.Rank, i.Name, newSymbol, prefix);
					}

					expectedRank = i.Rank + 1;
				}
			}
		}

		// Obfuscate all local symbols
		//  - enumerate all local symbols and tell the symbol allocator
		//    that it can be obfuscated.
		//	- where there are `holes` in the rank mapping, tell the symbol
		//    allocator to reserve those symbols.  These holes are to be
		//    filled by higher frequency symbols on the inner scopes.
		public void ObfuscateSymbols(RenderContext ctx)
		{
			// Quit if obfuscation prevented by evil
			if (ContainsEvil)
			{
				if (ctx.Compiler.Formatted && ctx.Compiler.SymbolInfo)
				{
					ctx.StartLine();
					ctx.AppendFormat("// Obfuscation prevented by evil");
				}
			}
			else
			{
				ObfuscateSymbols(ctx, Symbols, ctx.Symbols, "");
			}

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

			Utils.WriteIndentedLine(indent + 1, "Local:");
			Symbols.Dump(indent + 2);

			foreach (var i in InnerScopes)
			{
				var merged = new SymbolFrequency();
				merged.CopyFrom(Symbols);
				merged.MergeSymbols(i.AllSymbols);

				Utils.WriteIndentedLine(indent + 1, "Merged for {0}", i.Node);
				merged.Dump(indent + 2);
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

		public void AddPrivateSpec(ast.PrivateSpec spec)
		{
			string strExplicit = spec.GetExplicitMemberName();
			if (strExplicit != null)
			{
				Members.DefineSymbol(strExplicit);
			}
			else
			{
				m_PrivateSpecs.Add(spec);
			}
		}

		public bool DoesIdentifierMatchPrivateSpec(ast.ExprNodeIdentifier identifier)
		{
			foreach (var s in m_PrivateSpecs)
			{
				if (s.DoesMatch(identifier))
				{
					return s.GetExplicitMemberName() == null;
				}
			}

			return false;
		}

		public void DefinePrivateMemberIfMatchesAnySpec(ast.ExprNodeIdentifier identifier)
		{
			if (DoesIdentifierMatchPrivateSpec(identifier))
			{
				Members.DefineSymbol(identifier.Name);
			}
			else if (OuterScope!=null)
			{
				OuterScope.DefinePrivateMemberIfMatchesAnySpec(identifier);
			}
		}

		public ast.Node Node;
		public SymbolScope OuterScope = null;
		public List<SymbolScope> InnerScopes = new List<SymbolScope>();
		public bool ContainsEvil;		// Contains a 'with' or 'eval'

		public SymbolFrequency Symbols = new SymbolFrequency();
		public SymbolFrequency m_AllSymbols;

		public SymbolFrequency Members = new SymbolFrequency();
		public SymbolFrequency m_AllMembers;

		public List<ast.PrivateSpec> m_PrivateSpecs=new List<ast.PrivateSpec>();
	}
}

