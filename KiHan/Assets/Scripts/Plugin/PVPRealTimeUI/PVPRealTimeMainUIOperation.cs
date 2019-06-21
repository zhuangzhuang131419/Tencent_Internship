using UnityEngine;
using System.Collections;
using naruto.protocol;
using System.Collections.Generic;
using System;
using ProtoBuf;
using KH;
using System.IO;
using System.Text;
using KH.Plugins;
using KH.Ninja;
using KH.Network;
using KH.Remote;
using kihan_general_table;
using KH.LanPVP;

namespace KH
{
    public class PVPRealTimeMainUIOperation : Operation
    {
        private static string LOG_TAG = "PVPRealTimeMainUIOperation";

        PVPRealTimeMainUIModel cacheModel = null;

        [Operation("QueryPVPRealTimeInfoForArenaMain")]
        public void QueryPVPRealTimeInfoForArenaMain(object data)
        {
            List<NinjaData> list = RemoteModel.Instance.NinjaCollection.GetReleasedNinjaDatas(true);
            if (list == null /*|| list.Count < 3*/ )
            {
                UIAPI.ShowMsgTip("需要永久拥有3名忍者才能进入");
                return;
            }
            if (cacheModel == null)
                cacheModel = ParentPlugin.Model as PVPRealTimeMainUIModel;
            if (!RemoteModel.Instance.NinjaCollection.HasAllNinjaSynced(RemoteModel.Instance))
            {
                RemoteModel.Instance.NinjaCollection.GetAllActorInfo(successed => allNinjaSyncedDoneForArenaMain(successed), RemoteModel.Instance);
            }
            else
            {
                allNinjaSyncedDoneForArenaMain(true);
            }
        }

        public void allNinjaSyncedDoneForArenaMain(bool successed)
        {
            if (successed)
            {
                List<NinjaData> list = RemoteModel.Instance.NinjaCollection.GetReleasedNinjaDatas(true);
                cacheModel.CandidateNinjasForPlayer = list;
                cacheModel.CandidateNinjasForPlayer.Sort(new NinjaDataComparer());
                //cacheModel.CandidateNinjaPageCount = cacheModel.CandidateNinjasForPlayer.Count / PvPModel2.PVP_PAGE_NINJA_ITEM_NUM;

                //                if ((cacheModel.CandidateNinjasForPlayer.Count % PvPModel2.PVP_PAGE_NINJA_ITEM_NUM) != 0)
                //                {
                //                   cacheModel.CandidateNinjaPageCount++;
                //                }

                NetworkManager.Instance.Send<ZonePvp1v1GetInfoReq>((uint)ZoneCmd.ZONE_PVP_1V1_GET_INFO, new ZonePvp1v1GetInfoReq()
                    , data => _ZONE_PVP_1V1_GET_INFOForArenaMain(data), true, timeoutCallback =>
                    {
                        //_callback(false);
                        UIAPI.ShowMsgTip("获取决斗场信息超时");
                    }
                );
            }
            else
            {
                UIAPI.ShowMsgTip("获取忍者阵容超时");
            }
        }

        private void _ZONE_PVP_1V1_GET_INFOForArenaMain(object fullInfo)
        {
            ZonePvp1v1GetInfoResp tmpResponse = fullInfo as ZonePvp1v1GetInfoResp;
            RetInfo retInfo = tmpResponse.ret_info;

            if (retInfo != null && retInfo.ret_code == 0)
            {
                string strTips = "";
                if (tmpResponse.bubble_tips != null)
                {
                    strTips = System.Text.Encoding.UTF8.GetString(tmpResponse.bubble_tips);
                }

                cacheModel.fightingTips = strTips;
                KHPluginManager.Instance.SendMessage("PvpLimitShopPlugin", "GetPvpLimitPackInfo", tmpResponse.pvp_pack);
                KHPluginManager.Instance.SendMessage("PvpLimitShopPlugin", "GetPvpLimitOnlineTime", tmpResponse.task_online_time);
                if (tmpResponse.auto_adjust_team > 0)
                {
                    UIAPI.ShowMsgTip("阵容里有限时商品过期了，请及时更换阵容");
                }

                if(ArenaMainView.MultipleScretScroll)
                {
                    string tErrInf = string.Empty;
                    bool tTeamDataAssignSuccesful = true;
                    CommonTeamSet tTeamInf = tmpResponse.info.pvp_team;

                    //限免秘卷数据
                    if (tmpResponse.weekly_free_pack != null && tmpResponse.weekly_free_pack.scroll_list != null)
                    {
                        cacheModel.FreeWeekScroll = tmpResponse.weekly_free_pack.scroll_list;
                    }

                    //忍者数据
                    cacheModel.FightNinjasForPlayer.Clear();
                    if (tTeamInf.fight_ninja.Count > 0)
                    {
                        //Dictionary<int, NinjaData> tNinjaDatas = RemoteModel.Instance.NinjaCollection.PlayerNinjaDatas;
                        for (int i = 0; i < tTeamInf.fight_ninja.Count; ++i)
                        {
                            var tTeamNinjaID = tTeamInf.fight_ninja[i];
                            if (!RemoteModel.Instance.NinjaCollection.IsNinjaDatasContain((int)tTeamNinjaID))
                            {

                                if (!RemoteModel.Instance.NinjaCollection.IsNinjaDatasContain((int)tTeamNinjaID, false))
                                {
                                    tErrInf = String.Format("数据错误：忍者{0}不存在。即将退出登录。", (int)tTeamNinjaID);
                                    tTeamDataAssignSuccesful = false;
                                    break;
                                }

                                NinjaData tTeamMap2NinjaData = RemoteModel.Instance.NinjaCollection.GetNinjaData((int)tTeamNinjaID, false);
                                if (i < 3)
                                {
                                    cacheModel.FightNinjasForPlayer.Add(tTeamMap2NinjaData);
                                    cacheModel.FightNinjasFreeSignForPlayer.Add(true);
                                }
                            }
                            else
                            {
                                NinjaData tTeamMap2NinjaData = RemoteModel.Instance.NinjaCollection.GetNinjaData((int)tTeamNinjaID);
                                if (i < 3)
                                {
                                    cacheModel.FightNinjasForPlayer.Add(tTeamMap2NinjaData);
                                    cacheModel.FightNinjasFreeSignForPlayer.Add(false);
                                }
                            }
                        }
                    }

                    cacheModel.FightBeast.Clear();
                    if (tTeamInf.fight_psychic_beast.Count > 0)
                    {
                        for (int i = 0; i < tTeamInf.fight_psychic_beast.Count; ++i)
                        {
                            cacheModel.FightBeast.Add((int)tTeamInf.fight_psychic_beast[i]);
                        }
                    }

                    cacheModel.CurrentScrollList.Clear();
                    if (tTeamInf.fight_secret_scroll.Count > 0)
                    {
                        for (int i = 0; i < tTeamInf.fight_secret_scroll.Count; ++i)
                        {
                            var tId = tTeamInf.fight_secret_scroll[i];
                            var scroll = tmpResponse.info.all_ss_list.secret_scroll.Find((p) => { return p.gid.id == tId; });
                            uint tLevel = cacheModel.GetPlayerFreeScrollLevel(tId);
                            if (scroll != null)
                            {
                                tLevel = tLevel > scroll.level ? tLevel : scroll.level;
                            }
                            else
                            {
                                //限免忍者不在列表里
                                // williamtyma and fineman
                            }

                            // 限时
                            if (tmpResponse.pvp_pack != null && tmpResponse.pvp_pack.scroll_list != null)
                            {
                                for (int j = 0; j < tmpResponse.pvp_pack.scroll_list.Count; ++j)
                                {
                                    if (tmpResponse.pvp_pack.scroll_list[j].id == tId)
                                    {
                                        tLevel = tmpResponse.pvp_pack.scroll_list[j].star;
                                    }
                                }
                            }

                            cacheModel.CurrentScrollList.Add(new SecretScrollKeyVer2()
                            {
                                id = tId,
                                level = tLevel
                            });
                        }
                    }

                    if (!tTeamDataAssignSuccesful)
                    {
                        cacheModel.FightNinjasForPlayer.Clear();
                        cacheModel.FightBeast.Clear();
                        cacheModel.CurrentScrollList.Clear();

                        UIAPI.ShowMsgOK(tErrInf, "确定", EventOk);
                        return;
                    }
                }

                if (tmpResponse.info.all_ss_list != null)
                {
                    cacheModel.SecretScrollList = tmpResponse.info.all_ss_list;

                }

                if (tmpResponse.info.beasts != null)
                {
                    cacheModel.BeastList = new PsychicBeastList();
                    for (int i = 0; i < tmpResponse.info.beasts.beast_info.Count; i++)
                    {
                        PsychicBeastInfo info = tmpResponse.info.beasts.beast_info[i];
                        if (info.is_act)
                        {
                            cacheModel.BeastList.beast_info.Add(info);
                        }
                    }
                }
                cacheModel.FreeWeekNinjaList.Clear();
                if (tmpResponse.weekly_free_pack != null && tmpResponse.weekly_free_pack.ninja_list != null)
                {
                    KH.Ninja.NinjaData ninjiaData;
                    cacheModel.FreeWeekNinja = tmpResponse.weekly_free_pack.ninja_list;
                    int count = cacheModel.FreeWeekNinja.Count;
                    for (int i = 0; i < count; i++)
                    {
                        RemoteModel.Instance.NinjaCollection.TryGetNinjaData((int)cacheModel.FreeWeekNinja[i].id, out ninjiaData, false);
                        if (ninjiaData != null)
                        {
                            KH.Ninja.NinjaData ninjiaDataTmp = ninjiaData.Clone();
                            ninjiaDataTmp.starLevel = (int)cacheModel.FreeWeekNinja[i].star;
                            cacheModel.FreeWeekNinjaList.Add(ninjiaDataTmp);
                        }
                    }

                }
                cacheModel.FreeWeekPsyList.Clear();
                if (tmpResponse.weekly_free_pack != null && tmpResponse.weekly_free_pack.psychic_list != null)
                {
                    cacheModel.FreeWeekPsy = tmpResponse.weekly_free_pack.psychic_list;
                    int count = cacheModel.FreeWeekPsy.Count;
                    for (int i = 0; i < count; i++)
                    {
                        PsychicBeastInfo info = cacheModel.getFreePsyInfo((int)cacheModel.FreeWeekPsy[i].id, (int)cacheModel.FreeWeekPsy[i].star);
                        if (info != null)
                        {
                            cacheModel.FreeWeekPsyList.Add(info);
                        }
                    }

                }

                cacheModel.FreeWeekScrollList.Clear();
                if (tmpResponse.weekly_free_pack != null && tmpResponse.weekly_free_pack.scroll_list != null)
                {
                    cacheModel.FreeWeekScroll = tmpResponse.weekly_free_pack.scroll_list;
                    int count = cacheModel.FreeWeekScroll.Count;
                    for (int i = 0; i < count; i++)
                    {
                        SecretScrollInfoVer2 info = cacheModel.getFreeScrollInfo((int)cacheModel.FreeWeekScroll[i].id, (int)cacheModel.FreeWeekScroll[i].star);
                        if (info != null)
                        {
                            cacheModel.FreeWeekScrollList.Add(info);
                        }
                    }

                }


                cacheModel.DayFightCount = (int)tmpResponse.info.kill_cnt;
                cacheModel.WeekWinCount = tmpResponse.info.week_win_times;

                cacheModel.season_fight_times = (int)tmpResponse.info.game_total_cnt;
                cacheModel.season_win_times = (int)tmpResponse.info.game_win_cnt;
                cacheModel.current_season_end_time = tmpResponse.info.current_season_end_time;//(ulong)RemoteModel.Instance.CurrentTime - (ulong)UnityEngine.Random.Range(0, 10);//tmpResponse.info.current_season_end_time;
                cacheModel.next_season_start_time = tmpResponse.info.next_season_start_time;// (ulong)RemoteModel.Instance.CurrentTime + (ulong)UnityEngine.Random.Range(15, 20);//tmpResponse.info.next_season_start_time;
                if (cacheModel.current_season != tmpResponse.info.current_season)
                {
                    KHPluginManager.Instance.SendMessage(PVPSeasonUIPlugin.PluginName, "PS.ClearCacheData");
                }
                cacheModel.current_season = tmpResponse.info.current_season;

                //updateRewardState(tmpResponse.info.daily_reward, tmpResponse.info.week_reward);

                cacheModel.score = tmpResponse.info.score;
                cacheModel.oldPhaseIndex = cacheModel.phaseIndex;
                cacheModel.phaseIndex = KHDataManager.CONFIG.GetPvp1v1GradeGraphConfigIndexByScore(cacheModel.score);
                cacheModel.wins = (int)tmpResponse.info.game_win_cnt;
                cacheModel.freeWeekTips = tmpResponse.weekly_free_desc;
                ////season胜率
                int total = (int)tmpResponse.info.game_total_cnt;
                string str;
                if (total != 0)
                {
                    str = ((int)Mathf.Floor((float)cacheModel.season_win_times / total * 100 + 0.5f)).ToString() + "%";
                }
                else
                {
                    str = "0%";
                }
                cacheModel.winsRate = str;
                cacheModel.oldRank = cacheModel.rank;
                cacheModel.rank = tmpResponse.info.rank;

                //SendMessage
                KHPluginManager.Instance.SendMessage("ArenaPlugin","OpenNewArenaMainView");

                cacheModel.SetMainViewMode(PvPModel2.PvPMainViewMode.Detail);
            }
            else
            {
                ErrorCodeCenter.DefaultProcError(tmpResponse.ret_info);
            }
        }
 
