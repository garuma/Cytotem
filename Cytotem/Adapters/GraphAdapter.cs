using System;
using System.Linq;
using System.Collections.Generic;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Android.Util;

namespace Cytotem
{
	class GraphAdapter : RecyclerView.Adapter
	{
		List<EcoPublicApi.CounterEntry> entries;
		int highestCount;
		EcoPublicApi api;
		GeofenceDatabase db;

		RecyclerView recyclerView;
		int lastChkPosition = -1;

		public event Action<int, EcoPublicApi.CounterEntry> CounterSelected;

		public GraphAdapter (GeofenceDatabase db)
		{
			this.db = db;
			entries = new List<EcoPublicApi.CounterEntry> ();
			api = new EcoPublicApi ();
			HasStableIds = true;
		}

		public int GetAggregateCountUntil (EcoPublicApi.CounterEntry entry)
		{
			return entries.Where (ce => ce.Date <= entry.Date).Sum (ce => ce.Count);
		}

		public override void OnAttachedToRecyclerView (RecyclerView recyclerView)
		{
			this.recyclerView = recyclerView;
		}

		public override void OnDetachedFromRecyclerView (RecyclerView p0)
		{
			this.recyclerView = null;
		}

		public async void FillUp (string station, DateTime date)
		{
			try {
				var hadEntries = entries.Any ();
				entries.Clear ();
				var result = await api.Retry (() => api.GetStationCounts (station,
				                                                          date,
				                                                          EcoPublicApi.DataInterval.Quarter), 4);
				entries.AddRange (result);
				highestCount = entries.Max (ce => ce.Count);
				if (hadEntries)
					NotifyItemRangeChanged (0, entries.Count);
				else
					NotifyItemRangeInserted (0, entries.Count);
			} catch (Exception e) {
				Android.Util.Log.Error ("EcoPublicApi", e.ToString ());
			}
		}

		public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position)
		{
			var entry = entries [position];
			var graphBar = (GraphEntryHolder)holder;
			graphBar.CounterEntry = entry;
			graphBar.CounterText.Text = entry.Count.ToString ();
			graphBar.MeIcon.Visibility = ViewStates.Invisible;
			UpdateMeIcon (graphBar);
			graphBar.ChartBar.Text = entry.Date.ToString (entry.Date.Minute == 0 ? "hhtt" : ":mm");
			graphBar.ChartBar.Checked = lastChkPosition == position;

			var oldPadding = graphBar.ItemView.PaddingTop;
			var height = recyclerView != null ? recyclerView.Height : graphBar.ItemView.Height;
			var topPadding = (int)Math.Round (height * (1 - (entry.Count / ((float)highestCount))));
			topPadding = Math.Min (topPadding,
			                       height - (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 72, graphBar.ItemView.Resources.DisplayMetrics));
			graphBar.ItemView.SetPadding (0, topPadding, 0, 0);
		}

		async void UpdateMeIcon (GraphEntryHolder holder)
		{
			var ce = holder.CounterEntry;
			try {
				if ((await db.WasPresentAt (ce.Date, TimeSpan.FromMinutes (15))) && holder.CounterEntry.Date == ce.Date)
					holder.MeIcon.Visibility = ViewStates.Visible;
			} catch (Exception e) {
				Android.Util.Log.Error ("GeofenceDb", e.ToString ());
			}
		}

		public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType)
		{
			var inflater = LayoutInflater.From (parent.Context);
			var holder = new GraphEntryHolder (inflater.Inflate (Resource.Layout.ChartBarLayout,
			                                                     parent,
			                                                     false));
			holder.ChartBar.Click += (sender, e) => HandleChartBarClicked (holder.ChartBar,
			                                                               holder.CounterEntry,
			                                                               holder.AdapterPosition);

			return holder;
		}

		void HandleChartBarClicked (CheckedTextView chkTxt, EcoPublicApi.CounterEntry ce, int position)
		{
			if (lastChkPosition != -1) {
				var otherHolder = (GraphEntryHolder)recyclerView.FindViewHolderForAdapterPosition (lastChkPosition);
				if (otherHolder != null)
					otherHolder.ChartBar.Checked = false;
			}
			chkTxt.Checked = true;
			lastChkPosition = position;
			if (CounterSelected != null)
				CounterSelected (position, ce);
		}

		public override int ItemCount {
			get {
				return entries.Count;
			}
		}

		public override long GetItemId (int position)
		{
			return entries [position].Date.GetHashCode ();
		}

		public class GraphEntryHolder : RecyclerView.ViewHolder
		{
			public GraphEntryHolder (View baseView) : base (baseView)
			{
				MeIcon = baseView.FindViewById<ImageView> (Resource.Id.meIcon);
				ChartBar = baseView.FindViewById<CheckedTextView> (Resource.Id.chartBar);
				CounterText = baseView.FindViewById<TextView> (Resource.Id.counterText);
			}

			public EcoPublicApi.CounterEntry CounterEntry {
				get;
				set;
			}

			public TextView CounterText {
				get;
				private set;
			}

			public ImageView MeIcon {
				get;
				private set;
			}

			public CheckedTextView ChartBar {
				get;
				private set;
			}
		}
	}

}

