// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.CodeDom.Compiler;

namespace yanSecure
{
	[Register ("MainViewController")]
	partial class MainViewController
	{
		[Outlet]
		MonoTouch.UIKit.UITextField ChatKeyField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UISegmentedControl Cryptographist { get; set; }

		[Outlet]
		MonoTouch.UIKit.UISwitch MITMSwitch { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField NameField { get; set; }

		[Action ("shotBtnTapped:")]
		[GeneratedCode ("iOS Designer", "1.0")]
		partial void shotBtnTapped (UIButton sender);

		void ReleaseDesignerOutlets ()
		{
		}
	}
}
