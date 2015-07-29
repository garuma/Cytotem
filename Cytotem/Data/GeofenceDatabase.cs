using System;
using SQLite;
using Android.Content;
using System.Threading.Tasks;

namespace Cytotem
{
	public class GeofenceOccurence
	{
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }

		[Indexed, Column("occurence")]
		public DateTime Occurence { get; set; }
	}

	public class GeofenceDatabase
	{
		string dbPath;

		private GeofenceDatabase (string dbPath)
		{
			this.dbPath = dbPath;
		}

		public static GeofenceDatabase From (Context ctx)
		{
			var db = ctx.OpenOrCreateDatabase ("occurences.db", Android.Content.FileCreationMode.WorldReadable, null);
			var path = db.Path;
			db.Close ();
			return new GeofenceDatabase (path);
		}

		public async Task ClearDatabase ()
		{
			using (var connection = new SQLiteConnection (dbPath, true))
				await Task.Run (() => connection.DropTable<GeofenceOccurence> ()).ConfigureAwait (false);
		}

		async Task<SQLiteConnection> GrabConnection ()
		{
			var connection = new SQLiteConnection (dbPath, true);
			await Task.Run (() => connection.CreateTable<GeofenceOccurence> ()).ConfigureAwait (false);
			return connection;
		}

		public async Task AddOccurence (DateTime occurence)
		{
			// Make sure we only store UTC values
			occurence = occurence.ToUniversalTime ();

			using (var connection = await GrabConnection ().ConfigureAwait (false))
				await Task.Run (() => connection.Insert (new GeofenceOccurence { Occurence = occurence })).ConfigureAwait (false);
		}

		public async Task<bool> WasPresentAt (DateTime start, TimeSpan period)
		{
			start = start.ToUniversalTime ();
			var end = start + period;
			using (var connection = await GrabConnection ().ConfigureAwait (false)) {
				return await Task.Run (() => connection.ExecuteScalar<int> (
					"select count(*) from GeofenceOccurence where `occurence` >= ?1 and `occurence` <= ?2 limit 1", start, end
				) > 0).ConfigureAwait (false);
			}
		}
	}
}

