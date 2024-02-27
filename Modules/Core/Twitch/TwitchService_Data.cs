using System;
using Godot;
using Godot.Collections;

[Flags]
public enum TwitchBadge
{
    None = 0x0,
    Broadcaster = 0x1,
    Moderator = 0x2,
    Subscriber = 0x4,
    VIP = 0x8,
}

public abstract partial class TwitchBasePayload : RefCounted
{
    public static TwitchBadge GetBadgeFlags(TwitchBadgeStruct[] badges)
    {
        TwitchBadge badgeFlags = TwitchBadge.None;
        foreach(TwitchBadgeStruct b in badges)
        {
            switch(b.setid)
            {
                case "broadcaster":
                    badgeFlags |= TwitchBadge.Broadcaster;
                    break;
                case "moderator":
                    badgeFlags |= TwitchBadge.Moderator;
                    break;
                case "subscriber":
                    badgeFlags |= TwitchBadge.Subscriber;
                    break;
                case "vip":
                    badgeFlags |= TwitchBadge.VIP;
                    break;
            }
        }
        return badgeFlags;
    }

    public Dictionary rawPayload { get; private set; }

    public TwitchBasePayload(Dictionary _rawPayload)
    {
        rawPayload = _rawPayload;
    }
}

public partial class TwitchRedeemPayload : TwitchBasePayload
{
    public TwitchRedeemPayloadStruct data { get; private set; }

    public TwitchRedeemPayload(Dictionary _rawPayload) : base(_rawPayload)
    {
        data = new TwitchRedeemPayloadStruct(_rawPayload);
    }
}

public struct TwitchRedeemPayloadStruct
{
    public string id { get; private set; }
    public string broadcaster_user_id { get; private set; }
    public string broadcaster_user_login { get; private set; }
    public string broadcaster_user_name { get; private set; }
    public string user_id { get; private set; }
    public string user_login { get; private set; }
    public string user_name { get; private set; }
    public string user_input { get; private set; }
    public string status { get; private set; }
    public TwitchRewardStruct reward { get; private set; }
    public string redeemed_at { get; private set; }

    public TwitchRedeemPayloadStruct(Dictionary structData)
    {
        id = structData["id"].As<string>();
        broadcaster_user_id = structData["broadcaster_user_id"].As<string>();
        broadcaster_user_login = structData["broadcaster_user_login"].As<string>();
        broadcaster_user_name = structData["broadcaster_user_name"].As<string>();
        user_id = structData["user_id"].As<string>();
        user_login = structData["user_login"].As<string>();
        user_name = structData["user_name"].As<string>();
        user_input = structData["user_input"].As<string>();
        status = structData["status"].As<string>();
        reward = new TwitchRewardStruct(structData["reward"].As<Dictionary>());
        redeemed_at = structData["redeemed_at"].As<string>();
    }
}

public struct TwitchRewardStruct
{
    public string id { get; private set; }
    public string title { get; private set; }
    public int cost { get; private set; }
    public string prompt { get; private set; }

    public TwitchRewardStruct(Dictionary structData)
    {
        id = structData["id"].As<string>();
        title = structData["title"].As<string>();
        cost = structData["cost"].As<int>();
        prompt = structData["prompt"].As<string>();
    }
}

public partial class TwitchSubscriptionGiftPayload : TwitchBasePayload
{
    public TwitchSubscriptionGiftPayloadStruct data { get; private set; }

    public TwitchSubscriptionGiftPayload(Dictionary _rawPayload) : base(_rawPayload)
    {
        data = new TwitchSubscriptionGiftPayloadStruct(_rawPayload);
    }
}

public struct TwitchSubscriptionGiftPayloadStruct
{
    public string user_id { get; private set; }
    public string user_login { get; private set; }
    public string user_name { get; private set; }
    public string broadcaster_user_id { get; private set; }
    public string broadcaster_user_login { get; private set; }
    public string broadcaster_user_name { get; private set; }
    public int total { get; private set; }
    public string tier { get; private set; }
    public int cumulative_total { get; private set; }
    public bool is_anonymous { get; private set; }

    public TwitchSubscriptionGiftPayloadStruct(Dictionary structData)
    {
        broadcaster_user_id = structData["broadcaster_user_id"].As<string>();
        broadcaster_user_login = structData["broadcaster_user_login"].As<string>();
        broadcaster_user_name = structData["broadcaster_user_name"].As<string>();
        user_id = structData["user_id"].As<string>();
        user_login = structData["user_login"].As<string>();
        user_name = structData["user_name"].As<string>();
        total = structData["total"].As<int>();
        tier = structData["tier"].As<string>();
        is_anonymous = structData["is_anonymous"].As<bool>();
        if(!is_anonymous)
        {
            cumulative_total = structData["cumulative_total"].As<int>();
        }
        else
        {
            cumulative_total = 0;
        }
    }
}

