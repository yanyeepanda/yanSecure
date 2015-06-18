using System;
using System.Linq;

namespace yanSecure
{
	public class BufferManager
	{
		const UInt32 kNumDrawBuffers = 2;
		const UInt32 kDefaultDrawSamples = 1024;

		float[][]     mDrawBuffers = new float[kNumDrawBuffers][];
		int         mCurrentDrawBufferLen;
		int         mDrawBufferIndex;

		private static BufferManager instance = null;

		private BufferManager (int inMaxFramesPerSlice)
		{
			for(UInt32 i = 0; i < kNumDrawBuffers; ++ i) {
				mDrawBuffers [i] = new float[inMaxFramesPerSlice];
			}

			mCurrentDrawBufferLen = (int)kDefaultDrawSamples;
		}

		public static BufferManager GetInstance ()
		{
			if (instance == null) {
				instance = new BufferManager (AudioManager.maxFramesPerSlice);
			}
			return instance;
		}

		public float[][] GetDrawBuffers ()
		{
			return mDrawBuffers;
		}

		public int GetCurrentDrawBufferLength ()
		{
			return mCurrentDrawBufferLen;
		}

		// The Audio Manager will pass data via this method.
		// Buffer Manager will need to store the data.
		public void CopyAudioDataToDrawBuffer( float[] inData, uint inNumFrames )
		{
			if (inData == null) return;

			Console.WriteLine ("Get something new from audio manager...{0}:{1}", inData.Length, inNumFrames);

			for (int i = 0; i < (int)inNumFrames; i ++)
			{
				if ((i + mDrawBufferIndex) >= mCurrentDrawBufferLen)
				{
					CycleDrawBuffers();
					mDrawBufferIndex = -i;
				}
				mDrawBuffers[0][i + mDrawBufferIndex] = inData[i];
			}
			mDrawBufferIndex += (int)inNumFrames;
		}

		private void CycleDrawBuffers()
		{
			// Cycle the lines in our draw buffer so that they age and fade. The oldest line is discarded.
			for (int drawBuffer_i = (int)(kNumDrawBuffers - 2); drawBuffer_i >= 0; drawBuffer_i --)
//				memmove(mDrawBuffers[drawBuffer_i + 1], mDrawBuffers[drawBuffer_i], mCurrentDrawBufferLen);
				Buffer.BlockCopy (mDrawBuffers[drawBuffer_i], 0, mDrawBuffers[drawBuffer_i + 1], 0, mCurrentDrawBufferLen);
		}

		public void CopyAudioData (byte[] rawData, uint numberFrames)
		{
			Console.WriteLine (String.Join(",", rawData.Take(8).ToArray()));

			var floatArray = ConvertByteArrayToFloat(rawData);
			//				Buffer.BlockCopy(bufferBytes, 0, floatArray, 0, bufferBytes.Length);

			// Converting from byte array to float array and dividing floats by 32768 to get values between 0 and 1

			Console.WriteLine (String.Join(",", floatArray.Take(2).ToArray()));

			CopyAudioDataToDrawBuffer (floatArray, (uint)floatArray.Length);
		}

		private float[] ConvertByteArrayToFloat(byte[] bytes)
		{
			if(bytes == null)
				throw new ArgumentNullException("bytes");

			if(bytes.Length % 4 != 0)
				throw new ArgumentException
				("bytes does not represent a sequence of floats");

			var floatArray = new float[bytes.Length / 2];

			for (int i = 0; i < bytes.Length / 2; i++) {
				floatArray [i] = BytesToNormalized_16 (bytes [2 * i], bytes [2 * i + 1]);
			}

			return floatArray;
//			return Enumerable.Range(0, bytes.Length / 2)
//				.Select(i => BytesToNormalized_16(bytes.Skip(2 * i).Take(2).ToArray() ))
//				.ToArray();
		}

		// Convert two bytes to one double in the range -1 to 1
		static float BytesToNormalized_16(byte[] bytes2) 
		{
			// convert two bytes to one short (big endian)
			short s = (short)(bytes2[1] << 8 | bytes2[0]);
			// convert to range from -1 to (just below) 1
			return s / 32678f;
		}

		static float BytesToNormalized_16(byte byte1, byte byte2) 
		{
			// convert two bytes to one short (big endian)
			short s = (short)(byte2 << 8 | byte1);
			// convert to range from -1 to (just below) 1
			return s / 32678f;
		}
	}
}

