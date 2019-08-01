using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using KH;

public class UILevelSelectChapterItem : MonoBehaviour
{
    public UILabel lblNeedDonwload;
    public GameObject goNeedDownload;
    public UIWidget wgtAnimate;
	public bool isCenter;
	public UISprite scrollSprite;
	public UISprite nameSprite;
	public UIPlayAnimation uiPlayAni;
    public UISprite scopeSprite;
    public UISprite backGroundNameSpr;

	private ActiveAnimation uiAA;

    public GameObject AdapterHost;
    // 忍界大战卷轴有新的表现形式
    public GameObject scrollNormal;
    public UILevelSelectChapterItemScrollEx scrollExComp;

    private Transform _myTransform;
    public Transform myTransform
    {
        get
        {
            if (_myTransform == null)
            {
                _myTransform = this.transform;
            }

            return _myTransform;
        }
    }

    private bool chapterIDChanged = false;
    private int _chapterID;
	public int chapterID
    {
        get
        {
            return _chapterID;
        }
        set
        {
            _chapterID = value;
        }
    }

	public int resourceChapterId
	{
		get
		{
			return _resourceChapterId;
		}
	}
	private int _resourceChapterId
	{
		get
		{
            int ret = 0;
            /* 新做的章节没有动画 用老的第10话来代替先 */
            if (_chapterID < 100 && _chapterID > 22)
            {
                ret = 10;
            }
            else
            {
                ret = _chapterID > 100 ? _chapterID - 100 : _chapterID;
            }
            return ret;
        }
	}
    
    private KHSndInstance sndIns;
	private GameObject _juanzhou;
	GameObject juanzhou
	{
		get
		{
			if (_juanzhou == null || chapterIDChanged)
			{
				_juanzhou = myTransform.Find(string.Format("AdapterHost/ContainerClickScale/juanzhou{0}", _resourceChapterId)).gameObject;
                chapterIDChanged = false;
			}

			return _juanzhou;
		}
	}

    private DuplicateModel _model;
    public DuplicateModel model
    {
        get
        {
            if (_model == null)
            {
                _model = KHPluginManager.Instance.GetPluginByName(DuplicatePlugin.pluginName).Model as DuplicateModel;
            }

            return _model;
        }
    }

    private bool _isShowScrollEx = false;
    
    /// <summary>
    /// 点击卷轴之后, 是否先播动画再进入副本
    /// 0 不播动画
    /// 1 只播包含特殊卷轴动画Item的两个动画(卷轴和人物)
    /// 2 卷轴和人物动画都要播
    /// </summary>
    public static int clickItemPlayAnimSwitch = 1;
    public static int maxScrollExEvtCount = 2;
    public static int maxNormalEvtCount = 1;
    private KHEvent _exitAnimComplete;
    private int curEvtCount = 0;
    private bool isExitAnimPlaying = false;

	void Start()
	{
        _exitAnimComplete = new KHEvent(LevelSelectModel.CHAPTER_ITEM_PLAY_EXIT_ANIM_COMPLETE);
        if (uiPlayAni == null)
		{
			uiPlayAni = AdapterHost.GetComponent<UIPlayAnimation>();
		}
		uiAA = uiPlayAni.GetComponent<ActiveAnimation> ();
		uiPlayAni.onFinished.Add (new EventDelegate(OnPaComplete));
        lblNeedDonwload.bitmapFont = KHUIManager.Instance.FZYHJWFont;
    }

