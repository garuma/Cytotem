using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Json;

namespace Cytotem
{
	public enum WeatherCondition {
		Cloudy,
		PartlyCloudy,
		Rainy,
		Sunny
	}

	public class WeatherApi
	{
		const string Url = "https://api.forecast.io/forecast/{0}/{1},{2}?exclude=currently,minutely,hourly,alerts,flags";
		const string ApiKey = "...";
		const string Location = "42.3633025622483,-71.0857580911102";

		static readonly DateTime Epoch = new DateTime (1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		static Dictionary<int, WeatherCondition> cache = new Dictionary<int, WeatherCondition> ();

		HttpClient client = new HttpClient ();

		public bool UseDebugMode {
			get;
			set;
		}

		public async Task<WeatherCondition> GetWeatherConditionForDate (DateTime date)
		{
			if (UseDebugMode)
				return GetFakeWeatherConditionForDate (date);

			var seconds = (int)(date.AddHours (12) - Epoch).TotalSeconds;
			WeatherCondition condition = WeatherCondition.Sunny;
			if (cache.TryGetValue (seconds, out condition))
				return condition;

			var url = string.Format (Url, ApiKey, Location, seconds.ToString ());
			var json = await client.GetStringAsync (url).ConfigureAwait (false);
			var result = JsonObject.Parse (json);
			var icon = (string)(result ["daily"] ["data"] [0] ["icon"]);

			condition = GetConditionForIconName (icon);
			cache [seconds] = condition;

			return condition;
		}

		// Because the above API has rate-limiting and there is no way to do bulk fetch
		// of dates, we use this simplistic method here for debug purposes
		public WeatherCondition GetFakeWeatherConditionForDate (DateTime date)
		{
			return (WeatherCondition)(date.DayOfYear % 4);
		}

		WeatherCondition GetConditionForIconName (string icon)
		{
			switch (icon) {
			case "clear-day":
			case "clear-night":
				return WeatherCondition.Sunny;
			case "rain":
			case "snow":
			case "sleet":
				return WeatherCondition.Rainy;
			case "wind":
			case "fog":
			case "cloudy":
				return WeatherCondition.Cloudy;
			case "partly-cloudy-day":
			case "partly-cloudy-night":
				return WeatherCondition.PartlyCloudy;
			}

			return WeatherCondition.Sunny;
		}
	}
}

