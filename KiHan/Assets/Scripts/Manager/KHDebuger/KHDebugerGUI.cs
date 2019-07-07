using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KH.DebugerGUI;
using KH.Network;
using kihan_general_table;
using KH.Lua;
using UnityEngine;
using naruto.protocol;
using QCloud.CosApi.Api;
using QCloud.CosApi.Common;
using KH.NNClient;
using System.Threading;

namespace KH
{
    public class KHDebugerPermission
    {
        public const string Admin = KHDebugerGUIBase.Permission_Admin;
        public const string Log = "Log";
        public const string VersionSwitch = "VersionSwitch";
        public const string LanPVP = "LanPVP";
    }

    public class CommonDropParam
    {
        public int itmeID;
        public Action jumpCallback;
    }

    public class DataTestForCp
    {
        public SceneID scene_id;
        public int scene_busid;
    }

    public class OpenURLDebugInfo
    {
        public static string OPENID = "";
        public static string PARTITION = "";
        public static string GID = "";
        public static string ACCESS_TOKEN = "";
    }

    public class KHDebugerGUI : KHDebugerGUIBase
    {
        public const string BasePath = "Assets/Scripts/Manager/KHDebuger/";
        public const string UIBasePath = "Assets/Resources/";


        #region Show & Hide
        private static KHDebugerGUI ms_Instance;


        public static void Show(string permission)
        {
            if (KHResource.GetPropertyAsBool("CloseKHDebugerGUI")) return;
            if (string.IsNullOrEmpty(permission))
            {
                return;
            }

            if (ms_Instance == null)
            {
                GameObject prefab = (GameObject)KHResource.LoadResSync("KHDebugerGUI");
                if (prefab != null)
                {
                    GameObject obj = (GameObject)Instantiate(prefab);
                    ms_Instance = obj.GetComponent<KHDebugerGUI>();
                    DontDestroyOnLoad(obj);
                }
            }

            if (ms_Instance != null)
            {
                ms_Instance.EnsureLogCallback();
                (ms_Instance as KHDebugerGUIBase).Show(permission);
            }
        }

        public static void Show(string openId, string permission)
        {
            if (KHResource.GetPropertyAsBool("CloseKHDebugerGUI")) return;
            /* 主线程初始化 */
            FSPDebuger.EnableLog = false;

            //采用OpenId作为权限检查对象
            if (string.IsNullOrEmpty(openId))
            {
                return;
            }

            permission = ValidPermissionInWhiteList(permission, openId);
            if (string.IsNullOrEmpty(permission))
            {
                return;
            }

            Show(permission);
        }

        public static void Hide()
        {
            if (ms_Instance != null)
            {
                (ms_Instance as KHDebugerGUIBase).Hide();
                //Destroy(ms_Instance.gameObject);
                //ms_Instance = null;
            }
        }


        #endregion

        #region 白名单控制

        private static readonly string[] WhiteList_Auto =
        {
            //peckhuang 28577839
            "BD9AF1AE5E234F7730847F63DBA47083",
            "ozwwNj-TcD-NrL5vnM45mNQvs4is",

            //KiHan001 1487483585
            "B47560AE3C256447A23CE473509D8ACF",
            //KiHan002 927536736
            "FC786D76AE02F69D2B5F06B8C8D52C7A",
            //KiHan003 1878691349
            "36D68DCB05847059182CD65B0738E4DC",
            //KiHan004 3582159782
            "A4F4382643E44BCFDEC8CE236D2033C9",
            //KiHan005 2482835229
            "A372263E560BDB134193B26A505B53DF",

            //frankhao test帐号
            "49C4A1D002884B799F19046F020502A0",
        };

        private static readonly string[] WhiteList_Admin =
        {
            //Slicol
            "5FEF17CFD26A88F5C9349B71D3AD1DBE",
            "C4FD8FAE90332C1EEF96443415127AC4",
            "2383818E509286DDEF99183DB3E251D7",
            "2A02EC0596D89486937A411680F9071E",
            "ozwwNjxOTUPKIqEZk-hQ9pEkeTsA",
            "ozwwNj3hYrjqN9mx7Yskg3UoGj1U",

            //KevinLin
            "3403C66644A0AE91EA3DE856F4B61BFD",
            "ozwwNj40YLWeoX0USwzr3HPrnUxA",

            //peckhuang
            "9CBFB18113E158C3AA6E72D8EA814714",
            "BD9AF1AE5E234F7730847F63DBA47083",
            "ozwwNj-XybbSZLl46moOFAcE-OYU",
            "ozwwNj-TcD-NrL5vnM45mNQvs4is",

            //frankhao
            "49C4A1D002884B799F19046F020502A0",

            //york
            "E77122A45F4AA400B5339C1D6365A32C",

            //jerry
            "185932137D3DF4485434C5A7CF3E6FAD",
            "6C78BC0C08699D61BD7F2AFDF3BF254C",

            //snake
            "6BBBE7C546E538CE8669602346F75CE5",

            //yboy
            "A59B40922BEA2F4B4F3A18F8B439C74F",
            "7925A946554D1009C80619A7A2D6E64E",
            "D8D03515DACA83F2E9755F90848A5315",

            //qasim
            "3FD116AC2E3B5BCEE179CEB73CF670AC",
            "37AE4963D8C1BFA9E12B54B533803156",

            //williamtyma
            "4966AD9E0C157193739C910B1D9E8763",
            "1E19F51242458576575E7AEF99A83B24",

            //willshen
            "A278E0C7B1E9F1ECA307D7BA9565F605",
            "D4D5B4429615C7BF00E185A2D6497F52",

            //seed
            "DC14C266BB466AB269D7AB02F9184301",

            //pinkzhuang(庄创平) 11-18 19:43:45
            //安卓手Q：
            "540A519BC75AA4061D1E6E25084BF8B0",
            "957748AF3E6F4B474E9F780090DA2D72",
            "5D4C6FC005E8779B83CE836C0BC72D34",
            //ios 微信：
            "ozwwNj4p52bzY9_xctbQN9mckAjA",
            //ios手Q：
            "72CA4E46DDE37E8775AEFB023E82060D",

            //KiHan001 1487483585
            "B47560AE3C256447A23CE473509D8ACF",
            //KiHan002 927536736
            "FC786D76AE02F69D2B5F06B8C8D52C7A",
            //KiHan003 1878691349
            "36D68DCB05847059182CD65B0738E4DC",
            //KiHan004 3582159782
            "A4F4382643E44BCFDEC8CE236D2033C9",
            //KiHan005 2482835229
            "A372263E560BDB134193B26A505B53DF",
        };

        private static readonly string[] WhiteList_Log =
        {
            //hinuxmei
            "88648345772FBEA16258A9A516A7BAF2",
            "441D5671E7A2409B20436987CD594F86",

            //kennywu
            "D219A7103C95AB8B57D99247ABBF4FBC",

            //seanshen
            "D44E5E343B30B5DDD3F6001C8CF0704C",

            //jialingzhou
            "B4D6B7176008FDAD36A4CA5042A97EF5",

            //jiaweimo
            "9FE5996672DB832B7B70487C894E1A24",

            //shawnyou
            "433AAC44AB7F5039F75B80706A82A291"
        };

        private static readonly string[] WhiteList_VersionSwitch =
        {
            //Slicol
            "5FEF17CFD26A88F5C9349B71D3AD1DBE",
            "C4FD8FAE90332C1EEF96443415127AC4",
            "2383818E509286DDEF99183DB3E251D7",
            "2A02EC0596D89486937A411680F9071E",
            "ozwwNjxOTUPKIqEZk-hQ9pEkeTsA",
            "ozwwNj3hYrjqN9mx7Yskg3UoGj1U",
        };

        private static Dictionary<string, string[]> MapWhiteList;

        private static string ValidPermissionInWhiteList(string permission, string openId)
        {
            if (MapWhiteList == null)
            {
                MapWhiteList = new Dictionary<string, string[]>();
                MapWhiteList.Add(KHDebugerPermission.Admin, WhiteList_Admin);
                MapWhiteList.Add(KHDebugerPermission.Log, WhiteList_Log);
                MapWhiteList.Add(KHDebugerPermission.VersionSwitch, WhiteList_VersionSwitch);
                //TODO 新的白名单在这里增加
            }

            if (string.IsNullOrEmpty(permission))
            {
                bool valided = (Array.IndexOf(WhiteList_Admin, openId) >= 0);
                if (valided)
                {
                    return KHDebugerPermission.Admin;
                }

                permission = "";

                foreach (KeyValuePair<string, string[]> pair in MapWhiteList)
                {
                    valided = (Array.IndexOf(pair.Value, openId) >= 0);
                    if (valided)
                    {
                        if (permission == "")
                        {
                            permission = pair.Key;
                        }
                        else
                        {
                            permission = permission + "|" + pair.Key;
                        }
                    }
                }
            }
            else
            {
                bool valided = false;
                if (MapWhiteList.ContainsKey(permission))
                {
                    valided = (Array.IndexOf(MapWhiteList[permission], openId) >= 0);
                }

                if (!valided)
                {
                    permission = "";
                }
            }

            return permission;
        }

        public static bool IsAutoOpen(string openId)
        {
            return WhiteList_Auto.Contains(openId);
        }
        #endregion

        private static string _PROTECT_FLOW_TIME = "10";


        #region cos相关

        public static int APP_ID = 10045907;
        public static string SECRET_ID = "AKIDtNxPa389JUkckbPDpMZbMntCtDwWsYSU";
        public static string SECRET_KEY = "guORAFVtpZvXZLFwpmvYjQ11Y4F2K8sm";

        public static string bucketName = "kihantest"; //bucket4burning
        //static string localPath = @"D:\cos-dotnet-sdk\cos_dotnet_sdk\bin\IMG_2260.JPG";
        static string remotePathPrefix = "pvp1v1/2017/";

        #endregion

        //=========================================================================================
        private GUIStyle m_lowerLeftFontStyle;
        public UISprite Mask;
        private Reporter m_Reporter;
        //=========================================================================================

        private DateTime m_lastUploadLogTime;
        public static int UPLOAD_LOG_INTERVAL = 300;

        override protected void Awake()
        {
            base.Awake();

            ms_Instance = this;

            m_lowerLeftFontStyle = new GUIStyle();
            m_lowerLeftFontStyle.clipping = TextClipping.Clip;
            m_lowerLeftFontStyle.border = new RectOffset(0, 0, 0, 0);
            m_lowerLeftFontStyle.normal.background = null;
            m_lowerLeftFontStyle.fontSize = 12;
            m_lowerLeftFontStyle.normal.textColor = Color.white;
            m_lowerLeftFontStyle.fontStyle = FontStyle.Bold;
            m_lowerLeftFontStyle.alignment = TextAnchor.LowerLeft;
        }

        protected void Start()
        {
            m_Reporter = this.GetComponent<Reporter>();

            AddDbgGUI("通灵技", KHSettingCommand.OnGUI, KHDebugerPermission.Admin, 1000);
            AddDbgGUI("资源", KHResourceCommand.OnGUI, KHDebugerPermission.Admin, 999);
            AddDbgGUI("日志", OnGUI_LogToggle, KHDebugerPermission.Log, 998);
            AddDbgGUI("战斗", KHBattleCommand.OnGUI, KHDebugerPermission.Log, 500);
            AddDbgGUI("Lua测试", LuaTest.OnGUI, KHDebugerPermission.Admin, 201);

#if USEILRUNTIME
            // ILRuntime测试工具
            ILRuntime.ILRuntimeTest.Instance.InitDebugUI();
#endif

            // 性能测试工具
            KHPerformanceTest.Instance.InitDebugUI();

            // 练习场测试工具
            KHTrainingSceneToolsTest.Instance.InitDebugUI();

            // Wetest测试工具
            KH.WeTestDebuger.Instance.InitDebugUI();

            // GSDK测试工具
            GSDKTestManager.Instance.InitDebugUI();

            // 资源记录上报工具
            KHResourceRecorderTest.Instance.InitDebugUI();

            // 内存记录上报工具
            KHMemoryRecorderTest.Instance.InitDebugUI();

            // jerryqin测试工具
            //JerryTest.Instance.InitDebugUI();
            // SolZhang测试工具
            //SolTest.Instance.InitDebugUI();

            // 潘多拉测试工具
            PandoraTest.Instance.InitDebugUI();

            AddDbgGUI("版本工具", OnVerTool);
            AddDbgGUI("场景检测", OnSceneBugTool);
            AddDbgGUI("运营版本", OnDbgGUI_YunYing, KHDebugerPermission.Log);

            //不常用入口(折叠)在这
            AddDbgGUI("过期入口(折叠)", OnGUIObsoleteEntrance);

            //AddDbgGUI("远程上报", OnGUI_KHReport);
            AddDbgGUI("能源监控", OnPowerTool);

            //AddDbgGUI("组织争霸", OnGUIGuildHegemony);
            //AddDbgGUI("Pvp限时商店", OnGUIPvpLimitShop);
            //AddDbgGUI("小队", OnGUIOpenMysticaDuplicate);
            //AddDbgGUI("系统临时入口", OnGUITemplateEntance);

            //#if KHDebugerGUI
            //AddDbgGUI("通用房间", OnGUIKHRoom);
            //AddDbgGUI("弹幕测试", OnGUIBarrage);
            //AddDbgGUI("暗部3V3", OnGUIAnbuScroll3v3);
            //AddDbgGUI("3V3", OnGUITianDiJuanZhou);
            //AddDbgGUI("新小队激斗", OnGUITiaoZhanSai);
            //AddDbgGUI("战斗内一些开关", OnBattleNinjaTest);
            AddDbgGUI("剧情测试", _InitDebugerGUI);
            AddDbgGUI("扩展包版本号", OnShowSvrExpkgVer);
            //         AddDbgGUI("电视台测试", OnTVTest);
            //AddDbgGUI("组织拍卖测试",OnGuildAuctionTest);
            //AddDbgGUI("锦标赛入口", OnChampionshipTest);
            AddDbgGUI("leozzzhangTest", OnLeozzzhangTest);

            AddDbgGUI("bobcczhengTest", OnBobcczhengTest);

            //AddDbgGUI("3v3擂台赛", OnChallengeTest);
            AddDbgGUI("BundleLoadTest", OnBundleLoadTest);
            AddDbgGUI("ResLoadTest", OnResLoadTest);
            //AddDbgGUI("旧羁绊对战入口", OnGUIOldKizunaContestEntry);
            //AddDbgGUI("EditorSeneca操作", OnGUIAIAndGameCore);
            //#endif

            //AddDbgGUI("摇一摇", OnShakeAShake);
            AddDbgGUI("工具开关", OnToolsSwitcher);


            //AddDbgGUI("LBS 测试", OnLBSTest);
            AddDbgGUI("Game.txt配置", OnGameTxt);
            AddDbgGUI("ResLeakPrinter", OnResLeakPrinter);
            //         AddDbgGUI("跨服要塞战", OnCrossGuildWar);
            //         AddDbgGUI("新叛忍来袭", OnDebugBadNinja);
            //AddDbgGUI("暗部无差别",OnAnBuPVP);

            AddDbgGUI("Cube", OnCube);
            //AddDbgGUI("LoadImgTest", OnLoadImgTest);
            //AddDbgGUI("Zombie", OnZombie, KHDebugerPermission.Admin, 700);

            //AddDbgGUI("Battle", OnBattle, KHDebugerPermission.Admin, 300);

            AddDbgGUI("玩家时间", OnShowPlayerTime, KHDebugerPermission.Admin, 300);
            AddDbgGUI("3D招募测试工具", On3DRecruitTest, KHDebugerPermission.Admin, 300);
            AddDbgGUI("3D角色展示", Ninja3DShow);
            AddDbgGUI("黑鲨手机测试", BlackSharkTest);

            AddDbgGUI("设备模拟", OnDeviceSimulation, KHDebugerPermission.Admin);

            AddDbgGUI("内存工具V2", KHMemoryUtilV2.OnGUI, KHDebugerPermission.Admin, 150);

            AddDbgGUI("Zeyuzhang", KHMemoryUtil.OnGUI, KHDebugerPermission.Admin, 111);

            AddDbgGUI("开黑房间", RoomInvite, KHDebugerPermission.Admin, 111);

            //AddDbgGUI("PVP2.0", OnPvp20, KHDebugerPermission.Admin);

            AddDbgGUI("勇闯地牢", OnRoguelike);

            //AddDbgGUI("AI和GameCore测试", OnJerryTest, KHDebugerPermission.Admin, 500);

            AddDbgGUI("团队翻牌", MtdDrawCard);

            AddDbgGUI("视频分享", VideoShare);

            //AddDbgGUI("OpenURL测试", OnDebugOpenURL);

            //AddDbgGUI("天地战场",TiandiZhanChang);

            AddDbgGUI("百忍分享", OnHNShare);

            AddDbgGUI("巅峰对决", OnUK);

            AddDbgGUI("弱网模拟", OnWeakNetSimulate);

            AddDbgGUI("LuaProto加载统计", OnLuaProtoStats);

            KHCheckUIResEditor.Instance.InitDebugGUI();

            m_lastUploadLogTime = DateTime.Now;
        }

        void Update()
        {
#if !UNITY_STANDALONE_LINUX
            if (Debuger.EnableLogToFile)
            {
                double useTime = (DateTime.Now - m_lastUploadLogTime).TotalSeconds;
                if (useTime > UPLOAD_LOG_INTERVAL && m_IsLogUploading == false)
                {
                    m_lastUploadLogTime = DateTime.Now;
                    UploadCurrentLog();
                }
            }
#endif
            //ResLeakDetector();

        }

        private void OnGUITemplateEntance()
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("进入组织"))
            {
                KHJumpSystemHelper.DoJump(SystemConfigDef.Guild, null);
            }
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            if (GUILayout.Button("进入小队突袭"))
            {
                KHJumpSystemHelper.DoJump(SystemConfigDef.TeamPVE, null);
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (GUILayout.Button("进入符石"))
            {
                KHPluginManager.Instance.SendMessage("RunePlugin", "EnterRune");
            }
            if (GUILayout.Button("进入符石宝箱"))
            {
                KHPluginManager.Instance.SendMessage("RunePlugin", "EnterBox");
            }
            if (GUILayout.Button("进入追击晓"))
            {
                KHPluginManager.Instance.SendMessage("ChaseAkatsukiPlugin", "EnterChaseAkatsuki");
            }
            if (GUILayout.Button("进入锦标赛"))
            {
                KHPluginManager.Instance.SendMessage("ChampionshipPlugin", "EnterChampionshipView");
            }

            if (GUILayout.Button("监听进入pvp的notify"))
            {
                NetworkManager.Instance.AddMessageCallback((int)ZoneCmd.ZONE_PVP_1V1_ENTER_GAME_NTF, MatchSuccNtf);
            }

            GUILayout.EndVertical();
        }

        private void MatchSuccNtf(object message)
        {
            ZonePvp1v1EnterGameNtf ntf = message as ZonePvp1v1EnterGameNtf;

            PVPRealTimeMainUIOperation.EnterRealTimePVPByParam(new PVPEnterParam(ntf.ret_info,
                                                                        ntf.team_info_list,
                                                                        ntf.setting,
                                                                        4,
                                                                        _EndPVPBattle,
                                                                        "Callback",
                                                                        "_pvprealtime",
                                                                        PVPRTPlayMode.Normal,
                                                                        "UILua/PVPRealtimeMain/Loading/PVPAssistantLoadingView",
                                                                        UIDef.PVP_ASSISTANT_FINAL_RESULT)
            {
                useIOSReplayKit = false,
                useGuide = ntf.is_rookie == 1
            });
        }
        private void _EndPVPBattle()
        {
            KHPluginManager.Instance.GetPluginByName(PVPRealTimeMainUIPlugin.PluginName).SendMessage("QueryPVPRealTimeInfo");
        }

        public static string RoguelikeDungonID = "";
        public static string RoguelikeMonsterPlotID = "";
        public static string RoguelikeBossPlotID = "";
        void OnRoguelike()
        {
            GUILayout.BeginVertical();
            {
                //if (SGUILayout.Button("进入测试关卡"))
                //{
                //    //RequestEnterRoguelike();
                //}

                RoguelikeDungonID = KHUtil.GetString("strRoguelikeDungonID");
                RoguelikeMonsterPlotID = KHUtil.GetString("strRoguelikeMonsterPlotID");
                RoguelikeBossPlotID = KHUtil.GetString("strRoguelikeBossPlotID");

                GUILayout.Label("dungon plot id");
                string _RoguelikeDungonID = GUILayout.TextField(RoguelikeDungonID, 100);

                if (RoguelikeDungonID != _RoguelikeDungonID)
                {
                    RoguelikeDungonID = _RoguelikeDungonID;
                    KHUtil.SetString("strRoguelikeDungonID", RoguelikeDungonID);
                }

                GUILayout.Label("monster plot id");
                string _RoguelikeMonsterPlotID = GUILayout.TextField(RoguelikeMonsterPlotID, 100);
                if (RoguelikeMonsterPlotID != _RoguelikeMonsterPlotID)
                {
                    RoguelikeMonsterPlotID = _RoguelikeMonsterPlotID;
                    KHUtil.SetString("strRoguelikeMonsterPlotID", RoguelikeMonsterPlotID);
                }

                GUILayout.Label("boss plot id");
                string _RoguelikeBossPlotID = GUILayout.TextField(RoguelikeBossPlotID, 100);
                if (RoguelikeBossPlotID != _RoguelikeBossPlotID)
                {
                    RoguelikeBossPlotID = _RoguelikeBossPlotID;
                    KHUtil.SetString("strRoguelikeBossPlotID", RoguelikeBossPlotID);
                }

                if (SGUILayout.Button("打开幻之试炼"))
                {
                    KHPluginManager.Instance.GetPluginByName(RoguelikePlugin.pluginName).SendMessage("Open");
                }

                if (SGUILayout.Button("打开幻之试炼外部场景"))
                {
                    KHPluginManager.Instance.GetPluginByName(RoguelikePlugin.pluginName).SendMessage("RoguelikeOperation_EnterOuterScene");
                }
            }
            GUILayout.EndVertical();
        }

