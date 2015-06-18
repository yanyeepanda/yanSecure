using System;
using MonoTouch.AudioToolbox;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;

namespace yanSecure
{
	public class SpeakerOutput
	{
		public OutputAudioQueue audioQueue { get; set; }

		private readonly int packetsPerAudioQueueBuffer;

//		private AudioFileStream audioFileSteam;

		public event EventHandler<EventArgs> OnBroadcast;

		public int SampleRate
		{
			get;
			private set;
		}

		public int ChannelCount
		{
			get
			{
				return 1;
			}
		}

		public int BitsPerSample
		{
			get
			{
				return 16;
			}
		}

		public bool Active
		{
			get
			{
				return this.audioQueue.IsRunning;
			}
		}

		public SpeakerOutput (int sampleRate, int packetsPerAudioQueueBuffer)
		{
			this.SampleRate = sampleRate;
			this.packetsPerAudioQueueBuffer = packetsPerAudioQueueBuffer;
			this.Init ();
		}

		public void OutputData (String data) 
		{
			Console.WriteLine ("Speaker!");

			byte[] byteData = Convert.FromBase64String (data);

			IntPtr bufferPtr;

			audioQueue.AllocateBuffer(byteData.Length, out bufferPtr);

			IntPtr unmanagedPointer = Marshal.AllocHGlobal(byteData.Length);
			Marshal.Copy(byteData, 0, unmanagedPointer, byteData.Length);

			AudioQueue.FillAudioData (bufferPtr, 0, unmanagedPointer, 0, byteData.Length);

			audioQueue.EnqueueBuffer(bufferPtr, byteData.Length, null);

			Marshal.FreeHGlobal(unmanagedPointer);
		}

		public bool Start ()
		{
			var status = this.audioQueue.Start ();
			Console.Write ("SpearkOutput: ");
			Console.WriteLine (status);

			return (status == AudioQueueStatus.Ok);
		}

		public void Stop ()
		{
			this.audioQueue.Stop (true);
		}

		private void Init()
		{
			var audioFormat = new AudioStreamBasicDescription()
			{
				SampleRate = this.SampleRate,
				Format = AudioFormatType.LinearPCM,
				FormatFlags = AudioFormatFlags.LinearPCMIsSignedInteger | AudioFormatFlags.LinearPCMIsPacked,
				FramesPerPacket = 1,
				ChannelsPerFrame = 1,
				BitsPerChannel = this.BitsPerSample,
				BytesPerPacket = 2,
				BytesPerFrame = 2,
				Reserved = 0
			};
//
//			var format = AudioStreamBasicDescription.CreateLinearPCM (this.SampleRate, 1, 2, true);

			audioQueue = new OutputAudioQueue(audioFormat);
			audioQueue.OutputCompleted += QueueOutputCompleted;

			var bytesPerAudioQueueBuffer = this.packetsPerAudioQueueBuffer * audioFormat.BytesPerPacket;

			IntPtr bufferPtr;
			for (var index = 0; index < 3; index++)
			{
//				audioQueue.AllocateBufferWithPacketDescriptors(bytesPerAudioQueueBuffer, this.packetsPerAudioQueueBuffer, out bufferPtr);
				audioQueue.AllocateBuffer (bytesPerAudioQueueBuffer, out bufferPtr);
				audioQueue.EnqueueBuffer(bufferPtr, bytesPerAudioQueueBuffer, null);
			}
		}

		/// <summary>
		/// Handles iOS audio buffer queue completed message.
		/// </summary>
		/// <param name='sender'>Sender object</param>
		/// <param name='e'> Input completed parameters.</param>
		private void QueueOutputCompleted(object sender, OutputCompletedEventArgs e)
		{
			// return if we aren't actively monitoring audio packets
			if (!this.Active)
			{
				return;
			}

			Console.WriteLine ("Output Finished.");

			var buffer = (AudioQueueBuffer)System.Runtime.InteropServices.Marshal.PtrToStructure(e.IntPtrBuffer, typeof(AudioQueueBuffer));
			if (this.OnBroadcast != null)
			{
				// Initialize the input buffer using the actual buffer size from audio
				var outputBuffer = new byte[buffer.AudioDataByteSize];

				// Copy out the buffer from buffer.
				System.Runtime.InteropServices.Marshal.Copy(buffer.AudioData, outputBuffer, 0, (int)buffer.AudioDataByteSize);

				// Convert the byte[] buffer into string.
				var outputString = Convert.ToBase64String(outputBuffer);

				// Notify any registered event handler, passing the corresponding input audio string.
//				this.OnBroadcast(this, new OutputStreamCompletedEventArgs(outputString));
			}

//			var status = audioQueue.EnqueueBuffer(e.IntPtrBuffer, this.bufferSize, e.PacketDescriptions);  
//
//			if (status != AudioQueueStatus.Ok)
//			{
//				// todo:
//				Console.WriteLine ("Something went wrong!");
//			}
		}
	}
}

