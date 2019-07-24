using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using KH.Plugins;
using KH.Remote;
using kihan_general_table;
using KH.Ninja;
using KH.Lua;

namespace KH
{
    public class UIMainPage : UIWindow
    {
        public const string UIActorPrefabPath = "MainpageActor/";

        public Transform SceneRoot;

        [System.Obsolete("removed, do not ref it")]
        public GameObject sceneObj;
        [System.Obsolete("removed, do not ref it")]
        public GameObject sceneObjX2;

        public GameObject sceneLogic;

        [HideInInspector]
        [System.Obsolete("removed, instead of FlareObj")]
        public GameObject flareObj;

        public GameObject FlareObj
        {
            get 
            {
                GameObject tRet = null;
                KHSceneInfo tSceneInf = UIMainScene.getInstance().sceneInfo;
                if (tSceneInf as KHMainSceneInfo != null)
                {
                    tRet = ((KHMainSceneInfo)tSceneInf).FlareObject_;
                }
                return tRet;
            }
        }

        //public GameObject needHideObj;
        private PlayerEntity playerEntity;

        [HideInInspector]
        [System.Obsolete("removed, do not ref it")]
        public Transform CharacterParent;

        [System.Obsolete("removed, do not ref it")]
        [HideInInspector]
        public Transform CharacterParentX2;

        private GameObject curActorObj = null;
        private NinjaData curActorCfg = null;
        private string curActorPath = "";
        private KHSndInstance curActorSnd = null;

        private bool mIsDestroy = false;

        [HideInInspector]
        [System.Obsolete("removed, instead of CurActorSandAni")]
        public Animator curActorSndAni;

        public Animator CurActorSandAni
        {
            get 
            {
                Animator tRet = null;
                KHSceneInfo tSceneInf = UIMainScene.getInstance().sceneInfo;
                if (tSceneInf as KHMainSceneInfo != null)
                {
                    tRet = ((KHMainSceneInfo)tSceneInf).CurActorSndAni_;
                }
                return tRet;
            }
        }

        [HideInInspector]
        [System.Obsolete("removed, instead of ActorColorMask")]
        public Color DayColorMask = new Color(1, 1, 1, 1);

        [HideInInspector]
        [System.Obsolete("removed, instead of ActorColorMask")]
        public Color NightColorMask = new Color(1, 1, 1, 1);

        public Color ActorColorMask
        {
            get
            {
                Color tRet = default(Color);
                KHSceneInfo tSceneInf = UIMainScene.getInstance().sceneInfo;
                if (tSceneInf as KHMainSceneInfo != null)
                {
                    tRet = ((KHMainSceneInfo)tSceneInf).ActorSkinMask_;
                }
                return tRet;
            }
        }

        public int DebugNight = 0; // 0:不启用 1:白天 2:夜晚

        private MainUIModel mainModel;

        private List<UIEntryElement> allElements = new List<UIEntryElement>();
        private Dictionary<UIPlayerBar.BtnDestination, UIEntryElement> mainPageEntrys = new Dictionary<UIPlayerBar.BtnDestination, UIEntryElement>();

        public Transform CharacterPos
        {
            get 
            {
                Transform tRet = null;
                KHSceneInfo tSceneInf = UIMainScene.getInstance().sceneInfo;
                if (tSceneInf as KHMainSceneInfo != null)
                {
                    tRet = ((KHMainSceneInfo)tSceneInf).ActorStandParent_;
                }
                return tRet;

                //return MainUIModel.IsNight ? CharacterParentX2 : CharacterParent; 
            }
        }

#if UNITY_EDITOR
        //         void OnGUI()
        //         {
        //             if (GUI.Button(new Rect(150, 150, 200, 50), "切换白天黑夜"))
        //             {
        //                 mainModel.SetNight(!MainUIModel.IsNight);
        //                 mainModel.CheckNight(true);
        //             }
        //         }
#endif

        public override void OnInitWindow()
        {
        }

		void Awake()
		{
			UltimateKillPlugin plugin = KHPluginManager.Instance.GetPluginByName (UltimateKillPlugin.pluginName) as UltimateKillPlugin;
			UltimateKillModel model = plugin.Model as UltimateKillModel;
			model.LoginTime = RemoteModel.Instance.CurrentTime;
		}

        void Start()
        { 
            ChatManager.Inst.Init();
            GVoiceManager.Inst.Init(NetworkManager.Instance.AccountInfo.OpenId);
            //VoiceManager.Instance.CreateEngine(); // 因为要拉AuthKey, 必须得先拉了
            var chatPlugin = KHPluginManager.Instance.GetPluginByName(ChatPlugin.PluginName);
            if (chatPlugin != null)
            { // 启动拉黑名单
                chatPlugin.SendMessage("Chat.QueryBlacklist");
            }

            Plugin mainUIExtPlg = KHPluginManager.Instance.GetPluginByName("MainUIExtPlugin");
            if (mainUIExtPlg != null)
                mainUIExtPlg.SendMessage("StartUp");
        }

