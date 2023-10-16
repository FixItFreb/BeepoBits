using System;
using Godot;
using Godot.Collections;

public partial class TwitchService_PubSub : RefCounted
{
    private TwitchService twitchService = null;

    // Pubsub connection target.
    public static readonly string twitchServiceURL = "wss://pubsub-edge.twitch.tv";

    private WebSocketPeer clientPubSub = new WebSocketPeer();
    private double clientPubSubTimeToReconnect = 0.0;
    private double clientPubSubTimeToPing = 30.0;

    public static double ClientPubSubTimeToReconnectDefault = 20.0;
    public static double ClientPubSubTimeToPingDefault = 30.0;

    public void Init(TwitchService newTwitchService)
    {
        twitchService = newTwitchService;
    }

    private void ClientPubSubFailAndRestart(string errorMessage)
    {
        clientPubSubTimeToReconnect = 10.0f;
    }

    private void ClientPubSubHandleConnectionClosed(int peerId)
    {
        ClientPubSubFailAndRestart("Connection closed");
    }

    private void ClientPubSubHandleConnectionError(bool wasClean)
    {
        ClientPubSubFailAndRestart("Connection closed with error");
    }

    private void ClientPubSubSendPing()
    {
        // Send a ping! For funsies or something.
        Dictionary pingJson = new Dictionary();
        pingJson.Add("type", "PING");
        string pingData = Json.Stringify(pingJson);
        clientPubSub.SendText(pingData);

        GD.Print("pubsub ping!");
    }

    private void ClientPubSubHandleConnectionEstablished(int peerId)
    {
        // Send a ping! For funsies or something.
        ClientPubSubSendPing();

        // Register for channel point redeems.
        Dictionary eventRegistrationJson = new Dictionary();
        eventRegistrationJson.Add("type", "LISTEN");
        eventRegistrationJson.Add("nonce", "ChannelPoint");
        Dictionary data = new Dictionary();
        data.Add("topics", new string[] {
            "channel-points-channel-v1." + twitchService.TwitchUserID,
            "channel-bits-events-v1." + twitchService.TwitchUserID});
        eventRegistrationJson.Add("data", data);
        eventRegistrationJson.Add("auth_token", twitchService.twitchOAuth);

        string eventRegistrationData = Json.Stringify(eventRegistrationJson);
        clientPubSub.SendText(eventRegistrationData);
    }

    private void ClientPubSubHandleRewardRedeemed(string title, string username, string displayname, string userInput)
    {
        EmitSignal(TwitchService.SignalName.ChannelPointsRedeem, title, username, displayname, userInput);
    }

    private void ClientPubSubHandleMessage(string topic, Dictionary message)
    {
        if (message.ContainsKey("type"))
        {
            if ((string)message["type"] == "reward-redeemed")
            {
                string userInput = "";
                if (message["data"].As<Dictionary>()["redemption"].As<Dictionary>().TryGetValue("user-input", out Variant outUserInput))
                {
                    userInput = outUserInput.ToString();
                }

                string redeemTitle = message["data"].As<Dictionary>()["redemption"].As<Dictionary>()["reward"].As<Dictionary>()["title"].ToString();
                string redeemUsername = message["data"].As<Dictionary>()["redemption"].As<Dictionary>()["user"].As<Dictionary>()["login"].ToString();
                string redeemDisplayname = message["data"].As<Dictionary>()["redemption"].As<Dictionary>()["user"].As<Dictionary>()["display_name"].ToString();
                string redeemUserInput = userInput;
                ClientPubSubHandleRewardRedeemed(redeemTitle, redeemUsername, redeemDisplayname, redeemUserInput);
            }
        }
    }

    private void ClientPubSubHandleDataReceived()
    {
        string resultStr = clientPubSub.GetPacket().GetStringFromUtf8();
        PubSubInjectPacket(resultStr);
    }

    // Inject a packet to handle a pubsub message. This is used for both real and
    // fake (testing) packets.
    private void PubSubInjectPacket(string packetText)
    {
        Dictionary resultDict = Json.ParseString(packetText).AsGodotDictionary();
        string resultIndented = Json.Stringify(resultDict, "    ");

        if ((string)resultDict["type"] == "MESSAGE")
        {
            if (twitchService.debugPackets)
            {
                GD.Print("PubSub: " + packetText);
            }
            string messageTopic = resultDict["data"].As<Dictionary>()["topic"].ToString();
            Dictionary messageData = Json.ParseString(resultDict["data"].As<Dictionary>()["message"].ToString()).As<Dictionary>();
            ClientPubSubHandleMessage(messageTopic, messageData);
        }
    }

    private void ClientPubSubConnectToTwitch()
    {
        if (twitchService.twitchClientID == "")
        {
            throw new ApplicationException("Twitch Client ID not set");
        }

        // Attempt connection
        Error err = clientPubSub.ConnectToUrl(twitchServiceURL);
        if (err != Error.Ok)
        {
            ClientPubSubFailAndRestart("Connection failed: " + err);
            return;
        }

        // Wait for the connection to be fully established
        clientPubSub.Poll();
        while (clientPubSub.GetReadyState() == WebSocketPeer.State.Connecting)
        {
            clientPubSub.Poll();
        }

        // Handle failed connections
        if (clientPubSub.GetReadyState() == WebSocketPeer.State.Closing)
        {
            return;
        }
        if (clientPubSub.GetReadyState() == WebSocketPeer.State.Closed)
        {
            return;
        }

        // Send subscription messages
        clientPubSub.Poll();
        ClientPubSubHandleConnectionEstablished(1);
        clientPubSub.Poll();
    }

    public void ClientPubSubUpdate(double delta)
    {
        if (twitchService.TwitchUserID == -1)
        {
            return;
        }

        clientPubSub.Poll();

        Error err = clientPubSub.GetPacketError();
        if (err != Error.Ok)
        {
            GD.PrintErr("ERROR!!!! ", err);
        }

        while (clientPubSub.GetAvailablePacketCount() > 0)
        {
            ClientPubSubHandleDataReceived();
        }

        // See if we need to reconnect
        if (clientPubSub.GetReadyState() == WebSocketPeer.State.Closed)
        {
            clientPubSubTimeToReconnect -= delta;

            if (clientPubSubTimeToReconnect < 0.0f)
            {
                // Reconnect to Twitch websocket
                ClientPubSubConnectToTwitch();

                // Whatever happens, set a default reconnect delay
                clientPubSubTimeToReconnect = ClientPubSubTimeToReconnectDefault;
            }
            else
            {
                clientPubSubTimeToPing -= delta;
                if (clientPubSubTimeToPing < 0.0f)
                {
                    clientPubSubTimeToPing = ClientPubSubTimeToPingDefault;
                    ClientPubSubSendPing();
                }
            }

            clientPubSub.Poll();
        }
    }
}