	void OnPaComplete()
	{
		//uiAA.enabled = false;
		//uiPlayAni.enabled = false;   
		if (uiPlayAni.clipName.Contains("ChapterItemOnCenterAni"))
		{
			if (uiPlayAni.playDirection == AnimationOrTween.Direction.Forward)
			{
				if (uiPlayAni.clipName.EndsWith("Exit"))
				{
					if (NGUITools.GetActive(juanzhou))
					{
						uiAA.enabled = false;
						NGUITools.SetActive(juanzhou, false);
					}
                    KHUIManager.Instance.Dispatcher.dispatchEvent(_exitAnimComplete);
				}
				else if(uiPlayAni.clipName.EndsWith("Enter"))
				{
					if (uiPlayAni.clipName.EndsWith(string.Format("ChapterItemOnCenterAni{0}Enter", _resourceChapterId)))
					{
						uiPlayAni.clipName = string.Format("ChapterItemOnCenterAni{0}Recycle", _resourceChapterId);
						uiPlayAni.playDirection = AnimationOrTween.Direction.Forward;
						uiPlayAni.Play(true, false);
					}
				}
			}
		}
		else if (uiPlayAni.clipName.Contains("ChapterItemInitMoveDown0"))
		{
			uiAA.enabled = false;
		}
        else if (uiPlayAni.clipName.Contains("ChapterItemExpand"))
        {
            KHPluginManager.Instance.GetPluginByName(LevelSelectPlugin.pluginName).HideView(UIDef.LEVEL_SELECT_CHAPTER);
        }
	}

	public void OnClick()
	{
        if ((!isCenter || isExitAnimPlaying) && !MessageManager.Instance.IsActivate)
		{
            return;
		}

        KHUIManager.getInstance().dispatchClickToGuideEvent(this.tag);

		var c = KHDataManager.CONFIG.getChapterItemConfig(_chapterID);
		
		if (c==null)
		{
            Debuger.LogError(string.Format("没有找到章节ID为:{0}的配置!!!!!!!!!!", _chapterID));
			return;
		}

		KHAudioManager.PlaySound(9907);

		if (!ExpandPackManager.isValid(1, _resourceChapterId))
        {
			KHPluginManager.Instance.SendMessage(ExpandPackPlugin.PluginName, "ExpandPack.show", new ExpandPackView.ShowArg() { type = 1, data = _resourceChapterId });
            return;
        }
        
        if (clickItemPlayAnimSwitch == 1)   // 1只播有含有卷轴动画Item的退出动画, 目前只有忍界大战都卷轴动画
        {
            // 播完动画再跳转
            curEvtCount = 0;
            if (_isShowScrollEx)
            {
                isExitAnimPlaying = true;
                KHUIManager.Instance.Dispatcher.removeEventListener(LevelSelectModel.CHAPTER_ITEM_PLAY_EXIT_ANIM_COMPLETE, OnExitAnimPlayComplete);
                KHUIManager.Instance.Dispatcher.addEventListener(LevelSelectModel.CHAPTER_ITEM_PLAY_EXIT_ANIM_COMPLETE, OnExitAnimPlayComplete);
                HideAllJuanzhou();
            }
            else
            {
                JumpToDungeonView();
            }
        }
        else if (clickItemPlayAnimSwitch == 2)  // 卷轴和人物的退出动画都要播
        {
            // 播完动画再跳转
            curEvtCount = 0;
            isExitAnimPlaying = true;
            KHUIManager.Instance.Dispatcher.removeEventListener(LevelSelectModel.CHAPTER_ITEM_PLAY_EXIT_ANIM_COMPLETE, OnExitAnimPlayComplete);
            KHUIManager.Instance.Dispatcher.addEventListener(LevelSelectModel.CHAPTER_ITEM_PLAY_EXIT_ANIM_COMPLETE, OnExitAnimPlayComplete);
            HideAllJuanzhou();
        }
        else                                    // 不播动画
        {
            JumpToDungeonView();
        }

        MessageManager.Instance.serializeToLocal(
            new MouseAction(this, RemoteModel.Instance.CurrentTime), 
            MessageManager.DEST_PATH_MOUSE_EVENT);
    }

