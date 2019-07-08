//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections;
using KH;

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

[AddComponentMenu("NGUI/Interaction/Wrap Content Ex")]
public class UIWrapContentEx : UIWrapContent
{
	/// <summary>
	/// 总子物体个数
	/// </summary>
	private int totalSize = 20;
	public int TotalSize
	{
		get
		{
			return totalSize;
		}
		set
		{
			totalSize = value;
			Initialize();
		}
	}

	/// <summary>
	/// 是否无限循环模式.
	/// </summary>
	public bool isEndless = false;
	/// <summary>
	/// 左边or上边的索引值.
	/// </summary>
	public int leftTopIndex = 0;
	/// <summary>
	/// 右边or下边的索引值.
	/// </summary>
	public int rightBottomIndex = 0;

    public bool fixDragEffect = true;

    /// <summary>
    /// 下拉加载更多的tip信息
    /// </summary>
    public UIWidget loadingMoreTipWidget;

	public delegate void OnUpdateItem (Transform item, int index);
	/// <summary>
	/// item内容更新委托
	/// </summary>
	public OnUpdateItem onUpdateItem;



	protected override void Start ()
	{
		Initialize();
	}

	public override void Initialize()
	{
		SortBasedOnScrollMovement();
		WrapContentEx();

		/*onUpdateItem = delegate(Transform item, int index) 
		{
			item.FindChild("Label").GetComponent<UILabel>().text = index.ToString();
		};*/

		if (mScroll != null)
		{
            //mScroll.onDragStart = dragStart;
            //mScroll.onDragFinished = dragFinished;
			mScroll.GetComponent<UIPanel>().onClipMove = OnMove;
			if (isEndless)
			{
				mScroll.restrictWithinPanel = false;
				if (mScroll.dragEffect == UIScrollView.DragEffect.MomentumAndSpring)
					mScroll.dragEffect = UIScrollView.DragEffect.Momentum;
			}
			else
			{
				/*
				 * 非无限模式不能启用物件裁剪功能
				 */
				cullContent = false;
                if (fixDragEffect)
                {
				    mScroll.restrictWithinPanel = true;
				    mScroll.dragEffect = UIScrollView.DragEffect.MomentumAndSpring;
                }
			}
		}
		
		//KHInvokeLater.Invoke(gameObject, 0.1f, WrapContent);
	}
    private Vector3 startPos = Vector3.zero;
    public void dragFinished()
    {
        if (mScroll == null)
        {
            Debuger.LogWarning("scroll view 为空");
            return;
        }

        if (!mScroll.shouldMoveHorizontally)
        {
            if (mScroll.dragEffect == UIScrollView.DragEffect.MomentumAndSpring)
            {
                SpringPanel.Begin(mScroll.GetComponent<UIPanel>().gameObject, startPos, 13f).strength = 8f;
            }
        }

    }

    public void dragStart()
    {
        startPos = mScroll.gameObject.transform.localPosition;
    }


	[ContextMenu("Sort Based on Scroll Movement")]
	public override void SortBasedOnScrollMovement ()
	{
		if (!CacheScrollView()) return;
		
		// Cache all children and place them in order
		mChildren.Clear();
	    for (int i = 0; i < mTrans.childCount; ++i)
	    {
            mChildren.Add(mTrans.GetChild(i));
	    }
		
		// Sort the list of children so that they are in order
		if (mHorizontal) mChildren.Sort(UIGrid.SortHorizontal);
		else mChildren.Sort(UIGrid.SortVertical);
		ResetChildPositions();
	}

	/// <summary>
	/// Helper function that resets the position of all the children.
	/// </summary>
	
