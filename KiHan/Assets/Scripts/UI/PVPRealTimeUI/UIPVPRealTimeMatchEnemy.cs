using UnityEngine;
using System.Collections.Generic;
using KH;
using naruto.protocol;
using KH.Plugins;
using ProtoBuf;
using System.IO;
using System.Text;
using KH.Network;

public enum PvpMatchType{
	Entertainment,
}

public class MatchEnemyData{
    public int expectSeconds;
    public int lastSeconds;
    public float startTime;
    public bool isFriend;
    public string friendName;
	public PvpMatchType matchType;
    // 标记是否是再战一次
    public bool isFightAgain = false;

}

public class FightAgainData{
	public uint zoneid;
	public ulong player_id;
	public string name;
	public uint pvp_type;
}

public class MatchPvPAct
{
    public uint zoneid;
    public string player_id;
    public string name;
    public int act_id;
    public int remain_times;
    public uint pvp_type;
}

//好友等待及匹配等待界面
public class UIPVPRealTimeMatchEnemy : UIWindow
{
    private PVPRealTimeMainUIModel model;
    private PVPRealTimeMainUIPlugin plugin;

    public UILabel WaitSecondsLabel;
    public UILabel EstimatedTimeLabel;
    public UILabel DescLabel;
    public UILabel CancelLabel;
    public GameObject EstimatedContainer;
    public GameObject Button;
	public UIPlayAnimation MatchAni;

    MatchEnemyData data;
    BriefRoleInfo friend;
    MatchPvPAct actData;
    int timeoutSeconds = 60;
    int friendTimeoutSeconds = 60;
    int actTimeoutSeconds = 10;
    bool isStarted = false;

    public override void OnInitData(object _data)
    {
        Debug.LogWarning("OnInitData 当前场景" + KHGlobalExt.app.CurrentContext.contextName);
        isExiting = false;
        isMatchReturn = false;
        actData = null;
        plugin = KHPluginManager.Instance.GetPluginByName(PVPRealTimeMainUIPlugin.PluginName) as PVPRealTimeMainUIPlugin;
        model = plugin.Model as PVPRealTimeMainUIModel;
        data =  _data as MatchEnemyData;

        //// Update by Chicheng
        //MessageManager msgManager = MessageManager.Instance;
        //if (msgManager.IsDeserializeFromLocal)
        //{
        //    // KHGlobalExt.app.SwitchScene(KHLevelName.DUPLICATE);
        //    KHGlobalExt.app.CurrentContext.SendSingal("Switch", new SwitchContextArgument()
        //    {
        //        ToContext = "Duplicate"
        //    });

        //    Debug.LogWarning("切换后场景" + KHGlobalExt.app.CurrentContext.contextName);
        //    NetworkManager.Instance.AddMessageCallback((uint)ZoneCmd.ZONE_PVP_1V1_GAME_OVER_NTF, _PVP_1V1_GameOver_Info);
        //    return;
        //}


        /// _data可能有三种含义: MatchEnemyData(正常匹配), FightAgainData(与其再战), BriefRoleInfo(好友切磋)
        /// 这里写的太乱了, 有空的时候重构下
        FightAgainData againData = _data as FightAgainData;
		if(againData != null)
		{
            ShowForFightAgain(againData);
			return;
		}

        if (data == null) {
            friend = _data as BriefRoleInfo;
            if( friend == null )
            {
                // 用于活动主播接受队列某个玩家的挑战, 也是用邀请好友这一套
                actData = _data as MatchPvPAct;
                friend = new BriefRoleInfo();
                
                friend.name = actData.name;
                ulong player_id = 0;
                ulong.TryParse(actData.player_id, out player_id);
                friend.player_id = player_id;
                friend.zoneid = actData.zoneid;
            }
            data = new MatchEnemyData();
            data.isFightAgain = false;
            data.lastSeconds = 0;
            data.isFriend = true;
            data.expectSeconds = -1;
            data.friendName = friend.name;
            data.matchType = PvpMatchType.Entertainment;
            NetworkManager.Instance.AddMessageCallback((int)ZoneCmd.ZONE_PVP_1V1_ACCEPT_NTF, OnFriendAccept);
            InviteFriend(friend);
        } else {
            SendMatchEnemyReq(data);
        }
        NetworkManager.Instance.AddMessageCallback((int)ZoneCmd.ZONE_PVP_1V1_ENTER_GAME_NTF, MatchSuccNtf);
        NetworkManager.Instance.AddMessageCallback((int)ZoneCmdLua.ZONE_LUA_JDC_PVP_BAN_PICK_ROOM_MATCH_NTF, BanMatchSuccNtf);
		if(!data.isFriend){
            NGUITools.SetActive(EstimatedContainer.gameObject, false);
            //EstimatedTimeLabel.text = data.expectSeconds + "秒";
            DescLabel.text = "正在寻找旗鼓相当的对手...";
            CancelLabel.text = "取消匹配";
            NGUITools.SetActive(Button, false);
        }else{
            NGUITools.SetActive(EstimatedContainer.gameObject, false);
            DescLabel.text = "正在邀请 " + data.friendName + " 进行切磋";
            CancelLabel.text = "取消邀请";
            NGUITools.SetActive(Button, false);
        }
        BindingTargetHelper.AddListen (this);
    }

