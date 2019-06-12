using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;


namespace KH.Network
{
    public class FSPClient
    {
        //===========================================================
        public delegate void KHFSPTimeoutListener(FSPClient target, int val);
        
        //===========================================================
        //日志
        public string LOG_TAG_SEND = "FSPClient_Send";
        public string LOG_TAG_RECV = "FSPClient_Recv";
        public string LOG_TAG_MAIN = "FSPClient_Main";
        public string LOG_TAG = "FSPClient";

        //一些用于统计的字段
        #region 统计字段

        public static bool LastUseGSDK = false;
        public static int LastGSDKState
        {
            get
            {
                if (!LastUseGSDK)
                {
                    //后台未开启GSDK
                    return 0;
                }
                else
                {
                    if (!GSDKManager.Instance.IsWork)
                    {
                        //后台开启了GSDK，但是GSDK自己判断不启动
                        return 1;
                    }
                    else
                    {
                        //后台开启了GSDK，并且GSDK自己也启动了
                        return 2;
                    }
                }
            }
        }

        public static int LastRemotePort = 0;
        public static string LastRemoteHost = "";
        public static string LastRemoteHostIP = "";
        public static float LastFSPStartTimeSinceGameStart = 0;
        public static int SocketReceiveSleep = 10;
        public static bool EnableReceiveBlock = true;

        #endregion
        //===========================================================
        //基本数据
        
        //线程模块
        private bool m_IsRunning = false;
        private Thread m_ThreadSend;
        private Thread m_ThreadReceive;

        //基础通讯模块
        private IUdpSocket m_Socket;
        private string m_Host;
        private int m_Port;
        private IPEndPoint m_HostEndPoint = null;
        private ushort m_SessionId = 0;

        private bool m_UseGSDK = false;
        private bool m_UseEmptyFrameAckAvoid = false;
        private bool m_IsLanOB = false;
        //===========================================================
        //FSP编解码模块
        private FSPCodec m_FSPCodec;

        //CheckSum
        private bool m_UseCheckSum_Recv = false;
        private bool m_UseCheckSum_Send = false;
        //加解密
        private FSPCryptor m_Cryptor;

        //===========================================================
        //接收逻辑
        private NetBuffer m_ReceiveBufferTemp = new NetBuffer(4096);
        
        //发送逻辑
        private bool m_EnableFSPSend = true;
        private bool m_WaitForReconnect = false;
        private bool m_WaitForSendAuth = false;
        private KHFSPTimeoutListener m_FSPTimeoutHandler;

        //发送优化
        private int m_SendIntervalDefault = 99;
        private int m_SendIntervalWifi = 99;
        private bool m_UseWifiOptimize = false;

        private static FSPCodec cachedCodec = null;
        public static List<FSPCodec.UDPDropPacketInfo> DropPacketStates
        {
            get 
            { 
                return cachedCodec != null? cachedCodec.UDPDropPacketStat: null; 
            }
        }
        
        //===========================================================
        //===========================================================
        //------------------------------------------------------------
    #region 构造与析构
        public FSPClient()
        {
            m_FSPCodec = new FSPCodec();
			m_FSPCodec.EnableTimeOut = false;
            cachedCodec = m_FSPCodec;

            //初始化统计代码
            LastRemotePort = 0;
            LastRemoteHost = "";
            LastRemoteHostIP = "";
            LastUseGSDK = false;
            LastFSPStartTimeSinceGameStart = Time.realtimeSinceStartup;
        }



        public void Close()
        {
            Debuger.Log(LOG_TAG_MAIN, "Close()");
            Disconnect();
            //m_FSPCodec = null;这个类既然是构造时生成，那么就应该一直存在。直接真正析构
            m_FSPTimeoutHandler = null;
            m_WaitForReconnect = false;
            m_WaitForSendAuth = false;
            cachedCodec = null;
        }


    #endregion


        //------------------------------------------------------------
    #region Socket管理

        private IUdpSocket CreateSocket(AddressFamily family, string host, int port)
        {
            UdpSocket socket = null;
            if (family == AddressFamily.InterNetwork)
            {
                socket = new UdpSocket(family, EnableReceiveBlock);
                socket.Bind(UdpSocket.AnyPort);
            }
            else
            {
                socket = new UdpSocket(family, EnableReceiveBlock);
                socket.Bind(UdpSocket.AnyPort);
            }

            socket.SetLocalSocket(m_IsLanOB, true);
            return socket;
        }
    #endregion