    private void JumpToDungeonView()
    {
        var c = KHDataManager.CONFIG.getChapterItemConfig(_chapterID);
        if (c.chapterType == 0)
        {
            // 禁用输入
            KHUIManager.uiTouchEnabled = false;

            //DuplicateModel model = KHPluginManager.Instance.GetPluginByName (DuplicatePlugin.pluginName).Model as DuplicateModel;
            model.CurChapterID = _chapterID;

            KHUIManager.Instance.SendMessage(UIDef.LEVEL_SELECT_CHAPTER, "SetIsAni", true);
            KHUIManager.Instance.SendMessage(UIDef.LEVEL_SELECT_CHAPTER, "ChangeToDungeonView");

            //如果点击了最新的章节(冒险模式)，并且是第一次访问该章节，则记录下来
            if (_chapterID == model.NewestChapterID && model.VisitedNewestChapterID != model.NewestChapterID)
            {
                model.VisitedNewestChapterID = model.NewestChapterID;
                KHUtil.SetInt("JumpToChapterID", model.NewestChapterID);
                KHUtil.Save();
            }
        }
        else
        {
            model.CurChapterID = _chapterID;
            ClickChapterGroup(_chapterID);
        }
    }

    public void ClickChapterGroup(int chapterGroupID)
    {
        // 章节17跳转到忍界大战
        //ChapterItemConfig chapterGroupCfg = KHDataManager.CONFIG.getChapterItemConfig(chapterGroupID);
        //if (chapterGroupCfg.chapterType == 1)
        //{
        //    KHPluginManager.Instance.SendMessage(NinkaiTaisenLevelSelectPlugin.pluginName, NinkaiTaisenLevelSelectOperation.OP_EnterMainMap, chapterGroupID);
        //}
        // 关闭云彩背景
        KHUIManager.getInstance().SendMessage(UIDef.LEVEL_SELECTBG_UI, UIEventDef.PLAY_HIDE_ANI);
        // 现在直接跳转到忍界大战, 后续有新类型再说
        KHPluginManager.Instance.SendMessage(NinkaiTaisenLevelSelectPlugin.pluginName, NinkaiTaisenLevelSelectOperation.OP_ClickChapterItem, chapterGroupID);
    }

	public void UpdateItem(int inChapterID)
	{
        isCenter = false;
        if (chapterID > 0)
        {
            // 要先把老的juanzhou隐藏掉
            NGUITools.SetActive(juanzhou, false);
        }
        
        chapterID = inChapterID;
        chapterIDChanged = true;
		gameObject.tag = GameObjectTagDef.LevelSelect_Chapter + _resourceChapterId;
		scrollSprite.spriteName = string.Format("bg-juanzhou{0}", _resourceChapterId);
		nameSprite.spriteName = string.Format("wenzi-juanzhou{0}", _resourceChapterId);
        nameSprite.MakePixelPerfect();
        NGUITools.SetActive(goNeedDownload, false);
        UpdateItemEx();
    }

    /// <summary>
    /// 新增需求的额外操作
    /// </summary>
    public void UpdateItemEx()
    {
        _isShowScrollEx = model.spcailScrollChapterID.Contains(_resourceChapterId);
        // 第22关需要对卷轴进行特殊表现
        NGUITools.SetActiveEx(scrollNormal, !_isShowScrollEx);
        NGUITools.SetActiveEx(scrollExComp.gameObject, _isShowScrollEx);
        if (_isShowScrollEx)
        {
            UILevelSelectChapterItemScrollEx.ItemArg arg = new UILevelSelectChapterItemScrollEx.ItemArg
            {
                chapterID = _chapterID,
                resourceID = _resourceChapterId
            };
            scrollExComp.UpdateItem(arg);
        }
        isExitAnimPlaying = false;
    }

	public void OnLeaveCenter()
	{

		if (!isCenter)
		{
			return;
		}
		//Debuger.LogWarning (string.Format("OnLeaveCenter{0}", chapterID));
		isCenter = false;
		if(sndIns != null)
		{
			KHAudioManager.RemoveSound(sndIns);
			sndIns = null;
		}
		HideAllJuanzhou ();
        NGUITools.SetActive(goNeedDownload, false);
	}

