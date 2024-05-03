using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class BeepoAvatarVRM : Node3D, IBeepoListener
{
    private Skeleton3D skeleton;
    public Skeleton3D Skeleton { get { return skeleton; } }

    private AnimationPlayer animPlayer;
    public AnimationPlayer AnimPlayer { get { return animPlayer; } }

    private BoneMap vrmBoneMap;
    public BoneMap VRMBoneMap { get { return vrmBoneMap; } }

    private Dictionary<MeshInstance3D, Dictionary<uint, List<int>>> meshBlendShapes = new Dictionary<MeshInstance3D, Dictionary<uint, List<int>>>();
    public Dictionary<MeshInstance3D, Dictionary<uint, List<int>>> MeshBlendShapes { get { return meshBlendShapes; } }

    private string version = "";
    public string Version { get { return version; } }

    private Node vrmObject;
    /// <summary>
    /// The root Node for the VRM
    /// </summary>
    public Node VRMObject { get { return vrmObject; } }

    private Node vrmSecondary;
    /// <summary>
    /// The secondary data Node for the VRM
    /// </summary>
    public Node VRMSecondary { get { return vrmSecondary; } }

    public override void _Ready()
    {
        // Find our VRM object
        foreach (Node n in GetChildren())
        {
            if (IsCustomClass(n, "VRMTopLevel"))
            {
                vrmObject = n;
                break;
            }
        }

        // If we have a valid VRMObject find the secondary data node
        if (vrmObject != null)
        {
            foreach (Node n in vrmObject.GetChildren())
            {
                if (IsCustomClass(n, "VRMSecondary"))
                {
                    vrmSecondary ??= n;
                }

                if (n is Skeleton3D skelly)
                {
                    skeleton ??= skelly;
                }

                if (n is AnimationPlayer anim)
                {
                    animPlayer ??= anim;
                }
            }
            if (vrmObject.Get("vrm_meta").AsGodotObject().Get("humanoid_bone_mapping").AsGodotObject() is BoneMap boneMap)
            {
                // GD.Print("Here have all the bones are!");
                vrmBoneMap = boneMap;
            }
            else
            {
                // GD.Print("Where have all the bones go?");
            }

            version = vrmObject.Get("vrm_meta").AsGodotObject().Get("spec_version").AsString();
        }

        // Cache blendshape indexes
        string[] animList = animPlayer.GetAnimationList();
        for (int i = 0; i < animList.Length; i++)
        {
            Animation anim = animPlayer.GetAnimation(animList[i]);
            int trackCount = anim.GetTrackCount();
            uint animNameHash = animList[i].Hash();
            for (int j = 0; j < trackCount; j++)
            {
                // Split the anim track path into the anim root node and the blend shape key name.
                string trackPath = anim.TrackGetPath(j);
                string[] trackPathSplit = trackPath.Split(':');

                // Try and get the node for this animation and make sure it's a MeshInstance for blend shapes.
                if (animPlayer.GetParent().GetNode(trackPathSplit[0]).TryCast<MeshInstance3D>(out MeshInstance3D mesh))
                {
                    Dictionary<uint, List<int>> blendshapes;
                    int blendShapeIndex = mesh.FindBlendShapeByName(trackPathSplit[1]);

                    // Does this mesh have the expected blendshape?
                    if (blendShapeIndex > -1)
                    {
                        // Have we already cached this mesh?
                        if (!meshBlendShapes.ContainsKey(mesh))
                        {
                            // If not create a new entry and assign a new blend shape dictionary.
                            blendshapes = new Dictionary<uint, List<int>>();
                            meshBlendShapes.Add(mesh, blendshapes);
                        }
                        else
                        {
                            // If so grab the existing blend shape dictionary.
                            blendshapes = meshBlendShapes[mesh];
                        }

                        // Have we already cached this animation?
                        if (blendshapes.ContainsKey(animNameHash))
                        {
                            // Add the blend shape index.
                            blendshapes[animNameHash].Add(blendShapeIndex);
                        }
                        else
                        {
                            // Create a new entry and add the current blend shape index.
                            blendshapes.Add(animNameHash, new List<int>() { blendShapeIndex });
                        }
                    }
                }
            }
        }

        BeepoCore.GetInstance().RegisterEventListener("TrackingEvents", this);
    }

    public void Notify(BeepoEvent beepoEvent)
    {
        if (beepoEvent is BoneTrackingEvent boneEvent)
        {

            int boneIndex = Skeleton.FindBone(boneEvent.name);
            if (boneIndex > -1)
            {
                Transform3D newTransform =
                    Skeleton.GetBoneRest(boneIndex) *
                    new Transform3D(Skeleton.GetBoneGlobalRest(boneIndex).Basis, Vector3.Zero).Inverse() *
                    new Transform3D(new Basis(boneEvent.rot), Vector3.Zero) *
                    new Transform3D(Skeleton.GetBoneGlobalRest(boneIndex).Basis, Vector3.Zero);

                Skeleton.SetBonePoseRotation(boneIndex, newTransform.Basis.GetRotationQuaternion());
            }
        }

        if (beepoEvent is BlendShapeTrackingEvent blendShapeEvent)
        {
            foreach (BlendShapeTrackingData b in blendShapeEvent.data)
            {
                foreach (MeshInstance3D m in MeshBlendShapes.Keys)
                {
                    foreach (Dictionary<uint, List<int>> blendshape in MeshBlendShapes.Values)
                    {
                        List<int> blendShapeIndexes;
                        if (blendshape.ContainsKey(b.hash))
                        {
                            blendShapeIndexes = blendshape[b.hash];
                            for (int i = 0; i < blendShapeIndexes.Count; i++)
                            {
                                m.SetBlendShapeValue(blendShapeIndexes[i], b.value);
                            }
                        }
                    }
                }
            }
        }
    }

    private bool IsCustomClass(Node node, string className)
    {
        if (node.GetScript().VariantType == Variant.Type.Nil)
        {
            return false;
        }

        var scriptPath = node.GetScript().As<Script>().ResourcePath;

        var classes = ProjectSettings.GetGlobalClassList();
        foreach (var currentClass in classes)
        {
            if (scriptPath.Equals(currentClass["path"].As<string>()) && className == currentClass["class"].As<string>())
            {
                return true;
            }
        }
        return false;
    }
}