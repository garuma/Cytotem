
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;

using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Support.V4.View;

namespace Cytotem
{
	[Activity (Label = "Cytotem", Theme = "@style/Theme.Cytotem")]
	public class GraphActivity : AppCompatActivity, ViewTreeObserver.IOnGlobalLayoutListener
	{
		DateTime date;
		int globalCount;

		RecyclerView graphView;
		GraphAdapter adapter;

		TextView time, count, increment;
		Spinner filterSpinner;
		ImageView meIcon;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetContentView (Resource.Layout.GraphActivityLayout);

			var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar> (Resource.Id.toolbar);
			SetSupportActionBar (toolbar);
			SupportActionBar.SetDisplayShowTitleEnabled (false);
			SupportActionBar.SetDisplayHomeAsUpEnabled (true);

			graphView = FindViewById<RecyclerView> (Resource.Id.chartView);
			var db = GeofenceDatabase.From (this);
			adapter = new GraphAdapter (db);

			date = DateTime.Now.AddDays (-1);
			var ticks = Intent.GetLongExtra ("dateTime", -1);
			if (ticks != -1)
				date = new DateTime (ticks);
			globalCount = Intent.GetIntExtra ("globalCount", 0);

			adapter.FillUp (EcoPublicApi.CambridgeStation, date);

			graphView.ViewTreeObserver.AddOnGlobalLayoutListener (this);
			graphView.AddItemDecoration (new GraphEntryDecoration ());
			graphView.SetItemAnimator (new GraphEntryAnimator ());

			time = FindViewById<TextView> (Resource.Id.timeText);
			count = FindViewById<TextView> (Resource.Id.countText);
			increment = FindViewById<TextView> (Resource.Id.incrementText);
			meIcon = FindViewById<ImageView> (Resource.Id.meIcon);

			increment.Visibility = ViewStates.Invisible;
			time.Text = date.ToString ("ddd, MMM dd");
			count.Text = globalCount.ToString ();

			adapter.CounterSelected += HandleCounterSelected;

			filterSpinner = FindViewById<Spinner> (Resource.Id.filterSpinner);
		}

		public override bool OnOptionsItemSelected (IMenuItem item)
		{
			if (item.ItemId == Android.Resource.Id.Home) {
				OnBackPressed ();
				return true;
			}
			return base.OnOptionsItemSelected (item);
		}

		void HandleFilterSpinnerItemSelected (object sender, AdapterView.ItemSelectedEventArgs e)
		{
			var api = EcoPublicApi.CambridgeStation;
			if (e.Position == 1)
				api = EcoPublicApi.CambridgeStationIn;
			if (e.Position == 2)
				api = EcoPublicApi.CambridgeStationOut;
			adapter.FillUp (api, date);
		}

		void HandleCounterSelected (int position, EcoPublicApi.CounterEntry entry)
		{
			time.Text = entry.Date.ToString ("t");
			count.Text = adapter.GetAggregateCountUntil (entry).ToString ();
			increment.Text = "↗ " + entry.Count.ToString ();
			increment.Visibility = ViewStates.Visible;
			var holder = (GraphAdapter.GraphEntryHolder)graphView.FindViewHolderForAdapterPosition (position);
			meIcon.Visibility = holder != null && holder.MeIcon.Visibility == ViewStates.Visible ?
				ViewStates.Visible : ViewStates.Invisible;
		}

		public void OnGlobalLayout ()
		{
			graphView.ViewTreeObserver.RemoveOnGlobalLayoutListener (this);
			graphView.SetAdapter (adapter);
			filterSpinner.ItemSelected += HandleFilterSpinnerItemSelected;
		}
	}
}

