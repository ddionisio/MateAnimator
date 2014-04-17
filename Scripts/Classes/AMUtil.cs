using UnityEngine;
using System.Collections;
using System.Text;

public struct AMUtil {
	public static Transform GetTarget(Transform root, string path) {
		Transform ret = null;
		
		if(!string.IsNullOrEmpty(path)) {
			if(path[0] == '.') {
				ret = root;
			}
			else if(path[0] == '/') {
				GameObject go = GameObject.Find(path);
				if(go) ret = go.transform;
			}
			else {
				ret = root.FindChild(path);
			}
		}

		return ret;
	}

	public static Transform GetTransform(UnityEngine.Object target) {
		Transform tgt;
		if(target is GameObject) {
			tgt = (target as GameObject).transform;
		}
		else if(target is Component) {
			tgt = (target as Component).transform;
		}
		else {
			tgt = null;
		}
		return tgt;
	}

	public static string GetPath(Transform root, UnityEngine.Object target) {
		Transform tgt = GetTransform(target);
		
		if(tgt) {
			if(tgt == root) {
				return ".";
			}
			else {
				bool _targetPathAbsolute = true;
				StringBuilder strBuff = new StringBuilder(tgt.name, 128);
				Transform tgtParent = tgt.parent;
				while(tgtParent) {
					if(tgtParent == root) {
						_targetPathAbsolute = false;
						break;
					}
					
					strBuff.Insert(0, '/');
					strBuff.Insert(0, tgtParent.name);
					tgtParent = tgtParent.parent;
				}
				
				if(_targetPathAbsolute)
					strBuff.Insert(0, '/');
				
				return strBuff.ToString();
			}
		}
		else {
			return "";
		}
	}

    public static float EaseCustom(float startValue, float changeValue, float time, AnimationCurve curve) {
        return startValue + changeValue * curve.Evaluate(time);
    }

    public static float EaseInExpoReversed(float start, float end, float value) {
        end -= start;
        return 1 + (Mathf.Log(value - start) / (10 * Mathf.Log(2)));
    }

    public static float clerp(float start, float end, float value) {
        float min = 0.0f;
        float max = 360.0f;
        float half = Mathf.Abs((max - min) / 2.0f);
        float retval = 0.0f;
        float diff = 0.0f;
        if((end - start) < -half) {
            diff = ((max - start) + end) * value;
            retval = start + diff;
        }
        else if((end - start) > half) {
            diff = -((max - end) + start) * value;
            retval = start + diff;
        }
        else retval = start + (end - start) * value;
        return retval;
    }

    public static Holoville.HOTween.Core.TweenDelegate.EaseFunc GetEasingFunction(Holoville.HOTween.EaseType type) {
        switch(type) {
            case Holoville.HOTween.EaseType.Linear:
                return Holoville.HOTween.Core.Easing.Linear.EaseNone;
            case Holoville.HOTween.EaseType.EaseInSine:
                return Holoville.HOTween.Core.Easing.Sine.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutSine:
                return Holoville.HOTween.Core.Easing.Sine.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutSine:
                return Holoville.HOTween.Core.Easing.Sine.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInQuad:
                return Holoville.HOTween.Core.Easing.Quad.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutQuad:
                return Holoville.HOTween.Core.Easing.Quad.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutQuad:
                return Holoville.HOTween.Core.Easing.Quad.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInCubic:
                return Holoville.HOTween.Core.Easing.Cubic.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutCubic:
                return Holoville.HOTween.Core.Easing.Cubic.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutCubic:
                return Holoville.HOTween.Core.Easing.Cubic.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInQuart:
                return Holoville.HOTween.Core.Easing.Quart.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutQuart:
                return Holoville.HOTween.Core.Easing.Quart.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutQuart:
                return Holoville.HOTween.Core.Easing.Quart.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInQuint:
                return Holoville.HOTween.Core.Easing.Quint.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutQuint:
                return Holoville.HOTween.Core.Easing.Quint.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutQuint:
                return Holoville.HOTween.Core.Easing.Quint.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInExpo:
                return Holoville.HOTween.Core.Easing.Expo.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutExpo:
                return Holoville.HOTween.Core.Easing.Expo.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutExpo:
                return Holoville.HOTween.Core.Easing.Expo.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInCirc:
                return Holoville.HOTween.Core.Easing.Circ.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutCirc:
                return Holoville.HOTween.Core.Easing.Circ.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutCirc:
                return Holoville.HOTween.Core.Easing.Circ.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInElastic:
                return Holoville.HOTween.Core.Easing.Elastic.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutElastic:
                return Holoville.HOTween.Core.Easing.Elastic.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutElastic:
                return Holoville.HOTween.Core.Easing.Elastic.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInBack:
                return Holoville.HOTween.Core.Easing.Back.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutBack:
                return Holoville.HOTween.Core.Easing.Back.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutBack:
                return Holoville.HOTween.Core.Easing.Back.EaseInOut;
            case Holoville.HOTween.EaseType.EaseInBounce:
                return Holoville.HOTween.Core.Easing.Bounce.EaseIn;
            case Holoville.HOTween.EaseType.EaseOutBounce:
                return Holoville.HOTween.Core.Easing.Bounce.EaseOut;
            case Holoville.HOTween.EaseType.EaseInOutBounce:
                return Holoville.HOTween.Core.Easing.Bounce.EaseInOut;
            case Holoville.HOTween.EaseType.AnimationCurve:
                return null;
        }

        return null;
    }