        void OnEnable()
        {
            //this.RefereshMainScene();
            this.DoRefereshSceneLoad();

            RegistBuildingButtonClickEvtProc();
            SaveEntryElementOnBuild();

            //初始化人物样式
            playerEntity = RemoteModel.Instance.Player;
            playerEntity.addEventListener(PlayerEntity.MainNinjaChangeEvent, UpdateActor);
            KHEvent initEvent = new KHEvent("init");
            initEvent.data = playerEntity.MainNinja;
            UpdateActor(initEvent);

            KHUIManager.getInstance().Dispatcher.addEventListener("OnOpenWindow", OnOpenOtherWindow);
            KHUIManager.getInstance().Dispatcher.addEventListener("OnCloseWindow", OnCloseOtherWindow);

            mainModel = KHPluginManager.Instance.GetModel("MainUI") as MainUIModel;
            mainModel.Dispatcher.addEventListener(UIMainScene.EVT_REQ_SCENE_CHANGE_CHECK, OnReqSceneChangeCheckRecived);


			KHSceneSettingModel sceneSettingModel = KHPluginManager.Instance.GetModel(KHSceneSettingPlugin.pluginName) as KHSceneSettingModel;
			sceneSettingModel.Dispatcher.addEventListener(KHSceneSettingModel.SELECT_SCENE_SETTING_EVT, OnRreshSceneSetting);
			sceneSettingModel.Dispatcher.addEventListener(KHSceneSettingModel.PREVIEW_SCENE_SETTING_CANCEL_EVT, OnRreshSceneSetting);

			sceneSettingModel.Dispatcher.addEventListener(KHSceneSettingModel.PREVIEW_SCENE_SETTING_EVT, OnRreshSceneSetting);
			//显示气泡;
			ShowBubble();
        }

        void RefereshMainScene()
        {
            var tMainModel = KHPluginManager.Instance.GetModel("MainUI") as MainUIModel;

            bool tSceneChanged = false;

            //找到当前是否已经存在的纹理层
            Transform tCurSceneInstTrans = null;
            for (int i = 0, max = this.SceneRoot.childCount; i < max; ++i)
            {
                Transform tTrans = this.SceneRoot.GetChild(i);
                if (tTrans.gameObject != this.sceneLogic&& !tTrans.gameObject.name.Equals("IPX_BOUND"))
                {
                    if (null == tCurSceneInstTrans)
                    {
                        tCurSceneInstTrans = tTrans;
                    }
                    else
                    {
                        Debuger.LogError("场景层级结构错误, 有多个纹理层");
                    }
                }
            }

            //要销毁的旧场景
            GameObject tOldSceneTransToDestroy = null;

            //当前场景挂着的GameObject
            GameObject tCurSceneInstGo = (tCurSceneInstTrans != null) ? (tCurSceneInstTrans.gameObject) : (null);

            //当前场景所需 (must not null.)
            string tCurSceneResPath = MainSceneLoader.Instance.CurShouldUseSceneResPath();
            if (string.IsNullOrEmpty(tCurSceneResPath))
            {
                Debuger.LogError("当前场景资源无配置");
                return;
            }

            //当前实例指向
            string tCurUsingResPath = MainSceneLoader.Instance.CurUsingResPath;

            //如果当前所需场景和MainUi上已经挂接的实例不相同
            if (tCurSceneResPath != tCurUsingResPath)
            {
                //生成新场景, 此处必定保证非空
                GameObject tScenePrefab = MainSceneLoader.Instance.CurShouldUseSceneGo();
				if (tScenePrefab)
                {
					KHResource.unLoadRes(tCurSceneResPath, null);
					KHResource.unLoadRes(tCurUsingResPath, null);
					GameObject tSceneInstance = GameObject.Instantiate(tScenePrefab) as GameObject;
                    //append
                    tSceneInstance.transform.parent = this.SceneRoot;
                    tSceneInstance.transform.localPosition = Vector3.zero;
                    tSceneInstance.transform.localScale = Vector3.one;

                    //销毁旧场景
                    tOldSceneTransToDestroy = tCurSceneInstGo;

                    tCurSceneInstGo = tSceneInstance;
                    MainSceneLoader.Instance.CurUsingResPath = tCurSceneResPath;
                    MainSceneLoader.Instance.CurUsingInstance = tSceneInstance;

                    tSceneChanged = true;

                    Debuger.Log(string.Format("新场景加载完毕_ tCurSceneResPath:{0} | tCurUsingResPath:{1}", 
                        (tCurSceneResPath != null) ? tCurSceneResPath : "null", (tCurUsingResPath != null) ? tCurUsingResPath : "null"));
                }
                else
                {
                    Debuger.LogError("未读取到场景资源资源");
                    return;
                }
            }
            else
            {
                Debuger.Log(string.Format("场景实例不变_ {0}", (tCurUsingResPath != null) ? tCurUsingResPath : "null"));
                return;
            }

            SceneUIObject tSceneWarrper = new SceneUIObject();
            tSceneWarrper.AddScene(sceneLogic, true);
            if (tCurSceneInstGo != null)
            {
                tSceneWarrper.AddScene(tCurSceneInstGo);
                tSceneWarrper.SetCurrScene(tCurSceneInstGo);
                UIMainScene.getInstance()._initialize(tSceneWarrper);
            }

            //发出场景变换事件
            if (tSceneChanged)
            {
                tMainModel.Dispatcher.dispatchEvent(new KHEvent(MainUIModel.EVT_MAINSCNENE_CHANGED));
            }

            if (tOldSceneTransToDestroy != null)
            {
                GameObject.Destroy(tOldSceneTransToDestroy.gameObject);
            }
        }

