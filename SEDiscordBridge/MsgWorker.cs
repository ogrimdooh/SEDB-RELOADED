using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;

namespace SEDiscordBridge
{
    public static class MsgWorker
    {

        private const int NORMAL_LIVE = 1500;
        private const int LONG_LIVE = 5000;

        public struct DiscordMessage
        {

            public DiscordChannel Chann { get; set; }
            public string Message { get; set; }
            public DateTime Time { get; set; }
            public bool LongLive { get; set; }
            public Action<DSharpPlus.Entities.DiscordMessage> CallBack { get; set; }

        }
        
        private static ConcurrentQueue<DiscordMessage> messagesToSend = new ConcurrentQueue<DiscordMessage>();
        private static List<DiscordMessage> lastMessages = new List<DiscordMessage>();
        private static Thread mainThread = null;
        private static bool canRun = true;

        private static void DoWork()
        {
            if (SEDiscordBridgePlugin.DEBUG)
            {
                Logging.Instance.LogDebug(typeof(MsgWorker), $"Thread work start!");
            }
            while (canRun)
            {
                try
                {
                    if (messagesToSend.Any())
                    {
                        DiscordMessage msgToSend;
                        if (messagesToSend.TryDequeue(out msgToSend))
                        {
                            if (!lastMessages.Any(x => x.Message == msgToSend.Message))
                            {
                                if (SEDiscordBridgePlugin.DEBUG)
                                {
                                    Logging.Instance.LogInfo(typeof(MsgWorker), $"Send message : MSG={msgToSend.Message}");
                                }
                                
                                if (DiscordBridge.Discord != null && DiscordBridge.Ready)
                                {
                                    var task = DiscordBridge.Discord.SendMessageAsync(msgToSend.Chann, msgToSend.Message.Replace("/n", "\n"));
                                    task.Wait();
                                    if (msgToSend.CallBack != null)
                                    {
                                        msgToSend.CallBack.Invoke(task.Result);
                                    }
                                    Logging.Instance.LogInfo(typeof(MsgWorker), $"Message sent : MSG={task.Result.Id}");
                                }
                                else
                                {
                                    Logging.Instance.LogWarning(typeof(MsgWorker), $"Bridge is not ready!");
                                }
                            }
                            lastMessages.Add(new DiscordMessage() { Chann = msgToSend.Chann, Message = msgToSend.Message, Time = DateTime.Now });
                        }
                    }
                    if (lastMessages.Any())
                    {
                        lastMessages.RemoveAll(x => (DateTime.Now - x.Time).TotalMilliseconds > (x.LongLive ? LONG_LIVE : NORMAL_LIVE));
                    }
                }
                catch (Exception ex)
                {
                    Logging.Instance.LogError(typeof(MsgWorker), ex);
                }
                Thread.Sleep(250);
            }
            if (SEDiscordBridgePlugin.DEBUG)
            {
                Logging.Instance.LogDebug(typeof(MsgWorker), $"Thread work stop!");
            }
        }

        public static void DoLoad()
        {
            if (mainThread != null)
            {
                if (SEDiscordBridgePlugin.DEBUG)
                {
                    Logging.Instance.LogDebug(typeof(MsgWorker), $"Abort current main Thread!");
                }
                mainThread.Abort();
                mainThread = null;
            }
            mainThread = new Thread(DoWork);
            mainThread.Start();
            if (SEDiscordBridgePlugin.DEBUG)
            {
                Logging.Instance.LogDebug(typeof(MsgWorker), $"Start main Thread!");
            }
        }

        public static void SendToDiscord(DiscordChannel chann, string msg, bool longLive, Action<DSharpPlus.Entities.DiscordMessage> callBack = null)
        {
            messagesToSend.Enqueue(new DiscordMessage()
            {
                Chann = chann,
                Message = msg,
                Time = DateTime.Now,
                LongLive = longLive,
                CallBack = callBack
            });
            if (SEDiscordBridgePlugin.DEBUG)
            {
                Logging.Instance.LogDebug(typeof(MsgWorker), $"Register message : MSG={msg}");
            }
        }

        public static void DisconnectAfterSendAllMsgs(DiscordBridge bridge)
        {
            if (SEDiscordBridgePlugin.DEBUG)
            {
                Logging.Instance.LogInfo(typeof(MsgWorker), $"Bridge marked to disconect!");
            }
            int c = 0;
            while (messagesToSend.Any() && c < 10)
            {
                Thread.Sleep(250);
                c++;
            }
            canRun = false;
            if (bridge != null)
            {
                if (SEDiscordBridgePlugin.DEBUG)
                {
                    Logging.Instance.LogInfo(typeof(MsgWorker), $"Bridge disconect!");
                }
            }
        }

    }

}
