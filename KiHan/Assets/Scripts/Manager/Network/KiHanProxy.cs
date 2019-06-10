using System;
using System.IO;
using ProtoBuf;
using UnityEngine;
using System.Collections.Generic;
using KH.Lua;

namespace KH.Network
{
    public class KiHanProxy : IProxy
    {
        /// <summary>
        /// 连接类型TYPE
        /// </summary>
        static public Dictionary<int, Type> CONNECTION_CLS_TYPES = new Dictionary<int, Type>();


        private IConnection _defaultConn;
        private MessageTypeProvider _typeProvider;
#if UNITY_EDITOR && KHNET_DEBUG
        private MessageTypeProvider _typeProvider_D;
#endif
        private MessageRouter _router = null;
        private NetworkMessageQueue _messageQueue = null;
             
        /// <summary>
        /// 当前选择的Zone ID
        /// </summary>
        private int _zoneId;
        private ClientHead _recentHead;
        private ClientHead _send_head = new ClientHead();

        private uint _messageId = 0;

        // 统计使用的消息钩子
        private IConnectionHook _hook;


        private Dictionary<int, IConnection> _connectionDict;
        private List<IConnection> _connectionsArr;
        private int _next_connID = 0;

        public static bool bSimulationLossNetworkMessage = false;

        public KiHanProxy()
        {
            _connectionDict = new Dictionary<int, IConnection>();
            _connectionsArr = new List<IConnection>();

            this._typeProvider = new MessageTypeProvider();
#if UNITY_EDITOR && KHNET_DEBUG
            this._typeProvider_D = new MessageTypeProvider();
#endif 
            this._router = new MessageRouter();
            this._messageQueue = new NetworkMessageQueue(_router);

            ///代理..
            __Proxy.__DistributeMessage = _messageQueue.Distribute;
            __Proxy.__QueueAddMessage = _messageQueue.AddMessage;
            

            this._defaultConn = null;
            this._send_head.MagicNum = (ushort)MagicNum;
            this._send_head.Format = ClientHeadFormatType.Protobuf;
        }

        public void Update()
        {
            ///接收信息
            for (int i = 0; i < _connectionsArr.Count; i++)
            {
                _connectionsArr[i].DoReceive();
            }
            
            ///信息分发...
            _messageQueue.Distribute();
            ///超时检测
            _router.OnTimeoutCheck();
        }

        public MessageRouter Router
        {
            get
            {
                return _router;
            }
        }

        public NetworkMessageQueue MsgQueue
        {
            get
            {
                return _messageQueue;
            }
        }

        public int ZoneId
        {
            get
            {
                return _zoneId;
            }
            set
            {
                _zoneId = value;
            }
        }

        public ushort MagicNum
        {
            get
            {
                return 0xABAB;
            }
        }


        public IConnectionHook connectionHook
        {
            get
            {
                return _hook;
            }
            set
            {
                _hook = value;
            }
        }

        public IConnection Connection
        {
            get
            {
                if(_defaultConn==null) _defaultConn = new ApolloConnection();
                return _defaultConn;
            }
            set
            {
                _defaultConn = value;
            }
        }

        private void _OnConnConnected(IConnection connection)
        {
            if (!_connectionsArr.Contains(connection))
            {
                _connectionsArr.Add(connection);
            }
        }

        private void _OnConnDisconnected(IConnection connection)
        {
            ///连接断开了...
            if (_connectionsArr.Contains(connection))
            {
                _connectionsArr.Remove(connection);
            }
            ///残留的信息分发下 此处分发残留信息已经迟了，
            ///提前到连接自身
            ///_messageQueue.Distribute();
            ///清空并回调断开连接的超时回调
            _router.ClearUnicastCallbacks(true, connection.ID);
        }

        /// <summary>
        /// 设置默认连接
        /// </summary>
        /// <param name="conn"></param>
        public void setDefaultConnection(IConnection conn)
        {
            _defaultConn = conn;
            _router.defaultConnID = (_defaultConn == null ? -1 : _defaultConn.ID);
        }

        public IConnection CreateConnection(int connID = -1 ,string url = "" , bool isDefault = true ,int classType = ConnectionClassType.APOLLO_CONNECTION_TYPE, bool autoDis = true)
        {
            IConnection conn = null;

            if (connID != -1)
            {
                conn = GetConnection(connID);
            }
            
            if (conn == null)
            {
                Type connType = null;

                CONNECTION_CLS_TYPES.TryGetValue(classType, out connType);
                conn = System.Activator.CreateInstance(connType) as IConnection;

                conn.OnConnectedCallback += _OnConnConnected;
                conn.OnDisconnectedCallback += _OnConnDisconnected;
                conn.OnDatagramArrive += this.OnConnRead;

                ////_next_connID暂没有用到的地方, 注掉
                //_next_connID++;

                conn.ID = connID;

                _connectionDict.Add(connID, conn);
            }
            else if(autoDis)
            {
                conn.DisconnectError = 0;
                Disconnect(conn);
            }

            ///设置连接路径..
            conn.URL = (string.IsNullOrEmpty(url) ? DefineExt.EPConfig.Url : url);
            ///默认连接
            if (isDefault)
            {
                setDefaultConnection(conn);
            }

            return conn;
        }

