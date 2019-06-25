using System;
using System.Collections;
using System.Collections.Generic;
using naruto.protocol;
using Tss;
using UnityEngine;
using KH.Network;
using GameJoyAPI;
using KH.Lua;
using KH.Workflows;
using LuaInterface;
using KH.Plugins;
using KH.PVPRecord;

namespace KH
{

    class SysSetting
    {
        public bool _IsBaseQualityDevice = false;
        public bool _Enabled_Surface_dust = true;
        public bool _AllowPlayCG = false;
        public bool _AllowPauseBattle = true;
        public int _PVP_DEFAULT_PLAY_SPEED = 1; //这个不需要暂存
        public bool _KillActorEffect = true;
    }

    /// <summary>
    /// 把一个pvptype映射到其他的各类type以适应复杂需求
    /// </summary>
    public class PvpTypeConverter
    {
        /// <summary>
        /// 201: 锦标赛-积分赛-决斗场模式
        /// 202: 锦标赛-积分赛-无差别模式
        /// </summary>

        private readonly Dictionary<int, int> _loadingUiMap = new Dictionary<int, int>()
        {
            {201, 5},
            {202, 5},
        };
        private readonly Dictionary<int, int> _loadingTypeMap = new Dictionary<int, int>()
        {
            {201, 6},
            {202, 6},
        };
        private readonly Dictionary<int, int> _battleUiMap = new Dictionary<int, int>()
        {
            {201, 5},
            {202, 5},
        };
        private readonly Dictionary<int, int> _middleUiMap = new Dictionary<int, int>()
        {
            {201, 5},
            {202, 5},
        };
        private readonly Dictionary<int, int> _resultUiMap = new Dictionary<int, int>()
        {
            {201, 5},
            {202, 5},
        };
        private readonly Dictionary<int, int> _recordMap = new Dictionary<int, int>()
        {
            {201, 5},
            {202, 5},
        };

        private PvpTypeConverter()
        {
            
        }

        private static PvpTypeConverter instance;

        public static PvpTypeConverter Instance
        {
            get
            {
                if (instance == null)
                    instance = new PvpTypeConverter();
                return instance;
            }

        }

        public static int GetLoadingUIType(int pvptype)
        {
            return Instance._loadingUiMap.ContainsKey(pvptype)
                ? Instance._loadingUiMap[pvptype]
                : pvptype;
        }

        public static int GetLoadingType(int pvptype)
        {
            return Instance._loadingTypeMap.ContainsKey(pvptype)
                ? Instance._loadingTypeMap[pvptype]
                : pvptype;
        }

        public static int GetBattleUIType(int pvptype)
        {
            return Instance._battleUiMap.ContainsKey(pvptype)
                ? Instance._battleUiMap[pvptype]
                : pvptype;
        }

        public static int GetMiddleUIType(int pvptype)
        {
            return Instance._middleUiMap.ContainsKey(pvptype)
                ? Instance._middleUiMap[pvptype]
                : pvptype;
        }

        public static int GetResultUIType(int pvptype)
        {
            return Instance._resultUiMap.ContainsKey(pvptype)
                ? Instance._resultUiMap[pvptype]
                : pvptype;
        }

        public static int GetRecordType(int pvptype)
        {
            return Instance._recordMap.ContainsKey(pvptype)
                ? Instance._recordMap[pvptype]
                : pvptype;
        }
    }



    /*
    showMode
    0:最老的pvp模式，现在被拆成了4／5，现已无效
    1:公会战模式
    2:无差别赛事模式（但是没有用这个结算界面 所以2无效字段）
    3:强者对决模式
    4:忍术对战模式
    5:段位赛模式
    6:锦标赛模式
     */
    public class BattleFinalResultData
    {
        public int wins;
        public BattleFinalResultItemData myResult;
        public BattleFinalResultItemData enemyResult;
        public List<PVPPlayerStatisData> statisticResults;
        public PVPPlayerStatisData myStatis;
        public PVPPlayerStatisData enemyStatis;
        public int showMode; //
        public bool isLuxiang; // 是否有战斗录像
        public bool isMoment;
        public AwardNotify award;
        public List<BattleRoundResultData> roundResults;
        public List<int> scores;
        public string netWorkStr;
		public PvpFightType pvpServerType;
		public PvpPkType pkType;
		public bool isMineTeam1P;
		//public bool UseCard;
		public bool ShowFightAgain;
        public int fightAgainValidSeconds = 10; // 与其再战的等待时间
    }

    /// <summary>
    /// 实时PVP操作
    /// </summary>
    [Hotfix]
    class PVPRealTimeOperation : BattleOperation
    {
        public const string Op_SetUseDefaultRoundBeginPrepare = "SetUseDefaultRoundBeginPrepare";
        public const string Op_SetUseDefaultBattleEnd = "SetUseDefaultBattleEnd";
        public const string Op_RoundReady = "RoundReady";
        public const string Op_CheckWinner = "CheckWinner";
        public const string Op_RoundEndShowCompleted = "RoundEndShowCompleted";
        public const string Op_SetTotalRoundState = "SetTotalRoundState";
        public const string Op_ReqBattleEnd = "ReqBattleEnd";
        public const string Op_DoNextLoad = "DoNextLoad"; 

        private const string TAG = "PVPRealTimeOperation";

        public PVPRTBaseModel modelPRT;
        public PVPRTResource resroucePRT;
        public PVPRTRuntime runtimePRT;
        
        private float _round_load_value = 0.0f;
        private int _curr_round_end_state = 0;
        private int _total_round_end_state = 2;
        private bool _useDefaultRoundBeginPrepare = true;
        private bool _useDefaultBattleEnd = true;
        private bool _isEnter = false;
        private bool _isBuild = false;
        private bool _isGameOver = false;
        /// <summary>
        /// 是否已经开始加载了...
        /// </summary>
        private bool _isLoadingStart = false;
        private bool _useReplayKit = false;
        //private bool _recordSaved = false;
        private bool _useLocalPerform = false;
        private bool _otherProgressSet = false;
        private string _errormsg = "";
        private bool _isShowResultWin = false;

        private RoundResultParam _roundResultParam = null;

        private SysSetting _sysSetting = null;

        private PVPRTQuitWatcher _watcher = null;
        private KHVoidFunction _endFunction = null;
        private Action<PVPSyncData> _reportFunction = null;
        private string _endFuncType = "";
        private bool _isNormalGameOverNtf = false;

        private bool isMomentOpen = false;

        private int _checksumReportInterval = 100;

        private bool _isRoundEnd = true;

        private static int ROUND_END_TIME_OUT = 60;

        /// <summary>
        /// 连接超时记时..
        /// </summary>
        private KHDelayCallTimer _DelayCallTimer;

        public PVPRealTimeOperation()
        {
            _sysSetting = new SysSetting();
            _watcher = new PVPRTQuitWatcher(_InterruptGuide);
            Debug.LogWarning("初始化PVPRealTimeOperation");
        }
        
        [Operation(BattleOperation.RequireBattle)]
        public void Enter(object data = null)
        {
            Debuger.Log("PVPRealTimeOperation Enter()");
            _isLoadingStart = false;

            PVPRTParameter param = data as PVPRTParameter;

            ///如果是打电脑则打开进度设置
            _otherProgressSet = (param.playMode == PVPRTPlayMode.Playback) || param.vsCPU;

            //param.useGuide = true;

            ///获取当前的客户端版本和资源版本号...
            param.btlCodeVer = VersionManager.ClientCodeVer;
            param.btlResVer = VersionManager.ClientResVer;

            _ReadyEnter(param);
        }
        
        [Operation("SendEmoji")]
        public void OnSendEmoji(object data)
        {
            runtimePRT.SendEmoji(System.Convert.ToInt32(data));
        }

        [Operation("SendChoseNinja")]
        public void OnSendChoseNinja(object data)
        {
            runtimePRT.SendChoseNinja(System.Convert.ToInt32(data));
        }

        [Operation("SendStartChoseNinja")]
        public void OnSendStartChoseNinja(object data)
        {
            runtimePRT.SendStartChoseNinja(1);
        }

        [Operation("RequestCallHelp")]
        public void OnRequestCallHelp(object data)
        {
            Debuger.Log("OnRequestCallHelp");
            int arg = (int)data;
            runtimePRT.Cmmc.Request(VKeyDef.PVP_SKILL_ASSISTANT, arg, KHBattle._FrameIndex);
        }

        [Operation("RequestCallHelpConfirm")]
        public void OnRequestCallHelpConfirm(object data)
        {
            Debuger.Log("OnRequestCallHelpConfirm");
            int arg = (int)data;
            runtimePRT.Cmmc.Request(VKeyDef.PVP_SKILL_ASSISTANT_CONFIRM, arg, KHBattle._FrameIndex);
        }

        private void _ReadyEnter(PVPRTParameter param)
        {
            Debuger.Log("_ReadyEnter");
            if (param.IsValidity)
            {
                ///开始进入
                _Do_Enter(param);

                ///发送可以进入战斗的事件
                ParentPlugin.Dispatcher.dispatchEvent(new KHEvent(BattleEvent.AllowedEnterBattle));
            }
            else
            {
                ///错误了...
            }
        }


        #region 进入或退出的逻辑处理