	public void ShowForFightAgain(FightAgainData againData)
    {
        // 构造friend, 取消邀请的时候要用
        friend = new BriefRoleInfo();
        friend.player_id = againData.player_id;
        friend.zoneid = againData.zoneid;
        // 构造matchEnemyData
        data = new MatchEnemyData();
        data.isFightAgain = true;
		data.lastSeconds = 0;
		data.expectSeconds = -1;
        data.isFriend = true;  // 这里要设置为true, 如果为false, 则取消的时候会发送取消匹配的协议
		data.friendName = againData.name;
		data.matchType = PvpMatchType.Entertainment;
		NetworkManager.Instance.AddMessageCallback((int)ZoneCmd.ZONE_PVP_1V1_ACCEPT_NTF, OnFightAgainAccept);
		InviteFightAgain(againData);

		NetworkManager.Instance.AddMessageCallback((int)ZoneCmd.ZONE_PVP_1V1_ENTER_GAME_NTF, MatchSuccNtf);
		NetworkManager.Instance.AddMessageCallback((int)ZoneCmdLua.ZONE_LUA_JDC_PVP_BAN_PICK_ROOM_MATCH_NTF, FightAgainBanMatchSuccNtf);
		NGUITools.SetActive(EstimatedContainer.gameObject, false);
		DescLabel.text = "正在邀请 " + data.friendName + " 再来一战";
		CancelLabel.text = "取消邀请";
		NGUITools.SetActive(Button, false);
		BindingTargetHelper.AddListen (this);
	}

	private void InviteFightAgain(object data){
		FightAgainData againData = data as FightAgainData;
		var req = new ZonePvp1v1InviteReq ();
		req.friend_gid = againData.player_id;
		req.friend_zone = againData.zoneid;
		req.invite = true;
		req.plat_friend = false;
		req.fight_again = true;
        req.pvp_type = againData.pvp_type;

		NetworkManager.Instance.Send<ZonePvp1v1InviteReq>((uint)ZoneCmd.ZONE_PVP_1V1_INVITE, req, (object fullInfo) =>
		                                                  {
			ZonePvp1v1InviteResp resp = fullInfo as ZonePvp1v1InviteResp;
			if (resp.ret_info.ret_code == 0) {
				isStarted = true;
			} else {
				
				ErrorCodeCenter.DefaultProcError(resp.ret_info);
				_CloseWindow(null);
			}
		}, false,  timeoutCallback => {
			UIAPI.ShowMsgTip("邀请再来一战超时");
			_CloseWindow(null);
		});
	}

	private void OnFightAgainAccept(object message){
		var ntf = message as ZonePvp1v1AccepteNtf;
		if (!ntf.accept)
		{            
			_CloseWindow(null);
			
			if (!string.IsNullOrEmpty(ntf.reason))
			{
				UIAPI.ShowMsgTip(ntf.reason);
			}
			else
			{
				if (!ntf.is_fight)
				{
					UIAPI.ShowMsgTip("对方已拒绝你的邀请");
				}
				else
				{
					if(ntf.reason == "")
					{
						UIAPI.ShowMsgTip("对方当前状态不能接受邀请");
					}
					else
					{
						UIAPI.ShowMsgTip(ntf.reason);
					}
				}
			}
		}
	}

    public override void OnReOpenWindow(object _data)
    {
        OnDisable();
        OnInitData(_data);
    }

