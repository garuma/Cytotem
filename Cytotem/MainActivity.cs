using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Support.V4.View;
using Android.Support.V4.App;
using Android.Util;

using Android.Gms.Common.Apis;
using Android.Gms.Location;

namespace Cytotem
{
	[Activity (Label = "Cytotem", MainLauncher = true, Icon = "@drawable/icon", Theme = "@style/Theme.Cytotem")]
	public class MainActivity : AppCompatActivity, IGoogleApiClientConnectionCallbacks, IGoogleApiClientOnConnectionFailedListener
	{
		IGoogleApiClient client;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetContentView (Resource.Layout.DayOverviewLayout);

			var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar> (Resource.Id.toolbar);
			SetSupportActionBar (toolbar);
			var recyclerView = FindViewById<RecyclerView> (Resource.Id.dayListView);

			var db = GeofenceDatabase.From (this);
			var adapter = new EcoPublicAdapter (db);
			adapter.FillUp ();
			recyclerView.SetAdapter (adapter);

			SetupGeofencing ();
		}

		void SetupGeofencing ()
		{
			client = new GoogleApiClientBuilder (this, this, this)
				.AddApi (LocationServices.API)
				.Build ();
			client.Connect ();
		}

		public void OnConnected (Bundle connectionHint)
		{
			var geofence = new GeofenceBuilder ()
				.SetCircularRegion (42.3633025622483, -71.0857580911102, 100)
				.SetExpirationDuration (Geofence.NeverExpire)
				.SetRequestId ("org.neteril.Cytotem")
				.SetTransitionTypes (Geofence.GeofenceTransitionEnter)
				.SetNotificationResponsiveness ((int)TimeSpan.FromMinutes (10).TotalMilliseconds)
				.Build ();
			var request = new GeofencingRequest.Builder ()
				.AddGeofence (geofence)
				.SetInitialTrigger (GeofencingRequest.InitialTriggerEnter)
				.Build ();
			var intent = new Intent (this, typeof(GeofenceService));
			var pendingIntent = PendingIntent.GetService (this, 0, intent, PendingIntentFlags.UpdateCurrent);

			LocationServices.GeofencingApi.AddGeofences (client, request, pendingIntent);
			Android.Util.Log.Info ("GeofenceApi", "Successfully added geofence");
		}

		public void OnConnectionSuspended (int cause)
		{
		}

		public void OnConnectionFailed (Android.Gms.Common.ConnectionResult result)
		{
		}
	}
}