        private void  _Do_Enter(PVPRTParameter param)
        {

            if (_isEnter) return;
                _isEnter = true;

            _errormsg = "";
            _isGameOver = false;
            _isShowResultWin = false;
            #region  各种全局参数设置
            _sysSetting._AllowPlayCG = DefineExt.AllowPlayCG;
            _sysSetting._PVP_DEFAULT_PLAY_SPEED = DefineExt.PVP_DEFAULT_PLAY_SPEED;
            _sysSetting._KillActorEffect = DefineExt.KillActorEffect;
            _sysSetting._AllowPauseBattle = KHBattleManager.Instance.AllowPauseBattle;
            _sysSetting._Enabled_Surface_dust = DEFINE.Enabled_Surface_dust;
            _sysSetting._IsBaseQualityDevice = DEFINE.IsBaseQualityDevice;
            KHBattleAttribute.getInstance().PauseActorType = TargetPauseElement.ET_ALL;

            DEFINE.IN_PVP_BATTLE = true;
            DEFINE.IsBaseQualityDevice = true;
            DEFINE.Enabled_Surface_dust = false;
            TssManager.Instance.TssType = TssTypeDef.None;
            DefineExt.IgnoreNoticeScroll = true;
            DefineExt.AllowPlayCG = false;
            DefineExt.KillActorEffect = false;
            //DefineExt.PVP_RECORD_SPEED = 1;
            DEFINE.Enabled_Surface_dust = false;
            KHBattleManager.Instance.AllowPauseBattle = false;

            _useDefaultBattleEnd = true;

            setSceneHeartBeat(true);
            #endregion
            if(DefineExt.EnablePvPTss)
            {
                TssManager.Instance.TssType = TssTypeDef.PVP;   //tss by frank
            }
            ///请求进入之前先验证数据的准确性吧...
            modelPRT.Enter(param);

            KHMonsterManager mInstance = KHMonsterManager.getInstance();
            mInstance.ChangeSpawnLogic(false);
            mInstance.MonsterRTData = KHMonsterRequest.Instance.DefaultData;
            KHMonsterRequest.Instance.ResponseData = KHMonsterRequest.Instance.DefaultData;


            DEFINE.ALLOW_TIME_FLOW_PROTECT = modelPRT.pvpType == 1;

            ///预先构建一个起始的场景数据
            modelPRT.BuildNextRoundData();

            /// 初始化PVP战斗统计
            PVPStatis.Instance.Create(modelPRT, runtimePRT);
            PVPAntiCheat.Instance.Enter(modelPRT, runtimePRT, false);

            ///给runtime初始化必要数据
            runtimePRT.Create(modelPRT.pvpCurrentRTData);

            runtimePRT.addEventListener(PVPRTRuntimeEvent.EVT_SERVER_LOAD_PROGRESS, _SyncLoadProgress);
            runtimePRT.addEventListener(PVPRTRuntimeEvent.EVT_SERVER_ROUND_BEGIN, _SyncRoundBegin);
            runtimePRT.addEventListener(PVPRTRuntimeEvent.EVT_SERVER_CONTROL_START, _SyncControlBegin);
            runtimePRT.addEventListener(PVPRTRuntimeEvent.EVT_TIME_UPDATE, _RTTimeUpdate);
            runtimePRT.addEventListener(PVPRTRuntimeEvent.EVT_SERVER_ROUND_END, _SyncRoundEnd);
            runtimePRT.addEventListener(PVPRTRuntimeEvent.EVT_BATTLE_RESULT, _BattleResult);
            runtimePRT.addEventListener(PVPRTRuntimeEvent.EVT_PING_UPDATE, _PingUpdateEvt);
            runtimePRT.addEventListener(PVPRTRuntimeEvent.EVT_WATCH_NUM_UPDATE, _WatchNumUpdateEvt);
            runtimePRT.addEventListener(PVPRTRuntimeEvent.EVT_SERVER_PLAYER_EMOJI, _SyncPlayerEmoji);
            runtimePRT.addEventListener(PVPRTRuntimeEvent.EVT_CALL_ASSISTANT, _RefreshCallAssistantUI);
            runtimePRT.addEventListener(PVPRTRuntimeEvent.EVT_SERVER_PLAYER_ONLINE_STATE, _SyncPlayerOnlineState);

            // pvp 2.0
            runtimePRT.addEventListener(PVPRTRuntimeEvent.EVT_ROUND_BEGIN_PREPARE, _OnRoundBeginPrepare);
            runtimePRT.addEventListener(PVPRTRuntimeEvent.EVT_WILL_BATTLE_END, _OnWillBattleEnd);
            runtimePRT.addEventListener(PVPRTRuntimeEvent.EVT_WILL_DO_NEXT_LOAD, _OnWillDoNextLoad);

            // pvp 3.0
            runtimePRT.addEventListener(PVPRTRuntimeEvent.EVT_BUILD_PLAYER, _OnBuildPlayer);

            // 巅峰战
            runtimePRT.addEventListener(PVPRTRuntimeEvent.EVT_SERVER_CHOSE_NINJA, _SyncChoseNinja);
            runtimePRT.addEventListener(PVPRTRuntimeEvent.EVT_SERVER_START_CHOSE_NINJA, _SyncStartChoseNinja);

            EventRouter.instance.AddEventHandler<RecordingStartStatus>(EventID.GAMEJOY_STARTRECORDING_RESULT,onStartMomentsRecording);
            EventRouter.instance.AddEventHandler<long>(EventID.GAMEJOY_STOPRECORDING_RESULT,onEndMomentsRecording);
            ///侦听普通结算TCP回调
            _isNormalGameOverNtf = param.listenNormalGameoverNtf;
            if(_isNormalGameOverNtf)
            {
                NetworkManager.Instance.AddMessageCallback((uint)ZoneCmd.ZONE_PVP_1V1_GAME_OVER_NTF, _PVP_1V1_GameOver_Info);
            }
            else
            {
                NetworkManager.Instance.AddMessageCallback((uint)ClientCmd.CMD_LAN_PVP_1V1_GAME_OVER_NTF, _PVP_1V1_GameOver_Info);
            }

            ParentPlugin.Dispatcher.addEventListener(PhaseEvent.DUPLICATE_PREPARE_PHASE_START_PREPARE, _OnStartPreparing);

            _checksumReportInterval = param.check_sum_report_interval;
            if (_checksumReportInterval > 0)
            {
                KHGlobal.dispatcher.addEventListener(KHEvent.ENTER_FRAME, OnEnterFrame);
            }

            ///使用录相功能，这是屏幕录像功能
            if (modelPRT.useReplayKit)
            {
                _useReplayKit = true;
                ReplayKitPlugin.StartReplay();
            }

            isMomentOpen = KHVideoManager.GetMomenOpenState(param.subPluginName);
            Debuger.Log("isMomentOpen"+isMomentOpen);
            if(isMomentOpen)
            {
                Debuger.Log("startMomentRecording");
                KHVideoManager.getInstance().startMomentRecording();
                VideoBattleInfoMonitor.Instance.AddLinstener();
            }
#if UNITY_EDITOR
            if (param.subPluginName == "_pvprealtime")
            {
                KHVideoManager.beginTimeStamp = GameJoy.getSystemCurrentTimeMillis;
                Debuger.Log("VideoStartTime=" + KHVideoManager.beginTimeStamp);
                KHVideoManager.getInstance().startMomentRecording();
                VideoBattleInfoMonitor.Instance.AddLinstener();
            }
#endif
            ///构建通信信息
            CmmcInfo cmmcinfo = new CmmcInfo()
            {
                address = param.udpAddres,
                roomID = param.roomID,
                mineSID = param.mineSid,
                authID = param.auth,
                userID = param.mineUserID,
                cmmcProxyType = param.cmmcProxyType,
                vsCPU = param.vsCPU,
                playMode = param.playMode,
                udpErrorResult = _udp_error_result,
                encKey = param.encKey,
                useCheckSum = param.useCheckSum,
                useGSDK = param.useGSDK,
                useEmptyFrameAckAvoid = param.useEmptyFrameAckAvoid,
                wifiSendInterval = param.wifiSendInterval,
                isLan = param.isLan,
                defaultSendInterval = DefineExt.UDP_SEND_INTERVAL,
                timeout = DefineExt.UDP_DEFAULT_TIMEOUT_MS
            };
			if(param.pvpServerType == PvpFightType.PvpFightType_UKFinal)
			{
				cmmcinfo.timeout = DefineExt.UDP_UK_TIMEOUT_MS;
			}
            ///运行时请求加载开始...
            runtimePRT.RequestLoadStart(cmmcinfo);
            ///打电脑或是播录相不开启战斗预表现
            _useLocalPerform = (modelPRT.notUseLocalPerform ? false : DefineExt.PVP_USE_LOCAL_PERFORM);

            _endFunction = param.endFunction;
            _endFuncType = param.endFuncType;
            _reportFunction = param.reportFunction;

            if (_useLocalPerform)
            {
                PVPRTLocalPerform.Instance.Begin(runtimePRT);
            }
        }

