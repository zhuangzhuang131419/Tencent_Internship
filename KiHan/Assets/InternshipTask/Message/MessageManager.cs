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
        public static readonly string DEST_PATH_MOUSE_EVENT = DEST_PATH + "mouse.dat";
        public static readonly string DEST_PATH_DRAG_EVENT = DEST_PATH + "DragEvent.dat";
        // public static readonly string BATTLE_RESULT = "F:\\Tencent_Internship\\KiHan\\log\\Result.dat";
        // 内存中保存的一系列message，方便在给定cmdID的情况下取出 （利用cmdID作key，避免重复）
        public List<Message> messages = new List<Message>();
        public Dictionary<uint, List<NetworkMessage>> messagesBodySet = null;

        // 已加载的message地址
        private HashSet<string> paths = new HashSet<string>();


        public bool serializeDragEvent = false;

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
        /// 把服务器接收来的消息包序列化到本地
        /// </summary>
        /// <param name="destinationPath">要存放的地址</param>
        public void serializeToLocal(Message messageBody, string destinationPath)
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
                    // object temp = bf.Deserialize(fileStream);
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
                // Debug.Log("messageBody序列化成功! 文件大小: " + fileStream.Length);
                if (messageBodyLength > 0)
                {
                    fileStream.Position = beginPosition;
                    // 写入message head （因为这一步会把之前写的messagebody覆盖了，所以重新写入）
                    fileStream.Write(BitConverter.GetBytes(messageBodyLength), 0, NetworkMessage.MessageHeadLength);
                    fileStream.Write(messageBodyBuffer, 0, (int)messageBodyLength);
                    // Debug.Log("messageHead写入成功! 文件大小: " + fileStream.Length);
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
        public List<object> deserializeFromLocal(uint cmdID)
        {
            //从这个地址固定读取
            NetworkMessage packedMessage01 = deserializeFromLocalByCmdIDCache(MessageManager.DEST_PATH_CSharp, cmdID);
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
            return null;
        }

        /// <summary>
        /// 把本地的包反序列后取出
        /// </summary>
        /// <param name="messageBuffer"></param>
        /// <param name="count">取出第count个message</param>
        /// <returns>消息主体</returns>
        public Message deserializeFromLocal(string sourcePath, int count = 0)
        {
            Message message = new Message();
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
                byte[] messageHeadBuffer = new byte[NetworkMessage.MessageHeadLength];
                byte[] messageBodyBuffer;
                int messageBodyLength = 0;
                // 开始读取
                while (count > 0)
                {
                    // 读取message head来获得message body的长度
                    if (fileStream.Read(messageHeadBuffer, 0, NetworkMessage.MessageHeadLength) == NetworkMessage.MessageHeadLength)
                    {
                        messageBodyLength = BitConverter.ToInt32(messageHeadBuffer, 0);
                        // 跳过message body
                        fileStream.Seek(messageBodyLength, SeekOrigin.Current);
                        count--;
                    }
                    else
                    {
                        Debug.LogWarning("超出message包的范围");
                        return null;
                    }
                }

                try
                {
                    // 当前位置是我们所要的message
                    // 读取message head来获得message body的长度
                    if (fileStream.Read(messageHeadBuffer, 0, NetworkMessage.MessageHeadLength) != NetworkMessage.MessageHeadLength)
                    {
                        return null;
                    }
                    messageBodyLength = BitConverter.ToInt32(messageHeadBuffer, 0);
                    messageBodyBuffer = new byte[messageBodyLength];
                    fileStream.Read(messageBodyBuffer, 0, messageBodyLength);

                    using (MemoryStream mStream = new MemoryStream())
                    {
                        mStream.Write(messageBodyBuffer, 0, messageBodyLength);
                        mStream.Seek(0, SeekOrigin.Begin);
                        try
                        {
                            message = (Message)bf.Deserialize(mStream);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("反序列化战斗包出错");
                            Debug.LogWarning(e.Message);
                        }

                        mStream.Close();
                    }


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
        public NetworkMessage deserializeFromLocalByCmdIDCache(string targetPath, uint cmdID)
        {
            if (paths.Add(targetPath))
            {
                loadToMemory(targetPath);
                // 直接从提前存好的内存中读取
            }

            if (messagesBodySet == null)
            {
                sortMessagesByCmd();
            }
            return getMessageBodyByCmdIDFromResults(cmdID);
        }

        public List<T> deserializeFromLocalByTimeStamp<T>(string targetPath, ulong timeStamp) where T: Message
        {
            if (paths.Add(targetPath))
            {
                loadToMemory(targetPath);
            }
            // 直接从提前存好的内存中读取
            return getMessageBodyByTimeStampFromResults<T>(timeStamp);
        }

        private NetworkMessage getMessageBodyByCmdIDFromResults(uint cmdID)
        {
            if (messagesBodySet.ContainsKey(cmdID))
            {
                if (messagesBodySet[cmdID].Count > 0)
                {
                    NetworkMessage result = messagesBodySet[cmdID][0] as NetworkMessage;
                    messagesBodySet[cmdID].RemoveAt(0);
                    return result;
                }
            }
            UIAPI.ShowMsgTip("不存在" + cmdID);
            return null;
        }

        private List<T> getMessageBodyByTimeStampFromResults<T>(ulong timeStamp) where T : Message
        {
            List<T> results = new List<T>();
            try
            {
                foreach (Message item in messages)
                {
                    if (item is T)
                    {
                        if (item is NetworkMessage)
                        {
                            if (item.TimeStamp == timeStamp && (item as NetworkMessage).Serial == 0)
                            {
                                results.Add((T)item);
                            }
                        }

                        // if (item is MouseAction)
                        else {
                            Debuger.Log(timeStamp + ", " + item.TimeStamp);
                            if (item.TimeStamp == timeStamp)
                            {
                                results.Add((T)item);
                            }
                        }
                    }
                }
            }
            catch (InvalidCastException e)
            {
                Debug.LogWarning(timeStamp);
                Debug.LogWarning(e.Message);
            }

            foreach (var item in results)
            {
                messages.Remove(item);
            }
            
            
            return results;
        }

        /// <summary>
        /// // 将本地文件读进内存
        /// </summary>
        private void loadToMemory(string targetPath)
        {
            // if (messagesBodySet != null) return;
            // Dictionary<uint, List<T>> messagesBodySet = new Dictionary<uint, List<T>>();

            // 将本地文件读进内存
            Message tempMessage = null;
            FileStream fileStream = null;
            BinaryFormatter bf = new BinaryFormatter();
            try
            {
                fileStream = new FileStream(targetPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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

            byte[] messageHeadBuffer = new byte[NetworkMessage.MessageHeadLength];
            byte[] messageBodyBuffer;
            int messageBodyLength;
            // 开始读取
            do
            {
                // 读取message head来获得message body的长度
                if (fileStream.Read(messageHeadBuffer, 0, NetworkMessage.MessageHeadLength) == NetworkMessage.MessageHeadLength)
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
                            tempMessage = (Message)bf.Deserialize(mStream);
                            mStream.Close();
                        }
                        messages.Add(tempMessage);
                    }
                    else
                    {
                        Debug.LogWarning("CmdID::超出message包的范围");
                        Debug.Log("一共有" + messages.Count + "消息包");
                        fileStream.Close();
                        return;
                    }
                }
                else
                {
                    fileStream.Close();
                    Debug.Log("一共有" + messages.Count + "消息包");
                    return;
                }
            } while (true);
        }


        private void sortMessagesByCmd()
        {
            messagesBodySet = new Dictionary<uint, List<NetworkMessage>>();
            foreach (Message message in messages)
            {
                if (message is NetworkMessage)
                {
                    if (!messagesBodySet.ContainsKey(((NetworkMessage)message).CmdID))
                    {
                        messagesBodySet[((NetworkMessage)message).CmdID] = new List<NetworkMessage>();
                    }
                    messagesBodySet[((NetworkMessage)message).CmdID].Add(((NetworkMessage)message));
                }

            }
        }
    }
}