public partial class TwitchCheerPayload : TwitchBasePayload
{
    public TwitchCheerPayloadStruct data { get; private set; }

    public TwitchCheerPayload(Dictionary _rawPayload) : base(_rawPayload)
    {
        data = new TwitchCheerPayloadStruct(_rawPayload);
    }
}

public struct TwitchCheerPayloadStruct
{
    public bool is_anonymous { get; private set; }
    public string user_id { get; private set; }
    public string user_login { get; private set; }
    public string user_name { get; private set; }
    public string broadcaster_user_id { get; private set; }
    public string broadcaster_user_login { get; private set; }
    public string broadcaster_user_name { get; private set; }
    public string message { get; private set; }
    public int bits { get; private set; }

    public TwitchCheerPayloadStruct(Dictionary structData)
    {
        is_anonymous = structData["is_anonymous"].As<bool>();
        if(!is_anonymous)
        {
            user_id = structData["user_id"].As<string>();
            user_login = structData["user_login"].As<string>();
            user_name = structData["user_name"].As<string>();
        }
        else
        {
            user_id = "";
            user_login = "anonymous";
            user_name = "Anonymous";
        }
        broadcaster_user_id = structData["broadcaster_user_id"].As<string>();
        broadcaster_user_login = structData["broadcaster_user_login"].As<string>();
        broadcaster_user_name = structData["broadcaster_user_name"].As<string>();
        message = structData["message"].As<string>();
        bits = structData["bits"].As<int>();
    }
}

public partial class TwitchRaidPayload : TwitchBasePayload
{
    public TwitchRaidPayloadStruct data { get; private set; }

    public TwitchRaidPayload(Dictionary _rawPayload) : base(_rawPayload)
    {
        data = new TwitchRaidPayloadStruct(_rawPayload);
    }
}

public struct TwitchRaidPayloadStruct
{
    public string from_broadcaster_user_id { get; private set; }
    public string from_broadcaster_user_login { get; private set; }
    public string from_broadcaster_user_name { get; private set; }
    public string to_broadcaster_user_id { get; private set; }
    public string to_broadcaster_user_login { get; private set; }
    public string to_broadcaster_user_name { get; private set; }
    public int viewers { get; private set; }

    public TwitchRaidPayloadStruct(Dictionary structData)
    {
        from_broadcaster_user_id = structData["from_broadcaster_user_id"].As<string>();
        from_broadcaster_user_login = structData["from_broadcaster_user_login"].As<string>();
        from_broadcaster_user_name = structData["from_broadcaster_user_name"].As<string>();
        to_broadcaster_user_id = structData["to_broadcaster_user_id"].As<string>();
        to_broadcaster_user_login = structData["to_broadcaster_user_login"].As<string>();
        to_broadcaster_user_name = structData["to_broadcaster_user_name"].As<string>();
        viewers = structData["viewers"].As<int>();
    }
}

public partial class TwitchFollowPayload : TwitchBasePayload
{
    public TwitchFollowPayloadStruct data { get; private set; }

    public TwitchFollowPayload(Dictionary _rawPayload) : base(_rawPayload)
    {
        data = new TwitchFollowPayloadStruct(_rawPayload);
    }
}

public struct TwitchFollowPayloadStruct
{
    public string user_id { get; private set; }
    public string user_login { get; private set; }
    public string user_name { get; private set; }
    public string broadcaster_user_id { get; private set; }
    public string broadcaster_user_login { get; private set; }
    public string broadcaster_user_name { get; private set; }
    public string followed_at { get; private set; }

    public TwitchFollowPayloadStruct(Dictionary structData)
    {
        user_id = structData["user_id"].As<string>();
        user_login = structData["user_login"].As<string>();
        user_name = structData["user_name"].As<string>();
        broadcaster_user_id = structData["broadcaster_user_id"].As<string>();
        broadcaster_user_login = structData["broadcaster_user_login"].As<string>();
        broadcaster_user_name = structData["broadcaster_user_name"].As<string>();
        followed_at = structData["followed_at"].As<string>();
    }
}

public partial class TwitchSubscriptionMessagePayload : TwitchBasePayload
{
    public TwitchSubscriptionMessagePayloadStruct data;
    public TwitchSubscriptionMessagePayload(Dictionary _rawPayload) : base(_rawPayload)
    {
        data = new TwitchSubscriptionMessagePayloadStruct(_rawPayload);
    }
}

