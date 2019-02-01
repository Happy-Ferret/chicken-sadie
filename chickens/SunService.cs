using System;
using Android.App;
using Android.OS;
using Android.Content;
using Android.Util;
using Java.Util;
using System.Threading.Tasks;
using Android.Support.V7.App;
using Android.Graphics;
using Android.Media;

namespace chickens
{
	[Service(Exported = true)]
	public class SunService : Service, ISunService
	{
		public static string ACTION_SQUARK = "chickens.actions.Squark";

		private string TAG = "SunService";
		IBinder binder;

		private DateTime? upcomingTimesFetched;

		private ChickenTimes? upcomingTimesToday;
		public ChickenTimes? UpcomingTimesToday
		{
			get { return upcomingTimesToday; }
			private set
			{
				upcomingTimesToday = value;
				if (OnUpcomingTimesChanged != null)
				{
					OnUpcomingTimesChanged(upcomingTimesToday, upcomingTimesTomorrow);
				}
			}
		}

		private ChickenTimes? upcomingTimesTomorrow;
		public ChickenTimes? UpcomingTimesTomorrow
		{
			get { return upcomingTimesTomorrow; }
			private set
			{
				upcomingTimesTomorrow = value;
				if (OnUpcomingTimesChanged != null)
				{
					OnUpcomingTimesChanged(upcomingTimesToday, upcomingTimesTomorrow);
				}
			}
		}

		public SunService()
		{
		}

		public override void OnCreate()
		{
			base.OnCreate();
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
		}

		public event Action<DateTime?> OnNextAlarmChanged;
		public event Action<ChickenTimes?, ChickenTimes?> OnUpcomingTimesChanged;

		public static Java.Util.Date DateTimeToNativeDate(DateTime date)
		{
			long dateTimeUtcAsMilliseconds =
				(long)date
					.ToUniversalTime()
					.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
					.TotalMilliseconds;

			return new Date(dateTimeUtcAsMilliseconds);
		}

		public void SetNextAlarm(DateTime next)
		{
			var mgr = GetSystemService(Context.AlarmService) as AlarmManager;

			var squark = new Intent(this, typeof(SunService));
			squark.SetAction(ACTION_SQUARK);

			var pendingIntent = PendingIntent.GetService(this, 7734, squark, PendingIntentFlags.UpdateCurrent);

			var span = next.Subtract(DateTime.Now);
			mgr.Set(AlarmType.ElapsedRealtime, SystemClock.ElapsedRealtime() + (long)span.TotalMilliseconds, pendingIntent);

			RecordNextAlarmTime(next); // it's set now
		}

		public DateTime? NextAlarmTime
		{
			get { return ConfirmNextAlarmTime(); }
		}

		private DateTime? ConfirmNextAlarmTime()
		{
			var sharedPreferences = GetSharedPreferences("SunService", FileCreationMode.Private);
			if (sharedPreferences.All.ContainsKey("AlarmTime"))
			{
				var ticks = sharedPreferences.GetLong("AlarmTime", -1L);
				if (ticks != -1)
				{
					return new DateTime(ticks);
				}
				else
				{
					return null;
				}
			}
			else
			{
				return null;
			}
		}

		public async Task<bool> RefreshTimesAsync()
		{
			var timesToday = await RetrieveNextTimesAsync(false);
			var timesTomorrow = await RetrieveNextTimesAsync(true);

			if (timesToday != null && timesTomorrow != null)
			{
				upcomingTimesFetched = DateTime.Now;
				UpcomingTimesToday = timesToday;
				UpcomingTimesTomorrow = timesTomorrow;
				return true;
			}
			else
			{
				Log.Debug(TAG, "Could not update chicken times.");
				return false;
			}
		}

		private async Task<ChickenTimes?> RetrieveNextTimesAsync(bool tomorrow)
		{
			try
			{
				var dateStr = tomorrow ? DateTime.Now.AddDays(1).ToString("yyyy-MM-dd") : DateTime.Now.ToString("yyyy-MM-dd");

				var url =
					string.Format("http://api.sunrise-sunset.org/json?lat={0}&lng={1}&date={2}",
								  Constants.location_latitude,
								  Constants.Location_longitude,
								  dateStr);

				var jsonObject = await InternetHelper.GetJsonAsync(url);

				if (jsonObject != null)
				{
					Log.Debug(TAG, "Json: " + jsonObject.ToString());
					return InternetHelper.JsonToChickenTimes(jsonObject);
				}
				else
				{
					Log.Debug(TAG, "No jsonObject retrieved.");
					return null;
				}

			} catch (Exception e) {
				Log.Error(TAG, e.ToString());
				return null;
			}
		}