        static int tRoguelikeTestID = 400002;
        void RequestEnterRoguelike(bool reEnter = false)
        {
            KHAudioManager.PlaySound(9910);

            var mainNinjaInfo = KH.Remote.NinjaEntityCollection.GetMainNinjaEntity();

            if (mainNinjaInfo != null)
            {
                DuplicateModel model =
                    KHPluginManager.Instance.GetPluginByName(RoguelikeDuplicatePlugin.pluginName).Model as DuplicateModel;

                model.DungeonID = tRoguelikeTestID;
                tRoguelikeTestID += 1;

                KHPlayerManager.getInstance().actorToFight = (int)mainNinjaInfo.BasicInfo.id;
                KHAPCManager.getInstance().actorToFight = 0;

                if (!reEnter)
                {
                    KHBattleManager.Instance.PluginName = RoguelikeDuplicatePlugin.pluginName;
                }

                KHBattleManager.Instance.BattlePlugin.SendMessage("RequireBattle", new Action(() =>
                {
                    UIInvokeLater.Invoke(0.5f, () =>
                    {
                        RequestEnterRoguelike(true);
                    });

                }));
            }
            else
            {
                KHAudioManager.PlaySound(9901);
                UIAPI.ShowMsgTip("请选择出战忍者！");
            }
        }

        public void EnsureLogCallback()
        {
#if KHDebugerGUI
            if(m_Reporter!=null)
            {
                m_Reporter.EnsureLogCallback();
            }
#endif
        }

        override protected void OnTitleGUI(Rect rect)
        {
#if KHDebugerGUI
            if (IsExpended)
            {
                if (m_Reporter != null)
                {
                    if (GUI.Button(new Rect(WinRect.width - (BaseSize * 2 + 20) - 2, 2, BaseSize * 2 + 20, BaseSize), m_Reporter.show ? "关闭日志窗口" : "打开日志窗口"))
                    {
                        m_Reporter.show = !m_Reporter.show;
                        if (m_Reporter.show)
                        {
                            EnsureLogCallback();
                        }
                    }
                }
            }
#endif
        }

        protected override void OnCustomGUI(Rect rect)
        {

#if KHDebugerGUI
            if (m_Reporter != null)
            {
                if (m_Reporter.show)
                {
                    m_Reporter.OnGUIDraw();
                }

                string info = "FPS:" + m_Reporter.fpsText;
                GUI.Label(new Rect(0, Screen.height - 20, Screen.width, 20), info, m_lowerLeftFontStyle);
            }
#endif

        }

        protected override void OnMaskGUI(Rect rect)
        {
            if (Mask != null)
            {
                if (UICamera.currentCamera != null)
                {
                    Vector2 pos2 = new Vector2(WinRect.xMax, WinRect.yMax);
                    pos2 = GUI2ScreenPoint(pos2);

                    pos2.x = pos2.x / Screen.width;
                    pos2.y = pos2.y / Screen.height;

                    pos2 = UICamera.currentCamera.ViewportToWorldPoint(pos2);
                    Mask.transform.position = pos2;
                    Vector2 pos2Local = Mask.transform.localPosition;


                    Vector2 pos1 = new Vector2(WinRect.xMin, WinRect.yMin);
                    pos1 = GUI2ScreenPoint(pos1);

                    pos1.x = pos1.x / Screen.width;
                    pos1.y = pos1.y / Screen.height;

                    pos1 = UICamera.currentCamera.ViewportToWorldPoint(pos1);
                    Mask.transform.position = pos1;
                    Vector2 pos1Local = Mask.transform.localPosition;


                    Mask.width = (int)(pos2Local.x - pos1Local.x);
                    Mask.height = (int)(pos1Local.y - pos2Local.y);
                }
            }
        }


        private Vector2 GUI2ScreenPoint(Vector2 v)
        {
            return new Vector2(v.x, Screen.height - v.y);
        }
        private Vector2 ScreenToGUIPoint(Vector2 v)
        {
            return new Vector2(v.x, Screen.height - v.y);
        }



        //==========================================================================================
        //
        private const int DEFAULT_UPLOAD_FILE_SIZE = 500000;
        private static string m_LogUploadTips = "";
        private static string m_LogUploadCGI = "http://61.151.226.79:11111/save_log.py";//"https://101.227.153.40:8080/replay";
        private static bool m_IsLogUploading = false;
        private static string m_LogReportText = "";
        private static string m_LogUploadURL = "http://10.225.177.163/share/logStore/"; // "http://mft.oa.com/logShare/";
        private static int m_iUploadTime = 0;
        private static int m_uploadFileHeadSize = 500000;
        private static int m_uploadFileSize = 5000000;
        private static bool m_showLocalLogFiles = false;
        private static int m_showLocalLogFileCount = 10;
        private static string[] m_localLogFiles = null;

        public static int UploadFileHeadSize
        {
            get { return m_uploadFileHeadSize; }
            set { m_uploadFileHeadSize = value; }
        }

        public static int UploadFileSize
        {
            get { return m_uploadFileSize; }
            set { m_uploadFileSize = value; }
        }

        //==========================================================================================

        #region 日志开关ＧＵＩ

