using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Learning.Entities;

namespace Learning.Managers
{ 
	public static class ConvertingDataManager
	{
		public static void ConvertDirectory(string dataDirectoryPath)
		{
			if (!Directory.Exists(dataDirectoryPath))
				throw new DirectoryNotFoundException();

			string directoryName = Path.GetFileName(dataDirectoryPath);
			var newDirectoryPath = Path.Combine(Directory.GetParent(dataDirectoryPath).FullName, directoryName + "_new");

			if (!Directory.Exists(newDirectoryPath))
				Directory.CreateDirectory(newDirectoryPath);
			else
			{
				foreach (var fileToDelete in Directory.GetFiles(newDirectoryPath))
				{
					File.Delete(fileToDelete);
				}
			}

			foreach (var filePath in Directory.EnumerateFiles(dataDirectoryPath))
			{
				var fileName = Path.GetFileName(filePath);
				var newFilePath = Path.Combine(newDirectoryPath, fileName);

				ConvertFile(filePath, newFilePath);
			}
		}

		private static void ConvertFile(string oldPath, string newPath)
		{
			var lineEnumerable = File.ReadAllLines(oldPath).Skip(1);
			List<SignaturePoint> pointList = new List<SignaturePoint>();

			foreach (var line in lineEnumerable)
			{
				var partArray = line.Split(' ');
				pointList.Add(new SignaturePoint(int.Parse(partArray[0]), int.Parse(partArray[1])));
			}

			//Clear data
			var minX = pointList.Min(i => i.X);
			var minY = pointList.Min(i => i.Y);

			foreach (var point in pointList)
			{
				point.X -= minX;
				point.Y -= minY;
			}

			//Scale data
			var maxX = pointList.Max(i => i.X);
			var maxY = pointList.Max(i => i.Y);

			double xK = 100.0/maxX;
			double yk = 100.0/maxY;

			foreach (var point in pointList)
			{
				point.X = (int)(point.X * xK);
				point.Y = (int)(point.Y * yk);

				point.Y += 2 *(50 - point.Y);
			}

			//Add line points
			var linePointList = new List<SignaturePoint>();

			for (int i = 0; i < pointList.Count - 1; i++)
			{
				linePointList.AddRange(GetLinePointList(pointList[i], pointList[i + 1]));
			}

			//Write data
			using (var fileStream = File.Create(newPath))
			{
				using (StreamWriter writer = new StreamWriter(fileStream))
				{
					foreach (var point in linePointList.Distinct())
					{
						writer.WriteLine("{0} {1}", point.X, point.Y);
					}
				}
			}
		}

		private static List<SignaturePoint> GetLinePointList(SignaturePoint first, SignaturePoint second)
		{
			//http://stackoverflow.com/questions/11678693/all-cases-covered-bresenhams-line-algorithm
			List<SignaturePoint> result = new List<SignaturePoint>();

			int x = first.X;
			int y = first.Y;
			int x2 = second.X;
			int y2 = second.Y;

			int w = x2 - x;
			int h = y2 - y;
			int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
			if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
			if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
			if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
			int longest = Math.Abs(w);
			int shortest = Math.Abs(h);
			if (!(longest > shortest))
			{
				longest = Math.Abs(h);
				shortest = Math.Abs(w);
				if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
				dx2 = 0;
			}
			int numerator = longest >> 1;
			for (int i = 0; i <= longest; i++)
			{
				result.Add(new SignaturePoint(x, y));

				numerator += shortest;
				if (!(numerator < longest))
				{
					numerator -= longest;
					x += dx1;
					y += dy1;
				}
				else
				{
					x += dx2;
					y += dy2;
				}
			}

			return result;
		}
	}
}
