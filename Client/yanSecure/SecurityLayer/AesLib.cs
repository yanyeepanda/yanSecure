using System;
using System.Security.Cryptography;
using System.IO;

namespace yanSecure
{
	public class AesLib
	{
		private static readonly byte[] Salt = 
			new byte[] { 10, 20, 30 , 40, 50, 60, 70, 80};

		public static int IVSize = 16;

		public AesLib ()
		{
		}

		public static byte[] EncryptToBytes(byte[] plainText, byte[] Key, 
			byte[] IV, CipherMode mode)
		{
			// Check arguments. 
			if (plainText == null || plainText.Length <= 0)
				throw new ArgumentNullException("plainText");
			if (Key == null || Key.Length <= 0)
				throw new ArgumentNullException("Key");
			if (IV == null || IV.Length <= 0)
				throw new ArgumentNullException("IV");
			byte[] encrypted;
			// Create an Aes object 
			// with the specified key and IV. 
			using (Aes aesAlg = Aes.Create())
			{
				aesAlg.Key = Key;
				aesAlg.IV = IV;
				aesAlg.Mode = mode;

				// Create a decrytor to perform the stream transform.
				ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key
					, aesAlg.IV);

				// Create the streams used for encryption. 
				using (MemoryStream msEncrypt = new MemoryStream())
				{
					using (CryptoStream csEncrypt = new CryptoStream(msEncrypt
						, encryptor, CryptoStreamMode.Write))
					{
						using (BinaryWriter swEncrypt = new BinaryWriter(csEncrypt))
						{

							//Write all data to the stream.
							swEncrypt.Write(plainText);
						}



						encrypted = msEncrypt.ToArray();
					}
				}
			}
				
			// Return the encrypted bytes from the memory stream. 
			return encrypted;

		}

		public static byte[] DecryptFromBytes(byte[] cipherText, byte[] Key
			, byte[] IV, CipherMode mode)
		{
			// Check arguments. 
			if (cipherText == null || cipherText.Length <= 0)
				throw new ArgumentNullException("cipherText");
			if (Key == null || Key.Length <= 0)
				throw new ArgumentNullException("Key");
			if (IV == null || IV.Length <= 0)
				throw new ArgumentNullException("IV");

			// Declare the string used to hold 
			// the decrypted text. 
			byte[] plaintext = new byte[cipherText.Length];

			// Create an Aes object 
			// with the specified key and IV. 
			using (Aes aesAlg = Aes.Create())
			{
				aesAlg.Key = Key;
				aesAlg.IV = IV;
				aesAlg.Mode = mode;

				// Create a decrytor to perform the stream transform.
				ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key
					, aesAlg.IV);

				// Create the streams used for decryption. 
				using (MemoryStream msDecrypt = new MemoryStream(cipherText))
				{
					using (CryptoStream csDecrypt = new CryptoStream(msDecrypt
						, decryptor, CryptoStreamMode.Read))
					{
						using (BinaryReader srDecrypt = new BinaryReader(
							csDecrypt))
						{

							// Read the decrypted bytes from the decrypting stream
							// and place them in a string.
							plaintext = srDecrypt.ReadBytes (cipherText.Length);
						}
					}
				}

			}

			return plaintext;

		}

		public static byte[] CreateKey(string password, int keyBytes = 32)
		{
			const int Iterations = 300;
			var keyGenerator = new Rfc2898DeriveBytes(password, Salt, Iterations);
			return keyGenerator.GetBytes(keyBytes);
		}
	}
}

