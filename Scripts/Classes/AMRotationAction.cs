using UnityEngine;
using System.Collections;

using Holoville.HOTween;
using Holoville.HOTween.Plugins;

[AddComponentMenu("")]
public class AMRotationAction : AMAction {

    //public int type = 0; // 0 = Rotate To, 1 = Look At
    public int endFrame;
    public Transform obj;
    public bool isLocal;
    public Quaternion startRotation;
    public Quaternion endRotation;

    public override string ToString(int codeLanguage, int frameRate) {
        if(endFrame == -1) return null;
        string s = "";
        /*if(codeLanguage == 0) {
        // c#
            s = "AMTween.RotateTo (obj.gameObject, AMTween.Hash (\"delay\", "+getWaitTime(frameRate,0f)+"f, \"time\", "+getTime(frameRate)+"f, ";
            s += "\"rotation\", new Vector3("+endRotation.eulerAngles.x+"f, "+endRotation.eulerAngles.y+"f, "+endRotation.eulerAngles.z+"f), ";
			
            s += getEaseString(codeLanguage)+"));";
        } else {
        // js
            s = "AMTween.RotateTo (obj.gameObject, {\"delay\": "+getWaitTime(frameRate,0f)+", \"time\": "+getTime(frameRate)+", ";
            s += "\"rotation\": Vector3("+endRotation.eulerAngles.x+", "+endRotation.eulerAngles.y+", "+endRotation.eulerAngles.z+"), ";
            s += getEaseString(codeLanguage)+"});";
        }*/
        Debug.LogError("need implement");
        return s;
    }
    public override int getNumberOfFrames() {
        return endFrame - startFrame;
    }
    public float getTime(int frameRate) {
        return (float)getNumberOfFrames() / (float)frameRate;
    }
    public override Tweener buildTweener(Sequence sequence, int frameRate) {
        if(!obj) return null;
        if(endFrame == -1) return null;
        if(hasCustomEase()) {
            return HOTween.To(obj, getTime(frameRate), new TweenParms().Prop(isLocal ? "localRotation" : "rotation", new AMPlugQuaternionSlerp(endRotation)).Ease(easeCurve));
        }
        else {
            return HOTween.To(obj, getTime(frameRate), new TweenParms().Prop(isLocal ? "localRotation" : "rotation", new AMPlugQuaternionSlerp(endRotation)).Ease((EaseType)easeType));
        }
    }
    public override void execute(int frameRate, float delay) {
        if(!obj) return;
        if(endFrame == -1) return;
        Debug.LogError("need implement");
        //if(hasCustomEase()) AMTween.RotateTo (obj.gameObject,AMTween.Hash ("delay",getWaitTime(frameRate, delay),"time",getTime(frameRate),"rotation",endRotation.eulerAngles,"easecurve",easeCurve));
        //else AMTween.RotateTo (obj.gameObject,AMTween.Hash ("delay",getWaitTime(frameRate, delay),"time",getTime(frameRate),"rotation",endRotation.eulerAngles,"easetype",(AMTween.EaseType)easeType));

    }
    public Quaternion getStartQuaternion() {
        return startRotation;
    }
    public Quaternion getEndQuaternion() {
        return endRotation;
    }

    public override AnimatorTimeline.JSONAction getJSONAction(int frameRate) {
        if(!obj || endFrame == -1) return null;
        AnimatorTimeline.JSONAction a = new AnimatorTimeline.JSONAction();
        a.go = obj.gameObject.name;
        a.method = "rotateto";
        a.delay = getWaitTime(frameRate, 0f);
        a.time = getTime(frameRate);
        setupJSONActionEase(a);
        // set rotation
        a.setPath(new Vector3[] { endRotation.eulerAngles });
        return a;
    }
}
