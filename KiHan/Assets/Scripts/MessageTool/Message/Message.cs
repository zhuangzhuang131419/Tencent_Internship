using UnityEngine;
using System.Collections;
using System;

namespace KH
{
    [Serializable]
    public class Message
    {
        private ulong timeStamp;

        public ulong TimeStamp
        {
            get { return timeStamp; }
            set { timeStamp = value; }
        }
    }
}


