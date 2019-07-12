using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace KH
{
	public class DefaultZoneWin : UIWindow
	{
		public UILabel lb_platform;
		public UILabel lb_zone_num;
		public UILabel lb_zone_name;
		public ZoneStatusIcon _icon_status;
        public GameObject AgreeObj;
        public GameObject UseProtocol;

		LoginModel model;
		BootPlugin plugin;

        private bool mAgree = true;
        private GameObject _OpenedUseProtocolObj = null;

		void OnEnable()
		{
            RefreshUI();
            _UpdateAgreeUI();
#if UNITY_EDITOR
			// [sniperlin] 极速登录：自动选择大区
			if (DefineExt.AutoLoginAndSelectArea)
			{
				OnBtnStart();
			}
#endif
        }

        public void RefreshUI()
        {
            plugin = KHPluginManager.Instance.GetPluginByName("Boot") as BootPlugin;
            model = plugin.Model as LoginModel;
            int zoneid = model.selected_zoneid % 10000;
            lb_platform.text = KHEnumToStringUtil.getLoginTypeString(NetworkManager.Instance.Config.Platform);
            lb_zone_num.text = zoneid.ToString() + "区";
            lb_zone_name.text = model.selected_zoneName;

            _icon_status.status = model.selected_zonestatus;
        }

        private void _UpdateAgreeUI()
        {
            NGUITools.SetActive(AgreeObj, mAgree);
        }

		public void OnBtnBack()
		{
            KHGlobalExt.LogoutGame(true);
			plugin.SendMessage("Login.showLoginUI");

			LoginModel model = plugin.Model as LoginModel;
			(model as LoginModel).selected_zoneid = -1;
		}


		public void OnBtnStart()
		{
            //ShowForceUpGradeTip("ShowUpgradeTimeStamp");

            if (BootPlugin.loginButtonSndID != null)
            {
                for (int i=0;i<BootPlugin.loginButtonSndID.Length;i++)
                {
                    NGUITools.PlaySound(BootPlugin.loginButtonSndID[i], 1, 1);
                }
            }
            if (!mAgree)
            {
                UIAPI.ShowMsgTip("请勾选同意下方的[FFCC00]腾讯游戏用户协议[-]和[FFCC00]隐私政策[-], 即可进入游戏!");
                return;
            }
			plugin.SendMessage("Login.LoginGame");
            Debug.Log("OnBtnStart");

            if (MessageManager.Instance.IsActivate)
            {
                if (!MessageManager.Instance.IsSerializeToLocal)
                {
                    GameObject mouseActManager = new GameObject("MouseActionManager");
                    mouseActManager.AddComponent<MouseActionManager>();
                    // mouseevent.execute();
                    // GameObject mouseSimulator = new GameObject("MouseSimulator");
                    DontDestroyOnLoad(mouseActManager);
                }
                else {
                    GameObject mouseMonitor = new GameObject("MouseMonitor");
                    mouseMonitor.AddComponent<MouseMonitor>();
                    DontDestroyOnLoad(mouseMonitor);
                }
            }
            
        }

        public void ShowForceUpGradeTip(string attr)
        {
            if (Application.platform != RuntimePlatform.IPhonePlayer || !KHUtil.IsLowMemoryDevice()) return;

            //每天提示一次
            int lastTimeStamp = PlayerPrefs.GetInt(attr, 0);
            if ((int) RemoteModel.Instance.CurrentTime - lastTimeStamp < 86400) return;
            PlayerPrefs.SetInt(attr, (int) RemoteModel.Instance.CurrentTime);

            var info = KHUtil.GetOsInfo();
            if (info.MainLevel == 12 && info.Sub1Level == 1)
            {
                UIAPI.ShowMsgOK("建议您升级到iOS最新系统版本，可改善闪退和卡顿问题。");
            }
        }

		public void OnBtnSelectZone()
		{
            plugin.SendMessage("Login.showSelectZoneUI");

            OpenDebuger();
        }

        public void OpenDebuger()
        {
            //开debugergui偷鸡
            if (DefineExt.ClickSelectZoneCount < 0) return;
            DefineExt.ClickSelectZoneCount++;

            if (DefineExt.ClickSelectZoneCount == 6)
            {
                string[] strVer = KHVer.vernum.Split(new string[] { "." }, System.StringSplitOptions.RemoveEmptyEntries);
                ushort minor = ushort.Parse(strVer[1]);
                if (minor == 26)  //当前迭代版本
                    KHDebugerGUI.Show(KHDebugerPermission.Admin);
            }
        }

        

        public void OnBtnAgree()
        {
            mAgree = !mAgree;
            _UpdateAgreeUI();
        }

	    // 点击用户协议Link
	    public static string UseProtocolLinkWebUrl = "https://game.qq.com/contract.shtml";
		// 点击隐私策略Link
		public static string UsePrivacyLinkWebUrl {
			get {
				return "https://game.qq.com/privacy_guide.shtml";
			}
		}

        public void OnBtnShowProtocol()
        {
            // 改为跳转URL的方式
            string jumpUrl = UseProtocolLinkWebUrl;
            Debuger.Log("jumpUrl:" + jumpUrl);

            GlobalUtils.OpenURLWithExtraInfo(jumpUrl);
        }

		public void OnBtnShowPrivacy()
		{
			// 改为跳转URL的方式
			string jumpUrl = UsePrivacyLinkWebUrl;
			Debuger.Log("jumpUrl:" + jumpUrl);

			GlobalUtils.OpenURLWithExtraInfo(jumpUrl);
		}

		// 当新用户首次登录游戏时（对于老用户而言，也须强制弹出一次）
		public void FirstOnceShowProtocol()
	    {
            if (_OpenedUseProtocolObj == null)
            {

                _OpenedUseProtocolObj = NGUITools.AddChild(this.gameObject, UseProtocol);

                if (_OpenedUseProtocolObj != null)
                {
                    // 取得当前最大depth值
                    UIPanel[] panels = this.gameObject.GetComponentsInChildren<UIPanel>(true);
                    int depth = 0;
                    for (int i = 0; i < panels.Length; ++i)
                    {
                        UIPanel p = panels[i];
                        depth = Mathf.Max(p.depth, depth);
                    }

                    // 重新给新加进来的panel赋值
                    panels = _OpenedUseProtocolObj.GetComponentsInChildren<UIPanel>(true);
                    if (panels != null)
                    {
                        List<UIPanel> addPanels = new List<UIPanel>(panels);
                        addPanels.Sort((UIPanel p1, UIPanel p2) =>
                        {
                            if (p1 == null) return 1;
                            if (p2 == null) return -1;
                            return p1.depth - p2.depth;
                        });
                        for (int i = 0; i < addPanels.Count; ++i)
                        {
                            UIPanel p = panels[i];
                            p.depth = depth + i;
                        }
                    }
                }

            }

            UIAPI.SetVisible(_OpenedUseProtocolObj, true);
	    }
    }
}