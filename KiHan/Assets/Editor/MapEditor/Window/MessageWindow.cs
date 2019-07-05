using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class MessageWindow : EditorWindow {

    private comfirmDelegate comfirm;
    private cancelDelegate cancel;
    private string titleText;

    public comfirmDelegate Comfirm
    {
        set { comfirm = value; }
    }

    public cancelDelegate Cancel
    {
        set { cancel = value; }
    }

    public string TitleText
    {
        set { titleText = value; }
    }

    public delegate void comfirmDelegate(EditorWindow window);
    public delegate void cancelDelegate(EditorWindow window);

    void executeComfirm(comfirmDelegate comfirm)
    {
        comfirm(this);
    }
    void executeCancel(cancelDelegate cancel)
    {
        cancel(this);
    }

    void OnGUI()
    {
        GUILayout.Label(titleText);

        // 确定按钮
        if (GUI.Button(new Rect(30, 180, 100, 30), "确定"))
        {
            executeComfirm(comfirm);
        }

        // 取消按钮
        if (GUI.Button(new Rect(200, 180, 100, 30), "取消"))
        {
            Debug.Log("取消");
            executeCancel(cancel);
        }
    }

    public static void CreateMessageBox(string title, comfirmDelegate onComfirm, cancelDelegate onCancel)
    {
        MessageWindow messageWindow = CreateInstance<MessageWindow>();
        messageWindow.TitleText = title;
        messageWindow.Comfirm = onComfirm;
        messageWindow.Cancel = onCancel;
        messageWindow.Show();
    }
}