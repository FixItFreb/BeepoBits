using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using GDArray = Godot.Collections.Array;

[Flags] public enum TwitchBadge
{
    None = 0x0,
    Broadcaster = 0x1,
    Moderator = 0x2,
    Subscriber = 0x4,
    VIP = 0x8,
}

public partial class TwitchService : Node
{
    // Static Variables
    public static string twitchServiceURL = "wss://pubsub-edge.twitch.tv";
    public static string twitchIRCURL = "wss://irc-ws.chat.twitch.tv";
    public static string twitchUsersEndpoint = "https://api.twitch.tv/helix/users";
    public static ushort twitchRedirectPort = 8080;
    public static float twitchConnectRetryTime = 5.0f;
    public static float twitchPubSubPingTime = 30.0f;

    // Config Variables
    [Export] public string twitchClientID = "";
    [Export] public string twitchOAuth = "";
    [Export] public string twitchUsername = "";
    [Export] public string twitchConfigPath = "user://twitch_config.ini";
    [Export] public bool autoSaveCredentials = true;
    [Export] public bool autoLoadCredentials = true;
    [Export] public bool debugPackets = true;
    [Export] public bool pauseUpdates = false;


    // Twitch Event Handlers
    [Signal] public delegate void ChannelChatMessageEventHandler(string fromUsername, string fromDisplayName, string message, int bits, int badges);
    [Signal] public delegate void ChannelPointsRedeemEventHandler(string redeemTitle, string redeemerUsername, string redeemerDisplayName, string userInput);
    [Signal] public delegate void ChannelRaidEventHandler(string raiderUsername, string raiderDisplayName, string raiderUserCount);


    #region OAuth flow
    private bool oAuthInProcess = false;
    private TcpServer oAuthTCPServer = null;
    private StreamPeerTcp oAuthStreamPeerTCP = null;
    private string oAuthStreamPeerTCPInputBuffer = "";

    private void StopOAuthProcess()
    {
        if(oAuthTCPServer != null)
        {
            oAuthTCPServer.Stop();
            oAuthTCPServer = null;
        }

        if(oAuthStreamPeerTCP != null)
        {
            oAuthStreamPeerTCP.DisconnectFromHost();
            oAuthStreamPeerTCP = null;
        }

        oAuthInProcess = false;
        oAuthStreamPeerTCPInputBuffer = "";
    }

    private void OAuthSendPageData(StreamPeer peer, string data)
    {
        string httpResponse = string.Join(
            "\r\n",
            "HTTP/1.1 200 OK",
            "Content-Type: text/html; charset=utf-8",
            "Content-Length: " + (Int64)data.Length,
            "Connection: close",
            "Cache-Control: max-age=0",
            "",
            ""
        );

        string fullResponse = httpResponse + data + "\n\n\n\n\n";
        byte[] responseAscii = fullResponse.ToAsciiBuffer();
        peer.PutData(responseAscii);
    }

