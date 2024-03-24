using System;
//using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class TwitchService_EventSub : RefCounted
{
    private TwitchService twitchService;

    private WebSocketPeer clientEventSub = new WebSocketPeer();
    private double clientEventSubTimeToReconnect = 0.0;
    private double clientEventSubTimeToReconnectDefault = 10.0;
    private string eventSubSessionID = "-1";
    private HttpRequest twitchSubFetchHttpClient = null;

    public static readonly string TwitchSubEndpoint = "https://api.twitch.tv/helix/eventsub/subscriptions";
    public static readonly string TwitchSubURL = "wss://eventsub.wss.twitch.tv/ws";

    public void Init(TwitchService newTwitchService)
    {
        twitchService = newTwitchService;
    }

    private void ClientEventSubFailAndRestart(string errorMessage)
    {
        clientEventSubTimeToReconnect = clientEventSubTimeToReconnectDefault;
        GD.Print("ClientEventSubFailAndRestart - " + errorMessage);
    }

    private void ClientEventSubHandleConnectionClosed(int peerID)
    {
        ClientEventSubFailAndRestart("EventSub Connection closed");
    }

    private void ClientEventSubHandleConnectionError(bool wasClean)
    {
        ClientEventSubFailAndRestart("EventSub Connection closed with error");
    }

    private void SubFetchRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
    {
        Dictionary parsedResult = Json.ParseString(body.GetStringFromUtf8()).AsGodotDictionary();
        string parsedString = Json.Stringify(parsedResult);

        if (twitchService.debugPackets)
        {
            // BeepoCore.DebugLog("Sub Fetch - " + parsedString);
        }

        // If we get an authorization error, we need to re-do the oauth setup.
        if (responseCode == 401)
        {
            twitchService.twitchServiceOAuth.StartOAuthProcess();
            return;
        }
    }

    private void MakeSubRequest(Dictionary jsonData)
    {
        if (twitchService.debugPackets)
        {
            // BeepoCore.DebugLog("Making sub request: " + jsonData);
        }

        twitchSubFetchHttpClient = new HttpRequest();
        twitchSubFetchHttpClient.Name = "temp_request";
        twitchService.AddChild(twitchSubFetchHttpClient);
        twitchSubFetchHttpClient.RequestCompleted += SubFetchRequestCompleted;

        string[] headerParams = new string[] {
            "Authorization: Bearer " + twitchService.twitchOAuth,
            "Client-Id: " + twitchService.twitchClientID,
            "Content-Type: application/json"
        };

        Error err = twitchSubFetchHttpClient.Request(TwitchSubEndpoint, headerParams, HttpClient.Method.Post, Json.Stringify(jsonData));
    }

    private void ClientEventSubHandleConnectionEstablished(int peerID)
    {
        Dictionary channelUpdateRegistrationJson = new Dictionary();

        channelUpdateRegistrationJson.Add("type", "channel.update");
        channelUpdateRegistrationJson.Add("version", "2");
        Dictionary conditionData = new Dictionary();
        conditionData.Add("broadcaster_user_id", twitchService.TwitchUserID.ToString());
        channelUpdateRegistrationJson.Add("condition", conditionData);
        Dictionary transportData = new Dictionary();
        transportData.Add("method", "websocket");
        transportData.Add("session_id", eventSubSessionID);
        channelUpdateRegistrationJson.Add("transport", transportData);
        MakeSubRequest(channelUpdateRegistrationJson);

        Dictionary followEventRegistrationJson = new Dictionary();
        followEventRegistrationJson.Add("type", "channel.follow");
        followEventRegistrationJson.Add("version", "2");
        conditionData = new Dictionary();
        conditionData.Add("broadcaster_user_id", twitchService.TwitchUserID.ToString());
        conditionData.Add("moderator_user_id", twitchService.TwitchUserID.ToString());
        followEventRegistrationJson.Add("condition", conditionData);
        transportData = new Dictionary();
        transportData.Add("method", "websocket");
        transportData.Add("session_id", eventSubSessionID);
        followEventRegistrationJson.Add("transport", transportData);
        MakeSubRequest(followEventRegistrationJson);

        Dictionary subEventRegistrationJson = new Dictionary();
        subEventRegistrationJson.Add("type", "channel.subscription.message");
        subEventRegistrationJson.Add("version", "1");
        conditionData = new Dictionary();
        conditionData.Add("broadcaster_user_id", twitchService.TwitchUserID.ToString());
        subEventRegistrationJson.Add("condition", conditionData);
        transportData = new Dictionary();
        transportData.Add("method", "websocket");
        transportData.Add("session_id", eventSubSessionID);
        subEventRegistrationJson.Add("transport", transportData);
        MakeSubRequest(subEventRegistrationJson);

        Dictionary giftSubEventRegistrationJson = new Dictionary();
        giftSubEventRegistrationJson.Add("type", "channel.subscription.gift");
        giftSubEventRegistrationJson.Add("version", "1");
        conditionData = new Dictionary();
        conditionData.Add("broadcaster_user_id", twitchService.TwitchUserID.ToString());
        giftSubEventRegistrationJson.Add("condition", conditionData);
        transportData = new Dictionary();
        transportData.Add("method", "websocket");
        transportData.Add("session_id", eventSubSessionID);
        giftSubEventRegistrationJson.Add("transport", transportData);
        MakeSubRequest(giftSubEventRegistrationJson);

        Dictionary resubEventRegistrationJson = new Dictionary();
        resubEventRegistrationJson.Add("type", "channel.chat.message");
        resubEventRegistrationJson.Add("version", "1");
        conditionData = new Dictionary();
        conditionData.Add("broadcaster_user_id", twitchService.TwitchUserID.ToString());
        conditionData.Add("user_id", twitchService.TwitchUserID.ToString());
        resubEventRegistrationJson.Add("condition", conditionData);
        transportData = new Dictionary();
        transportData.Add("method", "websocket");
        transportData.Add("session_id", eventSubSessionID);
        resubEventRegistrationJson.Add("transport", transportData);
        MakeSubRequest(resubEventRegistrationJson);

        Dictionary cheerEventRegistrationJson = new Dictionary();
        cheerEventRegistrationJson.Add("type", "channel.cheer");
        cheerEventRegistrationJson.Add("version", "1");
        conditionData = new Dictionary();
        conditionData.Add("broadcaster_user_id", twitchService.TwitchUserID.ToString());
        cheerEventRegistrationJson.Add("condition", conditionData);
        transportData = new Dictionary();
        transportData.Add("method", "websocket");
        transportData.Add("session_id", eventSubSessionID);
        cheerEventRegistrationJson.Add("transport", transportData);
        MakeSubRequest(cheerEventRegistrationJson);

        Dictionary redeemEventRegistrationJson = new Dictionary();
        redeemEventRegistrationJson.Add("type", "channel.channel_points_custom_reward_redemption.add");
        redeemEventRegistrationJson.Add("version", "1");
        conditionData = new Dictionary();
        conditionData.Add("broadcaster_user_id", twitchService.TwitchUserID.ToString());
        redeemEventRegistrationJson.Add("condition", conditionData);
        transportData = new Dictionary();
        transportData.Add("method", "websocket");
        transportData.Add("session_id", eventSubSessionID);
        redeemEventRegistrationJson.Add("transport", transportData);
        MakeSubRequest(redeemEventRegistrationJson);
    }

    private void ClientEventSubHandleMessage(string type, Dictionary message)
    {
        switch (type)
        {
            case "channel.update":
                // BeepoCore.DebugLog("channel update event - " + message["title"]);
                break;
            case "channel.follow":
                // BeepoCore.DebugLog("channel follow event - " + message["user_name"]);
                twitchService.EmitSignal(TwitchService.SignalName.ChannelUserFollowed, new TwitchFollowPayload(message));
                break;
            case "channel.subscription.message":
                // BeepoCore.DebugLog("channel subscribe message event - " + message["user_name"]);
                twitchService.EmitSignal(TwitchService.SignalName.ChannelSubscriptionMessage, new TwitchSubscriptionMessagePayload(message));
                break;
            case "channel.subscription.gift":
                // BeepoCore.DebugLog("channel subscribe gift event - " + message["user_name"] + " gifted " + message["total"]);
                twitchService.EmitSignal(TwitchService.SignalName.ChannelGiftedSubs, new TwitchSubscriptionGiftPayload(message));
                break;
            case "channel.chat.message":
                // BeepoCore.DebugLog("channel message event - " + message["chatter_user_name"] + " : " + message["message"].As<Dictionary>()["text"]);
                twitchService.EmitSignal(TwitchService.SignalName.ChannelChatMessage, new TwitchChatMessagePayload(message));
                break;
            case "channel.cheer":
                // BeepoCore.DebugLog("channel cheer event - " + message["user_name"] + " cheered for " + message["bits"] + " bits");
                twitchService.EmitSignal(TwitchService.SignalName.ChannelCheer, new TwitchCheerPayload(message));
                break;
            case "channel.channel_points_custom_reward_redemption.add":
                // BeepoCore.DebugLog("channel redeem event - " + message["user_name"] + " redeemed " + message["reward"].As<Dictionary>()["title"]);
                twitchService.EmitSignal(TwitchService.SignalName.ChannelPointsRedeem, new TwitchRedeemPayload(message));
                break;
        }
    }

    private void ClientEventSubHandleDataReceived()
    {
        string resultStr = clientEventSub.GetPacket().GetStringFromUtf8();
        EventSubInjectPacket(resultStr);
    }

    private void EventSubInjectPacket(string packetText)
    {
        Dictionary resultDict = Json.ParseString(packetText).As<Dictionary>();
        if (resultDict.ContainsKey("metadata"))
        {
            if (resultDict["metadata"].As<Dictionary>().ContainsKey("message_type"))
            {
                if (resultDict["metadata"].As<Dictionary>()["message_type"].As<string>() == "session_welcome")
                {
                    eventSubSessionID = resultDict["payload"].As<Dictionary>()["session"].As<Dictionary>()["id"].As<string>();
                    if (clientEventSub.GetReadyState() == WebSocketPeer.State.Open)
                    {
                        ClientEventSubHandleConnectionEstablished(1);
                    }
                }
                if (resultDict["metadata"].As<Dictionary>()["message_type"].As<string>() == "notification")
                {
                    ClientEventSubHandleMessage(
                        resultDict["payload"].As<Dictionary>()["subscription"].As<Dictionary>()["type"].As<string>(),
                        resultDict["payload"].As<Dictionary>()["event"].As<Dictionary>()
                    );
                }
            }
        }
    }

    private void ClientEventSubConnectToTwitch()
    {
        if (twitchService.twitchClientID == "")
        {
            throw new ApplicationException("Twitch Client ID not set");
        }

        // Attempt connection
        Error err = clientEventSub.ConnectToUrl(TwitchSubURL);
        if (err != Error.Ok)
        {
            ClientEventSubFailAndRestart("EventSub Connection failed: " + err);
            return;
        }

        // Wait for the connection to be fully established.
        clientEventSub.Poll();
        while (clientEventSub.GetReadyState() == WebSocketPeer.State.Connecting)
        {
            clientEventSub.Poll();
        }

        // Handle failed connections.
        if (clientEventSub.GetReadyState() == WebSocketPeer.State.Closing)
        {
            return;
        }
        if (clientEventSub.GetReadyState() == WebSocketPeer.State.Closed)
        {
            return;
        }

        // Send subscription messages.
        clientEventSub.Poll();
        // The following was causing errors for some reason:
        //ClientEventSubHandleConnectionEstablished(1);
        //clientEventSub.Poll();
    }

    public void ClientEventSubUpdate(double delta)
    {
        if (twitchService.TwitchUserID == -1)
        {
            return;
        }

        clientEventSub.Poll();

        Error err = clientEventSub.GetPacketError();
        if (err != Error.Ok)
        {
            GD.Print("EventSub ERROR!!!! " + err);
        }

        while (clientEventSub.GetAvailablePacketCount() > 0)
        {
            ClientEventSubHandleDataReceived();
            clientEventSub.Poll();
        }

        // See if we need to reconnect.
        if (clientEventSub.GetReadyState() == WebSocketPeer.State.Closed)
        {
            clientEventSubTimeToReconnect -= delta;
            if (clientEventSubTimeToReconnect < 0.0)
            {
                // Reconnect to Twitch websocket.
                ClientEventSubConnectToTwitch();

                // Whatever happens, set a default reconnect delay.
                clientEventSubTimeToReconnect = clientEventSubTimeToReconnectDefault;
            }
        }

        clientEventSub.Poll();
    }
}