        private void _DO_Exit()
        {
            Debuger.Log("_DO_Exit");
            if (!_isEnter) return;

            _isEnter = false;
            _errormsg = "";
            _isShowResultWin = false;
            ///去掉超时侦听
            _watcher.UnListen();
            _endFunction = null;
            _reportFunction = null;

            #region  各种全局参数设置
            DEFINE.IN_PVP_BATTLE = false;
            DEFINE.IsBaseQualityDevice = _sysSetting._IsBaseQualityDevice;
            DEFINE.ALLOW_TIME_FLOW_PROTECT = false;
            DEFINE.Enabled_Surface_dust = _sysSetting._Enabled_Surface_dust;
            TssManager.Instance.ResetTssType();
            DefineExt.IgnoreNoticeScroll = false;
            DefineExt.AllowPlayCG = _sysSetting._AllowPlayCG;
            DefineExt.KillActorEffect = _sysSetting._KillActorEffect;
            KHBattleManager.Instance.AllowPauseBattle = _sysSetting._AllowPauseBattle;
            KHBattleAttribute.getInstance().PauseActorType = TargetPauseElement.ET_ALL;

            _useDefaultBattleEnd = true;

            setSceneHeartBeat(false);
           // DefineExt.PVP_RECORD_SPEED = _sysSetting._PVP_RECORD_SPEED;
            #endregion

            //在这里有可能要保护一下录像
            PVPRecorder.Instance.AutoSave();

            if (_useReplayKit)
            {
                _useReplayKit = false;
                ReplayKitPlugin.StopReplay();
            }

            if (modelPRT.useReplayKit)
            {
                ReplayKitPlugin.DestroyReplay();
            }

            runtimePRT.removeEventListener(PVPRTRuntimeEvent.EVT_SERVER_LOAD_PROGRESS, _SyncLoadProgress);
            runtimePRT.removeEventListener(PVPRTRuntimeEvent.EVT_SERVER_ROUND_BEGIN, _SyncRoundBegin);
            runtimePRT.removeEventListener(PVPRTRuntimeEvent.EVT_SERVER_CONTROL_START, _SyncControlBegin);
            runtimePRT.removeEventListener(PVPRTRuntimeEvent.EVT_TIME_UPDATE, _RTTimeUpdate);
            runtimePRT.removeEventListener(PVPRTRuntimeEvent.EVT_SERVER_ROUND_END, _SyncRoundEnd);
            runtimePRT.removeEventListener(PVPRTRuntimeEvent.EVT_PING_UPDATE, _PingUpdateEvt);
            runtimePRT.removeEventListener(PVPRTRuntimeEvent.EVT_WATCH_NUM_UPDATE, _WatchNumUpdateEvt);
            runtimePRT.removeEventListener(PVPRTRuntimeEvent.EVT_SERVER_PLAYER_EMOJI, _SyncPlayerEmoji);
            runtimePRT.removeEventListener(PVPRTRuntimeEvent.EVT_CALL_ASSISTANT, _RefreshCallAssistantUI);
            runtimePRT.removeEventListener(PVPRTRuntimeEvent.EVT_SERVER_PLAYER_ONLINE_STATE, _SyncPlayerOnlineState);

            // pvp 2.0
            runtimePRT.removeEventListener(PVPRTRuntimeEvent.EVT_ROUND_BEGIN_PREPARE, _OnRoundBeginPrepare);
            runtimePRT.removeEventListener(PVPRTRuntimeEvent.EVT_WILL_BATTLE_END, _OnWillBattleEnd);
            runtimePRT.removeEventListener(PVPRTRuntimeEvent.EVT_WILL_DO_NEXT_LOAD, _OnWillDoNextLoad);

            // pvp 3.0
            runtimePRT.removeEventListener(PVPRTRuntimeEvent.EVT_BUILD_PLAYER, _OnBuildPlayer);

            // 巅峰战
            runtimePRT.removeEventListener(PVPRTRuntimeEvent.EVT_SERVER_CHOSE_NINJA, _SyncChoseNinja);
            runtimePRT.removeEventListener(PVPRTRuntimeEvent.EVT_SERVER_START_CHOSE_NINJA, _SyncStartChoseNinja);

            EventRouter.instance.RemoveEventHandler<RecordingStartStatus>(EventID.GAMEJOY_SDK_FEATURE_CHECK_RESULT,onStartMomentsRecording);
            EventRouter.instance.RemoveEventHandler<long>(EventID.GAMEJOY_STOPRECORDING_RESULT,onEndMomentsRecording);
            if(_isNormalGameOverNtf)
            {
                NetworkManager.Instance.RemoveMessageCallback((uint)ZoneCmd.ZONE_PVP_1V1_GAME_OVER_NTF, _PVP_1V1_GameOver_Info);
            }
            else
            {
                NetworkManager.Instance.RemoveMessageCallback((uint)ClientCmd.CMD_LAN_PVP_1V1_GAME_OVER_NTF, _PVP_1V1_GameOver_Info);
            }

            ParentPlugin.Dispatcher.removeEventListener(PhaseEvent.DUPLICATE_PREPARE_PHASE_START_PREPARE, _OnStartPreparing);

            KHGlobal.dispatcher.removeEventListener(KHEvent.ENTER_FRAME, OnEnterFrame);

            //解决切回后台时刻录制sdk没有被唤醒即调用结束接口
            if (isMomentOpen)
            {
                VideoBattleInfoMonitor.Instance.RemoveListener();
                KHVideoManager.getInstance().endMomentRecording();
                KHVideoManager.getInstance().generateMomentVideo();
            }
            ///结束战斗预表现...
            if (_useLocalPerform)
            {
                _useLocalPerform = false;
                PVPRTLocalPerform.Instance.End();
            }

            KHMonsterManager mInstance = KHMonsterManager.getInstance();
            mInstance.ChangeSpawnLogic(true);
            mInstance.MonsterRTData = null;
            KHMonsterRequest.Instance.ResponseData = null;

            PVPRTUI.Instance.Unload();
            PVPStatis.Instance.Destory();
            PVPAntiCheat.Instance.Exit();

            modelPRT.Exit();
            runtimePRT.Destory();
        }

        #endregion

        private void setSceneHeartBeat(bool _state)
        {
            if (_state)
            {
                //addeventlistener
                KHSceneManager.dispatcher.addEventListener(KHSceneManager.SCENE_HEART_BEAT, sendSceneHeartBeat);
            }
            else
            {
                KHSceneManager.dispatcher.removeEventListener(KHSceneManager.SCENE_HEART_BEAT, sendSceneHeartBeat);

            }
        }

        private void sendSceneHeartBeat(KHEvent evt)
        {
            int type = -1;
            if (modelPRT.isNormalMode)
            {
                /////普通模式
                type = 3;
            }
            else if (modelPRT.isObStream)
            {
                ////rt ob模式
                type = 6;
            }
            if (type != -1)
            {
                KHSceneManager.Instance.SendHBExtraInfo(type);
            }
        }

        [Operation(BattleOperation.RequireExitBattle)]
        public void Exit(object data = null)
        {
            Debuger.Log("Exit");
            ParentPlugin.Dispatcher.dispatchEvent(new KHEvent(BattleEvent.AllowedExitBattle));
        }

        [Operation(BattleOperation.OnTerminateDuplicate)]
        public void OnTerminate(object data = null)
        {
            Debuger.Log("OnTerminate");
            if (KHBattleManager.Instance.BattlePlugin != null)
            {
                KHBattleManager.Instance.BattlePlugin.SendMessage("RequireExitBattle");
            }
        }

        [Operation(BattleOperation.BuildBattle)]
        public void Build(object data = null)
        {
            Debuger.Log("Build(), _isBuild=" + _isBuild);
            if (_isBuild) return;

            if (LuaDefine.ENABLE_GAME_OVER_SWITCH && _isGameOver) return;

            if(string.IsNullOrEmpty(_errormsg))
            { 
                _isBuild = true;

                modelPRT.Build();

                Debuger.Log("Build() PVP - Build");
                _createPlot();
                ///播放器构建中...
                runtimePRT.Build();
                ///首次加载需要调用一下
                _do_currround_begin();
                ///是否启动引导程序
                _BeginGuide();
            }
            else
            {
                UIAPI.alert("错误", _errormsg, 0, _error_gameover_click, null, true);
            }
        }


        [Operation(BattleOperation.UnbuildBattle)]
        public void Unbuild(object data = null)
        {
            Debuger.Log("Unbuild, _isBuild=" + _isBuild);
            if (_isBuild)
            { 
                _isBuild = false;
                runtimePRT.Unbuild();
                _InterruptGuide();
                modelPRT.Unbuild();
            }

            ///资源释放
            resroucePRT.PRTUnloadAll();
            ///系统退出...
            _DO_Exit();
            
            _roundResultParam = null;
        }

        private void _createPlot()
        {
            Debuger.Log("_createPlot(), plotId=" + modelPRT.plotId);
            if (modelPRT.plotId != 0)
            {
                PlotInfo plotInfo = KHDataManager.getInstance().getPlotInfo(modelPRT.plotId);
                modelPRT.currentPlotInfo = plotInfo;
                runtimePRT.BuildPlot();
            }
        }

        [Operation(BattleOperation.ActivePlot)]
        public void DoActivePlot(object data = null)
        {
            Debuger.Log("DoActivePlot()");
        }

        /// <summary>
        /// 自已资源加载进度的回调
        /// </summary>
        /// <param name="percentage"></param>
        [Operation(BattleOperation.OnLoadingProcess)]
        public void OnMineLoadProgress(object data)
        {
            float percentage = (float)data;
            ///加载已经完成了...
            if (_round_load_value >= percentage) return;

            _round_load_value = percentage;

            ///计算当前的进度...
            int progress = (int)(_round_load_value * 100.0f);

            /// 单纯的提交一下进度当前进度即可
            runtimePRT.RequestLoadProgress(progress);

            ///加载进度
            _load_progress(progress);
        }
        
        public void onStartMomentsRecording(RecordingStartStatus status)
        {
            KHVideoManager.getInstance().setMomentStartStatus(status);
        }

        public void onEndMomentsRecording(long duration)
        {
            KHVideoManager.getInstance().handleEndMomentRecoding(duration,_errormsg);
        }

        [Operation("CalculateTeamResultInfo")]
        public void OnCalculateTeamResultInfo(object data)
        {
            Debuger.Log("OnCalculateTeamResultInfo");
            PVPRTRoundResult resultInfo = data as PVPRTRoundResult;
            modelPRT.CalculateResultTeamInfo(runtimePRT, resultInfo);
        }

