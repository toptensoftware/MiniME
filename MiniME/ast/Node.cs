using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Implemented by anything that needs to walk the entire abstract
	// syntax tree as a visitor
	interface IVisitor
	{
		bool OnEnterNode(Node n);
		void OnLeaveNode(Node n);

	}

	// Base class for all nodes in the AST
	abstract class Node
	{
		public Node(Bookmark bookmark)
		{
			m_Bookmark = bookmark;
		}

		public Bookmark Bookmark
		{
			get
			{
				return m_Bookmark;
			}
		}

		// Override to dump this node
		public abstract void Dump(int indent);

		// Override to render this node
		public abstract bool Render(RenderContext dest);

		// Call visitor for this node and all child nodes
		public void Visit(IVisitor visitor)
		{
			if (visitor.OnEnterNode(this))
			{
				OnVisitChildNodes(visitor);
				visitor.OnLeaveNode(this);
			}
		}

		// Must be override to visit any child nodes
		public abstract void OnVisitChildNodes(IVisitor visitor);

		// Helpers for Dump implementation
		public static void write(int indent, string str, params string[] args)
		{
			Console.Write(new String(' ', indent * 4));
			Console.Write(str, args);
		}
		public static void write(string str, params string[] args)
		{
			Console.Write(str, args);
		}
		public static void writeLine(int indent, string str, params string[] args)
		{
			Console.Write(new String(' ', indent*4));
			Console.WriteLine(str, args);
		}

		// Scope that this node introduces.
		// Only used by function and CatchClause nodes
		public SymbolScope Scope;
		public SymbolScope PseudoScope;
		public Bookmark m_Bookmark;
	}

}