        private static string LogFileName
        {
            get
            {
                if (!string.IsNullOrEmpty(Debuger.LogFileName))
                {
                    return Debuger.LogFileName.Replace(".log", "-" + m_iUploadTime + ".log");
                }
                else
                {
                    return "temp-" + m_iUploadTime + ".log";
                }
            }
        }
        int bundleDownloadStatus = -3;
        string bundleId = "";
        private void OnDbgGUI_YunYing()
        {
            //if (SGUILayout.Button("还原资源到基础版本（" + KHVer.resver + "）"))
            //{
            //    VersionManager.getInstance().LoacalVersion = KHVer.resver;
            //    UnityEngine.PlayerPrefs.SetString("resver_" + KHVer.vernum, VersionManager.getInstance().LoacalVersion);
            //    KHUtil.Save();
            //}

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("输入bundle号: ");
                GUILayout.Space(5);
                bundleId = GUILayout.TextField(bundleId, 10);
                if (SGUILayout.Button("加载bundle"))
                {
                    if (!downloadFlag)
                    {
                        KHGlobalExt.StartCoroutine(DownloadBundleCo());
                    }
                }
                if (bundleDownloadStatus == -2)
                {
                    GUILayout.Label("下载完毕");
                }
                else if (bundleDownloadStatus == -1)
                {
                    GUILayout.Label("下载失败");
                }
                else if (bundleDownloadStatus == -3)
                {
                    GUILayout.Label("状态提示");
                }
                else
                {
                    GUILayout.Label("正在下载 " + bundleDownloadStatus + "%...");
                }
            }
            GUILayout.EndHorizontal();
            if (SGUILayout.Button("重登录"))
            {
                KH.KHGlobalExt.LogoutGame();
            }

        }
        bool downloadFlag = false;
        IEnumerator DownloadBundleCo()
        {
            bundleDownloadStatus = 0;
            string downloadUrl = "http://dlied5.qq.com/kihan/testbundle/";
            string downloadFileName = "";
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.WindowsEditor)
            {
                downloadFileName = "multipleadd_" + "android_" + bundleId + ".zip";
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                downloadFileName = "multipleadd_" + "ios_" + bundleId + ".zip";
            }
            if (string.IsNullOrEmpty(downloadFileName))
            {
                bundleDownloadStatus = -1;
                yield break;
            }
            downloadUrl += downloadFileName;
            downloadFlag = true;
            string resRootPath = VerUrl.AppData_Main_Path;
            Debuger.LogError(resRootPath);
            string zipFilePath = resRootPath + "TempZipFile.zip";
            string tempZipFolder = resRootPath + "TempZipFolder/";
            using (WWW www = new WWW(downloadUrl))
            {
                while (!www.isDone)
                {
                    bundleDownloadStatus = (int)(www.progress * 100);
                    yield return new WaitForSeconds(0.1f);
                }
                downloadFlag = false;
                if (www.error != null)
                {
                    bundleDownloadStatus = -1;
                    yield break;
                }
                else
                {
                    System.IO.File.WriteAllBytes(zipFilePath, www.bytes);
                }
            }
            if (System.IO.Directory.Exists(tempZipFolder))
            {
                string[] folderFiles = System.IO.Directory.GetFiles(tempZipFolder);
                for (int index = 0; index < folderFiles.Length; index++)
                {
                    //Debuger.LogError(files[index]);
                    System.IO.File.Delete(folderFiles[index]);
                }
                System.IO.Directory.Delete(tempZipFolder);
                System.IO.Directory.CreateDirectory(tempZipFolder);
            }
            ZipHelper.UnZip(zipFilePath, tempZipFolder, "", true);
            //			string resRootPath = System.IO.Directory.GetCurrentDirectory()+"\\ResTestFolder";


            if (!System.IO.Directory.Exists(resRootPath))
            {
                System.IO.Directory.CreateDirectory(resRootPath);
            }
            string configPath = resRootPath + "config/";
            string bundlePath = resRootPath + "bundle/";
            string audioPath = resRootPath + "Audio/";
            string moviePath = resRootPath + "Movie/";
            if (!System.IO.Directory.Exists(configPath))
            {
                System.IO.Directory.CreateDirectory(configPath);
            }
            if (!System.IO.Directory.Exists(bundlePath))
            {
                System.IO.Directory.CreateDirectory(bundlePath);
            }
            if (!System.IO.Directory.Exists(audioPath))
            {
                System.IO.Directory.CreateDirectory(audioPath);
            }
            if (!System.IO.Directory.Exists(moviePath))
            {
                System.IO.Directory.CreateDirectory(moviePath);
            }
            string[] files = System.IO.Directory.GetFiles(tempZipFolder);
            for (int index = 0; index < files.Length; index++)
            {
                string fileName = System.IO.Path.GetFileName(files[index]);
                if (System.IO.Path.GetExtension(files[index]) == ".xml" && System.IO.Path.GetFileNameWithoutExtension(files[index]) == "bundle")
                {
                    string destbundlefilepath = configPath + "bundle_" + VersionManager.getInstance().LoacalVersion + ".xml";
                    if (System.IO.File.Exists(destbundlefilepath))
                    {
                        System.IO.File.Delete(destbundlefilepath);
                    }
                    System.IO.File.Move(files[index], destbundlefilepath);
                }
                if (System.IO.Path.GetExtension(files[index]) == ".assetbundle")
                {
                    if (System.IO.File.Exists(bundlePath + fileName))
                    {
                        System.IO.File.Delete(bundlePath + fileName);
                    }
                    System.IO.File.Move(files[index], bundlePath + fileName);
                }
                if (System.IO.Path.GetExtension(files[index]) == ".bank")
                {
                    if (System.IO.File.Exists(audioPath + fileName))
                    {
                        System.IO.File.Delete(audioPath + fileName);
                    }
                    System.IO.File.Move(files[index], audioPath + fileName);
                }
                if (System.IO.Path.GetExtension(files[index]) == ".mp4")
                {
                    if (System.IO.File.Exists(moviePath + fileName))
                    {
                        System.IO.File.Delete(moviePath + fileName);
                    }
                    System.IO.File.Move(files[index], moviePath + fileName);
                }
            }
            bundleDownloadStatus = -2;
            ///重启游戏
            UINativeDialog.ShowNativeMessage(UINativeDialog.PriorityDef.PriorityDef2
                                             , "重启更新", "重启"
                                             , () =>
                                             {
                                                 KHGlobalExt.RebootGame();
                                             });
        }

        private bool isRichInit = false;
        private bool richTogleValue = false;
        private void OnGUI_LogToggle()
        {
            GUILayout.BeginVertical();

            Debuger.EnableLogToUnity = GUILayout.Toggle(Debuger.EnableLogToUnity, "Debuger.EnableLogToUnity");
            Debuger.EnableLogToFile = GUILayout.Toggle(Debuger.EnableLogToFile, "Debuger.EnableLogToFile");

            #region 富文本模式开关

            // 初始化从本地读取
            if (!isRichInit)
            {
                richTogleValue = Debuger.RichFormatSwitch;
                isRichInit = true;
            }

            string richName = "Debuger.RichFormatSwitch";
            if (richTogleValue != Debuger.RichFormatSwitch)
            {
                richName += "(重启生效)";
            }

            bool tmpRichTogleValue = GUILayout.Toggle(richTogleValue, richName);
            if (tmpRichTogleValue != richTogleValue)
            {
                // 点击了的时候
                richTogleValue = tmpRichTogleValue;

                PlayerPrefs.SetInt("Debuger_RichFormatSwitch", richTogleValue ? 1 : 0);
                PlayerPrefs.Save();
            }

            #endregion

            Debuger.EnableLogToMemory = GUILayout.Toggle(Debuger.EnableLogToMemory, "Debuger.EnableLogToMemory");
            Debuger.EnableLogLoop = GUILayout.Toggle(Debuger.EnableLogLoop, "Debuger.EnableLogLoop");
            Debuger.EnableTime = GUILayout.Toggle(Debuger.EnableTime, "Debuger.EnableTime");
            Debuger.EnableStack = GUILayout.Toggle(Debuger.EnableStack, "Debuger.EnableStack");

            if (Debuger.EnableLogToFile)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("特殊日志名：");
                GUILayout.Space(5);
                string tmpYourName = GUILayout.TextField(Debuger.YourName);
                // 有更改的时候，保存该值
                if (!tmpYourName.Equals(Debuger.YourName))
                {
                    Debuger.YourName = tmpYourName;
                    PlayerPrefs.SetString("Debuger_YourName", Debuger.YourName);
                    PlayerPrefs.Save();
                }
                GUILayout.EndHorizontal();

                GUILayout.Label("本地地址：");
                GUILayout.TextArea(Debuger.LogFileDir);
                GUILayout.TextField(Debuger.LogFileName);

                if (!string.IsNullOrEmpty(m_LogUploadTips))
                {
                    GUILayout.Label("Upload Tips:" + m_LogUploadTips);
                }

                if (GUILayout.Button("上传日志"))
                {
                    UploadCurrentLog(true);
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label("最大上传字节数：");
                GUILayout.Space(5);
                if (!int.TryParse(GUILayout.TextField(m_uploadFileSize.ToString()), out m_uploadFileSize))
                {
                    m_uploadFileSize = DEFAULT_UPLOAD_FILE_SIZE;
                }
                if (m_uploadFileSize < DEFAULT_UPLOAD_FILE_SIZE)
                {
                    m_uploadFileSize = DEFAULT_UPLOAD_FILE_SIZE;
                }
                GUILayout.EndHorizontal();

                GUILayout.Label("上传地址：");

                string uploadUrl = m_LogUploadURL + RemoteModel.Instance.Player.Gid;
                if (!string.IsNullOrEmpty(DefineExt.AccountInfo.OpenId)) uploadUrl += "_" + DefineExt.AccountInfo.OpenId;
                uploadUrl += "/" + LogFileName;
                GUILayout.TextArea(uploadUrl);
            }

            GUILayout.Space(5);

            if (!string.IsNullOrEmpty(m_LogReportText))
            {
                GUILayout.Label("当前日志");
                GUILayout.Label(m_LogReportText);
            }

            m_showLocalLogFiles = GUILayout.Toggle(m_showLocalLogFiles, "显示本地日志");
            if (m_showLocalLogFiles)
            {
                if (!int.TryParse(GUILayout.TextField(m_showLocalLogFileCount.ToString()), out m_showLocalLogFileCount))
                {
                    m_showLocalLogFileCount = 0;
                }

                if (m_localLogFiles == null)
                {
                    m_localLogFiles = Directory.GetFiles(Debuger.LogFileDir);
                }

                Color oldColor = GUI.color;
                Color color1 = new Color(1f, 0.7f, 0.7f, 1);
                Color color2 = new Color(0.7f, 1f, 0.7f, 1);

                // 最新的文件排在最前，当前文件不显示。
                for (int i = m_localLogFiles.Length - 2, j = 0; i >= 0 && j < m_showLocalLogFileCount; i--, j++)
                {
                    FileInfo fileInfo = new FileInfo(m_localLogFiles[i]);

                    if (j % 2 == 0)
                    {
                        GUI.color = color1;
                    }
                    else
                    {
                        GUI.color = color2;
                    }

                    GUILayout.BeginHorizontal();

                    GUILayout.Label(fileInfo.Name);
                    GUILayout.Space(5);
                    GUILayout.Label(string.Format("{0:0.00}kb", (double)fileInfo.Length / 1024));
                    GUILayout.Space(5);
                    if (GUILayout.Button("上传"))
                    {
                        UIAPI.ShowMsgTip(fileInfo.FullName);
                        UploadLog(fileInfo, true);
                    }

                    GUILayout.EndHorizontal();

                    GUILayout.Space(2);
                }

                GUI.color = oldColor;
            }

            if (GUILayout.Button("上传COV日志"))
            {
                UploadCovLog();
            }

            if (GUILayout.Button("显示或者刷新日志"))
            {
                byte[] logBytes = LoadCurrentLogFile();
                if (logBytes != null && logBytes.Length > 0)
                {
                    string logStr = Encoding.UTF8.GetString(logBytes);
                    if (logStr.Length >= 15000)
                    {
                        m_LogReportText = logStr.Substring(logStr.Length - 15000);
                    }
                    else if (m_LogReportText.Length + logStr.Length >= 15000)
                    {
                        m_LogReportText = m_LogReportText.Substring(m_LogReportText.Length - (15000 - logStr.Length));
                        m_LogReportText += logStr;
                    }
                    else
                    {
                        m_LogReportText += logStr;
                    }
                }
            }

            GUILayout.EndVertical();
        }



        private void OnGUI_KHReport()
        {
            if (GUILayout.Button("测试上报"))
            {
                KHReporter.LogRemote("Test", "Slicol|Tang", 1, 2, 3, 4.5, 6);
            }
        }

        #endregion

        //==========================================================================================
        #region 读取日志文本

        public static byte[] LoadCurrentLogFile(bool cleanLogFile = false)
        {
            string fullpath = Debuger.LogFileDir + Debuger.LogFileName;

            if (!File.Exists(fullpath))
            {
                Debuger.LogError("KHDebugerGUI", "LoadCurrentLogFile() File Is Not Exist:" + fullpath);
                return null;
            }

            try
            {
                bool isWriting = false;
                if (Debuger.LogFileWriter != null)
                {
                    isWriting = true;
                    Debuger.LogWarning("KHDebugerGUI, LoadCurrentLogFile() 取出当前日志文件内容，之后的日志将存入新的日志文件！");
                    Debuger.LogFileWriter.Flush();
                    Debuger.LogFileWriter.Close();
                    Debuger.LogFileWriter = null;
                }

                byte[] content = ReadFileContent(fullpath);

                if (isWriting)
                {
                    Debuger.LogFileWriter = new StreamWriter(fullpath, !cleanLogFile);
                }

                return content;
            }
            catch (Exception e)
            {
                Debug.LogError("KHDebugerGUI, LoadCurrentLogFile() Failed: " + e.Message + e.StackTrace);
                return null;
            }

        }

        private static byte[] ReadFileContent(string path)
        {
            //try
            //{
            //    StreamReader rd = new StreamReader(path);
            //    string content = rd.ReadToEnd();
            //    rd.Close();
            //    rd.Dispose();

            //    byte[] bytes = Encoding.UTF8.GetBytes(content);
            //    if (bytes.Length <= m_uploadFileSize)
            //    {
            //        return content;
            //    }
            //    else
            //    {
            //        string trim = content.Substring(0, m_uploadFileHeadSize) + "\n... ... ...\n" +
            //                      content.Substring(content.Length - (m_uploadFileSize - m_uploadFileHeadSize));
            //        return trim;
            //    }
            //}
            //catch (Exception e)
            //{
            //    Debuger.LogError("ReadFileContent, path=" + path + ", e=" + e.ToString());
            //}
            //return null;

            return ReadFileContent(path, m_uploadFileHeadSize, m_uploadFileSize);
        }

        private static byte[] connentBuff = Encoding.UTF8.GetBytes("\n.\n.\n.\n.\n.\n... ... ...\n.\n.\n.\n.\n.\n");
        private static byte[] ReadFileContent(string path, int head, int len)
        {
            byte[] buff = null;

            FileStream fileStream = null;

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    fileStream = new FileStream(path, FileMode.Open);
                    if (fileStream.Length <= len)
                    {
                        byte[] contentBuff = new byte[fileStream.Length];
                        fileStream.Read(contentBuff, 0, (int)fileStream.Length);
                        buff = contentBuff;
                    }
                    else
                    {
                        byte[] buffHead = new byte[head];
                        fileStream.Read(buffHead, 0, head);

                        fileStream.Seek(fileStream.Length - len, SeekOrigin.Current);

                        byte[] buffTail = new byte[len - head];
                        fileStream.Read(buffTail, 0, len - head);

                        buff = new byte[buffHead.Length + connentBuff.Length + buffTail.Length];
                        Buffer.BlockCopy(buffHead, 0, buff, 0, buffHead.Length);
                        Buffer.BlockCopy(connentBuff, 0, buff, buffHead.Length, connentBuff.Length);
                        Buffer.BlockCopy(buffTail, 0, buff, buffHead.Length + connentBuff.Length, buffTail.Length);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("ReadFileContent, path=" + path + ", e=" + e.ToString());
            }
            finally
            {
                fileStream.Close();
            }

            return buff;
        }
        #endregion

        //==========================================================================================
        #region 上传日志

        public static void UploadLog(FileInfo fileInfo, bool showTips = false)
        {
            byte[] content = ReadFileContent(fileInfo.FullName);
            if (content != null)
            {
                byte[] bytes = content;

                if (bytes != null && bytes.Length > 0)
                {
                    WWWForm form = new WWWForm();
                    form.AddField("gid", Convert.ToString(RemoteModel.Instance.Player.Gid));
                    form.AddField("openid", DefineExt.AccountInfo.OpenId);
                    form.AddField("name", fileInfo.Name);
                    form.AddBinaryData("content", bytes);

                    UploadUtil.Upload(m_LogUploadCGI, form, null, showTips);

                    Debuger.Log("UploadLog(), file=" + fileInfo.Name);
                }
            }
        }

        public static void UploadLog(string fileName, bool showTips)
        {
            byte[] content = ReadFileContent(Debuger.LogFileDir + fileName);
            if (content != null)
            {
                byte[] bytes = content;

                if (bytes != null && bytes.Length > 0)
                {
                    WWWForm form = new WWWForm();
                    form.AddField("gid", Convert.ToString(RemoteModel.Instance.Player.Gid));
                    form.AddField("openid", DefineExt.AccountInfo.OpenId);
                    form.AddField("name", fileName);
                    form.AddBinaryData("content", bytes);

                    UploadUtil.Upload(m_LogUploadCGI, form, null, showTips);

                    Debuger.Log("UploadLog(), file=" + fileName);
                }
            }
        }

        public static int MIN_UPLOAD_COS_LOG_FRAME_INDEX = 100;
        /// <summary>
        /// 把最近的本地日志上传到cos
        /// </summary>
        public static void UploadRecentLog2Cos(int pvpServerType, List<ulong> lstGids = null)
        {
            if (KHBattle._FrameIndex < MIN_UPLOAD_COS_LOG_FRAME_INDEX)
                return;

            string strGids = "";

            if (lstGids == null)
            {
                strGids = RemoteModel.Instance.Player.Gid.ToString();
            }
            else
            {
                strGids = RemoteModel.Instance.Player.Gid.ToString();

                for (int i = 0; i < lstGids.Count; i++)
                {
                    if (lstGids[i] != RemoteModel.Instance.Player.Gid)
                    {
                        strGids += ("_" + lstGids[i]);
                    }
                }
            }

            try
            {
                remotePathPrefix = "diff/" + pvpServerType + "/";

                string fullpath = string.Format("{0}{1}_{2}", Debuger.LogFileDir, strGids, Debuger.LogFileName);
                Debuger.Log("[cos][保存cos文件到本地] file path = " + fullpath);
                Debuger.OutputLogMemory(fullpath);
                Debuger.ResetLogMemory();

                WorkHandle handle = new WorkHandle((WorkHandle self) =>
                {
                    try
                    {
                        var cos = new CosCloud(APP_ID, SECRET_ID, SECRET_KEY);

                        //获取文件属性
                        string remotePath = string.Format("{0}{1}_{2}", remotePathPrefix, strGids, Debuger.LogFileName);
                        //var result = cos.GetFileStat(bucketName, remotePath);
                        //UnityEngine.Debuger.Log("[cos]GetFileStat:" + result);

                        int dotIndex = remotePath.IndexOf("T");
                        if (dotIndex != -1 && dotIndex >= 5)
                        {
                            string dateStr = remotePath.Substring(dotIndex - 5, 8);
                            string folderPath = remotePathPrefix + dateStr;
                            var foldState = cos.GetFolderStat(bucketName, folderPath);
                            UnityEngine.Debuger.Log("[cos]UploadFolder,folderPath : " + folderPath + ",  result : " + foldState);
                            if (foldState.Contains("ERROR_CMD_FILE_NOTEXIST"))
                            {
                                foldState = cos.CreateFolder(bucketName, folderPath);
                            }

                            remotePath = string.Format("{0}/{1}_{2}", folderPath, strGids, Debuger.LogFileName);
                        }
                        //string dateStr = 

                        var uploadParasDic = new Dictionary<string, string>();
                        uploadParasDic.Add(CosParameters.PARA_BIZ_ATTR, "");
                        uploadParasDic.Add(CosParameters.PARA_INSERT_ONLY, "0");
                        var result = cos.UploadFile(bucketName, remotePath, fullpath, uploadParasDic);
                        UnityEngine.Debuger.Log("[cos]UploadFile,remotePath : " + remotePath + ",  result : " + result);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debuger.LogError(e);
                    }
                });

                handle.start(null);
            }
            catch (Exception e)
            {
                UnityEngine.Debuger.LogError(e);
            }
        }

        public static void UploadRecentLog2Cos_backup(int pvpServerType, List<ulong> lstGids = null)
        {
            if (KHBattle._FrameIndex < MIN_UPLOAD_COS_LOG_FRAME_INDEX)
                return;

            byte[] bytes = LoadCurrentLogFile();

            if (bytes == null)
            {
                return;
            }

            string strGids = "";

            if (lstGids == null)
            {
                strGids = RemoteModel.Instance.Player.Gid.ToString();
            }
            else
            {
                strGids = RemoteModel.Instance.Player.Gid.ToString();

                for (int i = 0; i < lstGids.Count; i++)
                {
                    if (lstGids[i] != RemoteModel.Instance.Player.Gid)
                    {
                        strGids += ("_" + lstGids[i]);
                    }
                }
            }

            try
            {
                remotePathPrefix = "diff/" + pvpServerType + "/";

                string fullpath = string.Format("{0}{1}_{2}", Debuger.LogFileDir, strGids, Debuger.LogFileName);
                Debuger.Log("[cos][保存cos文件到本地] file path = " + fullpath);
                using (StreamWriter cosFile = new StreamWriter(fullpath))
                {
                    cosFile.Write(System.Text.Encoding.UTF8.GetString(bytes));
                    cosFile.Flush();
                    cosFile.Close();
                }

                WorkHandle handle = new WorkHandle((WorkHandle self) =>
                {
                    try
                    {
                        var cos = new CosCloud(APP_ID, SECRET_ID, SECRET_KEY);

                        //获取文件属性
                        string remotePath = string.Format("{0}{1}_{2}", remotePathPrefix, strGids, Debuger.LogFileName);
                        //var result = cos.GetFileStat(bucketName, remotePath);
                        //UnityEngine.Debuger.Log("[cos]GetFileStat:" + result);

                        int dotIndex = remotePath.IndexOf("T");
                        if (dotIndex != -1 && dotIndex >= 5)
                        {
                            string dateStr = remotePath.Substring(dotIndex - 5, 8);
                            string folderPath = remotePathPrefix + dateStr;
                            var foldState = cos.GetFolderStat(bucketName, folderPath);
                            UnityEngine.Debuger.Log("[cos]UploadFolder,folderPath : " + folderPath + ",  result : " + foldState);
                            if (foldState.Contains("ERROR_CMD_FILE_NOTEXIST"))
                            {
                                foldState = cos.CreateFolder(bucketName, folderPath);
                            }

                            remotePath = string.Format("{0}/{1}_{2}", folderPath, strGids, Debuger.LogFileName);
                        }
                        //string dateStr = 

                        var uploadParasDic = new Dictionary<string, string>();
                        uploadParasDic.Add(CosParameters.PARA_BIZ_ATTR, "");
                        uploadParasDic.Add(CosParameters.PARA_INSERT_ONLY, "0");
                        var result = cos.UploadFile(bucketName, remotePath, fullpath, uploadParasDic);
                        UnityEngine.Debuger.Log("[cos]UploadFile,remotePath : " + remotePath + ",  result : " + result);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debuger.LogError(e);
                    }
                });

                handle.start(null);
            }
            catch (Exception e)
            {
                UnityEngine.Debuger.LogError(e);
            }
        }

        public static int COS_LOG_APP_ID = APP_ID;
        public static string COS_LOG_BUCKET_NAME = bucketName;
        public static string COS_LOG_SECRET_ID = SECRET_ID;
        public static string COS_LOG_SECRET_KEY = SECRET_KEY;

        public static void UploadClientLog2Cos(string prefix, bool showTips = false)
        {
            if (m_isUploading2Cos)
            {
                if (showTips)
                {
                    UIAPI.ShowMsgTip("已经有一个日志正在上报，请稍候...");
                }

                return;
            }

            m_isShowTipCos = showTips;

            string strGid = RemoteModel.Instance.Player.Gid.ToString();
            string strOpenId = DefineExt.AccountInfo.OpenId;
            string strPlayerId = strGid + "_" + strOpenId;

            try
            {
                string fullpath = Debuger.LogFileDir + Debuger.LogFileName;

                if (!File.Exists(fullpath))
                {
                    Debuger.LogError("KHDebugerGUI", "UploadClientLog2Cos() File Is Not Exist:" + fullpath);
                }
                else
                {
                    WorkHandle handle = new WorkHandle((WorkHandle self) =>
                    {
                        try
                        {
                            var cos = new CosCloud(COS_LOG_APP_ID, COS_LOG_SECRET_ID, COS_LOG_SECRET_KEY);

                            string remotePathFolder = "log/" + strPlayerId;
                            if (Debuger.LogFileName != null)
                            {
                                int index = Debuger.LogFileName.IndexOf("T");
                                if (index != -1)
                                {
                                    remotePathFolder = "log/" + Debuger.LogFileName.Substring(0, index) + "/" + strPlayerId;
                                }
                            }

                            //获取文件属性
                            string remotePath = string.Format("{0}/{1}", remotePathFolder, Debuger.LogFileName);

                            var foldState = cos.GetFolderStat(COS_LOG_BUCKET_NAME, remotePathFolder);
                            Debuger.Log("[cos]UploadClientLog2Cos,remotePathFolder : " + remotePathFolder + ",  result : " + foldState);
                            if (foldState.Contains("ERROR_CMD_FILE_NOTEXIST"))
                            {
                                foldState = cos.CreateFolder(COS_LOG_BUCKET_NAME, remotePathFolder);
                            }

                            if (Debuger.LogFileWriter != null)
                            {
                                Debuger.LogFileWriter.Flush();
                            }

                            System.IO.File.Copy(Debuger.LogFileDir + Debuger.LogFileName, Debuger.LogFileDir + "cos_temp_file.log", true);
                            Debuger.Log("Copy Finish");

                            var uploadParasDic = new Dictionary<string, string>();
                            uploadParasDic.Add(CosParameters.PARA_BIZ_ATTR, "");
                            uploadParasDic.Add(CosParameters.PARA_INSERT_ONLY, "0");
                            var result = cos.UploadFile(COS_LOG_BUCKET_NAME, remotePath, Debuger.LogFileDir + "cos_temp_file.log", uploadParasDic);
                            Debuger.Log("UploadFile Finish");
                        }
                        catch (Exception e)
                        {
                            UnityEngine.Debuger.LogError(e);
                        }
                    });

                    m_isUploading2Cos = true;
                    handle.start(Upload2CosComplete);

                    if (showTips)
                    {
                        UIAPI.ShowMsgTip("开始上报...");
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debuger.LogError(e);
            }
        }

        private static bool m_isUploading2Cos = false;
        private static bool m_isShowTipCos = false;
        private static void Upload2CosComplete(string err)
        {
            m_isUploading2Cos = false;
            if (m_isShowTipCos)
            {
                UIAPI.ShowMsgTip("上报完成...");
            }
        }

        public static void UploadCurrentLog(bool showTips = false)
        {
            UploadCurrentLog("", showTips);
        }

        public static void UploadCurrentLog(string prefix, bool showTips = false)
        {
#if !UNITY_EDITOR
            try
            {
                if (DefineExt.EnableUploadClientLog2Cos)
                {
                    UploadClientLog2Cos(prefix, showTips);
                }
                else
                {
                    UploadClientLog2Idc(prefix, showTips);
                }
            }
            catch (Exception e)
            {
                Debuger.LogError("KHDebugerGUI", "UploadCurrentLog() Failed: " + e.Message + e.StackTrace);
                return;
            }
#endif
        }

        private static void UploadClientLog2Idc(string prefix, bool showTips = false)
        {
            byte[] bytes = LoadCurrentLogFile();

            if (bytes != null && bytes.Length > 0)
            {
                WWWForm form = new WWWForm();
                form.AddField("gid", Convert.ToString(RemoteModel.Instance.Player.Gid));
                form.AddField("name", prefix + LogFileName);
                form.AddField("openid", DefineExt.AccountInfo.OpenId == null ? "" : DefineExt.AccountInfo.OpenId);
                form.AddBinaryData("content", bytes);

                UploadUtil.Upload(m_LogUploadCGI, form, OnUploadCurrentLog, showTips);
            }
        }

        private static void OnUploadCurrentLog(int result)
        {
            if (result == 0)
            {
                m_iUploadTime += 1;
            }
        }

        private void UploadCovLog()
        {
            RuntimeLua.Instance.CallGlobalLuaFunction("UploadCovReport");
        }

        public static void CleanLogFile()
        {
            string fullpath = Debuger.LogFileDir + Debuger.LogFileName;

            if (!File.Exists(fullpath))
            {
                Debuger.LogError("CleanLogFile File Is Not Exist:" + fullpath);
            }

            try
            {
                if (Debuger.LogFileWriter != null)
                {
                    Debuger.LogFileWriter.Flush();
                    Debuger.LogFileWriter.Close();
                    Debuger.LogFileWriter = null;
                }

                Debuger.LogFileWriter = new StreamWriter(fullpath, false);
            }
            catch (Exception e)
            {
                Debug.LogError("CleanLogFile Failed: " + e.Message + e.StackTrace);
            }
        }

        #endregion
        #region 密境
        void OnGUIOpenMysticaDuplicate()
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("进入大秘境"))
            {
                KHPluginManager.Instance.SendMessage("MysticalDuplicatePlugin", "OnOpen");
            }
            GUILayout.EndVertical();

        }
        #endregion

        #region 不常用页签（隐藏）
        void OnGUIObsoleteEntrance()
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("EditorSeneca操作"))
            {
                AddDbgGUI("过期入口(折叠)", OnGUIAIAndGameCore, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("AI和GameCore"))
            {
                AddDbgGUI("过期入口(折叠)", OnJerryTest, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("LoadImgTest"))
            {
                AddDbgGUI("过期入口(折叠)", OnLoadImgTest, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("PVP2.0"))
            {
                AddDbgGUI("过期入口(折叠)", OnPvp20, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("OpenURL测试"))
            {
                AddDbgGUI("过期入口(折叠)", OnDebugOpenURL, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("天地战场"))
            {
                AddDbgGUI("过期入口(折叠)", TiandiZhanChang, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("3D剧情测试"))
            {
                AddDbgGUI("过期入口(折叠)", On3DPlotTest, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("忍界大战UI测试"))
            {
                AddDbgGUI("过期入口(折叠)", OnNinkaiTaisenUITest, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("暗部3v3"))
            {
                AddDbgGUI("过期入口(折叠)", OnGUIAnbuScroll3v3, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("天地卷轴"))
            {
                AddDbgGUI("过期入口(折叠)", OnGUITianDiJuanZhou, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("暗部无差别"))
            {
                AddDbgGUI("过期入口(折叠)", OnAnBuPVP, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("新叛忍来袭"))
            {
                AddDbgGUI("过期入口(折叠)", OnDebugBadNinja, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("跨服要塞战"))
            {
                AddDbgGUI("过期入口(折叠)", OnCrossGuildWar, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("LBS 测试"))
            {
                AddDbgGUI("过期入口(折叠)", OnLBSTest, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("3v3擂台赛"))
            {
                AddDbgGUI("过期入口(折叠)", OnChallengeTest, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("组织拍卖测试"))
            {
                AddDbgGUI("过期入口(折叠)", OnGuildAuctionTest, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("电视台测试"))
            {
                AddDbgGUI("过期入口(折叠)", OnTVTest, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("新小队激斗"))
            {
                AddDbgGUI("过期入口(折叠)", OnGUITiaoZhanSai, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("系统临时入口"))
            {
                AddDbgGUI("过期入口(折叠)", OnGUITemplateEntance, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("组织争霸"))
            {
                AddDbgGUI("过期入口(折叠)", OnGUIGuildHegemony, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("Pvp限时商店"))
            {
                AddDbgGUI("过期入口(折叠)", OnGUIPvpLimitShop, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("小队"))
            {
                AddDbgGUI("过期入口(折叠)", OnGUIOpenMysticaDuplicate, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("Zombie"))
            {
                AddDbgGUI("过期入口(折叠)", OnZombie, KHDebugerPermission.Admin, 700, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("Battle"))
            {
                AddDbgGUI("过期入口(折叠)", OnBattle, KHDebugerPermission.Admin, 300, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("弹幕测试"))
            {
                AddDbgGUI("过期入口(折叠)", OnGUIBarrage, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("通用房间"))
            {
                AddDbgGUI("过期入口(折叠)", OnGUIKHRoom, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("摇一摇"))
            {
                AddDbgGUI("过期入口(折叠)", OnShakeAShake, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("战斗内一些开关"))
            {
                AddDbgGUI("过期入口(折叠)", OnBattleNinjaTest, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("锦标赛入口"))
            {
                AddDbgGUI("过期入口(折叠)", OnChampionshipTest, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            if (GUILayout.Button("远程上报"))
            {
                AddDbgGUI("过期入口(折叠)", OnGUI_KHReport, optionalHandler: OnGUIBackToObsoleteEntrance);
            }

            GUILayout.EndVertical();

        }
        #endregion


        void OnGUIBackToObsoleteEntrance()
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("返回"))
            {
                AddDbgGUI("过期入口(折叠)", OnGUIObsoleteEntrance);
            }
            GUILayout.EndVertical();
        }

        #region 组织争霸赛
        private void OnGUIGuildHegemony()
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("进入组织争霸赛"))
            {
                //KHPluginManager.Instance.SendMessage("GuildHegemonyPlugin", "Query");
                KHJumpSystemHelper.DoJump(SystemConfigDef.GuildHegemony, 1);
            }

            GUILayout.Space(5);

            if (GUILayout.Button("获得奖励"))
            {
                KHPluginManager.Instance.SendMessage("GuildHegemonyPlugin", "ShowHegemonyReward");
            }

            GUILayout.Space(5);

            if (GUILayout.Button("战报"))
            {
                KHPluginManager.Instance.SendMessage("GuildHegemonyPlugin", "ShowHegemonyReport");
            }

            GUILayout.Space(5);

            if (GUILayout.Button("战斗界面/需先获取组织信息"))
            {
                KHPluginManager.Instance.SendMessage("GuildHegemonyPlugin", "EnterPvpScene");
            }
            GUILayout.EndVertical();
        }
        #endregion

        #region 决斗场限时商店
        private void OnGUIPvpLimitShop()
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("进入决斗场限时商店"))
            {
                KHJumpSystemHelper.DoJump(SystemConfigDef.PvpLimitShop, 1);
            }
            GUILayout.EndVertical();
        }
        #endregion

        #region 通用房间
        public string password1 = "";
        public string password2 = "";
        public string roomid = "";

        //////////////////测试动态配置////////////////////
        public string roomName = "神奇的房间";
        public string roomBg = "bg2";
        public string tip_title = "神奇的房间";
        public string tip_content = "[ffcc00]神奇的房间[-]里有一个神奇的房间。一间一间又一间。";
        public string room_title = "zi-mijingtanxian";
        public bool RTV_enable = true;
        public bool chat_enable = true;
        public string fightCap_mode = "1";//uint
        public string mResist_mode = "1";//uint
        public string title_mode = "1";//uint
        public bool ninjaInfo_enable = true;
        public string ninjaPortrait_mode = "0";  //uint
        public string ninjaPortrait = "portrait1";
        //public bool challenge_enable = false;
        public string single_mode = "0";  //uint
        public string single_tip = "有些事情一个人是干不了的";
        public string single_fight = "既然你想自己来，我也不阻止你。";
        public bool teamInfo_enable = true;
        public string calligraphy_mode = "1"; //uint
        public string calligraphy_content = "没钱玩你麻痹";
        public string invite_mode = "1"; //uint
        public string defaultPortrait = "portrait2";
        public bool showPlayerInfo = true;
        //public string incr_Info = "none";
        //public bool is_pvp = true;
        //////////////////测试动态配置////////////////////

        private void OnGUIKHRoom()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("password: ");
            GUILayout.Space(5);
            password1 = GUILayout.TextField(password1, 8);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (GUILayout.Button("创建房间"))
            {
                RuntimeLua.Instance.CallGlobalLuaFunction("CreateRoom", new string[1] { password1 });
            }
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("roomid: ");
            GUILayout.Space(5);
            roomid = GUILayout.TextField(roomid, 8);
            //roomid = GUILayout.TextField(new Rect(100, 80, 100, 20), roomid);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("password: ");
            GUILayout.Space(5);
            password2 = GUILayout.TextField(password2, 8);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            if (GUILayout.Button("查找房间"))
            {
                RuntimeLua.Instance.CallGlobalLuaFunction("SearchEnterRoom", new string[2] { password2, roomid });
            }
            GUILayout.Space(5);

            if (GUILayout.Button("匹配"))
            {
                RuntimeLua.Instance.CallGlobalLuaFunction("MatchEnterRoom");
            }

            GUILayout.Space(5);
#if UNITY_EDITOR
            if (GUILayout.Button("加载房间表"))
            {
                DEFINE.G_Environment = ENVIRONMENT.EDITOR;
                KHDataManager.getInstance().ReLoadRoomConfig();
            }
#elif UNITY_ANDROID
            addInputComponent("房间名: ", () => { roomName = GUILayout.TextField(roomName); });
            addInputComponent("房间背景: ", () => { roomBg = GUILayout.TextField(roomBg); });
            addInputComponent("活动说明标题: ", () => { tip_title = GUILayout.TextField(tip_title); });
            addInputComponent("活动说明内容: ", () => { tip_content = GUILayout.TextField(tip_content); });
            addInputComponent("房间标题: ", () => { room_title = GUILayout.TextField(room_title); });
            addInputComponent("实时语音: ", () => { RTV_enable = GUILayout.Toggle(RTV_enable, "实时语音"); });
            addInputComponent("聊天功能: ", () => { chat_enable = GUILayout.Toggle(chat_enable, "聊天功能"); });
            addInputComponent("战斗力: ", () => { fightCap_mode = GUILayout.TextField(fightCap_mode); });
            addInputComponent("抗魔: ", () => { mResist_mode = GUILayout.TextField(mResist_mode); });
            addInputComponent("称号: ", () => { title_mode = GUILayout.TextField(title_mode); });
            addInputComponent("忍者信息: ", () => { ninjaInfo_enable = GUILayout.Toggle(ninjaInfo_enable, "忍者信息"); });
            addInputComponent("忍者立绘显示方式: ", () => { ninjaPortrait_mode = GUILayout.TextField(ninjaPortrait_mode); });
            addInputComponent("忍者立绘: ", () => { ninjaPortrait = GUILayout.TextField(ninjaPortrait); });
            addInputComponent("个人出战: ", () => { single_mode = GUILayout.TextField(single_mode); });
            addInputComponent("个人不允许提示: ", () => { single_tip = GUILayout.TextField(single_tip); });
            addInputComponent("个人出战提示: ", () => { single_fight = GUILayout.TextField(single_fight); });

            addInputComponent("队伍信息: ", () => { teamInfo_enable = GUILayout.Toggle(teamInfo_enable, "队伍信息"); });
            addInputComponent("横幅显示: ", () => { calligraphy_mode = GUILayout.TextField(calligraphy_mode); });
            addInputComponent("横幅显示内容: ", () => { calligraphy_content = GUILayout.TextField(calligraphy_content); });
            addInputComponent("邀请模式: ", () => { invite_mode = GUILayout.TextField(invite_mode); });
            addInputComponent("默认立绘: ", () => { defaultPortrait = GUILayout.TextField(defaultPortrait); });
            addInputComponent("玩家信息: ", () => { showPlayerInfo = GUILayout.Toggle(showPlayerInfo, "玩家信息"); });


            if (GUILayout.Button("加载动态配置"))
            {
                if (roomBg != "bg2" && roomBg != "bg1") { UIAPI.ShowMsgTip("只能输入bg1或bg2!"); return; }
                if (room_title != "zi_jibanduizhan" && room_title != "zi-mijingtanxian" && room_title != "zi-xiaoduijidou")
                {
                    UIAPI.ShowMsgTip("只能输入zi_jibanduizhan或zi-mijingtanxian或zi-xiaoduijidou!");
                    return;
                }
                if (ninjaPortrait != "portrait2" && ninjaPortrait != "portrait1") { UIAPI.ShowMsgTip("只能输入portrait2或portrait1!"); return; }
                if (defaultPortrait != "portrait2" && defaultPortrait != "portrait1") { UIAPI.ShowMsgTip("只能输入portrait2或portrait1!"); return; }
                uint roomType = 5;
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].room_name = roomName;
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].room_bg = roomBg;
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].tip_title = tip_title;
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].tip_content = tip_content;
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].room_title = room_title;
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].RTV_enable = RTV_enable;
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].chat_enable = chat_enable;
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].fightCap_mode = Convert.ToUInt32(fightCap_mode);
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].mResist_mode = Convert.ToUInt32(mResist_mode);
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].title_mode = Convert.ToUInt32(title_mode);
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].ninjaInfo_enable = ninjaInfo_enable;
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].ninjaPortrait_mode = Convert.ToUInt32(ninjaPortrait_mode);
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].ninjaPortrait = ninjaPortrait;
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].single_mode = Convert.ToUInt32(single_mode);
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].single_tip = single_tip;
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].single_fight = single_fight;
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].teamInfo_enable = teamInfo_enable;
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].calligraphy_mode = Convert.ToUInt32(calligraphy_mode);
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].calligraphy_content = calligraphy_content;
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].invite_mode = Convert.ToUInt32(invite_mode);
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].defaultPortrait = defaultPortrait;
                GeneralTableConfig.getInstance().RoomCfgDict[roomType].showPlayerInfo = showPlayerInfo;
            }

#endif
            GUILayout.EndVertical();
        }

        private void OnGUIBarrage()
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("打开弹幕"))
            {
                RuntimeLua.Instance.CallGlobalLuaFunction("ShowBarrage");
            }
            GUILayout.Space(5);
            if (GUILayout.Button("关闭弹幕"))
            {
                RuntimeLua.Instance.CallGlobalLuaFunction("HideBarrage");
            }
            GUILayout.EndVertical();
        }

        private void addInputComponent(string name, KHVoidFunction addfun)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name);
            GUILayout.Space(5);
            if (addfun != null) addfun();
            GUILayout.EndHorizontal();
        }
        #endregion

        #region 版本工具
        public string errcode = "";
        public int selGridInt = -1;
        public string[] selStrings = new string[] { "Default", "AndRes2", "AndRes3", "IOSRes2", "IOSRes3" };
        private string MatchNo = ""; // 匹配号
        private void OnVerTool()
        {
            GUILayout.BeginVertical();
            {
                // ----- 匹配号 -----
                GUILayout.Label(VersionUtil.ToVersionString());

                this.MatchNo = string.IsNullOrEmpty(this.MatchNo) ? VersionUtil.GetMatchNo().ToString() : this.MatchNo;
                this.MatchNo = GUILayout.TextField(this.MatchNo, 3);
                if (SGUILayout.Button("修改匹配号为：" + this.MatchNo))
                {
                    int matchNum;
                    if (int.TryParse(this.MatchNo, out matchNum))
                    {
                        UnityEngine.PlayerPrefs.SetInt(ResCheckCallBack.RES_MATCH_NO, matchNum);
                        KHUtil.Save();
                        UIAPI.ShowMsgTip("已修改匹配号：" + matchNum);
                    }
                    else
                    {
                        UIAPI.ShowMsgTip("修改失败");
                    }
                }
                GUILayout.Label("----------");
                GUILayout.Space(5);
                // ----- End 匹配号 -----

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("ErrCode: ");
                    GUILayout.Space(5);
                    errcode = GUILayout.TextField(errcode, 10);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                if (GUILayout.Button("分析"))
                {
                    IIPSMobile.IIPSMobileErrorCodeCheck errCodeCheck = new IIPSMobile.IIPSMobileErrorCodeCheck();
                    errCodeCheck.CheckIIPSErrorCode(Convert.ToInt32(errcode));
                }

                GUILayout.Space(5);

                if (selGridInt == -1)
                {
                    selGridInt = PlayerPrefs.GetInt("TversionNum", 0);
                }
                selGridInt = GUI.SelectionGrid(new Rect(20, 100, 350, 50), selGridInt, selStrings, 5);
                GUILayout.Space(20);
                if (GUILayout.Button("保存tversion设置：" + selGridInt))
                {
                    PlayerPrefs.SetInt("TversionNum", selGridInt);
                    PlayerPrefs.Save();
                }
                GUILayout.Space(5);

                if (GUILayout.Button("重新全量更新资源"))
                {
                    ReUpdateResources();
                }
                GUILayout.Space(5);

                if (GUILayout.Button("删除全部扩展包"))
                {
                    ClearAllExpandResources();
                }
                if (GUILayout.Button("删除扩展包1"))
                {
                    ClearExpandResources(1);
                }
                if (GUILayout.Button("删除扩展包2"))
                {
                    ClearExpandResources(2);
                }
                if (GUILayout.Button("删除扩展包3"))
                {
                    ClearExpandResources(3);
                }
                GUILayout.Space(5);
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// 重新全量更新资源
        /// </summary>
        private void ReUpdateResources()
        {
            // #if UNITY_ANDROID
            //             string file_extract_path = Application.persistentDataPath + "/" + KHVer.vernum;
            // #else
            //             string file_extract_path = Application.temporaryCachePath + "/" + KHVer.vernum;
            // #endif
            string file_extract_path = VerUrl.AppData_Main_Path;
            Debuger.Log(string.Format("[KHDebugerGUI] ReUpdateResources file_extract_path is {0}", file_extract_path));
            try
            {
                Directory.Delete(file_extract_path, true);
            }
            catch (Exception ex)
            {
                Debuger.LogError(string.Format("[KHDebugerGUI] ReUpdateResources Exception: {0}", ex.ToString()));
            }

            ///重启游戏
            UINativeDialog.ShowNativeMessage(UINativeDialog.PriorityDef.PriorityDef2
                                             , "重启更新", "重启"
                                             , () =>
                                             {
                                                 KHGlobalExt.RebootGame();
                                             });
        }

        private void ClearExpandResources(int id)
        {
            string file_extract_path = VerUrl.AppDataPath + "/" + id;
            Debuger.Log(string.Format("[KHDebugerGUI] ClearExpandResources file_extract_path is {0}", file_extract_path));
            try
            {
                Directory.Delete(file_extract_path, true);
            }
            catch (Exception ex)
            {
                Debuger.LogError(string.Format("[KHDebugerGUI] ClearExpandResources Exception: {0}", ex.ToString()));
            }
        }

        private void ClearAllExpandResources()
        {
            ClearExpandResources(1);
            ClearExpandResources(2);
            ClearExpandResources(3);

        }

        #endregion


        #region 场景bug检测
        public static bool switchSceneConnect = false;
        void OnSceneBugTool()
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("设置连接属性: " + (switchSceneConnect ? "打开" : "关闭")))
            {
                switchSceneConnect = !switchSceneConnect;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            if (GUILayout.Button("进入工会场景"))
            {
                KHPluginManager.Instance.SendMessage(GuildPlugin.pluginName, GuildOperation.EnterGuildScene);
            }
            GUILayout.EndVertical();
        }
        #endregion


        #region 新小队激斗
        void OnGUITiaoZhanSai()
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("进入新小队激斗邀请界面"))
            {
                KHPluginManager.Instance.SendMessage("ChallengePlugin", "OnOpen");
            }
            GUILayout.EndVertical();
        }
        #endregion

        #region 旧版本羁绊对战(2v2)入口
        void OnGUIOldKizunaContestEntry()
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("进入旧版本羁绊对战(2v2)"))
            {
                KHJumpSystemHelper.DoJump(SystemConfigDef.KizunaContest, null);
            }
            GUILayout.EndVertical();
        }
        #endregion

        string _strSourceMonsterId = "";
        string _strSourceActionId = "";
        string _strTargetActionId = "";

        #region 战斗内, AI, GameCore测试
        void OnGUIAIAndGameCore()
        {
            GUILayout.BeginVertical();
            {
                GUILayout.Label("================偷技能相关==================");

                GUILayout.Label("技能来源Actor, id (40001001)：");
                _strSourceMonsterId = GUILayout.TextArea(_strSourceMonsterId);

                GUILayout.Label("技能来源Skill, action id (37, 38, 39)：");
                _strSourceActionId = GUILayout.TextArea(_strSourceActionId);

                GUILayout.Label("要替换的技能Skill, action id (37, 38, 39)：");
                _strTargetActionId = GUILayout.TextArea(_strTargetActionId);

                if (GUILayout.Button("给技能"))
                {
                    PlayerController mainCtr = KHPlayerManager.getInstance().mainPlayer;
                    PlayerController sourceCtr = KHPlayerManager.getInstance().getPlayerByActorID(int.Parse(_strSourceMonsterId));

                    if (mainCtr != null && sourceCtr != null)
                    {
                        int sourceSkillId = sourceCtr.model.getSkillModel(int.Parse(_strSourceActionId)).skillID;
                        int targetSkillId = mainCtr.model.getSkillModel(int.Parse(_strTargetActionId)).skillID;

                        DynamicSkillRTActorHelper.CopySkillFrom(sourceCtr, mainCtr, sourceSkillId, targetSkillId);
                    }
                    else
                    {
                        UIAPI.ShowMsgTip("参数有误, 给技能失败, 请检查.");
                    }

                }

                GUILayout.Label("===================END===================");

                //if (!NNManager.Enable)
                //{
                //    if (GUILayout.Button("开启NNManager"))
                //    {
                //        NNManager.Enable = true;
                //    }
                //}
                //else
                //{
                //    if (GUILayout.Button("关闭NNManager"))
                //    {
                //        NNManager.Enable = false;
                //    }
                //}

                //if (!NNManager.EnableStatistics)
                //{
                //    if (GUILayout.Button("开启发送统计数据"))
                //    {
                //        NNManager.EnableStatistics = true;
                //    }
                //}
                //else
                //{
                //    if (GUILayout.Button("关闭发送统计数据"))
                //    {
                //        NNManager.EnableStatistics = false;
                //    }
                //}
            }
            GUILayout.EndVertical();

            GUILayout.Label("NNStatistics.TrainingInterval = " + NNStatistics.TrainingInterval);
            NNStatistics.TrainingInterval = (int)GUILayout.HorizontalSlider((float)NNStatistics.TrainingInterval, 1, 30);

            GUILayout.Space(10);
        }
        #endregion

        #region 暗部卷轴3v3
        void OnGUIAnbuScroll3v3()
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("进入暗部卷轴3v3"))
            {
                KHPluginManager.Instance.SendMessage("PVP3V3Plugin", "OnOpen", (int)RoomType.E_ROOM_TYPE_ANBU_SCROLL_3v3);
            }
            GUILayout.EndVertical();
        }
        #endregion

        #region 天地卷轴
        void OnGUITianDiJuanZhou()
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("进入卷轴赛"))
            {
                KHPluginManager.Instance.SendMessage("PVP3V3Plugin", "OnOpen", (int)RoomType.E_ROOM_TYPE_TEAM_3V3_Collect);
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (GUILayout.Button("进入天地卷轴"))
            {
                KHPluginManager.Instance.SendMessage("PVP3V3Plugin", "OnOpen", 10);
                //KHJumpSystemHelper.DoJump(SystemConfigDef.PVP3V3, null);
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            if (GUILayout.Button("创建3v3 room"))
            {
                KHPluginManager.Instance.SendMessage("PVP3V3Plugin", "CreateEnterRoom");
                //KHJumpSystemHelper.DoJump(SystemConfigDef.PVP3V3, null);
            }
            GUILayout.EndVertical();
            //CreateEnterRoom(_roomType)
            GUILayout.BeginVertical();

            if (GUILayout.Button("秘境排行榜"))
            {
                KHPluginManager.Instance.SendMessage("MysticalDuplicatePlugin", "OpenRankUI");
                //KHJumpSystemHelper.DoJump(SystemConfigDef.PVP3V3, null);
            }
            GUILayout.EndVertical();

        }
        #endregion

        #region 剧情测试
        private string strDungeonId = "502301";
        private string strPlotId = "519030";
        private string strActId = "0";
        private void _InitDebugerGUI()
        {
            strDungeonId = KHUtil.GetString("strDungeonId");
            strPlotId = KHUtil.GetString("strPlotId");
            strActId = KHUtil.GetString("strActId");

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("dungeon id：");
            GUILayout.Space(100);
            string _strDungeonId = GUILayout.TextField(strDungeonId);
            if (strDungeonId != _strDungeonId)
            {
                strDungeonId = _strDungeonId;
                KHUtil.SetString("strDungeonId", strDungeonId);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("plot id：");
            GUILayout.Space(100);
            string _strPlotId = GUILayout.TextField(strPlotId);
            if (strPlotId != _strPlotId)
            {
                strPlotId = _strPlotId;
                KHUtil.SetString("strPlotId", strPlotId);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("act id：");
            GUILayout.Space(100);
            string _strActId = GUILayout.TextField(strActId);
            if (strActId != _strActId)
            {
                strActId = _strActId;
                KHUtil.SetString("strActId", strActId);
            }
            GUILayout.EndHorizontal();

            if (SGUILayout.Button("进入副本"))
            {
                uint dungeonId = 0;
                uint plotId = 0;
                uint actId = 0;

                if (uint.TryParse(strDungeonId, out dungeonId) && uint.TryParse(strPlotId, out plotId) && uint.TryParse(strActId, out actId))
                {
                    KHPluginManager.Instance.SendMessage(TestDuplicatePlugin.pluginName, BattleOperation.RequireBattle, new TestDuplicateContext { dungeonId = dungeonId, plotId = plotId, actId = actId });
                }
                else
                {
                    UIAPI.ShowMsgTip("输入错误");
                }
            }

            GUILayout.EndVertical();
        }
        #endregion

        #region 扩展包模拟修改服务端版本号
        private ExPkgVerInfo[] SvrExPkgVerArr;
        private ExPkgVerInfo[] LocalExPkgVerArr;
        private bool isInited = false;
        private string dbgTypeStr = "";
        private GUIStyle expkg_style = new GUIStyle() { };
        private void OnShowSvrExpkgVer()
        {
            GUILayout.BeginVertical(GUILayout.MaxHeight(300));

            // 如果太早还没有初始化, 或者配置没有加载, 这里就直接返回什么都不显示
            if (KHDataManager.CONFIG == null || KHDataManager.CONFIG.dicExpandPack.Count <= 0)
            {
                return;
            }

            if (!isInited)
            { // 初始化
                isInited = true;

                SvrExPkgVerArr = new ExPkgVerInfo[KHDataManager.CONFIG.dicExpandPack.Count];
                LocalExPkgVerArr = new ExPkgVerInfo[KHDataManager.CONFIG.dicExpandPack.Count];

                for (int i = 0; i < SvrExPkgVerArr.Length; ++i)
                {
                    SvrExPkgVerArr[i] = new ExPkgVerInfo();
                }

                for (int i = 0; i < LocalExPkgVerArr.Length; ++i)
                {
                    LocalExPkgVerArr[i] = new ExPkgVerInfo();
                }

                _LoadSvrExpkgVerInfo();
                _LoadLocalExpkgVerInfo();

                dbgTypeStr = TVerCfgMgr.dbgBuildType.ToString();

                expkg_style.normal.textColor = Color.yellow;
            }

            GUILayout.Space(10);
            GUILayout.Label("*(每次要切换环境下载扩展包时, 确保在打开扩展包界面前修改, 否则就重进游戏后先进来修改)", expkg_style);
            GUILayout.Label("*(进入本页面就使用一次刷新, 以确保是最新的数据)", expkg_style);
            GUILayout.Label("*(每次修改只有当前有效, 杀进程后需要重新修改)", expkg_style);
            GUILayout.Label("服务端版本号配置修改:");
            _ShowExpkgVerInfo();
            _ShowExpkgTVerCfgInfo();

            if (GUILayout.Button("保存bundle日志"))
            {
                KHBundleLogger.Save();
            }

            GUILayout.EndVertical();
        }

        private void _LoadSvrExpkgVerInfo()
        {
            using (var itr = KHDataManager.CONFIG.dicExpandPack.GetEnumerator())
            {
                int i = 0;
                while (itr.MoveNext())
                {
                    ExpandPackInfo cfgInfo = itr.Current.Value;
                    ExPkgVerInfo info = ExpandPackManager.getSvrExPkgVerInfo(cfgInfo.id);
                    if (info == null)
                    {
                        SvrExPkgVerArr[i].ver = 0;
                        SvrExPkgVerArr[i].id = (uint)cfgInfo.id;
                    }
                    else
                    {
                        SvrExPkgVerArr[i] = new ExPkgVerInfo();
                        SvrExPkgVerArr[i].has_reward = info.has_reward;
                        SvrExPkgVerArr[i].update_type = 1;
                        SvrExPkgVerArr[i].id = (uint)cfgInfo.id;
                        SvrExPkgVerArr[i].ver = info.ver;
                    }
                    i++;
                }
            }
        }

        private void _LoadLocalExpkgVerInfo()
        {
            using (var itr = KHDataManager.CONFIG.dicExpandPack.GetEnumerator())
            {
                int i = 0;
                while (itr.MoveNext())
                {
                    ExpandPackInfo cfgInfo = itr.Current.Value;
                    LocalExPkgVerArr[i] = new ExPkgVerInfo();
                    LocalExPkgVerArr[i].has_reward = false;
                    LocalExPkgVerArr[i].update_type = 1;
                    LocalExPkgVerArr[i].id = (uint)cfgInfo.id;
                    LocalExPkgVerArr[i].ver = ExpandPackManager.getExPkgVerNum(cfgInfo.id);
                    i++;
                }
            }
        }

        private void _ShowExpkgVerInfo()
        {
            if (KHDataManager.CONFIG.dicExpandPack.Count == 0)
                return;
            using (var itr = KHDataManager.CONFIG.dicExpandPack.GetEnumerator())
            {
                int i = 0;
                while (itr.MoveNext())
                {
                    ExpandPackInfo cfgInfo = itr.Current.Value;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(cfgInfo.packname);
                    GUILayout.Label("Server:");
                    //SvrExPkgVerArr[i].has_reward = int.TryParse();
                    int reward = 0;
                    string rewardStr = GUILayout.TextField(SvrExPkgVerArr[i].has_reward ? "1" : "0");
                    if (int.TryParse(rewardStr, out reward))
                    {
                        SvrExPkgVerArr[i].has_reward = (reward == 0) ? false : true;
                    }
                    SvrExPkgVerArr[i].ver = (uint)VersionUtil.toSimpleVersion(GUILayout.TextField(VersionUtil.toStringVersion(SvrExPkgVerArr[i].ver)));
                    GUILayout.Label("/Local:");
                    GUILayout.Label(VersionUtil.toStringVersion(LocalExPkgVerArr[i].ver));
                    GUILayout.EndHorizontal();
                    i++;
                }
            }

            if (GUILayout.Button("刷新"))
            {
                _LoadSvrExpkgVerInfo();
                _LoadLocalExpkgVerInfo();
            }
            if (GUILayout.Button("提交修改"))
            {
                ExpandPackManager.exPkgVerInfo = new List<ExPkgVerInfo>(SvrExPkgVerArr);
            }
        }

        private void _ShowExpkgTVerCfgInfo()
        {
            GUILayout.Space(20);
            GUILayout.BeginVertical();

            // TODO 如果dic不存在, 比如还没登录
            TVerCfg cfg = TVerCfgMgr.getInstance().getVer(1);
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(300));
            GUILayout.Label("dbgOpen:   " + TVerCfgMgr.dbgOpen);
            if (GUILayout.Button(TVerCfgMgr.dbgOpen ? "关" : "开"))
            {
                TVerCfgMgr.dbgOpen = !TVerCfgMgr.dbgOpen;
            }
            GUILayout.EndHorizontal();
            if (TVerCfgMgr.dbgOpen)
            {
                GUILayout.Label("dbgBuildType:   " + TVerCfgMgr.dbgBuildType);
            }
            else
            {
                GUILayout.Label("isGray:   " + DefineExt.isExPkgGrayUpdate);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("(其它:Gray, 1:Pub, 2:Tst)");
            dbgTypeStr = GUILayout.TextField(dbgTypeStr);
            GUILayout.EndHorizontal();
            GUILayout.Label("TVerKey:   " + TVerCfgMgr.ToExPkgVerKey(1));
            GUILayout.Label("Url:   " + cfg.getUrlsStr());

            if (GUILayout.Button("修改dbgBuildType"))
            {
                int typeInt = 0;
                int.TryParse(dbgTypeStr, out typeInt);
                TVerCfgMgr.dbgBuildType = typeInt;
                _ReloadTVerCfg4Pkg();
            }

            GUILayout.EndVertical();
        }

        private void _ReloadTVerCfg4Pkg()
        {
            using (var itr = KHDataManager.CONFIG.dicExpandPack.GetEnumerator())
            {
                int i = 0;
                while (itr.MoveNext())
                {
                    ExpandDownLoader dl = ExpandDownloaderManager.instance.getDownLoader(itr.Current.Value.id);
                    dl.dbgReloadTVerCfg();
                    i++;
                }
            }
        }

        #endregion

        #region 电视台测试
        private string dbgTVUrl = "";
        private void OnTVTest()
        {
            GUILayout.BeginVertical();

            GUILayout.Label("输入电视地址");
            dbgTVUrl = GUILayout.TextArea(dbgTVUrl);//GUILayout.TextField(dbgTypeStr);
            if (GUILayout.Button("打开电视台"))
            {
                KiHanLivePlugin.OpenUrl(dbgTVUrl, true);
            }
            GUILayout.EndVertical();
        }
        #endregion

        #region 团队副本

        private MtdModel mtdModel = null;
        public MtdModel MTDModel
        {
            get
            {
                if (mtdModel == null)
                {
                    mtdModel = KHPluginManager.Instance.GetModel(MtdPlugin.pluginName) as MtdModel;
                }
                return mtdModel;
            }
        }

        void MtdDrawCard()
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("团队副本翻牌"))
            {
                /*if(KHGlobalExt.app.CurrentContext is KHBootContext)
                {

                }
                //KHPluginManager.Instance.GetPluginByName(MtdPlugin.pluginName).ShowView(UIDef.MTD_DRAW_CARD_UI);
                KHPluginManager.Instance.SendMessage(TeamPlugin.pluginName, TeamOperation.EnterTeamScene);*/
                UIAPI.ShowMsgTip("哈哈哈哈");
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (GUILayout.Button("道具掉落途经"))
            {
                var arg = new CommonDropParam();
                arg.itmeID = 1006;
                arg.jumpCallback = jumpCallBack;
                KHPluginManager.Instance.SendMessageForLua("ActHelperPlugin", "ShowCommonPropOriginPanel", arg);
            }
            GUILayout.EndVertical();
        }

        #region 本地视频分享
        void VideoShare()
        {
            bool exist;
            GUILayout.BeginVertical();
            if (GUILayout.Button("分享到qq空间"))
            {
                string videoPath = KHVideoManager.shareVideoPath("Movie/104013.mp4");
                if (ResPathUtil.isPathExist(videoPath))
                {
                    SnsShare.ShareVideoToQQ(videoPath, "精彩视频");
                }
                else
                {
                    UIAPI.ShowMsgTip("视频路径不存在，分享失败");
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (GUILayout.Button("分享到微信朋友圈"))
            {
                string videoPath = KHVideoManager.shareVideoPath("Movie/104013.mp4");
                if (ResPathUtil.isPathExist(videoPath))
                {
                    SnsShare.ShareVideoToWeChat(videoPath, "朋友圈分享", "分享测试");
                }
                else
                {
                    UIAPI.ShowMsgTip("视频路径不存在，分享失败");
                }
            }
            GUILayout.EndVertical();
        }
        #endregion

        #region 天地战场
        void TiandiZhanChang()
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("打开天地战场"))
            {
                KHPluginManager.Instance.SendMessage(TianDiZhanChangPlugin.pluginName, TianDiZhanChangSystemOperation.RequestTianDiEntrance);
            }
            GUILayout.EndVertical();
        }
        #endregion

        void RoomInvite()
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("邀请微信开黑"))
            {
                string title = "团队挑战邀请";
                string desc = RemoteModel.Instance.Player.PlayerName + "邀请你参加佩恩入侵";
                if (MTDModel.myTeamData != null)
                {
                    string extInfo = "1_133_" + MTDModel.myTeamData.scene_id.ToString() + "_" + ((int)MTDModel.cacheSceneType).ToString() + "_"
                    + MTDModel.myTeamData.scene_busid.ToString() + "_" + MTDModel.myTeamData.team_id.ToString() + "_"
                    + RemoteModel.Instance.Player.Gid.ToString() + "_" + NetworkManager.Instance.ZoneID.ToString() + "_" + MTDModel.myTeamData.passwd;
                    Debuger.Log("[frankhao]" + extInfo);
                    SnsShare.SendToWeixinRoom(title, desc, extInfo);
                }
                else
                {
                    UIAPI.ShowMsgTip("数据有问题");
                }
            }
            GUILayout.EndVertical();
        }

        void OnPvp20()
        {
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("开启浮空保护血量百分比：");
                    string strRatio = GUILayout.TextField(DEFINE.OPEN_DMG_FLOAT_PROTECT_HP_PERCENTAGE.ToString());

                    int iRatio;
                    if (int.TryParse(strRatio, out iRatio))
                    {
                        DEFINE.OPEN_DMG_FLOAT_PROTECT_HP_PERCENTAGE_CLIENT = iRatio;
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("浮空保护重力系数：");
                    string strRatio = GUILayout.TextField(DEFINE.DMG_FLOAT_PROTECT_WEIGHT_FIX_PRECENTAGE.ToString());

                    int iRatio;
                    if (int.TryParse(strRatio, out iRatio))
                    {
                        DEFINE.DMG_FLOAT_PROTECT_WEIGHT_FIX_PRECENTAGE_CLIENT = iRatio;
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("设置控制半径：");
                    string joyStickRadius = GUILayout.TextField(DefineExt.JoyStickRadius.ToString());

                    float fRadius;
                    if (float.TryParse(joyStickRadius, out fRadius))
                    {
                        DefineExt.JoyStickRadius = fRadius;
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button((DefineExt.EnablePvPTss ? "关闭" : "打开") + "PVP TSS"))
                    {
                        DefineExt.EnablePvPTss = !DefineExt.EnablePvPTss;
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
            }
        }

        #endregion

        private void jumpCallBack()
        {
            UIAPI.ShowMsgTip("关闭跳转主界面");
        }


        #region
        private void OnGuildAuctionTest()
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("发送协议"))
            {
                KHPluginManager.Instance.SendMessage("GuildAuctionPlugin", "QueryAuctionData");
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (GUILayout.Button("拍卖道具"))
            {
                KHPluginManager.Instance.SendMessage("GuildAuctionPlugin", "SendOfferPriceMsg");
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (GUILayout.Button("进入组织"))
            {
                KHJumpSystemHelper.DoJump(SystemConfigDef.Guild, null);
            }
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            if (GUILayout.Button("进入小队突袭"))
            {
                KHJumpSystemHelper.DoJump(SystemConfigDef.TeamPVE, null);
            }
            GUILayout.EndVertical();
        }
        #endregion


        #region 锦标赛入口
        string h32 = "";
        string l31 = "";
        string busid = "";


        private void OnChampionshipTest()
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("打开忍话剧"))
            {
                KHPluginManager.Instance.SendMessage("MainUI", "HideView", "MainUI");
                KHPluginManager.Instance.SendMessage("MainUI", "HideView", "PlayerBarUI");
                KHPluginManager.Instance.SendMessageForLua("DramaPlugin", "ShowView", new ShowViewArgument("UILua/Drama/DramaView", false));
            }

            if (GUILayout.Button("打开锦标赛"))
            {
                KHPluginManager.Instance.SendMessageForLua("ChampionshipPlugin", "ShowView", new ShowViewArgument("UILua/Championship/ChampionshipBattleView",
                                                                                                                  true));
            }

            if (GUILayout.Button("打开结算界面"))
            {
                ///显示最后的结算
                KHBattleManager.Instance.BattlePlugin.ShowView("PVPRealTime/BattleFinalResult", false, null);
            }

            if (GUILayout.Button("开始监听拉进比赛的Ntf"))
            {
                KHPluginManager.Instance.GetPluginByName("ChampionshipPlugin").SendMessageForLua("Test", null);
            }

            if (GUILayout.Button("重新进入最近的场景"))
            {
                KHPluginManager.Instance.GetPluginByName("ChampionshipPlugin").SendMessageForLua("Test2", null);
            }

            if (GUILayout.Button("重现加入场景～"))
            {
                KHPluginManager.Instance.GetPluginByName("ChampionshipPlugin").SendMessageForLua("Test3", null);
            }

            h32 = GUILayout.TextField(h32);
            l31 = GUILayout.TextField(l31);
            busid = GUILayout.TextField(busid);
            if (GUILayout.Button("进入房间"))
            {
                SceneID id = new SceneID();
                id.h32 = uint.Parse(h32);
                id.l32 = uint.Parse(l31);
                DataTestForCp data = new DataTestForCp();
                data.scene_id = id;
                data.scene_busid = int.Parse(busid);

                KHPluginManager.Instance.GetPluginByName("ChampionshipPlugin").SendMessageForLua("requireEnterScene", data);
            }

            GUILayout.EndVertical();
        }
        #endregion

        #region 打开擂台赛


        private void OnChallengeTest()
        {
            GUILayout.BeginVertical();


            if (GUILayout.Button("监听擂台赛匹配"))
            {
                KHPluginManager.Instance.GetPluginByName("ChallengePlugin").SendMessageForLua("Test", null);
            }

            if (GUILayout.Button("监听2v2匹配"))
            {
                KHPluginManager.Instance.GetPluginByName("KizunaContestRoomPlugin").SendMessageForLua("Test", null);
            }

            if (GUILayout.Button("点击发送聊天消息"))
            {
                KHPluginManager.Instance.GetPluginByName("ChallengePlugin").SendMessageForLua("Test2", null);
            }


            GUILayout.EndVertical();
        }
        #endregion

        #region 捏造忍者系统


        private void OnLeozzzhangTest()
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("不公平之战测试"))
            {
                KHPluginManager.Instance.GetPluginByName(UnfairBattlePlugin.pluginName).SendMessage("QueayUnfairBattle", null);
            }

            if (GUILayout.Button("【巅峰赛】入口"))
            {
                KHPluginManager.Instance.GetPluginByName(PVPConquestPlugin.pluginName).SendMessage(PVPConquestOperation.ConquerGetBattleDetail, null);
            }

            if (GUILayout.Button("请求打开暗部3v3捏造忍者系统"))
            {
                KHPluginManager.Instance.GetPluginByName(CommonCreateNinjaPlugin.pluginName).SendMessage("QueryAllNinjaInfo", 116);
            }

            if (GUILayout.Button("打开捏造忍者系统"))
            {
                KHPluginManager.Instance.GetPluginByName(CommonCreateNinjaPlugin.pluginName).SendMessage("Test", null);
            }

            if (GUILayout.Button("打开人界Ui"))
            {
                KHPluginManager.Instance.GetPluginByName(PVPConquestPlugin.pluginName).SendMessage("test11", null);
            }

            if (GUILayout.Button("打开黑市商人"))
            {
                KHPluginManager.Instance.GetPluginByName(BlackMarketeerPlugin.pluginName).SendMessage("QueryAllInfo", null);
            }

            if (GUILayout.Button("购买"))
            {
                KHPluginManager.Instance.GetPluginByName(BlackMarketeerPlugin.pluginName).SendMessage("Test2", null);
            }

            if (GUILayout.Button("出售"))
            {
                KHPluginManager.Instance.GetPluginByName(BlackMarketeerPlugin.pluginName).SendMessage("Test3", null);
            }

            if (GUILayout.Button("请求忍具背包2"))
            {
                KHPluginManager.Instance.GetPluginByName(NinjaWeaponPlugin.pluginName).SendMessage(NinjaWeaponOperation.QueryWeaponPkgInfo, null);
            }

            if (GUILayout.Button("打开历史战绩"))
            {
                KHPluginManager.Instance.GetPluginByName(TianDiZhanChangPlugin.pluginName).SendMessage("GetHistory", null);
            }

            if (GUILayout.Button("打开百忍大战"))
            {
                KHPluginManager.Instance.GetPluginByName(HundredNinjaWarPlugin.pluginName).SendMessage("EnterHundredWarScene", null);
            }

            if (GUILayout.Button("打开brdz"))
            {
                KHPluginManager.Instance.GetPluginByName(HundredNinjaWarPlugin.pluginName).SendMessage("FightQueryCanEnter", null);
            }

            if (GUILayout.Button("监听brdz"))
            {
                KHPluginManager.Instance.GetPluginByName(HundredNinjaWarPlugin.pluginName).SendMessage("AddListeners", null);
            }

            if (GUILayout.Button("查询巅峰对决"))
            {
                UltimateKillModel.isTest = true;
                KHPluginManager.Instance.SendMessage(UltimateKillPlugin.pluginName, UltimateKillFinalOperation.QueayFinalViewInfo, null);
            }

            if (GUILayout.Button("巅峰赛决赛【关闭】分开测试"))
            {
                UltimateKillModel.isTest = false;
            }


            testBgm = GUILayout.TextField(testBgm, 50);

            if (GUILayout.Button("音乐播放"))
            {
                KHAudioManager.PlayMusic(int.Parse(testBgm));
            }

            GUILayout.EndVertical();
        }

        private void OnBobcczhengTest()
        {
            MessageManager msgManager = MessageManager.Instance;
            GUILayout.BeginVertical();

            string str1 = msgManager.IsActivate ? "关闭" : "开启";
            str1 += "序列化工具";
            if (GUILayout.Button(str1))
            {
                msgManager.IsActivate = !msgManager.IsActivate;
            }

            string str2 = "当前";
            str2 += msgManager.IsSerializeToLocal ? "录播" : "重播";
            if (GUILayout.Button(str2))
            {
                msgManager.IsSerializeToLocal = !msgManager.IsSerializeToLocal;
            }

            if (GUILayout.Button("鼠标自动化测试"))
            {
                // MouseAction mouseevent = msgManager.deserializeFromLocal<MouseAction>(MessageManager.DEST_PATH_MOUSE_EVENT, 0);

                // MouseAction mouseevent = (MouseAction)msgManager.deserializeFromLocalByTimeStamp();

                GameObject mouseActManager = new GameObject("MouseActionManager");
                mouseActManager.AddComponent<MouseActionManager>();
                // mouseevent.execute();
                // GameObject mouseSimulator = new GameObject("MouseSimulator");

            }

            if (GUILayout.Button("鼠标监控"))
            {
                if (GameObject.Find("MouseMonitor") == null)
                {
                    GameObject mouseMonitor = new GameObject("MouseMonitor");
                    mouseMonitor.AddComponent<MouseMonitor>();
                }
            }

            if (GUILayout.Button("清理缓存"))
            {
                File.Delete(MessageManager.DEST_PATH_MOUSE_EVENT);
                File.Delete(MessageManager.DEST_PATH_CSharp);
                Debug.Log("文件已删除");
            }




            GUILayout.EndVertical();

        }

        static string testBgm = "9001";
        public delegate void myDelegate();

        void tttt()
        {
            Debuger.LogError("tttt!!!!!!");
        }
        #endregion

        #region 摇一摇
        ShakeAShake shakeAshake = null;
        void OnShakeAShake()
        {
            if (shakeAshake == null)
            {
                shakeAshake = gameObject.AddComponent<ShakeAShake>();
            }
            GUILayout.BeginVertical();
            if (GUILayout.Button("摇一摇" + SwitcherToString(shakeAshake.bIsOpen)))
            {
                shakeAshake.bIsOpen = !shakeAshake.bIsOpen;
            }
            GUILayout.EndVertical();
        }
        #endregion
        private string actor_id = "90001001";
        private void OnToolsSwitcher()
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("模拟丢包" + SwitcherToString(KiHanProxy.bSimulationLossNetworkMessage)))
            {
                KiHanProxy.bSimulationLossNetworkMessage = !KiHanProxy.bSimulationLossNetworkMessage;
            }

            if (GUILayout.Button("战斗开始时打印图集" + SwitcherToString(DefineExt.EnableTraceAtlasOnBattleStart)))
            {
                DefineExt.EnableTraceAtlasOnBattleStart = !DefineExt.EnableTraceAtlasOnBattleStart;
                if (DefineExt.EnableTraceAtlasOnBattleStart)
                {
                    KHBattleManager.Instance.addEventListener(BattleEvent.START_BATTLE, OnStartBattle);
                }
                else
                {
                    KHBattleManager.Instance.removeEventListener(BattleEvent.START_BATTLE, OnStartBattle);
                }
            }

            if (GUILayout.Button("ABTest切换  当前方案：" + (DefineExt.NewUserGuideMode == NEW_USER_GUIDE_TYPE.NewConfig ? "A" : "B")))
            {
                if (DefineExt.NewUserGuideMode == NEW_USER_GUIDE_TYPE.OldConfig)
                    DefineExt.NewUserGuideMode = NEW_USER_GUIDE_TYPE.NewConfig;
                else
                    DefineExt.NewUserGuideMode = NEW_USER_GUIDE_TYPE.OldConfig;
            }

            GUILayout.BeginHorizontal();
            actor_id = GUILayout.TextField(actor_id, 100, GUILayout.MinWidth(100f));

            if (GUILayout.Button("创建角色"))
            {
                KHSceneManager.Instance.BuildClientPlayer(Int32.Parse(actor_id), "amy", true);
            }
            GUILayout.EndHorizontal();


            GUILayout.EndVertical();
        }

        private string SwitcherToString(bool value)
        {
            if (value)
                return "【开】";
            else
                return "【关】";
        }

        private void OnStartBattle(KHEvent e)
        {
            Debuger.Log("OnStartBattle(), pluginName=" + KHBattleManager.Instance.PluginName);
            Debuger.Log(KHCheckUIResEditor.Instance.GetAllUIAtlas());
        }

        private void OnBattleNinjaTest()
        {
            GUILayout.BeginVertical();

            _PROTECT_FLOW_TIME = GUILayout.TextField(_PROTECT_FLOW_TIME);
            if (!int.TryParse(_PROTECT_FLOW_TIME, out DEFINE.TIME_FLOW_PROTECT_TIME))
            {
                DEFINE.TIME_FLOW_PROTECT_TIME = 10;
                _PROTECT_FLOW_TIME = "10";
            }

            if (GUILayout.Button("打开时间落地保护"))
            {
                DEFINE.ALLOW_TIME_FLOW_PROTECT = true;
            }

            if (GUILayout.Button("关闭时间落地保护"))
            {
                DEFINE.ALLOW_TIME_FLOW_PROTECT = false;
            }

            GUILayout.EndVertical();
        }

        #region LBS Test
        public string locationInfo = "";
        public string latitude = "22.548065";
        public string longitude = "113.944799";
        private void OnLBSTest()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("LocInfo: " + locationInfo);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginVertical();

            if (GUILayout.Button("请求当前LBS信息"))
            {
                locationInfo = "start request time : " + RemoteModel.Instance.CurrentDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                LBSUtil.GetLngLat(OnLocationGotNotify);
            }
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("latitude: ");
                latitude = GUILayout.TextField(latitude);
                GUILayout.Label("longitude: ");
                longitude = GUILayout.TextField(longitude);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            if (GUILayout.Button("查询城市码"))
            {
                locationInfo = "start request time : " + RemoteModel.Instance.CurrentDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                Apollo.ApolloLocation alocation = new Apollo.ApolloLocation();
                alocation.Latitude = (float)Convert.ToDouble(latitude);
                alocation.Longitude = (float)Convert.ToDouble(longitude);
                LBSUtil.GetCityCodeByLngLat(alocation, (LBSRes res) =>
                {
                    if (res != null)
                    {
                        Debuger.Log("city = " + res.city + " citycode = " + res.citycode);
                        locationInfo = "city = " + res.city + " citycode = " + res.citycode;
                    }
                });
            }

            if (GUILayout.Button("clear"))
            {
                locationInfo = "";
            }

            GUILayout.EndVertical();
        }


        private void OnLocationGotNotify(Apollo.ApolloLocation alocation)
        {
            locationInfo += "            msdk return time : " + RemoteModel.Instance.CurrentDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            LBSUtil.GetCityCodeByLngLat(alocation, (LBSRes res) =>
            {
                if (res != null)
                {
                    Debuger.Log("lat = " + res.latitude + "lng = " + res.longitude + " city = " + res.city + " citycode = " + res.citycode);
                    locationInfo += "               web service return time : " + RemoteModel.Instance.CurrentDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    locationInfo += "lat = " + res.latitude + "lng = " + res.longitude + " city = " + res.city + " citycode = " + res.citycode;
                }
            });
        }

        #endregion

        #region 跨服要塞战
        private void OnCrossGuildWar()
        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("进入跨服要塞战"))
                {
                    KHPluginManager.Instance.SendMessage(CrossGuildWarPlugin.pluginName, CrossGuildWarOperation.OpenGuildWarMap);
                }
            }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("打开排名面板"))
            {
                KHPluginManager.Instance.GetPluginByName(CrossGuildWarPlugin.pluginName).ShowView(UIDef.CROSS_GUILD_WAR_RANK_MODAL);
            }
            if (GUILayout.Button("打开奖励面板"))
            {
                KHPluginManager.Instance.GetPluginByName(CrossGuildWarPlugin.pluginName).ShowView(UIDef.CROSS_GUILD_WAR_REWARD_MODAL);
            }
            if (GUILayout.Button("测试阵容调整接口"))
            {
                KHPluginManager.Instance.GetPluginByName(CrossGuildWarPlugin.pluginName)
                    .SendMessage(CSCommonTeamSettingOperation.QueryCommonTeam, SystemConfigDef.CrossGuildWar);
            }
            if (GUILayout.Button("打开组织礼包分配界面"))
            {
                KHPluginManager.Instance.GetPluginByName(CrossGuildWarPlugin.pluginName).ShowView(UIDef.CROSS_GUILD_WAR_GUILD_PRIZE_MODAL);
            }
            if (GUILayout.Button("（临时）打开结算面板"))
            {
                KHPluginManager.Instance.GetPluginByName(CrossGuildWarPlugin.pluginName).ShowView(UIDef.PVP_REALTIME_BATTLE_FINAL_RESULT);
            }
        }
        #endregion

        #region 新叛忍来袭
        private static string testBadNinjaAddScore = "";
        private void OnDebugBadNinja()
        {
            if (GUILayout.Button("打开叛忍入口界面"))
            {
                KHPluginManager.Instance.GetPluginByName(BadNinjaPlugin.pluginName).ShowView(UIDef.BAD_NINJA_ENTRANCE, false);
            }

            if (GUILayout.Button("打开叛忍排名界面"))
            {
                KHPluginManager.Instance.GetPluginByName(BadNinjaPlugin.pluginName).ShowView(UIDef.BAD_NINJA_RANK_MODAL, false);
            }

            if (GUILayout.Button("打开叛忍奖励预览界面"))
            {
                KHPluginManager.Instance.GetPluginByName(BadNinjaPlugin.pluginName).ShowView(UIDef.BAD_NINJA_REWARD_VIEW_MODAL, false);
            }

            if (GUILayout.Button("打开叛忍查看详情界面"))
            {
                KHPluginManager.Instance.GetPluginByName(BadNinjaPlugin.pluginName).ShowView(UIDef.BAD_NINJA_DETAIL_VIEW_MODAL, false);
            }

            if (GUILayout.Button("匹配BOSS"))
            {
                KHPluginManager.Instance.SendMessage(BadNinjaPlugin.pluginName, BadNinjaSceneOperation.RequestMatchBoss);
            }

            if (GUILayout.Button("打开活动结算界面"))
            {
                KHPluginManager.Instance.GetPluginByName(BadNinjaPlugin.pluginName)
                    .ShowView(UIDef.BAD_NINJA_ACTIVITY_END_VIEW);
            }

            GUILayout.BeginHorizontal();
            {
                testBadNinjaAddScore = GUILayout.TextField(testBadNinjaAddScore);
                int score;
                if (int.TryParse(testBadNinjaAddScore, out score))
                {
                    if (GUILayout.Button("客户端加积分"))
                    {
                        (KHPluginManager.Instance.GetPluginByName(BadNinjaPlugin.pluginName)
                            .Model as BadNinjaModel).TestAddScore((uint)score);
                    }
                }
            }
            GUILayout.EndHorizontal();
        }
        #endregion

        #region
        private void OnAnBuPVP()
        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("开始暗部无差别匹配"))
                {
                    KHPluginManager.Instance.GetPluginByName(AnBuPVPPlugin.PLUGIN_NAME).SendMessage(AnBuPVPUIOperation.RequireMatch);
                }
                if (GUILayout.Button("技能一览"))
                {
                    KHPluginManager.Instance.SendMessage(AnBuPVPPlugin.PLUGIN_NAME, "RequireSkillDetail");
                }
                if (GUILayout.Button("技能排行榜"))
                {
                    //KHPluginManager.Instance.SendMessage(AnBuPVPPlugin.PLUGIN_NAME, "RequireSkillRank");
                    ZoneAnbu1V1GetRankRsp resp = new ZoneAnbu1V1GetRankRsp();
                    resp.today_list.Clear();
                    for (int i = 0; i < 10; i++)
                    {
                        PvpAnbuUndiffSkillRecord arg = new PvpAnbuUndiffSkillRecord();
                        arg.skills.Add(90001001);
                        arg.skills.Add(90001001);
                        arg.skills.Add(90001001);
                        arg.fight_times = 10000;
                        arg.win_times = 999;
                        resp.today_list.Add(arg);
                    }
                    resp.all_list.Clear();
                    for (int i = 0; i < 4; i++)
                    {
                        PvpAnbuUndiffSkillRecord arg = new PvpAnbuUndiffSkillRecord();
                        arg.skills.Add(90001001);
                        arg.skills.Add(90001001);
                        arg.skills.Add(90001001);
                        arg.fight_times = 10000;
                        arg.win_times = 999;
                        resp.all_list.Add(arg);
                    }
                    KHPluginManager.Instance.SendMessage(AnBuPVPPlugin.PLUGIN_NAME, "ShowView", new ShowViewArgument(UIDef.AnBuPVP_BESTSKILLRANK_PANEL_VIEW, false, resp));
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("查询战报"))
                {
                    KHPluginManager.Instance.GetPluginByName(AnBuPVPPlugin.PLUGIN_NAME).SendMessage("RequireFightRecord");
                }
                if (GUILayout.Button("打开主入口(临时UI)"))
                {
                    // KHJumpSystemHelper.DoJump(117, null);
                    KHPluginManager.Instance.SendMessage(AnBuPVPPlugin.PLUGIN_NAME, "OpenAnBuPVPEntrance", null);
                }
            }
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Zomibe

        private void OnZombie()
        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("进入"))
                {
                    KHPluginManager.Instance.SendMessage(ZombieContextPlugin.PLUGIN_NAME, ZombieEntranceOperation.Op_Enter, null);
                }
            }
            GUILayout.EndHorizontal();
        }

        private void OnBattle()
        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("开启AI"))
                {
                    List<PlayerController> lstPlayers = KHPlayerManager.getInstance().getPlayerList();
                    for (int i = 0; i < lstPlayers.Count; i++)
                    {
                        lstPlayers[i].aiEnable = true;
                    }
                }

                if (GUILayout.Button("关闭AI"))
                {
                    List<PlayerController> lstPlayers = KHPlayerManager.getInstance().getPlayerList();
                    for (int i = 0; i < lstPlayers.Count; i++)
                    {
                        lstPlayers[i].aiEnable = false;
                    }
                }
            }
            GUILayout.EndHorizontal();
        }
        #endregion

        #region cube
        private void OnCube()
        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("启动cubesdk"))
                {
                    if (CubeSdkAgent.CubeInit())
                    {
                        UIAPI.ShowMsgTip("启动成功");
                    }
                    else
                    {
                        UIAPI.ShowMsgTip("启动失败");
                    }
                }

                if (GUILayout.Button("fps"))
                {
                    UIAPI.ShowMsgTip(CubeSdkAgent.GetFps().ToString());
                }

                if (GUILayout.Button("GetCpu"))
                {
                    UIAPI.ShowMsgTip(CubeSdkAgent.GetCpu().ToString());
                }

                if (GUILayout.Button("GetPss"))
                {
                    UIAPI.ShowMsgTip(CubeSdkAgent.GetPss().ToString());
                }

                if (GUILayout.Button("GetNative"))
                {
                    UIAPI.ShowMsgTip(CubeSdkAgent.GetNative().ToString());
                }

                if (GUILayout.Button("GetDalvik"))
                {
                    UIAPI.ShowMsgTip(CubeSdkAgent.GetDalvik().ToString());
                }

                if (GUILayout.Button("GetMonoR"))
                {
                    UIAPI.ShowMsgTip(CubeSdkAgent.GetMonoR().ToString());
                }

                if (GUILayout.Button("GetMonoU"))
                {
                    UIAPI.ShowMsgTip(CubeSdkAgent.GetMonoU().ToString());
                }

                if (GUILayout.Button("GetTriangles"))
                {
                    UIAPI.ShowMsgTip(CubeSdkAgent.GetTriangles().ToString());
                }

                if (GUILayout.Button("GetDrawcall"))
                {
                    UIAPI.ShowMsgTip(CubeSdkAgent.GetDrawcall().ToString());
                }

                if (GUILayout.Button("GetTcpS"))
                {
                    UIAPI.ShowMsgTip(CubeSdkAgent.GetTcpS().ToString());
                }

                if (GUILayout.Button("GetTcpR"))
                {
                    UIAPI.ShowMsgTip(CubeSdkAgent.GetTcpR().ToString());
                }

                if (GUILayout.Button("GetUdpS"))
                {
                    UIAPI.ShowMsgTip(CubeSdkAgent.GetUdpS().ToString());
                }

                if (GUILayout.Button("GetUdpR"))
                {
                    UIAPI.ShowMsgTip(CubeSdkAgent.GetUdpR().ToString());
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("FPS: {0}", CubeSdkAgent.GetFps().ToString()));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Triangles: {0}", CubeSdkAgent.GetTriangles().ToString()));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Drawcall: {0}", CubeSdkAgent.GetDrawcall().ToString()));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("PSS: {0}", CubeSdkAgent.GetPss().ToString()));
            GUILayout.EndHorizontal();
        }
        #endregion

        #region 保存到相册测试
        private void OnLoadImgTest()
        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("保存图片"))
                {
                    LoadImg("http://dlied5.qq.com/kihan/zhaomutupian/samuyiyongzhuang.png");
                    UIAPI.ShowMsgTip("保存图片成功");
                }
                if (GUILayout.Button("保存视频"))
                {
                    LoadMultiMedia("http://dlied5.qq.com/kihan/test/0.mp4");
                    UIAPI.ShowMsgTip("保存视频成功");
                }
                if (GUILayout.Button("保存音频"))
                {
                    LoadMultiMedia("http://dlied5.qq.com/kihan/test/test.mp3");
                    UIAPI.ShowMsgTip("保存音频成功");
                }
            }
            GUILayout.EndHorizontal();
        }

        public void LoadImg(string imgurl)
        {
            KHGlobalExt.StartCoroutine(UpdateImg(imgurl));
        }

        IEnumerator UpdateImg(string imgurl)
        {
            imgurl = imgurl.Trim();
            if (DefineExt.lstCloseSysID != null && DefineExt.lstCloseSysID.Contains(UISysSwitchDef.SettingHttps))
            { // 关闭https://, 将头替换成http://
                imgurl = PathUtils.SafeReplaceUrlHeadIgnoreCase(imgurl, "https://", "http://");
                Debuger.LogWarning("URL", "https closed, url converted http-url:" + imgurl);
            }
            else
            { // IOS上强制将http替换成https
#if UNITY_IPHONE
                imgurl = PathUtils.SafeReplaceUrlHeadIgnoreCase(imgurl, "http://", "https://");
#endif
            }
            string LocalCacheDirPath = Application.temporaryCachePath + "/cacheimg/";

            string texfilename = PathUtils.StripUrlProtocol2Path(imgurl);

            string localtexpath = LocalCacheDirPath + texfilename;

            string texurl = "";

            bool bNeedCache = false;

            if (File.Exists(localtexpath))
            {
#if UNITY_EDITOR
                texurl = "file:///" + localtexpath;
#else
				texurl = "file://" + localtexpath;
#endif
            }
            else
            {
                bNeedCache = true;
                texurl = imgurl;
            }

            Debuger.Log("[new ImgUrl]get " + texurl + ", filename:" + texfilename);

            using (WWW www = new WWW(texurl))
            {
                Debuger.Log("0000000000000000000000000000000000");
                yield return www;
                Debuger.Log("77777777777777777777777777777777777777");
                if (www.error == null)
                {
                    Debuger.Log("88888888888888888888888888888888888888");
                    if (bNeedCache)
                    {
                        Debuger.Log("gggggggggggggggggggggggggggg");
                        try
                        {
                            /// Todo 添加淘汰机制
                            Debuger.Log("11111111111111111111111111111111");
                            Debuger.Log("[new ImgUrl]save filename:" + texfilename);
                            FileUtils.SafeToFile(www.bytes, LocalCacheDirPath, texfilename);
                            if (Directory.Exists(LocalCacheDirPath))
                            {
                                Debuger.Log("eeeeeeeeeeeeeeeeeeeeeeeeeeeeeee");
                                if (File.Exists(LocalCacheDirPath + texfilename))
                                {
                                    Debuger.Log("3333333333333333333333333333333333333");
                                }
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debuger.LogWarning("我自己捕获了异常，不应该crash : " + e.Message);
                        }
                    }
                    Debuger.Log("fffffffffffffff");
                    KHFileUtil.SaveIOSPhoto(localtexpath); //存相册测试
                    Debuger.Log("hhhhhhhhhhhhhhhhhhhhhhhhhhh");
                }
                else
                {
                    Debuger.LogWarning("new ImgUrl", "get " + texurl + ", err:" + www.error);
                }

                //UIAPI.hideUILoading(UILoadingType.UILOADING_MINI);
                KH.Network.LoadingTipStack.Hide();
            }

        }

        public void LoadMultiMedia(string mediaurl)
        {
            KHGlobalExt.StartCoroutine(UpdateMultiMedia(mediaurl));
        }

        IEnumerator UpdateMultiMedia(string mediaurl)
        {
            mediaurl = mediaurl.Trim();
            if (DefineExt.lstCloseSysID != null && DefineExt.lstCloseSysID.Contains(UISysSwitchDef.SettingHttps))
            { // 关闭https://, 将头替换成http://
                mediaurl = PathUtils.SafeReplaceUrlHeadIgnoreCase(mediaurl, "https://", "http://");
                Debuger.LogWarning("URL", "https closed, url converted http-url:" + mediaurl);
            }
            else
            { // IOS上强制将http替换成https
#if UNITY_IPHONE
                mediaurl = PathUtils.SafeReplaceUrlHeadIgnoreCase(mediaurl, "http://", "https://");
#endif
            }
            string LocalCacheDirPath = Application.temporaryCachePath + "/cachemedia/";

            string texfilename = PathUtils.StripUrlProtocol2Path(mediaurl);

            string localtexpath = LocalCacheDirPath + texfilename;

            string texurl = "";

            bool bNeedCache = false;

            if (File.Exists(localtexpath))
            {
#if UNITY_EDITOR
                texurl = "file:///" + localtexpath;
#else
				texurl = "file://" + localtexpath;
#endif
            }
            else
            {
                bNeedCache = true;
                texurl = mediaurl;
            }

            Debuger.Log("[new mediaurl]get " + texurl + ", filename:" + texfilename);

            using (WWW www = new WWW(texurl))
            {
                Debuger.Log("mediaurl 0000000000000000000000000000000000");
                yield return www;
                Debuger.Log("mediaurl 77777777777777777777777777777777777777");
                if (www.error == null)
                {
                    Debuger.Log("mediaurl 88888888888888888888888888888888888888");
                    if (bNeedCache)
                    {
                        Debuger.Log("mediaurl gggggggggggggggggggggggggggg");
                        try
                        {
                            /// Todo 添加淘汰机制
                            Debuger.Log("mediaurl 11111111111111111111111111111111");
                            Debuger.Log("[new mediaurl]save filename:" + texfilename);
                            FileUtils.SafeToFile(www.bytes, LocalCacheDirPath, texfilename);
                            if (Directory.Exists(LocalCacheDirPath))
                            {
                                Debuger.Log("mediaurl eeeeeeeeeeeeeeeeeeeeeeeeeeeeeee");
                                if (File.Exists(LocalCacheDirPath + texfilename))
                                {
                                    Debuger.Log("mediaurl 3333333333333333333333333333333333333");
                                }
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debuger.LogWarning("我自己捕获了异常，不应该crash : " + e.Message);
                        }
                    }
                    Debuger.Log("mediaurl fffffffffffffff");
                    KHFileUtil.SaveIOSVideo(localtexpath); //存相册测试
                    Debuger.Log("mediaurl hhhhhhhhhhhhhhhhhhhhhhhhhhh");
                }
                else
                {
                    Debuger.LogWarning("new mediaurl", "get " + texurl + ", err:" + www.error);
                }

                //UIAPI.hideUILoading(UILoadingType.UILOADING_MINI);
                KH.Network.LoadingTipStack.Hide();
            }

        }

        #endregion

        #region
        private string bundle_url = "";
        private string bundle_result = "";
        private void OnBundleLoadTest()
        {
            GUILayout.BeginVertical();

            bundle_url = GUILayout.TextField(bundle_url, 100, GUILayout.MinWidth(100f));
            GUILayout.Label(bundle_result);

            if (GUILayout.Button("DOOOO", GUILayout.MinWidth(100)))
            {
                KHBundleInfo info = KHBundleConfigManager.getBundleInfoByRes(bundle_url);
                if (info == null)
                {
                    bundle_result = "<color=red>" + bundle_url + "不存在!</color>";
                }
                else
                {
                    bundle_result = "resCount:" + info.res_list.Count;
                    foreach (var _url in info.res_list)
                    {
                        KHResource.LoadRes(_url, (string url, UnityEngine.Object obj, LOADSTATUS result, object extra) =>
                        {
                        });
                    }

                    foreach (var _url in info.res_list)
                    {
                        KHResource.unLoadRes(_url, null);
                    }
                }
            }

            GUILayout.EndVertical();
        }
        #endregion

        #region EffectTest
        private string effectID = "";
        private GameObject _uiRoot = null;
        private List<GameObject> pools = new List<GameObject>();
        private void OnResLoadTest()
        {
            if (_uiRoot == null)
            {
                _uiRoot = GameObject.Find("UI Root");
            }

            GUILayout.BeginVertical();

            effectID = GUILayout.TextField(effectID, 100, GUILayout.MinWidth(100f));

            if (GUILayout.Button("Hide/Show GameUI", GUILayout.MinWidth(100)))
            {
                _uiRoot.SetActive(!_uiRoot.activeSelf);
            }

            if (GUILayout.Button("DOOOOOO Effect", GUILayout.MinWidth(100)))
            {
                KHResource.LoadRes("Effect/" + effectID, (string url, UnityEngine.Object obj, LOADSTATUS result, object extra) =>
                {
                    if (obj == null)
                    {
                        Debuger.Log("[OnResLoadTest], effect:" + url + ", no_exist");
                    }
                    else
                    {
                        GameObject go = GameObject.Instantiate(obj) as GameObject;
                        pools.Add(go);
                        //Debuger.Log("OnEffectLoadTest," + PrintGameObject(go, 0));


                        NcCurveAnimation[] anis = go.GetComponentsInChildren<NcCurveAnimation>();
                        if (anis != null)
                        {
                            StringBuilder log = new StringBuilder();
                            foreach (var ani in anis)
                            {
                                PrintComponent(ani, log);
                                log.Append("\r\n");
                            }
                            Debuger.Log(log);
                        }
                    }
                });
            }

            if (GUILayout.Button("DOOOOOO Res", GUILayout.MinWidth(100)))
            {
                KHResource.LoadRes(effectID, (string url, UnityEngine.Object obj, LOADSTATUS result, object extra) =>
                    {
                        if (obj == null)
                        {
                            Debuger.Log("[OnResLoadTest], res:" + url + ", no_exist");
                        }
                        else
                        {
                            GameObject go = GameObject.Instantiate(obj) as GameObject;
                            pools.Add(go);
                            //Debuger.Log("OnEffectLoadTest," + PrintGameObject(go, 0));


                        }
                    }
                );
            }

            if (GUILayout.Button("Clearrrr Effect", GUILayout.MinWidth(100)))
            {
                foreach (var go in pools)
                {
                    GameObject.Destroy(go);
                }
                pools.Clear();
            }

            GUILayout.EndVertical();
        }

        public static string PrintGameObject(GameObject go, int level = 0)
        {
            StringBuilder log = new StringBuilder();

            for (int i = 0; i < level; ++i)
                log.Append("++");
            log.Append(go.name).Append(", activeSelf:").Append(go.activeSelf);
            if (go.particleSystem != null)
                PrintComponent(go.particleSystem, log);

            Transform trans = go.transform;
            for (int i = 0; i < trans.childCount; ++i)
            {
                //Debuger.Log("====>"+ trans.GetChild(i).gameObject);
                PrintGameObject(trans.GetChild(i).gameObject, level + 1);
                log.Append("\r\n");
            }

            return log.ToString();
        }

        static void PrintComponent(Component comp, StringBuilder log)
        {
            log.Append("_C(").Append(comp.GetType()).Append(",");
            if (comp.renderer != null)
                PrintRender(comp.renderer, log);
            log.Append(")");
        }

        static void PrintRender(Renderer render, StringBuilder log)
        {
            log.Append("_R(").Append(render.GetInstanceID()).Append(",").Append("mats{");
            Material[] mats = render.materials;
            if (mats != null)
            {
                foreach (var mat in mats)
                {
                    PrintMat(mat, log);
                }
            }

            log.Append("},");
            log.Append("shared_mats{");

            mats = render.sharedMaterials;
            if (mats != null)
            {
                foreach (var mat in render.sharedMaterials)
                {
                    PrintMat(mat, log);
                }
            }
            log.Append("})");
        }

        static void PrintMat(Material mat, StringBuilder log)
        {
            log.Append("_M(").Append(mat.name).Append(",").Append(mat.GetInstanceID()).Append(",");
            if (mat.shader != null)
                PrintShader(mat.shader, log);
            log.Append(")");
        }

        static void PrintShader(Shader shader, StringBuilder log)
        {
            log.Append("_S(").Append(shader.name).Append(",").Append(shader.GetInstanceID()).Append(",").Append(shader.isSupported).Append(")");
        }
        #endregion

        #region Game.txt配置
        private static string gameTxt = null;
        static void OnGameTxt()
        {
            if (gameTxt == null)
            {
                Dictionary<string, string> cfg = KHResource._ListProperties();
                foreach (var entry in cfg)
                {
                    gameTxt += entry.Key + "=" + entry.Value + "\n";
                }
            }
            gameTxt = GUILayout.TextArea(gameTxt, GUILayout.MinWidth(100), GUILayout.MinHeight(80), GUILayout.ExpandHeight(true));
            if (GUILayout.Button("Save保存修改", GUILayout.MinWidth(100)))
            {
                FileUtils.SafeToFile(KHUtil.SafeConvert2Bytes(gameTxt), KHV2BundleUtil.getExpkgDir(0), "game.txt");
            }
        }

        #endregion

        #region ObjectCache Printer
        private static string leakTxt = "";
        private static string cmdTxt = "";
        static void OnResLeakPrinter()
        {
            if (GUILayout.Button("Print Object Leaker"))
            {
                leakTxt = _PrintObjectCache(1);
            }

            if (GUILayout.Button("Print Bundle Leaker"))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("PrintBundleCache Start:" + DateTime.Now.ToString());
                KHBundleLogger.Log("PrintBundleCache Start, count:" + BundleCacheInfo.url_map_bundle.Values.Count);
                foreach (var cache in BundleCacheInfo.url_map_bundle.Values)
                {
                    if (cache.IsValid && cache.refcount > 1)
                    {
                        StringBuilder line = new StringBuilder();
                        line.Append(cache.bdName + "," + cache.refcount);
                        sb.AppendLine(line.ToString());
                    }

                    KHBundleLogger.Log(cache.bdName + "," + cache.IsValid + "," + cache.refcount + ", (ref:" + cache.refcount + ", loadType:" + cache.loadType + ")");
                }
                leakTxt = sb.ToString();

                KHBundleLogger.Log("PrintBundleCache End");
                KHBundleLogger.Save();
            }

            if (GUILayout.Button("Print Object Cache All"))
            {
                leakTxt = _PrintObjectCache(0);
            }

            if (GUILayout.Button("Print Atlas Leaker"))
            {
                StringBuilder sb = new StringBuilder();
                Dictionary<string, int> nums = new Dictionary<string, int>();
                KHBundleLogger.Log("PrintUIAtlas Start");
                sb.AppendLine("PrintUIAtlas Start:" + DateTime.Now.ToString());

                UIAtlas[] atlasArr = Resources.FindObjectsOfTypeAll<UIAtlas>();
                foreach (var atlas in atlasArr)
                {
                    int num = 0;
                    nums.TryGetValue(atlas.name, out num);
                    nums[atlas.name] = ++num;
                }
                foreach (var e in nums)
                {
                    if (e.Value > 1)
                        sb.AppendLine(e.Key + ", " + e.Value);

                    KHBundleLogger.Log(e.Key + ", " + e.Value);
                }
                leakTxt = sb.ToString();

                KHBundleLogger.Log("PrintUIAtlas End");
                KHBundleLogger.Save();
            }

            if (GUILayout.Button("Print Snapshot"))
            {
                StringWriter writer = new StringWriter();
                KHModuleSnapshot.Inst.Print(writer);
                string msg = writer.GetStringBuilder().ToString();
                KHBundleLogger.Log(msg);
                KHBundleLogger.Save();

                UIAPI.ShowDbgMsg(msg);
            }

            GUILayout.BeginVertical();
            cmdTxt = GUILayout.TextField(cmdTxt, 20);
            if (GUILayout.Button("Do ChatCmd"))
            {
                KHUtilForLua.ExecLuaFunc("__dbg." + cmdTxt);
            }
            GUILayout.EndVertical();

            GUILayout.Space(20);
            GUILayout.Label("<color=yellow>" + leakTxt + "</color>");
        }

        private static string _PrintObjectCache(int refcount_min)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("PrintObjectCache Start:" + DateTime.Now.ToString());
            KHBundleLogger.Log("PrintObjectCache Start, count:" + ObjectCacheInfo.url_map_bdnames.Values.Count);
            foreach (var cache in ObjectCacheInfo.url_map_bdnames.Values)
            {
                if (cache.IsValid && cache.refcount > refcount_min && !IsBattleCommonRes(cache.bdName))
                {
                    StringBuilder line = new StringBuilder();
                    line.Append(cache.bdName + "," + cache.refcount);
                    List<String> list = cache.getDependentUrl();
                    if (list.Count > 0)
                    {
                        line.Append(", dependOn:(");
                        foreach (String str in list)
                        {
                            line.Append(str).Append(",");
                        }
                        line.Append(")");
                    }
                    sb.AppendLine(line.ToString());
                }
                KHBundleLogger.Log(cache.bdName + "," + cache.IsValid + "," + cache.refcount + ", (ref:" + cache.refcount + ", loadType:" + cache.loadType + ")");
            }
            KHBundleLogger.Log("PrintObjectCache End");
            KHBundleLogger.Save();

            return sb.ToString();
        }

        private static bool IsBattleCommonRes(string path)
        {
            KHBundleInfo battlecommon = KHBundleConfigManager.getBundleInfo("battlecommon");
            if (battlecommon == null)
            {
                return false; // 可能不是以bundle方法运行
            }
            return battlecommon.res_list.Contains(path);
        }

        private static int leakCount_obj_his = 0;
        private static int leakCount_bd_his = 0;
        private static int leakCount_atlas_his = 0;
        private static long lastMonitorTime = 0L;
        protected void ResLeakDetector()
        {
            long monitorTime = DateTime.Now.Ticks;
            if (monitorTime - lastMonitorTime < TimeSpan.TicksPerSecond * 60)
                return;
            lastMonitorTime = monitorTime;

            int leakCount_obj = 0;
            int leakCount_bd = 0;
            int leakCount_atlas = 0;
            foreach (var cache in ObjectCacheInfo.url_map_bdnames.Values)
            {
                if (cache.IsValid && cache.refcount > 1)
                {
                    leakCount_obj++;
                }
            }

            foreach (var cache in BundleCacheInfo.url_map_bundle.Values)
            {
                if (cache.IsValid && cache.refcount > 1)
                {
                    leakCount_bd++;
                }
            }

            { // atlas
                Dictionary<string, int> nums = new Dictionary<string, int>();
                UIAtlas[] atlasArr = Resources.FindObjectsOfTypeAll<UIAtlas>();
                foreach (var atlas in atlasArr)
                {
                    int num = 0;
                    nums.TryGetValue(atlas.name, out num);
                    nums[atlas.name] = ++num;
                }
                foreach (var e in nums)
                {
                    if (e.Value > 1)
                    {
                        leakCount_atlas++;
                    }

                }
            }

            bool needTips = false;
            if (leakCount_obj > 0 && leakCount_obj != leakCount_obj_his)
            {
                needTips = true;
                leakCount_obj_his = leakCount_obj;
                AddTip("发现疑似泄露object:" + leakCount_obj + "个, 请使用ResLeakPrinter工具查看详情!");
            }

            if (leakCount_bd > 0 && leakCount_bd != leakCount_bd_his)
            {
                needTips = true;
                leakCount_bd_his = leakCount_bd;
                AddTip("发现疑似泄露bundle:" + leakCount_bd + "个, 请使用ResLeakPrinter工具查看详情!");
            }

            if (leakCount_atlas > 0 && leakCount_atlas != leakCount_atlas_his)
            {
                needTips = true;
                leakCount_atlas_his = leakCount_atlas;
                AddTip("发现疑似泄露atlas:" + leakCount_atlas + "个, 请使用ResLeakPrinter工具查看详情!");
            }
            if (needTips)
            {
                UIAPI.ShowMsgTip("发现疑似资源泄露, 请使用ResLeakPrinter工具查看详情!");
                SetTipsVisible(needTips);
            }
        }
        #endregion


        #region 能源监控
        private CoreAffinityAsyncTaskBase TestCAATask = new CoreAffinityAsyncTaskBase();
        private string PowerInfo = string.Empty;
        private string Fps = "30";
        private bool EnableOutsideOpt = false;
        private string PerformanceButtom = "";
        private string PerformanceTop = "";
        void OnPowerTool()
        {
#if UNITY_ANDROID
            GUILayout.BeginVertical();
            {
                EnableOutsideOpt = GUILayout.Toggle(EnableOutsideOpt, "开启战斗外优化");
                KHPowerManager.Instance.EnableOutsideOpt = EnableOutsideOpt;

                GUILayout.Space(5);

                PerformanceButtom = KHPowerManager.Instance.PerformanceButtom.ToString();
                GUILayout.TextField("性能波谷", 50);
                PerformanceButtom = GUILayout.TextField(PerformanceButtom, 50);
                KHPowerManager.Instance.PerformanceButtom = int.Parse(PerformanceButtom);

                GUILayout.Space(5);

                PerformanceTop = KHPowerManager.Instance.PerformanceTop.ToString();
                GUILayout.TextField("性能峰值", 50);
                PerformanceTop = GUILayout.TextField(PerformanceTop, 50);
                KHPowerManager.Instance.PerformanceTop = int.Parse(PerformanceTop);

                GUILayout.TextField(this.PowerInfo, 50);
                if (SGUILayout.Button("获取电源信息"))
                {
                    var str = KHPowerManager.Instance.GetPowerSDKDescription();
                    this.PowerInfo = str;

                    UIAPI.ShowMsgTip(str);
                }

                if (SGUILayout.Button("获取核心信息"))
                {
                    var str = KHPowerManager.Instance.GetCoreDescription();
                    this.PowerInfo = str;

                    UIAPI.ShowMsgTip(str);
                }

                if (SGUILayout.Button("Saver"))
                {
                    bool ret = KHPowerManager.Instance.SetPowerPresetLevel(0);
                    UIAPI.ShowMsgTip(ret ? "成功" : "失败");
                }

                if (SGUILayout.Button("efficient"))
                {
                    bool ret = KHPowerManager.Instance.SetPowerPresetLevel(1);
                    UIAPI.ShowMsgTip(ret ? "成功" : "失败");
                }

                if (SGUILayout.Button("normal"))
                {
                    bool ret = KHPowerManager.Instance.SetPowerPresetLevel(2);
                    UIAPI.ShowMsgTip(ret ? "成功" : "失败");
                }

                if (SGUILayout.Button("burst"))
                {
                    bool ret = KHPowerManager.Instance.SetPowerPresetLevel(3);
                    UIAPI.ShowMsgTip(ret ? "成功" : "失败");
                }

                this.Fps = GUILayout.TextField(this.Fps, 20);
                if (SGUILayout.Button("帧率天花板"))
                {
                    var fps = Int32.Parse(this.Fps);
                    DEFINE.FPS = fps;
                    Application.targetFrameRate = fps;
                }

                //if (SGUILayout.Button("启动一个协程锁测试"))
                //{
                //    TestCAATask.Canceled = false;
                //    KHGlobalExt.StartCoroutine(TestCoroutineLock());
                //}

                //if (SGUILayout.Button("解开协程锁"))
                //{
                //    TestCAATask.Canceled = true;
                //}
            }
            GUILayout.EndVertical();
#endif
        }

        private IEnumerator TestCoroutineLock()
        {
            UIAPI.ShowMsgOK("开始");
            yield return TestCAATask.WaitFor;
            UIAPI.ShowMsgOK("完成");
        }
        #endregion

        static string recordFileDirForJerry = DefineExt.PVPRecordFile_LocalDIR;
        static string recordFileNameForJerry = "RecentSave";
        static void OnJerryTest()
        {
            PVPRecorder.EnableSF = GUILayout.Toggle(PVPRecorder.EnableSF, "秘境录像开关");
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(200);
                GUILayout.Label("录像文件目录:");
                recordFileDirForJerry = GUILayout.TextArea(recordFileDirForJerry);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(200);
                GUILayout.Label("录像文件名称:");
                recordFileNameForJerry = GUILayout.TextArea(recordFileNameForJerry);
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("播放秘境录像"))
            {
                PVPRecordManager.Instance.PlayLocalRecord(recordFileDirForJerry + recordFileNameForJerry, false, null);
            }

            if (GUILayout.Button("通过dump文件播放秘境录像"))
            {
                VerifyCommonCheckReq dump_arg;
                using (Stream file = File.OpenRead(recordFileDirForJerry + recordFileNameForJerry))
                {
                    dump_arg = ProtoBuf.Serializer.Deserialize<VerifyCommonCheckReq>(file);
                }
                string errinfo = "";
                VerifyTransData transData = PBSerializer.NDeserialize(dump_arg.trans_data, typeof(VerifyTransData)) as VerifyTransData;
                Debuger.Log("[dump文件结果] transData.win_group_id=" + transData.win_group_id);
                if (PVPRecordPlayer.Instance.Open(PBSerializer.NSerialize(dump_arg.record), out errinfo))
                {
                    PVPRecordPlayer.Instance.Play(false, 0);
                }
                else
                {
                    Debuger.LogError("[EnterPvpRealTime] PlayLocalRecord Error " + (string.IsNullOrEmpty(errinfo) ? "" : errinfo));
                }
            }

            if (GUILayout.Button("打开秘境"))
            {
                KHJumpSystemHelper.DoJump(SystemConfigDef.MysticalDuplicate, null);
            }

            if (GUILayout.Button("进入积分赛1"))
            {
                string filePath = Application.persistentDataPath + "/PVPScoreRecord/" + recordFileNameForJerry;
                using (Stream file = File.OpenRead(filePath))
                {
                    var ReplayData = ProtoBuf.Serializer.Deserialize<PvPReplayInfo>(file);
                    KHPluginManager.Instance.SendMessage(PvPUIPlugin2.PluginName, "DirectlyEnterPvpFight", ReplayData);
                }
            }

            if (GUILayout.Button("进入积分赛2"))
            {
                string filePath = Application.persistentDataPath + "/PVPScoreRecord/" + recordFileNameForJerry;
                object arg;
                byte[] buffer = FileUtils.getFileBytes(filePath);
                arg = PBSerializer.NDeserialize(buffer, typeof(VerifyWdjCheckReq));
                VerifyWdjCheckReq req2 = arg as VerifyWdjCheckReq;
                PvPReplayInfo ReplayData = null;
                VerifyWdjCheckReq _req = req2 as VerifyWdjCheckReq;
                try
                {
                    ReplayData = PBSerializer.NDeserialize<PvPReplayInfo>(_req.verify_data);
                }
                catch (ProtoBuf.ProtoException ex)
                {
                    Debug.LogError("ProtoBuf Exception: " + ex.ToString());
                }
                if (ReplayData != null)
                {
                    KHPluginManager.Instance.SendMessage(PvPUIPlugin2.PluginName, "DirectlyEnterPvpFight", ReplayData);
                }
            }
            GUILayout.Space(10);
            if (!DefineExt.IsSlimVersion)
            {
                if (GUILayout.Button("开启精简版本开关"))
                {
                    DefineExt.IsSlimVersion = true;
                    KH.Slim.SlimGameInit.SlimGameApp.initialize();
                }
            }
            else
            {
                if (GUILayout.Button("关闭精简版本开关"))
                {
                    DefineExt.IsSlimVersion = false;
                }
            }
            GUILayout.Label("SlimClientPlaySpeed = " + Slim.SlimDefine.SlimClientPlaySpeed);
            Slim.SlimDefine.SlimClientPlaySpeed = (int)GUILayout.HorizontalSlider((float)Slim.SlimDefine.SlimClientPlaySpeed, 1, 1800);
        }

        #region 设备模拟
        static string tDeviceSilmName = DEFINE.COMMON_SIMULATION_DEVICE;

        // 开启设备模拟函数
        static Action<string> SimulationDevice = (deviceName) =>
        {
            DEFINE.COMMON_SIMULATION_DEVICE = deviceName;
            if (KHDataManager.getInstance().HasLoaded(KHDeviceAdapterManager.ConfigPath))
                KHDeviceAdapterManager.Instance.RefreshCurrentDeviceData();
            else
                KHConfigReleaseManager.Instance.LoadConfigByte(KHDeviceAdapterManager.ConfigPath);
        };

        // 添加通用刘海/挖孔函数
        static Action<string> AddSimulation = (simName) =>
        {
            var root = GameObject.Find("KHDebugerGUI(Clone)");
            if (root != null)
            {
                var boundInst = GameObject.Find(simName);
                if (boundInst == null)
                {
                    var bound = KHResource.LoadResSync(simName);
                    boundInst = GameObject.Instantiate(bound) as GameObject;

                    boundInst.name = simName;
                    boundInst.transform.parent = root.transform;
                    boundInst.transform.localScale = Vector3.one;
                    DontDestroyOnLoad(boundInst);
                }
            }
        };

        // 关闭通用刘海/挖孔函数
        static Action<string> RemoveSimulation = (simName) =>
        {
            var boundInst = GameObject.Find(simName);
            if (boundInst != null)
            {
                GameObject.Destroy(boundInst);
            }
        };

        static void OnDeviceSimulation()
        {
            GUILayout.BeginVertical();
            {
                tDeviceSilmName = GUILayout.TextField(tDeviceSilmName, 50);
                GUILayout.Label("当前设备: " + DEFINE.COMMON_SIMULATION_DEVICE);
                if (SGUILayout.Button("开启设备模拟"))
                {
                    SimulationDevice(tDeviceSilmName);
                }

                if (SGUILayout.Button("关闭设备模拟"))
                {
                    tDeviceSilmName = "";
                    SimulationDevice(string.Empty);
                }

                if (SGUILayout.Button("快速开启设备模拟-IPX"))
                {
                    tDeviceSilmName = "iPhone10,3";
                    SimulationDevice(tDeviceSilmName);
                    AddSimulation(UIDef.BoundSimulationView);
                }

                if (SGUILayout.Button("快速开启设备模拟-三星S10+"))
                {
                    tDeviceSilmName = "samsung SM-G9750";
                    SimulationDevice("samsung SM-G9750");
                    AddSimulation(UIDef.HoleSimulationView);
                }

                GUILayout.Label("##################################");

                if (SGUILayout.Button("添加通用刘海"))
                {
                    AddSimulation(UIDef.BoundSimulationView);
                }
                if (SGUILayout.Button("关闭通用刘海"))
                {
                    RemoveSimulation(UIDef.BoundSimulationView);
                }

                GUILayout.Label("##################################");

                if (SGUILayout.Button("添加通用挖孔"))
                {
                    AddSimulation(UIDef.HoleSimulationView);
                }

                if (SGUILayout.Button("关闭通用挖孔"))
                {
                    RemoveSimulation(UIDef.HoleSimulationView);
                }
            }
            GUILayout.EndVertical();
        }

        #endregion

        #region 玩家时间
        static void OnShowPlayerTime()
        {
            GUILayout.Label("player svr time:");
            GUILayout.Label("" + RemoteModel.Instance.CurrentDateTime);
            GUILayout.Label("" + RemoteModel.Instance.CurrentTime);

            GUILayout.Space(30);

            if (GUILayout.Button("RemovePlugin"))
            {
                KHPluginManager.Instance.Tool__RemovePlugin("");
            }
        }
        #endregion

        #region 3D招募测试
        private static string Recruit_playNinjaId = "0";
        static void On3DRecruitTest()
        {
            var cfg = GeneralTableConfig.getInstance().GetRecruit3DAnimCfgsDict();

            GUILayout.Label("3D高招忍者改版");

            GUILayout.BeginHorizontal();
            GUILayout.Label("3d动画忍者:");
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            if (cfg != null && cfg.Count > 0)
            {
                foreach (Recruit3DAnimCfg cfgitem in cfg.Values)
                {
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("忍者ID: " + cfgitem.playNinjaId + " 在主包: " + cfgitem.inBase + " 星级 > " + cfgitem.ninjaStar);
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("配置为空, 无法设置");
            }



            GUILayout.BeginHorizontal();
            GUILayout.Label("是否强制播放:" + (DefineExt.Recruit_Play3dAnim == 1 ? "开" : "关"));
            if (GUILayout.Button(DefineExt.Recruit_Play3dAnim == 1 ? "关" : "开"))
            {
                DefineExt.Recruit_Play3dAnim = 1 - DefineExt.Recruit_Play3dAnim;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("填写需要播放3D高招动画忍者ID");
            GUILayout.Label("ninjaId: ");
            Recruit_playNinjaId = GUILayout.TextField(Recruit_playNinjaId);
            int playNinjaId = 0;
            int.TryParse(Recruit_playNinjaId, out playNinjaId);

            GUILayout.Label("最终播放:   " + playNinjaId);
            GUILayout.Space(10);
            GUILayout.Space(10);
            GUILayout.Space(10);
            if (GUILayout.Button("----直接显示招募动画界面-----"))
            {
                if (cfg.ContainsKey(playNinjaId))
                {
                    KHPluginManager.Instance.SendMessage(SharePlugin.PLUGIN_NAME, "Share.RecruitNinjaShowV2", RemoteModel.Instance.NinjaCollection.GetNinjaData(playNinjaId, false));
                    return;
                }
                else
                {
                    UIAPI.ShowMsgTip("资源不存在");
                }
            }



        }
        #endregion

        #region 黑鲨手机
        static string blacksharkID = "1";
        static BlackSharkMgr.SHARK_MOD blacksharkMod = BlackSharkMgr.SHARK_MOD.BOSS_APPEAR;
        void BlackSharkTest()
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("调起黑鲨事件"))
            {
                BlackSharkMgr.Instance().CallBSEvent(blacksharkMod);
            }

            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            {
                GUILayout.Space(200);
                GUILayout.Label("黑鲨事件ID:");
                blacksharkID = GUILayout.TextField(blacksharkID);
                int id = 0;
                if (int.TryParse(blacksharkID, out id))
                {
                    blacksharkMod = (BlackSharkMgr.SHARK_MOD)id;
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (GUILayout.Button("黑鲨开关 : " + (BlackSharkMgr.Switcher ? "开" : "关")))
            {
                BlackSharkMgr.Switcher = !BlackSharkMgr.Switcher;
            }

            GUILayout.EndVertical();
        }
        #endregion

        #region 3D角色展示
        private static string ShowNinjaId = "90902";
        private static string lastNinja3dPath = "";
        private static Transform Ninja3dParent = null;
        private static Transform PortraitContainer = null;
        private static bool ninjaShowFlag = false;
        void Ninja3DShow()
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("展示界面"))
            {
                KHPluginManager.Instance.GetPluginByName(AppreciatePlugin.pluginName).SendMessage(AppreciateOperation.QueryViewNinja);
                GameObject disroot = GameObject.Find("Appreciate/AppreciateNinjaView");
                if (disroot != null)
                {
                    Transform BGPanel = disroot.transform.Find("ContainerAnimator/BGPanel");
                    GameObject go = GameObject.Instantiate(KHResource.LoadResSync("UI/Portrait_3D/TestNode")) as GameObject;
                    go.transform.parent = BGPanel.transform;
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localScale = 320 * Vector3.one;
                    Ninja3dParent = disroot.transform.Find("ContainerAnimator/BGPanel/TestNode(Clone)/NinjaNode");
                    PortraitContainer = disroot.transform.Find("ContainerAnimator/BGPanel/NinjaContainer");
                    NGUITools.SetActive(PortraitContainer.gameObject, false);
                }
                ninjaShowFlag = true;
            }
            GUILayout.EndVertical();

            if (ninjaShowFlag)
            {
                GUILayout.BeginVertical();
                GUILayout.Label("填写3D忍者ID");
                GUILayout.Label("ninjaId: ");
                ShowNinjaId = GUILayout.TextField(ShowNinjaId);
                int playNinjaId = 0;
                int.TryParse(ShowNinjaId, out playNinjaId);

                GUILayout.Label("展示3D忍者:   " + playNinjaId);
                GUILayout.Space(10);
                GUILayout.Space(10);
                GUILayout.Space(10);
                if (GUILayout.Button("----展示-----"))
                {
                    var Ninja3dPath = KHUtil.getPortrait3DModelPath(ShowNinjaId);
                    if (Ninja3dPath != lastNinja3dPath)
                    {
                        KHResource.LoadRes(Ninja3dPath, on3dNinjaLoaded);
                    }
                }
                GUILayout.EndVertical();
            }

            GUILayout.BeginVertical();
            if (GUILayout.Button(KHQualityManager.ENABLE_SOFT_ANTIALIASING ? "关闭软抗" : "开启软抗"))
            {
                KHQualityManager.ENABLE_SOFT_ANTIALIASING = !KHQualityManager.ENABLE_SOFT_ANTIALIASING;
            }
            if (GUILayout.Button(QualitySettings.antiAliasing > 0 ? "关闭硬抗" : "开启硬抗"))
            {
                QualitySettings.antiAliasing = 2 - QualitySettings.antiAliasing;
            }
            GUILayout.EndVertical();
        }

        private void on3dNinjaLoaded(string url, UnityEngine.Object obj, LOADSTATUS result, object ext)
        {
            if (!string.IsNullOrEmpty(lastNinja3dPath))
            {
                KHResource.unLoadRes(lastNinja3dPath, on3dNinjaLoaded);
                lastNinja3dPath = "";
            }
            lastNinja3dPath = url;
            if (result == LOADSTATUS.LOAD_SECCUSS)
            {
                if (Ninja3dParent != null)
                {
                    for (int i = 0; i < Ninja3dParent.childCount; i++)
                    {
                        Destroy(Ninja3dParent.transform.GetChild(0).gameObject);
                    }
                    GameObject go = GameObject.Instantiate(obj) as GameObject;
                    go.transform.parent = Ninja3dParent.transform;
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localScale = Vector3.one;
                }
            }
        }
        #endregion

        #region openURLDebug
        static void OnDebugOpenURL()
        {
            GUILayout.Label("这里修改会影响OpenURL后面添加的参数, 不需要修改的字段留空就可以");
            GUILayout.Label("openid: ");
            OpenURLDebugInfo.OPENID = GUILayout.TextField(OpenURLDebugInfo.OPENID);
            GUILayout.Label("partition: ");
            OpenURLDebugInfo.PARTITION = GUILayout.TextField(OpenURLDebugInfo.PARTITION);
            GUILayout.Label("roleid: ");
            OpenURLDebugInfo.GID = GUILayout.TextField(OpenURLDebugInfo.GID);
            GUILayout.Label("access_token: ");
            OpenURLDebugInfo.ACCESS_TOKEN = GUILayout.TextField(OpenURLDebugInfo.ACCESS_TOKEN);
        }
        #endregion

        #region 忍界大战UI测试
        static public int ninkaiTaisenPlotID = 17;
        static void OnNinkaiTaisenUITest()
        {
            ninkaiTaisenPlotID = int.Parse(GUILayout.TextArea(ninkaiTaisenPlotID.ToString()));

            NinkaiTaisenLevelSelectModel.FirstClickAlawasToDungeon = GUILayout.Toggle(NinkaiTaisenLevelSelectModel.FirstClickAlawasToDungeon, "点卷轴一定进剧情关");

            if (GUILayout.Button("打开一章剧情副本"))
            {
                KHPluginManager.Instance.SendMessage(NinkaiTaisenLevelSelectPlugin.pluginName, NinkaiTaisenLevelSelectOperation.OP_OpenLevelSelectView, ninkaiTaisenPlotID);
            }

            if (GUILayout.Button("打开战场之外"))
            {
                KHPluginManager.Instance.SendMessage(NinkaiTaisenLevelSelectPlugin.pluginName, NinkaiTaisenLevelSelectOperation.OP_OpenPlotView);
            }

            if (GUILayout.Button("清除剧情副本的本地记录"))
            {
                NinkaiTaisenLevelSelectModel _model = KHPluginManager.Instance.GetModel(NinkaiTaisenLevelSelectPlugin.pluginName) as NinkaiTaisenLevelSelectModel;
                _model.ClearEnterPlotPref();
            }

            if (GUILayout.Button("跳转-直接打开某一章"))
            {
                NinkaiTaisenLevelSelectModel.BackToMainInfo arg = new NinkaiTaisenLevelSelectModel.BackToMainInfo();
                arg.jumpType = NinkaiTaisenLevelSelectModel.JumpType.Normal;
                arg.jumpData = ninkaiTaisenPlotID;
                KHPluginManager.Instance.SendMessage(NinkaiTaisenLevelSelectPlugin.pluginName, NinkaiTaisenLevelSelectOperation.OP_BackToMainMap, arg);
            }

            if (GUILayout.Button("跳转-直接打开剧情副本"))
            {
                NinkaiTaisenLevelSelectModel.BackToMainInfo arg = new NinkaiTaisenLevelSelectModel.BackToMainInfo();
                arg.jumpType = NinkaiTaisenLevelSelectModel.JumpType.Plot;
                KHPluginManager.Instance.SendMessage(NinkaiTaisenLevelSelectPlugin.pluginName, NinkaiTaisenLevelSelectOperation.OP_BackToMainMap, arg);
            }

        }

        #endregion

        #region 3D剧情测试
        static public string qteGroupName = "ZJvsB/ZJvsB_QTE01";
        static public string qteName = "QTE01";
        static public float cgDepth = 5.0f;
        static public string cgDepthStr = "5";

        static public string cgName = "CGRecruit";
        static public string widthScaleStr = "1";
        static public string heightScaleStr = "1";
        static public float widthScale = 1;
        static public float heightScale = 1;

        static public string xPosStr = "0";
        static public string yPosStr = "0";
        static public int xPos = 0;
        static public int yPos = 0;
        static void On3DPlotTest()
        {
            qteGroupName = GUILayout.TextField(qteGroupName);
            qteName = GUILayout.TextField(qteName);
            cgDepthStr = GUILayout.TextArea(cgDepthStr);

            cgName = GUILayout.TextField(cgName);
            widthScaleStr = GUILayout.TextField(widthScaleStr);
            heightScaleStr = GUILayout.TextField(heightScaleStr);
            xPosStr = GUILayout.TextField(xPosStr);
            yPosStr = GUILayout.TextField(yPosStr);


            if (GUILayout.Button("设置相机偏移-Set3DCameraOffsetRate"))
            {
                if (float.TryParse(cgDepthStr, out cgDepth))
                {
                    string strFuncName = "_G_TS_" + "Set3DCameraOffsetRate";
                    List<string> lstArgs = new List<string>();

                    lstArgs.Add(cgDepth.ToString());
                    CallGlobalLuaFunctionHandle(lstArgs, strFuncName);
                }
            }

            if (GUILayout.Button("设置3D场景重力-(负数, 默认是-0.0222)"))
            {
                if (float.TryParse(cgDepthStr, out cgDepth))
                {
                    //KHEnvironmentInfo.G_GRAVITY = cgDepth;
                    string strFuncName = "_G_TS_" + "Set3DSceneGravity";
                    List<string> lstArgs = new List<string>();
                    lstArgs.Add(cgDepth.ToString());
                    CallGlobalLuaFunctionHandle(lstArgs, strFuncName);
                }
            }

            //if (GUILayout.Button("设置摩擦系数-(默认是0.0222)"))
            //{
            //    if (float.TryParse(cgDepthStr, out cgDepth))
            //    {
            //        KHEnvironmentInfo.G_FRICTION = cgDepth;
            //    }
            //}

            if (GUILayout.Button("重置场景的相机-HideSceneCameraFor20002"))
            {
                if (float.TryParse(cgDepthStr, out cgDepth))
                {
                    string strFuncName = "_G_TS_" + "HideSceneCameraFor20002";
                    List<string> lstArgs = new List<string>();
                    CallGlobalLuaFunctionHandle(lstArgs, strFuncName);
                }
            }

            if (GUILayout.Button("设置场景bloom效果-SetSceneCameraBloom"))
            {
                if (float.TryParse(cgDepthStr, out cgDepth))
                {
                    string strFuncName = "_G_TS_" + "SetSceneCameraBloom";
                    List<string> lstArgs = new List<string>();
                    lstArgs.Add(cgDepth.ToString());
                    lstArgs.Add("0.2");
                    lstArgs.Add("0.7");
                    lstArgs.Add("1.0");
                    lstArgs.Add("2");

                    CallGlobalLuaFunctionHandle(lstArgs, strFuncName);
                }
            }

            if (GUILayout.Button("播放一段GC"))
            {
                if (float.TryParse(heightScaleStr, out heightScale) && float.TryParse(widthScaleStr, out widthScale) && int.TryParse(xPosStr, out xPos) && int.TryParse(yPosStr, out yPos))
                {
                    //KHMovieManager.getInstance().NewPlayMovie(cgName, 0, null, 1, cgDepth, null);
                    KHMovieManager.getInstance().PlayMovieByParam(cgName, 0, widthScale, heightScale, xPos, yPos, false);
                }
            }
            if (GUILayout.Button("播放一段QTE"))
            {
                KHFluxQTEManager.getInstance().Play(qteGroupName, qteName, QTEStartPlayFunc, QTEEndPlayFunc);
            }
            if (GUILayout.Button("设置场景的雾浓度-Set3DSceneFogIntensity"))
            {
                if (float.TryParse(cgDepthStr, out cgDepth))
                {
                    string strFuncName = "_G_TS_" + "Set3DSceneFogIntensity";
                    List<string> lstArgs = new List<string>();
                    lstArgs.Add(cgDepth.ToString());
                    CallGlobalLuaFunctionHandle(lstArgs, strFuncName);
                }
            }
            //if (GUILayout.Button("执行lua脚本-Play3DSceneFlux"))
            //{
            //    string strFuncName = "_G_TS_" + "Play3DSceneFlux";
            //    List<string> lstArgs = new List<string>();
            //    lstArgs.Add("False");
            //    lstArgs.Add(qteGroupName);
            //    lstArgs.Add(qteName);
            //    CallGlobalLuaFunctionHandle(lstArgs, strFuncName);
            //}
            //if (GUILayout.Button("执行lua脚本-Stop3DSceneFlux"))
            //{
            //    string strFuncName = "_G_TS_" + "Play3DSceneFlux";
            //    List<string> lstArgs = new List<string>();
            //    lstArgs.Add("True");
            //    lstArgs.Add("");
            //    lstArgs.Add("");
            //    CallGlobalLuaFunctionHandle(lstArgs, strFuncName);
            //}
            if (GUILayout.Button("执行lua脚本-PlayFluxQTE"))
            {
                string strFuncName = "_G_TS_" + "PlayFluxQTE";
                List<string> lstArgs = new List<string>();
                lstArgs.Add("False");
                lstArgs.Add(qteGroupName);
                lstArgs.Add(qteName);
                CallGlobalLuaFunctionHandle(lstArgs, strFuncName);
            }
            if (GUILayout.Button("执行lua脚本-StopFluxQTE"))
            {
                string strFuncName = "_G_TS_" + "PlayFluxQTE";
                List<string> lstArgs = new List<string>();
                lstArgs.Add("True");
                lstArgs.Add("");
                lstArgs.Add("");
                CallGlobalLuaFunctionHandle(lstArgs, strFuncName);
            }
            if (GUILayout.Button("执行lua脚本-HideMainCamera-hide"))
            {
                string strFuncName = "_G_TS_" + "HideMainCamera";
                List<string> lstArgs = new List<string>();
                lstArgs.Add("False");
                CallGlobalLuaFunctionHandle(lstArgs, strFuncName);
            }
            if (GUILayout.Button("执行lua脚本-HideMainCamera-show"))
            {
                string strFuncName = "_G_TS_" + "HideMainCamera";
                List<string> lstArgs = new List<string>();
                lstArgs.Add("True");
                CallGlobalLuaFunctionHandle(lstArgs, strFuncName);
            }
            if (GUILayout.Button("执行lua脚本-SetMainCameraParamImmediately"))
            {
                string strFuncName = "_G_TS_" + "SetMainCameraParamImmediately";
                List<string> lstArgs = new List<string>();
                lstArgs.Add("False");
                lstArgs.Add("1,2,3");
                lstArgs.Add("True");
                lstArgs.Add("4,5,6");
                lstArgs.Add("True");
                lstArgs.Add("6.66");
                CallGlobalLuaFunctionHandle(lstArgs, strFuncName);
            }
            if (GUILayout.Button("设置变量-成功QTE参数"))
            {
                string strFuncName = "_G_TS_" + "SetVariable_KVPair";
                List<string> lstArgs = new List<string>();
                lstArgs.Add("NextQTEParam");
                lstArgs.Add("Qte_part01,QTE_SUCCESS");
                CallGlobalLuaFunctionHandle(lstArgs, strFuncName);
            }
            if (GUILayout.Button("设置变量-失败QTE参数"))
            {
                string strFuncName = "_G_TS_" + "SetVariable_KVPair";
                List<string> lstArgs = new List<string>();
                lstArgs.Add("NextQTEParam");
                lstArgs.Add("Qte_part01,QTE_FAILED");
                CallGlobalLuaFunctionHandle(lstArgs, strFuncName);
            }
            if (GUILayout.Button("隐藏场景元素-QTEGroupName"))
            {
                string strFuncName = "_G_TS_" + "SetMainSceneObjectActive";
                List<string> lstArgs = new List<string>();
                lstArgs.Add("false");
                lstArgs.Add(qteGroupName);
                CallGlobalLuaFunctionHandle(lstArgs, strFuncName);
            }
            if (GUILayout.Button("显示场景元素-QTEGroupName"))
            {
                string strFuncName = "_G_TS_" + "SetMainSceneObjectActive";
                List<string> lstArgs = new List<string>();
                lstArgs.Add("true");
                lstArgs.Add(qteGroupName);
                CallGlobalLuaFunctionHandle(lstArgs, strFuncName);
            }
        }

        public static void CallGlobalLuaFunctionHandle(List<string> lstArgs, string strFuncName)
        {
            object[] objs = new object[lstArgs.Count + 1];
            objs[0] = null;
            for (int i = 0; i < lstArgs.Count; i++)
            {
                objs[i + 1] = lstArgs[i];
            }
            RuntimeLua.Instance.CallGlobalLuaFunction(strFuncName, objs);
        }

        static void QTEStartPlayFunc()
        {
            Debuger.Log("QTEStartPlayFunc");
        }

        static void QTEEndPlayFunc()
        {
            Debuger.Log("QTEEndPlayFunc");
        }
        #endregion



        #region 百忍分享

        static void OnHNShare()
        {
            if (GUILayout.Button("打开分享界面"))
            {
                var hnwModel = KHPluginManager.Instance.GetModel(HundredNinjaWarPlugin.pluginName) as HundredNinjaWarModel;
                hnwModel.GameEndNtf = new SceneGroupFightGameEndNtf();
                hnwModel.GameEndNtf.kill_num = 100;
                hnwModel.GameEndNtf.live_time = 200;
                hnwModel.GameEndNtf.rank = 1;
                hnwModel.GameEndNtf.pick_ninja_id = 90001001;
                KHPluginManager.Instance.SendMessage(HundredNinjaWarPlugin.pluginName, "ShowView", new ShowViewArgument(title: UIDef.HUNRED_NINJA_WAR_SHARE_VIEW));

                var model = KHPluginManager.Instance.GetPluginByName("Boot").Model as LoginModel;
                int zoneid = model.selected_zoneid % 10000;
                UIAPI.ShowMsgTip(KHUtilForLua.GidToZoneServerName(RemoteModel.Instance.Player.Gid.ToString()) + " " + zoneid.ToString() + "区 " + model.selected_zoneName);
                //UIAPI.ShowMsgTip(WWW.EscapeURL(RemoteModel.Instance.Player.playerAvatarUrl.Replace("https", "http")));
            }

            if (GUILayout.Button("检查FireParticleHightLight"))
            {
                var go = GameObject.Find("FireParticleHightLight");
                string s = "";
                if (go != null)
                {
                    //遍历当前物体及其所有子物体
                    Renderer r = go.transform.GetComponent<Renderer>();
                    if (r != null)
                    {
                        s += "ShaderName:" + r.material.shader.name + "##";
                        Material[] list = r.materials;

                        for (int i = 0; i < list.Length; i++)
                        {
                            if (list[i].HasProperty("_Stencil"))
                                s += "ShaderID" + list[i].GetFloat("_Stencil") + "##";
                        }
                    }
                }


                var goo = GameObject.Find("khstencilmask");
                if (goo != null)
                {
                    var temp = goo.transform.parent.GetComponent<SetShaderStencil>();
                    if (temp != null)
                    {
                        s += "MaskID:" + ShaderStencilManager.getInstance().getStencil(temp.objName);

                    }

                }
                UIAPI.ShowMsgTip(s);
                Debuger.LogError(s);
            }
            if (GUILayout.Button("Ark模版3"))
            {
                var share_handle = new SnsShareHandle();
                var template = new ArkTemplate3("火影忍者：百忍大战",
                    "超燃百忍大战一触即发！你敢来挑战么",
                    "本场战绩",
                    "淘汰人数", "1",
                    "存活时长", "2分20秒",
                    "本场名次", "33",
                    "http://dlied5.qq.com/kihan/app/icon2.png",
                    "火影忍者：百忍大战",
                    "超燃百忍大战一触即发！你敢来挑战么",
                    "火影叫你来吃鸡！",
                    "74",
                    "http://dlied5.qq.com/kihan/ark/bairendazhan.png");
                share_handle.ShareToQSessionArk(template.GetArkData());

            }
            if (GUILayout.Button("Ark模版7"))
            {
                var share_handle = new SnsShareHandle();
                var template = new ArkTemplate7(
                    "_Title", "_Desc",
                    "http://dlied5.qq.com/kihan/app/icon2.png",
                    "火影忍者三周年庆盛大开启", "百万玩家同庆！我在木叶等你！",
                    "【火影忍者】3周年庆盛大开启", "68",
                    "http://dlied5.qq.com/kihan/ark/bairendazhan.png");
                share_handle.ShareToQSessionArk(template.GetArkData());
            }

            if (GUILayout.Button("开关头像框保护"))
            {
                if (DefineExt.UsePFProtect)
                {
                    DefineExt.UsePFProtect = false;
                    UIAPI.ShowMsgTip("已关闭头像框保护");
                }
                else
                {
                    DefineExt.UsePFProtect = true;
                    UIAPI.ShowMsgTip("已开启头像框保护");
                }
            }

            if (GUILayout.Button("现在时间"))
            {
                UIAPI.ShowMsgTip(RemoteModel.Instance.CurrentDateTime.ToString());
            }
            GUILayout.BeginHorizontal();
            {
                contentTypeName = GUILayout.TextField(contentTypeName);
                if (GUILayout.Button("解析时间(旧)"))
                {
                    var temp = contentTypeName.Split('.');
                    if (temp.Length >= 4)
                    {
                        var PFDateTime = new DateTime(Int32.Parse(temp[0]), Int32.Parse(temp[1]), Int32.Parse(temp[2]), Int32.Parse(temp[3]), 0, 0, DateTimeKind.Utc);
                        var LocaclTime = new DateTime(Int32.Parse(temp[0]), Int32.Parse(temp[1]), Int32.Parse(temp[2]), Int32.Parse(temp[3]), 0, 0);
                        UIAPI.ShowMsg_OK_Close("Raw:" + PFDateTime.ToShortDateString() + " " + PFDateTime.ToLongTimeString() + "\nToUTC:" + PFDateTime.ToUniversalTime().ToShortDateString() + " " + PFDateTime.ToUniversalTime().ToLongTimeString() + "\nLocalRaw:" + LocaclTime.ToShortDateString() + " " + LocaclTime.ToLongTimeString() + "\nLocalToUTC:" + LocaclTime.ToUniversalTime().ToShortDateString() + " " + LocaclTime.ToUniversalTime().ToLongTimeString(), "OK", null);
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                TimeData = GUILayout.TextField(TimeData);
                if (GUILayout.Button("解析时间(新)"))
                {

                    DateTime date;
                    if (DateTime.TryParse(TimeData + "Z", out date))
                    {
                        UIAPI.ShowMsg_OK_Close("Raw:" + date.ToShortDateString() + " " + date.ToLongTimeString() + "\nToUTC:" + date.ToUniversalTime().ToShortDateString() + " " + date.ToUniversalTime().ToLongTimeString(), "tt", null);
                    }
                }
            }
            GUILayout.EndHorizontal();

        }
        private static string TimeData = "";
        private static string contentTypeName = "";

        #endregion

        #region 巅峰对决

        static void OnUK()
        {
            if (GUILayout.Button("打开选拔赛"))
            {
                KHPluginManager.Instance.SendMessage(UltimateKillPlugin.pluginName, UltimateKillSceneOperation.EnterXuanBaSaiScene);
            }
            if (GUILayout.Button("打开冠军宣言"))
            {
                KHPluginManager.Instance.SendMessage(UltimateKillPlugin.pluginName, "ShowView", new ShowViewArgument(UIDef.ULTIMATE_KILL_FINISHED_VIEW));
            }
            if (GUILayout.Button("打开活动结束"))
            {

            }

            if (GUILayout.Button("打开本服出征"))
            {

            }
            if (GUILayout.Button("选拔赛设置设置阵容"))
            {
                KHPluginManager.Instance.SendMessage(UltimateKillPlugin.pluginName, UltimateKillXuanBaSaiOperation.OpenChangeView);
            }
            if (GUILayout.Button("打开输入界面"))
            {
                InputWinArgument argument = new InputWinArgument(19, "ZhuFu", (string a) => { UIAPI.ShowMsgTip(a); });
                KHPluginManager.Instance.SendMessage(UltimateKillPlugin.pluginName, "ShowView", new ShowViewArgument(UIDef.UK_INPUT_WINDOW, false, argument));
            }
            if (GUILayout.Button("随机100"))
            {
                UIAPI.ShowMsgTip(UnityEngine.Random.Range(1, 101).ToString());
            }
            if (GUILayout.Button("粉丝榜"))
            {
                ulong gid = 0;
                KHPluginManager.Instance.SendMessage(UltimateKillPlugin.pluginName, UltimateKillDevoteOperation.OpenFansView, gid);
            }
            if (GUILayout.Button("助威"))
            {
                KHPluginManager.Instance.SendMessage(UltimateKillPlugin.pluginName, UltimateKillDevoteOperation.OpenDevoteView);
            }
            if (GUILayout.Button("竞猜"))
            {
                KHPluginManager.Instance.SendMessage(UltimateKillPlugin.pluginName, UltimateKillDevoteOperation.OpenQuizView);
            }
            if (GUILayout.Button("0错误码"))
            {
                var _errormsg = ErrorCodeCenter.GetErrorStringEx(0, "");
                UIAPI.alert("提示", _errormsg, 0, null, null, true);

            }
            if (GUILayout.Button("拉一次巅峰对决气泡"))
            {
                KHPluginManager.Instance.SendMessage(UltimateKillPlugin.pluginName, UltimateKillOperation.UKBubble);
            }

            if (GUILayout.Button("点击技能"))
            {

                GameObject go = GameObject.Find(UINewUserGuideMainView.handFocus.tag);
                go = go == null ? GameObject.FindGameObjectWithTag(UINewUserGuideMainView.handFocus.tag) : go;
                if (go != null)
                {
                    var button = go.GetComponentInChildren<SpellSlot>();
                    if (button != null)
                    {
                        button.OnKHHover(false);
                    }
                }
            }

            if (GUILayout.Button("打开防沉迷"))
            {
                ZoneAntiAddictionNtf zoneAntiAddictionNtfResp = new ZoneAntiAddictionNtf();
                KHUIManager.getInstance().OpenWindow(UIDef.ANTI_ADDICTION_NTF, _data: zoneAntiAddictionNtfResp);

            }
            if (GUILayout.Button("打开新手"))
            {
                GuideToClickArg arg = new GuideToClickArg();
                arg.gameObjectTabName = "PlayerBar.PVE";
                arg.isShow = true;
                arg.showMask = true;
                arg.needMask = true;
                arg.featureID = 17;
                object[] winArg = new object[] { 2, arg };
                KHPluginManager.Instance.SendMessage(KH.Plugins.NewUserGuidePlugin.NAME, "ShowView", new ShowViewArgument()
                {
                    title = UIDef.NEW_USER_GUIDE_MAIN_VIEW,
                    data = winArg
                });

            }
            if (GUILayout.Button("TopWindow"))
            {
                var w = KHUIManager.getInstance().GetTopWindow();
                if (w != null)
                {
                    UIAPI.ShowMsgOK(w.name);
                }
            }
        }


        #endregion

        #region 弱网模拟
        static void OnWeakNetSimulate()
        {
            NetworkManager.WeakNetSimu_IsPackageLoss_Up = GUILayout.Toggle(NetworkManager.WeakNetSimu_IsPackageLoss_Up, "模拟上行丢包");
            GUILayout.Label("模拟上行丢包率(%):" + (int)(NetworkManager.WeakNetSimu_PackageLossRate_Up * 100));
            NetworkManager.WeakNetSimu_PackageLossRate_Up = GUILayout.HorizontalSlider((float)NetworkManager.WeakNetSimu_PackageLossRate_Up, 0, 1);

            NetworkManager.WeakNetSimu_IsPackageLoss_Down = GUILayout.Toggle(NetworkManager.WeakNetSimu_IsPackageLoss_Down, "模拟下行丢包");
            GUILayout.Label("模拟下行丢包率(%):" + (int)(NetworkManager.WeakNetSimu_PackageLossRate_Down * 100));
            NetworkManager.WeakNetSimu_PackageLossRate_Down = GUILayout.HorizontalSlider((float)NetworkManager.WeakNetSimu_PackageLossRate_Down, 0, 1);
        }
        #endregion
        #region luaproto加载统计
        static void OnLuaProtoStats()
        {
            int protoNum = -1;
            List<string> protoList = new List<string>();

            LuaInterface.LuaTable tmpTable = RuntimeLua.VM.GetTable("already_loaded_pb");
            if (tmpTable != null)
            {
                protoNum = tmpTable.Count;

                GUILayout.Label("已加载的LuaProto数量:" + protoNum);
                foreach (string key in tmpTable.Keys)
                {
                    GUILayout.Label(key);
                }
            }
            else
            {
                GUILayout.Label("无LuaProto被加载");
            }
        }
        #endregion
    }


}