		public DateTime? GetNextAlarmRecommendation()
		{
			if (upcomingTimesFetched == null || upcomingTimesFetched.Value.Date != DateTime.Now.Date)
			{
				return null;
			}

			if (UpcomingTimesToday != null &&
				DateTime.Now.TimeOfDay < UpcomingTimesToday.Value.civil_twilight_end.TimeOfDay)
			{
				return DateTime.Today
					           .Add(UpcomingTimesToday.Value.civil_twilight_end.TimeOfDay)
					           .AddMinutes(Constants.AlarmOffsetMins);
			}

			if (UpcomingTimesTomorrow != null)
			{
				return DateTime.Today
					           .AddDays(1)
					           .Add(UpcomingTimesTomorrow.Value.civil_twilight_end.TimeOfDay)
					           .AddMinutes(Constants.AlarmOffsetMins); ;
			}

			return null;
		}

		private void RecordNextAlarmTime(DateTime time)
		{
			var sharedPreferences = GetSharedPreferences("SunService", FileCreationMode.Private);
			var editor = sharedPreferences.Edit();
			editor.PutLong("AlarmTime", time.Ticks);
			editor.Commit();

			if (OnNextAlarmChanged != null) { OnNextAlarmChanged(time); }
		}

		public void AlarmObviouslyPassed()
		{
			ClearNextAlarmTime();
		}

		private void ClearNextAlarmTime()
		{
			var sharedPreferences = GetSharedPreferences("SunService", FileCreationMode.Private);
			var editor = sharedPreferences.Edit();
			editor.Remove("AlarmTime");
			editor.Commit();

			if (OnNextAlarmChanged != null) { OnNextAlarmChanged(null); }
		}

		public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
		{
			Log.Debug(TAG, "OnStartCommand");

			// intent = null if the service was restarted
			if (intent != null)
			{
				Log.Debug(TAG, "Received action: " + intent.Action);

				// could be the chicken alarm!
				if (intent.Action == ACTION_SQUARK)
				{
					Log.Info(TAG, "CHICKENS!!!");

					var pending = PendingIntent.GetActivity(ApplicationContext, 0, new Intent(ApplicationContext, typeof(MainActivity)), 0);
					//var alarmSound = RingtoneManager.GetDefaultUri(RingtoneType.Notification);

					var alarmSound = Android.Net.Uri.Parse(
						"android.resource://" 
					    + PackageName + "/" 
						+ Resource.Raw.chicken_rt);

					var notification = 
						new NotificationCompat.Builder(this)
	  					  .SetContentIntent(pending)
	                      .SetLights(Color.Argb(255,255,255,255), 500, 200)
	                      .SetTicker("Chicken time!")
	                      .SetContentText("It's time to put the chickens to bed.")
	                      .SetContentTitle("Zzzzzz....")
	                      .SetSound(alarmSound)
	                      .SetVibrate(new[] { 500L, 500L, 500L, 500L, 500L, 500L })
	                      .SetVisibility(NotificationCompat.VisibilityPublic)
	                      .SetSmallIcon(Resource.Mipmap.Icon)
	                      .SetDefaults(0)
	                      .Build();

					NotificationManager manager = GetSystemService(Context.NotificationService) as NotificationManager;
					manager.Notify(7734, notification);

					ClearNextAlarmTime(); // wipe this one out
					UpcomingTimesToday = null;
					UpcomingTimesTomorrow = null;
				}
			}

			return StartCommandResult.Sticky;
		}

		public override Android.OS.IBinder OnBind(Android.Content.Intent intent)
		{
			binder = new ServiceBinder<SunService>(this);
			return binder;
		}
	}

	public class ServiceBinder<S> : Binder
	{
		S service;

		public ServiceBinder(S service)
		{
			this.service = service;
		}

		public S GetService()
		{
			return service;
		}
	}

	public class ServiceConnection<S, I> : Java.Lang.Object, IServiceConnection where S : Service where I : class
	{
		BindingActivity<S, I> activity;

		public ServiceConnection(BindingActivity<S,I> activity)
		{
			this.activity = activity;
		}

		public void OnServiceConnected(ComponentName name, IBinder service)
		{
			var sunServiceBinder = service as ServiceBinder<S>;
			if (sunServiceBinder != null)
			{
				activity.Binder = sunServiceBinder;
				activity.IsBound = true;
			}
		}

		public void OnServiceDisconnected(ComponentName name)
		{
			activity.IsBound = false;
		}
	}
}