        [Operation("ShowRealTimeMain")]
        public void ShowRealTimeMain(object data){
            List<NinjaData> list = RemoteModel.Instance.NinjaCollection.GetReleasedNinjaDatas(true);
            if(list == null /* || list.Count < 3*/ )
            {
				UIAPI.ShowMsgTip("需要永久拥有3名忍者才能进入");
                return;
            }
            ParentPlugin.ShowView(UIDef.PVP_REALTIME_MAIN);
        }

        [Operation("QueryPVPRealTimeInfo")]
        public void QueryPVPRealTimeInfo(object data){
            List<NinjaData> list = RemoteModel.Instance.NinjaCollection.GetReleasedNinjaDatas(true);
            if( list == null /*|| list.Count < 3*/ )
            {
				UIAPI.ShowMsgTip("需要永久拥有3名忍者才能进入");
                return;
            }
            if(cacheModel == null)
                cacheModel = ParentPlugin.Model as PVPRealTimeMainUIModel;
            if (!RemoteModel.Instance.NinjaCollection.HasAllNinjaSynced (RemoteModel.Instance)) {
                RemoteModel.Instance.NinjaCollection.GetAllActorInfo (successed=>allNinjaSyncedDone(successed, data != null ? (bool)data : true), RemoteModel.Instance);
            } else {
                allNinjaSyncedDone (true, data != null ? (bool)data : true);
            }
        }

        private void EventOk()
        {
            KHGlobalExt.LogoutGame();
        }

        public void allNinjaSyncedDone (bool successed, bool openView)
        {
            if (successed)
            {
                List<NinjaData> list = RemoteModel.Instance.NinjaCollection.GetReleasedNinjaDatas(true);
                cacheModel.CandidateNinjasForPlayer = list;
                cacheModel.CandidateNinjasForPlayer.Sort (new NinjaDataComparer ());
                //cacheModel.CandidateNinjaPageCount = cacheModel.CandidateNinjasForPlayer.Count / PvPModel2.PVP_PAGE_NINJA_ITEM_NUM;

//                if ((cacheModel.CandidateNinjasForPlayer.Count % PvPModel2.PVP_PAGE_NINJA_ITEM_NUM) != 0)
//                {
//                   cacheModel.CandidateNinjaPageCount++;
//                }

                NetworkManager.Instance.Send<ZonePvp1v1GetInfoReq>((uint)ZoneCmd.ZONE_PVP_1V1_GET_INFO, new ZonePvp1v1GetInfoReq()
                 , data=>_ZONE_PVP_1V1_GET_INFO(data, openView), true, timeoutCallback =>
                 {
                     //_callback(false);
                     UIAPI.ShowMsgTip("获取决斗场信息超时");
                 }
                );
            }
            else
            {
                UIAPI.ShowMsgTip ("获取忍者阵容超时");
            }
        }

