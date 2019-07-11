using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using KH;
using KH.CameraBehaviour;
using System;

public class SceneUIObject
{
    protected class Unit
    {
        public GameObject scene;
        public Transform background;
        public Transform foreground;
        public Transform interactground;
        public KHSceneInfo sceneInfo;

        public void Init()
        {
            if (this.background == null || this.interactground == null || this.foreground == null)
            {
                throw new Exception("场景资源的层有问题");
            }
            else
            {
                this.background.localPosition = new Vector3(-0.15f, 0, 0);//KHSceneUtil.BackgroundPosition;
                this.interactground.position = KHSceneUtil.InteractgroundPosition;
                this.foreground.position = KHSceneUtil.ForegroundPosition;
            }
        }
    }

    protected Unit Logic;
    protected Unit CurrActiveScene;
    public GameObject CurrActiveSceneGO { get { return CurrActiveScene.scene; } }

    public KHSceneInfo CurrActiveSceneInfo { get { return CurrActiveScene.sceneInfo; } }

    List<GameObject> needHideLayers = new List<GameObject>();
    Dictionary<GameObject, Unit> sceneDic = new Dictionary<GameObject, Unit>();
    List<Unit> sceneUnits = new List<Unit>();

    public void AddScene(GameObject sceneGo, bool isLogic = false)
    {
        Unit unit = _Add(sceneGo);
        if (isLogic)
        {
            Logic = unit;
        }
    }

    protected Unit _Add(GameObject sceneGo)
    {
        if (sceneGo == null) return null;


        Unit unit = null;
        sceneDic.TryGetValue(sceneGo, out unit);
        if (unit == null)
        {
            unit = new Unit();
            unit.background = sceneGo.transform.Find(KHSceneLayerDef.Background);
            unit.interactground = sceneGo.transform.Find(KHSceneLayerDef.Interactground);
            unit.foreground = sceneGo.transform.Find(KHSceneLayerDef.Foreground);
            unit.scene = sceneGo;
            unit.sceneInfo = sceneGo.GetComponent<KHSceneInfo>();
            sceneDic.Add(sceneGo, unit);

            sceneUnits.Add(unit);

            AddNeedHideLayer(unit.interactground.gameObject);
        }
        return unit;
    }

    public void SetCurrScene(GameObject sceneGo)
    {
        CurrActiveScene = _Add(sceneGo);
        //
        for (int i = 0;i<sceneUnits.Count;++i)
        {
            UIAPI.SetVisible(sceneUnits[i].scene, (sceneUnits[i] == CurrActiveScene) || (sceneUnits[i] == Logic));
        }
    }

    public void InitScene()
    {
        for (int i = 0; i < sceneUnits.Count; ++i)
        {
            sceneUnits[i].Init();
        }
    }

    void AddNeedHideLayer(GameObject layer)
    {
        if (layer == null) return;
        if (!needHideLayers.Contains(layer))
            needHideLayers.Add(layer);
    }

    //@warning : 仅检查needHideLayer, 兼容之前的逻辑用
    public bool activeSelf // 仅检查needHideLayer, 兼容之前的逻辑用
    {
        get
        {
            for (int i=0;i<needHideLayers.Count;++i)
            {// 有一个true, 则返回true
                if (needHideLayers[i] != null && needHideLayers[i].activeSelf) return true;
            }
            return false;
        }
    }

    //@warning : 仅检查needHideLayer, 兼容之前的逻辑用
    public void SetActive(bool y)   // 仅检查needHideLayer, 兼容之前的逻辑用
    {
        for (int i = 0; i < needHideLayers.Count; ++i)
        {
            if (needHideLayers[i] != null)
            {
                NGUITools.SetActive(needHideLayers[i], y);
            }
        }
    }

    public void UpdateFollowWithCamera()
    {
        for (int i = 0; i < sceneUnits.Count; ++i)
        {
            if (sceneUnits[i].scene != null)
            {
                sceneUnits[i].scene.transform.localPosition = MainUICamera.getInstance().position * -1.0f;
            }
        }
    }


}

public class UIMainScene {

    public const string EVT_REQ_SCENE_CHANGE_CHECK = "EVT_REQ_SCENECHANGED_CHECK";
    public const string ON_OPEN_DONE = "OnOpenAnimDone";
    public const string ON_ClOSE = "OnCloseWindow";
    public const float REFRESH_TIME = 0.9f;

    static private UIMainScene _Instance;
    static public UIMainScene getInstance()
    {
        if (_Instance == null)
        {
            _Instance = new UIMainScene();
            //_Instance._initialize();
        }

        return _Instance;
    }

    /// <summary>
    /// 容错相关
    /// </summary>
    private float stayTimeSpan;
    private float stayIntervel = 3.0f;


    //private GameObject _gameObject; // 场景 GameObject
    //private GameObject _needHideObj; // 可隐藏的场景 GameObject
    private SceneUIObject _needHideObj;
    //private MainUICamera _camera; // 镜头
    //private KHSceneInfo _sceneInfo;
    //private Hashtable IgnoreWindow = new Hashtable();
    private Hashtable IgnoreLayer = new Hashtable();
    //private Hashtable NeedHideFreeWindow = new Hashtable();
    private KHEventDispatcher _dispatcher;

