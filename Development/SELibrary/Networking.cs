using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class Networking
        {
            private string unicastTag = "";
            private string broadcastTag = "";
            private Action<long, object> HandleMessage;

            IMyUnicastListener unicastListener;
            IMyBroadcastListener broadcastListener;

            Program p;

            public Networking(Program program, string unicastTag, string broadcastTag)
            {
                p = program;
                this.unicastTag = unicastTag;
                this.broadcastTag = broadcastTag;

                unicastListener = p.IGC.UnicastListener;
                unicastListener.SetMessageCallback(unicastTag);

                broadcastListener = p.IGC.RegisterBroadcastListener(broadcastTag);
                broadcastListener.SetMessageCallback(broadcastTag);
            }

            public void Unicast<T>(long receiver, T message)
            {
                p.IGC.SendUnicastMessage<T>(receiver, unicastTag, message);
            }

            public void Broadcast<T>(T message)
            {
                p.IGC.SendBroadcastMessage<T>(broadcastTag, message, TransmissionDistance.TransmissionDistanceMax);
            }
                    
            public void SetMessageHandler(Action<long, object> handler)
            {
                HandleMessage = handler;
            }
        
            public void ProcessMessages()
            {
                if (broadcastListener.HasPendingMessage)
                {
                    MyIGCMessage message = broadcastListener.AcceptMessage();
                    HandleMessage(message.Source, message.Data);
                }
                if (unicastListener.HasPendingMessage)
                {
                    MyIGCMessage message = unicastListener.AcceptMessage();
                    HandleMessage(message.Source, message.Data);
                }
            }
        
            public bool IsReachableReceiver(long id)
            {
                return p.IGC.IsEndpointReachable(id);
            }
        
            public void SetUnicastTag(string tag)
            {
                unicastTag = tag;
            }

            public void SetBroadcastTag(string tag)
            {
                broadcastTag = tag;
            }
        }
    }
}
