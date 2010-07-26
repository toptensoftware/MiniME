using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MiniME;

namespace MiniMETestCases
{
	[TestFixture]
	public class SymbolAllocatorTests
	{
		[SetUp]
		public void Setup()
		{
			s = new MiniME.SymbolAllocator();
		}

		[Test]
		public void SingleCharSymbols()
		{
		}
		[Test]
		public void DoubleCharSymbols()
		{
		}

		MiniME.SymbolAllocator s;
	}
}