	public override void ResetChildPositions ()
	{
		mPanel.transform.localPosition = Vector3.zero;
		mPanel.clipOffset = Vector2.zero;
        // 还要把scrollview的momentum置0
        mScroll.currentMomentum = Vector3.zero;
        mScroll.DisableSpring();

		leftTopIndex = 0;
		rightBottomIndex = mChildren.size > totalSize ? totalSize - 1 : mChildren.size - 1;

		for (int i=0; i < mChildren.size; ++i)
		{
			Transform t = mChildren[i];
			t.localPosition = mHorizontal ? new Vector3(i * itemSize, 0f, 0f) : new Vector3(0f, -i * itemSize, 0f);
            // 无限模式不隐藏物件
			if (i>=totalSize && !isEndless)
			{
				NGUITools.SetActive(t.gameObject, false);
				continue;
			}

			if (!NGUITools.GetActive(t.gameObject))
			{
				NGUITools.SetActive(t.gameObject, true);
			}

			if (onUpdateItem != null)
			{
				onUpdateItem(t, i);
			}
		}

	    if (loadingMoreTipWidget != null)
	    {
	        loadingMoreTipWidget.excludeWhenCalculateBound = true;
            UIWidget[] widgets = loadingMoreTipWidget.GetComponentsInChildren<UIWidget>(true);
            for (int i = 0, imax = widgets.Length; i < imax; ++i)
            {
                UIWidget w = widgets[i];
                w.excludeWhenCalculateBound = true;
            }
            loadingMoreTipWidget.transform.localPosition = mHorizontal ? new Vector3(totalSize * itemSize + mTrans.localPosition.x, 0f, 0f) : new Vector3(0f, -totalSize * itemSize + mTrans.localPosition.y, 0f);
	    }
	}

	/// <summary>
	/// Callback triggered by the UIPanel when its clipping region moves (for example when it's being scrolled).
	/// </summary>
	
	protected override void OnMove (UIPanel panel) 
	{
		WrapContentEx(); 
	}

	/// <summary>
	/// Wrap all content, repositioning all children as needed.
	/// </summary>
	
	public void WrapContentEx ()
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

                if (distance < -extents && (/*rightBottomIndex != totalSize - 1*/Mathf.Abs(Mathf.RoundToInt(t.localPosition.x / itemSize)) + mChildren.size < totalSize || isEndless))
				{
					leftTopIndex= leftTopIndex == totalSize - 1 ? 0 : leftTopIndex+1;
					rightBottomIndex= rightBottomIndex == totalSize - 1 ? 0 : rightBottomIndex+1;

                    //  to do 瞬移的时候，这里不能只+一个extents * 2f，而要加到不能加为止（即，distance小于extents时）
                    while (distance < -extents && (Mathf.Abs(Mathf.RoundToInt(t.localPosition.x / itemSize)) + mChildren.size < totalSize || isEndless))
				    {
                        //Debuger.LogWarning(string.Format("distance: {0} -extents: {1} t.localPosition: {2} index: {3} min: {4} max: {5} t.name: {6} i: {7} rightBottomIndex: {8} totalSize: {9}", distance, -extents, t.localPosition, Mathf.Abs(Mathf.RoundToInt(t.localPosition.x / itemSize)), min, max, t.name, i, rightBottomIndex, totalSize));
                        t.localPosition += new Vector3(extents * 2f, 0f, 0f);
                        distance = t.localPosition.x - center.x;
				    }
					
					if (onUpdateItem != null)
					{
                        int dataIndex = Mathf.Abs(Mathf.RoundToInt(t.localPosition.x / itemSize));
                        if (isEndless)
                        {
                            while (dataIndex >= TotalSize && TotalSize > 0)
                                dataIndex -= TotalSize;
                        }
                        if (dataIndex < TotalSize)
                        {
                            onUpdateItem(t, dataIndex);
                        }
					}
                    // 需要重新计算bounds
                    mScroll.mCalculatedBounds = false;
                    //Debuger.LogWarning(string.Format("distance: {0} i: {1} rightBottomIndex: {3} totalSize: {4} t.localPosition: {5} min: {6} max: {7} t.name: {2} ", distance, i, t.name, rightBottomIndex, totalSize, t.localPosition, min, max));
				}
                else if (distance > extents && (/*leftTopIndex != 0*/Mathf.Abs(Mathf.RoundToInt(t.localPosition.x / itemSize)) - mChildren.size >= 0 || isEndless))
				{
					leftTopIndex= leftTopIndex == 0 ? totalSize - 1 : leftTopIndex - 1;
					rightBottomIndex= rightBottomIndex == 0 ? totalSize - 1 : rightBottomIndex - 1;

                    while (distance > extents && (Mathf.Abs(Mathf.RoundToInt(t.localPosition.x / itemSize)) - mChildren.size >= 0 || isEndless))
                    {
                        //Debuger.LogWarning(string.Format("distance: {0} extents: {1} t.localPosition: {2} index: {3} min: {4} max: {5} t.name: {6} i: {7} rightBottomIndex: {8} totalSize: {9}", distance, extents, t.localPosition, Mathf.Abs(Mathf.RoundToInt(t.localPosition.x / itemSize)), min, max, t.name, i, rightBottomIndex, totalSize));
                        t.localPosition -= new Vector3(extents * 2f, 0f, 0f);
                        distance = t.localPosition.x - center.x;
                    }
					
					if (onUpdateItem != null)
					{
                        int dataIndex = Mathf.Abs(Mathf.RoundToInt(t.localPosition.x / itemSize));
                        if (isEndless)
                        {
                            while (dataIndex >= TotalSize && TotalSize > 0)
                                dataIndex -= TotalSize;
                        }
                        if (dataIndex < TotalSize)
                        {
                            onUpdateItem(t, dataIndex);
                        }
					}
                    // 需要重新计算bounds
                    mScroll.mCalculatedBounds = false;

                    //Debuger.LogWarning(string.Format("distance: {0} i: {1} leftTopIndex: {2} totalSize: {4} t.localPosition: {5} min: {6} max: {7} t.name: {3} ", distance, i, leftTopIndex, t.name, totalSize, t.localPosition, min, max));
				}
				
