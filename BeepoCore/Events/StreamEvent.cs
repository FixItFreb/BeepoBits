using System;
using Godot;
using Godot.Collections;

public partial class DonationEvent : BeepoEvent
{
  public string user_name;
  public string type;
  public string message;
  public int total;
  public bool is_anonymous;
}

public partial class FollowEvent : BeepoEvent
{
  public string user_name;
}

public partial class RaidEvent : BeepoEvent
{
  public string user_name;
  public int viewers;

}

public partial class RedeemEvent : BeepoEvent
{
  public string user_name;
  public string title;
  public string input;
}

public partial class SubscriptionEvent : BeepoEvent
{
  public string user_name;
  public bool is_gift;
}