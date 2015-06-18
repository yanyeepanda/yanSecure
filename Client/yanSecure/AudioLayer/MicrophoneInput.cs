using System;
using System.Text;
using System.Threading;
using MonoTouch.AudioToolbox;
using MonoTouch.AVFoundation;

namespace yanSecure
{
	public class MicrophoneInput
	{
		private InputAudioQueue audioQueue;

		private readonly int packetsPerAudioQueueBuffer;

		#region IAudioStream implementation

		public event EventHandler<EventArgs> OnBroadcast;

		public int AverageBytesPerSecond
		{
			get
			{
				throw new NotImplementedException ();
			}
		}

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

		#endregion

		#region IMonitor implementation

//		public event EventHandler<EventArgs<bool>> OnActiveChanged;
//
//		public event EventHandler<EventArgs<Exception>> OnException;

		public bool Start ()
		{
			var status = this.audioQueue.Start ();
			Console.Write ("MicrophoneInput: ");
			Console.WriteLine (status);

			return (status == AudioQueueStatus.Ok);
		}

		public void Stop ()
		{
			this.audioQueue.Stop (true);
		}

		public bool Active
		{
			get
			{
				return this.audioQueue.IsRunning;
			}
		}

		#endregion

		public MicrophoneInput(int sampleRate, int packetsPerAudioQueueBuffer)
		{
			this.SampleRate = sampleRate;
			this.packetsPerAudioQueueBuffer = packetsPerAudioQueueBuffer;
			this.Init ();
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

			audioQueue = new InputAudioQueue(audioFormat);
			audioQueue.InputCompleted += QueueInputCompleted;

			// One audio queue buffer contains many packets.
			var bytesPerAudioQueueBuffer = this.packetsPerAudioQueueBuffer * audioFormat.BytesPerPacket;
//
			IntPtr bufferPtr;
			for (var index = 0; index < 3; index++)
			{
				audioQueue.AllocateBuffer (bytesPerAudioQueueBuffer, out bufferPtr);
//				audioQueue.AllocateBufferWithPacketDescriptors(bytesPerAudioQueueBuffer, this.packetsPerAudioQueueBuffer, out bufferPtr);

				// Enqueue the new buffer to the queue. Use null since the size of the queuebuffer is fixed.
				audioQueue.EnqueueBuffer(bufferPtr, bytesPerAudioQueueBuffer, null);
			}
		}

		/// <summary>
		/// Handles iOS audio buffer queue completed message.
		/// </summary>
		/// <param name='sender'>Sender object</param>
		/// <param name='e'> Input completed parameters.</param>
		private void QueueInputCompleted(object sender, InputCompletedEventArgs e)
		{
			// return if we aren't actively monitoring audio packets
			if (!this.Active)
			{
				return;
			}

			var buffer = (AudioQueueBuffer)System.Runtime.InteropServices.Marshal.PtrToStructure(e.IntPtrBuffer, typeof(AudioQueueBuffer));
			if (this.OnBroadcast != null)
			{
				// Initialize the input buffer using the actual buffer size from audio, the size should be the same as
				// bytesPerAudioQueueBuffer above.
				var inputBuffer = new byte[buffer.AudioDataByteSize];

				// Copy out the buffer from buffer.
				System.Runtime.InteropServices.Marshal.Copy(buffer.AudioData, inputBuffer, 0, (int)buffer.AudioDataByteSize);

				// Convert the byte[] buffer into string.
				var inputString = Convert.ToBase64String(inputBuffer);

				// Notify any registered event handler, passing the corresponding input audio string.
//				this.OnBroadcast(this, new InputStreamCompletedEventArgs(inputString));
			}

			var bytesPerAudioQueueBuffer = this.packetsPerAudioQueueBuffer * audioQueue.AudioStreamPacketDescription.BytesPerPacket;

			var status = audioQueue.EnqueueBuffer(e.IntPtrBuffer, bytesPerAudioQueueBuffer, e.PacketDescriptions);  

			if (status != AudioQueueStatus.Ok)
			{
				// todo:
				Console.WriteLine ("Something went wrong!");
			}
		}
	}
}