    public override void OnPlayOpenWindowAniComplete()
	{
		base.OnPlayOpenWindowAniComplete();
		MatchAni.clipName = "CircleAnimation";
		MatchAni.playDirection = AnimationOrTween.Direction.Forward;
		MatchAni.onFinished.Clear();
		MatchAni.Play(true, false);
	}

    void Update ()
    {
        if (data == null)
            return;
        if (!isStarted)
            return;
        //float tmp = Time.deltaTime;
        if (data.startTime == 0) {
            data.startTime = Time.time;
            data.lastSeconds = 0;
            WaitSecondsLabel.text = data.lastSeconds + "秒";
        }
        int lastSeconds = (int)(Time.time - data.startTime);
        if (data.lastSeconds != lastSeconds) {
            data.lastSeconds = lastSeconds;
            WaitSecondsLabel.text = data.lastSeconds + "秒";
            if(lastSeconds == 5){

                if( actData == null ) //不是活动，才现实取消按钮
                {
                    NGUITools.SetActive(Button, true);
                }                
            }
        }
        if (!data.isFriend) {
            if (!isExiting && lastSeconds >= timeoutSeconds) {
                UIAPI.ShowMsgTip("匹配战斗超时");
                this.OnClickCancelBtn ();
                isExiting = true;
            }
        } else {
            if( actData == null )
            {
				string timeOutTips = "邀请好友比试超时";
				if (data.isFightAgain)
				{
					timeOutTips = "与其再战邀请超时";
				}

                if (!isExiting && lastSeconds >= friendTimeoutSeconds)
                {
                    this.OnClickCancelBtn();
					UIAPI.ShowMsgTip(timeOutTips);
                    isExiting = true;
                }
            }
            else
            {
                string timeOutTips = "邀请好友比试超时";
                if (data.isFightAgain)
                {
                    timeOutTips = "与其再战邀请超时";
                }

                if (!isExiting && lastSeconds >= actTimeoutSeconds)
                {
                    this.OnClickCancelBtn();
                    UIAPI.ShowMsgTip(timeOutTips);
                    isExiting = true;
                }
            }
        }

    }

    bool isExiting = false;
    public void OnClickCancelBtn(){
        if (isMatchReturn)
            return;
        if (!data.isFriend)
            plugin.SendMessage ("SendCancelMatchEnemyReq");
        else {
            if ( actData == null )
            {
                KHPluginManager.Instance.GetPluginByName(PVPRealTimeMainUIPlugin.PluginName).SendMessage("CancelInviteFriend", friend);
            }
            else
            {
                KHPluginManager.Instance.GetPluginByName(PVPRealTimeMainUIPlugin.PluginName).SendMessage("ActCancelInviteFriend", friend);
                if ( actData.remain_times > 0)
                {
                    ActCenterPlugin actPlugin = KH.KHPluginManager.Instance.GetPluginByName(KH.Plugins.ActCenterPlugin.NAME) as KH.Plugins.ActCenterPlugin;
                    actPlugin.InvokeActFun((uint)actData.act_id, "ReqCmd7");
                }
                else
                {
                    _CloseWindow(null);
                }
            }
            
        }

        if ( actData == null )
        {
            LoadingTipStack.Show("等待取消中");
        }
        
    }

    [BindingTarget(PVPRealTimeMainUIPlugin.PluginName, "CloseMatchWindow")]
    public void _CloseWindow(object data)
    {
        if( this == null )
        {
            return;
        }
        if (!this.IsAnimating){
            this.PlayCloseWindowAni (false);
        }else{
            CloseWindow();
        }
    }

    [BindingTarget(PVPRealTimeMainUIPlugin.PluginName, "SetRemainTimesToZero")]
    public void _SetRemainTimesToZero(object data)
    {
        if (this == null)
        {
            return;
        }

        if (actData != null)
        {
            actData.remain_times = 0;
        }

    }

    private void OnDisable(){
        if (data.isFriend) {
            NetworkManager.Instance.RemoveMessageCallback((int)ZoneCmd.ZONE_PVP_1V1_ACCEPT_NTF, OnFriendAccept);
        }
        data = null;
        NetworkManager.Instance.RemoveMessageCallback ((int)ZoneCmd.ZONE_PVP_1V1_ENTER_GAME_NTF, MatchSuccNtf);
        NetworkManager.Instance.RemoveMessageCallback((int)ZoneCmdLua.ZONE_LUA_JDC_PVP_BAN_PICK_ROOM_MATCH_NTF, BanMatchSuccNtf);
        NetworkManager.Instance.RemoveMessageCallback((int)ZoneCmdLua.ZONE_LUA_JDC_PVP_BAN_PICK_ROOM_MATCH_NTF, FightAgainBanMatchSuccNtf);

        BindingTargetHelper.RemoveListen(this);
        isStarted = false;
    }

