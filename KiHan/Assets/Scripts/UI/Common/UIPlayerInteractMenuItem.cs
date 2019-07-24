using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace KH
{
    // 菜单项
    public class UIPlayerInteractMenuItem : MonoBehaviour
    {
        public UILabel _Name;

        private PlayerInteractInfo TargetPlayer;
        private UIPlayerInteractPopup.MenuItemArg MenuItemArg;

        public void Rebind(PlayerInteractInfo target, UIPlayerInteractPopup.MenuItemArg arg)
        {
            TargetPlayer = target;
            MenuItemArg = arg;
            _Name.text = arg.Name;
        }

        public void OnClick()
        {
            if (MenuItemArg == null) return;
            // 触发菜单功能
            MenuItemArg.OnClick(TargetPlayer);
        }

    }
}
