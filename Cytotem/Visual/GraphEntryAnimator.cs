using System;
using System.Linq;
using System.Collections.Generic;
using Android.Support.V7.Widget;
using Android.Support.V4.View;
using Run = Java.Lang.Runnable;

namespace Cytotem
{
	public class GraphEntryAnimator : RecyclerView.ItemAnimator
	{
		List<RecyclerView.ViewHolder> pendingAnimations = new List<RecyclerView.ViewHolder> ();
		int ongoingAnimations;

		public override bool AnimateAdd (RecyclerView.ViewHolder holder)
		{
			holder.ItemView.Visibility = Android.Views.ViewStates.Invisible;
			pendingAnimations.Add (holder);
			return true;
		}

		public override void RunPendingAnimations ()
		{
			var interpolator = new Android.Views.Animations.DecelerateInterpolator ();
			foreach (var holder in pendingAnimations) {
				var view = holder.ItemView;
				view.Visibility = Android.Views.ViewStates.Visible;
				ViewCompat.SetTranslationY (view, view.Height - view.PaddingTop);
				ViewCompat.Animate (view)
					.TranslationY (0)
					.SetStartDelay (holder.LayoutPosition * 50 + 10)
					.SetDuration (300)
					.SetInterpolator (interpolator)
					.WithStartAction (new Run (() => { ongoingAnimations++; DispatchAddStarting (holder); }))
					.WithEndAction (new Run (() => { ongoingAnimations--; DispatchAddFinished (holder); DispatchEndAnimations (); }))
					.Start ();
			}
			pendingAnimations.Clear ();
		}

		void DispatchEndAnimations ()
		{
			if (!IsRunning)
				DispatchAnimationsFinished ();
		}

		public override bool IsRunning {
			get {
				return pendingAnimations.Any () || ongoingAnimations > 0;
			}
		}

		public override bool AnimateChange (RecyclerView.ViewHolder oldHolder, RecyclerView.ViewHolder newHolder, int fromX, int fromY, int toX, int toY)
		{
			DispatchChangeFinished (oldHolder, true);
			DispatchChangeFinished (newHolder, false);
			return false;
		}

		public override bool AnimateMove (RecyclerView.ViewHolder holder, int fromX, int fromY, int toX, int toY)
		{
			DispatchMoveFinished (holder);
			return false;
		}

		public override bool AnimateRemove (RecyclerView.ViewHolder holder)
		{
			DispatchRemoveFinished (holder);
			return false;
		}

		public override void EndAnimation (RecyclerView.ViewHolder item)
		{
		}

		public override void EndAnimations ()
		{
		}
	}
}

