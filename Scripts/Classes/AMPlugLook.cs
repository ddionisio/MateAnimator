using System;
using Holoville.HOTween;
using Holoville.HOTween.Plugins.Core;
using UnityEngine;

public class AMPlugLookFollowTarget : ABSTweenPlugin {
    Transform follow;

    protected override object startVal { get { return _startVal; } set { _startVal = value; } }

    protected override object endVal { get { return _endVal; } set { _endVal = value; } }

    public AMPlugLookFollowTarget(Transform follow)
        : base(null, false) { this.follow = follow; }

    protected override float GetSpeedBasedDuration(float p_speed) {
        return p_speed;
    }

    protected override void SetChangeVal() {
    }

    protected override void SetIncremental(int p_diffIncr) {
    }

    protected override void DoUpdate(float p_totElapsed) {
        Transform t = tweenObj.target as Transform;
        if(t != null && follow != null)
            t.LookAt(follow);
    }
}

public class AMPlugLookToFollowTarget : ABSTweenPlugin {
    Transform follow;
    bool endPosAvail;
    Vector3 endPos;

    Vector3 eulerStart;
    Vector3 eulerEnd;

    protected override object startVal { get { return _startVal; } set { _startVal = value; } }

    protected override object endVal { get { return _endVal; } set { _endVal = value; } }

    public AMPlugLookToFollowTarget(Transform follow, Vector3 endPosition)
        : base(null, false) { this.follow = follow; endPosAvail = true; endPos = endPosition; }

    public AMPlugLookToFollowTarget(Transform follow, Vector3 endPosition, EaseType ease)
        : base(null, ease, false) { this.follow = follow; endPosAvail = true; endPos = endPosition; }

    public AMPlugLookToFollowTarget(Transform follow, Vector3 endPosition, AnimationCurve curve)
        : base(null, curve, false) { this.follow = follow; endPosAvail = true; endPos = endPosition; }

    public AMPlugLookToFollowTarget(Transform follow)
        : base(null, false) { this.follow = follow; endPosAvail = false; }

    public AMPlugLookToFollowTarget(Transform follow, EaseType ease)
        : base(null, ease, false) { this.follow = follow; endPosAvail = false; }

    public AMPlugLookToFollowTarget(Transform follow, AnimationCurve curve)
        : base(null, curve, false) { this.follow = follow; endPosAvail = false; }

    protected override float GetSpeedBasedDuration(float p_speed) {
        return p_speed;
    }

    protected override void SetChangeVal() {
        Transform t = tweenObj.target as Transform;
        if(t != null)
            eulerStart = t.eulerAngles;
    }

    protected override void SetIncremental(int p_diffIncr) {
    }

    protected override void DoUpdate(float p_totElapsed) {
        Transform t = tweenObj.target as Transform;
        if(t != null && follow != null) {
            if(endPosAvail) {
                Vector3 temp = t.position;
                t.position = endPos;
                t.LookAt(follow);
                t.position = temp;
            }
            else {
                t.LookAt(follow);
            }

            eulerEnd = t.eulerAngles;
            eulerEnd.Set(AMUtil.clerp(eulerStart.x, eulerEnd.x, 1.0f), AMUtil.clerp(eulerStart.y, eulerEnd.y, 1.0f), AMUtil.clerp(eulerStart.z, eulerEnd.z, 1.0f));

            Vector3 cur = new Vector3(
                ease(p_totElapsed, eulerStart.x, eulerEnd.x - eulerStart.x, _duration, tweenObj.easeOvershootOrAmplitude, tweenObj.easePeriod),
                ease(p_totElapsed, eulerStart.y, eulerEnd.y - eulerStart.y, _duration, tweenObj.easeOvershootOrAmplitude, tweenObj.easePeriod),
                ease(p_totElapsed, eulerStart.z, eulerEnd.z - eulerStart.z, _duration, tweenObj.easeOvershootOrAmplitude, tweenObj.easePeriod));

            t.rotation = Quaternion.Euler(cur);
        }
    }
}