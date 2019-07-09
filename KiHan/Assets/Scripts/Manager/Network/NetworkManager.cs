using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Apollo;
using KH.Network;
using naruto.protocol;
using ProtoBuf;
using KH.TimeUtility;

namespace KH
{
    public class SendUnicastRetInfo
    {
        public SendUnicastRetInfo()
        {

        }

        public SendUnicastRetInfo(bool _ret, uint _serial)
        {
            ret = _ret;
            serial = _serial;
        }

        public void SetData(bool _ret, uint _serial)
        {
            ret = _ret;
            serial = _serial;
        }

        public bool ret;
        public uint serial;
    }

    public class ConnectionID
    {
        /// <summary>
        /// DIR连接
        /// </summary>
        public const int DIR_CONNECTION = 1;
        /// <summary>
        /// GAME连接
        /// </summary>
        public const int GAME_CONNECTION = 2;

        /// <summary>
        /// GAME UDP 连接
        /// </summary>
        public const int GAME_UDP_CONNECTION = 3;

        /// <summary>
        /// TEAMPVE BATTLE 连接
        /// </summary>
        public const int TEAMPVE_BATTLE_CONNECTION = 4;
        /// <summary>
        /// Guild Scene连接
        /// </summary>
        public const int GUILD_SCENE_CONNECTION = 5;

        /// <summary>
        /// PVP战斗连接
        /// </summary>
        public const int GAME_PVP_CONNECTION = 6;

        /// <summary>
        /// 多人OB链接
        /// </summary>
        public const int OB_CONNECTION = 7;
    }

    /// <summary>
    /// 连接类型定义
    /// </summary>
    public class ConnectionClassType
    {
        /// <summary>
        /// 阿波罗的连接
        /// </summary>
        public const int APOLLO_CONNECTION_TYPE = 0;

        /// <summary>
        /// UDP的连接
        /// </summary>
        //public const int KIHAN_CONNECTION_UDP_TYPE  = 1;

        /// <summary>
        /// 自由连接[根据匹配情况选择]
        /// </summary>
        //public const int FREE_CONNECTION_TYPE       = 2;

        /// <summary>
        /// UDP的连接2
        /// </summary>
        public const int KIHAN_CONNECTION_UDP_TYPE2 = 3;
    }

    public class DelayTimeoutCallbackData
    {
        public TimerEntity.TimerEntityCallBack timeoutCallback;
        public uint serial;
        public DelayTimeoutCallbackData(TimerEntity.TimerEntityCallBack timeoutCallback, uint serial)
        {
            this.timeoutCallback = timeoutCallback;
            this.serial = serial;
        }
    }

    /// <summary>
    /// 用于管理网络连接和包分发
    /// 该文件为NetworkManager类的基本功能
    /// </summary>
    public partial class NetworkManager
    {
        #region 事件回了个调委托
        public delegate void NetworkMessageHandle(object message);
        #endregion

        static private KiHanProxy _net_work_proxy = null;
        static private bool _fix_udpate = false;
        static private List<DelayTimeoutCallbackData> timeoutCallbackDataList = new List<DelayTimeoutCallbackData>();

        // 上下行弱网模拟开关和丢包率
        static public bool WeakNetSimu_IsPackageLoss_Up = false;
        static public float WeakNetSimu_PackageLossRate_Up = 0.1f;
        static public bool WeakNetSimu_IsPackageLoss_Down = false;
        static public float WeakNetSimu_PackageLossRate_Down = 0.1f;

        static public void Update()
        {
            if (!_fix_udpate)
            {
                if (_net_work_proxy != null)
                    _net_work_proxy.Update();
            }

            _fix_udpate = false;

            /// Update by Chicheng
            /// 检测NTF
            MessageManager msgManager = MessageManager.Instance;
            if (msgManager.IsActivate && !msgManager.IsSerializeToLocal)
            {
                ReadNTFMessageFromLocal();
            }
        }

