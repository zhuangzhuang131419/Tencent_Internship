using UnityEngine;
using System.Collections;
using KH;
using naruto.protocol;
using System.Collections.Generic;
using kihan_general_table;
using KH.Ninja;
using KH.Plugins;
using GameJoyAPI;
using KH.Lua;

public class UIPVPRealTimeMainSelectNinja : MonoBehaviour
{
    /* 决斗场3.0 新增内容 */

    // 名字修改
    private static PvpFightType currentFightType;
    private static int currentSystemID = -1;

    // 不同UI界面是否带有战力
    private static Dictionary<string, bool> UIWindows = new Dictionary<string, bool>();

    /// <summary>
    /// 对外的接口
    /// </summary>
    public static void AddUIWindow(string windowTitle, bool isWithFight)
    {
        if (!UIWindows.ContainsKey(windowTitle))
        {
            UIWindows.Add(windowTitle, isWithFight);
        }
    }

    public static int CurrentSysID
    {
        get
        {
            return currentSystemID;
        }
        set
        {
            currentSystemID = value;
            Debug.LogWarning("当前systemID" + currentSystemID);
        }
    }

    public static bool IsWithFight
    {
        get
        {
            AddUIWindow(UIDef.PVP_REALTIME_MAIN, false);         // 忍术对决
            AddUIWindow("UILua/Arena/ArenaNinjaSelect", false);  // 段位赛
            // AddUIWindow(UIDef.CommonTeamSettingMainView, true); // 修行之路


            foreach (string title in UIWindows.Keys)
            {
                if (KHUIManager.Instance.IsWindowVisible(title))
                {
                    return UIWindows[title];
                }
            }

            if (currentSystemID == SystemConfigDef.Anbu_PVP_1v1 // 117 
                || currentSystemID == SystemConfigDef.Challenge // 112
                || currentSystemID == SystemConfigDef.PVP_REALTIME // 57
                || currentSystemID == SystemConfigDef.Arena_Main // 105
                || currentSystemID == SystemConfigDef.Championship // 108
                || currentSystemID == SystemConfigDef.Psychic_1 // 51
                || currentSystemID == 128
                || currentSystemID == 255
                || currentSystemID == 307
                || currentSystemID == SystemConfigDef.PractiseMode) // 101
            {
                Debug.LogWarning("带有通灵兽");
                return false;
            }


            Debug.LogWarning(currentSystemID);
            return true;
        }
    }



    // 仅查看功能

    public static bool OnlyWatch = false;
    public UIToggle ToggleOnlyWatch = null;

    public void OnClickOnlyWatch()
    {
        OnlyWatch = ToggleOnlyWatch.value;
    }

    public static int SPageNum = -1;
    public static int APageNum = -1;
    public static int BPageNum = -1;
    public static int CPageNum = -1;

    public UILabel LblNinjaLevel = null;
    public GameObject ObjNinajaLevelPopWindow = null;
    public UIScrollView ScrollView;
    public Vector3 RawPos;
    public int GridWidth;

    public LuaBehaviour PageGroup;
    public LuaBehaviourWrapper PageGroupNew;
    public UISetTeamFilterItem teamFileterComp;

    private bool isRefreshFilterTab = false;
    public void OnEnable()
    {
        UIPVPRealTimeMainSelectNinja.OnlyWatch = false;
        ToggleOnlyWatch.value = false;
    }

    public void OnDisable()
    {
        UIPVPRealTimeMainSelectNinja.OnlyWatch = false;
        ToggleOnlyWatch.value = false;
    }

    public void RefreshFilterTab(bool isForceRefresh = true)
    {
        Debuger.Log("RefreshFilterTab");
        if (!isForceRefresh && isRefreshFilterTab)
        {
            return;
        }
        isRefreshFilterTab = true;
        // 刷新该显示哪些页签
        if (teamFileterComp != null)
        {
            teamFileterComp.RefreshFilterTab();
        }

        string titleText = "";
        if (SPageNum != -1)
        {
            titleText = "[ffcc00][u]S级忍者";
        }
        else if (APageNum != -1)
        {
            titleText = "[ffcc00][u]A级忍者";
        }
        else if (BPageNum != -1)
        {
            titleText = "[ffcc00][u]B级忍者";
        }
        else
        {
            titleText = "[ffcc00][u]C级忍者";
        }

        LblNinjaLevel.text = titleText;
    }

    public void OnClickNinjaLevel()
    {
        if (NGUITools.GetActive(ObjNinajaLevelPopWindow))
        {
            NGUITools.SetActive(ObjNinajaLevelPopWindow, false);
        }
        else
        {
            NGUITools.SetActive(ObjNinajaLevelPopWindow, true);
            RefreshFilterTab(false);
        }
    }

    public void OnClickNinjaLevelS()
    {
        SpringPanel.Begin(ScrollView.gameObject, RawPos + Vector3.left * GridWidth * (SPageNum - 1), 13f);
        if (PageGroup != null)
            PageGroup.CallLuaFunctionForLua("SetCurrentPage", SPageNum);
        if (PageGroupNew != null)
            PageGroupNew.CallLuaFunction("SetCurrentPage", SPageNum);

        ObjNinajaLevelPopWindow.SetActive(false);

        LblNinjaLevel.text = "[ffcc00][u]S级忍者";
    }

    public void OnClickNinjaLevelA()
    {
        SpringPanel.Begin(ScrollView.gameObject, RawPos + Vector3.left * GridWidth * (APageNum - 1), 13f);
        if (PageGroup != null)
            PageGroup.CallLuaFunctionForLua("SetCurrentPage", APageNum);
        if (PageGroupNew != null)
            PageGroupNew.CallLuaFunction("SetCurrentPage", APageNum);

        ObjNinajaLevelPopWindow.SetActive(false);

        LblNinjaLevel.text = "[ffcc00][u]A级忍者";
    }

    public void OnClickNinjaLevelB()
    {
        SpringPanel.Begin(ScrollView.gameObject, RawPos + Vector3.left * GridWidth * (BPageNum - 1), 13f);
        if (PageGroup != null)
            PageGroup.CallLuaFunctionForLua("SetCurrentPage", BPageNum);
        if (PageGroupNew != null)
            PageGroupNew.CallLuaFunction("SetCurrentPage", BPageNum);

        ObjNinajaLevelPopWindow.SetActive(false);

        LblNinjaLevel.text = "[ffcc00][u]B级忍者";
    }

    public void OnClickNinjaLevelC()
    {
        SpringPanel.Begin(ScrollView.gameObject, RawPos + Vector3.left * GridWidth * (CPageNum - 1), 13f);
        if (PageGroup != null)
            PageGroup.CallLuaFunctionForLua("SetCurrentPage", CPageNum);
        if (PageGroupNew != null)
            PageGroupNew.CallLuaFunction("SetCurrentPage", CPageNum);

        ObjNinajaLevelPopWindow.SetActive(false);

        LblNinjaLevel.text = "[ffcc00][u]C级忍者";
    }
}
