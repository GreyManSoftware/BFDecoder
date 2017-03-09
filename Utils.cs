using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFDecoder
{
	public static class Utils
	{
		public static string ToHex(this byte[] data)
		{
			return BitConverter.ToString(data).Replace("-", string.Empty);
		}
	}
}
