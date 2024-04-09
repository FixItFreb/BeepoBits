
using System;
using Godot;
using Godot.Collections;

public class TwitchEvent
{
  public static StreamEvent BuildStreamEvent(string eventType, Dictionary payload)
  {
    StreamEvent newEvent = new()
    {
      EventDomainID = "StreamEvents",
      data = new(),
    };

    switch (eventType)
    {
      case "channel.follow":
        newEvent.type = "follow";
        newEvent.data["user_name"] = payload["user_name"].As<string>();
        newEvent.data["decorators"] = new Array<Dictionary>();
        break;
      case "channel.subscribe":
        newEvent.type = "subscription";
        newEvent.data["user_name"] = payload["user_name"].As<string>();
        newEvent.data["is_gift"] = payload["is_gift"].As<bool>();
        newEvent.data["decorators"] = new Array<Dictionary>();
        break;
      case "channel.subscription.message":
        newEvent.type = "subscription";
        newEvent.data["user_name"] = payload["user_name"].As<string>();
        newEvent.data["is_gift"] = false;
        newEvent.data["decorators"] = ParseSubMessageDecorators(payload["message"].As<Dictionary>());
        break;
      case "channel.subscription.gift":
        newEvent.type = "donation";
        newEvent.data["user_name"] = payload["user_name"].As<string>();
        newEvent.data["total"] = payload["total"].As<int>();
        newEvent.data["message"] = "";
        newEvent.data["is_anonymous"] = payload["is_anonymous"].As<bool>();
        newEvent.data["decorators"] = ParseGiftSubDecorators(payload);
        break;
      case "channel.cheer":
        newEvent.type = "donation";
        newEvent.data["user_name"] = payload["user_name"].As<string>();
        newEvent.data["total"] = payload["bits"].As<int>();
        newEvent.data["message"] = payload["message"].As<string>();
        newEvent.data["is_anonymous"] = payload["is_anonymous"].As<bool>();
        newEvent.data["decorators"] = ParseCheerDecorators(payload);
        break;
      case "channel.chat.message":
        newEvent.type = "message";
        newEvent.data["user_name"] = payload["chatter_user_name"].As<string>();
        newEvent.data["message"] = payload["message"].As<Dictionary>()["text"].As<string>();
        newEvent.data["decorators"] = ParseMessageDecorators(payload);
        break;
      case "channel.channel_points_custom_reward_redemption.add":
        newEvent.type = "redeem";
        newEvent.data["user_name"] = payload["user_name"].As<string>();
        newEvent.data["title"] = payload["reward"].As<Dictionary>()["title"].As<string>();
        newEvent.data["user_input"] = payload["user_input"].As<string>();
        newEvent.data["decorators"] = new Array<Dictionary>();
        break;
      case "channel.raid":
        newEvent.type = "raid";
        newEvent.data["user_name"] = payload["from_broadcaster_user_name"].As<string>();
        newEvent.data["viewers"] = payload["viewers"].As<int>();
        newEvent.data["decorators"] = new Array<Dictionary>();
        break;
    }

    GD.Print("Got: " + payload);
    GD.Print("Event: " + newEvent.data);

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

      if (fragment_type == "emote")
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