	public void OnMoveToCenter()
	{
		if (isCenter)
		{
			return;
		}
		//Debuger.LogWarning (string.Format("OnMoveToCenter{0}", chapterID));
		isCenter = true;
		HideAllJuanzhou ();
		KHAudioManager.PlaySound(9906);
		sndIns = KHAudioManager.PlaySound(40000 + _resourceChapterId);
		NGUITools.SetActive(juanzhou, true);
		UIPlayAnimation pa = AdapterHost.GetComponent<UIPlayAnimation>();
		pa.clipName = string.Format("ChapterItemOnCenterAni{0}Enter", _resourceChapterId);
		pa.playDirection = AnimationOrTween.Direction.Forward;
		pa.Play(true, false);

        DuplicateModel model = KHPluginManager.Instance.GetPluginByName(DuplicatePlugin.pluginName).Model as DuplicateModel;
        model.CurChapterID = _chapterID;

		if (ExpandPackManager.isValid(1, _resourceChapterId))
        {
            NGUITools.SetActive(goNeedDownload, false);
        }
        else
        {
            NGUITools.SetActive(goNeedDownload, true);
			if(ExpandPackManager.isNeedUpdate(1, _resourceChapterId))
            {
                lblNeedDonwload.text = "需更新扩展包";
            }
            else
            {
                lblNeedDonwload.text = "需下载扩展包";
            }
        }

        if (_isShowScrollEx)
        {
            scrollExComp.PlayEnterAnim();
        }
	}

	private void HideAllJuanzhou()
	{
		if (NGUITools.GetActive(juanzhou))
		{
			UIPlayAnimation pa = AdapterHost.GetComponent<UIPlayAnimation>();
			pa.clipName = string.Format("ChapterItemOnCenterAni{0}Exit", _resourceChapterId);
			pa.playDirection = AnimationOrTween.Direction.Forward;
			pa.Play(true, false);

            if (_isShowScrollEx)
            {
                scrollExComp.PlayExitAnim();
            }
		}
	}

	public void MoveDown()
	{
		StartCoroutine (MoveDownAni());
	}

	IEnumerator MoveDownAni()
	{
		if (myTransform.localScale.x < 0.625f && myTransform.localScale.x > 0.375f)
		{
			UIPlayAnimation pa = AdapterHost.GetComponent<UIPlayAnimation>();
			pa.clipName = "ChapterItemMoveDown";
			pa.playDirection = AnimationOrTween.Direction.Forward;
			pa.Play(true, false);
		}
		else if(myTransform.localScale.x < 0.875f && myTransform.localScale.x > 0.625f)
		{
			//yield return new WaitForSeconds(0.2f);
			UIPlayAnimation pa = AdapterHost.GetComponent<UIPlayAnimation>();
			pa.clipName = "ChapterItemMoveDown";
			pa.playDirection = AnimationOrTween.Direction.Forward;
			pa.Play(true, false);
		}
		else if(myTransform.localScale.x < 1.125f && myTransform.localScale.x > 0.875f)
		{
			yield return new WaitForSeconds(0.3f);
			UIPlayAnimation pa = AdapterHost.GetComponent<UIPlayAnimation>();
			pa.clipName = "ChapterItemExpand";
			pa.playDirection = AnimationOrTween.Direction.Forward;
			pa.Play(true, false);
		}

        yield return null;
	}


	public void InitMoveDown()
	{
	    wgtAnimate.alpha = 0.01f;
		StartCoroutine (InitMoveDownAni());
	}
	
