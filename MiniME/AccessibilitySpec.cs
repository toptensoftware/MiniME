// 
//   MiniME - http://www.toptensoftware.com/minime
// 
//   The contents of this file are subject to the license terms as 
//	 specified at the web address above.
//  
//   Software distributed under the License is distributed on an 
//   "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or
//   implied. See the License for the specific language governing
//   rights and limitations under the License.
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniME
{
	class IdentifierSpec
	{
		private string m_identifier;
		private Regex m_regex;

		public bool Parse(StringScanner s)
		{
			// Can't be empty
			if (s.eof)
				return false;

			// Regex?
			if (s.current == '/')
			{
				s.SkipForward(1);
				s.Mark();

				while (s.current != '/')
				{
					if (s.eof)
						return false;

					if (s.current == '\\')
						s.SkipForward(2);
					else
						s.SkipForward(1);
				}

				try
				{
					m_regex = new System.Text.RegularExpressions.Regex(s.Extract());
					s.SkipForward(1);
					return true;
				}
				catch (Exception)
				{
					return false;
				}
			}

			// Wildcard or explicit
			bool bWildcard = false;
			bool bLeading = true;
			s.Mark();
			while (!s.eof && s.current != '.')
			{
				// Wildcard?
				if (s.current == '?' || s.current == '*')
				{
					bWildcard = true;
					s.SkipForward(1);
					bLeading=false;
					continue;
				}

				// Valid identifier character?
				if (bLeading)
				{
					if (!Tokenizer.IsIdentifierLeadChar(s.current))
						return false;
				}
				else
				{
					if (!Tokenizer.IsIdentifierChar(s.current))
						return false;
				}

				// Next
				s.SkipForward(1);

				bLeading = false;
			}

			// Extract it
			string str = s.Extract();

			// If it ends with an asterix, it's a wildcard
			if (bWildcard)
			{
				str = str.Replace("*", "(.*)");
				str = str.Replace("?", "(.)");
				m_regex = new System.Text.RegularExpressions.Regex("^" + str + "$");
				return true;
			}

			/// Store it
			m_identifier = str;
			return true;
		}

		public bool DoesMatch(string identifier)
		{
			// Compare the name
			if (m_regex != null)
				return m_regex.IsMatch(identifier);
			else
				return m_identifier == identifier;
		}

		public bool IsWildcard
		{
			get
			{
				return m_regex != null;
			}
		}

		public string GetExplicitName()
		{
			return m_identifier;
		}
	}

	class AccessibilitySpec
	{
		public bool Parse(Accessibility accessibility, string str)
		{
			m_strSpec = str;
			m_accessibility = accessibility;

			// Create a scanner
			StringScanner s = new StringScanner(str);

			// Parse target spec
			if (s.current != '.')
			{
				m_specTarget = new IdentifierSpec();
				if (!m_specTarget.Parse(s))
					return false;
			}

			// Parse member spec
			if (s.current == '.')
			{
				s.SkipForward(1);

				if (!s.eof)
				{
					// Parse rhs
					m_specMember = new IdentifierSpec();
					if (!m_specMember.Parse(s))
						return false;
				}
			}
			else
			{
				if (m_specTarget != null)
				{
					m_specNonMember = m_specTarget;
					m_specTarget = null;
				}
				else
					return false;
			}

			if (!s.eof)
				return false;

			return true;
		}

		public bool DoesMatch(string identifier)
		{
			if (m_specNonMember != null)
				return m_specNonMember.DoesMatch(identifier);
			return false;
		}

		public bool DoesMatch(ast.ExprNodeIdentifier target, string member)
		{
			if (m_specNonMember != null)
				return false;

			// Check the member name matches
			if (m_specMember != null)
			{
				if (!m_specMember.DoesMatch(member))
					return false;
			}

			// Check the target name matches
			if (m_specTarget != null)
			{
				// Skip over `prototype` if present
				if (target.Name == "prototype" || target.Name == "__proto__")
				{
					if (target.Lhs == null)
						return false;

					if (target.Lhs.GetType() != typeof(ast.ExprNodeIdentifier))
						return false;

					target = (ast.ExprNodeIdentifier)target.Lhs;
				}

				// The target must be the left most part
				if (target.Lhs != null)
					return false;

				if (!m_specTarget.DoesMatch(target.Name))
					return false;
			}

			// Match!
			return true;
		}


		public override string ToString()
		{
			return string.Format("{0} `{1}`", m_accessibility.ToString(), m_strSpec);
		}

		public bool IsWildcard()
		{
			if (m_specNonMember!=null)
				return m_specNonMember.IsWildcard;

			// If it's a target specifier, it's always a wildcard
			if (m_specMember == null)
				return true;

			if (m_specTarget != null && m_specTarget.IsWildcard)
				return true;


			if (m_specMember.IsWildcard)
				return true;

			return false;
		}

		public string GetExplicitName()
		{
			if (m_specMember!=null)
				return m_specMember.GetExplicitName();
			if (m_specNonMember!=null)
				return m_specNonMember.GetExplicitName();
			return null;
		}

		public bool IsMemberSpec()
		{
			return m_specTarget!=null || m_specMember!=null;
		}

		public Accessibility GetAccessibility()
		{
			return m_accessibility;
		}

		Accessibility m_accessibility;
		string m_strSpec;
		IdentifierSpec m_specTarget;
		IdentifierSpec m_specMember;
		IdentifierSpec m_specNonMember;
	}

}
