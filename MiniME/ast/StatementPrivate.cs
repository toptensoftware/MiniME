using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class PrivateSpec
	{
		public bool Parse(string str)
		{
			m_strSpec = str;

			// Must start of end with a .
			if (str.StartsWith("."))
			{
				m_type = Type.Member;
				str=str.Substring(1);
			}
			else if (str.EndsWith("."))
			{
				m_type = Type.Target;
				str = str.Substring(0, str.Length - 1);
			}
			else
			{
				return false;
			}

			// Must have something
			if (String.IsNullOrEmpty(str))
				return false;

			// If it starts and ends with a slash, it's a regex
			if (str.StartsWith("/") && str.EndsWith("/"))
			{
				try
				{
					m_regex = new System.Text.RegularExpressions.Regex(str.Substring(1, str.Length - 2));
					return true;
				}
				catch(Exception)
				{
					return false;
				}
			}

			// If it ends with an asterix, it's a wildcard
			if (str.EndsWith("*"))
			{
				string strPrefix = str.Substring(0, str.Length - 1);
				if (!Tokenizer.IsIdentifier(strPrefix))
					return false;

				m_regex = new System.Text.RegularExpressions.Regex("^" + strPrefix + ".*$");
				return true;
			}

			// Exact
			if (!Tokenizer.IsIdentifier(str))
				return false;

			/// Store it
			m_strIdentifier = str;

			return true;
		}

		public bool DoesMatch(ast.ExprNodeIdentifier identifier)
		{
			if (identifier.Lhs==null)
				return false;

			if (m_type==Type.Target)
			{
				// Is the LHS an identifier?
				if (identifier.Lhs.GetType() != typeof(ast.ExprNodeIdentifier))
					return false;

				// Move the identifier on the left
				identifier = (ast.ExprNodeIdentifier)identifier.Lhs;

				// Skip over prototype if present
				if (identifier.Name == "prototype" && identifier.Lhs != null)
				{
					if (identifier.Lhs==null)
						return false;

					if (identifier.Lhs.GetType() != typeof(ast.ExprNodeIdentifier))
						return false;

					identifier=(ast.ExprNodeIdentifier)identifier.Lhs;
				}
			}

			// Compare the name
			if (m_regex != null)
				return m_regex.IsMatch(identifier.Name);
			else
				return m_strIdentifier == identifier.Name;
		}

		public override string ToString()
		{
			if (m_regex == null)
				return string.Format("`{0}`", m_strSpec);
			else
				return string.Format("`{0}` - `{1}`", m_strSpec, m_regex);
		}

		public string GetExplicitMemberName()
		{
			if (m_type == Type.Member && m_strIdentifier != null)
				return m_strIdentifier;

			return null;
		}

		enum Type
		{
			Member,		// Will match `.identifier`
			Target,		// Will match `identifier.`
		};

		Type m_type;
		string m_strSpec;
		string m_strIdentifier;
		System.Text.RegularExpressions.Regex m_regex;
	}

	// Represents a private statement
	class StatementPrivate : Statement
	{
		// Constructor
		public StatementPrivate()
		{
		}

		// Attributes
		public List<PrivateSpec> Specs=new List<PrivateSpec>();

		public override void Dump(int indent)
		{
			writeLine(indent, "private symbols:");
			foreach (var s in Specs)
			{
				writeLine(indent + 1, "`{0}`", s.ToString());
			}
		}

		public override bool Render(RenderContext dest)
		{
			return false;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
		}


	}
}