        private void _ZONE_PVP_1V1_GET_INFO(object fullInfo, bool openView)
        {
            ZonePvp1v1GetInfoResp tmpResponse = fullInfo as ZonePvp1v1GetInfoResp;
            RetInfo retInfo = tmpResponse.ret_info;

            if (retInfo != null && retInfo.ret_code == 0)
            {
				string strTips = "";
				if(tmpResponse.bubble_tips != null)
				{
					strTips = System.Text.Encoding.UTF8.GetString(tmpResponse.bubble_tips);
				}

				cacheModel.banNinjaList.Clear();
				cacheModel.banNinjaList.AddRange(tmpResponse.info.ban_ninja_list);
				cacheModel.fightingTips = strTips;
				cacheModel.isDoubleTask = tmpResponse.daily_task_multiple;
                KHPluginManager.Instance.SendMessage("PvpLimitShopPlugin", "GetPvpLimitPackInfo", tmpResponse.pvp_pack);
                KHPluginManager.Instance.SendMessage("PvpLimitShopPlugin", "GetPvpLimitOnlineTime", tmpResponse.task_online_time);
                if (tmpResponse.auto_adjust_team > 0)
                {
                    UIAPI.ShowMsgTip("阵容里有限时商品过期了，请及时更换阵容");
                }

                if (!UIPVPRealTimeMain.MultipleScretScroll)
                {
                    //cacheModel.FightNinjasForPlayer.Clear();
                    //Pvp1v1Info pvpInfo = tmpResponse.info;
                    //if (pvpInfo != null)
                    //{
                    //    //要改
                    //    List<PvpFightNinja> fight_team = pvpInfo.fight_team;
                    //    int count = (fight_team != null ? fight_team.Count : 0);
                    //    if (count > 0)
                    //    {
                    //        Dictionary<int, NinjaData> NinjaDatas = RemoteModel.Instance.NinjaCollection.PlayerNinjaDatas;

                    //        for (int i = 0; i < count; i++)
                    //        {

                    //            PvpFightNinja fNinja = fight_team[i];

                    //            if (!NinjaDatas.ContainsKey((int)fNinja.ninja_id))
                    //            {
                    //                UIAPI.ShowMsgOK(String.Format("数据错误：忍者{0}不存在。即将退出登录。", (int)fNinja.ninja_id), "确定", EventOk);
                    //                return;
                    //            }

                    //            NinjaData ninja = NinjaDatas[(int)fNinja.ninja_id];

                    //            if (i < 3)
                    //            {
                    //                cacheModel.FightNinjasForPlayer.Add(ninja);
                    //                cacheModel.FightNinjasFreeSignForPlayer.Add(false);
                    //            }
                    //        }
                    //    }
                    //}

                    ////要改
                    //cacheModel.CurrentScroll = tmpResponse.info.using_gid;

                    ////要改
                    //if (tmpResponse.info.fight_beast != null)
                    //{
                    //    cacheModel.FightBeast = tmpResponse.info.fight_beast;
                    //}
                }
                else
                {
                    string tErrInf = string.Empty;
                    bool tTeamDataAssignSuccesful = true;
                    CommonTeamSet tTeamInf = tmpResponse.info.pvp_team;

					//限免秘卷数据
					if(tmpResponse.weekly_free_pack != null && tmpResponse.weekly_free_pack.scroll_list != null)
					{
						cacheModel.FreeWeekScroll = tmpResponse.weekly_free_pack.scroll_list;
					}

                    //忍者数据
                    cacheModel.FightNinjasForPlayer.Clear();
                    if (tTeamInf.fight_ninja.Count > 0)
                    {
                        //Dictionary<int, NinjaData> tNinjaDatas = RemoteModel.Instance.NinjaCollection.PlayerNinjaDatas;
                        for (int i = 0; i < tTeamInf.fight_ninja.Count; ++i)
                        {
                            var tTeamNinjaID = tTeamInf.fight_ninja[i];
                            if (!RemoteModel.Instance.NinjaCollection.IsNinjaDatasContain((int)tTeamNinjaID))
                            {

                                if (!RemoteModel.Instance.NinjaCollection.IsNinjaDatasContain((int)tTeamNinjaID, false))
                                {
                                    tErrInf = String.Format("数据错误：忍者{0}不存在。即将退出登录。", (int)tTeamNinjaID);
                                    tTeamDataAssignSuccesful = false;
                                    break;
                                }

                                NinjaData tTeamMap2NinjaData = RemoteModel.Instance.NinjaCollection.GetNinjaData((int)tTeamNinjaID, false);
                                if (i < 3)
                                {
                                    cacheModel.FightNinjasForPlayer.Add(tTeamMap2NinjaData);
                                    cacheModel.FightNinjasFreeSignForPlayer.Add(true);
                                }
                            }
                            else
                            {
                                NinjaData tTeamMap2NinjaData = RemoteModel.Instance.NinjaCollection.GetNinjaData((int)tTeamNinjaID);
                                if (i < 3)
                                {
                                    cacheModel.FightNinjasForPlayer.Add(tTeamMap2NinjaData);
                                    cacheModel.FightNinjasFreeSignForPlayer.Add(false);
                                }
                            }
                        }
                    }

                    cacheModel.FightBeast.Clear();
                    if (tTeamInf.fight_psychic_beast.Count > 0)
                    {
                        for (int i = 0; i < tTeamInf.fight_psychic_beast.Count; ++i)
                        {
                            cacheModel.FightBeast.Add((int)tTeamInf.fight_psychic_beast[i]);
                        }
                    }
	
                    cacheModel.CurrentScrollList.Clear();
                    if (tTeamInf.fight_secret_scroll.Count > 0)
                    {
                        for (int i = 0; i < tTeamInf.fight_secret_scroll.Count; ++i)
                        {
                            var tId = tTeamInf.fight_secret_scroll[i];
                            var scroll = tmpResponse.info.all_ss_list.secret_scroll.Find((p) => { return p.gid.id == tId; });
                            uint tLevel = cacheModel.GetPlayerFreeScrollLevel(tId);
                            if (scroll != null)
                            {
								tLevel = tLevel > scroll.level ? tLevel : scroll.level; 
                            }
                            else
                            {
                                //限免忍者不在列表里
                                // williamtyma and fineman
                            }

                            // 限时
							if (tmpResponse.pvp_pack != null && tmpResponse.pvp_pack.scroll_list != null)
							{
								for (int j = 0; j < tmpResponse.pvp_pack.scroll_list.Count; ++j)
								{
									if (tmpResponse.pvp_pack.scroll_list[j].id == tId)
									{
										tLevel = tmpResponse.pvp_pack.scroll_list[j].star;
									}
								}
							}
                            
                            cacheModel.CurrentScrollList.Add(new SecretScrollKeyVer2()
                            {
                                id = tId,
                                level = tLevel
                            });
                        }
                    }

                    if (!tTeamDataAssignSuccesful)
                    {
                        cacheModel.FightNinjasForPlayer.Clear();
                        cacheModel.FightBeast.Clear();
                        cacheModel.CurrentScrollList.Clear();

                        UIAPI.ShowMsgOK(tErrInf, "确定", EventOk);
                        return;
                    }
                }

                if (tmpResponse.info.all_ss_list != null)
                {
                    cacheModel.SecretScrollList = tmpResponse.info.all_ss_list;

                }

                if (tmpResponse.info.beasts != null)
                {
                    cacheModel.BeastList = new PsychicBeastList();
                    for (int i = 0; i < tmpResponse.info.beasts.beast_info.Count; i++)
                    {
                        PsychicBeastInfo info = tmpResponse.info.beasts.beast_info[i];
                        if (info.is_act)
                        {
                            cacheModel.BeastList.beast_info.Add(info);
                        }
                    }
                }
				cacheModel.FreeWeekNinjaList.Clear();
				if(tmpResponse.weekly_free_pack != null && tmpResponse.weekly_free_pack.ninja_list != null)
				{
					KH.Ninja.NinjaData ninjiaData;
					cacheModel.FreeWeekNinja = tmpResponse.weekly_free_pack.ninja_list;
					int count = cacheModel.FreeWeekNinja.Count;
					for(int i = 0; i<count; i++)
					{
                        RemoteModel.Instance.NinjaCollection.TryGetNinjaData((int)cacheModel.FreeWeekNinja[i].id, out ninjiaData, false);
						if(ninjiaData != null)
						{
							KH.Ninja.NinjaData ninjiaDataTmp = ninjiaData.Clone();
							ninjiaDataTmp.starLevel = (int)cacheModel.FreeWeekNinja[i].star;
							cacheModel.FreeWeekNinjaList.Add(ninjiaDataTmp);
						}
					}

				}
				cacheModel.FreeWeekPsyList.Clear();
				if(tmpResponse.weekly_free_pack != null && tmpResponse.weekly_free_pack.psychic_list != null)
				{
					cacheModel.FreeWeekPsy = tmpResponse.weekly_free_pack.psychic_list;
					int count = cacheModel.FreeWeekPsy.Count;
					for(int i=0;i<count;i++)
					{
						PsychicBeastInfo info = cacheModel.getFreePsyInfo((int)cacheModel.FreeWeekPsy[i].id,(int)cacheModel.FreeWeekPsy[i].star);
						if (info != null)
						{
							cacheModel.FreeWeekPsyList.Add(info);
						}
					}

				}

				cacheModel.FreeWeekScrollList.Clear();
				if(tmpResponse.weekly_free_pack != null && tmpResponse.weekly_free_pack.scroll_list != null)
				{
					cacheModel.FreeWeekScroll = tmpResponse.weekly_free_pack.scroll_list;
					int count = cacheModel.FreeWeekScroll.Count;
					for(int i =0;i<count;i++)
					{
						SecretScrollInfoVer2 info = cacheModel.getFreeScrollInfo((int)cacheModel.FreeWeekScroll[i].id,(int)cacheModel.FreeWeekScroll[i].star);
						if(info != null)
						{
							cacheModel.FreeWeekScrollList.Add(info);
						}
					}
					
				}
				
				
				cacheModel.DayFightCount = (int)tmpResponse.info.kill_cnt;
                cacheModel.WeekWinCount = tmpResponse.info.week_win_times;

                cacheModel.season_fight_times = (int)tmpResponse.info.game_total_cnt;
                cacheModel.season_win_times = (int)tmpResponse.info.game_win_cnt;
                cacheModel.current_season_end_time = tmpResponse.info.current_season_end_time;//(ulong)RemoteModel.Instance.CurrentTime - (ulong)UnityEngine.Random.Range(0, 10);//tmpResponse.info.current_season_end_time;
                cacheModel.next_season_start_time = tmpResponse.info.next_season_start_time;// (ulong)RemoteModel.Instance.CurrentTime + (ulong)UnityEngine.Random.Range(15, 20);//tmpResponse.info.next_season_start_time;
                if (cacheModel.current_season != tmpResponse.info.current_season)
                {
                    KHPluginManager.Instance.SendMessage(PVPSeasonUIPlugin.PluginName, "PS.ClearCacheData");	
                }
                cacheModel.current_season = tmpResponse.info.current_season;

                //updateRewardState(tmpResponse.info.daily_reward, tmpResponse.info.week_reward);

                cacheModel.score = tmpResponse.info.score;
                cacheModel.oldPhaseIndex = cacheModel.phaseIndex;
                cacheModel.phaseIndex = KHDataManager.CONFIG.GetPvp1v1GradeGraphConfigIndexByScore(cacheModel.score);
                cacheModel.wins = (int)tmpResponse.info.game_win_cnt;
				cacheModel.freeWeekTips = tmpResponse.weekly_free_desc;
                ////season胜率
                int total = (int)tmpResponse.info.game_total_cnt;
                string str;
                if (total != 0) {
                    str = ((int)Mathf.Floor((float)cacheModel.season_win_times / total * 100 + 0.5f)).ToString() + "%";
                } else {
                    str = "0%";
                }
                cacheModel.winsRate = str;
                cacheModel.oldRank = cacheModel.rank;
                cacheModel.rank = tmpResponse.info.rank;
                if (openView && KHUIManager.getInstance().IsWindowVisible(UIDef.PVP_REALTIME_MAIN))
                {
                    ////已经打开了界面, 则只刷新UI
                    //RefreshUI
                    KHUIManager.getInstance().SendMessage(UIDef.PVP_REALTIME_MAIN, "RefreshUI");
                }
                else
                {
                    if(openView)
                        ShowRealTimeMain(null);
                }
                cacheModel.SetMainViewMode (PvPModel2.PvPMainViewMode.Detail);
            }
            else
            {
                ErrorCodeCenter.DefaultProcError (tmpResponse.ret_info);
            }
        }
        