        //------------------------------------------------------------
    #region 设置通用参数

        public void SetCryptKey(byte[] key)
        {
            Debuger.Log(LOG_TAG_MAIN, "SetCryptKey() {0}", key != null && key.Length > 0);
            if (key != null && key.Length > 0)
            {
                m_Cryptor = new FSPCryptor(key);
            }
            else
            {
                m_Cryptor = null;
            } 
        }


        public void SetCheckSumEnable(bool checkSend, bool checkRecv)
        {
            m_UseCheckSum_Send = checkSend;
            m_UseCheckSum_Recv = checkRecv;
        }

        public void SetSessionId(ushort sid)
        {
            LOG_TAG_MAIN = "FSPClient_Main<" + sid.ToString("d4") + ">";
            LOG_TAG_SEND = "FSPClient_Send<" + sid.ToString("d4") + ">";
            LOG_TAG_RECV = "FSPClient_Recv<" + sid.ToString("d4") + ">";
            LOG_TAG = LOG_TAG_MAIN;

            m_SessionId = sid;
            m_FSPCodec.SetSessionId(sid);
        }

        public void SetSendInterval(int defaultValue, int wifiValue)
        {
            Debuger.Log(LOG_TAG_MAIN, "SetSendInterval() default = {0}, wifi = {1} ", defaultValue, wifiValue);

            m_SendIntervalDefault = defaultValue;
            if (m_SendIntervalDefault <= 0)
            {
                m_SendIntervalDefault = 99;
            }

            m_SendIntervalWifi = wifiValue;
            if (m_SendIntervalWifi <= 0)
            {
                m_SendIntervalWifi = m_SendIntervalDefault;
            }

        }

        public void SetCheckSumEnable(bool enable)
        {
            Debuger.Log(LOG_TAG_MAIN, "SetCheckSumEnable() " + enable);
            m_UseCheckSum_Send = enable;
            m_UseCheckSum_Recv = enable;
        }

        public void SetGSDKEnable(bool enable)
        {
            Debuger.Log(LOG_TAG_MAIN, "SetGSDKEnable() " + enable);
            m_UseGSDK = enable;
            LastUseGSDK = enable;
        }

        public void SetEmptyFrameAckAvoid(bool enable)
        {
            Debuger.Log(LOG_TAG_MAIN, "SetEmptyFrameAckAvoid() " + enable);
            m_UseEmptyFrameAckAvoid = enable;
            m_FSPCodec.SetEmptyFrameAckAvoid(m_UseEmptyFrameAckAvoid);
        }

        public void SetLanOB(bool value)
        {
            Debuger.Log(LOG_TAG_MAIN, "SetLanOB() " + value);
            m_IsLanOB = value;

        }

    #endregion

        //------------------------------------------------------------
    #region 设置FSP参数
        /**
         * 如果参数为0，则超时开始，如果参数为-1，则超时结束，如果参数大于0，则超时进行中
         **/
        public void SetFSPTimeoutListenerInThread(KHFSPTimeoutListener handler)
        {
            m_FSPTimeoutHandler = handler;
        }

        public void SetFSPRoundControlVKey(int beginKey, int endKey)
        {
            m_FSPCodec.SetRoundControlVKey(beginKey, endKey);
        }

        public void SetFSPAuthInfo(int authKey, int authId)
        {
            Debuger.Log(LOG_TAG_MAIN, "SetFSPAuthInfo() " + authKey + ":" + authId);
            m_FSPCodec.SetAuthInfo(authKey, authId);
            m_FSPCodec.VerifyAuth();
        }
    #endregion

        //------------------------------------------------------------

    #region 基础连接函数

        public bool IsRunning { get { return m_IsRunning; } }

        public void VerifyAuth()
        {
            m_WaitForSendAuth = false;
            m_FSPCodec.VerifyAuth();
        }

        public void Reconnect()
        {
            Debuger.Log(LOG_TAG_MAIN, "Reconnect() 重新连接");
            m_WaitForReconnect = false;
            Disconnect();
            Connect(m_Host, m_Port);

            ////记录断线重连时的svr seq，用于统计seq丢包时过滤断线重连时的影响
            ////by johnfu
			m_FSPCodec.OnReconnect();

            m_FSPCodec.VerifyAuth();

			//add by kevinlin
			PVPBufferingRecord.Instance.Clear();
        }

