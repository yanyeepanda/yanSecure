using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.CodeDom.Compiler;
using MonoTouch.OpenGLES;
using MonoTouch.ObjCRuntime;
using MonoTouch.CoreAnimation;
using OpenTK.Graphics.ES11;
using OpenTK.Platform.iPhoneOS;
using OpenTK.Graphics;
using System.Drawing;
using System.Linq;

namespace yanSecure
{
	[Register("FFTView")]
	partial class FFTView : UIView
	{

		float[]					oscilLine;

		const UInt32 kNumDrawBuffers = 2;
		const UInt32 kDefaultDrawSamples = 1024;

		int BackingWidth;
		int BackingHeight;


//		iPhoneOSGraphicsContext Context;
		EAGLContext Context;


		uint ViewRenderBuffer, ViewFrameBuffer;
		uint DepthRenderBuffer;
		NSTimer AnimationTimer;
		internal double AnimationInterval;

		const bool UseDepthBuffer = false;

		[Export ("layerClass")]
		public static Class LayerClass ()
		{
			return new Class (typeof (CAEAGLLayer));
		}

		[Export ("initWithCoder:")]
		public FFTView (NSCoder coder) : base (coder)
		{
			CAEAGLLayer eaglLayer = (CAEAGLLayer) Layer;
			eaglLayer.Opaque = true;
			eaglLayer.DrawableProperties = NSDictionary.FromObjectsAndKeys (
				new NSObject []{NSNumber.FromBoolean(false),          EAGLColorFormat.RGBA8},
				new NSObject []{EAGLDrawableProperty.RetainedBacking, EAGLDrawableProperty.ColorFormat}
			);
//			Context = (iPhoneOSGraphicsContext) ((IGraphicsContextInternal) GraphicsContext.CurrentContext).Implementation;

//			Context.MakeCurrent(null);

			Context = new EAGLContext (EAGLRenderingAPI.OpenGLES1);
			EAGLContext.SetCurrentContext (Context);

			oscilLine = new float[kDefaultDrawSamples * 2];

			AnimationInterval = 1.0 / 60.0;
		}

		public FFTView (RectangleF frame)
		{

			this.Frame = frame;

			CAEAGLLayer eaglLayer = (CAEAGLLayer) Layer;
			eaglLayer.Opaque = true;
			eaglLayer.DrawableProperties = NSDictionary.FromObjectsAndKeys (
				new NSObject []{NSNumber.FromBoolean(false),          EAGLColorFormat.RGBA8},
				new NSObject []{EAGLDrawableProperty.RetainedBacking, EAGLDrawableProperty.ColorFormat}
			);
//			Context = (iPhoneOSGraphicsContext) ((IGraphicsContextInternal) GraphicsContext.CurrentContext).Implementation;

//			Context.MakeCurrent(null);

			Context = new EAGLContext (EAGLRenderingAPI.OpenGLES1);
			EAGLContext.SetCurrentContext (Context);
			CreateFrameBuffer ();

			oscilLine = new float[kDefaultDrawSamples * 2];

			AnimationInterval = 1.0 / 60.0;

			Console.WriteLine ("{0}:{1}", BackingWidth, BackingHeight);

			SetupView ();

			DrawView ();
		}

		public void SetupView ()
		{
//			Context.MakeCurrent(null);
//			GL.Oes.BindFramebuffer (All.FramebufferOes, ViewFrameBuffer);
			// Sets up matrices and transforms for OpenGL ES
			GL.Viewport (0, 0, BackingWidth, BackingHeight);
			GL.MatrixMode (All.Projection);
			GL.LoadIdentity ();
			GL.Ortho (0, BackingWidth, 0, BackingHeight, -1.0f, 1.0f);
			GL.MatrixMode (All.Modelview);

			// Clears the view with black
			GL.ClearColor (0.0f, 0.0f, 0.0f, 1.0f);
//			GL.Clear ((uint) All.ColorBufferBit);

			GL.EnableClientState (All.VertexArray);
		}

		public override void LayoutSubviews ()
		{
//			Context.MakeCurrent ();
			EAGLContext.SetCurrentContext (Context);
			DestroyFrameBuffer ();
			CreateFrameBuffer ();
			DrawView ();
		}

		bool CreateFrameBuffer ()
		{
			GL.Oes.GenFramebuffers (1, ref ViewFrameBuffer);
			GL.Oes.GenRenderbuffers (1, ref ViewRenderBuffer);

			GL.Oes.BindFramebuffer (All.FramebufferOes, ViewFrameBuffer);
			GL.Oes.BindRenderbuffer (All.RenderbufferOes, ViewRenderBuffer);
			Context.RenderBufferStorage ((uint) All.RenderbufferOes, (CAEAGLLayer) Layer);
			GL.Oes.FramebufferRenderbuffer (All.FramebufferOes,
				All.ColorAttachment0Oes,
				All.RenderbufferOes,
				ViewRenderBuffer);

			GL.Oes.GetRenderbufferParameter (All.RenderbufferOes, All.RenderbufferWidthOes, ref BackingWidth);
			GL.Oes.GetRenderbufferParameter (All.RenderbufferOes, All.RenderbufferHeightOes, ref BackingHeight);

			if (true) {
				GL.Oes.GenRenderbuffers (1, ref DepthRenderBuffer);
				GL.Oes.BindRenderbuffer (All.RenderbufferOes, DepthRenderBuffer);
				GL.Oes.RenderbufferStorage (All.RenderbufferOes, All.DepthComponent16Oes, BackingWidth, BackingHeight);
				GL.Oes.FramebufferRenderbuffer (All.FramebufferOes, All.DepthAttachmentOes, All.RenderbufferOes, DepthRenderBuffer);
			}
			if (GL.Oes.CheckFramebufferStatus (All.FramebufferOes) != All.FramebufferCompleteOes) {
				Console.Error.WriteLine("failed to make complete framebuffer object {0}",
					GL.Oes.CheckFramebufferStatus (All.FramebufferOes));
			}
			return true;
		}
			
