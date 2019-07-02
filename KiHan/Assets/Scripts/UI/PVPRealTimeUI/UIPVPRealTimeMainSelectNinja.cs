using UnityEngine;
using System.Collections;
using KH;
using naruto.protocol;
using System.Collections.Generic;
using kihan_general_table;
using KH.Ninja;
using KH.Plugins;
using GameJoyAPI;
using naruto.protocol;
using KH.Lua;

public class UIPVPRealTimeMainSelectNinja : MonoBehaviour 
{
    /* 决斗场3.0 新增内容 */

    // 名字修改
    private static PvpFightType currentFightType;

    public static PvpFightType FightType
    {
        get
        {
            if (KHUIManager.Instance.IsWindowVisible(UIDef.LEVEL_SELECTBG_UI))  // 通灵兽
            {
                return PvpFightType.PvpFightType_Arena;
            }
            else
            {
                Debug.LogWarning("没有对应的");
                return PvpFightType.PvpFightType_Count;
            }
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
		if(PageGroup != null)
			PageGroup.CallLuaFunctionForLua("SetCurrentPage", SPageNum);
		if(PageGroupNew != null)
			PageGroupNew.CallLuaFunction("SetCurrentPage", SPageNum);

		ObjNinajaLevelPopWindow.SetActive(false);

		LblNinjaLevel.text = "[ffcc00][u]S级忍者";
	}
	
	public void OnClickNinjaLevelA()
	{
		SpringPanel.Begin(ScrollView.gameObject, RawPos + Vector3.left * GridWidth * (APageNum - 1), 13f);
		if(PageGroup != null)
			PageGroup.CallLuaFunctionForLua("SetCurrentPage", APageNum);
		if(PageGroupNew != null)
			PageGroupNew.CallLuaFunction("SetCurrentPage", APageNum);

		ObjNinajaLevelPopWindow.SetActive(false);

		LblNinjaLevel.text = "[ffcc00][u]A级忍者";
	}
	
	public void OnClickNinjaLevelB()
	{
		SpringPanel.Begin(ScrollView.gameObject, RawPos + Vector3.left * GridWidth * (BPageNum - 1), 13f);
		if(PageGroup != null)
			PageGroup.CallLuaFunctionForLua("SetCurrentPage", BPageNum);
		if(PageGroupNew != null)
			PageGroupNew.CallLuaFunction("SetCurrentPage", BPageNum);

		ObjNinajaLevelPopWindow.SetActive(false);

		LblNinjaLevel.text = "[ffcc00][u]B级忍者";
	}
	
	public void OnClickNinjaLevelC()
	{
		SpringPanel.Begin(ScrollView.gameObject, RawPos + Vector3.left * GridWidth * (CPageNum - 1), 13f);
		if(PageGroup != null)
			PageGroup.CallLuaFunctionForLua("SetCurrentPage", CPageNum);
		if(PageGroupNew != null)
			PageGroupNew.CallLuaFunction("SetCurrentPage", CPageNum);

		ObjNinajaLevelPopWindow.SetActive(false);

		LblNinjaLevel.text = "[ffcc00][u]C级忍者";
	}
}
