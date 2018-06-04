using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace M8.Animator {
	public struct AMUtil {
	    private static Transform[] mRoots; //generated to grab targets

	    public static void SampleAnimation(Animation anim, string clipName, WrapMode wrap, float weight, float time) {
	        AnimationState animState = anim[clipName];
            if(!animState)
                return;
	        animState.enabled = true;
	        animState.wrapMode = wrap;
	        animState.weight = weight;
	        animState.time = time;
	        anim.Sample();
	        animState.enabled = false;
	    }

	    public static void SampleAnimationFadeIn(Animation anim, string clipName, WrapMode wrap, float fadeDelay, float time) {
	        AnimationState animState = anim[clipName];
            if(!animState)
                return;
	        animState.enabled = true;
	        animState.wrapMode = wrap;
	        animState.weight = time < fadeDelay ? time/fadeDelay : 1.0f;
	        animState.time = time;
	        anim.Sample();
	        animState.enabled = false;
	    }

	    public static void SampleAnimationCrossFade(Animation anim, float fadeDelay, string prevClipName, WrapMode prevWrap, float prevTime, string clipName, WrapMode wrap, float time) {
	        float weight = time < fadeDelay ? time / fadeDelay : 1.0f;

	        if(weight < 1.0f) {
	            AnimationState animPrevState = anim[prevClipName];
	            AnimationState animState = anim[clipName];

	            animPrevState.enabled = true;
	            animPrevState.wrapMode = prevWrap;
	            animPrevState.weight = 1.0f - weight;
	            animPrevState.time = prevTime;

	            animState.enabled = true;
	            animState.wrapMode = wrap;
	            animState.weight = weight;
	            animState.time = time;

	            anim.Sample();

	            animPrevState.enabled = false;
	            animState.enabled = false;
	        }
	        else {
	            AnimationState animState = anim[clipName];
	            animState.enabled = true;
	            animState.wrapMode = wrap;
	            animState.weight = weight;
	            animState.time = time;
	            anim.Sample();
	            animState.enabled = false;
	        }
	    }
	    
	    public static void SetTopCamera(Camera top, Camera bottom) {
	        if(top) top.depth = 0;
	        if(bottom) bottom.depth = -1;
	    }

	    public static void SetTopCamera(Camera top, Camera[] all) {
	        top.depth = 0;
	        foreach(Camera camera in all) {
	            if(camera != top) camera.depth = -1;
	        }
	    }

	    // should the cameras be reversed
	    public static bool isTransitionReversed(int transition, float[] r) {
	        switch(transition) {
	            case (int)AMCameraSwitcherKey.Fade.IrisShape:
	            case (int)AMCameraSwitcherKey.Fade.IrisRound:
	            case (int)AMCameraSwitcherKey.Fade.IrisBox:
	                // if iris type is grow, reverse
	                if(r.Length >= 1 && r[0] == 1f) return true;
	                break;
	            default:
	                break;
	        }
	        return false;
	    }

		public static Transform[] CreateTarget(Transform root, string path) {
			List<Transform> rets = new List<Transform>();
			if(path[0] == '.') {

			}
			else {
				string[] names;
				int startInd;
				Transform parent;
				if(path[0] == '/') {
					names = path.Substring(1).Split('/');
					startInd = 1;
					//get first parent
					GameObject go = GameObject.Find(names[0]);
					if(!go) {
						go = new GameObject(names[0]);
						rets.Add(go.transform);
					}
					parent = go.transform;
				}
				else {
					names = path.Split('/');
					startInd = 0;
					parent = root;
				}

				for(int i = startInd; i < names.Length; i++) {
					Transform t = parent.Find(names[i]);
					if(!t) {
						GameObject newGO = new GameObject(names[i]);
						t = newGO.transform;
						t.parent = parent;
						rets.Add(t);
					}
					parent = t;
				}
			}
			return rets.ToArray();
		}

	    /// <summary>
	    /// Call this before creating sequences for animator meta, after adding new root objects.
	    /// </summary>
	    public void ClearTargetRootCache() {
	        mRoots = null;
	    }

	    private static void GenerateTargetRootCache() {
	        Transform[] trans = Resources.FindObjectsOfTypeAll<Transform>();
	        if(trans.Length > 0) {
	            List<Transform> roots = new List<Transform>(trans.Length);

	            for(int i = 0; i < trans.Length; i++) {
	                Transform t = trans[i];
	                if(t.parent != null || t.gameObject.hideFlags == HideFlags.NotEditable || t.gameObject.hideFlags == HideFlags.HideAndDontSave) {
	                    continue;
	                }

	                roots.Add(t);
	            }

	            mRoots = roots.ToArray();
	        }
	        else
	            mRoots = new Transform[0];
	    }

	    private static Transform GetTargetRoot(string name) {
	        if(mRoots == null) GenerateTargetRootCache();

	        for(int i = 0; i < mRoots.Length; i++) {
	            if(mRoots[i] == null) {
	                //need to regenerate roots
	                mRoots = null;
	                return GetTargetRoot(name);
	            }
	            if(mRoots[i].name == name)
	                return mRoots[i];
	        }
	        return null;
	    }

		public static Transform GetTarget(Transform root, string path) {
			Transform ret = null;
			
			if(!string.IsNullOrEmpty(path)) {
				if(path[0] == '.') {
	                return root;
				}

	            string[] names;
	            int startInd;
	            Transform parent;
	            if(path[0] == '/') {
	                names = path.Substring(1).Split('/');
	                startInd = 1;
	                //get first parent
	                parent = GetTargetRoot(names[0]);
	                if(parent == null) {
	                    return null; //root not found, can't continue
	                }
	            }
	            else {
	                names = path.Split('/');
	                startInd = 0;
	                parent = root;
	            }

	            //iterate through names and store in ret
	            for(int i = startInd; i < names.Length; i++) {
	                //search child in current parent
	                int cCount = parent.childCount;
	                for(int c = 0; c < cCount; c++) {
	                    Transform child = parent.GetChild(c);
	                    if(child.name == names[i]) {
	                        ret = child;
	                        break;
	                    }
	                }

	                if(ret == null)
	                    break; //path not found

	                parent = ret;
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

        public static DG.Tweening.EaseFunction GetEasingFunction(DG.Tweening.Ease type) {
            return DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(type);
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
}