public struct TwitchSubscriptionMessagePayloadStruct
{
    public string user_id { get; private set; }
    public string user_login { get; private set; }
    public string user_name { get; private set; }
    public string broadcaster_user_id { get; private set; }
    public string broadcaster_user_login { get; private set; }
    public string broadcaster_user_name { get; private set; }
    public string tier { get; private set; }
    public TwitchSubMessageStruct message { get; private set; }
    public int cumulative_months { get; private set; }
    public int streak_months { get; private set; }
    public int duration_months { get; private set; }

    public TwitchSubscriptionMessagePayloadStruct(Dictionary structData)
    {
        user_id = structData["user_id"].As<string>();
        user_login = structData["user_login"].As<string>();
        user_name = structData["user_name"].As<string>();
        broadcaster_user_id = structData["broadcaster_user_id"].As<string>();
        broadcaster_user_login = structData["broadcaster_user_login"].As<string>();
        broadcaster_user_name = structData["broadcaster_user_name"].As<string>();
        tier = structData["tier"].As<string>();
        message = new TwitchSubMessageStruct(structData["message"].As<Dictionary>());
        cumulative_months = structData["cumulative_months"].As<int>();
        streak_months = structData["streak_months"].As<int>();
        duration_months = structData["duration_months"].As<int>();
    }
}

public struct TwitchSubMessageStruct
{
    public string text { get; private set; }
    public TwitchSubEmoteStruct[] emotes { get; private set; }

    public TwitchSubMessageStruct(Dictionary structData)
    {
        text = structData["text"].As<string>();
        Array<Dictionary> emotesData = structData["emotes"].As<Array<Dictionary>>();
        emotes = new TwitchSubEmoteStruct[emotesData.Count];
        for (int i = 0; i < emotesData.Count; i++)
        {
            emotes[i] = new TwitchSubEmoteStruct(emotesData[i]);
        }
    }
}

public struct TwitchSubEmoteStruct
{
    public int begin { get; private set; }
    public int end { get; private set; }
    public string id { get; private set; }

    public TwitchSubEmoteStruct(Dictionary structData)
    {
        begin = structData["begin"].As<int>();
        end = structData["end"].As<int>();
        id = structData["id"].As<string>();
    }
}

public partial class TwitchChatMessagePayload : TwitchBasePayload
{
    public TwitchChatMessagePayloadStruct data;
    public TwitchChatMessagePayload(Dictionary _rawPayload) : base(_rawPayload)
    {
        data = new TwitchChatMessagePayloadStruct(_rawPayload);
    }
}

public struct TwitchChatMessagePayloadStruct
{
    public string broadcaster_user_id { get; private set; }
    public string broadcaster_user_name { get; private set; }
    public string broadcaster_user_login { get; private set; }
    public string chatter_user_id { get; private set; }
    public string chatter_user_name { get; private set; }
    public string chatter_user_login { get; private set; }
    public string message_id { get; private set; }
    public TwitchMessageStruct message { get; private set; }
    public string message_type { get; private set; }
    public TwitchBadgeStruct[] badges { get; private set; }
    public TwitchCheerStruct cheer { get; private set; }
    public string color { get; private set; }
    public TwitchReplyStruct reply { get; private set; }
    public string channel_points_custom_reward_id { get; private set; }

    public TwitchChatMessagePayloadStruct(Dictionary structData)
    {
        broadcaster_user_id = structData["broadcaster_user_id"].As<string>();
        broadcaster_user_name = structData["broadcaster_user_name"].As<string>();
        broadcaster_user_login = structData["broadcaster_user_login"].As<string>();
        chatter_user_id = structData["chatter_user_id"].As<string>();
        chatter_user_name = structData["chatter_user_name"].As<string>();
        chatter_user_login = structData["chatter_user_login"].As<string>();
        message_id = structData["message_id"].As<string>();
        message = new TwitchMessageStruct(structData["message"].As<Dictionary>());
        message_type = structData["message_type"].As<string>();
        Array<Dictionary> badgesData = structData["badges"].As<Array<Dictionary>>();
        badges = new TwitchBadgeStruct[badgesData.Count];
        for (int i = 0; i < badgesData.Count; i++)
        {
            badges[i] = new TwitchBadgeStruct(badgesData[i]);
        }
        cheer = new TwitchCheerStruct(structData["cheer"].As<Dictionary>());
        color = structData["color"].As<string>();
        reply = new TwitchReplyStruct(structData["reply"].As<Dictionary>());
        channel_points_custom_reward_id = structData["channel_points_custom_reward_id"].As<string>();
    }
}

public struct TwitchMessageStruct
{
    public string text { get; private set; }
    public TwitchFragmentStruct[] fragments { get; private set; }

