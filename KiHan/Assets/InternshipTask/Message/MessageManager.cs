using UnityEngine;
using System.IO;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System;


namespace KH
{
    public class MessageManager
    {
        static private MessageManager messageManagerInstance = null;
        private static readonly string DEST_PATH = "Assets/InternshipTask/";
        public static readonly string DEST_PATH_CSharp = DEST_PATH + "Message.dat";
        // public static readonly string BATTLE_RESULT = "F:\\Tencent_Internship\\KiHan\\log\\Result.dat";
        // 内存中保存的一系列message，方便在给定cmdID的情况下取出 （利用cmdID作key，避免重复）
        public Dictionary<uint, List<MessageBody>> messagesBodySet = null;

        // 录播
        private bool isSerializeToLocal = false;
        private bool isActivate = false;

        static public MessageManager Instance
        {
            get
            {
                if (messageManagerInstance == null)
                {
                    messageManagerInstance = new MessageManager();
                }
                return messageManagerInstance;
            }
        }

        public bool IsSerializeToLocal
        {
            get { return isSerializeToLocal; }
            set { isSerializeToLocal = value; }
        }

        public bool IsActivate
        {
            get { return isActivate; }
            set { isActivate = value; }
        }

        private MessageManager() { }

        /// <summary>
        /// 把读到的消息包序列化到本地
        /// </summary>
        /// <param name="msgs"></param>
        /// <param name="messageType"></param>
        public void serializeToLocalWithType(List<object> msgs, Type messageType, uint cmdID, ulong timeStamp, uint serial, MessageSource source)
        {
            MessageBody packedMessageBody = new MessageBody(messageType, cmdID, timeStamp, serial, source);
            try
            {
                switch (source)
                {
                    case MessageSource.Lua:

                        if (msgs[0] is byte[])
                        {
                            packedMessageBody.MessagesBodyBuffer.Add((byte[])msgs[0]);
                        }
                        break;
                    case MessageSource.CSharp:
                        try
                        {
                            PBSerializer.NDeserialize(PBSerializer.NSerialize(msgs[0]), messageType);
                        }
                        catch (Exception)
                        {
                            Debug.LogWarning(cmdID + "NTF测试未通过");
                            break;
                        }
                        packedMessageBody.MessagesBodyBuffer.Add(PBSerializer.NSerialize(msgs[0]));

                        break;
                    case MessageSource.CSharpAndLua:

                        foreach (var item in msgs)
                        {
                            if (item is byte[])
                            {
                                packedMessageBody.MessagesBodyBuffer.Add((byte[])item);
                            }
                            else
                            {
                                packedMessageBody.MessagesBodyBuffer.Add(PBSerializer.NSerialize(item));
                            }
                        }
                        break;
                }
            }
            catch
            {
                Debug.LogError("序列化出现问题");
                Debug.LogError("消息数量：" + msgs.Count + " ；序列号：" + serial);
            }
            serializeToLocal(packedMessageBody, DEST_PATH_CSharp);
        }

