using System;
using MonoTouch.AudioUnit;
using System.Collections.Concurrent;
using System.Threading;
using System.Runtime.InteropServices;
using MonoTouch.AudioToolbox;
using System.Linq;

namespace yanSecure
{
	public class AudioManager
	{

		public static int bufferSize = 0;
		public static int maxFramesPerSlice = 2048;
		private AudioUnit rioUnit = null;
		private BufferManager bufferManager;

		// This is the queue which stores all the audio buffer from the microphone. (input bus)
		private BlockingCollection<byte[]> inputBufferQueue = null;
	
		// This is the queue which provides audio buffer for speaker to consumer. (output bus)
		private BlockingCollection<byte[]> outputBufferQueue = null;

		public AudioManager (BlockingCollection<byte[]> inputBufferQueue, BlockingCollection<byte[]> outputBufferQueue)
		{
			this.inputBufferQueue = inputBufferQueue;
			this.outputBufferQueue = outputBufferQueue;
			this.bufferManager = BufferManager.GetInstance ();
			this.ProcessRawData += HandleProcessRawData;

			try {
				// Create a new instance of AURemoteIO

				AudioComponentDescription desc = new AudioComponentDescription();
				desc.ComponentType = AudioComponentType.Output;
				desc.ComponentSubType = (int)AudioTypeOutput.VoiceProcessingIO; // This is recommended for voice call, it will do some optimization
				desc.ComponentManufacturer = AudioComponentManufacturerType.Apple;
				desc.ComponentFlags = 0;
				desc.ComponentFlagsMask = 0;

				var comp = AudioComponent.FindNextComponent(null, desc);

				rioUnit = new AudioUnit (comp);

				//  Enable input and output on AURemoteIO
				AudioUnitStatus status;

				//  Input is enabled on the input scope of the input element
				status = rioUnit.SetEnableIO (true, AudioUnitScopeType.Input, 1);
				checkStatus ((int)status);

				//  Output is enabled on the input scope of the output element
				status = rioUnit.SetEnableIO (true, AudioUnitScopeType.Output, 0);
				checkStatus ((int)status);

				// Explicitly set the input and output client formats
				// sample rate = 44100, num channels = 1, format = 16 bit floating point
				var audioFormat = new AudioStreamBasicDescription()
				{
					SampleRate = 44100, // The preferred sample rate for iPhone.
					Format = AudioFormatType.LinearPCM,
					FormatFlags = AudioFormatFlags.LinearPCMIsSignedInteger | AudioFormatFlags.LinearPCMIsPacked,
					FramesPerPacket = 1, // Linear PCM
					ChannelsPerFrame = 1, // Mono
					BitsPerChannel = 16, // Bits per sample.
					BytesPerPacket = 2, // 16 / 8
					BytesPerFrame = 2, // the same as bytes per frame
					Reserved = 0
				};

				// Set the audio format for the input scope of the input element. (this data format comes from microphone, cannot be changed)
				// rioUnit.SetAudioFormat (audioFormat, AudioUnitScopeType.Input, 1);

				// Set the audio format for the output scope of the input element. This is because we use callbacks.
				rioUnit.SetAudioFormat (audioFormat, AudioUnitScopeType.Output, 1);

				// Set the audio format for the input scope of the output element. This data is coming from udp.
				rioUnit.SetAudioFormat (audioFormat, AudioUnitScopeType.Input, 0);

				// Set the audio format for the output scope of the output element. (this data format goes to speaker, cannot be changed)
				// rioUnit.SetAudioFormat (audioFormat, AudioUnitScopeType.Output, 0);

				// We need references to certain data in the render callback
				// This simple struct is used to hold that information

				// Set the render callback for the output scope of the input element to send the audio data to processor
				// We use global since it's the same as output here.
				status = rioUnit.SetInputCallback (dataInputted, AudioUnitScopeType.Global, 1);
				checkStatus ((int)status);

				// Set the render callback for the input scope of the output element to fetch the audio data from processor
				// Note the difference of this one and the one above. SetInputCallback is for input element, SetRenderCallback
				// is for output element (output bus).
				status = rioUnit.SetRenderCallback (dataToOutput, AudioUnitScopeType.Global, 0);
				checkStatus ((int)status);
	

				status = rioUnit.SetMaximumFramesPerSlice ((uint)maxFramesPerSlice, AudioUnitScopeType.Global, 1);
				checkStatus ((int)status);
				// Get the MaximumFramesPerSlice property. This property is used to describe to an audio unit the maximum number
				// of samples it will be asked to produce on any single given call to AudioUnitRender
				maxFramesPerSlice = (int)rioUnit.GetMaximumFramesPerSlice (AudioUnitScopeType.Global, 0);


				// Initialize the AURemoteIO instance
				rioUnit.Initialize ();
			}
			catch (Exception e) {
				Console.WriteLine("Error returned from setupIOUnit: %s", e.Message);
			}
		}

