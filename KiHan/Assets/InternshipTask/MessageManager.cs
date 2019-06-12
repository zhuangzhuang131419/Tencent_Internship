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
        public static readonly string DEST_PATH = "F:\\Tencent_Internship\\KiHan\\log\\Temp.dat";
        // 内存中保存的一系列message，方便在给定cmdID的情况下取出 （利用cmdID作key，避免重复）
        public Dictionary<uint, MessageBody> messagesBodySet = null;

        private bool isSerializeToLocal = true;
        private bool isDeserializeFromLocal = false;

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

        public bool IsDeserializeFromLocal
        {
            get { return isDeserializeFromLocal; }
            set { isDeserializeFromLocal = value; }
        }

        private MessageManager() { }

        /// <summary>
        /// 把读到的消息包序列化到本地
        /// </summary>
        /// <param name="msgs"></param>
        /// <param name="messageType"></param>
        public void serializeToLocal(List<object> msgs, Type messageType, uint cmdID, ulong timeStamp, uint serial)
        {
            MessageBody packedMessageBody = new MessageBody(messageType, cmdID, timeStamp, serial);
            // 如果是NTF包，List<object>会有两项
            try
            {
                switch (msgs.Count)
                {
                    case 1:
                        // 非NTF包
                        packedMessageBody.MessagesBodyBuffer.Add(PBSerializer.NSerialize(msgs[0]));
                        break;
                    case 2:
                        // NTF包

                        // 测试是否可以反序列化
						// Debug.LogWarning("测试是否可以反序列化");
                        PBSerializer.NDeserialize(PBSerializer.NSerialize(msgs[1]), messageType);
                        packedMessageBody.MessagesBodyBuffer.Add(PBSerializer.NSerialize(msgs[1]));
                        break;
                    default:
                        Debug.LogError("出错");
                        break;
                }
            }
            catch
            {
                Debug.LogError("序列化出现问题");
                Debug.LogError("消息数量：" + msgs.Count + " ；序列号：" + serial);
            }
            
            serializeToLocal(packedMessageBody, DEST_PATH);
        }

        /// <summary>
        /// 把服务器接收来的消息包序列化到本地
        /// </summary>
        /// <param name="destinationPath">要存放的地址</param>
        public void serializeToLocal(MessageBody messageBody, string destinationPath)
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
                bf.Serialize(fileStream, messageBody);
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
                Debug.Log("利用cmdID成功读取");
                try
                {
                    List<object> messageBodyResult = new List<object>();
                    foreach (byte[] msgBodyBuffer in packedMessage01.MessagesBodyBuffer)
                    {
                        messageBodyResult.Add(PBSerializer.NDeserialize(msgBodyBuffer, packedMessage01.MessageType));
                    }

                    // 成功解析出message
                    return messageBodyResult;

                }
                catch (Exception)
                {
                    Debug.LogWarning("ReadMessageFromLocal：NDeserialize 出现了异常");
                    return null;
                }
            }
            else
            {
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
        public MessageBody deserializeFromLocal(string sourcePath, int count)
        {
            MessageBody message = null;
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
                        return null;
                    }
                }

                try
                {
                    // 当前位置是我们所要的message
                    // 读取message head来获得message body的长度
                    long beginPosition = fileStream.Position;
                    if (fileStream.Read(messageHeadBuffer, 0, MessageBody.MessageHeadLength) != MessageBody.MessageHeadLength)
                    {
                        return null;
                    }
                    messageBodyLength = BitConverter.ToInt32(messageHeadBuffer, 0);
                    messageBodyBuffer = new byte[messageBodyLength];
                    fileStream.Read(messageBodyBuffer, 0, messageBodyLength);

                    using (MemoryStream mStream = new MemoryStream())
                    {
                        mStream.Write(messageBodyBuffer, 0, messageBodyLength);
                        mStream.Flush();
                        mStream.Seek(0, SeekOrigin.Begin);
                        message = (MessageBody)bf.Deserialize(mStream);
                        mStream.Close();
                    }

                    long endPosition = fileStream.Position;

                    // 读完这个包后要把这段数据去掉(beginPosition 到 endPosition 之间的)
                    deleteBuffer(fileStream, beginPosition, endPosition);

                }
                catch (IOException)
                {
                    Debug.LogWarning("IOException");
                }
            }
            fileStream.Close();
            return message;
        }

        private void deleteBuffer(FileStream fileStream, long from, long to)
        {
            Debug.Log("删除相对应的包");
            if (fileStream == null) { return; }
            byte[] tempBuffer1 = new byte[from];
            byte[] tempBuffer2 = new byte[fileStream.Length - to];
            fileStream.Position = 0;
            if (from != 0 && fileStream.Read(tempBuffer1, 0, tempBuffer1.Length) == tempBuffer1.Length) { }

            fileStream.Position = to;
            if (to != fileStream.Length && fileStream.Read(tempBuffer2, 0, tempBuffer2.Length) == tempBuffer2.Length) { }
            fileStream.Close();

            FileStream tempFileStream = new FileStream(DEST_PATH, FileMode.Truncate, FileAccess.ReadWrite);
            tempFileStream.Write(tempBuffer1, 0, tempBuffer1.Length);
            tempFileStream.Write(tempBuffer2, 0, tempBuffer2.Length);
            tempFileStream.Close();
            Debug.Log("删除成功");
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
                fileStream = new FileStream(DEST_PATH, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
                                deleteBuffer(fileStream, beginPosition, endPosition);
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
            MessageBody result = null;
            messagesBodySet.TryGetValue(cmdID, out result);
            return result;
        }

        private MessageBody getMessageBodyByTimeStampFromResults(ulong timeStamp)
        {
            foreach (var messageBody in messagesBodySet.Values)
            {
                if (messageBody.TimeStamp == timeStamp)
                {
                    return messageBody;
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
            messagesBodySet = new Dictionary<uint, MessageBody>();
            // 将本地文件读进内存
            MessageBody tempMessageBody = null;
            MessageBody resultMessageBody = null;
            FileStream fileStream = null;
            BinaryFormatter bf = new BinaryFormatter();
            try
            {
                fileStream = new FileStream(DEST_PATH, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
                            mStream.Seek(0, SeekOrigin.Begin);
                            tempMessageBody = (MessageBody)bf.Deserialize(mStream);
                            mStream.Close();
                        }

                        if (messagesBodySet.ContainsKey(tempMessageBody.CmdID))
                        {
                            MessageBody m = null;
                            messagesBodySet.TryGetValue(tempMessageBody.CmdID, out m);
                            if (m != null && m.TimeStamp < tempMessageBody.TimeStamp)
                            {
                                messagesBodySet.Remove(tempMessageBody.CmdID);
                                messagesBodySet.Add(tempMessageBody.CmdID, tempMessageBody);
                            }
                        }
                        else
                        {
                            messagesBodySet.Add(tempMessageBody.CmdID, tempMessageBody);
                        }
                        
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