    public GameObject gameObject
    {
        get 
        {
            if (_needHideObj != null)
            {
                return _needHideObj.CurrActiveSceneGO; 
            }
            else
            {
                return null;
            }
        }
    }

    //public MainUICamera camera
    //{
    //    get { return _camera; }
    //}

    public KHSceneInfo sceneInfo
    {
        get
        {
            if (_needHideObj != null)
            {
                return _needHideObj.CurrActiveSceneInfo;
            }
            else
            {
                return null;
            }
        }
    }

    private Rectangle _openAreaRect;
    public Rectangle openAreaRect
    {
        get { return _openAreaRect; }
    }

    public KHEventDispatcher dispatcher
    {
        get
        {
            if (_dispatcher == null)
            {
                _dispatcher = new KHEventDispatcher(this);
            }
            return _dispatcher;
        }
    }

    private bool isWaitingForChState;
    private float curCountDown = 0.0f;

    public void ClearUIRef()
    {
        _needHideObj = null;
    }
    public void _initialize(SceneUIObject _needhideObj)
    {
        //_camera = MainUICamera.getInstance();
        //_gameObject = _obj;
        _needHideObj = _needhideObj;
        _needHideObj.SetActive(true);
        _openAreaRect = null;
        buildByResID();
        MainUICamera.getInstance().initialize(this);
        //InitAnimIgnoreWindow();
        InitIgnoreLayer();
        //InitNeedHideFreeWindow();
        //KHUIManager.getInstance().Dispatcher.addEventListener(UIMainScene.ON_OPEN_DONE, OnRecieveOpen);
        KHUIManager.getInstance().Dispatcher.addEventListener(UIMainScene.ON_ClOSE, OnRecieveClose);
        KHUIManager.getInstance().Dispatcher.addEventListener(UIEventDef.ON_PLAY_ClOSE_ANIM, OnRecieveClose);
        stayTimeSpan = Time.realtimeSinceStartup + stayIntervel;
    }

    // 重设当前scene
    [System.Obsolete("unseemly design -williamtyma")]
    public void _ResetActiveScene(GameObject sceneGO)
    {
        _needHideObj.SetCurrScene(sceneGO);
    }

    ///// <summary>
    ///// 不需要隐藏主界面且Layer为Fullwindow的界面
    ///// </summary>
    //void InitAnimIgnoreWindow()
    //{
    //    IgnoreWindow.Clear();
    //    IgnoreWindow.Add(UIDef.LOADING_TIP_UI, true);
    //    IgnoreWindow.Add(UIDef.LEVEL_SELECTBG_UI, true);
    //    IgnoreWindow.Add(UIDef.MSG_BOX, true);
    //    IgnoreWindow.Add(UIDef.MSG_TIP, true);
    //    IgnoreWindow.Add(UIDef.FLY_NEXT, true);
    //    IgnoreWindow.Add(UIDef.DRAW_CARD_MAIN_VIEW, true);
    //    IgnoreWindow.Add(UIDef.LOADING_UI, true);
    //    IgnoreWindow.Add(UIDef.LOGINGIFT_VIEW, true);
    //    IgnoreWindow.Add(UIDef.UI_SETTING_VIEW, true);
    //    IgnoreWindow.Add(UIDef.PlayerInfo_View, true);
    //    IgnoreWindow.Add(UIDef.LevelUp_View, true);
    //    IgnoreWindow.Add(UIDef.SHOP_UI, true);
    //    IgnoreWindow.Add(UIDef.MAIL_MAIN_VIEW, true);
    //    IgnoreWindow.Add(UIDef.TASK_VIEW, true);
    //    IgnoreWindow.Add(UIDef.MODAL_VIEW, true);
    //    IgnoreWindow.Add(UIDef.COPPER_VIEW, true);
    //    IgnoreWindow.Add(UIDef.FULL_FOOD_MAIN_PANEL_VIEW, true);
    //}

    /// <summary>
    /// 初始化需要忽略的UI Layer
    /// </summary>
    void InitIgnoreLayer()
    {
        IgnoreLayer.Clear();
        IgnoreLayer.Add(WindowLayer.MessageBoxWindow, true);
        IgnoreLayer.Add(WindowLayer.LoadingTipView, true);
        IgnoreLayer.Add(WindowLayer.TopMessageBox, true);
        IgnoreLayer.Add(WindowLayer.BGWindow, true);
        IgnoreLayer.Add(WindowLayer.UIEffect, true);
        IgnoreLayer.Add(WindowLayer.NewUserGuide, true);
        //IgnoreLayer.Add(WindowLayer.FreeWindow,true);
    }