        void ShowBubble()
        {
            //丰饶的气泡
            //Bubble FullFoodBubble = new Bubble();
            //BubbleArg arg = new BubbleArg();
            //arg.content = "挑战获得大量[ffcc00]经验，铜币[-]和[ffcc00]声望";
            //arg.show_duration = KHBubbleManager.yeartime;
            //arg.start_time = (uint)RemoteModel.Instance.CurrentTime;

            //FullFoodBubble.args = new List<BubbleArg>();
            //FullFoodBubble.args.Add(arg);
            //FullFoodBubble.SysID = BubbleSystemCfg.FullFood;

            //KHBubbleManager.Instance().AddBubble(FullFoodBubble);

            { //丰饶的气泡
                UIInvokeLater.Invoke(1.0f, () => {
                    KHPluginManager.Instance.SendMessage(FullFoodPlugin.NAME, FullFoodOperation.CheckBubbleShow, null);
                });
            }

            { // 忍剧气泡
                UIInvokeLater.Invoke(1.0f, () => {
                    KHPluginManager.Instance.SendMessageForLua("DramaPlugin", "CheckBubbleShow", null);
                });
            }
        }

        private int actorId;
        private List<string> actorEffectList = new List<string>();
        private int curEffectResIndex = 0;
        /// <summary>
        /// 更新主界面的角色
        /// </summary>
        /// <param name="e"></param>
        void UpdateActor(KHEvent e)
        {
			actorId = int.Parse(e.data.ToString());
            if (!RemoteModel.Instance.NinjaCollection.TryGetNinjaData(actorId, out curActorCfg, false))
            {
                Debuger.LogWarning("更换了不存在的忍者. id="+actorId);
                return;
            }
            if (curActorObj != null)
            {
                Destroy(curActorObj);
                KHResource.unLoadRes(curActorPath, OnLoaded);

                for (int i = 0; i< actorEffectList.Count; ++i)
                {
                    KHResource.unLoadRes(string.Format("Effect/{0}", actorEffectList[i]), OnEffectLoadedCallback);
                }
            }

            if (curActorCfg == null)
            {
                Debuger.LogWarning("更换了不存在的忍者. id=" + actorId);
				return;
            }
			if(!KHVer.IsOfflineMatch)
			{
				curActorPath = UIActorPrefabPath + curActorCfg.res_id;
				KHResource.LoadRes(curActorPath, OnLoaded);
			}
            
            actorEffectList.Clear();
            curEffectResIndex = 0;
        }

        public void OnEmptyCallback(string url, Object obj, LOADSTATUS result, object extra) { }

        public void OnEffectLoadedCallback(string url, Object obj, LOADSTATUS result, object extra)
        {
            if (curActorObj != null && curActorObj.activeSelf && actorEffectList.Count > 0)
            {
                curEffectResIndex++;
                //actorEffectList.RemoveAt(0);
                if (actorEffectList.Count > curEffectResIndex)
                {
                    string path = string.Format("Effect/{0}", actorEffectList[curEffectResIndex]);
                    KHResource.LoadRes(path, OnEffectLoadedCallback);
                }
            }
        }

