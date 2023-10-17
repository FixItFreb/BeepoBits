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
    public static float twitchConnectRetryTime = 5.0f;
    private static TwitchService _instance;
    public static TwitchService Instance { get { return _instance; } }

    // Config Variables

    /// <summary>
    ///  Client ID for the twitch application. Found here:
    ///  https://dev.twitch.tv/console/apps
    /// </summary>
    [Export] public string twitchClientID = "";

    /// <summary>
    /// NOTE: Whatever setting you put here will be clobbered by whatever is in the
    /// saved configuration file, so if you're modifying it directly (through the
    /// editor) instead of relying on saved credentials, you'll have to make sure the
    /// saved credentials file gets cleared out when you need a new token.
    /// </summary>
    [Export] public string twitchOAuth = "";

    /// <summary>
    /// To be filled out per-user.
    /// </summary>
    [Export] public string twitchUsername = "";

    /// <summary>
    /// Location to store config once it's set, so you don't have to go through the
    /// token generation flow all the time.
    /// </summary>
    [Export] public string twitchConfigPath = "user://twitch_config.ini";

    /// <summary>
    /// Automatically save credentials on startup and any time set_twitch_credentials
    /// is called.
    /// </summary>
    [Export] public bool autoSaveCredentials = true;

    /// <summary>
    /// Automatically load credentials when starting.
    /// </summary>
    [Export] public bool autoLoadCredentials = true;

    [Export] public bool debugPackets = true;
    [Export] public bool pauseUpdates = false;


    // Twitch Event Handlers

    /// <summary>
    /// Emitted when a user uses bits to cheer.
    /// </summary>
    /// <param name="fromUsername"></param>
    /// <param name="fromDisplayName"></param>
    /// <param name="message"></param>
    /// <param name="bits"></param>
    /// <param name="badges"></param>
    [Signal] public delegate void ChannelChatMessageEventHandler(string fromUsername, string fromDisplayName, string message, int bits, int badges);

    /// <summary>
    /// Emitted when a user redeems a channel point redeem.
    /// </summary>
    /// <param name="redeemTitle"></param>
    /// <param name="redeemerUsername"></param>
    /// <param name="redeemerDisplayName"></param>
    /// <param name="userInput"></param>
    [Signal] public delegate void ChannelPointsRedeemEventHandler(string redeemTitle, string redeemerUsername, string redeemerDisplayName, string userInput);

    /// <summary>
    /// Emitted when another user raids your channel.
    /// </summary>
    /// <param name="raiderUsername"></param>
    /// <param name="raiderDisplayName"></param>
    /// <param name="raiderUserCount"></param>
    [Signal] public delegate void ChannelRaidEventHandler(string raiderUsername, string raiderDisplayName, string raiderUserCount);

    /// <summary>
    /// Emitted when a user follows your channel.
    /// </summary>
    /// <param name="followerUsername"></param>
    /// <param name="followerDisplayName"></param>
    [Signal] public delegate void ChannelUserFollowedEventHandler(string followerUsername, string followerDisplayName);

    #region Individual Services
    public TwitchService_OAuth twitchServiceOAuth = new TwitchService_OAuth();
    public TwitchService_PubSub twitchServicePubSub = new TwitchService_PubSub();
    public TwitchService_IRC twitchServiceIRC = new TwitchService_IRC();
    public TwitchService_EventSub twitchServiceEventSub = new TwitchService_EventSub();
    public TwitchService_Users twitchServiceUsers = new TwitchService_Users();
    public TwitchService_Emotes twitchServiceEmotes = new TwitchService_Emotes();
    #endregion

    # region Constants
    public static string twitchUsersEndpoint = "https://api.twitch.tv/helix/users";
    #endregion

    #region User ID fetch
    private int twitchUserID = -1;
    public int TwitchUserID { get { return twitchUserID; } }

    private double twitchUserIDFetchTimeToRetry = 0.0f;
    private HttpRequest twitchUserIDFetchHttpClient = null;

    private void UserIdRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
    {
        Dictionary parsedResult = Json.ParseString(body.GetStringFromUtf8()).AsGodotDictionary();

        // If we get an authorization error, we need to re-do the oauth setup.
        if (responseCode == 401)
        {
            twitchServiceOAuth.StartOAuthProcess();
            if (twitchUserIDFetchHttpClient != null)
            {
                twitchUserIDFetchHttpClient.QueueFree();
                twitchUserIDFetchHttpClient = null;
                return;
            }
        }

        // Get the user ID and login from the incoming Twitch data
        twitchUserID = -1;
        Array<Dictionary> parsedResultData = parsedResult["data"].AsGodotArray<Dictionary>();
        foreach (Dictionary user in parsedResultData)
        {
            twitchUserID = (int)user["id"];
            SetTwitchCredentials((string)user["login"], twitchOAuth);
            break;
        }

        // Clean up
        if (twitchUserIDFetchHttpClient != null)
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
        twitchUserIDFetchHttpClient.RequestCompleted += UserIdRequestCompleted;

        string[] headerParams =
        {
            "Authorization: Bearer " + twitchOAuth,
            "Client-Id: " + twitchClientID
        };

        Error err = twitchUserIDFetchHttpClient.Request(twitchUsersEndpoint, headerParams);

        if (err != Error.Ok)
        {
            twitchUserIDFetchHttpClient.QueueFree();
            twitchUserIDFetchHttpClient = null;
        }

        twitchUserIDFetchTimeToRetry = twitchConnectRetryTime;
    }

    private void UpdateUserID(double delta)
    {
        if (twitchServiceOAuth.oAuthInProcess)
        {
            return;
        }

        if (twitchUserID == -1)
        {
            twitchUserIDFetchTimeToRetry -= delta;
            if (twitchUserIDFetchTimeToRetry < 0.0f)
            {
                // Try every 5 (by default) seconds
                twitchUserIDFetchTimeToRetry = twitchConnectRetryTime;
                FetchUserID();
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
    public override void _EnterTree()
    {
        _instance = this;
    }
    
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

        twitchServiceUsers.Init(this);
        twitchServiceOAuth.Init(this);
        twitchServicePubSub.Init(this);
        twitchServiceIRC.Init(this);
        twitchServiceEventSub.Init(this);
        twitchServiceEmotes.Init(this);
    }

    public override void _Process(double delta)
    {
        if (pauseUpdates)
        {
            return;
        }

        // Check user ID
        UpdateUserID(delta);

        // Update PubSub
        twitchServicePubSub.ClientPubSubUpdate(delta);

        // Update IRC
        twitchServiceIRC.ClientIRCUpdate(delta);

        // Update EventSub
         twitchServiceEventSub.ClientEventSubUpdate(delta);

        // Poll oauth
        twitchServiceOAuth.PollOAuthServer();

        // Update users
        twitchServiceUsers.Update(delta);

        // Emotes
        twitchServiceEmotes.Update(delta);
    }
    #endregion
}
