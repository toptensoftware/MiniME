using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniME
{
	/*
	 * This is a direct port of Isaac Schlueter's CSS minification rules.
	 * Source: git://github.com/isaacs/cssmin.git
	 */


	public class CssMin
	{
		public CssMin()
		{
		}

		Regex Regex(string pattern)
		{
			return new Regex(pattern,
					RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);
		}

		public string hex(string s)
		{
			var val=int.Parse(s);
			return	string.Format("{0:X2}", val);
		}

		public string Minify(string input, int MaxLineLength)
		{
			var PreservedComments = new List<string>();
			// remove preserved comments
			input = Regex(@"\/\*!(.*?)\*\/").Replace(input, m =>
					{
						PreservedComments.Add(m.Groups[1].Value);
						return string.Format("___PRESERVED_COMMENT_{0}___", PreservedComments.Count - 1);
					}
			);

			// remove comment blocks, everything between /* and */
			input = Regex(@"\/\*(.*?)\*\/").Replace(input, "");

			// normalize whitespace
			input = Regex(@"\s+").Replace(input, " ");

			// support the box model hack (although it's a bit dated now)
			input = Regex(@"""\\""}\\""""").Replace(input, "___BMHCRAZINESS___");

			// Remove the spaces before the things that should not have spaces before them.
			// But, be careful not to turn "p :link {...}" into "p:link{...}"
			// Swap out any selector colons with a token, and then swap back.
			input = Regex(@"(^|\})([^{]+)\{").Replace(input, m=>
					{
						return m.Value.Replace(":", "___SELECTORCOLON___");
					}
			);
			input = Regex(@"\s+([!{};:>+\(\)\],])").Replace(input, "$1");
			input = input.Replace("___SELECTORCOLON___", ":");


			// If there is a @charset, then only allow one, and push to the top of the file.
			input = Regex(@"^(.*)(@charset ""[^""]*"";)").Replace(input, "$2$1");
			input = Regex(@"^(\s*@charset [^;]+;\s*)+").Replace(input, "$1");

			// Put the space back in some cases, to support stuff like
			// @media screen and (-webkit-min-device-pixel-ratio:0){
			// where @media screen and(-webkit-...){ would fail.
			input = Regex(@"(@media[^{]*[^\s])\(").Replace(input, "$1 (");

			// Remove the spaces after the things that should not have spaces after them.
			input = Regex(@"([!{}:;>+\(\[,])\s+").Replace(input, "$1");

			// Add the semicolon where it's missing. This is no longer necessary,
			// and will be removed later, but it makes a few of the next rules simpler.
			input = Regex(@"([^;\}])}").Replace(input, "$1;}");

			// Replace 0(px,em,%) with 0.
			input = Regex(@"([\s:])(0)(px|em|%|in|cm|mm|pc|pt|ex)").Replace(input, "$1$2");

				// Replace 0 0 0 0; with 0.
			input = Regex(@":0 0 0 0;").Replace(input, ":0;");
			input = Regex(@":0 0 0;").Replace(input, ":0;");
			input = Regex(@":0 0;").Replace(input, ":0;");

			// Replace background-position:0; with background-position:0 0;
			// since we just broke that with the last bit.
			input = Regex(@"background-position:0;").Replace(input, "background-position:0 0;");

			// Replace 0.6 to .6, but only when preceded by : or a white-space
			input = Regex(@"(:|\s)0+\.(\d+)").Replace(input, "$1.$2");

			// Shorten colors from rgb(51,102,153) to #336699
			// This makes it more likely that it'll get further compressed in the next step.
			input = Regex(@"rgb\(\s*([0-9]{1,3})\s*,\s*([0-9]{1,3})\s*,\s*([0-9]{1,3})\)").Replace(input, 
					delegate(Match m)
					{
						return "#"
							+ hex(m.Groups[1].Value)
							+ hex(m.Groups[2].Value)
							+ hex(m.Groups[3].Value);

					}
				);

			// normalize #aBc to #ABC.  These will be in groups of either 3 or 6
			input = Regex(@"#([a-fA-F0-9]{3}){1,2}\b").Replace(input, m => m.Value.ToUpperInvariant());

			// Shorten colors from #AABBCC to #ABC. Note that we want to make sure
			// the color is not preceded by either ", " or =. Indeed, the property
			//     filter: chroma(color="#FFFFFF");
			// would become
			//     filter: chroma(color="#FFF");
			// which makes the filter break in IE.
			input = Regex(@"([^""'=\s])(\s*)#([0-9A-F])\3([0-9A-F])\4([0-9A-F])\5\b").Replace(input, "$1$2#$3$4$5");

			// Replace multiple semi-colons in a row by a single one
			// See SF bug #1980989
			input = Regex(@";;+").Replace(input, ";");

			// Remove the final semicolons
			input = Regex(@";}").Replace(input, "}");

			// Remove empty rules.
			input = Regex(@"[^}]+{}").Replace(input, "");

			// Replace the pseudo class for the Box Model Hack
			input = input.Replace(@"___BMHCRAZINESS___", @"""\""}\""""");

			// And trim
			input = input.Trim();

			// Put back preserved comments
			for (int i = 0; i < PreservedComments.Count; i++)
			{
				input = Regex(string.Format(@"___PRESERVED_COMMENT_{0}___[ ]?", i)).Replace(input, "/*" + PreservedComments[i] + "*/\n");
			}

			// Insert line breaks
			input = InsertLineBreaks(input, MaxLineLength);

			return input;
		}

		public string InsertLineBreaks(string input, int MaxLineLength)
		{
			// Disabled?
			if (MaxLineLength <= 0)
				return input;
			const string hack = @"""\""}\""""";

			var buf = new StringBuilder();
			int iLineStartOffset = 0;
			int iPreviousBreakPos = 0;
			int i = 0;
			while (i < input.Length)
			{
				// look for a suitable break pos
				if (input[i] == '{' || input[i] == ';' || input[i] == '}')
				{
					// check it's not a box model hack
					if (input[i] == '}' && i >= 3 && i+hack.Length < input.Length)
					{
						if (input.Substring(i - 3, hack.Length) == hack)
						{
							// It is, ignore it
							i++;
							continue;
						}
					}

					iPreviousBreakPos = input[i] == '{' ? i : i + 1;

					i++;
					continue;
				}

				// Are we past the line length?
				if (i - iLineStartOffset > MaxLineLength && iPreviousBreakPos > iLineStartOffset)
				{
					// Yes
					buf.Append(input, iLineStartOffset, iPreviousBreakPos - iLineStartOffset);
					buf.Append("\n");
					iLineStartOffset = iPreviousBreakPos;
				}

				// Next character
				i++;
			}

			// Append the rest
			buf.Append(input, iLineStartOffset, input.Length - iLineStartOffset);

			return buf.ToString();
		}

	}
}

