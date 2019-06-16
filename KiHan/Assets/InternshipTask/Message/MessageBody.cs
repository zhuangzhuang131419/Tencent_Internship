using UnityEngine;
using KH;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;

namespace KH
{
    /// <summary>
    /// 保存在本地的包
    /// </summary>
    [Serializable]
    public class MessageBody
    {
        private static readonly int MESSAGEHEADLENGTH = 4;
        private uint cmdId;

        // 不同type的message对应不同反序列化方法
        private Type messageType;
        // 已经被序列化成byte字符串的消息包
        private List<byte[]> messageBodyBuffer = new List<byte[]>();
        private ulong timeStamp;
        private uint serial;

        public MessageBody()
        {

        }

        public MessageBody(Type messageType, byte[] messageBodyBuffer, uint cmdID)
        {
            this.messageType = messageType;
            this.messageBodyBuffer.Add(messageBodyBuffer);
            this.cmdId = cmdID;
        }

        public MessageBody(Type messageType, uint cmdID)
        {
            this.messageType = messageType;
            this.cmdId = cmdID;
        }

        public MessageBody(Type messageType, uint cmdID, ulong timeStamp, uint serial)
        {
            this.messageType = messageType;
            this.cmdId = cmdID;
            this.timeStamp = timeStamp;
            this.serial = serial;
        }


        public static int MessageHeadLength
        {
            get { return MESSAGEHEADLENGTH; }
        }

        public Type MessageType
        {
            get { return messageType; }
            set { MessageType = value; }
        }

        public List<byte[]> MessagesBodyBuffer
        {
            get { return messageBodyBuffer; }
            set { messageBodyBuffer = value; }
        }

        public uint CmdID
        {
            get { return cmdId; }
        }

        public ulong TimeStamp
        {
            get { return timeStamp; }
        }

        public uint Serial
        {
            get { return serial; }
        }
    }
}
