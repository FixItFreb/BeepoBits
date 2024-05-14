
using System;
using Godot;
using Godot.Collections;

public class TwitchEvent
{
  public static BeepoEvent BuildStreamEvent(string eventType, Dictionary payload)
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

  private static Array<Dictionary> ParseSubMessageDecorators(Dictionary message)
  {
    Array<Dictionary> decorators = new();
    Dictionary text = new();
    text["type"] = "twitch_text";
    text["text"] = message["text"].As<string>();
    decorators.Add(text);

    var fragments = message["emotes"].As<Array<Dictionary>>();
    foreach (var fragment in fragments)
    {
      Dictionary emote = new();
      emote["type"] = "twitch_emote";
      emote["begin"] = fragment["begin"].As<string>();
      emote["end"] = fragment["end"].As<string>();
      emote["id"] = fragment["id"].As<string>();

      decorators.Add(emote);
    }

    return decorators;
  }

  private static Array<Dictionary> ParseGiftSubDecorators(Dictionary payload)
  {
    Array<Dictionary> decorators = new();
    Dictionary donationType = new();
    donationType["type"] = "twitch_donation_type";
    donationType["donation_type"] = "gift_sub";
    decorators.Add(donationType);

    return decorators;
  }

  private static Array<Dictionary> ParseCheerDecorators(Dictionary payload)
  {
    Array<Dictionary> decorators = new();
    Dictionary donationType = new();
    donationType["type"] = "twitch_donation_type";
    donationType["donation_type"] = "cheer";
    decorators.Add(donationType);

    return decorators;
  }

  private static Array<Dictionary> ParseMessageDecorators(Dictionary payload)
  {
    Array<Dictionary> decorators = new();

    var cursor = 0;
    var fragments = payload["message"].As<Dictionary>()["fragments"].As<Array<Dictionary>>();
    foreach (var fragment in fragments)
    {
      var fragment_type = fragment["type"].As<String>();
      if (fragment_type == "text" || fragment_type == "mention")
      {
        cursor += fragment["text"].As<string>().Length;
      }

      if (fragment_type == "emote")
      {
        Dictionary decorator = new();

        decorator["type"] = "twitch_emote";
        decorator["begin"] = cursor;
        cursor += fragment["text"].As<string>().Length;
        decorator["end"] = cursor;
        decorator["id"] = fragment["emote"].As<Dictionary>()["id"].As<string>();

        decorators.Add(decorator);
      }

      if (fragment_type == "cheermote")
      {
        Dictionary decorator = new();

        decorator["type"] = "twitch_cheer";
        decorator["begin"] = cursor;
        cursor += fragment["text"].As<string>().Length;
        decorator["end"] = cursor;
        decorator["total"] = fragment["cheermote"].As<Dictionary>()["bits"].As<string>();

        decorators.Add(decorator);
      }
    }

    var badges = payload["badges"].As<Array<Dictionary>>();
    foreach (var badge in badges)
    {
      Dictionary decorator = new();

      decorator["type"] = "twitch_badge";
      decorator["set_id"] = badge["set_id"];

      decorators.Add(decorator);
    }

    return decorators;
  }
}