        /// <summary>
        /// 首次动画播放完毕的回调
        /// </summary>
        public void OnFristLoadingMovieEnd()
        {
            Debuger.Log("OnFristLoadingMovieEnd");

            ///首次加载了...
            _round_load_value = 0;
            if (!LuaDefine.ENABLE_GAME_OVER_SWITCH)
            {
                _isLoadingStart = true;
            }

            ///通知开始加载资源啦...
            ParentPlugin.Dispatcher.dispatchEvent(
                            new KHEvent(BattleEvent.OnLoadingAnimFinished));

        }

        #region 运行时事件处理

        // Loading开始了
        private void _OnStartPreparing(KHEvent e)
        {
            Debuger.Log("_OnStartPreparing, _isGameOver=" + _isGameOver);
            if (LuaDefine.ENABLE_GAME_OVER_SWITCH)
            {
                _isLoadingStart = true;
                if (_isGameOver)
                {
                    UIAPI.alert("提示", _errormsg, 0, _error_gameover_click, null, true);
                }
            }
        }

        private void _SyncLoadProgress(KHEvent e)
        {
            List<SyncCmd> items = e.data as List<SyncCmd>;

            if (items != null)
            {
                int mineTeamID = modelPRT.pvpMineTeamID;

                PVPRTUI pvpui = PVPRTUI.Instance;

                for (int i = 0; i < items.Count; i++)
                {
                    SyncCmd cmd = items[i];

                    if (cmd.playerId == mineTeamID)
                    {
                        pvpui.UpdateMineProgress(cmd.args[0]);
                    }
                    else
                    {
                        pvpui.UpdateOtherProgress(cmd.args[0]);
                    }

                    pvpui.UpdateProgress(cmd.playerId, cmd.args[0]);
                }

                items.Clear();
            }
        }

        private void _SyncRoundBegin(KHEvent e)
        {
            Debuger.Log("_SyncRoundBegin()");
            _watcher.UnListen();

            ////新局开始...
            if ((ParentPlugin as PVPRealTimePlugin).GetModelType() == (int)PVPRTPlayMode.AssistantMode)
            {
                PVPStatis.Instance.AssitantRoundBegin(modelPRT.newPlayerGroupId, modelPRT.newPlayerTeamId);

            }
            else
            {
                PVPStatis.Instance.RoundBegin(modelPRT.newPlayerGroupId, modelPRT.newPlayerTeamId);
            }

            _isRoundEnd = false;

            ///UI的结束状态重置
            _curr_round_end_state = 0;
            _total_round_end_state = 2;

            _useDefaultRoundBeginPrepare = true;

            ///更新UI ...
            PVPRTUI.Instance.UIRoundBeginFlyNext(modelPRT, _roundBeginFlyNextEnd);
        }
        
        private void _roundBeginFlyNextEnd()
        {
            Debuger.Log("_roundBeginFlyNextEnd");
            if (DefineExt.EnableJumpIn_PVP && (ParentPlugin as PVPRealTimePlugin).GetModelType() == (int)PVPRTPlayMode.Normal)
            {
                _JumpIn();
            }
            else
            {
                runtimePRT.SendRoundPrepareEvent();
            }
        }

        private void _OnBuildPlayer(KHEvent e)
        {
            Debuger.Log("_OnBuildPlayer()");

            if (DefineExt.EnableJumpIn_PVP && (ParentPlugin as PVPRealTimePlugin).GetModelType() == (int)PVPRTPlayMode.Normal)
            {
                List<PlayerController> lstPlayers = KHPlayerManager.getInstance().getPlayerList();
                for (int i = 0; i < lstPlayers.Count; i++)
                {
                    PlayerController player = lstPlayers[i];

                    IdleAction action = player.getCurrentAction() as IdleAction;
                    if (action != null)
                    {
                        action.WaitingJumpInEnd();
                    }
                }
            }
        }

        private Vector3 GetBornPositionByTeamId(int teamId)
        {
            PVPRTActorInfo rtActorInfo = runtimePRT.CurrentRoundData.FindByTeamID(teamId);
            return rtActorInfo.dynamicInfo.JumpBornShowPosition;
        }

        private int _jumpInCount = 0;
        private void _JumpIn()
        {
            Debuger.Log("_JumpIn()");

            _jumpInCount = 0;

            List<PlayerController> lstPlayers = KHPlayerManager.getInstance().getPlayerList();
            for (int i = 0; i < lstPlayers.Count; i++)
            {
                GuildFightNinjaJumpin jumpIn = new GuildFightNinjaJumpin(lstPlayers[i], _OnJumpIn);
                jumpIn.StartJumpInWithDir(GetBornPositionByTeamId(lstPlayers[i].model.teamId));
            }
        }

        private void _OnJumpIn()
        {
            Debuger.Log("_OnJumpIn()");

            ++_jumpInCount;
            if (_jumpInCount == 2)
            {
                List<PlayerController> lstPlayers = KHPlayerManager.getInstance().getPlayerList();
                for (int i = 0; i < lstPlayers.Count; i++)
                {
                    PlayerController player = lstPlayers[i];
                    player.getMoveComponet().mapPosition = GetBornPositionByTeamId(lstPlayers[i].model.teamId);
                }

                runtimePRT.SendRoundPrepareEvent();
            }
        }

        private void _OnRoundBeginPrepare(KHEvent e)
        {
            Debuger.Log("_OnRoundBeginPrepare(), _useDefaultRoundBeginPrepare=" + _useDefaultRoundBeginPrepare);
            if (_useDefaultRoundBeginPrepare)
            {
                RoundReady();
            }
        }

        [Operation(Op_SetUseDefaultRoundBeginPrepare)]
        public void SetUseDefaultRoundBeginPrepare(object data = null)
        {
            _useDefaultRoundBeginPrepare = Convert.ToBoolean(data);
            Debuger.Log("SetUseDefaultRoundBeginPrepare(), _useDefaultRoundBeginPrepare=" + _useDefaultRoundBeginPrepare);
        }

        /// <summary>
        /// 战斗准备完毕
        /// </summary>
        [Operation(Op_RoundReady)]
        public void RoundReady(object data = null)
        {
            Debuger.Log("RoundReady()");
            ///移到主角上去...
            runtimePRT.CameraAttach();

            if (DefineExt.IsSlimVersion)
            {
                runtimePRT.RequestControl();
            }
            else
            {
                VideoBattleInfoMonitor.Instance.v_roundResultParam.mode = RoundResultParam.START_MODE;
                VideoBattleInfoMonitor.Instance.v_roundResultParam.round = modelPRT.pvpRoundCount;
                PVPRTUI.Instance.UIRoundBeginMovie(new RoundResultParam
                {
                    mode = RoundResultParam.START_MODE,
                    round = modelPRT.pvpRoundCount,
                    intCallBack = _roundBeginMoviePlayEnd
                });
            }
        }

        /// <summary>
        /// 开战动画播放结束.
        /// </summary>
        /// <param name="val"></param>
        private void _roundBeginMoviePlayEnd(int val)
        {
            Debuger.Log("_roundBeginMoviePlayEnd()");
            runtimePRT.RequestControl();
        }

        private void _SyncControlBegin(KHEvent e)
        {
            Debuger.Log("_SyncControlBegin()");
            PVPRTUI.Instance.UIMovieBeginFight();

            List<PlayerController> lstPlayers = KHPlayerManager.getInstance().getPlayerList();
            for (int i = 0; i < lstPlayers.Count; i++)
            {
                KHStatusManager.AddStatus(lstPlayers[i], 2019001);
                KHStatusManager.AddStatus(lstPlayers[i], 2019002);

                if (LogUtil.CheckRTLog(RT_LOG_LEVEL.EnableHitTest))
                {
                    Debuger.Log("ControlBegin(), id=" + lstPlayers[i].model.id + ", transform=" + lstPlayers[i].TransformStr);
                }
            }
        }
        
        private void _SyncChoseNinja(KHEvent e)
        {
            Debuger.Log("_SyncChoseNinja()");
            SyncCmd cmd = e.data as SyncCmd;
            if (cmd != null && cmd.args != null && cmd.args.Count > 0)
            {
                for (int i = 0; i < modelPRT.teamDatas.Count; i++)
                {
                    PVPRTTeamData teamData = modelPRT.teamDatas[i];
                    if (teamData.teamID == cmd.args[0])
                    {
                        teamData.NextActorIndex = cmd.args[1];
                    }
                    if (cmd.args.Count > 2 && teamData.teamID == cmd.args[2])
                    {
                        teamData.NextActorIndex = cmd.args[3];
                    }
                }
            }
            PVPRTUI.Instance.UICloseChoseNinja();
            _will_do_next_load();
        }

        private void _SyncStartChoseNinja(KHEvent e)
        {
            Debuger.Log("_SyncStartChoseNinja()");
            PVPRTUI.Instance.UIOpenChoseNinja();
        }

        #region 单局结束一系列的操作
        /// <summary>
        /// 判定胜负
        /// </summary>
        /// <param name="data"></param>
        [Operation(Op_CheckWinner)]
        public void CheckWinner(object data = null)
        {
            Debuger.Log("CheckWinner()");
            runtimePRT.CheckWinner();
        }

        /// <summary>
        /// 设置当前回合状态
        /// </summary>
        /// <param name="data"></param>
        [Operation(Op_SetTotalRoundState)]
        public void SetTotalRoundState(object data = null)
        {
            _total_round_end_state = Convert.ToInt32(data);
            Debuger.Log("SetTotalRoundState(), _total_round_end_state=" + _total_round_end_state);
        }

        /// <summary>
        /// 回合结束表演完毕
        /// </summary>
        /// <param name="data"></param>
        [Operation(Op_RoundEndShowCompleted)]
        public void RoundEndShowCompleted(object data = null)
        {
            Debuger.Log("RoundEndShowCompleted()");
            _curr_round_end_state++;
            _can_do_round_end();
        }