				if (cullContent)
				{
					distance += mPanel.clipOffset.x - mTrans.localPosition.x;

                    if (!UICamera.IsPressed(t.gameObject))
                        t.gameObject.SetActive((distance > min && distance < max));
						//NGUITools.SetActive(t.gameObject, (distance > min && distance < max), false);
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

                if (distance < -extents && (/*leftTopIndex != 0*/ Mathf.Abs(Mathf.RoundToInt(t.localPosition.y / itemSize)) - mChildren.size >= 0 || isEndless))
				{
                    //Debuger.Log(string.Format("totalSize: {0} leftTopIndex: {1} rightBottomIndex: {2}", totalSize, leftTopIndex, rightBottomIndex));
					leftTopIndex= leftTopIndex == 0 ? totalSize - 1 : leftTopIndex - 1;
					rightBottomIndex= rightBottomIndex == 0 ? totalSize - 1 : rightBottomIndex - 1;

                    while (distance < -extents && (Mathf.Abs(Mathf.RoundToInt(t.localPosition.y / itemSize)) - mChildren.size >= 0 || isEndless))
                    {
                        t.localPosition += new Vector3(0f, extents * 2f, 0f);
                        distance = t.localPosition.y - center.y;
                        //Debuger.LogWarning(string.Format("distance: {0} i: {1} leftTopIndex: {2} rightBottomIndex: {9} updateIndex: {10} totalSize: {4} t.localPosition: {5} min: {6} max: {7} t.name: {3} preLocalPos: {8}", distance, i, leftTopIndex, t.name, totalSize, t.localPosition.y, min, max, preLocalPos, rightBottomIndex, Mathf.Abs(Mathf.RoundToInt(t.localPosition.y / itemSize))));
                    }
                    
					if (onUpdateItem != null)
					{
                        int dataIndex = Mathf.Abs(Mathf.RoundToInt(t.localPosition.y / itemSize));
                        if (isEndless)
                        {
                            while (dataIndex >= TotalSize && TotalSize > 0)
                                dataIndex -= TotalSize;
                        }
                        if (dataIndex < TotalSize)
                        {
                            onUpdateItem(t, dataIndex);
                        }
					}
                    // 需要重新计算bounds
				    mScroll.mCalculatedBounds = false;
				}
                else if (distance > extents && (/*rightBottomIndex != totalSize - 1 */Mathf.Abs(Mathf.RoundToInt(t.localPosition.y / itemSize)) + mChildren.size < totalSize || isEndless))
				{
                    //Debuger.Log(string.Format("totalSize: {0} leftTopIndex: {1} rightBottomIndex: {2}", totalSize, leftTopIndex, rightBottomIndex));
					leftTopIndex= leftTopIndex == totalSize - 1 ? 0 : leftTopIndex+1;
					rightBottomIndex= rightBottomIndex == totalSize - 1 ? 0 : rightBottomIndex+1;

                    while (distance > extents && (Mathf.Abs(Mathf.RoundToInt(t.localPosition.y / itemSize)) + mChildren.size < totalSize || isEndless))
                    {
                        t.localPosition -= new Vector3(0f, extents * 2f, 0f);
                        //Debuger.LogWarning(string.Format("distance: {0} i: {1} leftTopIndex: {2} rightBottomIndex: {9} updateIndex: {10} totalSize: {4} t.localPosition: {5} min: {6} max: {7} t.name: {3} preLocalPos: {8}", distance, i, leftTopIndex, t.name, totalSize, t.localPosition.y, min, max, preLocalPos, rightBottomIndex, Mathf.Abs(Mathf.RoundToInt(t.localPosition.y / itemSize))));
                        distance = t.localPosition.y - center.y;
                    }
					
					if (onUpdateItem != null)
					{
                        int dataIndex = Mathf.Abs(Mathf.RoundToInt(t.localPosition.y / itemSize));
                        if (isEndless)
                        {
                            while (dataIndex >= TotalSize && TotalSize > 0)
                                dataIndex -= TotalSize;
                        }
                        if (dataIndex < TotalSize)
                        {
                            onUpdateItem(t, dataIndex);
                        }
					}
                    // 需要重新计算bounds
                    mScroll.mCalculatedBounds = false;
				}
				
				if (cullContent)
				{
					distance += mPanel.clipOffset.y - mTrans.localPosition.y;
					if (!UICamera.IsPressed(t.gameObject))
                        t.gameObject.SetActive((distance > min && distance < max));
						//NGUITools.SetActive(t.gameObject, (distance > min && distance < max), false);
				}
			}
		}
	}

	public bool IsHorizontal()
	{
		return mHorizontal;
	}

    /// <summary>
    /// 运行时动态调整可滑动物件的总个数，不初始化各种信息
    /// </summary>
    /// <param name="nSize"></param>
    /// <returns></returns>
    public bool AdjustTotalSizeWithoutInit(int nSize)
    {
        //bool bAdjustIndex = false;

        totalSize = nSize;

        //Debuger.Log(string.Format("totalSize: {0} leftTopIndex: {1} rightBottomIndex: {2}", totalSize, leftTopIndex, rightBottomIndex));

        // 这边修改了索引值，会导致业务界面中缓存的选中索引值不正确，业务界面中的缓存值也需要做相应的修改 
        //if (rightBottomIndex >= totalSize && totalSize > 0)
        //{
            /*if (leftTopIndex > 0)
            {
                --leftTopIndex;
            }

            if (rightBottomIndex > 0)
            {
                --rightBottomIndex;
            }*/
        // 如果有物件的位置超出了范围，说明调整总个数后，需要将物件的位置调整至正确
        int maxOffset = 0;
        for (int i = 0; i < mChildren.size && i < totalSize; ++i)
        {
            Transform t = mChildren[i];

            if (mHorizontal)
            {
                var offset = Mathf.Abs(Mathf.RoundToInt(t.localPosition.x / itemSize)) - (totalSize - 1);
                maxOffset = maxOffset >= offset ? maxOffset : offset;
            }
            else
            {
                var offset = Mathf.Abs(Mathf.RoundToInt(t.localPosition.y / itemSize)) - (totalSize - 1);
                maxOffset = maxOffset >= offset ? maxOffset : offset;
            }
        }

        // 复用的物件的位置也需要调整
        if (maxOffset > 0)
        {
            for (int i = 0; i < mChildren.size; ++i)
            {
                Transform t = mChildren[i];

                if (mHorizontal)
                {
                    t.localPosition -= new Vector3(itemSize * maxOffset, 0f, 0f);
                }
                else
                {
                    t.localPosition += new Vector3(0f, itemSize * maxOffset, 0f);
                }
            }
        }
            
        //}

        // 将tip放到最末尾
        if (loadingMoreTipWidget != null)
        {
            loadingMoreTipWidget.excludeWhenCalculateBound = true;
            UIWidget[] widgets = loadingMoreTipWidget.GetComponentsInChildren<UIWidget>(true);
            for (int i = 0, imax = widgets.Length; i < imax; ++i)
            {
                UIWidget w = widgets[i];
                w.excludeWhenCalculateBound = true;
            }
            loadingMoreTipWidget.transform.localPosition = mHorizontal ? new Vector3(totalSize * itemSize + mTrans.localPosition.x, 0f, 0f) : new Vector3(0f, -totalSize * itemSize + mTrans.localPosition.y, 0f);
        }

        return maxOffset > 0;
    }

    /// <summary>
    /// 更新物件内容，在修改totalSize后，即调用AdjustTotalSizeWithoutInit后，需要主动调用
    /// </summary>
    public void UpdateContents(bool bNeedRestricWithBounds)
    {
        if (mHorizontal) mChildren.Sort(UIGrid.SortHorizontal);
        else mChildren.Sort(UIGrid.SortVertical);

        for (int i = 0; i < mChildren.size; ++i)
        {
            Transform t = mChildren[i];
            // 无限模式不隐藏物件
            
            // 20170313 - symonwu
            // 这里如果只按照索引i来隐藏物件，隐藏的物件不一定是最边上的，有可能是中间的
            
            if (i >= totalSize && !isEndless)
            {
                NGUITools.SetActive(t.gameObject, false);
                continue;
            }

            if (!NGUITools.GetActive(t.gameObject))
            {
                NGUITools.SetActive(t.gameObject, true);
            }

            if (onUpdateItem != null)
            {
                if (mHorizontal)
                {
                    onUpdateItem(t, Mathf.Abs(Mathf.RoundToInt(t.localPosition.x / itemSize)));
                }
                else
                {
                    onUpdateItem(t, Mathf.Abs(Mathf.RoundToInt(t.localPosition.y / itemSize)));
                }
            }
        }

        if (mScroll != null && bNeedRestricWithBounds)
        {
            mScroll.RestrictWithinBounds(false, mScroll.canMoveHorizontally, mScroll.canMoveVertically, true);
        }
    }

    public void UpdateContents()
    {
        UpdateContents(true);
    }

    private float _aniTime = 1f;
    private float _aniSpringStrenth = 8f;
    private SpringPosition.OnFinished _onAniFinish;
    /// <summary>
    /// 目前只支持横向的滚动条。
    /// 只支持减少一个子控件，并且移除的子控件会移动到滚动条最右边或最左边，位于被移除的控件右边的控件会往左移动一个itemsize的距离（包括被移除的控件）。
    /// </summary>
    /// <param name="goItem"></param>
    /// <param name="newSize"></param>
    /// <param name="aniTime"></param>
    /// <param name="aniStrenth"></param>
    /// <param name="onFinish"></param>
    public void DisappearOneAndMoveOn(GameObject goItem, int newSize, float aniTime, float aniStrenth, SpringPosition.OnFinished onFinish)
    {
        _aniTime = aniTime;
        totalSize = newSize;
        _aniSpringStrenth = aniStrenth;
        _onAniFinish = onFinish;
        for (int i = 0; i < mChildren.size; ++i)
        {
            Transform t = mChildren[i];

            if (t.gameObject == goItem)
            {
                goItem.SendMessage("OnWrapDisappear", aniTime, SendMessageOptions.DontRequireReceiver);
                StartCoroutine("MoveOn", goItem);
                break;
            }
        }
    }

    private IEnumerator MoveOn(GameObject goItem)
    {
        // 等待消失动画完成
        yield return new WaitForSeconds(_aniTime);
        goItem.SendMessage("OnWrapDisappearEnd", SendMessageOptions.DontRequireReceiver);

        Vector3 disappearPos = goItem.transform.localPosition;
        // 移到最后或者最前
        Vector3 rightPos = GetRightestPos() + new Vector3(itemSize, 0f, 0f);
        // 可以移动到最右
        // 不需要取绝对值，横向滚动条物件的x值肯定大于等于0
        // 纵向的才要
        if (Mathf.RoundToInt(rightPos.x / itemSize) < totalSize)
        {
            goItem.transform.localPosition = rightPos;
            onUpdateItem(goItem.transform, Mathf.Abs(Mathf.RoundToInt(goItem.transform.localPosition.x / itemSize)));
        }
        else 
        {
            Vector3 leftPos = GetLeftestPos() - new Vector3(itemSize, 0f, 0f);

            // 可以移动最左
            // 这里不能取绝对值。。 否则结果肯定大于等于0了
            if (Mathf.RoundToInt(leftPos.x / itemSize) >= 0)
            {
                goItem.transform.localPosition = leftPos;
                onUpdateItem(goItem.transform, Mathf.Abs(Mathf.RoundToInt(goItem.transform.localPosition.x / itemSize)));
            }
            else
            {
                // 两边都不满足，则移动到最右，并且隐藏
                goItem.transform.localPosition = rightPos;
                NGUITools.SetActive(goItem, false);
            }
        }
        
        // 如果总大小小于预留控件大小，则需要隐藏刚消失的控件
//         if (totalSize < mChildren.size)
//         {
//             NGUITools.SetActive(goItem, false);
//         }
//         else
//         {
//             onUpdateItem(goItem.transform, Mathf.Abs(Mathf.RoundToInt(goItem.transform.localPosition.x / itemSize)));
//         }

        bool hasSpring = false;
        // 超过消失点的控件，spring到合适位置
        // to do 如果是不可见的控件，不能用spring，直接设置到目标位置
        for (int i = 0; i < mChildren.size; ++i)
        {
            Transform t = mChildren[i];

            if (t.localPosition.x > disappearPos.x)
            {
                if (NGUITools.GetActive(t.gameObject))
                {
                    hasSpring = true;
                    if (Mathf.RoundToInt(t.localPosition.x - disappearPos.x) == itemSize)
                    {
                        t.gameObject.SendMessage("OnWrapSpringBegin", SendMessageOptions.DontRequireReceiver);
                        SpringPosition.Begin(t.gameObject, t.localPosition - new Vector3(itemSize, 0f, 0f), _aniSpringStrenth).onFinished = _onAniFinish;
                    }
                    else
                    {
                        SpringPosition.Begin(t.gameObject, t.localPosition - new Vector3(itemSize, 0f, 0f), _aniSpringStrenth);
                    }
                }
                else
                {
                    t.localPosition = t.localPosition - new Vector3(itemSize, 0f, 0f);
                }
            }
        }

        if (!hasSpring)
        {
            if (_onAniFinish != null)
            {
                _onAniFinish();
                _onAniFinish = null;
            }
        }
    }

    private Vector3 GetRightestPos()
    {
        Transform righttestTr = mChildren[0];
        for (int i = 0; i < mChildren.size; ++i)
        {
            Transform t = mChildren[i];

            if (righttestTr.localPosition.x < t.localPosition.x)
            {
                righttestTr = t;
            }
        }

        return righttestTr.localPosition;
    }

    private Vector3 GetLeftestPos()
    {
        Transform LeftestTr = mChildren[0];
        for (int i = 0; i < mChildren.size; ++i)
        {
            Transform t = mChildren[i];

            if (LeftestTr.localPosition.x > t.localPosition.x)
            {
                LeftestTr = t;
            }
        }

        return LeftestTr.localPosition;
    }

	/// <summary>
	/// 参数item为需要改变数据的元素
	/// 参数index为数据的索引（0-totalSize）
	/// 每个不同的滚动条，可以根据需要重载此函数，实现自己的内容更新
	/// </summary>

	/*protected override void UpdateItem (Transform item, int index) 
	{
		item.FindChild("Label").GetComponent<UILabel>().text = index.ToString();
	}*/
}
