using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MiniME
{
	public class TextFileUtils
	{
		static bool DoesPreambleMatch(byte[] data, byte[] preamble)
		{
			for (int i=0; i<preamble.Length; i++)
			{
				if (data[i]!=preamble[i])
					return false;
			}

			return true;
		}

		public static EncodingInfo DetectFileEncoding(string FileName)
		{
			// Build a list of encodings sorted by preamble size (largest->smallest)
			var EncodingsByPreambleSize = 
						(from e in Encoding.GetEncodings()
						  where e.GetEncoding().GetPreamble()!=null 
						  orderby e.GetEncoding().GetPreamble().Length descending
						  select e).ToList();

			if (EncodingsByPreambleSize.Count==0)
				return null;

			int MaxPreamble=EncodingsByPreambleSize[0].GetEncoding().GetPreamble().Length;
			byte[] buf = new byte[MaxPreamble];

			using (FileStream stream = File.OpenRead(FileName))
			{
				// Work out how much to read
				int ReadLen = Math.Min((int)stream.Length, MaxPreamble);
				if (ReadLen == 0)
					return null;

				// Read it
				stream.Read(buf, 0, (int)ReadLen);
			}

			// Find matching encoding
			return (from e in EncodingsByPreambleSize
					where DoesPreambleMatch(buf, e.GetEncoding().GetPreamble())
					select e).FirstOrDefault();
		}

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

