using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Android.Content;

namespace Cytotem
{
	class EcoPublicAdapter : RecyclerView.Adapter
	{
		List<EcoPublicApi.CounterEntry?> counters;
		EcoPublicApi api;
		WeatherApi weatherApi;
		GeofenceDatabase db;

		public EcoPublicAdapter (GeofenceDatabase db)
		{
			this.db = db;
			HasStableIds = true;
			counters = new List<EcoPublicApi.CounterEntry?> ();
			api = new EcoPublicApi ();
			weatherApi = new WeatherApi () { UseDebugMode = true };
		}

		public async void FillUp ()
		{
			try {
				var start = DateTime.Now.Date;
				start = new DateTime (start.Year, start.Month, 1);
				var result = await api.Retry (() => api.GetStationCounts (EcoPublicApi.CambridgeStation,
				                                                          start,
				                                                          DateTime.Now.Date.AddDays (-1),
				                                                          EcoPublicApi.DataInterval.Day), 4);
				var oldCount = counters.Count;
				var calendar = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar;
				var groups = result
					.OrderByDescending (ce => ce.Date)
					.ToLookup (ce => calendar.GetWeekOfYear (ce.Date,
					                                         CalendarWeekRule.FirstDay,
					                                         DayOfWeek.Monday));
				foreach (var grp in groups) {
					counters.Add (null);
					foreach (var item in grp)
						counters.Add (item);
				}
				NotifyItemRangeInserted (oldCount, counters.Count - oldCount);
			} catch (Exception e) {
				Android.Util.Log.Error ("EcoPublicApi", e.ToString ());
			}
		}

		public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position)
		{
			if (holder.ItemViewType == 0) {
				var counter = counters [position].Value;
				var ecoHolder = (EcoPublicItemHolder)holder;

				ecoHolder.CounterEntry = counter;
				ecoHolder.CounterText.Text = counter.Count.ToString ();
				ecoHolder.DateText.Text = counter.Date.Date.ToString ("ddd dd");
				ecoHolder.MeIcon.Visibility = ViewStates.Invisible;
				ecoHolder.WeatherIcon.Visibility = ViewStates.Invisible;
				UpdateMeIcon (ecoHolder);
				UpdateWeatherIcon (ecoHolder);
			} else {
				var counter = counters [position + 1].Value;
				var calendar = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar;

				var ecoHeader = (EcoPublicHeaderHolder)holder;
				ecoHeader.Header.Text = "Week " + calendar.GetWeekOfYear (counter.Date,
				                                                          CalendarWeekRule.FirstDay,
				                                                          DayOfWeek.Monday);
			}
		}

		async void UpdateMeIcon (EcoPublicItemHolder holder)
		{
			var ce = holder.CounterEntry;
			try {
				if ((await db.WasPresentAt (ce.Date.Date, TimeSpan.FromHours (23))) && holder.CounterEntry.Date == ce.Date)
					holder.MeIcon.Visibility = ViewStates.Visible;
			} catch (Exception e) {
				Android.Util.Log.Error ("GeofenceDb", e.ToString ());
			}
		}

		async void UpdateWeatherIcon (EcoPublicItemHolder holder)
		{
			var ce = holder.CounterEntry;
			try {
				var condition = await weatherApi.GetWeatherConditionForDate (ce.Date.Date);
				if (holder.CounterEntry.Date == ce.Date) {
					var weatherIconId = Resource.Drawable.weather_sunny;
					switch (condition) {
					case WeatherCondition.Cloudy:
						weatherIconId = Resource.Drawable.weather_cloudy;
						break;
					case WeatherCondition.PartlyCloudy:
						weatherIconId = Resource.Drawable.weather_partlycloudy;
						break;
					case WeatherCondition.Rainy:
						weatherIconId = Resource.Drawable.weather_pouring;
						break;
					case WeatherCondition.Sunny:
						weatherIconId = Resource.Drawable.weather_sunny;
						break;
					}
					holder.WeatherIcon.SetImageResource (weatherIconId);
					holder.WeatherIcon.Visibility = ViewStates.Visible;
				}
			} catch (Exception e) {
				Android.Util.Log.Error ("WeatherApi", e.ToString ());
			}
		}

		public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType)
		{
			var inflater = LayoutInflater.From (parent.Context);

			if (viewType == 0) {
				var dayView = inflater.Inflate (Resource.Layout.DayOverviewEntryLayout,
				                                parent,
				                                false);
				return new EcoPublicItemHolder (dayView);
			} else {
				return new EcoPublicHeaderHolder (inflater.Inflate (Resource.Layout.DayOverviewHeader,
				                                                    parent,
				                                                    false));
			}
		}

		public override int ItemCount {
			get {
				return counters.Count;
			}
		}

		public override long GetItemId (int position)
		{
			if (counters [position] == null)
				return 0xfff0000 | counters[position + 1].Value.Date.DayOfYear;
			return counters[position].Value.Date.GetHashCode ();
		}

		public override int GetItemViewType (int position)
		{
			return counters [position] != null ? 0 : 1;
		}

		class EcoPublicItemHolder : RecyclerView.ViewHolder
		{
			public EcoPublicItemHolder (View baseView) : base (baseView)
			{
				baseView.Click += DayView_Click;
				DateText = baseView.FindViewById<TextView> (Resource.Id.dateText);
				MeIcon = baseView.FindViewById<ImageView> (Resource.Id.meIcon);
				WeatherIcon = baseView.FindViewById<ImageView> (Resource.Id.weatherIcon);
				CounterText = baseView.FindViewById<TextView> (Resource.Id.counterText);
			}

			public EcoPublicApi.CounterEntry CounterEntry {
				get;
				set;
			}

			public TextView DateText {
				get;
				private set;
			}

			public ImageView MeIcon {
				get;
				private set;
			}

			public ImageView WeatherIcon {
				get;
				private set;
			}

			public TextView CounterText {
				get;
				private set;
			}

			void DayView_Click (object sender, EventArgs e)
			{
				var context = ItemView.Context;
				var intent = new Intent (context, typeof(GraphActivity));
				intent.PutExtra ("dateTime", CounterEntry.Date.Date.Ticks);
				intent.PutExtra ("globalCount", CounterEntry.Count);

				context.StartActivity (intent);
			}
		}

		class EcoPublicHeaderHolder : RecyclerView.ViewHolder
		{
			public EcoPublicHeaderHolder (View baseView) : base (baseView)
			{
				Header = (TextView)baseView;
			}

			public TextView Header {
				get;
				private set;
			}
		}
	}
}

