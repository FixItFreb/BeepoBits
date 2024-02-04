using Godot;
using System;

public partial class ChatCommandTest : ChatCommandNode
{
    public override void ExecuteCommand(TwitchChatMessagePayload payload, string[] commandParams)
    {
        if(!CheckPermissions(payload))
        {
            return;
        }

        string paramString = "";
        for (int i = 0; i < commandParams.Length; i++)
        {
            paramString += " " + commandParams[i];
        }
        string testString;
        if (paramString.Length > 0)
        {
            testString = string.Format("{0} executed the testma command with params:{1}", payload.data.chatter_user_name, paramString);
        }
        else
        {
            testString = string.Format("{0} executed the testma command!", payload.data.chatter_user_name);
        }

        TrailsManager.Instance.TriggerSet("FingersL");
        TrailsManager.Instance.TriggerSet("FingersR");

        BeepoCore.DebugLog(testString);
        GD.Print(payload.rawPayload);
    }
}
