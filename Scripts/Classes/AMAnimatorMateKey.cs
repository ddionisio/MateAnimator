using UnityEngine;
using System.Collections;

using Holoville.HOTween;
using Holoville.HOTween.Core;

[AddComponentMenu("")]
public class AMAnimatorMateKey : AMKey {
    public AMPlugMateAnimator.LoopType loop;
    public string take;
    public float duration;

    public override void CopyTo(AMKey key) {

        AMAnimatorMateKey a = key as AMAnimatorMateKey;
        a.enabled = false;
        a.loop = loop;
        a.take = take;
        a.duration = duration;
    }

    // get number of frames, -1 is infinite
    public override int getNumberOfFrames(int frameRate) {
        switch(loop) {
            case AMPlugMateAnimator.LoopType.None:
                return Mathf.CeilToInt(duration*(float)frameRate);
            default:
                return -1;
        }
    }

    public void updateDuration(AMTrack track, AMITarget target) {
        AnimatorData anim = track.GetTarget(target) as AnimatorData;

        int takeInd = string.IsNullOrEmpty(take) ? -1 : anim.GetTakeIndex(take);
        if(takeInd != -1) {
            AMTakeData _take = anim._takes[takeInd];
            float fLastFrame = _take.getLastFrame();
            duration = fLastFrame/_take.frameRate;
        }
        else
            duration = 0f;
    }

    #region action
    public override void build(AMSequence seq, AMTrack track, int index, UnityEngine.Object target) {
        if(string.IsNullOrEmpty(take)) return; //this is a stop point

        int frameRate = seq.take.frameRate;
        float waitTime = getWaitTime(frameRate, 0.0f);
        AnimatorData anim = target as AnimatorData;
        int takeInd = anim.GetTakeIndex(take);

        if(takeInd == -1) { Debug.LogError(string.Format("take {0} does not exist in {1}.", take, anim.name)); return; }

        AMTakeData takeDat = anim._takes[takeInd];

        int endFrame;
        if(index < track.keys.Count-1) {
            AMAnimatorMateKey nextKey = track.keys[index+1] as AMAnimatorMateKey;
            endFrame = nextKey.frame;
        }
        else
            endFrame = seq.take.getLastFrame();

        float _endDuration = ((endFrame-frame)+1)/(float)frameRate;
        float _duration = loop == AMPlugMateAnimator.LoopType.None ? Mathf.Min(duration, _endDuration) : _endDuration;

        Holoville.HOTween.Plugins.Core.ABSTweenPlugin plug;

        plug = new AMPlugMateAnimator(seq.target, anim, takeDat, loop);

        seq.sequence.Insert(waitTime, HOTween.To(target, _duration, new TweenParms().Prop("width_track", plug)));
    }
    #endregion
}