        private void updateRewardState(List<uint> dayStates, List<uint> weekStates)
        {
            List<Pvp1v1Reward> list = KHDataManager.CONFIG.Pvp1v1DayRewardCfg;
            cacheModel.DayRewardState.Clear ();
            cacheModel.WeekRewardState.Clear ();
            bool flag = false;
            for (int i = 0; i<list.Count; i++) {
                //未领取，还要检测次数
                if(dayStates[i] == 0){
                    Pvp1v1Reward rewardConfig = list[i];
                    if(cacheModel.DayFightCount<rewardConfig.fightTimes){
                        //未达到次数
                        cacheModel.DayRewardState.Add(RewardStateType.CANT_GET);
                        if(!flag){
                            flag = true;
                            cacheModel.NextDayRewardCount =  rewardConfig.fightTimes - cacheModel.DayFightCount;
                        }
                    }else{
                        //可领取
                        cacheModel.DayRewardState.Add(RewardStateType.CAN_GET);
                        flag = true;
                    }
                }else{
                    //已领取
                    cacheModel.DayRewardState.Add(RewardStateType.HAS_GOT);
                }
            }

            list = KHDataManager.CONFIG.Pvp1v1WeekRewardCfg;
            flag = false;
            for (int i = 0; i<list.Count; i++) {
                //未领取，还要检测次数
                if(weekStates[i] == 0){
                    Pvp1v1Reward rewardConfig = list[i];
                    if(cacheModel.WeekWinCount < rewardConfig.fightTimes){
                        //未达到次数
                        cacheModel.WeekRewardState.Add(RewardStateType.CANT_GET);
                        if(!flag){
                            flag = true;
                            cacheModel.NextWeekRewardCount =  rewardConfig.fightTimes - cacheModel.WeekWinCount;
                        }
                    }else{
                        //可领取
                        cacheModel.WeekRewardState.Add(RewardStateType.CAN_GET);
                        flag = true;
                    }
                }else{
                    //已领取
                    cacheModel.WeekRewardState.Add(RewardStateType.HAS_GOT);
                }
            }
        }

        [Operation("ZonePvp1V1SetTeamReq")]
        public void ZonePvpSetTeamReq(object data)
        {
            ZonePvp1v1SetTeamReq req = data as ZonePvp1v1SetTeamReq;
            
            NetworkManager.Instance.Send((uint)ZoneCmd.ZONE_PVP_1V1_SET_TEAM, req,(object fullInfo) =>{
                ZonePvp1v1SetTeamResp resp = fullInfo as ZonePvp1v1SetTeamResp;
                if (resp.ret_info.ret_code == 0) {
                    cacheModel.SetMainViewMode(PvPModel2.PvPMainViewMode.Detail);
                }else{
                    ErrorCodeCenter.DefaultProcError(resp.ret_info);
                }
            }, true, onTimeout=>{
                UIAPI.ShowMsgTip("设置决斗阵容超时");
            });
        }

        public class RankArgument
        {
            public int Type;
            public Action Action;
        }
        int RankPageRecordCount = 20;
        public int lastRequiredType = -1;
        [Operation("SendRankReq")]
        public void SendRankReq(object data)
        {
            var argument = (RankArgument) data;

            ZonePvp1v1GetRankReq req = new ZonePvp1v1GetRankReq ();
            req.begin_rank = 1;
            req.count = RankPageRecordCount;
            req.rank_type = argument.Type;
            lastRequiredType = argument.Type;
            //UnityEngine.Debuger.LogWarning("REQ TYPE = "+lastRequiredType);
            cacheModel.RankList.Clear ();
            NetworkManager.Instance.Send ((uint)ZoneCmd.ZONE_PVP_1V1_GET_RANK, req, (fullInfo)=>{
                ZonePvp1v1GetRankResp resp = fullInfo as ZonePvp1v1GetRankResp;
                if (resp.ret_info.ret_code == 0) {
                    if (resp.rank_type != lastRequiredType)
                    {
                        //UnityEngine.Debuger.LogWarning(string.Format("RESP SKIPPED. RESP TYPE = {0}, WANNA TYPE = {1}", resp.rank_type, lastRequiredType));
                        return;
                    }
                    //UnityEngine.Debuger.LogWarning(string.Format("RANK LIST RSP UPDATE. TYPE = {0}, LENGTH = {1}", resp.rank_type, resp.rank_info != null ? resp.rank_info.Count : 0));

                    cacheModel.currentRankList = resp.rank_type;
                    if (resp.rank_info != null){
                        cacheModel.RankList.AddRange (resp.rank_info);
                        if(resp.rank_info.Count == RankPageRecordCount)
                            KHGlobalExt.globalCC.StartCoroutine(getSecondRankPage(true, argument.Type));
                    }
                    //ParentPlugin.ShowView(UIDef.PVP_REALTIME_RANK, false, null);
                    argument.Action();
                }else{
                    ErrorCodeCenter.DefaultProcError(resp.ret_info);
                }
            }, true, onTimeout => {
                UIAPI.ShowMsgTip("排行榜获取超时");
            },
            tag:lastRequiredType.ToString());
        }

        private void OnRankResp(object fullInfo, int type){
            if (!cacheModel.RankWindowIsOpen)
                return;
            ZonePvp1v1GetRankResp resp = fullInfo as ZonePvp1v1GetRankResp;
            if (resp.ret_info.ret_code == 0)
            {
                if (resp.rank_type != lastRequiredType)
                {
                    //UnityEngine.Debuger.LogWarning(string.Format("RESP SKIPPED. RESP TYPE = {0}, WANNA TYPE = {1}", resp.rank_type, lastRequiredType));
                    return;
                }
                //UnityEngine.Debuger.LogWarning(string.Format("RANK LIST RSP UPDATE. TYPE = {0}, LENGTH = {1}", resp.rank_type, resp.rank_info != null ? resp.rank_info.Count : 0));

                if (resp.rank_info!= null && resp.rank_info.Count > 0){
                    cacheModel.RankList.AddRange (resp.rank_info);
                    cacheModel.TriggerBinding("RankCount");
                    if(resp.rank_info.Count >= RankPageRecordCount && cacheModel.RankList.Count < 100){
                        KHGlobalExt.globalCC.StartCoroutine(getSecondRankPage(false, type));
                    }
                }
            }else{
                ErrorCodeCenter.DefaultProcError(resp.ret_info);
            }
        }

        IEnumerator getSecondRankPage(bool delay, int type)
        {
            if (delay) {
                yield return new WaitForSeconds (1f);
            } else {
                yield return null;
                yield return null;
                yield return null;
                yield return null;
            }
            if (type == lastRequiredType)
            {
                ZonePvp1v1GetRankReq req = new ZonePvp1v1GetRankReq();
                req.begin_rank = (uint)cacheModel.RankList.Count + 1;
                req.count = RankPageRecordCount;
                req.rank_type = type;
                //UnityEngine.Debuger.LogWarning("REQ TYPE = " + type);
                NetworkManager.Instance.Send((uint)ZoneCmd.ZONE_PVP_1V1_GET_RANK, req, (data) => OnRankResp(data, type), false, onTimeout =>
                {
                    UIAPI.ShowMsgTip("排行榜获取超时");
                },
                tag: type.ToString());
            }
        }

//        [Operation("SendMatchEnemyReq")]
//        public void SendMatchEnemyReq(object data){
//            PVPRealTimeMainUIModel.MatchType = PVPMatchType.Enemy;
//            NetworkManager.Instance.Send ((uint)ZoneCmd.ZONE_PVP_1V1_MATCH, new ZonePvp1v1MatchReq(), (object fullInfo) => {
//                ZonePvp1v1MatchResp resp = fullInfo as ZonePvp1v1MatchResp;
//                if (resp.ret_info.ret_code == 0) {
//                    MatchEnemyData tmpData = new MatchEnemyData();
//                    tmpData.expectSeconds = (int)resp.expect_wait_second;
//                    ParentPlugin.ShowView(UIDef.PVP_REALTIME_MATCH_ENEMY, false, tmpData);
//                }else{
//                    ErrorCodeCenter.DefaultProcError(resp.ret_info);
//                }
//            },true, onTimeout => {
//                UIAPI.ShowMsgTip("匹配请求超时");
//            });
//        }

