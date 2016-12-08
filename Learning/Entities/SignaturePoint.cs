namespace Learning.Entities
{
	public class SignaturePoint
	{
		public SignaturePoint()
		{
			
		}

		public SignaturePoint(int x, int y)
		{
			X = x;
			Y = y;
		}

		public int X { get; set; }
		public int Y { get; set; }

		public override bool Equals(object obj)
		{
			if (obj is SignaturePoint == false)
				return false;

			var second = (SignaturePoint) obj;
			return X.Equals(second.X) && Y.Equals(second.Y);
		}
	}
}