		public void OnLoaded(string url, Object obj, LOADSTATUS result, object extra)
		{
			if (obj != null)
			{
				curActorObj = Instantiate(obj as GameObject) as GameObject;

                MainPageActorEffectComp comp = curActorObj.GetComponent<MainPageActorEffectComp>();
                if (comp != null)
                {
                    if (comp.effectIds != null)
                    {
                        actorEffectList.AddRange(comp.effectIds);
                        string effPath = null;
                        //for (int j = 0; j < actorEffectList.Count; ++j )
                        if (actorEffectList.Count > curEffectResIndex)
                        {
                            effPath = string.Format("Effect/{0}", actorEffectList[curEffectResIndex]);
                            KHResource.LoadRes(effPath, OnEffectLoadedCallback);
                        }
                    }
                }

				//var aInfo1 = KHDataManager.CONFIG.NinjaDatas[actorId];
				UpdateActorPrefabSet(actorId, curActorCfg, CharacterPos);
			}
		}

        void UpdateActorPrefabSet(int actorId, NinjaData aInfo, Transform targetPos)
        {
            if (aInfo == null)
            {
                Debuger.LogWarning("ActorInfo is null, actorId:"+actorId);
                return;
            }
            if (curActorObj != null && targetPos != null)
            {
                curActorObj.transform.parent = targetPos;
                curActorObj.transform.localPosition = Vector3.zero;
                curActorObj.transform.localScale = Vector3.one;
                curActorObj.transform.localRotation = Quaternion.identity;
                curActorObj.layer = 5;
                SpriteRenderer sprRender = curActorObj.GetComponent<SpriteRenderer>();
                if (sprRender != null && sprRender.sprite != null)
                {
                    if (this.CurActorSandAni != null)
                    {
                        // 将喇叭定位到idle帧的高度位置
                        this.CurActorSandAni.transform.localPosition = new Vector3(-sprRender.sprite.rect.width / 2, sprRender.sprite.rect.height - 25, 0);
                    }
                }

                SpriteRenderer[] sprRenders = curActorObj.GetComponentsInChildren<SpriteRenderer>(true);
                if (sprRenders != null)
                {
                    for (int i =0;i<sprRenders.Length;++i)
                    { // 根据白天/黑夜修改角色颜色
                        if (sprRenders[i] != null)
                        {
                            sprRenders[i].color = this.ActorColorMask;
                        }
                    }
                }
                UIEventListener.Get(targetPos.gameObject).onClick = (GameObject go) =>
                {
                    Debuger.Log("点击忍者");

                    if (aInfo.audioId == null || aInfo.audioId == "0.0")
                    {
                        aInfo.audioId = "9003";
                    }
                    bool canPlay = true;
                    if (curActorSnd != null && curActorSnd.EventInst != null)
                    {
                        FMOD.Studio.PLAYBACK_STATE stat;
                        if (FMOD.RESULT.OK == curActorSnd.EventInst.getPlaybackState(out stat) && stat != FMOD.Studio.PLAYBACK_STATE.STOPPED)
                        {
                            canPlay = false;
                        }
                    }

                    if (canPlay)
                    {
                        int audioId;
                        if (int.TryParse(aInfo.audioId, out audioId))
                        {
                            curActorSnd = KHAudioManager.PlaySound(audioId);
                        }
                        if (this.CurActorSandAni != null)
                        {
//                             curActorSndAni.gameObject.SetActive(true);
//                             curActorSndAni.ResetTrigger("beginTalk");
//                             curActorSndAni.SetTrigger("beginTalk");
                        }
                    }
                };
            }
        }

        void RegistBuildingButtonClickEvtProc()
        {
            KHUIManager tUiMgr = KHUIManager.getInstance();
            if (tUiMgr != null)
            {
                KHEventDispatcher tEvtSender = tUiMgr.Dispatcher;
                tEvtSender.addEventListener(UIEntryButton.EVT_ENTRY_BUTTON_ONCLICK, OnBuildingButtonClickEvtRecived);
            }
        }

        void UnReigstBuildingButtonClickEvtProc()
        {
            KHUIManager tUiMgr = KHUIManager.getInstance();
            if (tUiMgr != null)
            {
                KHEventDispatcher tEvtSender = tUiMgr.Dispatcher;
                tEvtSender.removeEventListener(UIEntryButton.EVT_ENTRY_BUTTON_ONCLICK, OnBuildingButtonClickEvtRecived);
            }
        }

        void OnBuildingButtonClickEvtRecived(KHEvent evt)
        {
            UIPlayerBar.BtnDestination tTargetDest = UIPlayerBar.BtnDestination.None;
            if ((evt.data as UIEntryButton) != null)
            {
                tTargetDest = ((UIEntryButton)evt.data).MyDest;
            }
            this.OnClickButton(tTargetDest);
        }