        /// <summary>
        /// 把服务器接收来的消息包序列化到本地
        /// </summary>
        /// <param name="destinationPath">要存放的地址</param>
        public void serializeToLocal<T>(T messageBody, string destinationPath)
        {
            // 要把type也序列化进去
            FileStream fileStream = null;

            try
            {
                fileStream = new FileStream(destinationPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            }
            catch (DirectoryNotFoundException)
            {
                Debug.LogWarning("Directory not find");
            }
            catch (IOException)
            {
                Debug.LogWarning("serializeToLocal: IOException");
            }


            if (fileStream != null)
            {
                long originalFileStreamLength = fileStream.Length;
                long beginPosition = fileStream.Seek(0, SeekOrigin.End);
                BinaryFormatter bf = new BinaryFormatter();
                // 初次序列化，获取数组的长度
                try
                {
                    bf.Serialize(fileStream, messageBody);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("序列化出错");
                    Debug.LogWarning(e.Message);
                }

                
                long messageBodyLength = fileStream.Length - originalFileStreamLength;

                byte[] messageBodyBuffer = new byte[messageBodyLength];
                fileStream.Position = beginPosition;
                fileStream.Read(messageBodyBuffer, 0, (int)messageBodyLength);
                Debug.Log("messageBody序列化成功! 文件大小: " + fileStream.Length);
                if (messageBodyLength > 0)
                {
                    fileStream.Position = beginPosition;
                    // 写入message head （因为这一步会把之前写的messagebody覆盖了，所以重新写入）
                    fileStream.Write(BitConverter.GetBytes(messageBodyLength), 0, MessageBody.MessageHeadLength);
                    fileStream.Write(messageBodyBuffer, 0, (int)messageBodyLength);
                    Debug.Log("messageHead写入成功! 文件大小: " + fileStream.Length);
                }
                else
                {
                    Debug.LogWarning("message body < 0");
                }
                fileStream.Close();
            }
            Debug.Log("file stream has already been closed.");
        }


        /// <summary>
        /// 根据cmdID从本地读取包，仅在mock server下使用
        /// </summary>
        /// <param name="cmdID"></param>
        public List<object> ReadMessageFromLocal(uint cmdID)
        {
            //从这个地址固定读取
            MessageBody packedMessage01 = deserializeFromLocalByCmdIDCache(cmdID);
            if (packedMessage01 != null)
            {
                // Debug.Log("利用cmdID成功读取");
                List<object> messageBodyResult = new List<object>();
                try
                {

                    switch (packedMessage01.Source)
                    {
                        case MessageSource.Lua:
                            messageBodyResult.Add(packedMessage01.MessagesBodyBuffer[0]);
                            break;
                        case MessageSource.CSharp:
                            messageBodyResult.Add(PBSerializer.NDeserialize(packedMessage01.MessagesBodyBuffer[0], packedMessage01.MessageType));
                            break;
                        case MessageSource.CSharpAndLua:
                            messageBodyResult.Add(packedMessage01.MessagesBodyBuffer[0]);
                            messageBodyResult.Add(PBSerializer.NDeserialize(packedMessage01.MessagesBodyBuffer[1], packedMessage01.MessageType));
                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("MessageManager" + e.Message);
                }

                // 成功解析出message
                return messageBodyResult;
            }
            else
            {
                if (cmdID != 50397186) // 过滤掉心跳包
                {
                    
                }
                Debug.LogWarning(cmdID + "没有对应的message");
                return null;
            }
        }

        /// <summary>
        /// 把本地的包反序列后取出
        /// </summary>
        /// <param name="messageBuffer"></param>
        /// <param name="count">取出第count个message</param>
        /// <returns>消息主体</returns>
        public T deserializeFromLocal<T>(string sourcePath, int count = 0)
        {
            T message = default(T);
            FileStream fileStream = null;
            BinaryFormatter bf = new BinaryFormatter();
            try
            {
                fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (DirectoryNotFoundException)
            {
                Debug.LogWarning("读取路径出现问题");
            }
            catch (FileNotFoundException)
            {
                Debug.LogWarning("无法找到文件");
            }

            if (fileStream != null)
            {
                byte[] messageHeadBuffer = new byte[MessageBody.MessageHeadLength];
                byte[] messageBodyBuffer;
                int messageBodyLength = 0;
                // 开始读取
                while (count > 0)
                {
                    // 读取message head来获得message body的长度
                    if (fileStream.Read(messageHeadBuffer, 0, MessageBody.MessageHeadLength) == MessageBody.MessageHeadLength)
                    {
                        messageBodyLength = BitConverter.ToInt32(messageHeadBuffer, 0);
                        // 跳过message body
                        fileStream.Seek(messageBodyLength, SeekOrigin.Current);
                        count--;
                    }
                    else
                    {
                        Debug.LogWarning("超出message包的范围");
                        return default(T);
                    }
                }

                try
                {
                    // 当前位置是我们所要的message
                    // 读取message head来获得message body的长度
                    long beginPosition = fileStream.Position;
                    if (fileStream.Read(messageHeadBuffer, 0, MessageBody.MessageHeadLength) != MessageBody.MessageHeadLength)
                    {
                        return default(T);
                    }
                    messageBodyLength = BitConverter.ToInt32(messageHeadBuffer, 0);
                    messageBodyBuffer = new byte[messageBodyLength];
                    fileStream.Read(messageBodyBuffer, 0, messageBodyLength);

                    using (MemoryStream mStream = new MemoryStream())
                    {
                        mStream.Write(messageBodyBuffer, 0, messageBodyLength);
                        mStream.Flush();
                        mStream.Seek(0, SeekOrigin.Begin);
                        try
                        {
                            message = (T)bf.Deserialize(mStream);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("反序列化战斗包出错");
                            Debug.LogWarning(e.Message);
                        }
                        
                        mStream.Close();
                    }

                    long endPosition = fileStream.Position;

                    // 读完这个包后要把这段数据去掉(beginPosition 到 endPosition 之间的)
                    // deleteBuffer(fileStream, beginPosition, endPosition);

                }
                catch (IOException)
                {
                    Debug.LogWarning("IOException");
                }
            }
            fileStream.Close();
            return message;
        }

        /// <summary>
        /// 根据提供的cmdID序号获得相对应的包
        /// </summary>
        /// <param name="cmdID"></param>
        /// <returns>对应的包</returns>
        public MessageBody deserializeFromLocalByCmdIDCache(uint cmdID)
        {
            if (messagesBodySet == null)
            {
                loadFromLocalDisk();
                // 直接从提前存好的内存中读取
            }
            return getMessageBodyByCmdIDFromResults(cmdID);
        }

        public MessageBody deserializeFromLocalByCmdID(uint cmdID)
        {
            // 将本地文件读进内存
            MessageBody tempMessageBody = null;
            MessageBody resultMessageBody = null;
            FileStream fileStream = null;
            BinaryFormatter bf = new BinaryFormatter();
            try
            {
                fileStream = new FileStream(DEST_PATH_CSharp, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (DirectoryNotFoundException)
            {
                Debug.LogWarning("读取路径出现问题");
            }
            catch (FileNotFoundException)
            {
                Debug.LogWarning("无法找到文件");
            }

            if (fileStream != null)
            {
                byte[] messageHeadBuffer = new byte[MessageBody.MessageHeadLength];
                byte[] messageBodyBuffer;
                int messageBodyLength;
                // 开始读取
                do
                {
                    // 读取message head来获得message body的长度
                    long beginPosition = fileStream.Position;
                    if (fileStream.Read(messageHeadBuffer, 0, MessageBody.MessageHeadLength) == MessageBody.MessageHeadLength)
                    {
                        messageBodyLength = BitConverter.ToInt32(messageHeadBuffer, 0);
                        // Debug.Log("message body length: " + messageBodyLength);
                        messageBodyBuffer = new byte[messageBodyLength];

                        if (fileStream.Read(messageBodyBuffer, 0, messageBodyLength) == messageBodyLength)
                        {
                            // 通过获得的message body长度来获取message body的buffer
                            long endPosition = fileStream.Position;
                            using (MemoryStream mStream = new MemoryStream())
                            {
                                mStream.Write(messageBodyBuffer, 0, messageBodyLength);
                                mStream.Flush();
                                mStream.Seek(0, SeekOrigin.Begin);
                                tempMessageBody = (MessageBody)bf.Deserialize(mStream);
                                mStream.Close();
                            }

                            if (tempMessageBody.CmdID == cmdID)
                            {
                                // deleteBuffer(fileStream, beginPosition, endPosition);
                                resultMessageBody = tempMessageBody;
                                Debug.Log("读取成功");
                                return resultMessageBody;
                            }
                        }
                        else
                        {
                            Debug.LogWarning("CmdID::超出message包的范围");
                            fileStream.Close();
                            return null;
                        }
                    }
                    else
                    {
                        if (resultMessageBody == null)
                        {
                            Debug.LogWarning("CmdID::超出message包的范围");
                        }
                        fileStream.Close();
                        return resultMessageBody;
                    }
                } while (true);
            }
            else
            {
                Debug.LogWarning("file stream == null");
                return null;
            }
        }

        public MessageBody deserializeFromLocalByTimeStamp(ulong timeStamp)
        {
            if (messagesBodySet == null)
            {
                loadFromLocalDisk();
            }
            // 直接从提前存好的内存中读取
            return getMessageBodyByTimeStampFromResults(timeStamp);
        }

        private MessageBody getMessageBodyByCmdIDFromResults(uint cmdID)
        {
            if (messagesBodySet.ContainsKey(cmdID))
            {
                if (messagesBodySet[cmdID].Count > 0)
                {
                    MessageBody result = messagesBodySet[cmdID][0];
                    messagesBodySet[cmdID].RemoveAt(0);
                    return result;
                }
            }
            UIAPI.ShowMsgTip("不存在" + cmdID);
            return null;
        }

        private MessageBody getMessageBodyByTimeStampFromResults(ulong timeStamp)
        {
            foreach (var messageBodyList in messagesBodySet.Values)
            {
                foreach (var item in messageBodyList)
                {
                    if (item.TimeStamp == timeStamp && item.Serial == 0)
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// // 将本地文件读进内存
        /// </summary>
        private void loadFromLocalDisk()
        {
            if (messagesBodySet != null) return;
            messagesBodySet = new Dictionary<uint, List<MessageBody>>();
            // 将本地文件读进内存
            MessageBody tempMessageBody = null;
            FileStream fileStream = null;
            BinaryFormatter bf = new BinaryFormatter();
            try
            {
                fileStream = new FileStream(DEST_PATH_CSharp, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (DirectoryNotFoundException)
            {
                Debug.LogWarning("读取路径出现问题");
            }
            catch (FileNotFoundException)
            {
                Debug.LogWarning("无法找到文件");
            }

            if (fileStream == null)
            {
                Debug.LogWarning("fileStream == null");
                return;
            }

            byte[] messageHeadBuffer = new byte[MessageBody.MessageHeadLength];
            byte[] messageBodyBuffer;
            int messageBodyLength;
            // 开始读取
            do
            {
                // 读取message head来获得message body的长度
                if (fileStream.Read(messageHeadBuffer, 0, MessageBody.MessageHeadLength) == MessageBody.MessageHeadLength)
                {
                    messageBodyLength = BitConverter.ToInt32(messageHeadBuffer, 0);
                    // Debug.Log("message body length: " + messageBodyLength);
                    messageBodyBuffer = new byte[messageBodyLength];

                    if (fileStream.Read(messageBodyBuffer, 0, messageBodyLength) == messageBodyLength)
                    {
                        // 通过获得的message body长度来获取message body的buffer
                        using (MemoryStream mStream = new MemoryStream())
                        {
                            mStream.Write(messageBodyBuffer, 0, messageBodyLength);
                            mStream.Seek(0, SeekOrigin.Begin);
                            tempMessageBody = (MessageBody)bf.Deserialize(mStream);
                            mStream.Close();
                        }
                        if (!messagesBodySet.ContainsKey(tempMessageBody.CmdID))
                        {
                            messagesBodySet[tempMessageBody.CmdID] = new List<MessageBody>();
                            
                        }
                        messagesBodySet[tempMessageBody.CmdID].Add(tempMessageBody);
                    }
                    else
                    {
                        Debug.LogWarning("CmdID::超出message包的范围");
                        Debug.Log("一共有" + messagesBodySet.Count + "消息包");
                        fileStream.Close();
                        return;
                    }
                }
                else
                {
                    fileStream.Close();
                    Debug.Log("一共有" + messagesBodySet.Count + "消息包");
                    return;
                }
            } while (true);
        }
    }
}

