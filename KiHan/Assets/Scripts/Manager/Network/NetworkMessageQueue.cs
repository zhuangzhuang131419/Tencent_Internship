using System.Collections.Generic;
using UnityEngine;

namespace KH
{
    public class UnroutedMessage
    {
        public uint cmdId;
        public uint serial;
        public object csharpmessage;
        public object luaMessage;
    }

    public class NetworkMessageQueue
    {
        private List<UnroutedMessage> _messages = new List<UnroutedMessage>();
        private int _msgSize = 0;
        private int _msgIndex = 0;
        private KH.Network.MessageRouter _msgRouter = null;

        public NetworkMessageQueue(KH.Network.MessageRouter router)
        {
            this._msgRouter = router;
        }

        public void AddMessage(uint cmdId, uint serial, List<object> message)
        {
            _msgSize++;

            UnroutedMessage routeMessage = null;

            if (_messages.Count < _msgSize)
            {
                _messages.Add(routeMessage = new UnroutedMessage());
            }
            else
            {
                int index = _msgSize - 1;
                routeMessage = _messages[index];
            }

            routeMessage.cmdId = cmdId;
            routeMessage.serial = serial;
            //if (message.Count == 1)
            //{
            //    if(message[0] is Object)
            //        routeMessage.csharpmessage = message[0];
            //    else
            //        routeMessage.luaMessage = message[0];
            //}
            //else if(message.Count == 2)
            //{
            //    if (message[0] is Object)
            //    {
            //        routeMessage.csharpmessage = message[0];
            //        routeMessage.luaMessage = message[1];
            //    }
            //    else
            //    {
            //        routeMessage.csharpmessage = message[1];
            //        routeMessage.luaMessage = message[0];
            //    }
            //}
            //else
            //{
            //    Debuger.LogError("收发包msg list 有bug");
            //}

            for (int i = 0; i != message.Count; i++)
            {
                var msg = message[i];
                if (msg is byte[])
                    routeMessage.luaMessage = msg;
                else
                    routeMessage.csharpmessage = msg;
            }

        }

        public bool BlockDispatch = false;
        public void Distribute()
        {
            ///_Instance.DoReceive();

            if (_msgSize > _msgIndex)
            {
                while (_msgSize > _msgIndex)
                {
                    UnroutedMessage message = _messages[_msgIndex];
                    _msgIndex++;

                    object csharpmsg = message.csharpmessage;
                    object luamsg = message.luaMessage;
                    
                    ///清空数据
                    message.luaMessage = null;
                    message.csharpmessage = null;

                    if (_msgRouter != null && !BlockDispatch)
                    {
                        _msgRouter.Route(message.cmdId, message.serial, csharpmsg, luamsg);
                    }
                }

                _msgIndex = 0;
                _msgSize = 0;
            }
        }
    }
}