        void SaveEntryElementOnBuild()
        {
#region OLD
            /*
             * Removed by williamtyma
             * 2016.10.20
             */

            // ui btn
            //UIEntryButton[] buttons = null;
            //buttons = sceneObj.GetComponentsInChildren<UIEntryButton>(true);
            //if (buttons != null)
            //{
            //    for (int i=0;i<buttons.Length;i++)
            //    {
            //        if (buttons[i] != null)
            //        {
            //            buttons[i].onClick = OnClickButton;
            //        }
            //    }
            //}
            //// ui2
            //buttons = sceneObjX2.GetComponentsInChildren<UIEntryButton>(true);
            //if (buttons != null)
            //{
            //    for (int i = 0; i < buttons.Length; i++)
            //    {
            //        if (buttons[i] != null)
            //        {
            //            buttons[i].onClick = OnClickButton;
            //        }
            //    }
            //}
#endregion


            // logic btn
            allElements = new List<UIEntryElement> ();
            mainPageEntrys.Clear();
            UIEntryElement[] elements = sceneLogic.GetComponentsInChildren<UIEntryElement>();
			allElements.AddRange (elements);
            UIEntryElement tmpElement = null;
            for (int i = 0; i < elements.Length; ++i)
            {
                tmpElement = elements[i];
                if (mainPageEntrys.ContainsKey(tmpElement.MyDest))
                {
                    throw new System.Exception("重复的按钮类型"+tmpElement.MyDest);
                }
                mainPageEntrys.Add(tmpElement.MyDest, tmpElement);
                //UIEventListener.Get(tmpElement.gameObject).onClick = OnClickEntry;
                KHBaseRedPoint redDot = tmpElement.GetComponentInChildren<KHBaseRedPoint>();
                KHRedPointManager.getInstance().AddType(tmpElement.MyDest, redDot);
            }
        }

        private bool CheckShowNextOpenSysTipIfLock(UIPlayerBar.BtnDestination btnDest)
        {
            int cfgID = LockSysHash.getConfigByBtnDestination(btnDest);
            if (cfgID != 0 && !LockSysHash.CanUnlock(cfgID))
            { // 未解锁
                UINextOpenTips.Show(cfgID);
                return true;
            }
            return false;
        }


