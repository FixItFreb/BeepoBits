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
    // public static readonly string TwitchSubURL = "ws://127.0.0.1:8080/ws";
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

        if (err != Error.Ok)
        {
            GD.PrintErr("Failed to subscribe to twitch event [" + jsonData["type"] + "], reason: " + err);
        }
    }

    private void ClientEventSubHandleConnectionEstablished(int peerID)
    {
        Dictionary conditionData;
        Dictionary transportData;

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

        Dictionary raidEventRegistrationJson = new Dictionary();
        raidEventRegistrationJson.Add("type", "channel.raid");
        raidEventRegistrationJson.Add("version", "1");
        conditionData = new Dictionary();
        conditionData.Add("broadcaster_user_id", twitchService.TwitchUserID.ToString());
        conditionData.Add("moderator_user_id", twitchService.TwitchUserID.ToString());
        raidEventRegistrationJson.Add("condition", conditionData);
        transportData = new Dictionary();
        transportData.Add("method", "websocket");
        transportData.Add("session_id", eventSubSessionID);
        raidEventRegistrationJson.Add("transport", transportData);
        MakeSubRequest(raidEventRegistrationJson);

        Dictionary subEventRegistrationJson = new Dictionary();
        subEventRegistrationJson.Add("type", "channel.subscribe");
        subEventRegistrationJson.Add("version", "1");
        conditionData = new Dictionary();
        conditionData.Add("broadcaster_user_id", twitchService.TwitchUserID.ToString());
        conditionData.Add("moderator_user_id", twitchService.TwitchUserID.ToString());
        subEventRegistrationJson.Add("condition", conditionData);
        transportData = new Dictionary();
        transportData.Add("method", "websocket");
        transportData.Add("session_id", eventSubSessionID);
        subEventRegistrationJson.Add("transport", transportData);
        MakeSubRequest(subEventRegistrationJson);

        Dictionary resubEventRegistrationJson = new Dictionary();
        resubEventRegistrationJson.Add("type", "channel.subscription.message");
        resubEventRegistrationJson.Add("version", "1");
        conditionData = new Dictionary();
        conditionData.Add("broadcaster_user_id", twitchService.TwitchUserID.ToString());
        resubEventRegistrationJson.Add("condition", conditionData);
        transportData = new Dictionary();
        transportData.Add("method", "websocket");
        transportData.Add("session_id", eventSubSessionID);
        resubEventRegistrationJson.Add("transport", transportData);
        MakeSubRequest(resubEventRegistrationJson);

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

        Dictionary messageEventRegistrationJson = new Dictionary();
        messageEventRegistrationJson.Add("type", "channel.chat.message");
        messageEventRegistrationJson.Add("version", "1");
        conditionData = new Dictionary();
        conditionData.Add("broadcaster_user_id", twitchService.TwitchUserID.ToString());
        conditionData.Add("user_id", twitchService.TwitchUserID.ToString());
        messageEventRegistrationJson.Add("condition", conditionData);
        transportData = new Dictionary();
        transportData.Add("method", "websocket");
        transportData.Add("session_id", eventSubSessionID);
        messageEventRegistrationJson.Add("transport", transportData);
        MakeSubRequest(messageEventRegistrationJson);

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

        var newEvent = BuildStreamEvent(type, message);
        BeepoCore.GetInstance().SendEvent(newEvent, "StreamEvents");
    }

    public BeepoEvent BuildStreamEvent(string eventType, Dictionary payload)
    {
        GD.Print("Got: " + payload);
        BeepoEvent newEvent = new();
        switch (eventType)
        {
            case "channel.follow":
                FollowEvent followEvent = new();
                followEvent.user_name = payload["user_name"].As<string>();
                newEvent = followEvent;
                break;
            case "channel.subscribe":
                SubscriptionEvent subscriptionEvent = new();
                subscriptionEvent.user_name = payload["user_name"].As<string>();
                subscriptionEvent.is_gift = payload["is_gift"].As<bool>();
                newEvent = subscriptionEvent;
                break;
            case "channel.subscription.message":
                SubscriptionEvent resubEvent = new();
                resubEvent.user_name = payload["user_name"].As<string>();
                resubEvent.is_gift = false;
                newEvent = resubEvent;
                break;
            case "channel.subscription.gift":
                DonationEvent giftSubs = new();
                giftSubs.type = "subscription";
                giftSubs.user_name = payload["user_name"].As<string>();
                giftSubs.total = payload["total"].As<int>();
                giftSubs.message = "";
                giftSubs.is_anonymous = payload["is_anonymous"].As<bool>();
                newEvent = giftSubs;
                break;
            case "channel.cheer":
                DonationEvent cheerEvent = new();
                cheerEvent.type = "cheer";
                cheerEvent.user_name = payload["user_name"].As<string>();
                cheerEvent.total = payload["bits"].As<int>();
                cheerEvent.message = payload["message"].As<string>();
                cheerEvent.is_anonymous = payload["is_anonymous"].As<bool>();
                newEvent = cheerEvent;
                break;
            case "channel.channel_points_custom_reward_redemption.add":
                RedeemEvent redeemEvent = new();
                redeemEvent.user_name = payload["user_name"].As<string>();
                redeemEvent.title = payload["reward"].As<Dictionary>()["title"].As<string>();
                redeemEvent.input = payload["user_input"].As<string>();
                newEvent = redeemEvent;
                break;
            case "channel.raid":
                RaidEvent raidEvent = new();
                raidEvent.user_name = payload["from_broadcaster_user_name"].As<string>();
                raidEvent.viewers = payload["viewers"].As<int>();
                newEvent = raidEvent;
                break;
        }

        return newEvent;
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