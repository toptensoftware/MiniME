using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	class SymbolFrequency
	{
		public void DefineSymbol(string str)
		{
			LocalSymbols.Increment(str);
		}

		public void UseSymbol(string str)
		{
			if (LocalSymbols.ContainsKey(str))
			{
				LocalSymbols.Increment(str);
			}
			else
			{
				ExternalSymbols.Increment(str);
			}
		}

		public void CopyFrom(SymbolFrequency other)
		{
			LocalSymbols.Clear();
			ExternalSymbols.Clear();

			foreach (var i in other.LocalSymbols)
			{
				LocalSymbols.Add(i.Key, i.Value);
			}
			foreach (var i in other.ExternalSymbols)
			{
				ExternalSymbols.Add(i.Key, i.Value);
			}
		}
		public void MergeSymbols(SymbolFrequency other)
		{
			foreach (var s in other.LocalSymbols)
			{
				LocalSymbols.Increment(s.Key, s.Count);
			}

		}

		public void Dump(int indent)
		{
			Utils.WriteIndentedLine(indent, "Local Symbol Frequency:");
			foreach (var i in LocalSymbols)
			{
				Utils.WriteIndentedLine(indent + 1, "{0}: {1}", i.Key, i.Value);
			}

			Utils.WriteIndentedLine(indent, "External Symbol Frequency:");
			foreach (var i in ExternalSymbols)
			{
				Utils.WriteIndentedLine(indent + 1, "{0}: {1}", i.Key, i.Value);
			}
		}

		public Dictionary<string, int> LocalSymbols = new Dictionary<string, int>();
		public Dictionary<string, int> ExternalSymbols = new Dictionary<string, int>();
	}
}