	IEnumerator InitMoveDownAni()
	{
		if (myTransform.localScale.x < 0.625f/* && myTransform.localScale.x > 0.375f*/)
		{
			yield return new WaitForSeconds(0.4f);
			UIPlayAnimation pa = AdapterHost.GetComponent<UIPlayAnimation>();
			pa.clipName = "ChapterItemInitMoveDown0";
			pa.playDirection = AnimationOrTween.Direction.Forward;
			pa.Play(true, false);
			KHUIManager.Instance.SendMessage (UIDef.LEVEL_SELECT_CHAPTER, "SetIsAni", false);
		}
		else if(myTransform.localScale.x < 0.875f && myTransform.localScale.x > 0.625f)
		{
			yield return new WaitForSeconds(0.2f);
			UIPlayAnimation pa = AdapterHost.GetComponent<UIPlayAnimation>();
			pa.clipName = "ChapterItemInitMoveDown0";
			pa.playDirection = AnimationOrTween.Direction.Forward;
			pa.Play(true, false);
			//yield return new WaitForSeconds(0.6f);
			KHUIManager.Instance.SendMessage (UIDef.LEVEL_SELECT_CHAPTER, "SetIsAni", false);
		}
		else if(myTransform.localScale.x < 1.125f && myTransform.localScale.x > 0.875f)
		{
			UIPlayAnimation pa = AdapterHost.GetComponent<UIPlayAnimation>();
			pa.clipName = "ChapterItemInitMoveDown0";
			pa.playDirection = AnimationOrTween.Direction.Forward;
			pa.Play(true, false);
		}
		else
		{
			UIPlayAnimation pa = AdapterHost.GetComponent<UIPlayAnimation>();
			pa.clipName = "ChapterItemInitMoveDown0";
			pa.playDirection = AnimationOrTween.Direction.Forward;
			pa.Play(true, false);
		}

		yield return null;
        wgtAnimate.alpha = 1f;
	}

	public void CloseMoveUp()
	{
		HideAllJuanzhou ();

		StartCoroutine (CloseMoveUpAni());
	}
	
	IEnumerator CloseMoveUpAni()
	{
		//等待隐藏小动画完成
		yield return new WaitForSeconds (0.5f);

		if (myTransform.localScale.x <= 0.625f && myTransform.localScale.x >= 0.375f)
		{
			UIPlayAnimation pa = AdapterHost.GetComponent<UIPlayAnimation>();
			pa.clipName = "ChapterItemInitMoveDown0";
			pa.playDirection = AnimationOrTween.Direction.Reverse;
			pa.Play(true, false);
		}
		else if(myTransform.localScale.x <= 0.875f && myTransform.localScale.x >= 0.625f)
		{
			UIPlayAnimation pa = AdapterHost.GetComponent<UIPlayAnimation>();
			pa.clipName = "ChapterItemInitMoveDown0";
			pa.playDirection = AnimationOrTween.Direction.Reverse;
			pa.Play(true, false);
		}
		else if(myTransform.localScale.x <= 1.125f && myTransform.localScale.x >= 0.875f)
		{
			UIPlayAnimation pa = AdapterHost.GetComponent<UIPlayAnimation>();
			pa.clipName = "ChapterItemInitMoveDown0";
			pa.playDirection = AnimationOrTween.Direction.Reverse;
			pa.Play(true, false);
		}
		
		yield return null;
	}

    private void OnExitAnimPlayComplete(KHEvent _evt)
    {
        curEvtCount++;
        if (curEvtCount >= (_isShowScrollEx ? maxScrollExEvtCount : maxNormalEvtCount))
        {
            KHUIManager.Instance.Dispatcher.removeEventListener(LevelSelectModel.CHAPTER_ITEM_PLAY_EXIT_ANIM_COMPLETE, OnExitAnimPlayComplete);
            isExitAnimPlaying = false;
            JumpToDungeonView();
        }
    }

    void OnDisable()
    {
        if (KHUIManager.Instance != null && KHUIManager.Instance.Dispatcher != null)
        {
            KHUIManager.Instance.Dispatcher.removeEventListener(LevelSelectModel.CHAPTER_ITEM_PLAY_EXIT_ANIM_COMPLETE, OnExitAnimPlayComplete);
        }
    }
}
