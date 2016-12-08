using System.Collections.Generic; 

namespace Learning.Entities
{
	public class User
	{
		public User(int userId)
		{
			UserId = userId;
			SignatureList = new List<Signature>();
		}

		public User(int userId, List<Signature> signatureList) : this(userId)
		{
			SignatureList = signatureList;
		}

		public int UserId { get; set; }

		public List<Signature> SignatureList { get; set; } 
	}
}
