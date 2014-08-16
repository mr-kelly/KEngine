//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// This script makes it possible for a scroll view to wrap its content, creating endless scroll views.
/// Usage: simply attach this script underneath your scroll view where you would normally place a UIGrid:
/// 
/// + Scroll View
/// |- UIWrappedContent
/// |-- Item 1
/// |-- Item 2
/// |-- Item 3
/// </summary>

[AddComponentMenu("NGUI/Interaction/Wrap Content")]
public class UIWrapContent : MonoBehaviour
{
	/// <summary>
	/// Width or height of the child items for positioning purposes.
	/// </summary>

	public int itemSize = 100;

	/// <summary>
	/// Whether the content will be automatically culled. Enabling this will improve performance in scroll views that contain a lot of items.
	/// </summary>

	public bool cullContent = true;

	Transform mTrans;
	UIPanel mPanel;
	UIScrollView mScroll;
	bool mHorizontal = false;
	BetterList<Transform> mChildren = new BetterList<Transform>();

	/// <summary>
	/// Initialize everything and register a callback with the UIPanel to be notified when the clipping region moves.
	/// </summary>

	protected virtual void Start ()
	{
		SortBasedOnScrollMovement();
		WrapContent();

		if (mScroll != null)
		{
			mScroll.GetComponent<UIPanel>().onClipMove = OnMove;
			mScroll.restrictWithinPanel = false;
			if (mScroll.dragEffect == UIScrollView.DragEffect.MomentumAndSpring)
				mScroll.dragEffect = UIScrollView.DragEffect.Momentum;
		}
	}

	/// <summary>
	/// Callback triggered by the UIPanel when its clipping region moves (for example when it's being scrolled).
	/// </summary>

	protected virtual void OnMove (UIPanel panel) { WrapContent(); }

	/// <summary>
	/// Immediately reposition all children.
	/// </summary>

	[ContextMenu("Sort Based on Scroll Movement")]
	public void SortBasedOnScrollMovement ()
	{
		if (!CacheScrollView()) return;

		// Cache all children and place them in order
		mChildren.Clear();
		for (int i = 0; i < mTrans.childCount; ++i)
			mChildren.Add(mTrans.GetChild(i));

		// Sort the list of children so that they are in order
		if (mHorizontal) mChildren.Sort(UIGrid.SortHorizontal);
		else mChildren.Sort(UIGrid.SortVertical);
		ResetChildPositions();
	}

	/// <summary>
	/// Immediately reposition all children, sorting them alphabetically.
	/// </summary>

	[ContextMenu("Sort Alphabetically")]
	public void SortAlphabetically ()
	{
		if (!CacheScrollView()) return;

		// Cache all children and place them in order
		mChildren.Clear();
		for (int i = 0; i < mTrans.childCount; ++i)
			mChildren.Add(mTrans.GetChild(i));

		// Sort the list of children so that they are in order
		mChildren.Sort(UIGrid.SortByName);
		ResetChildPositions();
	}

	/// <summary>
	/// Cache the scroll view and return 'false' if the scroll view is not found.
	/// </summary>

	protected bool CacheScrollView ()
	{
		mTrans = transform;
		mPanel = NGUITools.FindInParents<UIPanel>(gameObject);
		mScroll = mPanel.GetComponent<UIScrollView>();
		if (mScroll == null) return false;
		if (mScroll.movement == UIScrollView.Movement.Horizontal) mHorizontal = true;
		else if (mScroll.movement == UIScrollView.Movement.Vertical) mHorizontal = false;
		else return false;
		return true;
	}

	/// <summary>
	/// Helper function that resets the position of all the children.
	/// </summary>

	void ResetChildPositions ()
	{
		for (int i = 0; i < mChildren.size; ++i)
		{
			Transform t = mChildren[i];
			t.localPosition = mHorizontal ? new Vector3(i * itemSize, 0f, 0f) : new Vector3(0f, -i * itemSize, 0f);
		}
	}

	/// <summary>
	/// Wrap all content, repositioning all children as needed.
	/// </summary>

	public void WrapContent ()
	{
		float extents = itemSize * mChildren.size * 0.5f;
		Vector3[] corners = mPanel.worldCorners;
		
		for (int i = 0; i < 4; ++i)
		{
			Vector3 v = corners[i];
			v = mTrans.InverseTransformPoint(v);
			corners[i] = v;
		}
		Vector3 center = Vector3.Lerp(corners[0], corners[2], 0.5f);

		if (mHorizontal)
		{
			float min = corners[0].x - itemSize;
			float max = corners[2].x + itemSize;

			for (int i = 0; i < mChildren.size; ++i)
			{
				Transform t = mChildren[i];
				float distance = t.localPosition.x - center.x;

				if (distance < -extents)
				{
					t.localPosition += new Vector3(extents * 2f, 0f, 0f);
					distance = t.localPosition.x - center.x;
					UpdateItem(t, i);
				}
				else if (distance > extents)
				{
					t.localPosition -= new Vector3(extents * 2f, 0f, 0f);
					distance = t.localPosition.x - center.x;
					UpdateItem(t, i);
				}

				if (cullContent)
				{
					distance += mPanel.clipOffset.x - mTrans.localPosition.x;
					if (!UICamera.IsPressed(t.gameObject))
						NGUITools.SetActive(t.gameObject, (distance > min && distance < max), false);
				}
			}
		}
		else
		{
			float min = corners[0].y - itemSize;
			float max = corners[2].y + itemSize;

			for (int i = 0; i < mChildren.size; ++i)
			{
				Transform t = mChildren[i];
				float distance = t.localPosition.y - center.y;

				if (distance < -extents)
				{
					t.localPosition += new Vector3(0f, extents * 2f, 0f);
					distance = t.localPosition.y - center.y;
					UpdateItem(t, i);
				}
				else if (distance > extents)
				{
					t.localPosition -= new Vector3(0f, extents * 2f, 0f);
					distance = t.localPosition.y - center.y;
					UpdateItem(t, i);
				}

				if (cullContent)
				{
					distance += mPanel.clipOffset.y - mTrans.localPosition.y;
					if (!UICamera.IsPressed(t.gameObject))
						NGUITools.SetActive(t.gameObject, (distance > min && distance < max), false);
				}
			}
		}
	}

	/// <summary>
	/// Want to update the content of items as they are scrolled? Override this function.
	/// </summary>

	protected virtual void UpdateItem (Transform item, int index) {}
}
