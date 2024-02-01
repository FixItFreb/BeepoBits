
using System;
using Godot;
using Godot.Collections;

public partial class TwitchService_Emotes : RefCounted
{
    private TwitchService twitchService = null;

    public static readonly string TwitchEmotesEndpoint = "https://api.twitch.tv/helix/chat/emotes?broadcaster_id=";

    public Dictionary<string, SpriteFrames> cachedEmotes = new Dictionary<string, SpriteFrames>();
    private HttpRequest twitchEmotesFetchHttpClient = null;

    private bool hasCachedEmotes = false;

    public void Init(TwitchService newTwitchService)
    {
        twitchService = newTwitchService;
    }

    public void CacheChannelEmotes(string channelID, string emoteID = "")
    {
        twitchEmotesFetchHttpClient = new HttpRequest();
        twitchEmotesFetchHttpClient.Name = "temp_request";
        twitchService.AddChild(twitchEmotesFetchHttpClient);
        //twitchEmotesFetchHttpClient.RequestCompleted += EmotesFetchRequestCompleted;

        string[] headerParams = new string[] {
            "Authorization: Bearer " + twitchService.twitchOAuth,
            "Client-Id: " + twitchService.twitchClientID,
        };

        if (twitchService.debugPackets)
        {
            BeepoCore.DebugLog("Fetching Emotes: " + TwitchEmotesEndpoint + channelID);
        }

        Error err = twitchEmotesFetchHttpClient.Request(TwitchEmotesEndpoint + channelID, headerParams, HttpClient.Method.Get);
        twitchEmotesFetchHttpClient.Connect(HttpRequest.SignalName.RequestCompleted, Callable.From((long result, long responseCode, string[] headers, byte[] body) =>
        {
            Dictionary parsedResult = Json.ParseString(body.GetStringFromUtf8()).AsGodotDictionary();
            string parsedString = Json.Stringify(parsedResult);

            if (twitchService.debugPackets)
            {
                BeepoCore.DebugLog("Emotes Fetch - " + parsedString);
            }

            Array<Dictionary> data = parsedResult["data"].As<Array<Dictionary>>();
            foreach (Dictionary emote in data)
            {
                string emoteName = emote["name"].As<string>();
                bool isAnimated = emote["format"].As<string[]>().Length > 1;
                string emoteAddress = emote["images"].As<Dictionary>()["url_2x"].As<string>();
                if (emoteID.Length == 0 || emoteID == emoteName)
                {
                    //TwitchEmote.CacheEmote(emoteName, isAnimated, emoteAddress);
                }
            }

            // If we get an authorization error, we need to re-do the oauth setup.
            if (responseCode == 401)
            {
                twitchService.twitchServiceOAuth.StartOAuthProcess();
                return;
            }
        }));
    }

    // private void EmotesFetchRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
    // {
    //     Dictionary parsedResult = Json.ParseString(body.GetStringFromUtf8()).AsGodotDictionary();
    //     string parsedString = Json.Stringify(parsedResult);

    //     if (twitchService.debugPackets)
    //     {
    //         GD.Print("Emotes Fetch - " + parsedString);
    //     }

    //     Array<Dictionary> data = parsedResult["data"].As<Array<Dictionary>>();
    //     foreach (Dictionary emote in data)
    //     {
    //         string emoteName = emote["name"].As<string>();
    //         bool isAnimated = emote["format"].As<string[]>().Length > 1;
    //         string emoteAddress = emote["images"].As<Dictionary>()["url_4x"].As<string>();
    //         TwitchEmote.CacheEmote(emoteName, isAnimated, emoteAddress);
    //     }

    //     // If we get an authorization error, we need to re-do the oauth setup.
    //     if (responseCode == 401)
    //     {
    //         twitchService.twitchServiceOAuth.StartOAuthProcess();
    //         return;
    //     }
    // }

    public void Update(double delta)
    {
        if (twitchService.TwitchUserID == -1)
        {
            return;
        }

        if(!hasCachedEmotes)
        {
            hasCachedEmotes = true;
            CacheChannelEmotes(twitchService.TwitchUserID.ToString());
        }
    }
}