        /// <summary>
        /// 收到战斗结果
        /// </summary>
        /// <param name="e"></param>
        private void _BattleResult(KHEvent e)
        {
            Debuger.Log("_BattleResult()");
            Debug.LogWarning("_BattleResult 当前处于" + KHGlobalExt.app.CurrentContext.contextName);
            PVPRTRoundResult result = e.data as PVPRTRoundResult;

            //Debuger.Log("[_BattleResult] _BattleResult killflag = " + result.killFlag);
            // 自动化播录像检测作弊
            if (PlayRecordDebugMgr.isDebugerVersion)
            {
                PlayRecordDebugMgr.Instance.ReportPVPRoundResult(result);
            }

            if (DefineExt.IsSlimVersion)
            {
                Slim.EnterPvpRealTimeHelper.ReportRoundResult(result);
                _UIBattleEndMovieBack();
                return;
            }

            if (result.killFlag != 0)
            {
                KHBattle.getInstance().Dispatcher.dispatchEvent(BattleStatisEvent.FinalAttackEvent);
                PVPRTUI.Instance.UIBattleEndMovie(result, _UIBattleEndMovieBack);
            }
            else
                _UIBattleEndMovieBack();
        }

        private void _UIBattleEndMovieBack()
        {
            Debuger.Log("_UIBattleEndMovieBack()");
            _curr_round_end_state++;

           // Debuger.Log("[_UIBattleEndMovieBack] _curr_round_end_state = " + _curr_round_end_state);

            if(!_can_do_round_end())
            {
                ///超时侦听
                _watcher.Listen(_error_gameover_click, ROUND_END_TIME_OUT);
            }
        }

        private void _SyncRoundEnd(KHEvent e)
        {
            Debuger.Log("_SyncRoundEnd()");

            if ((ParentPlugin as PVPRealTimePlugin).GetModelType() == (int)PVPRTPlayMode.AssistantMode)
            {
                PVPStatis.Instance.AssistantRoundEnd(modelPRT.pvpCurrentRoundData);
            }
            else
            {
                PVPStatis.Instance.RoundEnd(modelPRT.pvpCurrentRoundData);
            }

            if (_reportFunction != null)
            {
                PVPSyncData syncData = new PVPSyncData
                {
                    runtime = runtimePRT,
                    model = modelPRT,
                };
                _reportFunction(syncData);
                _reportFunction = null;
            }

            _curr_round_end_state++;
            _can_do_round_end();
        }

        /// <summary>
        /// 即要收到 _SyncRoundEnd 事件
        /// 又要客户端所需要的表现全部结束
        /// </summary>
        private bool _can_do_round_end()
        {
            Debuger.Log("_can_do_round_end, state=" + _curr_round_end_state);
            if (_curr_round_end_state >= _total_round_end_state)
            {
                _watcher.UnListen();
                Debuger.Log("_can_do_round_end() exec..");
                _curr_round_end_state = -100;
                
                if (DefineExt.IsSlimVersion)
                {
                    _will_do_next_load();
                }
                else
                {
                    runtimePRT.ListenDonothing(_do_round_end);
                }

                return true;
            }
            else
                Debuger.Log("_can_do_round_end() not..");
            return false;
        }

        private void _do_round_end()
        {
            Debuger.Log("_do_round_end()");

            /// -1表示超时的情况，反正双方都没干死
            PVPRTRoundData currentRoundData = modelPRT.pvpCurrentRoundData;
            PVPRTRoundResult currentResult = currentRoundData.result;

            if (_roundResultParam == null)
                _roundResultParam = new RoundResultParam();

            /// 主角默认是死亡的情况
            if (currentResult == null)
            {
                return;
            }

            /// 如果谁被奥义击杀 则记录一下 需要显示奥义击杀的黄色图标
            if (currentResult.killFlag == 2)
            {
                for (int i=0;i< modelPRT.teamDatas.Count;i++)
                {
                    if (modelPRT.teamDatas[i].teamID == currentResult.loseTeamId)
                    {
                        modelPRT.teamDatas[i].AoYiKilledActorList.Add(modelPRT.teamDatas[i].orderIndex);
                    }
                }
            }

            _roundResultParam.mode = currentResult.mode;
            _roundResultParam.winnerName = modelPRT.GetTeamName(currentResult.winTeamID);///currentResult.winName;
            _roundResultParam.winnerNinjaId = currentResult.winNijiaId;
            _roundResultParam.loseOrWin = modelPRT.MineloseOrWin;
            _roundResultParam.round = modelPRT.pvpRoundCount;
            _roundResultParam.callback = _will_do_next_load;

            VideoBattleInfoMonitor.Instance.v_roundResultParam = _roundResultParam;
            if ((ParentPlugin as PVPRealTimePlugin).GetModelType() == (int)PVPRTPlayMode.ConquesMode)
            {
                PVPRTUI.Instance.UIRoundEndMovieAndChoseNext(_roundResultParam);
            }
            else
            {
                PVPRTUI.Instance.UIRoundEndMovie(_roundResultParam);
            }
        }

        #endregion

        private void _RTTimeUpdate(KHEvent e)
        {
            PVPRTUI.Instance.UICountDownTime((int)e.data);
        }

        #endregion

        

        private void _load_progress(int progress)
        {
            if (progress >= 100)
            {
                _do_currround_begin();
            }
            else
            {
                ///强制设置下他人的进度
                if (_otherProgressSet)
                {
                    int otherset = progress + (int)(10 * UnityEngine.Random.value);
                    otherset = (otherset > 100 ? 100 : otherset);

                    PVPRTUI.Instance.UpdateOtherProgress(otherset);
                }

                PVPRTUI.Instance.UpdateMineProgress(progress);

                PVPRTUI.Instance.UpdateProgress(modelPRT.pvpMineTeamID, progress);
            }   
        }

        private void _will_do_next_load()
        {
            Debuger.Log("_will_do_next_load() _isBuild=" + _isBuild);

            ////这是个异步回调, 如果在退出战斗后仍然回调回来, 是有问题的
            if (!_isBuild)
            {
                return;
            }

            runtimePRT.WillDoNextLoad();
        }

        private void _OnWillDoNextLoad(KHEvent e)
        {
            Debuger.Log("_OnWillDoNextLoad");
            _do_next_load();
        }

        [Operation(Op_DoNextLoad)]
        public void DoNextLoad(object data = null)
        {
            Debuger.Log("DoNextLoad");
            _do_next_load();
        }

        private void _do_next_load()
        {
            Debuger.Log("_do_next_load");

            _isRoundEnd = true;

            ///构建下一个对局..
            modelPRT.BuildNextRoundData();

            if (modelPRT.hasNextRound)
            {
                runtimePRT.EndRound();

                _round_load_value = 0.0f;

                if (DefineExt.IsSlimVersion)
                {
                    _do_currround_begin();
                }
                else
                {
					PVPRTUI.Instance.OpenLoadingViewAgain(PvpTypeConverter.GetMiddleUIType(modelPRT.pvpType), modelPRT.pvpLoadingViewName, modelPRT.teamDatas);
                    ///侦听系统的整体进度即可...
                    resroucePRT.PRTBeginLoad(null);
                    // 给UI一个消息
                    PVPRTUI.Instance.BeginNextRoundLoading();
                }
            }
            else
            {
                _SendTssReport();  //tss by frank
            }
        }

        ///tss by frank
        private void _SendTssReport()
        {
            bool isNormalPvp = false;
            if (KHBattleManager.Instance.BattlePlugin != null)
            {
                PVPRealTimePlugin plugin = KHBattleManager.Instance.BattlePlugin as PVPRealTimePlugin;
                if (plugin != null && plugin.GetModelType() == (int)PVPRTPlayMode.Normal) isNormalPvp = true; //只处理正常pvp  tss by frank
            }
            if (!DefineExt.EnablePvPTss || !isNormalPvp)
            {
                _will_battle_end(0);
                return;
            }
            
            if (DefineExt.IsSlimVersion)
            {
                _will_battle_end(0);
                return;
            }

            ZoneTssBattleEndReq req = new ZoneTssBattleEndReq();
            req.type = modelPRT.pvpServerType; //TssManager.Instance.GetPvpFightType();
            req.tss_reqs.AddRange(modelPRT.pvpCurrentRTData.tss_reqs);

            NetworkManager.Instance.Send<ZoneTssBattleEndReq>((uint)ZoneCmd.ZONE_TSS_LOG_BATTLE_END, req,
                _OnRoundAllEndCallback, false, _OnRoundAllEndTimeout, 5);
        }

        private void _OnRoundAllEndCallback(object data)
        {
            Debuger.Log("[PVPRealTimeOperation] _OnRoundAllEndCallback");
            _will_battle_end(1);
        }


        private void _OnRoundAllEndTimeout(object data)
        {
            Debuger.Log("[PVPRealTimeOperation] _OnRoundAllEndTimeout");
            _will_battle_end(0);
        }
        ///tss by frank

        private void _do_currround_begin()
        {
            Debuger.Log("_do_currround_begin, _isBuild=" + _isBuild + ", roundIdx=" + modelPRT.pvpCurrentRoundIndex);
            _round_load_value = 1.0f;

            if (_isBuild)
            {
                if (!modelPRT.hasNextRound)
                {
                    _SendTssReport();  //tss by frank test
                }
                else
                {
                    //_watcher.Listen(_error_gameover_click);
                    runtimePRT.BeginRound();
                }
            }
        }

        private void _will_battle_end(int flag = 0)
        {
            Debuger.Log("_will_battle_end");
            runtimePRT.WillBattleEnd(flag); ///tss by frank
        }

        private void _OnWillBattleEnd(KHEvent e)
        {
            Debuger.Log("_OnWillBattleEnd");

            if (_useDefaultBattleEnd)
            {
                _do_battle_end((int)e.data); //tss by frank
            }
        }

