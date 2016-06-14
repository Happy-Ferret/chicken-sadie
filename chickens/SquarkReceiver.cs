
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Util;

namespace chickens
{
	[BroadcastReceiver(Exported = true)]
	[IntentFilter(actions: new [] { "chickens.actions.Squark" })]
	public class SquarkReceiver : BroadcastReceiver
	{
		private string TAG = "SquarkReceiver";

		public override void OnReceive(Context context, Intent intent)
		{
			if (intent != null)
			{
				Log.Info(TAG, "Intent received, action = " + intent.Action);
			}
			else
			{
				Log.Debug(TAG, "Null intent received");
			}

		}
	}
}

