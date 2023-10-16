using Godot;
using System;

public partial class TwitchService_OAuth : RefCounted
{
    private TwitchService twitchService = null;

    public bool oAuthInProcess = false;
    public TcpServer oAuthTCPServer = null;

    private StreamPeerTcp oAuthStreamPeerTCP = null;
    private string oAuthStreamPeerTCPInputBuffer = "";

    private string twitchRedirectAddress = "localhost";
    private ushort twitchRedirectPort = 8080;

    public void Init(TwitchService parentTwitchService)
    {
        twitchService = parentTwitchService;
    }

    public void StopOAuthProcess()
    {
        if (oAuthTCPServer != null)
        {
            oAuthTCPServer.Stop();
            oAuthTCPServer = null;
        }

        if (oAuthStreamPeerTCP != null)
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

    public void PollOAuthServer()
    {
        if(!oAuthInProcess)
        {
            return;
        }

        // Accept incoming connections.
        if(oAuthTCPServer != null)
        {
            if(oAuthTCPServer.IsConnectionAvailable())
            {
                oAuthStreamPeerTCP = oAuthTCPServer.TakeConnection();
            }
        }

        // Add any new incoming bytes to our input buffer.
        if(oAuthStreamPeerTCP != null)
        {
            while(oAuthStreamPeerTCP.GetAvailableBytes() > 0)
            {
                string incomingByte = oAuthStreamPeerTCP.GetUtf8String(1);
                if(incomingByte != "\r")
                {
                    oAuthStreamPeerTCPInputBuffer += incomingByte;
                }
            }
        }

        // Only act on stuff once we have two newlines at the end of a request.
        if(oAuthStreamPeerTCPInputBuffer.EndsWith("\n\n"))
        {
            // For each line...
            while(oAuthStreamPeerTCPInputBuffer.Contains("\n"))
            {
                // Take the line and pop it out of the buffer.
                string getLine = oAuthStreamPeerTCPInputBuffer.Split("\n", true)[0];
                oAuthStreamPeerTCPInputBuffer = oAuthStreamPeerTCPInputBuffer.Substring(getLine.Length + 1);

                // All we care about here is the GET line
                if (getLine.StartsWith("GET "))
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
                    else if (httpGetPath.Contains("?"))
                    {
                        string htmlResponse = @"<html><head></head><body>You may now close this window.</body></html>";

                        // Attempt to extract the access token from the GET data.
                        string[] pathParts = httpGetPath.Split("?");
                        if (pathParts.Length > 1)
                        {
                            string parameters = pathParts[1];
                            string[] argList = parameters.Split("&");
                            foreach (string arg in argList)
                            {
                                string[] argParts = arg.Split("=");
                                if (argParts.Length > 1)
                                {
                                    if (argParts[0] == "access_token")
                                    {
                                        twitchService.twitchOAuth = argParts[1];
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

    public void StartOAuthProcess()
    {
        oAuthInProcess = true;

        // Kill any existing websocket server
        if (oAuthTCPServer != null)
        {
            oAuthTCPServer.Stop();
            oAuthTCPServer = null;
        }

        // Fire up a new server
        oAuthTCPServer = new TcpServer();
        oAuthTCPServer.Listen(twitchRedirectPort, "127.0.0.1");

        // Check client ID to make sure we aren't about to do something we'll regret
        byte[] asciiTwitchID = twitchService.twitchClientID.ToAsciiBuffer();
        foreach (byte k in asciiTwitchID)
        {
            // Make sure we're only using alphanumeric values
            if ((k >= 65 && k <= 90) || (k >= 97 && k <= 122) || (k >= 48 && k <= 57))
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

        string[] scopeArray = new string[]{
            "channel:read:redemptions",
            "chat:read",
            "bits:read",
            "channel:read:subscriptions",
            "moderator:read:followers",
            "bits:read"
        };

        string scopeStr = String.Join(" ", scopeArray);
        scopeStr = scopeStr.URIEncode();

        string oAuthURL = string.Format(@"https://id.twitch.tv/oauth2/authorize?response_type=token&client_id={0}&redirect_uri=http://{1}:{2}/&scope={3}",
        twitchService.twitchClientID, twitchRedirectAddress, twitchRedirectPort, scopeStr);
        GD.Print("OAuthUrl: " + oAuthURL);
        OS.ShellOpen(oAuthURL);
    }
}