        [Operation(Op_SetUseDefaultBattleEnd)]
        public void SetUseDefaultBattleEnd(object data = null)
        {
            _useDefaultBattleEnd = Convert.ToBoolean(data);
            Debuger.Log("SetUseDefaultBattleEnd(), _useDefaultBattleEnd=" + _useDefaultBattleEnd);
        }

        [Operation(Op_ReqBattleEnd)]
        public void ReqBattleEnd(object data = null)
        {
            Debuger.Log("ReqBattleEnd");
            _do_battle_end(0);
        }

        private void _do_battle_end(int flag)
        {
            Debuger.Log("_do_battle_end()");
            //int winnerTeamID = (modelPRT.pvpCurrentRoundData != null ?
            //    modelPRT.pvpCurrentRoundData.winTeamID : -1);

            runtimePRT.RequestBattleFinish(flag);

            ///超时处理侦听
            _watcher.Listen(_error_gameover_click, ROUND_END_TIME_OUT);
        }

        private string _old_msg = "";

        private void _PingUpdateEvt(KHEvent e)
        {
            if (e == null)
            {
                _MinePingUpdate(-1000);
            }
            else
            {
                Hashtable hashTB = e.data as Hashtable;
                
                if(hashTB.ContainsKey("mine"))
                {
                    hashTB.Remove("mine");
                    int delay = (int)hashTB["delay"];
                    _MinePingUpdate(delay);

                    if(delay > 0)
                    {
                        PVPRTUI.Instance.UIWifiStateUpdate(
                                modelPRT.pvpMineTeamID
                            ,   delay);
                    }
                }

                if(hashTB.ContainsKey("other"))
                {
                    int teamID = (int)hashTB["other"];
                    hashTB.Remove("other");
                    int delay = (int)hashTB["other_delay"];

                    PVPRTUI.Instance.UIWifiStateUpdate(teamID, delay);
                }
            }
            
        }

        private void _WatchNumUpdateEvt(KHEvent e)
        {
            int num = (int)e.data;
            PVPRTUI.Instance.UIWatchInfo(num);
        }

        private void _MinePingUpdate(int delay)
        {
            string msg = "RSP : 2s+";

            if (delay == -100)
            {
                msg = "网络缓冲";
            }
            else if (delay == -101)
            {
                msg = "战斗结束";
            }
            else if (delay == -1000)
            {
                msg = "精彩回放";
            }
            else if (delay == 0)
            {
                msg = _old_msg;
            }
            else if (delay <= 10000000)
            {
                msg = "";
                //msg = "RSP : " + delay + " ms";
                _old_msg = msg;
            }

            if (this.modelPRT != null)
            {
                if (this.modelPRT.playMode == PVPRTPlayMode.OBStream ||
                    this.modelPRT.playMode == PVPRTPlayMode.Observer)
                {
                    msg = "正在观战";
                }
                else if (this.modelPRT.playMode == PVPRTPlayMode.Playback)
                {
                    msg = "录像回放";
                }
                
            }

            PVPRTUI.Instance.UIPingInfo(msg);
        }

        /// <summary>
        /// 角色发送emoji表情
        /// </summary>
        /// <param name="e"></param>
        private void _SyncPlayerEmoji(KHEvent e)
        {
            SyncCmd cmd = e.data as SyncCmd;
            if (cmd.args.Count > 0)
            {
                PVPRTUI.Instance.UIShowEmoji(cmd);
            }
        }

        private void _RefreshCallAssistantUI(KHEvent e)
        {
            Debuger.Log("_RefreshCallAssistantUI()");
            List<int> args = e.data as List<int>;
            PVPRTUI.Instance.UIBeginAssistant(args);
        }

        /// <summary>
        /// 角色在线状态UI提示, 我自己掉线不用在UI上展示
        /// </summary>
        /// <param name="e"></param>
        private void _SyncPlayerOnlineState(KHEvent e)
        {
            Debuger.Log("_SyncPlayerOnlineState()");
            SyncCmd item = e.data as SyncCmd;
            if (item.args.Count == 0)
            {
                return;
            }

            PVPRTTeamData teamData = modelPRT.GetTeamDataByTeamID(item.playerId);
            if (teamData != null)
            {
                teamData.onlineState = item.args[0];
            }

            //if (item.arg != modelPRT.pvpMineTeamID)
            {
                PVPRTUI.Instance.UpdateOnlineState(item);
            }
        }

        
        #region  最后的战斗结算面板


        private void _InterruptGuide()
        {
            Debuger.Log("_InterruptGuide()");
            if (modelPRT.useGuide)
            {
                PVPGuide.Interrupt();
            }
        }

        private void _BeginGuide()
        {
            Debuger.Log("_BeginGuide()");
            if (modelPRT.useGuide)
            {
                PVPGuide.Begin(runtimePRT);
            }
        }