        [Operation("SendMatchEnemyReq")]
         public void SendMatchEnemyReq(object data){
            PVPRealTimeMainUIPlugin plugin = KHPluginManager.Instance.GetPluginByName (PVPRealTimeMainUIPlugin.PluginName) as PVPRealTimeMainUIPlugin;
            MatchEnemyData tmpData = new MatchEnemyData();
            tmpData.expectSeconds = 60;
			tmpData.matchType = PvpMatchType.Entertainment;
            plugin.ShowView (UIDef.PVP_REALTIME_MATCH_ENEMY, false, tmpData);
        }

        [Operation("SendCancelMatchEnemyReq")]
        public void SendCancelMatchEnemyReq(object data){
            NetworkManager.Instance.Send ((uint)ZoneCmd.ZONE_PVP_1V1_CANCEL_MATCH, new ZonePvp1v1CancelMatchReq (), (object fullInfo) => {
                ZonePvp1v1CancelMatchResp resp = fullInfo as ZonePvp1v1CancelMatchResp;
                if (resp.ret_info.ret_code == 0) {
                    //UIAPI.ShowMsgTip ("匹配已取消");
                    cacheModel.TriggerBinding("CloseMatchWindow");
                    LoadingTipStack.Hide(true);
                } else {
                    ErrorCodeCenter.DefaultProcError(resp.ret_info);
                    DelayedCloseWindow();
                }
            }, false, onTimeout => {
                UIAPI.ShowMsgTip("取消匹配请求超时");
                DelayedCloseWindow();
            });
        }

        [Operation("SendFightReportReq")]
        public void SendFightReportReq(object data){
            //ParentPlugin.ShowView(UIDef.PVP_REALTIME_FIGHT_RECORD, false, null);
            NetworkManager.Instance.Send ((uint)ZoneCmd.ZONE_PVP_1V1_REPORT_LIST, new ZonePvp1v1ReportListReq (), (object fullInfo) => {
                ZonePvp1v1ReportListResp resp = fullInfo as ZonePvp1v1ReportListResp;
                if(resp.ret_info.ret_code == 0){
                    ParentPlugin.ShowView(UIDef.PVP_REALTIME_FIGHT_RECORD, false, resp);
                }else{
                    //UIAPI.ShowMsgTip ("战报获取失败:" +resp.ret_info.ret_code);
					ErrorCodeCenter.DefaultProcError(resp.ret_info);
                }
            } ,true, onTimeout => {
                UIAPI.ShowMsgTip("战报获取超时");
            });
        }

        [Operation("GetReward")]
        public void GetReward(object data){
            ZonePvp1v1GetRewardReq req = data as ZonePvp1v1GetRewardReq;
            NetworkManager.Instance.Send ((uint)ZoneCmd.ZONE_PVP_1V1_GET_REWARD, req, (object fullInfo) => {
                ZonePvp1v1GetRewardResp resp = fullInfo as ZonePvp1v1GetRewardResp;
                if(resp.ret_info.ret_code == 0){
                    //updateRewardState(resp.info.daily_reward, resp.info.week_reward);
                    List<ItemMsg> items = resp.items;
                    UICommonRewardView.Show(items, false);
                    cacheModel.TriggerBinding("DayFightCount");
                }else{
                    ErrorCodeCenter.DefaultProcError(resp.ret_info);
                }
            },true, onTimeout => {
                UIAPI.ShowMsgTip("领取奖励超时");
            });
        }

        [Operation("ShowScoreLevelList")]
        public void ShowScoreLevelList(object data){
            ParentPlugin.ShowView(UIDef.PVP_REALTIME_SCORE_LEVEL_LIST, false, data);
        }

        //        [Operation("InviteFriend")]
        //        public void InviteFriend(object data){
        //
        //            var inviteWindow = KHUIManager.Instance.FindWindow<UIPVPRealTimeMatchEnemy>(UIDef.PVP_REALTIME_MATCH_ENEMY);
        //            if (inviteWindow != null && inviteWindow.gameObject.activeSelf){
        //                return;
        //            }
        //            Pvp1v1Friend friend = data as Pvp1v1Friend;
        //            var req = new ZonePvp1v1InviteReq ();
        //            req.friend_gid = friend.player_id;
        //            req.friend_zone = friend.zoneid;
        //            req.invite = true;
        //            var friendModel = KHPluginManager.Instance.GetModel(PVPRealTimeFriendPlugin.PluginName) as PVPRealTimeFriendModel;
        //            if (friendModel.platformFriendList.Contains (friend)) {
        //                req.plat_friend = true;
        //            } else {
        //                req.plat_friend = false;
        //            }
        //            NetworkManager.Instance.Send<ZonePvp1v1InviteReq>((uint)ZoneCmd.ZONE_PVP_1V1_INVITE, req, (object fullInfo) =>
        //                                                              {
        //                ZonePvp1v1InviteResp resp = fullInfo as ZonePvp1v1InviteResp;
        //                PVPRealTimeMainUIPlugin plugin = KHPluginManager.Instance.GetPluginByName (PVPRealTimeMainUIPlugin.PluginName) as PVPRealTimeMainUIPlugin;
        //                if (resp.ret_info.ret_code == 0) {
        //                    plugin.ShowView(UIDef.PVP_REALTIME_MATCH_ENEMY, false, friend);
        //                } else {
        //                    ErrorCodeCenter.DefaultProcError(resp.ret_info);
        //                }
        //                if(resp.refresh){
        //                    var model = plugin.Model as PVPRealTimeMainUIModel;
        //                    model.TriggerBinding("RefreshFriendList");
        //                }
        //            }, true,  timeoutCallback => {
        //                UIAPI.ShowMsgTip("邀请好友比试超时");
        //            });
        //        }

       // start 用于活动主播接受队列某个玩家的挑战
        [Operation("ActInviteFriend")]
        public void ActInviteFriend(object data)
        {
            PVPRealTimeFriendModel friendModel = KHPluginManager.Instance.GetModel(PVPRealTimeFriendPlugin.PluginName) as PVPRealTimeFriendModel;

            PVPRealTimeMainUIPlugin plugin = KHPluginManager.Instance.GetPluginByName(PVPRealTimeMainUIPlugin.PluginName) as PVPRealTimeMainUIPlugin;
            MatchPvPAct friend = data as MatchPvPAct;
            plugin.ShowView(UIDef.PVP_REALTIME_MATCH_ENEMY, false, friend);
        }

        [Operation("ActCancelInviteFriend")]
        public void ActCancelInviteFriend(object data)
        {

            if (cacheModel == null)
                cacheModel = ParentPlugin.Model as PVPRealTimeMainUIModel;

            BriefRoleInfo friend = data as BriefRoleInfo;
            var req = new ZonePvp1v1InviteReq();
            req.friend_gid = friend.player_id;
            req.friend_zone = friend.zoneid;
            req.invite = false;
            NetworkManager.Instance.Send<ZonePvp1v1InviteReq>((uint)ZoneCmd.ZONE_PVP_1V1_INVITE, req, (object fullInfo) =>
            {
                ZonePvp1v1InviteResp resp = fullInfo as ZonePvp1v1InviteResp;
                if (resp.ret_info.ret_code == 0)
                {
                }
                else
                {
                    ErrorCodeCenter.DefaultProcError(resp.ret_info);
                }
            }, false, timeoutCallback => {
                UIAPI.ShowMsgTip("取消好友比试超时");
            });
        }

        [Operation("ActCloseMatchWin")]
        public void ActCloseMatchWin(object data)
        {
            if (cacheModel == null)
                cacheModel = ParentPlugin.Model as PVPRealTimeMainUIModel;

            cacheModel.TriggerBinding("SetRemainTimesToZero");
            
            KHGlobalExt.StartCoroutine(TimeoutCloseWinTask());
        }


        private IEnumerator TimeoutCloseWinTask()
        {
            yield return new WaitForSeconds(5);
            cacheModel.TriggerBinding("CloseMatchWindow");
        }

        // end 用于活动主播接受队列某个玩家的挑战