    public TwitchMessageStruct(Dictionary structData)
    {
        text = structData["text"].As<string>();
        Array<Dictionary> fragmentsData = structData["fragments"].As<Array<Dictionary>>();
        fragments = new TwitchFragmentStruct[fragmentsData.Count];
        for (int i = 0; i < fragmentsData.Count; i++)
        {
            fragments[i] = new TwitchFragmentStruct(fragmentsData[i]);
        }
    }
}

public struct TwitchFragmentStruct
{
    public string type { get; private set; }
    public string text { get; private set; }
    public TwitchCheermoteStruct cheermote { get; private set; }
    public TwitchEmoteStruct emote { get; private set; }
    public TwitchMentionStruct mention { get; private set; }

    public TwitchFragmentStruct(Dictionary structData)
    {
        type = structData["type"].As<string>();
        text = structData["text"].As<string>();
        cheermote = new TwitchCheermoteStruct(structData["cheermote"].As<Dictionary>());
        emote = new TwitchEmoteStruct(structData["emote"].As<Dictionary>());
        mention = new TwitchMentionStruct(structData["mention"].As<Dictionary>());
    }
}

public struct TwitchCheermoteStruct
{
    public string prefix { get; private set; }
    public int bits { get; private set; }
    public int tier { get; private set; }

    public TwitchCheermoteStruct(Dictionary structData)
    {
        if (!structData.IsNullOrEmpty())
        {
            prefix = structData["prefix"].As<string>();
            bits = structData["bits"].As<int>();
            tier = structData["tier"].As<int>();
        }
        else
        {
            prefix = "";
            bits = 0;
            tier = 0;
        }
    }
}

public struct TwitchEmoteStruct
{
    public string id { get; private set; }
    public string emote_set_id { get; private set; }
    public string owner_id { get; private set; }
    public string format { get; private set; }

    public TwitchEmoteStruct(Dictionary structData)
    {
        if (!structData.IsNullOrEmpty())
        {
            id = structData["id"].As<string>();
            emote_set_id = structData["emote_set_id"].As<string>();
            owner_id = structData["owner_id"].As<string>();
            format = structData["format"].As<string>();
        }
        else
        {
            id = "";
            emote_set_id = "";
            owner_id = "";
            format = "";
        }
    }
}

public struct TwitchMentionStruct
{
    public string user_id { get; private set; }
    public string user_name { get; private set; }
    public string user_login { get; private set; }

    public TwitchMentionStruct(Dictionary structData)
    {
        if (!structData.IsNullOrEmpty())
        {
            user_id = structData["user_id"].As<string>();
            user_name = structData["user_name"].As<string>();
            user_login = structData["user_login"].As<string>();
        }
        else
        {
            user_id = "";
            user_name = "";
            user_login = "";
        }
    }
}

public struct TwitchBadgeStruct
{
    public string setid { get; private set; }
    public string id { get; private set; }
    public string info { get; private set; }

    public TwitchBadgeStruct(Dictionary structData)
    {
        setid = structData["set_id"].As<string>();
        id = structData["id"].As<string>();
        info = structData["info"].As<string>();
    }
}

public struct TwitchCheerStruct
{
    public int bits { get; private set; }

    public TwitchCheerStruct(Dictionary structData)
    {
        if (!structData.IsNullOrEmpty())
        {
            bits = structData["bits"].As<int>();
        }
        else
        {
            bits = 0;
        }
    }
}

public struct TwitchReplyStruct
{
    public string parent_message_id { get; private set; }
    public string parent_message_body { get; private set; }
    public string parent_user_id { get; private set; }
    public string parent_user_name { get; private set; }
    public string parent_user_login { get; private set; }
    public string thread_message_id { get; private set; }
    public string thread_user_id { get; private set; }
    public string thread_user_name { get; private set; }
    public string thread_user_login { get; private set; }

    public TwitchReplyStruct(Dictionary structData)
    {
        if (!structData.IsNullOrEmpty())
        {
            parent_message_id = structData["parent_message_id"].As<string>();
            parent_message_body = structData["parent_message_body"].As<string>();
            parent_user_id = structData["parent_user_id"].As<string>();
            parent_user_name = structData["parent_user_name"].As<string>();
            parent_user_login = structData["parent_user_login"].As<string>();
            thread_message_id = structData["thread_message_id"].As<string>();
            thread_user_id = structData["thread_user_id"].As<string>();
            thread_user_name = structData["thread_user_name"].As<string>();
            thread_user_login = structData["thread_user_login"].As<string>();
        }
        else
        {
            parent_message_id = "";
            parent_message_body = "";
            parent_user_id = "";
            parent_user_name = "";
            parent_user_login = "";
            thread_message_id = "";
            thread_user_id = "";
            thread_user_name = "";
            thread_user_login = "";
        }
    }
}