        /// <summary>
        /// 结算的面板弹出...
        /// </summary>
        /// <param name="message"></param>
        private void _PVP_1V1_GameOver_Info(object message)
        {
            Debuger.Log("_PVP_1V1_GameOver_Info()");
            Debug.LogWarning("_PVP_1V1_GameOver_Info 当前场景" + KHGlobalExt.app.CurrentContext.contextName);
            if (_isShowResultWin) return;
            
            if (modelPRT.playMode == PVPRTPlayMode.Normal)
            {
                ///结束战斗 战斗结束关闭断线重连
                runtimePRT.CloseTimeout();
                _DelayCallTimer = UIInvokeLater.Invoke(30, TimeOutCloseUdpSend);
            }
            else
            {
                ///结束战斗删除UDP连接
                runtimePRT.BattleEnd();
            }

            PVPRTUI.Instance.UICloseEmoji();

            if (message == null) return;

            _isGameOver = true;

            _InterruptGuide();
            ///去掉侦听
            _watcher.UnListen();

            Debuger.Log("PVP - _PVP_1V1_GameOver_Info");

            ZonePvp1v1GameOverNtf ntf = message as ZonePvp1v1GameOverNtf;
            RetInfo retInfo = ntf.ret_info;

            int retcode = (retInfo != null ? retInfo.ret_code : -1);
            _errormsg = (retInfo != null ? retInfo.ret_msg : "");


            //GameEnter协议是在 PVPRealTimeMainUIOperation类里处理的，
            //与GameOver协议不对称在同一个类里
            //由于现有系统已经存在不对称，那么只能不对称了
            PVPRecorder.Instance.Stop(ntf);
            if (retcode == (int) ZoneErr.ZONE_ERR_PVP_1v1_ABNORM)
            {
                PVPRecorder.Instance.Upload();

                if (LogUtil.RtLogLevel > 0)
                {
                    KH.KHDebugerGUI.UploadRecentLog2Cos((int)modelPRT.pvpServerType, modelPRT.GetTeamPlayerGids());
                }
            }

            ///使用的APPLE录相功能
            if (_useReplayKit)
            {
                _useReplayKit = false;
                ReplayKitPlugin.StopReplay();
            }
            // 自动化播录像检测作弊
            if (PlayRecordDebugMgr.isDebugerVersion)
            {
                PlayRecordDebugMgr.Instance.ReportPVPGameResult();
            }

            if (DefineExt.IsSlimVersion)
            {
                Slim.EnterPvpRealTimeHelper.ReportGameResult();
                runtimePRT.BattleEnd();
                OnTerminate(null);
                return;
            }

            if (retcode == 0 && string.IsNullOrEmpty(_errormsg))
            {
                #region  正常退出信息

                string netWorkStateTips = ntf.network_tips != null ? System.Text.Encoding.Default.GetString(ntf.network_tips) : "";

                List<PVPStatisResult> list = null;// PVPStatis.Instance.GetPVPStatisResults((ParentPlugin as PVPRealTimePlugin).GetModelType() != (int)PVPRTPlayMode.AssistantMode);

                if ((ParentPlugin as PVPRealTimePlugin).GetModelType() == (int)PVPRTPlayMode.AssistantMode)
                {
                    list = PVPStatis.Instance.GetMultiTeamPVPStatiss();
                }
                else
                {
                    list = PVPStatis.Instance.GetPVPStatisResults();
                }

                SendRoundStatisData(ntf.change_score, ntf.rival_change_score, ntf.game_id, ntf.pvp_type, list);

                if (list.Count > 1)
                {
                    BattleFinalResultItemData myResult = null;
                    BattleFinalResultItemData enemyResult = null;
                    PVPPlayerStatisData myStatis = new PVPPlayerStatisData();
                    PVPPlayerStatisData enemyStatis = new PVPPlayerStatisData();
                    PVPPlayerStatisData tempStatis = new PVPPlayerStatisData();
                    List<PVPPlayerStatisData> stasticResults = new List<PVPPlayerStatisData>();

                    for (int i = 0; i < list.Count; i++)
                    {
                        PVPStatisResult item = list[i];

                        BattleFinalResultItemData result = new BattleFinalResultItemData
                        {
                            name = item.name,
                            plat_pic = item.plat_pic,
                            hitCount = item.maxComboHit,
                            hurt = item.totalDamge,
                            score = ntf.change_score,
                            rankScore = item.teamScore,
                            lose = item.usedNinjaCnt,
                            ninjaIds = item.ninjaIds,
							useNinjaCount = item.usedNinjaCnt,
                            zoneId = item.zoneId,
                            playGid = item.playerGid,
                            isSweep = (ntf.is_self_continue_win_score_add == 1),
                            cur_pic_frame = item.cur_pic_frame
                        };

                        tempStatis = new PVPPlayerStatisData();
                        tempStatis.listNinjaStatis = item.listNinjaStatis;

                        if (item.isMine)
                        {
                            //myGroupResult
                            myResult = result;
                            myStatis.listNinjaStatis = item.listNinjaStatis;

                            result.winning_streak = PVPStatis.Instance.winning_streak;
                            result.action_loss = PVPStatis.Instance.action_loss;
                            result.protectReason = ntf.protect_reason;
                        }
                        else
                        {
                            enemyResult = result;
                            enemyStatis.listNinjaStatis = item.listNinjaStatis;
                            enemyResult.isSweep = (ntf.is_rival_continue_win_score_add == 1);
                            enemyResult.score = ntf.rival_change_score;
                            result.winning_streak = PVPStatis.Instance.rival_winning_streak;
                            result.action_loss = PVPStatis.Instance.rival_action_loss;
                            result.protectReason = ntf.rival_protect_reason;
                        }

                        stasticResults.Add(tempStatis);
                    }



                    if (enemyResult != null && myResult != null)
                    {
                        int winTimes = enemyResult.lose - myResult.lose;

                        ///以服务器的结果为准
                        if (ntf.result == FightResult.E_FIGHT_LOSE)
                        {
                            if (winTimes >= 0) winTimes = -1;
#if PandoraSwitch
                            if (PandoraPlugin.pandoraSwitch)
                            {
                                PandoraEntranceModel _model = KHPluginManager.Instance.GetModel(PandoraEntrancePlugin.pluginName) as PandoraEntranceModel;
                                if (_model != null)
                                {
                                    _model.popType = PandoraEntranceModel.EntrancePopType.PvpFail;
                                }
                            }
#endif

                        }
                        else if (ntf.result == FightResult.E_FIGHT_DEUCE)
                        {
                            winTimes = 0;
                        }
                        else if (ntf.result == FightResult.E_FIGHT_WIN_PERFECT)
                        {
                            winTimes = 2;
                        }
                        else
                        {
                            winTimes = 1;
                        }

                        BattleFinalResultData data = new BattleFinalResultData
                        {
                            wins = winTimes,
                            myResult = myResult,
                            statisticResults = stasticResults,
                            enemyResult = enemyResult,
                            myStatis = myStatis,
                            enemyStatis = enemyStatis
                        };

                        data.roundResults = PVPStatis.Instance.GetPVPRoundResults();

                        ///是否有录相...
                        data.isLuxiang = modelPRT.useReplayKit;
                        data.showMode = modelPRT.pvpType;
						data.pvpServerType = modelPRT.pvpServerType;
                        data.isMoment = isMomentOpen;
						data.isMineTeam1P = modelPRT.isMineTeam1P;
						data.ShowFightAgain = ntf.show_fight_again;
						data.pkType = (PvpPkType)ntf.pk_type;
                        // 这里先暂时兼容下, 等下次协议同步之后, 就可以完全依赖后台值了
                        data.fightAgainValidSeconds = ntf.fight_again_valid_seconds <= 0 ? PvpBattleResult.DEFAULT_FIGTH_AGAIN_TIME_OUT : ntf.fight_again_valid_seconds;
                        // pc 测试
#if UNITY_EDITOR
                        data.isMoment = true;
#endif
                        data.scores = ntf.score_list;
                        data.netWorkStr = netWorkStateTips;
                        if (modelPRT.pvpResultViewName != "")
                        {
                            ///显示最后的结算
                            KHBattleManager.Instance.BattlePlugin.ShowView(modelPRT.pvpResultViewName, false, data);
                        }
                        else
                        {
                            ///显示最后的结算
                            KHBattleManager.Instance.BattlePlugin.ShowView(UIDef.PVP_REALTIME_BATTLE_FINAL_RESULT, false,
                                data);
                        }

                        if (_DelayCallTimer != null)
                        {
                            _DelayCallTimer.StopImmediately();
                            _DelayCallTimer = null;
                        }

                        _isShowResultWin = true;

                        runtimePRT.StopPlot();

                        return;
                    }
                }

                #endregion
            }


            //如果没有正常退出，则判断是否需要对OBStream进行处理
            if (retcode == 0 && string.IsNullOrEmpty(_errormsg) && 
                modelPRT.playMode == PVPRTPlayMode.OBStream)
            {
                //如果还没有Loading完就收到正常的GameOverNtf，那么，就是在OB进入观战时，PVP刚好结束，这个时候给一个提示吧
                _errormsg = "战斗已经结束了，请退出观战!";
                
                #region 对OBStream的特殊处理
                /*
                //如果是OBStream模式，并无法以上面的方式正常退出，则以NTF数据为准
                BattleFinalResultData data = new BattleFinalResultData();

                ///以服务器的结果为准
                if (ntf.result == FightResult.E_FIGHT_LOSE)
                {
                    data.wins = -1;
                }
                else if (ntf.result == FightResult.E_FIGHT_DEUCE)
                {
                    data.wins = 0;
                }
                else if (ntf.result == FightResult.E_FIGHT_WIN_PERFECT)
                {
                    data.wins = 2;
                }
                else
                {
                    data.wins = 1;
                }

                ///是否有录相...
                data.isLuxiang = modelPRT.useReplayKit;
                data.showMode = modelPRT.pvpType;
                data.isMoment = isStartMomentSuccess;

                data.roundResults = new List<BattleRoundResultData>();

                data.myResult = new BattleFinalResultItemData();
                data.myResult.score = ntf.change_score;
                data.myResult.ninjaIds = new List<int>();

                data.enemyResult = new BattleFinalResultItemData();
                data.enemyResult.score = ntf.rival_change_score;
                data.enemyResult.ninjaIds = new List<int>();

                if (modelPRT.pvpResultViewName != "")
                {
                    ///显示最后的结算
                    KHBattleManager.Instance.BattlePlugin.ShowView(modelPRT.pvpResultViewName, false, data);
                }
                else
                {
                    ///显示最后的结算
                    KHBattleManager.Instance.BattlePlugin.ShowView(UIDef.PVP_REALTIME_BATTLE_FINAL_RESULT, false,
                        data);
                }

                _isShowResultWin = true;

                return;
                 * */
                #endregion
            }

            _errormsg = ErrorCodeCenter.GetErrorStringEx(retcode, _errormsg);

            if (string.IsNullOrEmpty(_errormsg))
            {
                _errormsg = "未知错误，退出战斗!";
            }

            if (_isLoadingStart)
            {
                UIAPI.alert("提示", _errormsg, 0, _error_gameover_click, null, true);
            }
        }

        [Operation("ExitRTPVP")]
        public void ExitRTPVP(object message)
        {
            Debuger.Log("ExitRTPVP()");
            if (message == null) return;

            _isGameOver = true;

            ZonePvp1v1GameOverNtf ntf = message as ZonePvp1v1GameOverNtf; //其他战斗结算的notify都先转成这个
            RetInfo retInfo = ntf.ret_info;

            if (_isShowResultWin) return;
            ///结束战斗删除UDP连接
            runtimePRT.BattleEnd();
            if (_DelayCallTimer != null)
            {
                _DelayCallTimer.StopImmediately();
                _DelayCallTimer = null;
            }

            _InterruptGuide();
            ///去掉侦听
            _watcher.UnListen();


            int retcode = (retInfo != null ? retInfo.ret_code : -1);
            _errormsg = (retInfo != null ? retInfo.ret_msg : "");

            //GameEnter协议是在 PVPRealTimeMainUIOperation类里处理的，
            //与GameOver协议不对称在同一个类里
            //由于现有系统已经存在不对称，那么只能不对称了
            PVPRecorder.Instance.Stop(ntf);
            if (retcode == (int)ZoneErr.ZONE_ERR_PVP_1v1_ABNORM)
            {
                PVPRecorder.Instance.Upload();
            }  //这个后面还是改到业务系统关闭比较好

            ///使用的APPLE录相功能
            if (_useReplayKit)
            {
                _useReplayKit = false;
                ReplayKitPlugin.StopReplay();
            }

            if (retcode == 0 && string.IsNullOrEmpty(_errormsg))
            {
                Debuger.Log("[PVP结算成功]");
                _isShowResultWin = true;
                SendRoundStatisData(ntf.change_score, ntf.rival_change_score, ntf.game_id, ntf.pvp_type);
                return;
            }

            _errormsg = ErrorCodeCenter.GetErrorStringEx(retcode, _errormsg);

            if (string.IsNullOrEmpty(_errormsg))
            {
                _errormsg = "未知错误，退出战斗!";
            }

            if (_isLoadingStart)
            {
                Debuger.Log("ExitRTPVP");
                UIAPI.alert("提示", _errormsg, 0, _error_gameover_click, null, true);
            }
        }