        public bool Connect(string host, int port)
        {
            if (m_Socket != null)
            {
                Debuger.LogError(LOG_TAG_MAIN, "Connect() 无法建立连接，需要先关闭上一次连接！");
                return false;
            }

            Debuger.Log(LOG_TAG_MAIN, "Connect() 建立基础连接， host = {0}, port = {1}", (object)host, port);

            m_Host = host;
            m_Port = port;

            try
            {
                //获取Host对应的IPEndPoint
                Debuger.Log(LOG_TAG_MAIN, "Connect() 获取Host对应的IPEndPoint");
                m_HostEndPoint = UdpSocket.GetHostEndPoint(m_Host, m_Port);
                if (m_HostEndPoint == null)
                {
                    Debuger.LogError(LOG_TAG_MAIN, "Connect() 无法将Host解析为IP！");
                    Close();
                    return false;
                }
                Debuger.Log(LOG_TAG_MAIN, "Connect() HostEndPoint = {0}", m_HostEndPoint.ToString());

                m_IsRunning = true;

                //创建Socket
                Debuger.Log(LOG_TAG_MAIN, "Connect() 创建UdpSocket, AddressFamily = {0}", m_HostEndPoint.AddressFamily);
                m_Socket = CreateSocket(m_HostEndPoint.AddressFamily, m_Host, m_Port);
                
                if (m_UseGSDK)
                {
                    Debuger.Log(LOG_TAG_MAIN, "Connect() 启动GSDK加速！");
                    GSDKManager.Instance.StartSpeed(m_HostEndPoint.Address.ToString(), m_Host, m_Port);

                    GSDKManager.Instance.QueryNetwork(true, true, true);
                }

                //创建线程
                Debuger.Log(LOG_TAG_MAIN, "Connect() 创建接收线程");
                m_ThreadReceive = new Thread(Thread_Receive) { IsBackground = true };
                m_ThreadReceive.Start();

                Debuger.Log(LOG_TAG_MAIN, "Connect() 创建发送线程");
                m_ThreadSend = new Thread(Thread_Send) { IsBackground = true };
                m_ThreadSend.Start();

            }
            catch (Exception e)
            {
                Debuger.LogError(LOG_TAG_MAIN, "Connect() " + e.Message + e.StackTrace);
                Close();
                return false;
            }


            //统计字段
            LastRemotePort = port;
            LastRemoteHost = host;
            LastRemoteHostIP = m_HostEndPoint.Address.ToString();
            

            //当用户直接用UnityEditor上的停止按钮退出游戏时，会来不及走完整的析构流程。
            //这里做一个监听保护
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playmodeStateChanged -= OnEditorPlayModeChanged;
            UnityEditor.EditorApplication.playmodeStateChanged += OnEditorPlayModeChanged;
#endif
            KHDebugerGUI.AddDbgGUI("FSPClient", OnDbgGUI);

            return true;
        }

        private void Disconnect()
        {
            Debuger.Log(LOG_TAG_MAIN, "Disconnect()");

            KHDebugerGUI.RemoveDbgGUI("FSPClient");

            m_IsRunning = false;

            if (m_ThreadReceive != null)
            {
                m_ThreadReceive.Interrupt();
                m_ThreadReceive = null;
            }

            if (m_ThreadSend != null)
            {
                m_ThreadSend.Interrupt();
                m_ThreadSend = null;
            }

            if (m_Socket != null)
            {
                m_Socket.Close();
                m_Socket = null;
            }

            if (m_UseGSDK)
            {
                GSDKManager.Instance.EndSpeed();
            }

            m_HostEndPoint = null;
        }


#if UNITY_EDITOR
        private void OnEditorPlayModeChanged()
        {
            if (Application.isPlaying == false)
            {
                UnityEditor.EditorApplication.playmodeStateChanged -= OnEditorPlayModeChanged;
                Disconnect();
            }
        }
#endif

#endregion


        //------------------------------------------------------------

#region Receive线程