        public void DeleteConnection(int connID)
        {
            IConnection conn = Disconnect(connID);

            if (conn != null)
            {
                ///从字曲中删除
                _connectionDict.Remove(connID);

                conn.OnConnectedCallback -= _OnConnConnected;
                conn.OnDisconnectedCallback -= _OnConnDisconnected;
                conn.OnDatagramArrive -= this.OnConnRead;

                ///如果还在收听中从收听中删除
                if (_connectionsArr.Contains(conn))
                {
                    _connectionsArr.Remove(conn);
                    ///清空回调且不需要调用
                    _router.ClearUnicastCallbacks(false , connID);
                }

                if (_defaultConn == conn)
                {
                    setDefaultConnection(null);
                }

                conn.Dispose();
            }
        }

        public IConnection GetConnection(int connID)
        {
            IConnection conn = null;
            _connectionDict.TryGetValue(connID, out conn);
            return conn;
        }

        public IConnection Disconnect(int connID = - 1)
        {
            IConnection conn = GetConnection(connID);
            return Disconnect(conn);
        }

        public IConnection Disconnect(IConnection conn)
        {
            if (conn != null)
            {
                conn.Disconnect();
            }
            return conn;
        }

        public MessageTypeProvider TypeProvider
        {
            get
            {
                return _typeProvider;
            }
            set
            {
                _typeProvider = value;
            }
        }

#if UNITY_EDITOR && KHNET_DEBUG
        public MessageTypeProvider TypeProvider_D
        {
            get
            {
                return _typeProvider_D;
            }
            set
            {
                _typeProvider_D = value;
            }
        }
#endif

        private MemoryStream sendMessageBodyStream = new MemoryStream();
        public MemoryStream SendMessageBodyStream
        {
            get
            {
                return sendMessageBodyStream;
            }
        }

        // 清理消息流和消息体流的状态
        private void clearStreams()
        {
            sendMessageBodyStream.Position = 0;
            sendMessageBodyStream.SetLength(0);
        }

        public bool Write(uint cmdId
            , object message
            , uint serialNumber = 0
            , int connID = -1)
        {
            bool result = false;

            IConnection conn = _defaultConn;
            if (connID != -1)
            {
                _connectionDict.TryGetValue(connID, out conn);
            }

            ///连接不为空或者尝试可以发送...
            if (conn != null && conn.TryDoSend(cmdId))
            {
                clearStreams();

                _send_head.CmdId = cmdId;
                _send_head.ZoneId = ZoneId;
                _send_head.MessageId = _messageId++;

                if (connectionHook != null
                    && connectionHook.FilterCmd(cmdId, message, _messageId - 1, serialNumber))
                {
                    connectionHook.HookMessage(_send_head, message, WriteMSG);
                    return true;
                }

                if (message is byte[])
                {
                    byte[] buff = message as byte[];

                    if (buff != null)
                    {
                        sendMessageBodyStream.Write(buff, 0, buff.Length);
                    }
                }
                else
                {
                    ///序列化message且设置长度
                    Serializer.NonGeneric.Serialize(sendMessageBodyStream, message);

                    if ((DefineExt.Net_Log_Level & DefineExt.LOG_FULL_SEND_PROTO_INFO) > 0)
                    {
                        Debuger.Log(String.Format("Send ----> cmd={0}, serial={1}, proto={2}", cmdId, serialNumber, KHUtil.GetProtoStr(message, message.GetType().FullName)));
                    }
                }
                _send_head.BodyLength = (uint)sendMessageBodyStream.Length;
                _send_head.Serial = serialNumber;

                MessageManager msgManager = MessageManager.Instance;
                // 模拟服务器，从本地读包
                if (!msgManager.IsDeserializeFromLocal)
                {
                    result = conn.DoSend(_send_head, message, sendMessageBodyStream);
                }
                if (!result) _messageId--;

            }

            return result;
        }

        private bool WriteMSG(uint cmdId, object message, uint serialNumber = 0, int connID = -1)
        {
            bool result = false;

            IConnection conn = _defaultConn;

            if (connID != -1)
            {
                _connectionDict.TryGetValue(connID, out conn);
            }

            if (conn != null && conn.IsConnected)
            {
                clearStreams();

                Serializer.NonGeneric.Serialize(sendMessageBodyStream, message);

                _send_head.CmdId = cmdId;
                _send_head.BodyLength = (uint)sendMessageBodyStream.Length;
                _send_head.Serial = serialNumber;

                result = conn.DoSend(_send_head, message, sendMessageBodyStream);
            }


            return result;
        }

        protected byte[] HeadBuffer = new byte[ClientHead.HeadLength];
        protected MemoryStream ReceiveMessageStream = new MemoryStream();

