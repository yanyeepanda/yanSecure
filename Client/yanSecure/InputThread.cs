using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;

namespace yanSecure
{
	public class InputThread
	{
		private static int limit = 10000;

		public InputThread ()
		{

		}

		private InputProcessor inputProcessor = new InputProcessor();
		private Cryptographist cryptographist;
        private UdpSender sender;


		// Queues
		private BlockingCollection<byte[]> inputtedDataQueue = null;
		private BlockingCollection<byte[]> processedDataQueue = new BlockingCollection<byte[]> (limit);
		private BlockingCollection<byte[]> encryptedDataQueue = new BlockingCollection<byte[]> (limit);

		public void Run(BlockingCollection<byte[]> inputtedDataQueue, UdpSender sender, Cryptographist cryptographist) {

			this.inputtedDataQueue = inputtedDataQueue;
			this.sender = sender;
			this.cryptographist = cryptographist;

			var f = new TaskFactory(TaskCreationOptions.LongRunning, 
				TaskContinuationOptions.None);
				
			var processingStage = f.StartNew(() => operateData (inputtedDataQueue, processedDataQueue, inputProcessor.processData));
			var encryptionStage = f.StartNew(() => operateData (processedDataQueue, encryptedDataQueue, cryptographist.Encrypt));
			var sendingStage = f.StartNew(() => sendData(encryptedDataQueue));

			Task.WaitAll(processingStage, encryptionStage, sendingStage);
		}
			
		#region private methods

		private void operateData (BlockingCollection<byte[]> inDataQueue, BlockingCollection<byte[]> outDataQueue, Func<byte[], byte[]>Operator) {
			while (true) {
				try {
					var nextInData = inDataQueue.Take ();

					var operatedData = Operator (nextInData);

					outDataQueue.Add (operatedData);
				} catch (Exception ex) {
					Console.WriteLine (ex.Message);
					processedDataQueue.CompleteAdding ();
				}
			}
		}

		private void sendData(BlockingCollection<byte[]> encryptedDataQueue) {
			while (true) {
				try {
                    sender.sendData(encryptedDataQueue);

				} catch (Exception ex) {
					Console.WriteLine (ex.Message);
				}
			}
		}

		#endregion
	}
}