    private void OnFriendAccept(object message){
        var ntf = message as ZonePvp1v1AccepteNtf;
        if (!ntf.accept)
        {            
            if (actData != null && actData.remain_times > 0)
            {
                ActCenterPlugin plugin = KH.KHPluginManager.Instance.GetPluginByName(KH.Plugins.ActCenterPlugin.NAME) as KH.Plugins.ActCenterPlugin;
                plugin.InvokeActFun((uint)actData.act_id, "ReqCmd7");
            }
            else
            {
                _CloseWindow(null);
            }


            if (!string.IsNullOrEmpty(ntf.reason))
            {
                UIAPI.ShowMsgTip(ntf.reason);
            }
            else
            {
                if (!ntf.is_fight)
                {
                    UIAPI.ShowMsgTip("好友已拒绝你的比试邀请");
                }
                else
                {
                    UIAPI.ShowMsgTip("好友当前状态不能接受邀请");
                }
            }

        }
    }

    private void _EndPVPBattle()
    {       
       if( actData != null )
        {
            KHPluginManager.Instance.SendMessage("ActCenter", "ActCenter.GeAllActInfoReq", actData.act_id);

            actData = null;
        }
       else
        {
            KHPluginManager.Instance.GetPluginByName(PVPRealTimeMainUIPlugin.PluginName).SendMessage("QueryPVPRealTimeInfo");

			// 新增检测在结算和loading的过程中是否有收到邀请 有则弹出
			PVPRealTimeFriendModel friendModel = KHPluginManager.Instance.GetModel(PVPRealTimeFriendPlugin.PluginName) as PVPRealTimeFriendModel;
			friendModel.CheckInviteInfo();
        }
    }

    bool isMatchReturn = false;

    private void MatchSuccNtf(object message)
    {
		MatchAni.clipName = "MatchSuccess";
		MatchAni.playDirection = AnimationOrTween.Direction.Forward;
		MatchAni.onFinished.Clear();
		MatchAni.Play(true, false);

		int pvpmodel = 0;
		if(data.matchType == PvpMatchType.Entertainment)
		{
			pvpmodel = 4;
		}
		Invoke("DelayCloseWindow",0.5f);
		isMatchReturn = true;
		ZonePvp1v1EnterGameNtf ntf = message as ZonePvp1v1EnterGameNtf;
		
		PVPRealTimeMainUIOperation.EnterRealTimePVPByParam(new PVPEnterParam(ntf.ret_info,
		                                                                     ntf.team_info_list,
		                                                                     ntf.setting,
		                                                                     pvpmodel,
		                                                                     _EndPVPBattle,
		                                                                     "Callback",
		                                                                     "_pvprealtime",
		                                                                     PVPRTPlayMode.Normal,
		                                                                     "",
		                                                                     "")
		                                                   {
			useIOSReplayKit = model.IOS9ReplayOpen,
			useGuide = ntf.is_rookie == 1
		});
		
		
		if (data != null && data.isFriend) {
			model.TriggerBinding("CloseFriendWindow");
		}
    }

	public void DelayCloseWindow()
	{
		CloseWindow();
	}

    private void BanMatchSuccNtf(object message)
    {
		MatchAni.clipName = "MatchSuccess";
		MatchAni.playDirection = AnimationOrTween.Direction.Forward;
		MatchAni.onFinished.Clear();
		MatchAni.Play(true, false);
		Invoke("DelayCloseWindow",0.5f);
		isMatchReturn = true;
		ZoneLuaRoomMatchNtf ntf = message as ZoneLuaRoomMatchNtf;
		KHPluginManager.Instance.SendMessage("ArenaPlugin", "inviteMatchSuccess", ntf.room_id);
		if (data != null && data.isFriend)
		{
			model.TriggerBinding("CloseFriendWindow");
		}
    }

