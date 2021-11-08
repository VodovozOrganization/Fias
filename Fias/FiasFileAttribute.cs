using System;
using System.Collections.Generic;
using System.Text;

namespace Fias
{
	public class FiasFileAttribute : Attribute
	{
		public string FiasFileTypeName { get; set; }
	}
}
