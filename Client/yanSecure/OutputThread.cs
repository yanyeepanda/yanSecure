using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using MonoTouch.AudioToolbox;

namespace yanSecure
{
	public class OutputThread
	{
		public OutputThread ()
		{

		}

		private OutputProcessor outputProcessor = new OutputProcessor();
		private Cryptographist cryptographist;
        private UdpListener listener;

		private static int limit = 10000;

		// Queues

		private BlockingCollection<byte[]> receivedDataQueue = new BlockingCollection<byte[]> (limit);
		private BlockingCollection<byte[]> decryptedDataQueue = new BlockingCollection<byte[]> (limit);
		private BlockingCollection<byte[]> outputtingDataQueue = null;

		public void Run(BlockingCollection<byte[]> outputtingDataQueue, UdpListener listener, Cryptographist cryptographist) {
			this.outputtingDataQueue = outputtingDataQueue;
			this.listener = listener;
			this.cryptographist = cryptographist;

			var f = new TaskFactory(TaskCreationOptions.LongRunning, 
				TaskContinuationOptions.None);
				
			var receivingStage = f.StartNew(() => receiveData(receivedDataQueue));
			var decryptionStage = f.StartNew(() => operateData (receivedDataQueue, decryptedDataQueue, cryptographist.Decrypt));
			var processingStage = f.StartNew(() => operateData (decryptedDataQueue, outputtingDataQueue, outputProcessor.processData));


			Task.WaitAll(receivingStage, decryptionStage, processingStage);
		}

		public void receiveData (BlockingCollection<byte[]> receivedDataQueue) {
			while (true) {
                listener.receiveData(receivedDataQueue);
			}
		}

		private void operateData (BlockingCollection<byte[]> inDataQueue, BlockingCollection<byte[]> outDataQueue, Func<byte[], byte[]>Operator) {
			while (true) {
				try {
					var nextInData = inDataQueue.Take ();

					var operatedData = Operator (nextInData);

					outDataQueue.Add (operatedData);
				} catch (Exception ex) {
					Console.WriteLine (ex.Message);
					outDataQueue.CompleteAdding ();
				}
			}
		}
	}
}