        public void OnClickButton(UIPlayerBar.BtnDestination btnDest)
        {
			if(KHVer.IsOfflineMatch)
			{
				if(btnDest != UIPlayerBar.BtnDestination.Match && btnDest != UIPlayerBar.BtnDestination.PVPRealTime)
				{
					UIAPI.ShowMsgTip("赛事版本，该功能暂不开放");
					return;
				}
			}
            UIEntryElement ele = null;
            mainPageEntrys.TryGetValue(btnDest, out ele);
            if (ele == null)
            {
                Debuger.Log("OnClickButton, btn:"+btnDest+", not binding logic UIEntryElement");
                return;
            }
            // 新手引导通知
            if (btnDest != UIPlayerBar.BtnDestination.None)
            {
                KHUIManager.getInstance().dispatchClickToGuideEvent(ele.gameObject.tag);
            }
            // 如果未开启，则显示系统开启提示
            if (CheckShowNextOpenSysTipIfLock(btnDest)) return;

            if (DefineExt.shieldSystemLst.Contains((uint)LockSysHash.StateBit_2_Excel[LockSysHash.BtnDestination_2_StateBit[btnDest]]))
            {
                UIAPI.ShowMsgTip("客户端不是最新版本，无法参加当前玩法，请更新版本后尝试");
                return;
            }
            if (KHLowDeviceManager.GetLowDeviceShield((uint)LockSysHash.StateBit_2_Excel[LockSysHash.BtnDestination_2_StateBit[btnDest]]))
            {
                return;
            }

            if (false && !playerEntity.GetOpenInfo(LockSysHash.BtnDestination_2_StateBit[btnDest]))
            {
                int configId = LockSysHash.getConfigByBtnDestination(btnDest);
                FeatureUnlockConfig conf = null;
                GeneralTableConfig.getInstance().FeatureDefine.TryGetValue(configId, out conf);
                if (conf != null)
                {
                    UIAPI.ShowMsgTip(conf.tip);
                }
                else
                {
                    Debuger.LogWarning("未找到对应的解锁配置, Id = " + configId);
                }
            }
            switch (btnDest)
            {
                case UIPlayerBar.BtnDestination.None:
                    break;

                case UIPlayerBar.BtnDestination.FullFood:
                    KHPluginManager.Instance.SendMessage(FullFoodPlugin.NAME, "ShowView", new ShowViewArgument(UIDef.FULL_FOOD_MAIN_PANEL_VIEW));
                    break;

                case UIPlayerBar.BtnDestination.CityShop:
                    KHPluginManager.Instance.SendMessage("Shop", "ShowView", new ShowViewArgument(UIDef.SHOP_UI, true, 1u));
                    break;

                case UIPlayerBar.BtnDestination.EliteLevel:

                    KHPluginManager.Instance.SendMessage(LevelSelectPlugin.pluginName, "ShowView", new ShowViewArgument(UIDef.LEVEL_SELECT_CHAPTER, needBg: false, data: UILevelSelectMainView.LevelSelectMainViewTabEnum.Elite));
                    break;

                case UIPlayerBar.BtnDestination.Talent:
                    KHPluginManager.Instance.SendMessage("NewTalentPlugin", "OpenSystem");
                    break;

                case UIPlayerBar.BtnDestination.MissonMode:
                    UIAPI.ShowMsgTip("功能尚未开放");
                    break;

                case UIPlayerBar.BtnDestination.SurvivalChallenge:
                    //KHPluginManager.Instance.SendMessage(SurvivalChallPlugin.pluginName, SurvivalChallOperation.OperRequestEnter);
                    KHPluginManager.Instance.SendMessage("Mirage", "ShowView",
                                                 new ShowViewArgument(UIDef.MIRAGE_PRACTICE_VIEW));
                    break;

                case UIPlayerBar.BtnDestination.MirageMode:
                    KHPluginManager.Instance.SendMessage("Mirage", "Mirage.GetMirageData");
                    break;

                case UIPlayerBar.BtnDestination.NewPVPMode:
                    KHPluginManager.Instance.SendMessage(PvPUIPlugin2.PluginName, "OpenSystem");
                    break;

                case UIPlayerBar.BtnDestination.PVPRealTime:
                    //KHPluginManager.Instance.SendMessage("ArenaPlugin", "OpenHomeView");
					KHUtil.GoToPvpMainView(null,true,true);
                    break;

                case UIPlayerBar.BtnDestination.TeamPVE:
                    KHPluginManager.Instance.SendMessage("TeamPVEUI", "ShowView", new ShowViewArgument(UIDef.TEAMPVE_MAIN_VIEW));
                    break;

                case UIPlayerBar.BtnDestination.Guild:
                    KHPluginManager.Instance.SendMessage(GuildPlugin.pluginName, GuildOperation.ClickGuild, false);

                    //KHPluginManager.Instance.GetPluginByName("DramaPlugin").ShowView("UILua/Drama/DramaView", false);

                    break;

                case UIPlayerBar.BtnDestination.RankingView:
                    KHPluginManager.Instance.SendMessage(RankListPlugin.NAME, "ShowView", new ShowViewArgument(UIDef.RANK_LIST_VIEW));
                    break;

                case UIPlayerBar.BtnDestination.NinjaTask:
                    KHPluginManager.Instance.SendMessage(NinjaTaskPlugin.pluginName, NinjaTaskOperation.OpenNinjaTask);
                    break;

                case UIPlayerBar.BtnDestination.TeamGroup:
                    //KHPluginManager.Instance.SendMessage(GroupPlugin.pluginName, GroupOperation.ClickGroup);
                    KHPluginManager.Instance.SendMessage("MysticalDuplicatePlugin", "OnOpen");
                    break;
                case UIPlayerBar.BtnDestination.Match:
					if(KHVer.IsOfflineMatch)
					{
						KHUtilForLua.SendMessageForLuaSystem("OfflineMatchPlugin", "OpenFirstView", null);
					}
					else
					{
						LuaSystemPlugin sysPlugin = KHPluginManager.Instance.GetPluginByName("MatchMainEntrancePlugin") as LuaSystemPlugin;
						sysPlugin.CallLuaFunctionToOperation("kihan.match.MatchMainEntranceOperation", "OpenSystem", null);
					}
					break;
                case UIPlayerBar.BtnDestination.KizunaContest:
                    //KHPluginManager.Instance.SendMessage("KizunaContestEntrancePlugin", "TryEnter");
                    break;
			    case UIPlayerBar.BtnDestination.GuildHegemony:
                    KHPluginManager.Instance.SendMessage("GuildHegemonyPlugin", "Query");
					break;
                case UIPlayerBar.BtnDestination.ZoneFightMatch:
                    KHPluginManager.Instance.SendMessage(ZoneFightMatchPlugin.pluginName,ZoneFightMatchOperation.OpenEntranceView);
                    break;
                case UIPlayerBar.BtnDestination.Main_NinjaFight:
                    //KHPluginManager.Instance.SendMessage("MysticalDuplicatePlugin", "OnOpen");
                    KHPluginManager.Instance.SendMessage(TeamPlugin.pluginName, TeamOperation.EnterTeamScene);
                    break;
                case UIPlayerBar.BtnDestination.Drama:
                    KHUIManager.Instance.ShowFlyNext(true, () => {
                        KHPluginManager.Instance.SendMessage("MainUI", "HideView", "MainUI");
                        KHPluginManager.Instance.SendMessage("MainUI", "HideView", "PlayerBarUI");
                        KHPluginManager.Instance.GetPluginByName("DramaPlugin").ShowView("UILua/Drama/DramaView", false);
                    });
                    break;
                default:
                    break;
            }
        }