        /// <summary>
        /// 从本地主动获取NTF消息包
        /// </summary>
        private static void ReadNTFMessageFromLocal()
        {
            MessageManager msgManager = MessageManager.Instance;
            RemoteModel remoteModel = RemoteModel.Instance;
            // ulong time = 1560234066;
            List<NetworkMessage> NTFMessageBody = msgManager.deserializeFromLocalByTimeStamp<NetworkMessage>(MessageManager.DEST_PATH_CSharp, remoteModel.CurrentTime);
            if (NTFMessageBody.Count > 0)
            {
                List<object> messagesBody = new List<object>();
                try
                {
                    messagesBody.Add(NTFMessageBody[0].MessagesBodyBuffer[0]);
                    messagesBody.Add(PBSerializer.NDeserialize(NTFMessageBody[0].MessagesBodyBuffer[1], NTFMessageBody[0].MessageType));
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e.Message);
                }
                __Proxy.__QueueAddMessage(NTFMessageBody[0].CmdID, 0, messagesBody);
            }

        }

        static public void FixUpdate()
        {
            _fix_udpate = true;

            if (_net_work_proxy != null)
            {
                _net_work_proxy.Update();
            }

            for (int i = 0; i < NetworkManager.timeoutCallbackDataList.Count; i++)
            {
                DelayTimeoutCallbackData timeoutData = NetworkManager.timeoutCallbackDataList[i];
                timeoutData.timeoutCallback(timeoutData.serial);
            }

            NetworkManager.timeoutCallbackDataList.Clear();
        }


        private static NetworkManager _instance = null;

        public static NetworkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new NetworkManager();
                }

                return _instance;
            }
        }

        /// <summary>
        /// 最后的错误信息
        /// </summary>
        public string LastErrorMessage = "";

        /// <summary>
        /// 登录信息
        /// </summary>
        public ApolloAccountInfo AccountInfo
        {
            get
            {
                return DefineExt.AccountInfo;
            }
        }

        public EndpointConfig Config
        {
            get
            {
                return DefineExt.EPConfig;
            }
        }

        public CandidateEndpoint CandidateEndpoint
        {

            get
            {
                return DefineExt.CandidateEndpoint;
            }
        }

        /// <summary>
        /// 当前默认的连接
        /// </summary>
        public IConnection Connection
        {
            get { return proxy.Connection; }
        }

        public int ZoneID
        {
            get { return proxy.ZoneId; }

            set
            {
                proxy.ZoneId = value;
            }
        }

        private string _selectedZoneName;
        public string SelectedZoneName
        {
            get { return _selectedZoneName; }

            set
            {
                _selectedZoneName = value;
            }
        }

        public uint CurrentSSerial
        {
            get { return router == null ? 0 : router.CurrentSerial; }
        }

        public uint CurrentRSerial
        {
            get { return router == null ? 0 : router.CurrentDispatchSerial; }
        }

        public uint NextSSerial
        {
            get { return router == null ? 0 : router.NextSerial(0); }
        }

        protected KiHanProxy proxy = null;
        protected MessageTypeProvider typeProvider = null;
#if UNITY_EDITOR && KHNET_DEBUG
        protected MessageTypeProvider typeProvider_D = null;
