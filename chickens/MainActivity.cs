using System;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content;
using Android.Util;
using System.Collections.Generic;
using Android.Content.PM;

namespace chickens
{
	[Activity(Label = "chicken sadie", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, Icon = "@mipmap/icon")]
	public class MainActivity : BindingActivity<SunService, ISunService>
	{
		TextView nextAlarmText;
		TextView nextTwilightText;
		TextView alarmRecommendationText;
		Button testButton;
		Button updateButton;
		Button setRecommendedAlarmButton;

		DateTime? nextAlarmRecommendation;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);

			// retrieve items from view
			nextAlarmText = FindViewById<TextView>(Resource.Id.nextAlarmText);
			testButton = FindViewById<Button>(Resource.Id.testButton);
			updateButton = FindViewById<Button>(Resource.Id.updateTimesButton);
			nextTwilightText = FindViewById<TextView>(Resource.Id.nextTwilightText);
			alarmRecommendationText = FindViewById<TextView>(Resource.Id.nextAlarmRecommendationText);
			setRecommendedAlarmButton = FindViewById<Button>(Resource.Id.setRecommendedAlarmButton);

			testButton.Click += delegate 
			{
				var soon = DateTime.Now.AddSeconds(5);
				Service.SetNextAlarm(soon);
			};

			updateButton.Click += async delegate
			{
				var ok = await Service.RefreshTimesAsync();
				if (!ok)
				{
					var dialog = new AlertDialog.Builder(this)
												.SetTitle("Error")
												.SetMessage("Unable to refresh times from internet service.")
					                            .Create();
					RunOnUiThread(() =>
					{
						dialog.Show();
					});
				}

			};

			setRecommendedAlarmButton.Click += delegate
			{
				string complaint = null;
				if (nextAlarmRecommendation != null)
				{
					if (Service.NextAlarmTime == null)
					{
						if (nextAlarmRecommendation > DateTime.Now)
						{
							Service.SetNextAlarm(nextAlarmRecommendation.Value);
						}
						else
						{
							complaint = "The recommended time is in the past. Please update times again.";
						}
					}
					else
					{
						complaint = "An alarm is already set.";
					}
				}
				else
				{
					complaint = "Could not determine recommended time. Please update times.";

				}

				if (complaint != null)
				{
					var dialog = new AlertDialog.Builder(this)
												.SetTitle("Error")
												.SetMessage(complaint)
												.Create();
					dialog.Show();
				}
			};

			// ensure service started
			StartupReceiver.StartService(ApplicationContext);
		}

		protected override void OnServiceConnected()
		{
			Log.Debug(TAG, "Service connected.");
			AlarmChange(Service.NextAlarmTime);
			Service.OnNextAlarmChanged += AlarmChange;
			Service.OnUpcomingTimesChanged += UpcomingChange;

			if (DateTime.Now > Service.NextAlarmTime)
			{
				Service.AlarmObviouslyPassed();
			}

			Service.RefreshTimesAsync();

			ShowButtons(true);
		}

		protected override void OnServiceDisconnecting()
		{
			Log.Debug(TAG, "Service disconnected.");
			Service.OnNextAlarmChanged -= AlarmChange;
			Service.OnUpcomingTimesChanged -= UpcomingChange;
			nextAlarmText.SetText("Disconnected.", TextView.BufferType.Normal);
			ShowButtons(false);
		}

		public void UpcomingChange(ChickenTimes? timesToday, ChickenTimes? timesTomorrow)
		{
			var items = new List<string>();

			if (timesToday != null)
			{
				items.Add("Darkness today: " + timesToday.Value.civil_twilight_end.ToShortTimeString());
			}
			if (timesTomorrow != null)
			{
				items.Add("Darkness tomorrow: " + timesTomorrow.Value.civil_twilight_end.ToShortTimeString());
			}

			if (items.Count > 0)
			{
				nextTwilightText.SetText(string.Join("\n", items), TextView.BufferType.Normal);
			}
			else
			{
				nextTwilightText.SetText(GetString(Resource.String.nextTwilightPending), TextView.BufferType.Normal);
			}

			nextAlarmRecommendation = Service.GetNextAlarmRecommendation();
			alarmRecommendationText.SetText(
				string.Format("Next alarm recommendation: {0}",
					nextAlarmRecommendation != null
							  ? nextAlarmRecommendation.Value.ToShortTimeString()
							  : "not yet available"),
				TextView.BufferType.Normal);

			UpdateSetAlarmButtonEnabled();
		}

		public void AlarmChange(DateTime? newAlarm)
		{
			if (newAlarm != null)
			{
				nextAlarmText.SetText("New alarm time: " + newAlarm.Value.ToShortTimeString(), TextView.BufferType.Normal);
				EnableButtons(false);
			}
			else
			{
				nextAlarmText.SetText("No alarm set.", TextView.BufferType.Normal);
				EnableButtons(true);
			}
		}

		private void ShowButtons(bool visible)
		{
			testButton.Visibility = visible ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Gone;
			updateButton.Visibility = visible ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Gone;
			setRecommendedAlarmButton.Visibility = visible ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Gone;
		}

		private void EnableButtons(bool enable)
		{
			testButton.Enabled = enable;
			updateButton.Enabled = enable;
			setRecommendedAlarmButton.Enabled = enable && (nextAlarmRecommendation != null);
		}

		private void UpdateSetAlarmButtonEnabled()
		{
			setRecommendedAlarmButton.Enabled = 
				(nextAlarmRecommendation != null && Service.NextAlarmTime == null);		
		}
	}
}