        [Operation("InviteFriend")]
        public void InviteFriend(object data){
            PVPRealTimeFriendModel friendModel = KHPluginManager.Instance.GetModel(PVPRealTimeFriendPlugin.PluginName) as PVPRealTimeFriendModel;

            PVPRealTimeMainUIPlugin plugin = KHPluginManager.Instance.GetPluginByName(PVPRealTimeMainUIPlugin.PluginName) as PVPRealTimeMainUIPlugin;
            BriefRoleInfo friend = data as BriefRoleInfo;
            plugin.ShowView(UIDef.PVP_REALTIME_MATCH_ENEMY, false, friend);
        }

        
        [Operation("CancelInviteFriend")]
        public void CancelInviteFriend(object data){

            if (cacheModel == null)
                cacheModel = ParentPlugin.Model as PVPRealTimeMainUIModel;

            BriefRoleInfo friend = data as BriefRoleInfo;
            var req = new ZonePvp1v1InviteReq ();
            req.friend_gid = friend.player_id;
            req.friend_zone = friend.zoneid;
            req.invite = false;
            NetworkManager.Instance.Send<ZonePvp1v1InviteReq>((uint)ZoneCmd.ZONE_PVP_1V1_INVITE, req, (object fullInfo) =>
                                                              {
                ZonePvp1v1InviteResp resp = fullInfo as ZonePvp1v1InviteResp;
                if (resp.ret_info.ret_code == 0) {
                    //UIAPI.ShowMsgTip("取消好友比试成功");
                    LoadingTipStack.Hide(true);
                    cacheModel.TriggerBinding("CloseMatchWindow");
                } else {
                    ErrorCodeCenter.DefaultProcError(resp.ret_info);
                    DelayedCloseWindow();
                }
            }, false,  timeoutCallback => {
                UIAPI.ShowMsgTip("取消好友比试超时");
                DelayedCloseWindow();
            });
        }

        private void DelayedCloseWindow(){
            KHGlobalExt.StartCoroutine(TimeoutTask());
        }

        private IEnumerator TimeoutTask(){
            yield return new WaitForSeconds (5);
            LoadingTipStack.Hide(true);
            cacheModel.TriggerBinding ("CloseMatchWindow");
        }

        [Operation("ListenEnterPvP")]
        public void ListenEnterPvP(object data)
        {
            NetworkManager.Instance.AddMessageCallback((int)ZoneCmd.ZONE_PVP_1V1_ENTER_GAME_NTF, MatchSuccNtf);
        }

        [Operation("UnListenEnterPvP")]
        public void UnListenEnterPvP(object data)
        {
            NetworkManager.Instance.RemoveMessageCallback((int)ZoneCmd.ZONE_PVP_1V1_ENTER_GAME_NTF, MatchSuccNtf);
        }

        private void MatchSuccNtf(object message)
        {
            PVPRealTimeMainUIModel model = ParentPlugin.Model as PVPRealTimeMainUIModel;
            ZonePvp1v1EnterGameNtf ntf = message as ZonePvp1v1EnterGameNtf;

            NetworkManager.Instance.RemoveMessageCallback((int)ZoneCmd.ZONE_PVP_1V1_ENTER_GAME_NTF, MatchSuccNtf);

            PVPEnterParam param = new PVPEnterParam(ntf.ret_info,
                                        ntf.team_info_list,
                                        ntf.setting,
                                        0,
                                        null,
                                        "Callback",
                                        "",
                                        PVPRTPlayMode.Normal,
                                        "",
                                        "")
                                        {
                                            useIOSReplayKit = model.IOS9ReplayOpen,
                                            useGuide = ntf.is_rookie == 1,
                                        };

            PVPRealTimeMainUIOperation.EnterRealTimePVPByParam(param);
        }

		/* EnterRealTimePvpForLua EnterRealTimePvp提供给lua的接口需要传入ntf，pvpType（设置为2），ui name 设置  */
        public static void EnterRealTimePVPForLua(object message, int pvpType, int playMode, KHVoidFunction endFunction, string loadingUIName, string resultUIName, string subPluginName = "")
		{
			PVPRealTimeMainUIModel model = KHPluginManager.Instance.GetPluginByName("PVPRealTimeMainUIPlugin").Model as PVPRealTimeMainUIModel;
			ZonePvp1v1EnterGameNtf ntf = message as ZonePvp1v1EnterGameNtf;

            PVPRTPlayMode _playMode = PVPRTPlayMode.Normal;
		    if (Enum.IsDefined(typeof (PVPRTPlayMode), playMode))
		    {
		        _playMode = (PVPRTPlayMode) Enum.ToObject(typeof (PVPRTPlayMode), playMode);
		    }
		    else
		    {
                Debuger.LogError(LOG_TAG, "EnterRealTimePVPForLua() playMode={0} 未定义！", playMode);
                UIAPI.ShowMsgTip("战斗的播放模式不存在, PlayMode = " + playMode);
                return;
		    }

            PVPEnterParam param = new PVPEnterParam(ntf.ret_info,
                                        ntf.team_info_list,
                                        ntf.setting,
                                        pvpType,
                                        endFunction,
                                        "Callback",
                                        "",
                                        _playMode,
                                        loadingUIName,
                                        resultUIName)
                                        {
                                            useIOSReplayKit = model.IOS9ReplayOpen,
                                            useGuide = ntf.is_rookie == 1,
                                            playMode = _playMode
                                        };

            PVPRealTimeMainUIOperation.EnterRealTimePVPByParam(param);
		}

        /// <summary>
        /// ObEnterGameRsp 进入战斗(OBStream观战模式)
        /// </summary>
        public static void EnterRealTimePVPForOBStream(object message,
                                                ulong obGid,
                                                int pvpType,
                                                PVPRTPlayMode playMode,
                                                KHVoidFunction endFunction,
                                                string loadingUIName,
                                                string resultUIName)
        {
            ObEnterGameRsp resp = message as ObEnterGameRsp;

            if (resp == null) return;

            if (resp.ret_info != null && resp.ret_info.ret_code == 0)
            {
                PVPEnterParam param = new PVPEnterParam(resp.ret_info,
                                        resp.enter.team_info_list,
                                        resp.enter.setting,
                                        pvpType,
                                        endFunction,
                                        "Callback",
                                        "",
                                        playMode,
                                        loadingUIName,
                                        resultUIName)
                                        {
                                            useGuide = resp.enter.is_rookie == 1,
                                            _OBStreamGid = obGid,
                                            _obStreamDatae = resp
                                        };

                PVPRealTimeMainUIOperation.EnterRealTimePVPByParam(param);
            }
            else
            {
                ErrorCodeCenter.DefaultProcError(resp.ret_info);
            }
        }

        

		/* EnterRealTimePvpForLocal 局域网 EnterRealTimePvp提供给 局域网 的接口需要传入ntf，pvpType（设置为2），ui name 设置  */
		public static void EnterRealTimePVPForLocal(object message,int pvpType, int playMode, KHVoidFunction endFunction, string loadingUIName, string resultUIName)
		{
			PVPRealTimeMainUIModel model = KHPluginManager.Instance.GetPluginByName("PVPRealTimeMainUIPlugin").Model as PVPRealTimeMainUIModel;
			ZonePvp1v1EnterGameNtf ntf = message as ZonePvp1v1EnterGameNtf;
			
			PVPRTPlayMode _playMode = PVPRTPlayMode.Normal;
			if (Enum.IsDefined(typeof (PVPRTPlayMode), playMode))
			{
				_playMode = (PVPRTPlayMode) Enum.ToObject(typeof (PVPRTPlayMode), playMode);
			}
			else
			{
				Debuger.LogError(LOG_TAG, "EnterRealTimePVPForLua() playMode={0} 未定义！", playMode);
				UIAPI.ShowMsgTip("战斗的播放模式不存在, PlayMode = " + playMode);
				return;
			}

            PVPEnterParam param = new PVPEnterParam(ntf.ret_info,
                                                    ntf.team_info_list,
                                                    ntf.setting,
                                                    pvpType,
                                                    endFunction,
                                                    "Callback",
                                                    "",
                                                    _playMode,
                                                    loadingUIName,
                                                    resultUIName)
                                                    {
                                                        useIOSReplayKit = model.IOS9ReplayOpen,
                                                        useGuide = ntf.is_rookie == 1
                                                    };

			param.isLanPVP = true;
            PVPRealTimeMainUIOperation.EnterRealTimePVPByParam(param);
		}

        ///=============================================================================================
        ////////////////////////////////////////////////////////////////////////////////////////////////
        ///=============================================================================================
        /// <summary>
        /// 以后请各位用传PVPEnterParam的方式进入战斗
        /// </summary>
        /// <param name="_param"></param>
        public static void EnterRealTimePVPByParam(PVPEnterParam _param)
        {
            EnterRealTimePVP(_param.ret_info,
                             _param.team_info_list,
                             _param.setting,
                             _param.useIOSReplayKit,
                             _param.pvptype,
                             _param.endFunction,
                             _param.useGuide,
                             _param.endType,
                             _param.subPluginName,
                             _param.playMode,
                             _param.loadingUIName,
                             _param.resultUIName,
                             _param.isLanPVP,
                             _param._OBStreamGid,
                             _param._obStreamDatae);
        }
        ///=============================================================================================
        ////////////////////////////////////////////////////////////////////////////////////////////////
        ///=============================================================================================