        /// <summary>
        /// 上报决斗场统计数据
        /// </summary>
        private void SendRoundStatisData(int myScore, int rivalScore, UInt64 gameId, int pvpType, List<PVPStatisResult> lstStatisResult = null)
        {
            Debuger.Log("SendRoundStatisData()");
            ZoneTssPVPBattleEndStatisInfoReq req = new ZoneTssPVPBattleEndStatisInfoReq();
            req.info = new ZoneTssPVPBattleEndStatisInfo();

            List<PVPNinjaStatisData> lstMyNinjaStaticDatas = null;
            List<PVPNinjaStatisData> lstEnemyNinjaStaticDatas = null;

            #region 回合数据
            uint useTime = 0;

            List<BattleRoundStatisData> lstStaticDatas = PVPStatis.Instance.GetPVPRoundStatisData();

            for (int i = 0; i < lstStaticDatas.Count; i++)
            {
                BattleRoundStatisData data = lstStaticDatas[i];

                ZoneTssPVPBattleEndStatisInfo.RoundStatisInfo roundStatisInfo = new ZoneTssPVPBattleEndStatisInfo.RoundStatisInfo();
                roundStatisInfo.result = data.result;
                roundStatisInfo.myNinjaId = data.myNinjaId;
                roundStatisInfo.myMaxComboHit = data.myMaxComboHit;
                roundStatisInfo.myTotalDamge = data.myTotalDamge;
                roundStatisInfo.enemyNinjaId = data.enemyNinjaId;
                roundStatisInfo.enemyMaxComboHit = data.enemyMaxComboHit;
                roundStatisInfo.enemyTotalDamge = data.enemyTotalDamge;
                req.info.round_statis_info_list.Add(roundStatisInfo);

                useTime += data.useTime;

                Debuger.Log(string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}", data.myNinjaId, data.myMaxComboHit, data.myTotalDamge, data.enemyNinjaId, data.enemyMaxComboHit, data.enemyTotalDamge, data.useTime));
            }
            #endregion 

            #region 忍者数据

            if (lstStatisResult == null)
            {
                lstStatisResult = PVPStatis.Instance.GetPVPStatisResults();
            }

            if (lstStatisResult.Count > 1)
            {
                for (int i = 0; i < lstStatisResult.Count; i++)
                {
                    PVPStatisResult item = lstStatisResult[i];

                    if (item.isMine)
                    {
                        lstMyNinjaStaticDatas = item.listNinjaStatis;
                    }
                    else
                    {
                        lstEnemyNinjaStaticDatas = item.listNinjaStatis;
                    }
                }
            }

            if (lstMyNinjaStaticDatas != null && lstEnemyNinjaStaticDatas != null)
            {
                for (int i = 0; i < lstMyNinjaStaticDatas.Count; i++)
                {
                    PVPNinjaStatisData pvpNinjaStatisData = lstMyNinjaStaticDatas[i];

                    ZoneTssPVPBattleEndStatisInfo.NinjaStatisInfo info = new ZoneTssPVPBattleEndStatisInfo.NinjaStatisInfo();
                    info.ninjaId = pvpNinjaStatisData.ninjaId;
                    info.totalDamage = pvpNinjaStatisData.totalDamage;
                    info.maxCombo = pvpNinjaStatisData.maxCombo;
                    info.selfNormalAttackHitCount = pvpNinjaStatisData.totalSelfNormalAttackHitNum;
                    info.selfSkillHitCount = pvpNinjaStatisData.totalSelfSkillHitNum;
                    info.totalCtrlTime = KHUtil.FloatToInt(pvpNinjaStatisData.totalCtrlTime);
                    info.selfSkillHitRate = KHUtil.FloatToInt(pvpNinjaStatisData.selfSkillHitRate, 100);
                    info.enemySkillAvoidRate = KHUtil.FloatToInt(pvpNinjaStatisData.enemySkillAvoidRate, 100);
                    req.info.my_ninja_statis_info_list.Add(info);
                }

                for (int i = 0; i < lstEnemyNinjaStaticDatas.Count; i++)
                {
                    PVPNinjaStatisData pvpNinjaStatisData = lstEnemyNinjaStaticDatas[i];

                    ZoneTssPVPBattleEndStatisInfo.NinjaStatisInfo info = new ZoneTssPVPBattleEndStatisInfo.NinjaStatisInfo();
                    info.ninjaId = pvpNinjaStatisData.ninjaId;
                    info.totalDamage = pvpNinjaStatisData.totalDamage;
                    info.maxCombo = pvpNinjaStatisData.maxCombo;
                    info.totalCtrlTime = KHUtil.FloatToInt(pvpNinjaStatisData.totalCtrlTime);
                    info.selfSkillHitRate = KHUtil.FloatToInt(pvpNinjaStatisData.selfSkillHitRate, 100);
                    info.enemySkillAvoidRate = KHUtil.FloatToInt(pvpNinjaStatisData.enemySkillAvoidRate, 100);
                    req.info.enemy_ninja_statis_info_list.Add(info);
                }
            }
            #endregion


            #region 基础数据
            req.info.gid = RemoteModel.Instance.Player.Gid;
            req.info.rival_gid = GetRivalGid();
            req.info.score = myScore;
            req.info.rival_score = rivalScore;
            req.info.game_time = useTime;
            req.info.game_id = gameId;
            req.info.pvp_type = pvpType;
            req.info.reserve1 = KHUtil.GetMatchDeviceType(true); //作弊检测
            #endregion

            

            NetworkManager.Instance.Send<ZoneTssPVPBattleEndStatisInfoReq>((uint)ZoneCmd.ZONE_TSS_LOG_PVP_BATTLE_END, req,
                (msg) =>
                {
                    
                });
        }

        private ulong GetRivalGid()
        {
            Debuger.Log("GetRivalGid()");
            for (int i = 0; i < modelPRT.teamDatas.Count; i++)
            {
                PVPRTTeamData teamData = modelPRT.teamDatas[i];
                if (teamData.teamID != modelPRT.pvpMineTeamID)
                {
                    return teamData.playerGid;
                }
            }

            return 0;
        }

        /// <summary>
        ///  UDP的连接错误...
        /// </summary>
        /// <param name="code"></param>
        private void _udp_error_result(int code)
        {
            Debuger.Log("_udp_error_result()");
            //UIAPI.ShowMsgTip("网络异常, 退出战斗！");
            //KHGlobalExt.app.SwitchScene(KHLevelName.GAME);

            if (string.IsNullOrEmpty(_errormsg))
                _errormsg = "网络异常断开, 退出战斗！";
            
            ///结束战斗删除UDP连接
            runtimePRT.BattleEnd();
            if (_DelayCallTimer != null)
            {
                _DelayCallTimer.StopImmediately();
                _DelayCallTimer = null;
            }

            if (_isBuild)
            {
                Debuger.Log("_udp_error_result");
                UIAPI.alert("提示", _errormsg, 0, _error_gameover_click, null, true);
            }
        }

        private void _error_gameover_click(int code)
        {
            Debuger.Log("_error_gameover_click()");
            ExitBattle(null);
        }

        #endregion

        static private KHVoidFunction _END_BATTLE_VOID = null;

        static private void _End_battle_func()
        {
            Debuger.Log("_End_battle_func()");
            KHVoidFunction endFunc = _END_BATTLE_VOID;
            _END_BATTLE_VOID = null;

            if (endFunc != null)
                endFunc();
        }

        [Operation(BattleOperation.PVP_ExitBattle)]
        public void ExitBattle(object data = null)
        {
            Debuger.Log("ExitBattle() [PVP - ExitBattle] _endFunction == null ?" + (_endFunction == null).ToString());
            Debug.LogWarning("ExitBattle 当前处于" + KHGlobalExt.app.CurrentContext.contextName);

            if (_endFunction != null)
            {
                _END_BATTLE_VOID = _endFunction;
                _endFunction = null;

                if (_END_BATTLE_VOID != null)
                {
                    KHGlobalExt.app.MessageQueue.Enqueue(new ContextMessage()
                    {
                        Message = _endFuncType,
                        Data = new ContextMessage.ContextCallback(_End_battle_func)
                    });
                }
            }

            ///结束战斗删除UDP连接
            runtimePRT.BattleEnd();
            if (_DelayCallTimer != null)
            {
                _DelayCallTimer.StopImmediately();
                _DelayCallTimer = null;
            }

            ///不恢复当局中屏蔽表情的标志位
            //modelPRT.emojiShield = false;
            
            OnTerminate(null);
            
        }

        private void TimeOutCloseUdpSend()
        {
            Debuger.Log("TimeOutCloseUdpSend");
            runtimePRT.CloseSend();
        }

        private void OnEnterFrame(KHEvent e)
        {
            if (KHBattle._FrameIndex % _checksumReportInterval == 0)
            {
                if (!_isRoundEnd)
                {
                    PVPAntiCheat.Instance.SendBattleRuntimeCheckSum();
                }
            }
        }
    }

    /// <summary>
    /// 超时侦听器...
    /// </summary>
    [Hotfix]
    class PVPRTQuitWatcher
    {
        /// <summary>
        /// 是否在侦听...
        /// </summary>
        private bool _islisten = false;
        private AlertClick _click = null;
        private float _beginTime = 0;
        private float _timeout = 30.0f;
        private IUIAlert _alertUI = null;
        private KHVoidFunction _InterruptGuide;

        public PVPRTQuitWatcher(KHVoidFunction interGuide)
        {
            _InterruptGuide = interGuide;
        }

        private void __timeout_click(int code)
        {
            _alertUI = null;
            AlertClick callback = _click;

            UnListen();

            if (callback != null)
                callback(code);
        }

        private void OnGameTimeUpdate(KHEvent e)
        {
            if (!_islisten || _alertUI != null) return;

            if (RealTime.time - _beginTime >= _timeout)
            {
                ///已经超时...
                _alertUI = UIAPI.alert("提示", "服务器响应超时!", 0 , __timeout_click, null, true);

                if (_InterruptGuide != null)
                    _InterruptGuide();
            }
        }

        public void Listen(AlertClick click, float timeout = 30.0f)
        {
            if (_islisten) return;

            Debuger.Log("[Listen] 000000000000 开始监听");

            _timeout = timeout;
            _islisten = true;
            _click = click;
            _beginTime = RealTime.time;

            KHGlobal.dispatcher.addEventListener(KHGameEvent.GAME_TIME_UPDATE, OnGameTimeUpdate);
        }

        public void UnListen()
        {
            if (!_islisten) return;

            Debuger.Log("[Listen] 11111111111111 结束监听");

            _islisten = false;
            _click = null;
            KHGlobal.dispatcher.removeEventListener(KHGameEvent.GAME_TIME_UPDATE, OnGameTimeUpdate);

            if (_alertUI != null)
            {
                _alertUI.close();
                _alertUI = null;
            }
        }
    }
}