		void HandleProcessRawData (byte[] rawData, uint numberFrames)
		{
		
		}

		public void Start () 
		{
			// Start the AudioUnit.
			rioUnit.Start ();
		}

		public void Stop ()
		{
			// Stop the AudioUnit.
			rioUnit.Stop ();
		}

		/* This will be called when the data becomes available from microphone.
		 * The 
		 */
		private AudioUnitStatus dataInputted (AudioUnitRenderActionFlags actionFlags, AudioTimeStamp timeStamp, uint busNumber, uint numberFrames, AudioUnit audioUnit)
		{
			// Instantialize an AudioBuffers to store the input data from microphone.
			var bufferList = new AudioBuffers (1);

			// Since we have only one channel (mono) and we use Linear PCM means each packet will only contain one frame. 
			// One frame will only have one channel. So each frame will have only one sample which will be 16 bits as what
			// we've defined in the audio format part. So bytesPerFrame will be 2 bytes.
			// The buffer size will be the numberFrames * (bytesPerFrame)

			// The number of frames is actually different from devices. iPhone is 256 in such case. Mac is 512.
			// Console.WriteLine (numberFrames);

			// Getting microphone input data from I/O unit which will be stored in the data object
			rioUnit.Render (ref actionFlags, timeStamp, busNumber, numberFrames, bufferList);

			// Get the first buffer as we only have one.
			var buffer = bufferList [0];

			bufferSize = buffer.DataByteSize;

			// Add the buffer list to the queue.
			inputBufferQueue.Add (bufferToBytes (buffer));

			printBuffer (buffer);

			return AudioUnitStatus.NoError;
		}

		public delegate void BlablaDelegate(byte[] rawData, uint numberFrames);
		public event BlablaDelegate ProcessRawData;

		// This will be called when the speaker is ready to output the next buffer.
		private AudioUnitStatus dataToOutput (AudioUnitRenderActionFlags actionFlags, AudioTimeStamp timeStamp, uint busNumber, uint numberFrames, AudioBuffers data)
		{
			// Get one buffer from the bufferQueue.
			byte[] bufferBytes = new byte[data[0].DataByteSize];

			// In real time voice over ip, we have to use TryTake instead of Take to avoid delay.
			if (outputBufferQueue.TryTake (out bufferBytes)) 
			{
				// We only store one buffer in the list during the input callback. 
				// And the size of the data is always 1 when we use the audio format above.
				// Console.WriteLine (data.Count);
				data [0] = bytesToBuffer (bufferBytes);


//				// create a second float array and copy the bytes into it.
//				// One float will be 4 bytes

				Console.WriteLine (bufferBytes.Length);

				//float* floatArray = data [0].Data.ToPointer;

//				// Add bufferBytes to bufferManager
				bufferManager.CopyAudioData (bufferBytes, numberFrames);
				//ProcessRawData (bufferBytes, numberFrames);


				printBuffer (data [0]);
			}

			return AudioUnitStatus.NoError;
		}

		private void checkStatus(int status){
			if (status != 0) {
				Console.WriteLine("Status not 0! %d", status);
				//		exit(1);
			}
		}

		private byte[] bufferToBytes(AudioBuffer buffer) 
		{
			IntPtr bufferPtr = buffer.Data;

			byte[] bufferBytes = new byte[buffer.DataByteSize];
			
			Marshal.Copy (bufferPtr, bufferBytes, 0, bufferBytes.Length);

			return bufferBytes;
		}

		private AudioBuffer bytesToBuffer(byte[] bufferBytes)
		{
			AudioBuffer buffer = new AudioBuffer ();

			buffer.DataByteSize = bufferBytes.Length;

			// We are using mono.
			buffer.NumberChannels = 1;

			// Alloc memory for the buffer data.
			buffer.Data = Marshal.AllocHGlobal (buffer.DataByteSize);

			IntPtr bufferPtr = buffer.Data;

			// Set a new description.

			Marshal.Copy (bufferBytes, 0, bufferPtr, bufferBytes.Length);

			return buffer;
		}

		private void printBuffer(AudioBuffer buffer)
		{
			printBytes (bufferToBytes (buffer));
		}

		private void printBytes(byte[] bytes)
		{
//			Console.WriteLine (bytes.Length);
//			Console.WriteLine (Convert.ToBase64String (bytes));
		}
			
	}
}

