using System;
using System.Linq;
using System.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Cytotem
{
	public class EcoPublicApi
	{
		public const string CambridgeStation = "100023038";
		public const string CambridgeStationIn = "101023038";
		public const string CambridgeStationOut = "102023038";

		HttpClient client = new HttpClient ();

		public static readonly string DataUrl = "http://www.eco-public.com/api/h7q239dd/data/periode/{0}/?begin={1}&end={2}&step={3}";

		Dictionary<string, List<CounterEntry>> cache = new Dictionary<string, List<CounterEntry>> ();
		
		public EcoPublicApi ()
		{
		}

		public async Task<TResult> Retry<TResult> (Func<Task<TResult>> method, int tryCount)
		{
			if (tryCount <= 0)
				throw new ArgumentOutOfRangeException ("tryCount");

			while (true) {
				try {
					return await method ().ConfigureAwait (false);
				} catch {
					if (--tryCount <= 0)
						throw;
				}
				await Task.Delay (500).ConfigureAwait (false);
			}
		}

		public Task<IEnumerable<CounterEntry>> GetStationCounts (string stationId,
		                                                         DateTime date,
		                                                         DataInterval interval)
		{
			return GetStationCounts (stationId, date, date, interval);
		}

		public async Task<IEnumerable<CounterEntry>> GetStationCounts (string stationId,
		                                                               DateTime beginDate,
		                                                               DateTime endDate,
		                                                               DataInterval interval)
		{
			var url = GetDataUrl (stationId, beginDate, endDate, interval);
			List<CounterEntry> entries;
			if (cache.TryGetValue (url, out entries))
				return entries.AsReadOnly ();
			var json = await client.GetStringAsync (url).ConfigureAwait (false);
			var array = JsonArray.Parse (json) as JsonArray;
			entries = array.Select (jv => new CounterEntry {
				Count = jv ["comptage"],
				Date = DateTime.Parse (jv ["date"])
			}).ToList ();
			cache [url] = entries;
			return entries.AsReadOnly ();
		}

		string GetDataUrl (string stationID, DateTime beginDate, DateTime endDate, DataInterval interval)
		{
			return string.Format (
				DataUrl,
				stationID,
				beginDate.ToString ("yyyyMMdd"),
				endDate.ToString ("yyyyMMdd"),
				((int)interval).ToString ()
			);
		}

		public enum DataInterval {
			Quarter = 2,
			Hour = 3,
			Day = 4
		}

		public struct CounterEntry
		{
			public DateTime Date { get; set; }
			public int Count { get; set; }
		}
	}
}