    //andeeee from the Unity forum's steller Catmull-Rom class ( http://forum.unity3d.com/viewtopic.php?p=218400#218400 ):
    public static Vector3 Interp(Vector3[] pts, float t) {
        int numSections = pts.Length - 1;
        int currPt = Mathf.Min(Mathf.FloorToInt(t * (float)numSections), numSections - 1);
        float u = t * (float)numSections - (float)currPt;

		Vector3 a,b,c,d;

		bool isLoop = pts[0] == pts[pts.Length - 1];

		if(currPt == 0) {
			if(isLoop)
				a = pts[pts.Length - 2];
			else
				a = pts[0] + (pts[0] - pts[1]);

			b = pts[0];
			c = pts[1];

			if(pts.Length < 3) {
				d = isLoop ? pts[1] : pts[1] + (pts[1] - pts[0]);
			}
			else
				d = pts[2];
		}
		else {
			a = pts[currPt - 1];
			b = pts[currPt];
			c = pts[currPt + 1];

			if(currPt + 2 >= pts.Length)
				d = isLoop ? pts[1] : pts[pts.Length - 1] + (pts[pts.Length - 1] - pts[pts.Length - 2]);
			else
				d = pts[currPt + 2];
		}

        return .5f * (
            (-a + 3f * b - 3f * c + d) * (u * u * u)
            + (2f * a - 5f * b + 4f * c - d) * (u * u)
            + (-a + c) * u
            + 2f * b
        );
    }

    /// <summary>
    /// Puts a GameObject on a path at the provided percentage 
    /// </summary>
    /// <param name="target">
    /// A <see cref="GameObject"/>
    /// </param>
    /// <param name="path">
    /// A <see cref="Vector3[]"/>
    /// </param>
    /// <param name="percent">
    /// A <see cref="System.Single"/>
    /// </param>
    public static void PutOnPath(GameObject target, Vector3[] path, float percent, bool local) {
        if(local)
            target.transform.localPosition = Interp(path, percent);
        else
            target.transform.position = Interp(path, percent);
    }

    /// <summary>
    /// Puts a GameObject on a path at the provided percentage 
    /// </summary>
    /// <param name="target">
    /// A <see cref="Transform"/>
    /// </param>
    /// <param name="path">
    /// A <see cref="Vector3[]"/>
    /// </param>
    /// <param name="percent">
    /// A <see cref="System.Single"/>
    /// </param>
    public static void PutOnPath(Transform target, Vector3[] path, float percent, bool local) {
        if(local)
            target.localPosition = Interp(path, percent);
        else
            target.position = Interp(path, percent);
    }
    // get position on path
    public static Vector3 PositionOnPath(Vector3[] path, float percent) {
        return Interp(path, percent);
    }
    /// <summary>
    /// Puts a GameObject on a path at the provided percentage 
    /// </summary>
    /// <param name="target">
    /// A <see cref="GameObject"/>
    /// </param>
    /// <param name="path">
    /// A <see cref="Transform[]"/>
    /// </param>
    /// <param name="percent">
    /// A <see cref="System.Single"/>
    /// </param>
    public static void PutOnPath(GameObject target, Transform[] path, float percent, bool pathLocal, bool local) {
        //create and store path points:
        Vector3[] suppliedPath = new Vector3[path.Length];
        for(int i = 0; i < path.Length; i++) {
            suppliedPath[i] = pathLocal ? path[i].localPosition : path[i].position;
        }

        if(local)
            target.transform.position = Interp(suppliedPath, percent);
        else
            target.transform.localPosition = Interp(suppliedPath, percent);
    }

    /// <summary>
    /// Puts a GameObject on a path at the provided percentage 
    /// </summary>
    /// <param name="target">
    /// A <see cref="Transform"/>
    /// </param>
    /// <param name="path">
    /// A <see cref="Transform[]"/>
    /// </param>
    /// <param name="percent">
    /// A <see cref="System.Single"/>
    /// </param>
    public static void PutOnPath(Transform target, Transform[] path, float percent, bool pathLocal, bool local) {
        //create and store path points:
        Vector3[] suppliedPath = new Vector3[path.Length];
        for(int i = 0; i < path.Length; i++) {
            suppliedPath[i] = pathLocal ? path[i].localPosition : path[i].position;
        }

        if(local)
            target.localPosition = Interp(suppliedPath, percent);
        else
            target.position = Interp(suppliedPath, percent);
    }

    /// <summary>
    /// Returns a Vector3 position on a path at the provided percentage  
    /// </summary>
    /// <param name="path">
    /// A <see cref="Transform[]"/>
    /// </param>
    /// <param name="percent">
    /// A <see cref="System.Single"/>
    /// </param>
    /// <returns>
    /// A <see cref="Vector3"/>
    /// </returns>
    public static Vector3 PointOnPath(Transform[] path, float percent, bool local) {
        //create and store path points:
        Vector3[] suppliedPath = new Vector3[path.Length];
        for(int i = 0; i < path.Length; i++) {
            suppliedPath[i] = local ? path[i].localPosition : path[i].position;
        }
        return (Interp(suppliedPath, percent));
    }

    /// <summary>
    /// Returns a Vector3 position on a path at the provided percentage  
    /// </summary>
    /// <param name="path">
    /// A <see cref="Vector3[]"/>
    /// </param>
    /// <param name="percent">
    /// A <see cref="System.Single"/>
    /// </param>
    /// <returns>
    /// A <see cref="Vector3"/>
    /// </returns>
    public static Vector3 PointOnPath(Vector3[] path, float percent) {
        return (Interp(path, percent));
    }
}
