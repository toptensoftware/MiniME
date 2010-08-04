using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class AccessibilitySpec
	{
		public bool Parse(Accessibility accessibility, string str)
		{
			m_strSpec = str;
			m_accessibility = accessibility;

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
				m_type = Type.Global;
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
			if (str.Contains('*') || str.Contains('?'))
			{
				str = str.Replace("*", "(.*)");
				str = str.Replace("?", "(.)");
				m_regex = new System.Text.RegularExpressions.Regex("^" + str + "$");
				return true;
			}

			// Exact
			if (!Tokenizer.IsIdentifier(str) && !Tokenizer.IsKeyword(str))
				return false;

			/// Store it
			m_strIdentifier = str;

			return true;
		}

		public bool DoesMatchName(string identifier)
		{
			// Compare the name
			if (m_regex != null)
				return m_regex.IsMatch(identifier);
			else
				return m_strIdentifier == identifier;
		}

		public bool DoesMatchDeclaration(string identifier)
		{
			return m_type == Type.Global && DoesMatchName(identifier);
		}

		public bool DoesMatch(ast.ExprNodeIdentifier identifier)
		{
			switch (m_type)
			{
				case Type.Global:
					if (identifier.Lhs != null)
						return false;
					break;

				case Type.Member:
					if (identifier.Lhs == null)
						return false;
					break;

				case Type.Target:
				{
					// Is the LHS an identifier?
					if (identifier.Lhs.GetType() != typeof(ast.ExprNodeIdentifier))
						return false;

					// Move the identifier on the left
					identifier = (ast.ExprNodeIdentifier)identifier.Lhs;

					// Skip over `prototype` if present
					if ((identifier.Name == "prototype" || identifier.Name=="__proto__") && identifier.Lhs != null)
					{
						if (identifier.Lhs == null)
							return false;

						if (identifier.Lhs.GetType() != typeof(ast.ExprNodeIdentifier))
							return false;

						identifier = (ast.ExprNodeIdentifier)identifier.Lhs;
					}
					break;
				}
			}

			// Does the name match
			return DoesMatchName(identifier.Name);
		}

		public override string ToString()
		{
			if (m_regex == null)
				return string.Format("{0} `{1}`", m_accessibility.ToString(), m_strSpec);
			else
				return string.Format("{0} `{1}` - `{2}`", m_accessibility.ToString(), m_strSpec, m_regex);
		}

		public bool IsWildcard()
		{
			return m_regex != null;
		}

		public string GetExplicitMemberName()
		{
			if (m_type != Type.Target && m_strIdentifier != null)
				return m_strIdentifier;

			return null;
		}

		public enum Type
		{
			Member,		// Will match `.identifier`
			Target,		// Will match `identifier.`
			Global,		// Will match global scope `identifier`
		};

		public Type GetSpecType()
		{
			return m_type;
		}

		public Accessibility GetAccessibility()
		{
			return m_accessibility;
		}

		Type m_type;
		Accessibility m_accessibility;
		string m_strSpec;
		string m_strIdentifier;
		System.Text.RegularExpressions.Regex m_regex;
	}

	// Represents a private statement
	class StatementAccessibility : Statement
	{
		// Constructor
		public StatementAccessibility(Bookmark bookmark) : base(bookmark)
		{
		}

		// Attributes
		public List<AccessibilitySpec> Specs=new List<AccessibilitySpec>();

		public override void Dump(int indent)
		{
			writeLine(indent, "accessibility:");
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
