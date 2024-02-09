using Godot;
using System;

public partial class BodyMorpher : Node
{
    [Export] private Skeleton3D targetSkeleton;
    [Export] private int targetBoneIndex = -1;

    private Vector3 morphDefault;
    private Vector3 morphTo;
    private Vector3 morphFrom;
    private float morphDuration = 0;
    private float currentTime = 0;
    private float morphProgress = 0;
    private float morphToTime = 0;
    private float morphBackTime = 0;

    private AudioStream morphToAudio;
    private AudioStream morphBackAudio;

    private bool morphActive = false;

    public void ApplyMorph(BodyMorphSet toApply, int morphIndex)
    {
        // morphToTime = toApply.morphToTime;
        // morphBackTime = toApply.morphBackTime;
        // morphDuration = morphDuration + morphToTime + morphBackTime;
        morphTo = toApply.morphData[morphIndex].morphTo;
        morphProgress = 0;
        currentTime = 0;
        morphFrom = targetSkeleton.GetBonePoseScale(targetBoneIndex);

        // Work out which audio to play
        if (!toApply.morphAudio.IsNullOrEmpty())
        {
            MorphAudioPair m = toApply.morphAudio.GetRandom();
            if (morphTo.Length() > morphFrom.Length())
            {
                morphToAudio = m.morphUpAudio;
                morphBackAudio = m.morphDownAudio;
            }
            else
            {
                morphToAudio = m.morphDownAudio;
                morphBackAudio = m.morphUpAudio;
            }
        }
        else
        {
            morphToAudio = null;
            morphBackAudio = null;
        }

        morphToTime = morphToAudio != null ? (float)morphToAudio.GetLength() : toApply.morphToTime;
        morphBackTime = morphBackAudio != null ? (float)morphBackAudio.GetLength() : toApply.morphBackTime;
        morphDuration = toApply.morphDuration + morphToTime + morphBackTime;
        morphActive = true;
    }

    public override void _Ready()
    {
        morphDefault = targetSkeleton.GetBonePoseScale(targetBoneIndex);
    }

    public override void _Process(double delta)
    {
        if(!morphActive)
        {
            return;
        }

        currentTime += (float)delta;

        // Morph from current state to target state over our specified time
        if (targetSkeleton.GetBonePoseScale(targetBoneIndex) != morphTo && currentTime < morphDuration)
        {
            // This will only be true the first time we get here per morph
            if(morphProgress == 0)
            {
                BeepoAudio.PlayAudio(morphToAudio);
            }

            morphProgress += (float)(delta / morphToTime);

            if(morphProgress >= 1.0f)
            {
                morphProgress = 0;
                targetSkeleton.SetBonePoseScale(targetBoneIndex, morphTo);
            }
            else
            {
                targetSkeleton.SetBonePoseScale(targetBoneIndex, morphFrom.Lerp(morphTo, morphProgress));
            }
        }
        // Has this morph hit the duration it needs
        else if (currentTime >= morphDuration)
        {
            if (targetSkeleton.GetBonePoseScale(targetBoneIndex) != morphDefault)
            {
                // This will only be true the first time we get here per morph
                if (morphProgress == 0)
                {
                    BeepoAudio.PlayAudio(morphBackAudio);
                }

                morphProgress += (float)(delta / morphBackTime);

                if (morphProgress >= 1.0f)
                {
                    // We are back to default state, disable tick
                    targetSkeleton.SetBonePoseScale(targetBoneIndex, morphDefault);
                    morphActive = false;
                }
                else
                {
                    targetSkeleton.SetBonePoseScale(targetBoneIndex, morphTo.Lerp(morphDefault, morphProgress));
                }
            }
        }
    }
}