        static void EnterRealTimePVP(RetInfo ret_info
            , List<MatchTeamInfo> team_info_list
            , PvpGameSetting setting
            , bool useIOSReplayKit
            , int pvptype = 0
            , KHVoidFunction endFunction = null
            , bool useGuide = false
            , string endType = "Callback"
            , string subPluginName = ""
            , PVPRTPlayMode playMode = PVPRTPlayMode.Normal
            , string loadingUIName = ""
            , string resultUIName = ""
		    , bool isLanPVP = false
            , ulong _OBStreamGid = 0
            , ObEnterGameRsp _obStreamData = null
            , Action<PVPSyncData> reportFunction = null
            , string battleUIName = "")
        {
            // Update by chicheng
            MessageManager msgManager = MessageManager.Instance;
            //if (msgManager.IsDeserializeFromLocal)
            {
                Debug.LogWarning("从本地读取战斗包");
                //BattleFinalResultData battleFinalResult = msgManager.deserializeFromLocal<BattleFinalResultData>(MessageManager.BATTLE_RESULT);
                //if (battleFinalResult != null)
                //{
                //    Debug.Log("读取成功, myResult:" + battleFinalResult.myResult);
                //}
                //else
                //{
                //    Debug.Log("读取失败");
                //}
                Debug.LogWarning("直接endfunction");
                if (endFunction != null)
                {
                    Debug.LogWarning("EnterRealTimePVP 当前处于" + KHGlobalExt.app.CurrentContext.contextName);
                    // KHGlobalExt.app.SwitchScene(KHLevelName.GAME);
                    // endFunction();
                }
                else
                {
                    Debug.Log("endfunction 是null");
                }
            }

            if (ret_info.ret_code == 0)
            {
                Debuger.Log(LOG_TAG, "EnterRealTimePVP() PlayMode={0}, pvpType={1}", playMode, pvptype);

                //记录点赞所需的GameID、RandomSeed
                PVPRecorder.Instance.game_id = setting.game_id;
                PVPRecorder.Instance.random_seed = (uint)setting.random_seed;
                PVPRecorder.Instance.showLikeBtn = setting.can_praise;

                PVPRTParameter param = new PVPRTParameter();
                
                PVPRecorder.Instance.Reset();

                // 重置AI上报开关
                KH.NNClient.NNManager.EnableStatistics = false;

                //在这里启动录像机
                if (playMode == PVPRTPlayMode.Normal)
                {
                    //如果是普通模式，则可以进行录像

                    if (!useGuide)
                    {
                        //Stop录像是在另一个类里，与GameOver协议关联
                        ZonePvp1v1EnterGameNtf ntf = new ZonePvp1v1EnterGameNtf();
                        ntf.ret_info = ret_info;
                        ntf.setting = setting;
                        ntf.team_info_list.AddRange(team_info_list);
                        PVPRecorder.Instance.Start(ntf, PvpTypeConverter.GetRecordType(pvptype), loadingUIName, resultUIName,
                            RemoteModel.Instance.Player.Gid);
                        // 如果是普通模式, 则根据GameSetting设置AI上报参数
                        KH.NNClient.NNManager.EnableStatistics = setting.ai_report_switch;
                        KH.NNClient.NNStatistics.TrainingInterval = (int)setting.ai_report_frame_interval;
                    }
                }
                else if(playMode == PVPRTPlayMode.Playback)
                {
                    //如果是录像回放，则使用PVPRecordPlayer对应的CmmcProxyType
                    param.cmmcProxyType = PVPRTCmmcProxyType.Playback;
                    if (DefineExt.IsSlimVersion)
                    {
                        NNClient.NNManager.EnableStatistics = Slim.SlimDefine.EnablePVPRTStatistics;
                        NNClient.NNStatistics.TrainingInterval = Slim.SlimDefine.PVPRTStatisticsInterval;
                    }
                }
                else if (playMode == PVPRTPlayMode.OBStream)
                {
                    //这是多人OB的Stream播放模式
                    param.cmmcProxyType = PVPRTCmmcProxyType.OBStream;
                }
                else if (playMode == PVPRTPlayMode.Observer)
                {
                    //如果是OB模式，则
                    param.cmmcProxyType = PVPRTCmmcProxyType.Observer;

                    //在OB模式下，也可以录像
                    //Stop录像是在另一个类里，与GameOver协议关联
                    ZonePvp1v1EnterGameNtf ntf = new ZonePvp1v1EnterGameNtf();
                    ntf.ret_info = ret_info;
                    ntf.setting = setting;
                    ntf.team_info_list.AddRange(team_info_list);
                    PVPRecorder.Instance.Start(ntf, PvpTypeConverter.GetRecordType(pvptype), loadingUIName, resultUIName,
                        RemoteModel.Instance.Player.Gid);
                }

                param.isLan = isLanPVP;
                param.playMode = playMode;
                param.useCheckSum = setting.use_checksum;
                param.useGSDK = setting.use_mna_sdk;
                param.useEmptyFrameAckAvoid = setting.use_empty_frame_ack_avoid;
                param.wifiSendInterval = (int)setting.wifi_ack_interval;

                param.pvp1v1Setting = setting.pvp_1v1_gamesetting;
                param.pvpSceneID = (int)setting.map_id;
                param.roomID = (int)setting.game_id;
                param.uniqueGameID = setting.pvp_game_id;
                param.udpAddres = KHUtil.GetBytesString(setting.gamesvr_url);
                param.randomSeed = setting.random_seed;
                param.client_stat_report_switch = setting.client_stat_report_switch;
                param.client_buffering_threshold = (int)setting.client_buffering_threshold;
                param.enableLogSync = (int)setting.report_inconformity_log_level;
                param.check_sum_report_interval = setting.check_sum_report_interval;
                param.check_sum_report_rank = setting.check_sum_report_rank;

                //comment by zoomin
                param.clientFrameInterval = (int)setting.cs_frame_factor;
                param.clientFrameCache = (int)setting.client_frame_cache_size;
                param.timeoutSecond = (int)setting.round_time_second;
                param.vsCPU = ((int)setting.is_vs_cpu != 0);
                param.pvpServerType = setting.fight_type;
                param.useReplayKit = useIOSReplayKit && ReplayKitPlugin.isOpen;
                param.pvpType = pvptype;
                param.endFunction = endFunction;
                param.endFuncType = endType;
                param.reportFunction = reportFunction;
                param.useGuide = useGuide;
                param.subPluginName = subPluginName;
                param.pvpLoadingUIName = loadingUIName;
                param.pvpBattleUIName = battleUIName;
                //param.pvpLoadingUIName = "UILua/PVPRealtimeMain/Loading/PVPAssistantLoadingView";//loadingUIName;

				// add by solzhang
				param.wifi_4g_dual_channel_switch = (int)setting.wifi_4g_dual_channel_switch;

                int battleType = PVPBattleType.GetBattleType((int)param.pvpServerType);

                ////不同的战斗模式用不同的model
                param.rtModelType = PVPRTParameter.GetModelType(battleType);

                /////////////////////////////////////test begin, 需要删掉/////////////////////
                if (param.rtModelType == 4)
                {
                    param.pvpLoadingUIName = "UILua/PVPRealtimeMain/Loading/PVPAssistantLoadingView";//loadingUIName;
                }
                /////////////////////////////////////test end//////////////////////////////////

                //我无法明确知道pvptype的定义是什么
                if (playMode == PVPRTPlayMode.OBStream)
                {
                    param.pvpResultUIName = resultUIName;
                }
                else
                {
                    param.pvpResultUIName = PvpTypeConverter.GetResultUIType(pvptype) > 1 ? resultUIName : "";    
                }

                param.listenNormalGameoverNtf = !isLanPVP;

                //记录一下最近的一场决斗场地图
                LanPVPManager.Instance.SetLastMapId((uint)param.pvpSceneID);

                // 控制是否打开录像校验数据的上报开关
                if (setting.pvp_replay_setting != null)
                {
                    PVPSyncInfoManagerEx.EnableSync = setting.pvp_replay_setting.switch_dump_state_frame;
                    PVPSyncInfoManagerEx.SyncInterval = (int)setting.pvp_replay_setting.dump_state_frame_interval;
                }
                else
                {
                    PVPSyncInfoManagerEx.EnableSync = false;
                }

                Debuger.Log(LOG_TAG, "EnterRealTimePVP() udpAddres = " + param.udpAddres);

                List<PVPRTTeamData> teamDatas = new List<PVPRTTeamData>();
                param.teamDatas = teamDatas;

                //收集小队信息
                int teamCount = team_info_list.Count;
                int group1Count = 0, group2Count = 0;


                for (int i = 0; i < teamCount; i++)
                {
                    MatchTeamInfo teamInfo = team_info_list[i];


                    Debuger.Log(LOG_TAG, "EnterRealTimePVP() sid={0}, name={1} teamID={2}, groupId={3}, is_ob={4}",
                        teamInfo.sid, teamInfo.role_info.name, teamInfo.team_id, teamInfo.group_id, teamInfo.is_ob);
                    

                    //获取连接参数
                    ///获取自己的连接参数
                    ///如果是OB模式，则取OB者的连接参数
                    ulong mineGID = RemoteModel.Instance.Player.Gid;
                    if (mineGID == teamInfo.role_info.gid)
                    {
                        Debuger.Log(LOG_TAG, "EnterRealTimePVP()  MineSid={0}, MineGid={1}, Auth={2}", teamInfo.sid, mineGID, teamInfo.secret_num);
                        param.auth = (int)teamInfo.secret_num;
                        param.mineTeamID = (int)teamInfo.team_id;
                        param.mineUserID = (int)teamInfo.team_id;
                        param.mineSid = (int)teamInfo.sid;
                        param.encKey = teamInfo.enc_key;
                    }

                    //如果是播放录像，判断录像中是否有玩家自己
                    if (playMode == PVPRTPlayMode.Playback)
                    {
                        if (i == teamCount - 1)
                        {
                            //已经到了最后一个小队了
                            if (param.auth == 0)
                            {
                                //说明在所有小队中，都没有玩家自己

                                Debuger.Log(LOG_TAG, "EnterRealTimePVP() 录像中没有玩家自己，取第一个人赋给自己");
                                param.mineTeamID = (int)team_info_list[0].team_id;
                                param.mineUserID = (int)team_info_list[0].team_id;

                                //以下参数用于通讯，但其实对于录像来说，通讯并不会真正建立。
                                param.auth = (int)team_info_list[0].secret_num;
                                param.mineSid = (int)team_info_list[0].sid;
                                param.encKey = team_info_list[0].enc_key;
                            }
                        }
                    }
                    else if (playMode == PVPRTPlayMode.Observer)
                    {
                        //如果是OB者，则录像中肯定没有自己。所以，这里取第1个人赋予自己。
                        param.mineTeamID = (int)team_info_list[0].team_id;
                        param.mineUserID = (int)team_info_list[0].team_id;
                    }
                    else if (playMode == PVPRTPlayMode.OBStream)
                    {
                        Debuger.Log(LOG_TAG, "EnterRealTimePVP()   _OBStreamGid={0}, teamInfo[{1}].role_info.gid={2}",
                            _OBStreamGid, i, teamInfo.role_info.gid);

                        //如果是OB者，则录像中肯定没有自己。所以，这里取第1个人赋予自己。
                        if (_OBStreamGid == teamInfo.role_info.gid)
                        {
                            param.auth = (int)teamInfo.secret_num;
                            param.mineTeamID = (int)teamInfo.team_id;
                            param.mineUserID = (int)teamInfo.team_id;
                            param.mineSid = (int)teamInfo.sid;
                            param.encKey = teamInfo.enc_key;
                        }
                    }


                    //如果小队中有OB者，则将OB屏蔽
                    if (teamInfo.is_ob == 1)
                    {
                        Debuger.Log(LOG_TAG, "EnterRealTimePVP() 找到OB信息: Sid={0}", teamInfo.sid);
                        continue;
                    }

                    //param.mineTeamID = 2;

                    PVPRTTeamData teamData = new PVPRTTeamData();
                    int groupCount = 0;

                    teamData.sid = (int) teamInfo.sid;

                    //小队信息
                    teamData.teamID = (int)teamInfo.team_id;
                    teamData.userID = (int)teamInfo.team_id;
                    teamData.GroupId = (int)teamInfo.group_id;

                    teamData.playerControlType = (int) teamInfo.role_info.player_role;


                    //玩家信息
                    teamData.playerGid = teamInfo.role_info.gid;
                    teamData.name = teamInfo.role_info.name;
                    teamData.title = teamInfo.role_info.cur_title;
                    teamData.lvl = (int) teamInfo.role_info.level;
                    teamData.plat_pic = teamInfo.role_info.plat_pic;
                    teamData.cur_pic_frame = teamInfo.role_info.cur_pic_frame;
                    teamData.ninja_info_list = teamInfo.role_info.ninja_info_list;//忍者列表
                    teamData.score = (int)teamInfo.role_info.score;
					teamData.chaoyingRank = (int)teamInfo.role_info.chaoying_rank;
                    teamData.AI_tempID = (int)teamInfo.role_info.robot_tmpl_id;

                    ///一些显示用的信息
                    teamData.fightCapacity = (int)teamInfo.role_info.fight_capacity;
                    teamData.guildName = KHUtil.GetBytesString(teamInfo.role_info.guild_name);
                    teamData.fcUpgradeLevel = (int)teamInfo.role_info.fc_upgrade_level;
                    teamData.Morale = (int)teamInfo.role_info.offset_chakras;
                    teamData.teamName = KHUtil.GetBytesString(teamInfo.role_info.team_name);

					//大区信息
					teamData.zoneId = teamInfo.role_info.zoneid;
					teamData.zoneName = teamInfo.role_info.zone_name;

					// 如果这个玩家不是我，则记录为我的对手用于再战一次功能
					if(teamInfo.role_info.gid != RemoteModel.Instance.Player.Gid)
					{
						PvpBattleResult.FightPlayerName = teamInfo.role_info.name;
						PvpBattleResult.FightPlayerPlayerId = teamInfo.role_info.gid;
						PvpBattleResult.FightPlayerZoneid = teamInfo.role_info.zoneid;
					}

                    if (teamData.GroupId == 1)
                    {
                        teamData.Direction = Direction.RIGHT;
                        teamData.MapPosition = PVPRTTeamData.P_1_POS;
                        group1Count++;
                        groupCount = group1Count;
                    }
                    else
                    {
                        teamData.Direction = Direction.LEFT;
                        teamData.MapPosition = PVPRTTeamData.P_2_POS;
                        group2Count++;
                        groupCount = group2Count;
                    }

                    if (groupCount > 1)
                    {
                        ///向下移20个像素错开...
                        teamData.MapPosition.z = teamData.MapPosition.z + (groupCount - 1) * 0.2f;
                    }


                    //Decode忍者信息
                    if (!PVPRTTeamData.DecodeNinjaInfoList(ref teamData, teamData.ninja_info_list))
                    {
                        Debuger.LogError(LOG_TAG, "EnterRealTimePVP() 获取到的人物信息缺失！");
                        UIAPI.ShowMsgTip("获取到的人物信息缺失!!!");
                        return;
                    }

                    ////自定义忍者信息
                    teamData.custom_ninja_info = teamInfo.role_info.custom_ninja_info;

                    //////test
                    //teamData.custom_ninja_info = new CustomFightNinjaInfo();
                    //CustomNinjaFullInfoMsg infoMsg = new CustomNinjaFullInfoMsg();
                    //NinjaSkillMsg skill1 = new NinjaSkillMsg();
                    //skill1.id = 200180101;
                    //infoMsg.skills.Add(skill1);

                    //NinjaSkillMsg skill2 = new NinjaSkillMsg();
                    //skill2.id = 200190101;
                    //infoMsg.skills.Add(skill2);

                    //NinjaSkillMsg skill3 = new NinjaSkillMsg();
                    //skill3.id = 200200101;
                    //infoMsg.skills.Add(skill3);
                    //teamData.custom_ninja_info.ninjia_list.Add(infoMsg);
                    //teamData.custom_ninja_info.ninjia_list.Add(infoMsg);
                    //teamData.custom_ninja_info.ninjia_list.Add(infoMsg);

                    teamDatas.Add(teamData);
                }


#region 初始化一些战局变量(从ob数据结构中取值)
                if (_obStreamData != null)
                {
                    for (int i = 0; i < teamCount; i++)
                    {
                        PVPRTTeamData teamData = teamDatas[i];
                        if (i >= _obStreamData.round_result.results.Count)
                        {
                            Debuger.LogWarning("[OB信息ERROR] _obData.results.count = " + _obStreamData.round_result.results.Count);
                            break;
                        }
                        ObRoundEndResult.ObRoleRoundEndResult teamObdata = _obStreamData.round_result.results[i];
                        //ObRoundEndResult.ObRoleRoundEndResult teamObdata = new ObRoundEndResult.ObRoleRoundEndResult();

                        ////当前的ninjaIndex, startwith 0
                        int createIndex = (int)teamObdata.ninja;
                        teamData.orderIndex = createIndex - 1;

                        ////之前已经死亡的忍者hp赋值为0
                        int length = teamData.actorInfos.Count;
                        for (int k = 0; k <= teamData.orderIndex; ++k)
                        {
                            if (k < length)
                            {
                                teamData.actorInfos[k].hp = 0;
                            }
                        }

                        ////要创建的忍者, 把mp更新掉
                        if (createIndex < length)
                        {
                            teamData.actorInfos[createIndex].mp = (int)teamObdata.aoyi;
                            double hpRate = (double)teamObdata.hp / 10000.0f;
                            teamData.actorInfos[createIndex].hp = (int)(teamData.actorInfos[createIndex].maxhp * hpRate);
                        }

                        ////通灵兽技能索引
                        teamData.PsychicIndex = (int)teamObdata.psychic;
                    }

                    ////当前是第几局了, start with 0
                    if (_obStreamData.round_data != null)
                    {
                        param.roundInx = (int)_obStreamData.round_data.round_index - 2;
                    }
                    else
                    {
                        param.roundInx = -1;
                    }
                    //param.roundInx = 0;
                }
#endregion
                
                ByteStream.FreePublic();

                ////数据准备成功进入战斗..
                string curPluginName = KHBattleManager.Instance.PluginName;
                if (curPluginName == null
                  || (curPluginName != null && curPluginName != PVPRealTimePlugin.PluginName))
                {
                    KHJumpSystemHelper.DoJump(SystemConfigDef.PVP_REALTIME_Battle, param);
                }
                else
                {
                    Debuger.LogError("[ERROR!!!!!!!][重复进入决斗场战斗了!!!!!!!!!!!!!]");
                }
            }
            else
            {
                ErrorCodeCenter.DefaultProcError(ret_info);
            }
        }



    }
}