#endif
        protected MessageRouter router = null;

        public NetworkManager()
        {
            //UnityEngine.Debuger.Log("NetworkManager::NetworkManager()");

            proxy = new KiHanProxy();
            router = proxy.Router;
            typeProvider = proxy.TypeProvider;
#if UNITY_EDITOR && KHNET_DEBUG
            typeProvider_D = proxy.TypeProvider_D;
#endif
            NetworkManager._net_work_proxy = proxy;

            ///注册命令字...
            registerTypes();
            registerDTypes();
        }

        /// <summary>
        /// 消息队例
        /// </summary>
        public NetworkMessageQueue MsgQueue
        {
            get
            {
                return proxy.MsgQueue;
            }
        }

        public MessageTypeProvider MsgTypes
        {
            get
            {
                return typeProvider;
            }
        }

        public IConnection CreateConnection(int connID = -1, string url = "", bool isDefault = true, int classType = ConnectionClassType.APOLLO_CONNECTION_TYPE, bool autoDis = true)
        {
            return proxy.CreateConnection(connID, url, isDefault, classType, autoDis);
        }

        public void DeleteConnection(int connID)
        {
            proxy.DeleteConnection(connID);
        }

        public IConnection GetConnection(int connID)
        {
            return proxy.GetConnection(connID);
        }

        public IConnection Disconnect(int connID = -1)
        {
            return proxy.Disconnect(connID);
        }

        public void RegisterDCmdType(uint cmdId, Type type)
        {
#if UNITY_EDITOR && KHNET_DEBUG
            typeProvider_D.RegisterCmdType(cmdId, type); //根据type去找本地序列化好的预设结果
#endif
        }

        /// <summary>
        /// 添加一个CmdId到Type的映射
        /// </summary>
        public void RegisterCmdType(uint cmdId, Type type)
        {
            typeProvider.RegisterCmdType(cmdId, type);
        }

        public Type GetTypeFromCmd(uint cmdId)
        {
            return typeProvider.GetTypeFromCmd(cmdId);
        }

        public bool Send<T>(UInt32 cmdId, T message, int connID = -1) where T : IExtensible
        {
            return proxy.Write(cmdId, message, 0, connID);
        }

        /// <summary>
        /// 供lua使用的非泛型方法
        /// </summary>
        public bool SendBordcast(UInt32 cmdId, object message, int connID = -1)
        {
            return proxy.Write(cmdId, message as IExtensible, 0, connID);
        }

        /// <summary>
        /// 添加一个回调
        /// </summary>
        /// <param name="cmdId">触发回调的CmdId</param>
        /// <param name="handle">被触发的回调</param>
        /// <param name="inMainThread">回调是否应当在主线程调用</param>
        public void AddMessageCallback(uint cmdId, NetworkMessageHandle handle)
        {
            router.AddMessageCallback(cmdId, handle);
        }

        /// <summary>
        /// 去除一个回调
        /// </summary>
        /// <param name="cmdId">触发回调的CmdId</param>
        /// <param name="handle">被触发的回调</param>
        /// <param name="inMainThread">回调是否应当在主线程调用</param>
        public void RemoveMessageCallback(uint cmdId, NetworkMessageHandle handle)
        {
            router.RemoveMessageCallback(cmdId, handle);
        }

        public bool SendWithSerial(uint cmdId
            , IExtensible message
            , NetworkMessageHandle callback
            , bool displayTip = false
            , TimerEntity.TimerEntityCallBack timeoutCallback = null
            , double timeout = 10
            , int connID = -1
            , uint serial = 0
            , string tag = "None")
        {
            bool result = proxy.Write(cmdId, message, serial, connID);

            if (!result)
            {
                ///发送失败直接超时回调
                if (timeoutCallback != null)
                {
                    DelayTimeoutCallbackData timeoutData = new DelayTimeoutCallbackData(timeoutCallback, serial);
                    NetworkManager.timeoutCallbackDataList.Add(timeoutData);
                    router.OnTimeoutConnectCheck();
                }
            }
            else
            {
                router.AddUnicastCallback(cmdId, serial, callback
                    , displayTip, timeoutCallback, timeout
                    , connID, tag, PBTYPE.CSharpPB);
            }

            return result;
        }

        /// <summary>
        /// 发送协议且侦听回调
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmdId"></param>
        /// <param name="message"></param>
        /// <param name="callback"></param>
        /// <param name="displayTip"></param>
        /// <param name="timeoutCallback"></param>
        /// <param name="timeout"></param>
        /// <param name="connID"></param>
        /// <returns></returns>
        public bool Send<T>(uint cmdId
            , T message
            , NetworkMessageHandle callback
            , bool displayTip = false
            , TimerEntity.TimerEntityCallBack timeoutCallback = null
            , double timeout = 10
            , int connID = -1
            , string tag = "None") where T : IExtensible
        {
#if UNITY_EDITOR && KHNET_DEBUG
            if (typeProvider_D.ContainsKey(cmdId))
            {
                if (callback != null)
                {
                    object mockresp = getRespByType(typeProvider_D[cmdId]);
                    if (mockresp != null)
                    {
                        callback(mockresp);
                        return false; //模拟回包
                    }
                    else
                    {
                        UnityEngine.Debuger.LogError("数据实体未定义");
                    }
                }
                
            }
#endif
            return SendMessage(cmdId, message, callback, displayTip, timeoutCallback, timeout, connID, tag);
        }


        public bool SendMessage(uint cmdId
            , IExtensible message
            , NetworkMessageHandle callback
            , bool displayTip = false
            , TimerEntity.TimerEntityCallBack timeoutCallback = null
            , double timeout = 10
            , int connID = -1
            , string tag = "None")
        {
            uint serial = router.NextSerial(cmdId, tag);
            bool result = true;

            // Update by Chicheng
            MessageManager msgManager = MessageManager.Instance;
            // 模拟服务器，从本地读包
            if (msgManager.IsActivate && !msgManager.IsSerializeToLocal)
            {
                // 反序列化之后的结果
                List<object> messagesBody = msgManager.deserializeFromLocal(cmdId);
                try
                {
                    __Proxy.__QueueAddMessage(cmdId, serial, messagesBody);
                }
                catch (NullReferenceException)
                {
                    return false;
                }
            }
            else
            {
                result = proxy.Write(cmdId, message, serial, connID);
            }

            if (!result)
            {
                ///发送失败直接超时回调
                if (timeoutCallback != null)
                {
                    DelayTimeoutCallbackData timeoutData = new DelayTimeoutCallbackData(timeoutCallback, serial);
                    NetworkManager.timeoutCallbackDataList.Add(timeoutData);
                    router.OnTimeoutConnectCheck();
                }
            }
            else
            {
                router.AddUnicastCallback(cmdId, serial, callback
                    , displayTip, timeoutCallback, timeout
                    , connID, tag, PBTYPE.CSharpPB);
            }

            return result;
        }

        public void SendFake<T, TRsp>(uint cmdId,
                                T message, NetworkMessageHandle callback,
                                TRsp messageRsp
            , int connID = -1
            ) where T : IExtensible
        {
            //Send<T>(cmdId, message, callback, true , _default_timeout_func,10,connID);
            callback(messageRsp);
        }

        /// <summary>
        /// 默认超时函数处理
        /// </summary>
        private void _default_timeout_func(object data)
        {
            UIAPI.ShowMsgTip("发送请求超时了");
        }

        private SendUnicastRetInfo m_retInfo = new SendUnicastRetInfo();
        /// <summary>
        /// 共Lua使用的非泛型方法
        /// 返回值为 0 说明是发送失败，否则返回 serial
        /// </summary>
        public SendUnicastRetInfo SendUnicast(uint cmdId,
                                object message,
                                NetworkMessageHandle callback,
                                bool displayTip = false,
                                TimerEntity.TimerEntityCallBack timeoutCallback = null,
                                double timeout = 10,
                                int connID = -1,
                                string tag = "None")
        {
            uint serial = router.NextSerial(cmdId, tag);
            bool result = true;

            // Update by Chicheng
            MessageManager msgManager = MessageManager.Instance;
            // 模拟服务器，从本地读包
            if (msgManager.IsActivate && !msgManager.IsSerializeToLocal)
            {
                // Debug.LogWarning("模拟服务器，从本地读包Lua");
                // Lua反序列化之后的结果
                try
                {
                    List<object> messagesBody = msgManager.deserializeFromLocal(cmdId);
                    if (messagesBody != null)
                    {
                        __Proxy.__QueueAddMessage(cmdId, serial, messagesBody);
                        //__Proxy.AddMessage(cmdId, serial, messagesBody[0]);
                    }
                }
                catch (Exception)
                {
                    Debug.LogWarning("Lua这里出错");
                }

            }
            else
            {
                result = proxy.Write(cmdId, message, serial, connID);
            }



            if (result)
            {
                PBTYPE pbtype = PBTYPE.None;
                if (message is byte[])
                    pbtype = PBTYPE.LuaPB;
                else
                    pbtype = PBTYPE.CSharpPB;

                router.AddUnicastCallback(cmdId, serial, callback
                    , displayTip
                    , timeoutCallback
                    , timeout
                    , connID
                    , tag
                    , pbtype);

                m_retInfo.SetData(true, serial);
            }
            else
            {
                if (timeoutCallback != null)
                {
                    DelayTimeoutCallbackData timeoutData = new DelayTimeoutCallbackData(timeoutCallback, serial);
                    NetworkManager.timeoutCallbackDataList.Add(timeoutData);
                    router.OnTimeoutConnectCheck();
                }

                m_retInfo.SetData(false, serial);
            }

            return m_retInfo;
        }

    }
}

