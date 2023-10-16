using Godot;
using Godot.Collections;
using System.Collections.Generic;

public partial class TwitchService_Users : RefCounted
{
    // Twitch user data endpoint. We'll use this to fetch a user ID based on the username.
    public static readonly string twitchUsersEndpoint = "https://api.twitch.tv/helix/users";

    private TwitchService twitchService = null;

    private double twitchUserIDFetchTimeToRetry = 0.0;
    private HttpRequest twitchUserIDFetchHttpClient = null;

    private double TwitchUserIDFetchTimeToRetryDefault = 5.0;

    private List<string> userRequestQueue = new List<string>();
    private Dictionary cachedUserData = new Dictionary();

    public void Init(TwitchService newTwitchService)
    {
        twitchService = newTwitchService;
    }

    private void UserIDRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
    {
        Dictionary parsedResult = Json.ParseString(body.GetStringFromUtf8()).AsGodotDictionary();

        foreach(Dictionary user in parsedResult["data"].AsGodotArray<Dictionary>())
        {
            cachedUserData[user["login"]] = user;
        }

        // Clean up
        if (twitchUserIDFetchHttpClient != null)
        {
            twitchUserIDFetchHttpClient.QueueFree();
            twitchUserIDFetchHttpClient = null;
        }

        twitchUserIDFetchTimeToRetry = TwitchUserIDFetchTimeToRetryDefault;
    }

    // Determine the user ID of the user who's authorized this
    private void FetchUserID(string userLogin = "", int userID = -1)
    {
        if(twitchUserIDFetchHttpClient != null)
        {
            // Request already in-flight.
            return;
        }

        twitchUserIDFetchHttpClient = new HttpRequest();
        twitchUserIDFetchHttpClient.Name = "temp_request";
        twitchService.AddChild(twitchUserIDFetchHttpClient);
        twitchUserIDFetchHttpClient.RequestCompleted += UserIDRequestCompleted;

        string[] headerParams =
{
            "Authorization: Bearer " + twitchService.twitchOAuth,
            "Client-Id: " + twitchService.twitchClientID
        };

        string paramsString = "";
        if(userLogin.Length > 0)
        {
            paramsString += "login=" + userLogin.URIEncode();
        }
        if(userID != -1)
        {
            if(paramsString.Length > 0)
            {
                paramsString += "&";
            }
            paramsString += "id=" + userID.ToString();
        }

        Error err = twitchUserIDFetchHttpClient.Request(twitchUsersEndpoint + "?" + paramsString, headerParams);

        if(err != Error.Ok)
        {
            twitchUserIDFetchHttpClient.QueueFree();
            twitchUserIDFetchHttpClient = null;
        }

        twitchUserIDFetchTimeToRetry = TwitchUserIDFetchTimeToRetryDefault;
    }

    public void AddLookupRequest(string userLogin)
    {
        userRequestQueue.Add(userLogin);
    }

    public void Update(double delta)
    {
        if(twitchUserIDFetchHttpClient != null)
        {
            return;
        }

        if(userRequestQueue.Count > 0)
        {
            string nextUser = userRequestQueue[0];
            userRequestQueue.RemoveAt(0);
            if(!cachedUserData.ContainsKey(nextUser))
            {
                FetchUserID(nextUser);
            }
        }
    }

    private Dictionary CheckCachedUserData(Dictionary userLogin)
    {
        if(cachedUserData.ContainsKey(userLogin))
        {
            return cachedUserData[userLogin].As<Dictionary>();
        }
        return null;
    }
}