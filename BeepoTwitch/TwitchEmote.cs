using Godot;

public partial class TwitchEmote : Resource
{
    public static void FetchEmote(string channelID, string emoteID)
    {
        //TwitchService.Instance.twitchServiceEmotes.
    }

    public async static void CacheEmote(string emoteName, bool isAnimated, string emoteAddress)
    {
        GD.Print("Caching: " + emoteName);
        TwitchEmote newEmote = new TwitchEmote();

        GDScript gifReaderScript = (GDScript)GD.Load("res://addons/gif-importer/GIFReader.gd");
        GodotObject gifReader = (GodotObject)gifReaderScript.New();

        AnimatedTexture animTex = null;
        Image staticImage = new Image();

        // Set the emote name
        newEmote.emoteName = emoteName;

        if (isAnimated)
        {
            HttpRequest emoteAnimImageRequest = new HttpRequest();
            emoteAnimImageRequest.Name = "temp_request";
            TwitchService.GetInstance().AddChild(emoteAnimImageRequest);

            emoteAnimImageRequest.Request(emoteAddress.Replace("/static/", "/animated/"));
            emoteAnimImageRequest.Connect(HttpRequest.SignalName.RequestCompleted, Callable.From((long result, long responseCode, string[] headers, byte[] body) =>
            {
                //GD.Print("Requesting: " + emoteAddress.Replace("/static/", "/animated/"));
                GD.Print("Anim 1: " + emoteName);
                animTex = gifReader.Call("read_data", body).As<AnimatedTexture>();
            }));

            await (emoteAnimImageRequest.ToSignal(emoteAnimImageRequest, HttpRequest.SignalName.RequestCompleted));

            SpriteFrames frames = new SpriteFrames();
            float minDuration = 1000;
            float maxDuration = 0;
            for (int i = 0; i < animTex.Frames; i++)
            {
                frames.AddFrame("default", animTex.GetFrameTexture(i));
                float duration = animTex.GetFrameDuration(i);
                minDuration = Mathf.Min(minDuration, duration);
                maxDuration = Mathf.Max(maxDuration, duration);
            }
            frames.SetAnimationSpeed("default", 2.0 / (minDuration + maxDuration));
            newEmote.emoteAnimated = frames;
            GD.Print("Anim 2: " + emoteName);
        }

        HttpRequest emotStaticImageRequest = new HttpRequest();
        emotStaticImageRequest.Name = "temp_request";
        TwitchService.GetInstance().AddChild(emotStaticImageRequest);

        emotStaticImageRequest.Request(emoteAddress);
        emotStaticImageRequest.Connect(HttpRequest.SignalName.RequestCompleted, Callable.From((long result, long responseCode, string[] headers, byte[] body) =>
        {
            //GD.Print("Requesting: " + emoteAddress);
            staticImage.LoadPngFromBuffer(body);
            newEmote.emoteStatic = ImageTexture.CreateFromImage(staticImage);
        }));

        await (emotStaticImageRequest.ToSignal(emotStaticImageRequest, HttpRequest.SignalName.RequestCompleted));

        // Grab emote file and parse static image
        ResourceSaver.Save(newEmote, "res://Local_Emotes/" + emoteName + ".tres", ResourceSaver.SaverFlags.None);
    }

    public TwitchEmote()
    {
        emoteName = "";
        emoteAnimated = null;
        emoteStatic = null;
    }

    public TwitchEmote(string _emoteName, SpriteFrames _emoteAnimated, ImageTexture _emoteStatic)
    {
        emoteName = _emoteName;
        emoteAnimated = _emoteAnimated;
        emoteStatic = _emoteStatic;
    }

    [Export] public string emoteName;
    [Export] public SpriteFrames emoteAnimated;
    [Export] public ImageTexture emoteStatic;
    [Export] public int cacheTimestamp;
}