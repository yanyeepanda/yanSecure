using System;
using System.Threading;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MonoTouch.AudioToolbox;
using MonoTouch.AVFoundation;
using UdpPunchClient;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Rabbit;

namespace yanSecure
{
	partial class MainViewController : UIViewController
	{
		private Cryptographist cryptographist;
		private FFTView fftView = null;

		public MainViewController (IntPtr handle) : base (handle)
		{

		}
			
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			// Set up the audio session.
			setupAudioSession ();
			NameField.ShouldReturn += ShouldReturn;
			ChatKeyField.ShouldReturn += ShouldReturn;

		}

		void HandlePeerDataReceived (IPAddress remoteIP, int remotePort, Socket socket)
		{
//			Console.WriteLine (peer);
			Console.WriteLine(string.Format("Connected to {0}:{1}", remoteIP, remotePort));

			// Start a process to handle the sending to the target port and ip, 
			// another process to handle the receiving with the port sent to server.

			/*OutputThread outputThread = new OutputThread ();
            Thread oThread = new Thread(new ThreadStart(outputThread.run));

            oThread.Start();

            InputThread inputThread = new InputThread ();
            Thread iThread = new Thread(new ThreadStart(inputThread.run));

            iThread.Start();*/

//			var sending_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			var remoteEndPoint = new IPEndPoint(remoteIP, remotePort);
			var udpSender = new UdpSender (socket, remoteEndPoint);
			var udpListener = new UdpListener (socket);

			var key = sha256(NameField.Text);

			setCryptographist (key);

			var inputDataQueue = new BlockingCollection<byte[]>(10000);

			var outputDataQueue = new BlockingCollection<byte[]>(10000);

			var outputThread = new OutputThread();
			var inputThread = new InputThread();
			var audioManager = new AudioManager(inputDataQueue, outputDataQueue);

			var f = new TaskFactory(TaskCreationOptions.LongRunning,
				TaskContinuationOptions.None);
				
			f.StartNew(() => outputThread.Run(outputDataQueue, udpListener, cryptographist));
			f.StartNew(() => inputThread.Run(inputDataQueue, udpSender, cryptographist));
			f.StartNew(() => audioManager.Start());
		
			fftView = new FFTView (View.Frame);
			View.AddSubview (fftView);
//			fftView.StartAnimation ();
			StartAnimation ();

		}

		public void StartAnimation ()
		{
			NSTimer.CreateRepeatingScheduledTimer (TimeSpan.FromSeconds (1 / 30), () => fftView.DrawView ());
		}

		bool ShouldReturn (UITextField textField)
		{
			textField.ResignFirstResponder ();
			return false;
		}

		private void inputAvailabilityChangedNow(Object sender, AVStatusEventArgs args) 
		{
			Console.WriteLine ("INPUT CHANGED");
		}

		private void setupAudioSession() 
		{
			try {
				// Obtain the reference to the singleton instance of AVAudioSession.
				var audioSession = AVAudioSession.SharedInstance ();

				// Ask for permission if needed
				if (audioSession.RecordPermission != AVAudioSessionRecordPermission.Granted) {
					audioSession.RequestRecordPermission (permissionRequestCallback);
					return;
				}

				NSError error = null;

				// We are going to do voice over ip, so pick that category.
				error = audioSession.SetCategory (AVAudioSessionCategory.PlayAndRecord, AVAudioSessionCategoryOptions.DefaultToSpeaker);
				throwIfError (error);

				// Set the mode for voice over ip. DO NOT set this. The default to speaker will be invalidate and there won't
				// be output from speaker any more.
//				audioSession.SetMode (AVAudioSession.ModeVoiceChat, out error);
//				throwIfError (error);

				// Buffer Duration and Sample Rate are two critical settings for optimization the audio communication.
				// To enable the best audio quality, set the bufferDuration to 5ms and sample rate to 44100HZ.
				// To enable the fast network transmission, set the bufferDuration to 500ms and sample rate to 8000HZ.

				// Since we require very low delay, so set the buffer duration to a proper value. To low will end up with bad quality.
//				double bufferDuration = .5;
//				audioSession.SetPreferredIOBufferDuration (bufferDuration, out error);
//				throwIfError (error);

				// Set the sample rate as the hardware sample rate to avoid sample rate conversion. 8000 is actually enough for audio call.
				double sampleRate = 44100;
				audioSession.SetPreferredSampleRate (sampleRate, out error);
				throwIfError (error);

				// Check the actuall settings.
				Console.WriteLine ("Current sample rate is: " + audioSession.SampleRate);
				Console.WriteLine ("Current IO Buffer Duration is: " + audioSession.IOBufferDuration);


				// TODO Add interruption handler

				// TODO Add route change notification

				// TODO Add reset handler

				// Activate the audio session
				audioSession.SetActive (true, out error);
				throwIfError (error);

//				// add interruption handler
//				[[NSNotificationCenter defaultCenter] addObserver:self
//					selector:@selector(handleInterruption:)
//					name:AVAudioSessionInterruptionNotification
//					object:sessionInstance];
//
//				// we don't do anything special in the route change notification
//				[[NSNotificationCenter defaultCenter] addObserver:self
//					selector:@selector(handleRouteChange:)
//					name:AVAudioSessionRouteChangeNotification
//					object:sessionInstance];
//
//				// if media services are reset, we need to rebuild our audio chain
//				[[NSNotificationCenter defaultCenter]	addObserver:	self
//					selector:	@selector(handleMediaServerReset:)
//					name:	AVAudioSessionMediaServicesWereResetNotification
//					object:	sessionInstance];
			}
			catch (NSErrorException e) {
				Console.WriteLine ("Error returned from setupAudioSession: %s", e.Message);
			}
			catch {
				Console.WriteLine ("Unknown error returned from setupAudioSession");
			}
		}

		private void permissionRequestCallback (bool granted) 
		{
			if (!granted) 
			{
				var alert = new UIAlertView ("Warning", "Without Permission, the functionalities of vSecure will be limited. Please permit.", 
					null, "OK", null);
				alert.Show ();
			}
		}

        partial void shotBtnTapped (UIButton sender) 
		{
			// Using web service to get the target information from the server
			/*
			var serverInterface = new ServerInterface ("128.199.146.244", 5060, ChatKeyField.Text);
			serverInterface.ReceivedConnection += HandlePeerDataReceived;
			serverInterface.Connect ();
			*/

			// Fake the data to test locally.
			var localTest = IPAddress.Parse ("127.0.0.1");
			HandlePeerDataReceived(localTest, 9999, null);

    		// Check the input is valid

			// Send request to server, with the name, the ip, the port and the secret key.

			// After get the response from server, parse out the target name, target ip and target port.
		}

		private void throwIfError (NSError err)
		{
			if (err != null) 
			{
				throw new NSErrorException (err);
			}
		}

		static byte[] sha256(string password)
		{
			SHA256Managed crypt = new SHA256Managed();
			byte[] crypto = crypt.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password), 0, System.Text.Encoding.UTF8.GetByteCount(password));

			return crypto;
		}

		private void setCryptographist (byte[] key)
		{
			Console.WriteLine (Cryptographist.SelectedSegment);

			switch (Cryptographist.SelectedSegment) 
			{
			case 0:
				cryptographist = new VAes (key, CipherMode.CBC);
				break;
			case 1:
				cryptographist = new VAes (key, CipherMode.ECB);
				break;
			case 2:
				cryptographist = new RC4 (key);
				break;
			case 3:
				cryptographist = new Rabbit (key);
				break;
			default:
				Console.WriteLine ("Invalid Cryptographist.");
				break;
			} 
		}

	}
}
