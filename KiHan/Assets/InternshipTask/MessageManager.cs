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

        private MessageManager() { }

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
            if (fileStream == null) { return;}
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

        public MessageBody deserializeFromLocalByCmdID(uint cmdID)
        {
            MessageBody message = null;
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
                        Debug.Log("message body length: " + messageBodyLength);
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
                                message = (MessageBody)bf.Deserialize(mStream);
                                mStream.Close();
                            }

                            if (message.CmdID == cmdID)
                            {
                                deleteBuffer(fileStream, beginPosition, endPosition);
                                fileStream.Close();
                                return message;
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
                        Debug.LogWarning("CmdID::超出message包的范围");
                        fileStream.Close();
                        return null;
                    }
                } while (true);
            }
            else
            {
                Debug.LogWarning("file stream == null");
                return null;
            }
        }
    }
}

