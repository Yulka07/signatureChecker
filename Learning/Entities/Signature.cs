using System.Collections.Generic;

namespace Learning.Entities
{
	public class Signature
	{
		public Signature(List<SignaturePoint> signaturePointList)
		{
			SignaturePointList = signaturePointList;
		}

		public List<SignaturePoint> SignaturePointList { get; set; } 
	}
}
