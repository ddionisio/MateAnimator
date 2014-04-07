using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Holoville.HOTween;
using Holoville.HOTween.Plugins.Core;
using Holoville.HOTween.Core;

public class AMPlugGOActive : ABSTweenPlugin {
	private GameObject mGo;
    private bool eVal;

    protected override object startVal { get { return _startVal; } set { _startVal = value; } }

	protected override object endVal { get { return _endVal; } set { _endVal = value; } }

    public AMPlugGOActive(GameObject go, bool end)
	: base(null, false) { mGo = go; eVal = end; }

    protected override float GetSpeedBasedDuration(float p_speed) {
        return p_speed;
    }

    protected override void SetChangeVal() {
		mGo.SetActive(eVal);
    }

    protected override void SetIncremental(int p_diffIncr) {}

    protected override void DoUpdate(float p_totElapsed) {
		if(mGo != null && mGo.activeSelf != eVal) {
			mGo.SetActive(eVal);
        }
    }

    protected override void SetValue(object p_value) { }
    protected override object GetValue() { return eVal; }
}

[AddComponentMenu("")]
public class AMGOSetActiveKey : AMKey {
    public bool setActive;

    public int endFrame;

    public override void destroy() {
        base.destroy();
    }
    // copy properties from key
    public override AMKey CreateClone(GameObject go) {

		AMGOSetActiveKey a = go ? go.AddComponent<AMGOSetActiveKey>() : gameObject.AddComponent<AMGOSetActiveKey>();
        a.enabled = false;
        a.frame = frame;
        a.setActive = setActive;
        return a;
    }

    #region action
    public override int getNumberOfFrames() {
        return endFrame == -1 ? 1 : endFrame - frame;
    }
    public float getTime(int frameRate) {
        return (float)getNumberOfFrames() / (float)frameRate;
    }

    public override Tweener buildTweener(Sequence sequence, UnityEngine.Object target, int frameRate) {
		GameObject go = target as GameObject;

        if(go == null) return null;

        //active won't really be set, it's just a filler along with ease
        return HOTween.To(go, getTime(frameRate), new TweenParms().Prop("active", new AMPlugGOActive(go, setActive)).Ease(EaseType.Linear));
    }
    #endregion
}