    private void PollOAuthServer()
    {
        if(!oAuthInProcess)
        {
            return;
        }

        // Accept incoming connections
        if(oAuthTCPServer != null)
        {
            if(oAuthTCPServer.IsConnectionAvailable())
            {
                oAuthStreamPeerTCP = oAuthTCPServer.TakeConnection();
            }
        }

        // Add any new incoming bytes to our input buffer
        if(oAuthStreamPeerTCP != null)
        {
            while(oAuthStreamPeerTCP.GetAvailableBytes() > 0)
            {
                string incoming_byte = oAuthStreamPeerTCP.GetUtf8String(1);
                if(incoming_byte != "\r")
                {
                    oAuthStreamPeerTCPInputBuffer += incoming_byte;
                }
            }
        }

        // Only act on stuff once we have two newlines at the end of a request
        if(oAuthStreamPeerTCPInputBuffer.EndsWith("\n\n"))
        {
            // For each line...
            while(oAuthStreamPeerTCPInputBuffer.Contains("\n"))
            {
                // Take the line and pop it out of the buffer
                string getLine = oAuthStreamPeerTCPInputBuffer.Split("\n", true)[0];
                oAuthStreamPeerTCPInputBuffer = oAuthStreamPeerTCPInputBuffer.Substring(getLine.Length + 1);

                // All we care about here is the GET line
                if(getLine.StartsWith("GET "))
                {
                    // Split "GET <path> HTTP/1.1" into "GET", <path>, and
                    // "HTTP/1.1".
                    string[] getLineParts = getLine.Split(" ");
                    string httpGetPath = getLineParts[1];

                    // If we get the root path without the arguments, then it means
                    // that Twitch has stuffed the access token into the fragment
                    // (after the #). Send a redirect page to read that and give it
                    // to us in a GET request.
                    if (httpGetPath == "/")
                    {
                        // Response page: Just a Javascript program to do a redirect
                        // so we can get the access token into the a GET argument
                        // instead of the fragment.
                        string htmlResponse = @"
                            <html><head></head><body><script>
							  var url_parts = String(window.location).split(""#"");
							  if(url_parts.length > 1) {
								  var redirect_url = url_parts[0] + ""?"" + url_parts[1];
								  window.location = redirect_url;
							  }
						</script></body></html>
                        ";

                        // Send webpage and disconnect.
                        OAuthSendPageData(oAuthStreamPeerTCP, htmlResponse);
                        oAuthStreamPeerTCP.DisconnectFromHost();
                        oAuthStreamPeerTCP = null;
                    }

                    // If the path has a '?' in it at all, then it's probably our
                    // redirected page
                    else if(httpGetPath.Contains("?"))
                    {
                        string htmlResponse = @"<html><head></head><body>You may now close this window.</body></html>";

                        // Attempt to extract the access token from the GET data.
                        string[] pathParts = httpGetPath.Split("?");
                        if(pathParts.Length > 1)
                        {
                            string parameters = pathParts[1];
                            string[] argList = parameters.Split("&");
                            foreach(string arg in argList)
                            {
                                string[] argParts = arg.Split("=");
                                if(argParts.Length > 1)
                                {
                                    if(argParts[0] == "access_token")
                                    {
                                        twitchOAuth = argParts[1];
                                    }
                                }
                            }
                        }

                        // Send webpage and disconnect
                        OAuthSendPageData(oAuthStreamPeerTCP, htmlResponse);
                        oAuthStreamPeerTCP.DisconnectFromHost();
                        oAuthStreamPeerTCP = null;
                        StopOAuthProcess();
                    }
                }
            }
        }
    }

    private void StartOAuthProcess()
    {
        oAuthInProcess = true;

        // Kill any existing websocket server
        if(oAuthTCPServer != null)
        {
            oAuthTCPServer.Stop();
            oAuthTCPServer = null;
        }

        // Fire up a new server
        oAuthTCPServer = new TcpServer();
        oAuthTCPServer.Listen(twitchRedirectPort, "127.0.0.1");

        // Check client ID to make sure we aren't about to do something we'll regret
        byte[] asciiTwitchID = twitchClientID.ToAsciiBuffer();
        foreach(byte k in asciiTwitchID)
        {
            // Make sure we're only using alphanumeric values
            if((k >= 65 && k <= 90) || (k >= 97 && k <= 122) || (k >= 48 && k <= 57))
            {
            }
            else
            {
                throw new ApplicationException("Tried to connect with invalid Twitch Client ID");
            }
        }

        // Notes on scopes used in this URL:
        // channel:read:redemptions - Needed for point redeems.
        // chat:read                - Needed for reading chat (and raids?).
        // bits:read                - Needed for reacting to bit donations.
        string oAuthURL = string.Format(@"https://id.twitch.tv/oauth2/authorize?response_type=token&client_id={0}&redirect_uri=http://localhost:{1}/&scope=channel%3Aread%3Aredemptions%20chat%3Aread%20bits%3Aread%20chat%3Aedit",
        twitchClientID, twitchRedirectPort);
        OS.ShellOpen(oAuthURL);
    }
    #endregion

    #region User ID fetch
    private int twitchUserID = -1;
    private double twitchUserIDFetchTimeToRetry = 0.0f;
    private HttpRequest twitchUserIDFetchHttpClient = null;

    private void UserIDRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
    {
        Dictionary parsedResult = Json.ParseString(body.GetStringFromUtf8()).AsGodotDictionary();

        // If we get an authorization error, we need to re-do the oauth setup.
        if(responseCode == 401)
        {
            StartOAuthProcess();
            if(twitchUserIDFetchHttpClient != null)
            {
                twitchUserIDFetchHttpClient.QueueFree();
                twitchUserIDFetchHttpClient = null;
                return;
            }
        }

        // Get the user ID and login from the incoming Twitch data
        twitchUserID = -1;
        Array<Dictionary> parsedResultData = parsedResult["data"].AsGodotArray<Dictionary>();
        foreach(Dictionary user in parsedResultData)
        {
            twitchUserID = (int)user["id"];
            SetTwitchCredentials((string)user["login"], twitchOAuth);
            break;
        }

        // Clean up
        if(twitchUserIDFetchHttpClient != null)
        {
            twitchUserIDFetchHttpClient.QueueFree();
            twitchUserIDFetchHttpClient = null;
        }

        twitchUserIDFetchTimeToRetry = twitchConnectRetryTime;
    }

    // Determine the user ID of the user who's authorized this
    private void FetchUserID()
    {
        if (twitchUserIDFetchHttpClient != null)
        {
            // Request already in-flight
            return;
        }

        twitchUserIDFetchHttpClient = new HttpRequest();
        twitchUserIDFetchHttpClient.Name = "temp_request";
        AddChild(twitchUserIDFetchHttpClient);
        twitchUserIDFetchHttpClient.Name = "temp_request";
        twitchUserIDFetchHttpClient.RequestCompleted += UserIDRequestCompleted;

        string[] headerParams =
        {
            "Authorization: Bearer " + twitchOAuth,
            "Client-Id: " + twitchClientID
        };

        Error err = twitchUserIDFetchHttpClient.Request(twitchUsersEndpoint, headerParams);

        if(err != Error.Ok)
        {
            twitchUserIDFetchHttpClient.QueueFree();
            twitchUserIDFetchHttpClient = null;

            twitchUserIDFetchTimeToRetry = twitchConnectRetryTime;
        }
    }

    private void UpdateUserID(double delta)
    {
        if(oAuthInProcess)
        {
            return;
        }

        if(twitchUserID == -1)
        {
            twitchUserIDFetchTimeToRetry -= delta;
            if(twitchUserIDFetchTimeToRetry < 0.0f)
            {
                // Try every 5 (by default) seconds
                twitchUserIDFetchTimeToRetry = twitchConnectRetryTime;
                FetchUserID();
            }
        }
    }
    #endregion

    #region PubSub
    private WebSocketPeer clientPubSub = new WebSocketPeer();
    private double clientPubSubTimeToReconnect = 0.0f;
    private double clientPubSubTimeToPing = 30.0f;

    private void ClientPubsubFailAndRestart(string errorMessage)
    {
        clientPubSubTimeToReconnect = 10.0f;
    }

    private void ClientPubSubHandleConnectionClosed(int peerID)
    {
        ClientPubsubFailAndRestart("Connection closed");
    }

    private void ClientPubSubHandleConnectionError(bool wasClean = false)
    {
        ClientPubsubFailAndRestart("Connection closed with error");
    }

    private void ClientPubSubSendPing()
    {
        // Send a ping! For funsies or something
        Dictionary pingJson = new Dictionary();
        pingJson.Add("type", "PING!");
        string pingData = Json.Stringify(pingJson);
        clientPubSub.SendText(pingData);
    }

    private void ClientPubSubHandleConnectionEstablished(int peerID)
    {
        // Send a ping! For funsies or something
        ClientPubSubSendPing();

        // Register for channel point redeems
        Dictionary eventRegistrationJson = new Dictionary();
        eventRegistrationJson.Add("type", "LISTEN");
        eventRegistrationJson.Add("nonce", "ChannelPoints");

        // Not sure how to do this inline in C#
        Dictionary jsonData = new Dictionary();
        jsonData.Add("topics", new string[] { "channel-points-channel-v1." + twitchUserID, "channel-bits-events-v1." + twitchUserID });
        jsonData.Add("auth_token", twitchOAuth);
        eventRegistrationJson.Add("data", jsonData);

        string eventRegistrationData = Json.Stringify(eventRegistrationJson);
        clientPubSub.SendText(eventRegistrationData);
    }

    private void ClientPubSubHandleRewardRedeemed(string title, string username, string displayname, string userInput)
    {
        EmitSignal(TwitchService.SignalName.ChannelPointsRedeem, title, username, displayname, userInput);
    }

    private void ClientPubSubHandleMessage(string topic, Dictionary message)
    {
        if(message.ContainsKey("type"))
        {
            if((string)message["type"] == "reward-redeemed")
            {
                string userInput = "";
                if(message["data"].As<Dictionary>()["redemption"].As<Dictionary>().TryGetValue("user-input", out Variant outUserInput))
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

        if((string)resultDict["type"] == "MESSAGE")
        {
            if (debugPackets)
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
        if(twitchClientID == "")
        {
            throw new ApplicationException("Twitch Client ID not set");
        }

        // Attempt connection
        Error err = clientPubSub.ConnectToUrl(twitchServiceURL);
        if(err != Error.Ok)
        {
            ClientPubsubFailAndRestart("Connection failed: " + err);
            return;
        }

        // Wait for the connection to be fully established
        clientPubSub.Poll();
        while(clientPubSub.GetReadyState() == WebSocketPeer.State.Connecting)
        {
            clientPubSub.Poll();
        }

        // Handle failed connections
        if(clientPubSub.GetReadyState() == WebSocketPeer.State.Closing)
        {
            return;
        }
        if(clientPubSub.GetReadyState() == WebSocketPeer.State.Closed)
        {
            return;
        }

        // Send subscription messages
        clientPubSub.Poll();
        ClientPubSubHandleConnectionEstablished(1);
        clientPubSub.Poll();
    }

    private void ClientPubSubUpdate(double delta)
    {
        if(twitchUserID == -1)
        {
            return;
        }

        clientPubSub.Poll();

        Error err = clientPubSub.GetPacketError();
        if(err != Error.Ok)
        {
            GD.PrintErr("ERROR!!!! ", err);
        }

        while(clientPubSub.GetAvailablePacketCount() > 0)
        {
            ClientPubSubHandleDataReceived();
            clientIRC.Poll();
        }

        // See if we need to reconnect
        if(clientPubSub.GetReadyState() == WebSocketPeer.State.Closed)
        {
            clientPubSubTimeToReconnect -= delta;

            if(clientPubSubTimeToReconnect < 0.0f)
            {
                // Reconnect to Twitch websocket
                ClientPubSubConnectToTwitch();

                // Whatever happens, set a default reconnect delay
                clientPubSubTimeToReconnect = 20.0f;
            }
            else
            {
                clientPubSubTimeToPing -= delta;
                if(clientPubSubTimeToPing < 0.0f)
                {
                    clientPubSubTimeToPing = twitchPubSubPingTime;
                    ClientPubSubSendPing();
                }
            }

            clientPubSub.Poll();
        }
    }
    #endregion

    #region IRC
    private WebSocketPeer clientIRC = new WebSocketPeer();
    private double clientIRCTimeToReconnect = 0.0f;

    private void ClientIRCFailAndRestart(string errorMessage)
    {
        clientIRCTimeToReconnect = 10.0f;
    }

    private void ClientIRCHandleConnectionClosed(bool wasClean = false)
    {
        ClientIRCFailAndRestart("Connection closed");
    }

    private void ClientIRCHandleConnectionError(bool wasClean = false)
    {
        ClientIRCFailAndRestart("Connection closed with error");
    }

    public void ClientIRCSend(string message)
    {
        clientIRC.SendText(message);
    }

    private void ClientIRCHandleConnectionEstablished(string proto = "")
    {
        // Send IRC handshaking messages
        ClientIRCSend("CAP REQ :twitch.tv/membership twitch.tv/tags twitch.tv/commands");
        ClientIRCSend("PASS oauth:" + twitchOAuth);
        ClientIRCSend("NICK " + twitchUsername);
        ClientIRCSend("JOIN #" + twitchUsername);
    }

    private Dictionary ParseIRCMessage(string message)
    {
        string[] splitMessage;
        Dictionary output = new Dictionary();
        output.Add("tags", new Dictionary());
        output.Add("prefix", "");
        output.Add("command", "");
        output.Add("params", new GDArray());

        // Parse tags
        if(message.Length > 0)
        {
            if(message[0] == '@')
            {
                splitMessage = message.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                string tagsStr = splitMessage[0].Substring(1);
                if(splitMessage.Length > 1)
                {
                    message = splitMessage[1];
                }
                else
                {
                    message = "";
                }

                string[] tagsPairStrs = tagsStr.Split(';');
                foreach(string tagPair in tagsPairStrs)
                {
                    string[] tagParts = tagPair.Split('=');
                    output["tags"].As<Dictionary>().Add(tagParts[0], tagParts[1]);
                }
            }
        }

        // Parse prefix, and chop it off from the message if it's there
        if(message.Length > 0)
        {
            if(message[0] == ':')
            {
                splitMessage = message.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                output["prefix"] = splitMessage[0].Substring(1);
                if(splitMessage.Length > 1)
                {
                    message = splitMessage[1];
                }
                else
                {
                    message = "";
                }
            }
        }

        if(output["prefix"].As<string>().Length > 0)
        {
            // Here are what I think are the three forms of prefix we might be
            // dealing with here:
            // - nick!user@host
            // - user@host (maybe?)
            // - host

            // Split on "!" to separate the nick from everything else. We might not
            // have a nick, but that's okay. We'll just leave the field blank.
            string[] prefixNickUser = output["prefix"].As<string>().Split('!', 2);
            string nick;
            string prefixUserHost;
            if(prefixNickUser.Length > 1)
            {
                nick = prefixNickUser[0];
                prefixUserHost = prefixNickUser[1];
            }
            else
            {
                nick = "";
                prefixUserHost = prefixNickUser[0];
            }

            // Split the user@host by "@" to get a user and host. It may also just
            // be a host, so if we only have one result from this, assume it's a host
            // with no user (message directly from server, etc).
            string[] prefixUserHostSplit = prefixUserHost.Split('@', 2);
            string user;
            string host;
            if(prefixUserHostSplit.Length > 1)
            {
                user = prefixUserHostSplit[0];
                host = prefixUserHostSplit[1];
            }
            else
            {
                user = "";
                host = prefixUserHostSplit[0];
            }

            output.Add("prefix_nick", nick);
            output.Add("prefix_host", host);
            output.Add("prefix_user", user);

            // Parse command, and chop it off from the message if it's there.
            if(message.Length > 0)
            {
                splitMessage = message.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                output["command"] = splitMessage[0];
                if(splitMessage.Length > 1)
                {
                    message = splitMessage[1];
                }
                else
                {
                    message = "";
                }
            }

            // Parse the parameters to the command
            while(message.Length > 0)
            {
                if(message[0] == ':')
                {
                    output["params"].As<GDArray>().Add(message.Substring(1));
                    message = "";
                }
                else
                {
                    splitMessage = message.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    output["params"].As<GDArray>().Add(splitMessage[0]);
                    if(splitMessage.Length > 1)
                    {
                        message = splitMessage[1];
                    }
                    else
                    {
                        message = "";
                    }
                }
            }
        }

        return output;
    }

    private void ClientIRCHandleDataReceived()
    {
        string packetText = clientIRC.GetPacket().GetStringFromUtf8();
        IRCInjectPacket(packetText);
    }

    private void IRCInjectPacket(string packetText)
    {
        // This might be multiple messages, separated by CRLF, so split it up.
        string[] ircMessages = packetText.Split("\r\n");

        foreach(string message in ircMessages)
        {
            if(message.Length > 0)
            {
                if (debugPackets)
                {
                    GD.Print("IRC: " + message);
                }
                Dictionary parsedMessage = ParseIRCMessage(message);
                string messageCommand = ((string)parsedMessage["command"]).ToLower();

                GD.Print("ParsedMessage: " + parsedMessage);

                // Just respond to pings right here
                if(messageCommand == "ping")
                {
                    ClientIRCSend("PONG :" + (string[])parsedMessage["params"].As<Dictionary>()[0]);
                }

                // Raids and other stuff that comes in by USERNOTICE
                if(messageCommand == "usernotice")
                {
                    if(parsedMessage["tags"].As<Dictionary>().ContainsKey("msg-id"))
                    {
                        string msgID = parsedMessage["tags"].As<Dictionary>()["msg-id"].ToString();
                        if (debugPackets)
                        {
                            GD.Print("Message ID: " + msgID);
                        }
                        if(msgID == "raid")
                        {
                            // Looks like we got an actual raid! Fire off the signal
                            string raiderUsername = parsedMessage["tags"].As<Dictionary>()["msg-param-login"].ToString();
                            string raiderDisplayname = parsedMessage["tags"].As<Dictionary>()["msg-param-displayName"].ToString();
                            string raiderUserCount = parsedMessage["tags"].As<Dictionary>()["msg-param-viewerCount"].ToString();
                            EmitSignal(TwitchService.SignalName.ChannelRaid, raiderUsername, raiderDisplayname, raiderUserCount);
                        }
                    }
                }

                // Handle incoming messages, including bit cheers
                if(messageCommand == "privmsg")
                {
                    string messageText = "";
                    if(parsedMessage["params"].As<GDArray>().Count > 1)
                    {
                        messageText = (string)((GDArray)parsedMessage["params"])[1];
                    }

                    // Make sure this is meant for us (for the channel)
                    if (parsedMessage["params"].As<GDArray>().Count > 0)
                    {
                        if((string)parsedMessage["params"].As<GDArray>()[0] == "#" + twitchUsername)
                        {
                            // Bit cheer message?
                            if(parsedMessage["tags"].As<Dictionary>().ContainsKey("bits"))
                            {
                                string cheererUsername = parsedMessage["prefix_user"].ToString();
                                string cheererDisplayName = parsedMessage["tags"].As<Dictionary>()["display-name"].ToString();
                                int bits = 0;
                                if(Int32.TryParse(parsedMessage["tags"].As<Dictionary>()["bits"].ToString(), out int parsedBits))
                                {
                                    bits = parsedBits;
                                }
                                string badgesString = parsedMessage["tags"].As<Dictionary>()["badges"].ToString();
                                TwitchBadge badges = TwitchBadge.None;
                                badges = badgesString.Contains("broadcaster") ? badges | TwitchBadge.Broadcaster : badges;
                                badges = badgesString.Contains("moderator") ? badges | TwitchBadge.Moderator : badges;
                                badges = badgesString.Contains("subscriber") ? badges | TwitchBadge.Subscriber : badges;
                                badges = badgesString.Contains("vip") ? badges | TwitchBadge.VIP : badges;
                                EmitSignal(TwitchService.SignalName.ChannelChatMessage, cheererUsername, cheererDisplayName, messageText, bits, (int)badges);
                            }
                            else
                            {
                                string cheererUsername = parsedMessage["prefix_user"].ToString();
                                string cheererDisplayName = parsedMessage["tags"].As<Dictionary>()["display-name"].ToString();
                                string badgesString = parsedMessage["tags"].As<Dictionary>()["badges"].ToString();
                                TwitchBadge badges = TwitchBadge.None;
                                badges = badgesString.Contains("broadcaster") ? badges | TwitchBadge.Broadcaster : badges;
                                badges = badgesString.Contains("moderator") ? badges | TwitchBadge.Moderator : badges;
                                badges = badgesString.Contains("subscriber") ? badges | TwitchBadge.Subscriber : badges;
                                badges = badgesString.Contains("vip") ? badges | TwitchBadge.VIP : badges;
                                EmitSignal(TwitchService.SignalName.ChannelChatMessage, cheererUsername, cheererDisplayName, messageText, 0, (int)badges);
                            }
                        }
                    }
                }
            }
        }
    }

    private void ClientIRCConnectToTwitch()
    {
        // If you hit this assert, it's because you never filled out the Twitch
        // client ID, which is specific to your application. If you want to find out
        // what it is for your app, you can find it in your app settings here:
        // https://dev.twitch.tv/console/apps

        if(twitchClientID == "")
        {
            throw new ApplicationException("Twitch Client ID not set");
        }

        Error err = clientIRC.ConnectToUrl(twitchIRCURL);
        if(err != Error.Ok)
        {
            ClientIRCFailAndRestart("Connection failed: " + err);
        }

        clientIRC.Poll();
        while(clientIRC.GetReadyState() == WebSocketPeer.State.Connecting)
        {
            clientIRC.Poll();
        }

        if(clientIRC.GetReadyState() == WebSocketPeer.State.Closed || clientIRC.GetReadyState() == WebSocketPeer.State.Closing)
        {
            return;
        }

        ClientIRCHandleConnectionEstablished("");
    }

    private void ClientIRCUpdate(double delta)
    {
        if(twitchUserID == -1)
        {
            return;
        }

        clientIRC.Poll();
        while(clientIRC.GetAvailablePacketCount() > 0)
        {
            ClientIRCHandleDataReceived();
            clientIRC.Poll();
        }

        // See if we need to reconnect
        if(clientIRC.GetReadyState() == WebSocketPeer.State.Closed)
        {
            clientIRCTimeToReconnect -= delta;

            if(clientIRCTimeToReconnect < 0.0f)
            {
                // Reconnect to Twitch websocket
                ClientIRCConnectToTwitch();

                // Whatever happens, etc a default reconnect delay
                clientIRCTimeToReconnect = 20.0f;
            }
        }
    }
    #endregion

    #region Config Management
    private void LoadConfig()
    {
        if(twitchConfigPath == "")
        {
            return;
        }

        ConfigFile config = new ConfigFile();
        Error err = config.Load(twitchConfigPath);
        if(err != Error.Ok)
        {
            return;
        }

        // Load the values, but default to whatever was there (export values that
        // may have been set in the editor)
        if(config.HasSectionKey("twitch", "twitch_username"))
        {
            twitchUsername = (string)config.GetValue("twitch", "twitch_username", twitchUsername);
        }
        if(config.HasSectionKey("twitch", "twitch_oauth_token"))
        {
            twitchOAuth = (string)config.GetValue("twitch", "twitch_oauth_token");
        }
    }

    private void SaveConfig()
    {
        if (twitchConfigPath == "")
        {
            return;
        }

        ConfigFile config = new ConfigFile();
        config.SetValue("twitch", "twitch_username", twitchUsername);
        config.SetValue("twitch", "twitch_oauth_token", twitchOAuth);
        config.Save(twitchConfigPath);
    }

    private void SetTwitchCredentials(string username, string oAuthToken)
    {
        if(username != null && username != "")
        {
            twitchUsername = username;
        }

        if(oAuthToken != null && oAuthToken != "")
        {
            twitchOAuth = oAuthToken;
        }

        if (autoSaveCredentials)
        {
            SaveConfig();
        }
    }
    #endregion

    #region Normal Node entry points
    public override void _Ready()
    {
        if(autoLoadCredentials)
        {
            LoadConfig();
        }

        if(autoSaveCredentials)
        {
            SaveConfig();
        }
    }

    public override void _Process(double delta)
    {
        if(pauseUpdates)
        {
            return;
        }

        // Check user ID
        UpdateUserID(delta);

        // Update PubSub
        ClientPubSubUpdate(delta);

        // Update IRC
        ClientIRCUpdate(delta);

        // Poll oauth
        PollOAuthServer();
    }
    #endregion
}
