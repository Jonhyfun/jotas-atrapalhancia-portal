﻿using JotasTwitchPortal.JSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.WebSockets;
using System.Text;

namespace TwitchHandler
{

    public class TwitchMessage
    {
        public class Metadata
        {
            public string message_id { get; set;}
            public string message_type { get; set; }
            public string message_timestamp { get; set;}
            public string subscription_type { get; set; }
        }

        public Metadata metadata { get; set;}
        public JObject payload { get; set;}
    }

    public class EventSub : IDisposable
    {
        public event EventHandler<RewardEvent> OnChatRewardRedeemed;
        public event EventHandler<string> OnConnected;
        public string SessionId;
        ClientWebSocket ws = new ClientWebSocket();

        public EventSub()
        {

        }

        public async void Connect()
        {
            await ws.ConnectAsync(new Uri("wss://eventsub.wss.twitch.tv/ws"), CancellationToken.None);

            var buffer = new byte[4096];

            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

                if(json != null)
                {
                    var message = JsonConvert.DeserializeObject<TwitchMessage>(json)!;
                    if (message != null)
                    {
                        if (message.metadata.message_type == "session_welcome")
                        {
                            SessionId = message.payload.Value<JObject>("session")!.Value<string>("id")!;
                            Console.WriteLine(json);
                            OnConnected.Invoke(this, SessionId);
                            //await ws.SendAsync(new ArraySegment<byte>(/**/, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        else if(message.metadata.message_type == "notification" && message.metadata.subscription_type == "channel.channel_points_custom_reward_redemption.add")
                        {
                            var redeem = JsonConvert.DeserializeObject<RewardEvent>(message.payload.ToString())!;
                            OnChatRewardRedeemed.Invoke(this, redeem);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).GetAwaiter().GetResult();
            ws.Dispose();
        }
    }
}