		public void DrawOscilloscope ()
		{
			// Clear the view
			GL.Clear ((int)All.ColorBufferBit);

			GL.BlendFunc (All.SrcAlpha, All.One);

			GL.Color4 (1, 1, 1, 1);

			GL.PushMatrix ();

			// xy coord. offset for various devices
			float offsetY = (this.Bounds.Size.Height - 480) / 2;
			float offsetX = (this.Bounds.Size.Width - 320) / 2;

			GL.Translate (offsetX, 480 + offsetY, 0);

			GL.Rotate (-90, 0, 0, 1);

			GL.Enable (All.Texture2D);
			GL.EnableClientState (All.VertexArray);
			GL.EnableClientState (All.TextureCoordArray);

			{
				// Draw our background oscilloscope screen
				float[] vertices = {
					0, 0,
					512, 0,
					0,  512,
					512,  512,
				};

				float[] textCoords = {
					0, 0,
					1, 0,
					0, 1,
					1, 1,
				};

				GL.VertexPointer (2, All.Float, 0, vertices);
				GL.TexCoordPointer (2, All.Short, 0, textCoords);

				GL.DrawArrays (All.TriangleStrip, 0, 4);
			}


			GL.PushMatrix ();
			//
			// Translate to the left side and vertical center of the screen, and scale so that the screen coordinates
			// go from 0 to 1 along the X, and -1 to 1 along the Y
			GL.Translate (17, 182, 0);
			GL.Scale (448, 116, 1);
			////

			GL.Disable (All.Texture2D);
			GL.DisableClientState (All.TextureCoordArray);
			GL.DisableClientState (All.ColorArray);
			GL.Disable (All.LineSmooth);
			GL.LineWidth (2);


			BufferManager bufferManager = BufferManager.GetInstance ();
			float[][] drawBuffers = bufferManager.GetDrawBuffers();

			// Should be define in buffer manager
			float max = kDefaultDrawSamples;

			float[] drawBuffer_ptr;

			UInt32 drawBuffer_i;
			// Draw a line for each stored line in our buffer (the lines are stored and fade over time)
			for (drawBuffer_i  = 0; drawBuffer_i < kNumDrawBuffers; drawBuffer_i++) {

				if (drawBuffers[drawBuffer_i] == null) continue;

				//				oscilLine_ptr = oscilLine;
				drawBuffer_ptr = drawBuffers[drawBuffer_i];

				int i;
				// Fill our vertex array with point
				for (i = 0; i < max; i++) {
					oscilLine [2 * i] = (float)i / max * 4;
					oscilLine [2 * i + 1] = drawBuffer_ptr[i] * 10;
				}

				if (drawBuffer_i == 0)
					GL.Color4 (0, 1, 0, 1);
				else 
					GL.Color4 (0.0f, 1.0f, 0.0f, 0.24f * (1.0f - drawBuffer_i / 12.0f));

				// Set up vertex pointer,
				GL.VertexPointer (2, All.Float, 0, oscilLine);

				GL.DrawArrays (All.LineStrip, 0, bufferManager.GetCurrentDrawBufferLength());
			}

			GL.PopMatrix ();
			GL.PopMatrix ();
		}

		private float clamp (float min, float x, float max)
		{
			return x < min ? min : (x > max ? max : x);
		}


		// Updates the OpenGL view when the timer fires
		public void DrawView ()
		{
			// the NSTimer seems to fire one final time even though it's been invalidated
			// so just make sure and not draw if we're resigning active
			//			if (self.applicationResignedActive) return;

			// Make sure that you are drawing to the current 
			EAGLContext.SetCurrentContext (Context);

//			Context.MakeCurrent (null);

			GL.Oes.BindFramebuffer (All.FramebufferOes, ViewFrameBuffer);

			DrawOscilloscope();

			GL.Oes.BindRenderbuffer (All.RenderbufferOes, ViewRenderBuffer);
			Context.PresentRenderBuffer ((uint)All.RenderbufferOes);
		}

		void DestroyFrameBuffer ()
		{
			GL.Oes.DeleteFramebuffers (1, ref ViewFrameBuffer);
			ViewFrameBuffer = 0;
			GL.Oes.DeleteRenderbuffers (1, ref ViewRenderBuffer);
			ViewRenderBuffer = 0;

			if (DepthRenderBuffer != 0) {
				GL.Oes.DeleteRenderbuffers (1, ref DepthRenderBuffer);
				DepthRenderBuffer = 0;
			}
		}

		public void StartAnimation ()
		{
			AnimationTimer = NSTimer.CreateRepeatingScheduledTimer (TimeSpan.FromSeconds (AnimationInterval), () => DrawView ());
		}

		public void StopAnimation ()
		{
			AnimationTimer = null;
		}

		public void SetAnimationTimer (NSTimer timer)
		{
			AnimationTimer.Invalidate ();
			AnimationTimer = timer;
		}

		public void SetAnimationInterval (double interval)
		{
			AnimationInterval = interval;
			if (AnimationTimer != null) {
				StopAnimation ();
				StartAnimation ();
			}
		}
	}
}
