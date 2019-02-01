using System;
using Android.Content;
using Android.App;
using Android.Util;

namespace chickens
{
	[BroadcastReceiver(Name = "chickens.StartupReceiver", Label = "Chickens startup receiver")]
	[IntentFilterAttribute(new[] { Intent.ActionBootCompleted })]
	public class StartupReceiver : BroadcastReceiver
	{
		private static string TAG = "StartupReceiver";

		public override void OnReceive(Context context, Intent intent)
		{
			if (intent.Action == Intent.ActionBootCompleted)
			{
				StartService(context);
			}
		}

		public static void StartService(Context context)
		{
			Log.Debug(TAG, "Starting the chicken sunshine service...");
			var intent = new Intent(context, typeof(SunService));
			context.StartService(intent);
		}
	}
}