        public void OnConnRead(MemoryStream receiveStream)
        {
            List<object> message = new List<object>();
            while (Read(receiveStream, out message)) 
            {
                // 测试要求加的工具，模拟丢包
                if (bSimulationLossNetworkMessage)
                    continue;

                // 分发包
                _messageQueue.AddMessage(_recentHead.CmdId
                    , _recentHead.Serial
                    , message);
            };
        }

        protected bool Read(MemoryStream stream, out List<object> message)
        {
            if(stream.Length > stream.Position)
            {
                _recentHead = ReadHead(stream);
                message = ReadBody(_recentHead, stream);

                return true;
            }
            else
            {
                message = new List<object>();
                return false;
            }
        }

        protected ClientHead ReadHead(MemoryStream stream)
        {
            stream.Read(HeadBuffer, 0, ClientHead.HeadLength);
            return ClientHead.ConvertFromByte(HeadBuffer);
        }

        protected List<object> ReadBody(ClientHead head, MemoryStream stream)
        {
            ///此处对读取 BODYSTEAM做了一些优化，如果有问题请注意排查 2015 08 21
            //byte[] bodyBuffer = new byte[head.BodyLength];
            //int bodyBufferOffset = 0;
            //stream.Read(bodyBuffer, 0, (int)head.BodyLength);

            byte[] bodyBuffer = stream.GetBuffer();

            int bodyBufferOffset = (int) stream.Position;

            int bodyLen = (int)head.BodyLength;
            if (bodyLen < 0) bodyLen = 0;

            stream.Position = bodyBufferOffset + bodyLen;

            ///bodylen大于零才去读MESSAGE

            //if (bodyLen > 0) //去掉吧，有些协议的长度就是为零
            {
                List<object> msgs = new List<object>();
                if (head.Format == ClientHeadFormatType.Protobuf)
                {
                    if (__Proxy.__LuaPBProcessor.ContainsKey(head.CmdId) && _router.getPBType(head.Serial) == PBTYPE.LuaPB || _router.getPBType(head.Serial) == PBTYPE.Both)
                    {
                        byte[] buff = new byte[bodyLen];
                        //stream.Write(buff, bodyBufferOffset, bodyLen);
                        stream.Position = bodyBufferOffset;
                        stream.Read(buff, 0, bodyLen);
                        stream.Position = bodyBufferOffset + bodyLen;
                        msgs.Add(buff);
                        //return buff;
                    }
                    if (_typeProvider.ContainsKey(head.CmdId) && _router.getPBType(head.Serial) == PBTYPE.CSharpPB || _router.getPBType(head.Serial) == PBTYPE.Both)
                    {
                        ReceiveMessageStream.Write(bodyBuffer, bodyBufferOffset, bodyLen);
                        ReceiveMessageStream.Position = 0;

                        Type messageType;
                        TypeProvider.TryGetValue(head.CmdId, out messageType);

                        if (DefineExt.Net_Log_Level > 0)
                        {
                            Debuger.Log(string.Format("Received Package 0x{0}({4}), Body Length = {1}, Message Id = {2}, Serial Id = {3}.", _recentHead.CmdId.ToString("X2"), _recentHead.BodyLength.ToString(), _recentHead.MessageId, _recentHead.Serial, _recentHead.CmdId));
                            ///解析详细的字段
                            ///todo..
                        }

                        if (messageType == null)
                        {
                            ReceiveMessageStream.SetLength(0);
                            if (_router.getPBType(head.Serial) == PBTYPE.CSharpPB)
                            {
                                Debuger.LogWarning(@"返回cmdID消息类型未注册。请在NetworkManager.registerTypes()方法中添加cmdID到返回消息类型的映射。
        CmdId = 0x" + _recentHead.CmdId.ToString("X2") + "(" + _recentHead.CmdId + ")");
                                //return null;
                            }
                        }
                        else
                        {
                            object message = null;
                            message = Serializer.NonGeneric.Deserialize(messageType, ReceiveMessageStream);
                            ReceiveMessageStream.SetLength(0);

                            if ((DefineExt.Net_Log_Level & DefineExt.LOG_FULL_RECV_PROTO_INFO) > 0)
                            {
                                Debuger.Log(String.Format("Receive ----> cmd={0}, serial={1}, proto={2}", head.CmdId, head.Serial, KHUtil.GetProtoStr(message, messageType.FullName)));
                            }

                            msgs.Add(message);
                        }

                        // Update by Chicheng
                        MessageManager msgManager = MessageManager.Instance;
                        if (msgManager.IsSerializeToLocal)
                        {
                            Debug.LogWarning("把读到的消息包序列化到本地");
                            RemoteModel remoteModel = RemoteModel.Instance;
                            msgManager.serializeToLocal(msgs, messageType, head.CmdId, remoteModel.CurrentTime, head.Serial);
                            if (head.Serial == 0)
                            {
                                Debug.LogWarning("这个消息包是NTF");
                            }
                            Debug.Log("message type:" + messageType.ToString());
                            Debug.Log("cmdID: " + head.CmdId.ToString());
                        }
                    }
                }
                return msgs;
            }
        }

        public void Dispose()
        {
            sendMessageBodyStream.Dispose();
            ReceiveMessageStream.Dispose();
        }
    }
}
