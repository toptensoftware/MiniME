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
		void OnEnterNode(Node n);
		void OnLeaveNode(Node n);

	}

	// Base class for all nodes in the AST
	abstract class Node
	{
		// Override to dump this node
		public abstract void Dump(int indent);

		// Override to render this node
		public abstract bool Render(RenderContext dest);

		// Render this node in an indented block (if in formatted mode)
		public bool RenderIndented(RenderContext dest)
		{
			if (dest.Compiler.Formatted && GetType()!=typeof(StatementBlock))
			{
				dest.Indent();
				dest.StartLine();
				bool b = Render(dest);
				dest.Unindent();
				return b;
			}
			else
			{
				return Render(dest);
			}
		}

		// Call visitor for this node and all child nodes
		public void Visit(IVisitor visitor)
		{
			visitor.OnEnterNode(this);
			OnVisitChildNodes(visitor);
			visitor.OnLeaveNode(this);
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
	}

}