        private void Thread_Receive()
        {
            Debuger.Log(LOG_TAG_RECV, "Thread_Receive() Begin......");
            while (m_IsRunning)
            {
                try
                {
                    if (!DoReceive())
                    {
                        if (SocketReceiveSleep > 0)
                        {
                            Thread.Sleep(SocketReceiveSleep);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (m_IsRunning)
                    {
                        Debuger.LogError(LOG_TAG_RECV, "Thread_Receive() " + e.Message + "\n" + e.StackTrace);
                        Thread.Sleep(500);
                    }
                }
            }
            Debuger.Log(LOG_TAG_RECV, "Thread_Receive() End!");
        }

        private bool DoReceive()
        {
            NetBuffer buffer = ReceiveBuffer();

            if (buffer == null)
            {
                return false;
            }


            //判断数据是否FSP协议
            //获取Seq和Ack，在FSP协议中，Seq和Ack不为0
            buffer.Position = 0;
            ushort seq = buffer.ReadUShort();//第1个字段必然是Decode的SEQ
            ushort ack = buffer.ReadUShort();//第2个字段必然是Decode的ACK
            if (seq == 0 && ack == 0)
            {
                //如果为0，则不是FSP协议
                HandleCustomMessage(buffer);
            }
            else
            {
                //接收FSP数据    
                m_FSPCodec.WriteToRecvQueue(buffer);
            }
            return true;
        }


        private NetBuffer ReceiveBuffer()
        {
            int len = 0;
            IPEndPoint ipepRemote = m_HostEndPoint;
            len = m_Socket.ReceiveFrom(m_ReceiveBufferTemp.GetBytes(), m_ReceiveBufferTemp.Capacity, ref ipepRemote);

            if (len <= 0)
            {
                //Debuger.LogWarning(LOG_TAG_RECV, "DoReceive() 收到的数据Len <= 0！ ");
                return null;
            }

            m_ReceiveBufferTemp.AddLength(len, 0);
            m_ReceiveBufferTemp.Position = 0;

            NetBuffer buffer = m_ReceiveBufferTemp;

            //CheckSum
            if (m_UseCheckSum_Recv)
            {
                if (!FSPCryptor.ValidCheckSum(buffer))
                {
                    return null;
                }
            }

            //解密
            if (m_Cryptor != null)
            {
                m_Cryptor.Decrypt(buffer.GetBytes(), buffer.Length);
            }

            if (FSPDebuger.EnableLog)
            {
                Debuger.Log(LOG_TAG_RECV, "ReceiveBuffer() Size={0}, IP={1}, Buffer={2}",
                    buffer.Length, ipepRemote, buffer.ToHexString());
            }

            return buffer;
        }



        public int ReceiveFSP(out List<SyncFrame> listFrames)
        {
            if (m_IsRunning)
            {
                listFrames = m_FSPCodec.ReadFromRecvQueue();
                if (listFrames != null)
                {
                    if (FSPDebuger.EnableLog)
                    {
                        for (int i = 0; i < listFrames.Count; i++)
                        {
                            SyncFrame frame = listFrames[i];
                            if(frame != null)
                            {
                                Debuger.Log(LOG_TAG_MAIN, "ReceiveFSP() [" + i + "]Frame = " + frame.ToString());
                            }
                            else
                            {
                                Debuger.LogError(LOG_TAG_MAIN, "ReceiveFSP() [" + i + "]Frame = null, Maybe MultiThread Problem!");
                            }
                        }
                    }

                    return listFrames.Count;
                }    
            }
            listFrames = null;
            return 0;
        }

#endregion


        //------------------------------------------------------------

#region Send线程
        private void Thread_Send()
        {
            Debuger.Log(LOG_TAG_SEND, "Thread_Send() Begin......");
            while (m_IsRunning)
            {
                try
                {
                    DoSend();
                }
                catch (Exception e)
                {
                    if (m_IsRunning)
                    {
                        Debuger.LogError(LOG_TAG_SEND, "Thread_Send() " + e.Message + "\n" + e.StackTrace);
                        Thread.Sleep(1000);
                    }
                }

                lock (this)
                {
                    if (m_UseWifiOptimize)
                    {
                        Monitor.Wait(this, m_SendIntervalWifi);
                    }
                    else
                    {
                        Monitor.Wait(this, m_SendIntervalDefault);    
                    }
                }
            }
            Debuger.Log(LOG_TAG_SEND, "Thread_Send() End! ");
        }


        private void DoSend()
        {
            NetBuffer buffer = null;

            //发送FSP数据
            if (m_FSPCodec != null)
            {
                if (m_EnableFSPSend)
                {
                    bool isSendBufferChanged = false;
                    buffer = m_FSPCodec.ReadFromSendQueue(out isSendBufferChanged);

                    if (buffer != null && buffer.Length > 0)
                    {
                        int sendCnt = SendBuffer(buffer, !isSendBufferChanged);
                    }

                    if (m_EnableFSPTimeout)
                    {
                        CheckFSPTimeout();
                    }
                }
                else
                {
                    Debuger.Log(LOG_TAG_SEND, "DoSend() m_EnableFSPSend = {0}", m_EnableFSPSend);
                }
            }
        }

        private int SendBuffer(NetBuffer buffer, bool isReSend)
        {
            //进行加密
            if (m_Cryptor != null)
            {
                if (!isReSend)
                {
                    m_Cryptor.Encrypt(buffer.GetBytes(), buffer.Length);
                }
            }

            //写入CheckSum
            if (m_UseCheckSum_Send)
            {
                if (isReSend)
                {
                    FSPCryptor.ModifyCheckSum(buffer);
                }
                else
                {
                    FSPCryptor.AddCheckSum(buffer);
                }
            }

            if (FSPDebuger.EnableLog)
            {
                Debuger.Log(LOG_TAG_RECV, "SendBuffer() Size={0}, IP={1}, Buffer={2}",
                    buffer.Length, m_HostEndPoint, buffer.ToHexString());
            }

            int sendCnt = 0;

            try
            {
                sendCnt = m_Socket.SendTo(buffer.GetBytes(), buffer.Length, m_HostEndPoint);
            }
            catch (Exception e)
            {
                Debuger.LogWarning(LOG_TAG_SEND, "SendBuffer() " + e);
            }

            if (sendCnt <= 0)
            {
                if (FSPDebuger.EnableLog)
                {
                    Debuger.LogWarning(LOG_TAG_SEND, "SendBuffer() 数据发送失败, IP={0}, Size={1}", m_HostEndPoint,
                        buffer.Length);
                }
            }

            return sendCnt;
        }


        public bool EnableFSPSend
        {
            get { return m_EnableFSPSend; }
            set
            {
                if (m_EnableFSPSend != value)
                {
                    m_EnableFSPSend = value;
                    if (m_IsFSPTimeout && value)
                    {
                        m_CheckFSPTimeoutStartTicks = DateTime.Now.Ticks;
                        m_CheckFSPTimeoutReconnTicks = m_CheckFSPTimeoutStartTicks;
                    }
                }
            }
        }

        public bool SendFSP(short vkey, short arg, short clientFrameId)
        {
            if (m_IsRunning)
            {
                if (m_FSPCodec.WriteToSendQueue(vkey, arg, clientFrameId))
                {
                    if (FSPDebuger.EnableLog)
                    {
                        ushort seq = m_FSPCodec.CurrentSEQ;
                        Debuger.Log(LOG_TAG_MAIN, "SendFSP() vkey = {0}, arg = {1}, seq = {2}", vkey, arg, seq);
                    }

                    lock (this)
                    {
                        Monitor.Pulse(this);
                    }

                    return true;
                }
            }
            return false;
        }

        public bool SendFSP(short vkey, short[] arg, short clientFrameId)
        {
            if (m_IsRunning)
            {
                if (m_FSPCodec.WriteToSendQueue(vkey, arg, clientFrameId))
                {
                    if (FSPDebuger.EnableLog)
                    {
                        ushort seq = m_FSPCodec.CurrentSEQ;
                        Debuger.Log(LOG_TAG_MAIN, "SendFSP() vkey = {0}, arg = {1}, seq = {2}", vkey, arg, seq);
                    }

                    lock (this)
                    {
                        Monitor.Pulse(this);
                    }

                    return true;
                }
            }
            return false;
        }

#endregion


        //------------------------------------------------------------

#region FSP 超时检测
        private static long FSP_TIMEOUT_CHECK_TICKS = 500 * 10000;//0.5秒检测一次
        private static long FSP_TIMEOUT_TICKS = 2000 * 10000;//2秒

        private ushort m_CheckFSPTimeoutSeq = 0;
        private long m_CheckFSPTimeoutStartTicks = 0;//用于2秒触发一次超时逻辑
        private long m_CheckFSPTimeoutLastTicks = 0;// 用于0.5秒检测一次是否超时
        private long m_CheckFSPTimeoutReconnTicks = 0;
        private bool m_IsFSPTimeout = false;
        private bool m_EnableFSPTimeout = true;
        
        private void CheckFSPTimeout()
        {
            //每一次Send之后，都会检测一下。
            if (m_CheckFSPTimeoutSeq == 0)
            {
                m_CheckFSPTimeoutSeq = m_FSPCodec.CurrentSEQ;

                if (m_CheckFSPTimeoutSeq == m_FSPCodec.ConfirmSEQ)
                {
                    m_CheckFSPTimeoutSeq = 0;
                }
                else
                {
                    //启动超时跟踪
                    m_CheckFSPTimeoutStartTicks = DateTime.Now.Ticks;
                    m_CheckFSPTimeoutLastTicks = m_CheckFSPTimeoutStartTicks;
                }
            }
            else
            {
                //如果上面检测到有可能超时，则99MS后再检测一下
                if (LoopNumber.loop_ushort_more_than(m_FSPCodec.ConfirmSEQ + 1, m_CheckFSPTimeoutSeq))
                {
                    //说明需要Check的SEQ已经被确认了
                    m_CheckFSPTimeoutSeq = 0;
                    m_CheckFSPTimeoutStartTicks = 0;
                    m_CheckFSPTimeoutReconnTicks = 0;
                    m_WaitForSendAuth = false;

                    if (m_IsFSPTimeout)
                    {
                        m_IsFSPTimeout = false;
                        Debuger.LogWarning(LOG_TAG, "CheckFSPTimeout() Timeout end!");
                        //超时结束
                        if (m_FSPTimeoutHandler != null)
                        {
                            m_FSPTimeoutHandler(this, -1);
                        }
                    }
                }
                else
                {
                    //那么Check的SEQ还没有被确认，确实超时了至少99MS
                    //这个时候有可能是网络繁忙，并没有物理断线，所以有1秒的缓冲

                    long nowticks = DateTime.Now.Ticks;

                    //每0.5秒检测一次是否真正超时
                    long passticks = nowticks - m_CheckFSPTimeoutLastTicks;
                    if (passticks >= FSP_TIMEOUT_CHECK_TICKS)//
                    {
                        m_CheckFSPTimeoutLastTicks = nowticks;

                        ///追加一个授权验证...
                        m_WaitForSendAuth = true;

                        //每2秒触发一次超时逻辑
                        passticks = nowticks - m_CheckFSPTimeoutStartTicks;
                        int timeout_ms = (int)(passticks / 10000);//超时时长
                        if (passticks >= FSP_TIMEOUT_TICKS)
                        {
                            //触发[第]1次超时
                            if (!m_IsFSPTimeout)
                            {
                                m_IsFSPTimeout = true;

                                Debuger.LogWarning(LOG_TAG,
                                    "CheckFSPTimeout() Timeout begin! CurrentSEQ = {0}, ConfirmSEQ = {1}",
                                    m_FSPCodec.CurrentSEQ, m_FSPCodec.ConfirmSEQ);

                                timeout_ms = 0;
                                m_CheckFSPTimeoutStartTicks = nowticks;
                                m_CheckFSPTimeoutReconnTicks = nowticks;

                                ///追加一个授权验证...
                                m_WaitForSendAuth = true;
                            }
                            else
                            {
                                Debuger.LogWarning(LOG_TAG,
                                    "CheckFSPTimeout() Timeout happen: {0}ms, CurrentSEQ = {1}, ConfirmSEQ = {2}",
                                    timeout_ms, m_FSPCodec.CurrentSEQ, m_FSPCodec.ConfirmSEQ);
                            }

                            //触发超时逻辑
                            if (m_FSPTimeoutHandler != null)
                            {
                                //如果timeout_ms为0，则表示是触发第1次超时。如果非0，则表示已持续进入超时多少MS了
                                m_FSPTimeoutHandler(this, timeout_ms);
                            }
                        }

                        //触发超时了
                        if (m_IsFSPTimeout)
                        {
                            //但不是第1次超时
                            if (nowticks - m_CheckFSPTimeoutReconnTicks >= FSP_TIMEOUT_TICKS)
                            {
                                m_CheckFSPTimeoutReconnTicks = nowticks;
                                m_WaitForReconnect = true;

                                Debuger.LogWarning(LOG_TAG,
                                    "CheckFSPTimeout() Timeout, try reconnect, CurrentSEQ = {1}, ConfirmSEQ = {2}",
                                    timeout_ms, m_FSPCodec.CurrentSEQ, m_FSPCodec.ConfirmSEQ);
                            }
                        }
                        
                    }

                }
            }
        }

        public void ResetFSPTimeout()
        {
            m_CheckFSPTimeoutStartTicks = DateTime.Now.Ticks;
            m_CheckFSPTimeoutReconnTicks = m_CheckFSPTimeoutStartTicks;
        }


        public bool EnableFSPTimeout
        {
            get { return m_EnableFSPTimeout; }
            set {
				if(m_FSPCodec != null)
				{
					m_FSPCodec.EnableTimeOut = value;
				}
				m_EnableFSPTimeout = value; 
			}
        }
#endregion

        public int GetCurrentAuthOffset()
        {
            return m_FSPCodec != null ? m_FSPCodec.GetCurrentAuthOffset() : 0;
        }

        //------------------------------------------------------------
        public void EnterFrame()
        {
            if (!m_IsRunning)
            {
                return;
            }

            if (m_SendIntervalDefault != m_SendIntervalWifi)
            {
                m_UseWifiOptimize = NetCheck.isWifi();
            }

            if (m_WaitForReconnect)
            {
                if (NetCheck.isAvailable())
                {
                    Reconnect();
                }
                else
                {
                    Debuger.Log(LOG_TAG_MAIN, "EnterFrame() 等待重连，但是网络不可用！");
                }
            }

			bool gsdk_need_send_auth = false;
			gsdk_need_send_auth = GSDKManager.Instance.IsNeedSendAuth;
			if (gsdk_need_send_auth)
            {
				Debuger.Log(LOG_TAG_MAIN, "EnterFrame() gsdk_need_send_auth = {0}", gsdk_need_send_auth);
                VerifyAuth();
				GSDKManager.Instance.ResetSendAuth();
            }

            if (m_WaitForSendAuth)
            {
                Debuger.Log(LOG_TAG_MAIN, "EnterFrame() m_CheckFSPTimeoutSeq = {0}", m_CheckFSPTimeoutSeq);
                VerifyAuth();
            }
        }

        //------------------------------------------------------------
#region 处理自定义消息 
        private List<SyncCmd> m_ListCustomCmd_InSend = new List<SyncCmd>();
        private NetBuffer m_CustomSendBuffer = new NetBuffer(512);
        private void HandleCustomMessage(NetBuffer buffer)
        {
            Debuger.Log(LOG_TAG_RECV, "HandleCustomMessage()");

            m_ListCustomCmd_InSend.Clear();

            buffer.Position = 0;
            buffer.Skip(4);
            List<SyncFrame> listFrame = FSPCodec.ReadFrameListFromBufferS2C(buffer);
            
            for (int i = 0; i < listFrame.Count; i++)
            {
                SyncFrame frame = listFrame[i];
                for (int j = 0; j < frame.cmdList.Count; j++)
                {
                    SyncCmd cmd = frame.cmdList[j];
                    HandleCustomCmd(cmd);
                }
            }

            m_CustomSendBuffer.Clear();
            m_CustomSendBuffer.WriteUShort(0);
            m_CustomSendBuffer.WriteUShort(0);
            m_CustomSendBuffer.WriteUShort(m_SessionId);
            FSPCodec.WriteCmdListToBufferC2S(m_ListCustomCmd_InSend, m_CustomSendBuffer);
            SendBuffer(m_CustomSendBuffer, false);
        }

        private void HandleCustomCmd(SyncCmd cmd)
        {

            if (cmd.vkey == VKeyDef.PVP_PING_REAL)
            {
                m_ListCustomCmd_InSend.Add(cmd);
            }
        }



#endregion

        //------------------------------------------------------------
#region DebugUI
        private void OnDbgGUI()
        {
            GUILayout.Label("Use Wifi Optimize:" + m_UseWifiOptimize);

            GUILayout.Label("Default Send Interval:" + m_SendIntervalDefault);
            m_SendIntervalDefault = (int)GUILayout.HorizontalSlider((float)m_SendIntervalDefault, 66, 200);

            GUILayout.Label("Wifi Send Interval:" + m_SendIntervalWifi);
            m_SendIntervalWifi = (int)GUILayout.HorizontalSlider((float)m_SendIntervalWifi, 66, 200);

            bool tmp = GUILayout.Toggle(m_UseEmptyFrameAckAvoid, "空帧免确认");
            if (tmp != m_UseEmptyFrameAckAvoid)
            {
                SetEmptyFrameAckAvoid(tmp);
            }
        }
#endregion
    }
}
