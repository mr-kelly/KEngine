//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// UIDragDropItem is a base script for your own Drag & Drop operations.
/// </summary>

[AddComponentMenu("NGUI/Interaction/Drag and Drop Item")]
public class UIDragDropItem : MonoBehaviour
{
	public enum Restriction
	{
		None,
		Horizontal,
		Vertical,
		PressAndHold,
	}

	/// <summary>
	/// What kind of restriction is applied to the drag & drop logic before dragging is made possible.
	/// </summary>

	public Restriction restriction = Restriction.None;

	/// <summary>
	/// Whether a copy of the item will be dragged instead of the item itself.
	/// </summary>

	public bool cloneOnDrag = false;

	/// <summary>
	/// How long the user has to press on an item before the drag action activates.
	/// </summary>

	[HideInInspector]
	public float pressAndHoldDelay = 1f;

#region Common functionality

	protected Transform mTrans;
	protected Transform mParent;
	protected Collider mCollider;
	protected UIButton mButton;
	protected UIRoot mRoot;
	protected UIGrid mGrid;
	protected UITable mTable;
	protected int mTouchID = int.MinValue;
	protected float mPressTime = 0f;
	protected UIDragScrollView mDragScrollView = null;

	/// <summary>
	/// Cache the transform.
	/// </summary>

	protected virtual void Start ()
	{
		mTrans = transform;
		mCollider = collider;
		mButton = GetComponent<UIButton>();
		mDragScrollView = GetComponent<UIDragScrollView>();
	}

	/// <summary>
	/// Record the time the item was pressed on.
	/// </summary>

	void OnPress (bool isPressed) { if (isPressed) mPressTime = RealTime.time; }

	/// <summary>
	/// Start the dragging operation.
	/// </summary>

	void OnDragStart ()
	{
		if (!enabled || mTouchID != int.MinValue) return;

		// If we have a restriction, check to see if its condition has been met first
		if (restriction != Restriction.None)
		{
			if (restriction == Restriction.Horizontal)
			{
				Vector2 delta = UICamera.currentTouch.totalDelta;
				if (Mathf.Abs(delta.x) < Mathf.Abs(delta.y)) return;
			}
			else if (restriction == Restriction.Vertical)
			{
				Vector2 delta = UICamera.currentTouch.totalDelta;
				if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) return;
			}
			else if (restriction == Restriction.PressAndHold)
			{
				if (mPressTime + pressAndHoldDelay > RealTime.time) return;
			}
		}

		if (cloneOnDrag)
		{
			GameObject clone = NGUITools.AddChild(transform.parent.gameObject, gameObject);
			clone.transform.localPosition = transform.localPosition;
			clone.transform.localRotation = transform.localRotation;
			clone.transform.localScale = transform.localScale;

			UIButtonColor bc = clone.GetComponent<UIButtonColor>();
			if (bc != null) bc.defaultColor = GetComponent<UIButtonColor>().defaultColor;

			UICamera.currentTouch.dragged = clone;

			UIDragDropItem item = clone.GetComponent<UIDragDropItem>();
			item.Start();
			item.OnDragDropStart();
		}
		else OnDragDropStart();
	}

	/// <summary>
	/// Perform the dragging.
	/// </summary>

	void OnDrag (Vector2 delta)
	{
		if (!enabled || mTouchID != UICamera.currentTouchID) return;
		OnDragDropMove((Vector3)delta * mRoot.pixelSizeAdjustment);
	}

	/// <summary>
	/// Notification sent when the drag event has ended.
	/// </summary>

	void OnDragEnd ()
	{
		if (!enabled || mTouchID != UICamera.currentTouchID) return;
		OnDragDropRelease(UICamera.hoveredObject);
	}

#endregion

	/// <summary>
	/// Perform any logic related to starting the drag & drop operation.
	/// </summary>

	protected virtual void OnDragDropStart ()
	{
		// Automatically disable the scroll view
		if (mDragScrollView != null) mDragScrollView.enabled = false;

		// Disable the collider so that it doesn't intercept events
		if (mButton != null) mButton.isEnabled = false;
		else if (mCollider != null) mCollider.enabled = false;

		mTouchID = UICamera.currentTouchID;
		mParent = mTrans.parent;
		mRoot = NGUITools.FindInParents<UIRoot>(mParent);
		mGrid = NGUITools.FindInParents<UIGrid>(mParent);
		mTable = NGUITools.FindInParents<UITable>(mParent);

		// Re-parent the item
		if (UIDragDropRoot.root != null)
			mTrans.parent = UIDragDropRoot.root;

		Vector3 pos = mTrans.localPosition;
		pos.z = 0f;
		mTrans.localPosition = pos;

		TweenPosition tp = GetComponent<TweenPosition>();
		if (tp != null) tp.enabled = false;

		SpringPosition sp = GetComponent<SpringPosition>();
		if (sp != null) sp.enabled = false;

		// Notify the widgets that the parent has changed
		NGUITools.MarkParentAsChanged(gameObject);

		if (mTable != null) mTable.repositionNow = true;
		if (mGrid != null) mGrid.repositionNow = true;
	}

	/// <summary>
	/// Adjust the dragged object's position.
	/// </summary>

	protected virtual void OnDragDropMove (Vector3 delta)
	{
		mTrans.localPosition += delta;
	}

	/// <summary>
	/// Drop the item onto the specified object.
	/// </summary>

	protected virtual void OnDragDropRelease (GameObject surface)
	{
		if (!cloneOnDrag)
		{
			mTouchID = int.MinValue;

			// Re-enable the collider
			if (mButton != null) mButton.isEnabled = true;
			else if (mCollider != null) mCollider.enabled = true;

			// Is there a droppable container?
			UIDragDropContainer container = surface ? NGUITools.FindInParents<UIDragDropContainer>(surface) : null;

			if (container != null)
			{
				// Container found -- parent this object to the container
				mTrans.parent = (container.reparentTarget != null) ? container.reparentTarget : container.transform;

				Vector3 pos = mTrans.localPosition;
				pos.z = 0f;
				mTrans.localPosition = pos;
			}
			else
			{
				// No valid container under the mouse -- revert the item's parent
				mTrans.parent = mParent;
			}

			// Update the grid and table references
			mParent = mTrans.parent;
			mGrid = NGUITools.FindInParents<UIGrid>(mParent);
			mTable = NGUITools.FindInParents<UITable>(mParent);

			// Re-enable the drag scroll view script
			if (mDragScrollView != null)
				mDragScrollView.enabled = true;

			// Notify the widgets that the parent has changed
			NGUITools.MarkParentAsChanged(gameObject);

			if (mTable != null) mTable.repositionNow = true;
			if (mGrid != null) mGrid.repositionNow = true;
		}
		else NGUITools.Destroy(gameObject);
	}
}
