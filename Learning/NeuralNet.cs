using System;
using System.Collections.Generic;
using System.Linq;
using ConvNetSharp;
using Learning.Entities;
using Learning.Managers;

namespace Learning
{
	public class NeuralNet
	{
		private readonly Random random = new Random();
		private readonly CircularBuffer<double> trainAccWindow = new CircularBuffer<double>(100);
		private readonly CircularBuffer<double> valAccWindow = new CircularBuffer<double>(100);
		private readonly CircularBuffer<double> wLossWindow = new CircularBuffer<double>(100);
		private readonly CircularBuffer<double> xLossWindow = new CircularBuffer<double>(100);
		private Net net;
		private int stepCount;
		private Trainer trainer;
		List<User> userList;
		int neededUserId = 1;
		int imageSize = 105;
		int imageUsingSize = 101;

		public void Demo()
		{
			// Load data
			userList = ReadingManager.ReadData(@"C:\Users\RustamSalakhutdinov\Documents\visual studio 2015\Projects\signatureChecker\data_new");

			// Create network
			this.net = new Net();
			this.net.AddLayer(new InputLayer(imageSize, imageSize, 1));
			this.net.AddLayer(new ConvLayer(25, 25, 8) { Stride = 1, Pad = 2, Activation = Activation.Relu });
			this.net.AddLayer(new PoolLayer(2, 2) { Stride = 2 });
			this.net.AddLayer(new ConvLayer(5, 5, 16) { Stride = 1, Pad = 2, Activation = Activation.Relu });
			this.net.AddLayer(new PoolLayer(3, 3) { Stride = 3 });
			this.net.AddLayer(new SoftmaxLayer(2));

			this.trainer = new Trainer(this.net)
			{
				BatchSize = 20,
				L2Decay = 0.001,
				TrainingMethod = Trainer.Method.Adadelta
			};

			do
			{
				var sample = this.SampleTrainingInstance();
				if (!Step(sample))
					break;
			} while (!Console.KeyAvailable);

			foreach (User user in userList)
			{
				var signature = user.SignatureList[0];

				var x = new Volume(imageSize, imageSize, 1, 0.0);

				foreach (var point in signature.SignaturePointList)
				{
					x.Weights[point.X * 101 + point.Y] = 1;
				}

				x = x.Augment(imageUsingSize);

				var result = net.Forward(x);
				Console.WriteLine("UserId: {0}. Result: {1} | {2}", user.UserId, result.Weights[0], result.Weights[1]);
			}
		}

		private bool Step(Item sample)
		{
			var x = sample.Volume;
			var y = sample.Label;

			if (sample.IsValidation)
			{
				// use x to build our estimate of validation error
				this.net.Forward(x);
				var yhat = this.net.GetPrediction();
				var valAcc = yhat == y ? 1.0 : 0.0;
				this.valAccWindow.Add(valAcc);
				return true;
			}

			// train on it with network
			this.trainer.Train(x, y);
			var lossx = this.trainer.CostLoss;
			var lossw = this.trainer.L2DecayLoss;

			// keep track of stats such as the average training error and loss
			var prediction = this.net.GetPrediction();
			var trainAcc = prediction == y ? 1.0 : 0.0;
			this.xLossWindow.Add(lossx);
			this.wLossWindow.Add(lossw);
			this.trainAccWindow.Add(trainAcc);

			if (this.stepCount % 200 == 0)
			{
				if (this.xLossWindow.Count == this.xLossWindow.Capacity)
				{
					var xa = this.xLossWindow.Items.Average();
					var xw = this.wLossWindow.Items.Average();
					var loss = xa + xw;

					Console.WriteLine("Loss: {0} Train accuray: {1} Test accuracy: {2}", loss,
						Math.Round(this.trainAccWindow.Items.Average() * 100.0, 2),
						Math.Round(this.valAccWindow.Items.Average() * 100.0, 2));

					Console.WriteLine("Example seen: {0} Fwd: {1}ms Bckw: {2}ms", this.stepCount,
						Math.Round(this.trainer.ForwardTime.TotalMilliseconds, 2),
						Math.Round(this.trainer.BackwardTime.TotalMilliseconds, 2));

					if (trainAccWindow.Items.Average()*100.0 < 0.05)
						return false;
				}
			}

			if (this.stepCount % 1000 == 0)
			{
				this.TestPredict();
			}

			this.stepCount++;

			return true;
		}

		private void TestPredict()
		{
			for (var i = 0; i < 50; i++)
			{
				List<Item> sample = this.SampleTestingInstance();
				var y = sample[0].Label; // ground truth label

				// forward prop it through the network
				var average = new Volume(1, 1, 2, 0.0);
				var n = sample.Count;
				for (var j = 0; j < n; j++)
				{
					var a = this.net.Forward(sample[j].Volume);
					average.AddFrom(a);
				}
			}
		}

		private Item SampleTrainingInstance()
		{
			Random random = new Random();
			var user = userList[random.Next(userList.Count)];
			var signature = user.SignatureList[random.Next(user.SignatureList.Count)];

			// Create volume from image data
			var x = new Volume(imageSize, imageSize, 1, 0.0);

			foreach (var point in signature.SignaturePointList)
			{
				x.Weights[point.X*101 + point.Y] = 1;
			}

			x = x.Augment(imageUsingSize);

			return new Item { Volume = x, Label = user.UserId == neededUserId? 1 : 0, IsValidation = random.Next(10) == 0 };
		}

		private List<Item> SampleTestingInstance()
		{
			List<Item> result = new List<Item>(4);
			for (int i = 0; i < 4; i++)
			{
				Item instance = SampleTrainingInstance();
				instance.IsValidation = false;
				result.Add(instance);
			}

			return result;
		}

		private class Item
		{
			public Volume Volume { get; set; }

			public int Label { get; set; }

			public bool IsValidation { get; set; }
		}
	}
}