    ///// <summary>
    ///// 需要隐藏主界面的Layer为Freewindow的界面
    ///// </summary>
    //void InitNeedHideFreeWindow()
    //{
    //    NeedHideFreeWindow.Clear();
    //    NeedHideFreeWindow.Add(UIDef.LEVEL_SELECT_DUNGEON, true);
    //    NeedHideFreeWindow.Add(UIDef.LEVEL_SELECT_DETAIL, true);
    //    NeedHideFreeWindow.Add(UIDef.ELITE_LEVEL_DETAIL_VIEW, true);
    //}

    /// <summary>
    /// 监听弹出动画结束事件
    /// </summary>
    /// <param name="e"></param>
    //void OnRecieveOpen(KHEvent e)
    //{
    //    string _name = e.data.ToString();
    //    if (!IgnoreWindow.Contains(_name))
    //    {
    //        RefreshShowState(false, _name);
    //    }
    //}

    /// <summary>
    /// 监听关闭窗口事件
    /// </summary>
    /// <param name="e"></param>
    void OnRecieveClose(KHEvent e)
    {
        isWaitingForChState = false;
        curCountDown = REFRESH_TIME;
        if (_needHideObj == null)
        {
            return;
        }

        if (_needHideObj.activeSelf || e.data == null)
        {
            return;
        }
        string _name = e.data.ToString();
        int layer = KHUIManager.getInstance().GetWindowLayer(_name);
        UIWindow win = KHUIManager.getInstance().FindWindow<UIWindow>(_name);
        if (win != null )
        {
            bool freeNeedHide = win.FreeLayerHideMainScene;
            bool fullNeedShow = win.FullLayerShowMainScene;
            if (!IgnoreLayer.Contains(layer) && ((layer == WindowLayer.FullScreenWindow && !fullNeedShow) || (layer == WindowLayer.FreeWindow && freeNeedHide)))
            {
                RefreshShowState(true, _name);
            }
        }
       
    }



   public void RefreshShowState(bool _showFlag, string _winName = "")
    {
        if (_needHideObj == null)
        {
            return;
        }

        _needHideObj.SetActive(_showFlag);

        KHUIManager.getInstance().SendMessage(UIDef.MAIN_BAR_VIEW, "UpdateContainerVisible", _showFlag);

        if (false == _showFlag)
        {
            MainUIModel tMainUiMode = KHPluginManager.Instance.GetModel("MainUI") as MainUIModel;
            tMainUiMode.Dispatcher.dispatchEvent(new KHEvent(EVT_REQ_SCENE_CHANGE_CHECK));
        }
        else
        {
            //KHUIManager.ResetCameraBoundForDeviceWhileWndChanged(UIDef.MAIN_PAGE_VIEW);
        }
    }

    public void Update()
    {
        if (_needHideObj != null)
        {
            _needHideObj.UpdateFollowWithCamera();
        }
        CheckVisibleState();
    }

    public void OnDisable()
    {
        KHUIManager manager = KHUIManager.getInstance();
        if (manager != null)
        {
            manager.Dispatcher.removeEventListener(UIMainScene.ON_ClOSE, OnRecieveClose);
            manager.Dispatcher.removeEventListener(UIEventDef.ON_PLAY_ClOSE_ANIM, OnRecieveClose);
        }
        MainUICamera.getInstance().UnBuild();
    }

    public void CheckVisibleState()
    {
        if (_needHideObj != null)
        {
            UIWindow topWin = KHUIManager.getInstance().TopWindow;
            if (topWin != null)
            {
                if (topWin.name != UIDef.MAIN_PAGE_VIEW)
                {
                    string topName = KHUIManager.getInstance().TopWindow.name;
                    int layer = KHUIManager.getInstance().GetWindowLayer(topName);
                    bool freeNeedHide = topWin.FreeLayerHideMainScene;
                    bool fullNeedShow = topWin.FullLayerShowMainScene;

                    ///全屏界面以及非全屏但是标记成需要隐藏的才会去更新显示状态
                    if (_needHideObj.activeSelf
                        && !IgnoreLayer.ContainsKey(layer)
                        && ((layer == WindowLayer.FullScreenWindow && !fullNeedShow)
                            || (layer == WindowLayer.FreeWindow && freeNeedHide)))
                    {
                        //准备隐藏
                        if (!isWaitingForChState)
                        {
                            isWaitingForChState = true;
                            curCountDown = REFRESH_TIME;
                        }
                        curCountDown -= Time.deltaTime;
                        if (curCountDown <= 0.0f)
                        {
                            RefreshShowState(false);
                        }
                    }
                    stayTimeSpan = Time.realtimeSinceStartup + stayIntervel;
                }
                else
                {
                    if (!_needHideObj.activeSelf)
                    {
                        if (stayTimeSpan <= Time.realtimeSinceStartup)
                        {
                            //保护机制
                            RefreshShowState(true);
                            stayTimeSpan = Time.realtimeSinceStartup + stayIntervel;
                        }
                    }
                }
            }
            
        }
    }

    public void buildByResID()
    {
        _needHideObj.InitScene();
        this.dispatcher.dispatchEvent(new KHEvent(KHEvent.COMPLETE));
    }

    public bool GetVisibleState()
    {
        if (_needHideObj != null)
        {
            return _needHideObj.activeSelf;
        }
        return false;
    }
}
