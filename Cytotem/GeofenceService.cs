
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

using Android.Gms.Location;
using Android.Support.V4.App;

namespace Cytotem
{
	[Service]
	public class GeofenceService : IntentService
	{
		protected override void OnHandleIntent (Intent intent)
		{
			Android.Util.Log.Info ("CytotemGeofence", "Got geofence intent");

			var geofencingEvent = GeofencingEvent.FromIntent (intent);
			if (geofencingEvent.HasError) {
				var errorMessage = GeofenceStatusCodes.GetStatusCodeString (geofencingEvent.ErrorCode);
				Android.Util.Log.Error ("CytotemGeofence", errorMessage);
				return;
			}

			if (geofencingEvent.GeofenceTransition == Geofence.GeofenceTransitionEnter) {
				Android.Util.Log.Info ("CytotemGeofence", "Adding geofence occurence at " + DateTime.Now.ToString ());

				var db = GeofenceDatabase.From (this);
				AddOccurence (db);

				var activityIntent = new Intent (this, typeof (MainActivity));
				var notification = new NotificationCompat.Builder (this)
					.SetSmallIcon (Resource.Drawable.ic_notification)
					.SetContentTitle ("Broadway totem hit")
					.SetContentText ("You passed by the bike totem at " + DateTime.Now.ToShortTimeString ())
					.SetContentIntent (PendingIntent.GetActivity (this, 0, activityIntent, PendingIntentFlags.UpdateCurrent))
					.Build ();
				NotificationManagerCompat.From (this).Notify (0, notification);
			}
		}

		async void AddOccurence (GeofenceDatabase db)
		{
			try {
				await db.AddOccurence (DateTime.UtcNow);
			} catch (Exception e) {
					Android.Util.Log.Error ("CytotemGeofence", e.ToString ());
			}
		}
	}
}

