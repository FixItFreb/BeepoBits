using Godot;
using System;

public partial class ChatCommandTest : ChatCommandNode
{
    public override void ExecuteCommand(ChatCommandPayload payload)
    {
        string testString = string.Format("{0} executed the testma command!", payload.displayname);
        GD.Print(testString);
        beepoCore.SendTwitchMessage(testString);
    }
}