        /// <summary>
        /// 分发同步的红点通知信息
        /// </summary>
        /// <param name="_param"></param>
        public void OnSpreadNotification(UIPlayerBar.BtnDestination _param)
        {
            if (allElements != null && allElements.Count > 0)
            {
                int index = allElements.FindIndex(p => p.MyDest == _param);
                if (index != -1)
                {
                    if (allElements[index].gameObject.activeSelf)
                    {
                        allElements[index].gameObject.SendMessage("OnUpdateUI", new KHEvent("None"), SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
        }

        //void OnGUI()
        //{
        //    if (GUI.Button(new Rect(0, 100, 100, 60), " add speedRate"))
        //    {
        //        MainUICamera.getInstance().speedRate += 0.01f;
        //    }
        //    if (GUI.Button(new Rect(120, 100, 100, 60), " minus speedRate"))
        //    {
        //        MainUICamera.getInstance().speedRate -= 0.01f;
        //    }


        //    if (GUI.Button(new Rect(0, 200, 100, 60), " add destRate"))
        //    {
        //        MainUICamera.getInstance().destRate += 0.001f;
        //    }
        //    if (GUI.Button(new Rect(120, 200, 100, 60), " minus destRate"))
        //    {
        //        MainUICamera.getInstance().destRate -= 0.001f;
        //    }
        //    Color or = GUI.color;

        //    GUI.color = Color.red;

        //    GUI.Label(new Rect(220, 100, 500, 60), "SystemInfo.deviceModel = " +SystemInfo.deviceModel);
        //    GUI.Label(new Rect(220, 200, 500, 60), "SystemInfo.deviceName = " + SystemInfo.deviceName);
        //    GUI.Label(new Rect(220, 300, 500, 60), "SystemInfo.graphicsMemorySize = " + SystemInfo.graphicsMemorySize);
        //    GUI.Label(new Rect(220, 400, 500, 60), "SystemInfo.systemMemorySize = " + SystemInfo.systemMemorySize);
        //    GUI.Label(new Rect(220, 500, 500, 60), "SystemInfo.processorCount = " + SystemInfo.processorCount);

        ////    GUI.Label(new Rect(220, 200, 200, 60), "destRate = " + MainUICamera.getInstance().destRate);
        ////    GUI.Label(new Rect(220, 300, 300, 60), "resolution = " + Screen.width + " * "+Screen.height);

        //    GUI.color = or;

        //}

        void CheckFlareState(object _topWinName)
        {
            GameObject tFlareObj = this.FlareObj;
            if (tFlareObj != null)
            {
                tFlareObj.SetActive(_topWinName.ToString().Equals(UIDef.MAIN_PAGE_VIEW));
            }
        }

        void Update()
        {
            MainUICamera.getInstance().update(Time.deltaTime);
            UIMainScene.getInstance().Update();

//			if (Input.GetKeyDown (KeyCode.A)) 
//			{
//				Debuger.Log("KeyCode........................................A");
//				//UIActivityTipView.ShowTip("ABC", "12345");
//				//KHPluginManager.Instance.SendMessage(UltimateKillPlugin.pluginName, UltimateKillZhanjiAndRewardOperation.OpenZhanjiRewardView);
//				//KHPluginManager.Instance.SendMessage(UltimateKillPlugin.pluginName, UltimateKillOperation.OpenRewardView);
//
//				UIShareMainCityView.TopName = KHUIManager.getInstance ().GetTopWindow ().name;
//				KHPluginManager.Instance.SendMessage(SharePlugin.PLUGIN_NAME, "ShowView", new ShowViewArgument(UIDef.SHARE_MAINCITY_VIEW, false, null));
//				//UIActivityTipView.ShowTipV2(KHDataManager.CONFIG.GetFeatureDefineTitle1(SystemConfigDef.Match), KHDataManager.CONFIG.GetFeatureDefineDes1(SystemConfigDef.Match),"");
//				//单触发定时器：Invoke(string method, int Secondtimes) 过Secondtimes 秒后触发method 函数，
//				//重复触发InvokeRepeating(string method, int Secondtimetowake, int Secondtimetonext)每Secondtimetonext触发下一次
//			}
        }

        /// <summary>
        /// 给新手引导用的, 提前或者推后入口层级
        /// </summary>
        /// <param name="_dir"></param>
        /// _dir : true 是表示上面
        /// _param : 层级偏移量
        public void UpdateEntryLayer(UIPlayerBar.BtnDestination _dest, bool _dir, int _param)
        {
            int index = allElements.FindIndex(p => p.MyDest == _dest);
            if (index != -1)
            {
                Renderer[] renders = allElements[index].GetComponentsInChildren<Renderer>();
                for (int i = 0; i < renders.Length; ++i )
                {
                    renders[i].material.renderQueue = _dir? 3000 + _param : 3000;
                }
            }
        }

        public void OnOpenOtherWindow(KHEvent e)
        {
            if (curActorSnd != null)
            {
                KHAudioManager.RemoveSound(curActorSnd);
            }
            if (this.CurActorSandAni != null)
            {
                this.CurActorSandAni.gameObject.SetActive(false);
                this.CurActorSandAni.StopPlayback();
            }
        }

        public void OnCloseOtherWindow(KHEvent e)
        {
        }

        public override void OnCloseWindow()
        {
            base.OnCloseWindow();
			KHResource.unLoadRes(curActorPath, OnLoaded);
            for (int i = 0; i < actorEffectList.Count; ++i)
            {
                KHResource.unLoadRes(string.Format("Effect/{0}", actorEffectList[i]), OnEffectLoadedCallback);
            }

            Transform tTrans = CharacterPos;
            if (tTrans != null)
            {
                UIEventListener.Get(tTrans.gameObject).onClick = null;
            }

            if ( curActorSnd != null )
            {
                KHAudioManager.RemoveSound(curActorSnd);
            }
            if (this.CurActorSandAni != null)
            {
                this.CurActorSandAni.gameObject.SetActive(false);
                this.CurActorSandAni.StopPlayback();
            }
        }

        void OnDisable()
        {
            MainUICamera.getInstance().UnRegisterGesture();
            UIMainScene.getInstance().OnDisable();
            if (playerEntity != null)
            {
                playerEntity.removeEventListener(PlayerEntity.MainNinjaChangeEvent, UpdateActor);
            }
            KHUIManager inst = KHUIManager.getInstance();
            if (inst != null && inst.Dispatcher != null)
            {
                inst.Dispatcher.removeEventListener("OnOpenWindow", OnOpenOtherWindow);
                inst.Dispatcher.removeEventListener("OnCloseWindow", OnCloseOtherWindow);
            }

            mainModel.Dispatcher.removeEventListener(UIMainScene.EVT_REQ_SCENE_CHANGE_CHECK, OnReqSceneChangeCheckRecived);

			KHSceneSettingModel sceneSettingModel = KHPluginManager.Instance.GetModel(KHSceneSettingPlugin.pluginName) as KHSceneSettingModel;
			sceneSettingModel.Dispatcher.removeEventListener(KHSceneSettingModel.SELECT_SCENE_SETTING_EVT, OnRreshSceneSetting);
			sceneSettingModel.Dispatcher.removeEventListener(KHSceneSettingModel.PREVIEW_SCENE_SETTING_CANCEL_EVT, OnRreshSceneSetting);
			sceneSettingModel.Dispatcher.removeEventListener(KHSceneSettingModel.PREVIEW_SCENE_SETTING_EVT, OnRreshSceneSetting);

			UnReigstBuildingButtonClickEvtProc();
        }

        private void ResetActorEffects()
        {
            if (curActorObj != null)
            {
                MainPageActorEffectComp comp = curActorObj.GetComponent<MainPageActorEffectComp>();
                if (comp != null)
                {
                    comp.Reset();
                }
            }

            for (int i = 0; i < actorEffectList.Count; ++i)
            {
                KHResource.unLoadRes(string.Format("Effect/{0}", actorEffectList[i]), OnEffectLoadedCallback);
            }
            actorEffectList.Clear();
            curEffectResIndex = 0;
        }

        void OnDestroy()
        {
            mIsDestroy = true;
            MainSceneLoader.Instance.ClearUsingInf();
            UIMainScene.getInstance().ClearUIRef();
        }

		//强制刷新场景背景
		private bool forceToRefresh = false;

		private void OnRreshSceneSetting(KHEvent evt) {
			MainSceneLoader.Instance.RefreshResPath();
			forceToRefresh = true;

			this.DoRefereshSceneLoad();
			ResetActorEffects();
		}



		public void OnReqSceneChangeCheckRecived(KHEvent evt)
        {
            this.DoRefereshSceneLoad();
            ResetActorEffects();
        }

		private void DoRefereshSceneLoad()
        {
            MainSceneLoader.Instance.RefereshLoad((praScenePrefabGo, praIsDiff) =>
            {
                if ((praIsDiff) && (praScenePrefabGo != null) && !mIsDestroy)
                {
                    //如果处于隐藏状态, 则尝试切换
                    if (UIMainScene.getInstance().GetVisibleState() == false|| forceToRefresh)
                    {
                        //Debuger.Log("来切换一把");
                        this.RefereshMainScene();

                        //更新忍者皮肤设置
                        this.UpdateActorPrefabSet(actorId, curActorCfg, CharacterPos);

                        forceToRefresh = false;
                    }
                }
            });
        }

    }
}