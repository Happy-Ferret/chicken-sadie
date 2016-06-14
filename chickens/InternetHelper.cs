using System;
using Java.Util;
using Org.Json;
using Java.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Android.Util;

namespace chickens
{
	public class InternetHelper
	{
		public static string TAG = "InternetHelper";

		public static async Task<JSONObject> GetJsonAsync(String url)
		{
			try
			{
				var client = new HttpClient();
				var jsonString = await client.GetStringAsync(url);
				var jsonObject = new JSONObject(jsonString);
				return jsonObject;
			}
			catch (Exception e)
			{
				Log.Error(TAG, e.ToString());
				return null;
			}
		}

		public static ChickenTimes JsonToChickenTimes(JSONObject obj)
		{
			var results = obj.GetJSONObject("results");
			var chickenTimes = new ChickenTimes()
			{
				sunrise = DateTime.Parse(results.GetString("sunrise")),
				sunset = DateTime.Parse(results.GetString("sunset")),
				civil_twilight_begin = DateTime.Parse(results.GetString("civil_twilight_begin")),
				civil_twilight_end = DateTime.Parse(results.GetString("civil_twilight_end"))
			};
			return chickenTimes;
		}

	}
}

