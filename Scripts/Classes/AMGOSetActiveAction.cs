using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

using Holoville.HOTween;
using Holoville.HOTween.Plugins.Core;
using Holoville.HOTween.Core;

public class AMPlugGOActive : ABSTweenPlugin {

    private bool eVal;

    protected override object startVal { get { return _startVal; } set { _startVal = value; } }

    protected override object endVal { get { return _endVal; } set { _endVal = value; } }

    public AMPlugGOActive(bool end)
        : base(null, false) { eVal = end; }

    protected override float GetSpeedBasedDuration(float p_speed) {
        return p_speed;
    }

    protected override void SetChangeVal() {
    }

    protected override void SetIncremental(int p_diffIncr) {
    }
    
    protected override void DoUpdate(float p_totElapsed) {
        GameObject go = tweenObj.target as GameObject;
        if(go != null && go.activeSelf != eVal) {
            go.SetActive(eVal);
        }
    }

    protected override void SetValue(object p_value) { }
    protected override object GetValue() { return null; }
}

[AddComponentMenu("")]
public class AMGOSetActiveAction : AMAction {
    public GameObject go;
    public bool endVal;

    public int endFrame;

    public override int getNumberOfFrames() {
        return endFrame == -1 ? 1 : endFrame - startFrame;
    }
    public float getTime(int frameRate) {
        return (float)getNumberOfFrames() / (float)frameRate;
    }

    public override Tweener buildTweener(Sequence sequence, int frameRate) {
        if(go == null) return null;

        //active won't really be set, it's just a filler along with ease
        return HOTween.To(go, getTime(frameRate), new TweenParms().Prop("active", new AMPlugGOActive(endVal)).Ease(EaseType.Linear));
    }

    public override void execute(int frameRate, float delay) {
        Debug.LogError("need implement");
    }

    public string ToString(int codeLanguage, int frameRate, string methodInfoVarName) {
        if(go == null) return null;
        string s = "";
        Debug.LogError("need implement");
        return s;
    }

    public override AnimatorTimeline.JSONAction getJSONAction(int frameRate) {
        if(go == null) return null;

        AnimatorTimeline.JSONAction a = new AnimatorTimeline.JSONAction();
        Debug.LogError("need implement");
        return a;
    }
}
