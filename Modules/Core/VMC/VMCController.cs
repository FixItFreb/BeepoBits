using System;
using Godot;
using System.Collections.Generic;
using uOSC;

public struct BoneTrackingData
{
    public string name;
    public Vector3 pos;
    public Quaternion rot;
}

public struct BlendShapeTrackingData
{
    public uint hash;
    public float value;
}

public partial class VMCController : Node
{
    private uint boneTrackingAddress = "/VMC/Ext/Bone/Pos".Hash();
    private uint blendTrackingAddress = "/VMC/Ext/Blend/Val".Hash();
    private uint blendApplyAddress = "/VMC/Ext/Blend/Apply".Hash();

    [Export] private BeepoAvatarVRM currentAvatar;
    private string[] avatarAnimList;

    private List<BlendShapeTrackingData> blendShapesToApply = new List<BlendShapeTrackingData>();

    public void SetCurrentAvatar(BeepoAvatarVRM _currentAvatar)
    {
        currentAvatar = _currentAvatar;
        avatarAnimList = currentAvatar.AnimPlayer.GetAnimationList();
    }

    public void ReceiveOSCMessage(MessageObject message)
    {
        uint addressHash = message.data.address.Hash();
        object[] values = message.data.values;

        if (addressHash == boneTrackingAddress)
        {
            BoneTrackingData boneData = new BoneTrackingData();
            boneData.name = (string)values[0];
            boneData.rot = new Quaternion((float)values[4], -(float)values[5], -(float)values[6], (float)values[7]).Normalized();

            int boneIndex = currentAvatar.Skeleton.FindBone(boneData.name);
            if (boneIndex > -1)
            {
                Transform3D newTransform =
                    currentAvatar.Skeleton.GetBoneRest(boneIndex) *
                    new Transform3D(currentAvatar.Skeleton.GetBoneGlobalRest(boneIndex).Basis, Vector3.Zero).Inverse() *
                    new Transform3D(new Basis(boneData.rot), Vector3.Zero) *
                    new Transform3D(currentAvatar.Skeleton.GetBoneGlobalRest(boneIndex).Basis, Vector3.Zero);

                currentAvatar.Skeleton.SetBonePoseRotation(boneIndex, newTransform.Basis.GetRotationQuaternion());
            }
        }
        else if (addressHash == blendTrackingAddress)
        {
            BlendShapeTrackingData blendShapeData = new BlendShapeTrackingData();
            string animName = (string)values[0];
            blendShapeData.value = (float)values[1];
            blendShapeData.hash = animName.Hash();
            blendShapesToApply.Add(blendShapeData);
        }
        else if (addressHash == blendApplyAddress)
        {
            // Apply each of our blend shapes
            foreach (BlendShapeTrackingData b in blendShapesToApply)
            {
                foreach (MeshInstance3D m in currentAvatar.MeshBlendShapes.Keys)
                {
                    foreach (Dictionary<uint, List<int>> blendshape in currentAvatar.MeshBlendShapes.Values)
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

            // Clear all blend shape data ready for next update
            blendShapesToApply.Clear();
        }
    }
}