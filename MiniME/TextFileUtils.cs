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
using System.IO;

namespace MiniME
{
	public class TextFileUtils
	{
		// Check if leading bytes in a file match a specified preamble
		static bool DoesPreambleMatch(byte[] data, byte[] preamble)
		{
			for (int i=0; i<preamble.Length; i++)
			{
				if (data[i]!=preamble[i])
					return false;
			}

			return true;
		}

		// Detect the encoding of a file by comparing it's leading bytes
		// against all known encoding preambles.
		public static EncodingInfo DetectFileEncoding(string FileName)
		{
			// Build a list of encodings sorted by preamble size (largest->smallest)
			var EncodingsByPreambleSize = 
						(from e in Encoding.GetEncodings()
						  let p=e.GetEncoding().GetPreamble()
						  where p!=null && p.Length>0
						  orderby e.GetEncoding().GetPreamble().Length descending
						  select e).ToList();

			// Quit if there are none (that would be weird)
			if (EncodingsByPreambleSize.Count==0)
				return null;

			// Work out the max preamble size and allocate a buffer for it
			int MaxPreamble=EncodingsByPreambleSize[0].GetEncoding().GetPreamble().Length;
			byte[] buf = new byte[MaxPreamble];

			// Open the file, read the preamble
			using (FileStream stream = File.OpenRead(FileName))
			{
				// Work out how much to read
				int ReadLen = Math.Min((int)stream.Length, MaxPreamble);
				if (ReadLen == 0)
					return null;

				// Read it
				stream.Read(buf, 0, (int)ReadLen);
			}

			// Find an encoding with a matching preamble
			return (from e in EncodingsByPreambleSize
					where DoesPreambleMatch(buf, e.GetEncoding().GetPreamble())
					select e).FirstOrDefault();
		}

		// Find an encoding given a name (used to parse command line arguments to MiniME)
		public static Encoding EncodingFromName(string Name)
		{
			var ei=(from e in Encoding.GetEncodings()
			 where e.Name==Name || e.CodePage.ToString()==Name
			 select e).SingleOrDefault();
			if (ei == null)
				return null;
			else
				return ei.GetEncoding();
		}
	}
}

