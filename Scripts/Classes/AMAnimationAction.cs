using UnityEngine;
using System.Collections;

using Holoville.HOTween;
using Holoville.HOTween.Core;

[AddComponentMenu("")]
public class AMAnimationAction : AMAction {
    public AnimationClip amClip;
    public WrapMode wrapMode;
    public GameObject obj;
    public bool crossfade;
    public float crossfadeTime;

    void OnMethodCallbackParams(TweenEvent dat) {
        if(obj != null && obj.animation != null && amClip != null) {
            float elapsed = dat.tween.elapsed;
            float frameRate = (float)dat.parms[0];
            float curFrame = frameRate * elapsed;
            float numFrames = getNumberOfFrames(Mathf.RoundToInt(frameRate));

            if(numFrames > 0.0f && curFrame > startFrame + numFrames) return;

            Animation anm = obj.animation;

            anm.wrapMode = wrapMode;

            if(crossfade) {
                anm.CrossFade(amClip.name, crossfadeTime);
            }
            else {
                anm.Play(amClip.name);
            }
        }
    }

    public override Tweener buildTweener(Sequence sequence, int frameRate) {
        sequence.InsertCallback(getWaitTime(frameRate, 0.0f), OnMethodCallbackParams, (float)frameRate);

        return null;
    }

    public override void execute(int frameRate, float delay) {
        if(!amClip || !obj) return;
        Debug.LogError(" need implement");
        //AMTween.PlayAnimation(obj, AMTween.Hash ("delay", getWaitTime(frameRate,delay), "animation", amClip.name, "wrapmode", wrapMode, "crossfade", crossfade, "fadeLength", crossfadeTime));
    }

    public override string ToString(int codeLanguage, int frameRate) {
        string s = "";
        /*if(!amClip) return null;
        if(codeLanguage == 0) {
            // c#
            s += "AMTween.PlayAnimation(obj.gameObject, AMTween.Hash (\"delay\", "+getWaitTime(frameRate,0f)+"f, \"animation\", \""+amClip.name+"\", \"wrapmode\", "+"WrapMode."+wrapMode.ToString()+",\"crossfade\", "+crossfade.ToString ().ToLower();
            if(crossfade) s += ", \"fadeLength\", "+crossfadeTime.ToString()+"f";
            s += "));";
        } else {
            // js
            s += "AMTween.PlayAnimation(obj.gameObject, {\"delay\": "+getWaitTime(frameRate,0f)+", \"animation\": \""+amClip.name+"\", \"wrapmode\": "+"WrapMode."+wrapMode.ToString()+",\"crossfade\": "+crossfade.ToString ().ToLower();
            if(crossfade) s += ", \"fadeLength\": "+crossfadeTime.ToString();
            s += "});";
        }*/
        Debug.LogError(" need implement");
        return s;
    }

    // get number of frames, -1 is infinite
    public int getNumberOfFrames(int frameRate) {
        if(!amClip) return -1;
        if(wrapMode != WrapMode.Once) return -1;
        return Mathf.CeilToInt(amClip.length * frameRate);
    }

    public override AnimatorTimeline.JSONAction getJSONAction(int frameRate) {
        if(!amClip || !obj) return null;
        AnimatorTimeline.JSONAction a = new AnimatorTimeline.JSONAction();
        a.method = "playanimation";
        a.go = obj.gameObject.name;
        a.delay = getWaitTime(frameRate, 0f);
        a.strings = new string[] { amClip.name };
        a.floats = new float[] { (float)wrapMode, crossfadeTime };
        a.bools = new bool[] { crossfade };

        return a;
    }
}
