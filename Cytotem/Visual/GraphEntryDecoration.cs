using System;
using Android.Graphics;
using Android.Support.V7.Widget;

namespace Cytotem
{
	class GraphEntryDecoration : RecyclerView.ItemDecoration
	{
		Paint lightPaint, darkPaint;

		public GraphEntryDecoration ()
		{
			lightPaint = new Paint {
				Color = Color.Argb (0x38, 0xff, 0xff, 0xff),
				StrokeWidth = 1
			};
			lightPaint.SetStyle (Paint.Style.Stroke);
			darkPaint = new Paint {
				Color = Color.Argb (0x20, 0, 0, 0),
				StrokeWidth = 1
			};
			darkPaint.SetStyle (Paint.Style.Stroke);
		}

		public override void OnDrawOver (Canvas cValue, RecyclerView parent, RecyclerView.State state)
		{
			var childCount = parent.ChildCount;
			for (int i = 0; i < childCount; i++) {
				var child = parent.GetChildAt (i);
				if (child.Visibility != Android.Views.ViewStates.Visible)
					continue;

				var left = child.Left;
				var right = child.Right;
				var top = child.Top + child.PaddingTop + child.TranslationY;
				var bottom = child.Bottom;

				cValue.DrawLine (left + .5f, top, left + .5f, bottom, lightPaint);
				cValue.DrawLine (right - .5f, top, right - .5f, bottom, darkPaint);
			}
		}
	}

}

