using System;
using Godot;
using Godot.Collections;
using GDArray = Godot.Collections.Array;

public partial class TwitchService_IRC : RefCounted
{
    private TwitchService twitchService;

    // IRC (over websocket) connection target.
    public static readonly string TwitchIRCURL = "wss://irc-ws.chat.twitch.tv";

    private WebSocketPeer clientIRC = new WebSocketPeer();
    private double clientIRCTimeToReconnect = 0.0;
    private double clientIRCTimeToReconnectDefault = 20.0;

    public void Init(TwitchService newTwitchService)
    {
        twitchService = newTwitchService;
    }

    private void ClientIRCFailAndRestart(string errorMessage)
    {
        clientIRCTimeToReconnect = clientIRCTimeToReconnectDefault;
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
        ClientIRCSend("PASS oauth:" + twitchService.twitchOAuth);
        ClientIRCSend("NICK " + twitchService.twitchUsername);
        ClientIRCSend("JOIN #" + twitchService.twitchUsername);
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
        if (message.Length > 0)
        {
            if (message[0] == '@')
            {
                splitMessage = message.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                string tagsStr = splitMessage[0].Substring(1);
                if (splitMessage.Length > 1)
                {
                    message = splitMessage[1];
                }
                else
                {
                    message = "";
                }

                string[] tagsPairStrs = tagsStr.Split(';');
                foreach (string tagPair in tagsPairStrs)
                {
                    string[] tagParts = tagPair.Split('=');
                    output["tags"].As<Dictionary>().Add(tagParts[0], tagParts[1]);
                }
            }
        }

        // Parse prefix, and chop it off from the message if it's there
        if (message.Length > 0)
        {
            if (message[0] == ':')
            {
                splitMessage = message.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                output["prefix"] = splitMessage[0].Substring(1);
                if (splitMessage.Length > 1)
                {
                    message = splitMessage[1];
                }
                else
                {
                    message = "";
                }
            }
        }

        if (output["prefix"].As<string>().Length > 0)
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
            if (prefixNickUser.Length > 1)
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
            if (prefixUserHostSplit.Length > 1)
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
            if (message.Length > 0)
            {
                splitMessage = message.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                output["command"] = splitMessage[0];
                if (splitMessage.Length > 1)
                {
                    message = splitMessage[1];
                }
                else
                {
                    message = "";
                }
            }

            // Parse the parameters to the command
            while (message.Length > 0)
            {
                if (message[0] == ':')
                {
                    output["params"].As<GDArray>().Add(message.Substring(1));
                    message = "";
                }
                else
                {
                    splitMessage = message.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    output["params"].As<GDArray>().Add(splitMessage[0]);
                    if (splitMessage.Length > 1)
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

        foreach (string message in ircMessages)
        {
            if (message.Length > 0)
            {
                if (twitchService.debugPackets)
                {
                    // BeepoCore.DebugLog("IRC: " + message);
                }
                Dictionary parsedMessage = ParseIRCMessage(message);
                string messageCommand = ((string)parsedMessage["command"]).ToLower();

                //GD.Print("ParsedMessage: " + parsedMessage);

                // Just respond to pings right here
                if (messageCommand == "ping")
                {
                    ClientIRCSend("PONG :" + (string[])parsedMessage["params"].As<Dictionary>()[0]);
                }

                // Raids and other stuff that comes in by USERNOTICE
                if (messageCommand == "usernotice")
                {
                    if (parsedMessage["tags"].As<Dictionary>().ContainsKey("msg-id"))
                    {
                        string msgID = parsedMessage["tags"].As<Dictionary>()["msg-id"].ToString();
                        if (twitchService.debugPackets)
                        {
                            // BeepoCore.DebugLog("Message ID: " + msgID);
                        }
                        if (msgID == "raid")
                        {
                            // Looks like we got an actual raid! Fire off the signal
                            string raiderUsername = parsedMessage["tags"].As<Dictionary>()["msg-param-login"].ToString();
                            string raiderDisplayname = parsedMessage["tags"].As<Dictionary>()["msg-param-displayName"].ToString();
                            string raiderUserCount = parsedMessage["tags"].As<Dictionary>()["msg-param-viewerCount"].ToString();
                            twitchService.EmitSignal(TwitchService.SignalName.ChannelRaid, raiderUsername, raiderDisplayname, raiderUserCount);
                        }
                    }
                }

                // Handle incoming messages, including bit cheers
                if (messageCommand == "privmsg")
                {
                    string messageText = "";
                    if (parsedMessage["params"].As<GDArray>().Count > 1)
                    {
                        messageText = (string)((GDArray)parsedMessage["params"])[1];
                    }

                    // Make sure this is meant for us (for the channel)
                    if (parsedMessage["params"].As<GDArray>().Count > 0)
                    {
                        if ((string)parsedMessage["params"].As<GDArray>()[0] == "#" + twitchService.twitchUsername)
                        {
                            // Bit cheer message?
                            if (parsedMessage["tags"].As<Dictionary>().ContainsKey("bits"))
                            {
                                string cheererUsername = parsedMessage["prefix_user"].ToString();
                                string cheererDisplayName = parsedMessage["tags"].As<Dictionary>()["display-name"].ToString();
                                int bits = 0;
                                if (Int32.TryParse(parsedMessage["tags"].As<Dictionary>()["bits"].ToString(), out int parsedBits))
                                {
                                    bits = parsedBits;
                                }
                                string badgesString = parsedMessage["tags"].As<Dictionary>()["badges"].ToString();
                                TwitchBadge badges = TwitchBadge.None;
                                badges = badgesString.Contains("broadcaster") ? badges | TwitchBadge.Broadcaster : badges;
                                badges = badgesString.Contains("moderator") ? badges | TwitchBadge.Moderator : badges;
                                badges = badgesString.Contains("subscriber") ? badges | TwitchBadge.Subscriber : badges;
                                badges = badgesString.Contains("vip") ? badges | TwitchBadge.VIP : badges;
                                twitchService.EmitSignal(TwitchService.SignalName.ChannelChatMessage, cheererUsername, cheererDisplayName, messageText, bits, (int)badges);
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
                                twitchService.EmitSignal(TwitchService.SignalName.ChannelChatMessage, cheererUsername, cheererDisplayName, messageText, 0, (int)badges);
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

        if (twitchService.twitchClientID == "")
        {
            throw new ApplicationException("Twitch Client ID not set");
        }

        Error err = clientIRC.ConnectToUrl(TwitchIRCURL);
        if (err != Error.Ok)
        {
            ClientIRCFailAndRestart("Connection failed: " + err);
        }

        clientIRC.Poll();
        while (clientIRC.GetReadyState() == WebSocketPeer.State.Connecting)
        {
            clientIRC.Poll();
        }

        if (clientIRC.GetReadyState() == WebSocketPeer.State.Closed || clientIRC.GetReadyState() == WebSocketPeer.State.Closing)
        {
            return;
        }

        ClientIRCHandleConnectionEstablished("");
    }

    public void ClientIRCUpdate(double delta)
    {
        if (twitchService.TwitchUserID == -1)
        {
            return;
        }

        clientIRC.Poll();
        while (clientIRC.GetAvailablePacketCount() > 0)
        {
            ClientIRCHandleDataReceived();
            clientIRC.Poll();
        }

        // See if we need to reconnect
        if (clientIRC.GetReadyState() == WebSocketPeer.State.Closed)
        {
            clientIRCTimeToReconnect -= delta;

            if (clientIRCTimeToReconnect < 0.0f)
            {
                // Reconnect to Twitch websocket
                ClientIRCConnectToTwitch();

                // Whatever happens, etc a default reconnect delay
                clientIRCTimeToReconnect = clientIRCTimeToReconnectDefault;
            }
        }
    }
}