    // 用于再战一次的回调, 区别是调用的Operation不同
    private void FightAgainBanMatchSuccNtf(object message)
    {
		MatchAni.clipName = "MatchSuccess";
		MatchAni.playDirection = AnimationOrTween.Direction.Forward;
		MatchAni.onFinished.Clear();
		MatchAni.Play(true, false);

		Invoke("DelayCloseWindow",0.5f);
		isMatchReturn = true;
		ZoneLuaRoomMatchNtf ntf = message as ZoneLuaRoomMatchNtf;
		KHPluginManager.Instance.SendMessage("ArenaPlugin", "fightAgainMatchSuccess", ntf.room_id);
		if (data != null && data.isFriend)
		{
			model.TriggerBinding("CloseFriendWindow");
		}
    }

    //先进界面再发请求，以防止未打开界面时，就已经匹配了。
    private void InviteFriend(object data){
        BriefRoleInfo friend = data as BriefRoleInfo;
        var req = new ZonePvp1v1InviteReq ();
        req.friend_gid = friend.player_id;
        req.friend_zone = friend.zoneid;
        req.invite = true;
        var friendModel = KHPluginManager.Instance.GetModel(PVPRealTimeFriendPlugin.PluginName) as PVPRealTimeFriendModel;
        if( actData != null )
        {
            req.pvp_type = actData.pvp_type;
        }
        else
        {
            req.pvp_type = friendModel.banMode ? 1u : 0u;
        }
        

        if (friendModel.platformFriendList.Contains (friend)) {
            req.plat_friend = true;
        } else {
            req.plat_friend = false;
        }
        NetworkManager.Instance.Send<ZonePvp1v1InviteReq>((uint)ZoneCmd.ZONE_PVP_1V1_INVITE, req, (object fullInfo) =>
                                                          {
            ZonePvp1v1InviteResp resp = fullInfo as ZonePvp1v1InviteResp;
            if (resp.ret_info.ret_code == 0) {
                isStarted = true;
                if (actData == null)
                {
                    NGUITools.SetActive(Button, true);
                }
                
            } else {

                ErrorCodeCenter.DefaultProcError(resp.ret_info);

                if (actData != null && actData.remain_times > 0)
                {
                    ActCenterPlugin plugin = KH.KHPluginManager.Instance.GetPluginByName(KH.Plugins.ActCenterPlugin.NAME) as KH.Plugins.ActCenterPlugin;
                    plugin.InvokeActFun((uint)actData.act_id, "ReqCmd7");
                }
                else
                {
                    _CloseWindow(null);
                }

            }
            if(resp.refresh){
                var model = plugin.Model as PVPRealTimeMainUIModel;
                model.TriggerBinding("RefreshFriendList");
            }
        }, false,  timeoutCallback => {
            UIAPI.ShowMsgTip("邀请好友比试超时");

            if (actData != null && actData.remain_times > 0)
            {
                ActCenterPlugin plugin = KH.KHPluginManager.Instance.GetPluginByName(KH.Plugins.ActCenterPlugin.NAME) as KH.Plugins.ActCenterPlugin;
                plugin.InvokeActFun((uint)actData.act_id, "ReqCmd7");
            }
            else
            {
                _CloseWindow(null);
            }

        });
    }

    private void SendMatchEnemyReq(object data){
        PVPRealTimeMainUIModel.MatchType = PVPMatchType.Enemy;
        MatchEnemyData tmpData = data as MatchEnemyData;
		ZonePvp1v1MatchReq req = new ZonePvp1v1MatchReq();
		int state = KHUtil.GetInt(UISettingMoreView.OnlyReal, 0);
		req.only_real_player = state == 1;
		NetworkManager.Instance.Send ((uint)ZoneCmd.ZONE_PVP_1V1_MATCH, req, (object fullInfo) => {
            ZonePvp1v1MatchResp resp = fullInfo as ZonePvp1v1MatchResp;
            if (resp.ret_info.ret_code == 0) {
                tmpData.expectSeconds = (int)resp.expect_wait_second;
                EstimatedTimeLabel.text = tmpData.expectSeconds + "秒";
                NGUITools.SetActive(EstimatedContainer.gameObject, true);
                isStarted = true;
            }else{
                ErrorCodeCenter.DefaultProcError(resp.ret_info);
                _CloseWindow (null);
            }
        },false , onTimeout => {
            UIAPI.ShowMsgTip("匹配请求超时");
            _CloseWindow (null);
        });
    }
}

