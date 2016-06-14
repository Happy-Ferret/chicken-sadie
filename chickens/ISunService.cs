using System;
using System.Threading.Tasks;

namespace chickens
{
	public interface ISunService
	{
		void SetNextAlarm(DateTime time);
		DateTime? NextAlarmTime { get; }
		void AlarmObviouslyPassed();

		Task<bool> RefreshTimesAsync();

		ChickenTimes? UpcomingTimesToday { get; }
		ChickenTimes? UpcomingTimesTomorrow { get; }

		DateTime? GetNextAlarmRecommendation();

		event Action<DateTime?> OnNextAlarmChanged;
		event Action<ChickenTimes?,ChickenTimes?> OnUpcomingTimesChanged;
	}
}

