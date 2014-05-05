using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween;

public class AMCameraSwitcherKey : AMKey {
    public enum Fade {
        CrossFade = 0,
        VenetianBlinds = 1,
        Doors = 2,
        IrisBox = 3,
        IrisRound = 4,
        None = 5,
        IrisShape = 6,
        ShapeWipe = 7,
        LinearWipe = 8,
        RadialWipe = 9,
        WedgeWipe = 10

    }

    public int type = 0;		// 0 = camera, 1 = color
    public int typeEnd;

    [SerializeField]
    Camera _camera;
    [SerializeField]
    string _cameraPath;

    [SerializeField]
    Camera _cameraEnd;
    [SerializeField]
    string _cameraEndPath;

    public Color color;

    public Color colorEnd;

    public int cameraFadeType = (int)Fade.CrossFade;
    public List<float> cameraFadeParameters = new List<float>();
    public Texture2D irisShape;
    public bool still = false;	// is it still or does it use render texture

    public int endFrame;

    public Camera getCamera(AMITarget itarget) {
        if(itarget.TargetIsMeta()) {
            Transform t = itarget.TargetGetCache(_cameraPath);
            if(!t) {
                t = AMUtil.GetTarget(itarget.TargetGetRoot(), _cameraPath);
                if(t) itarget.TargetSetCache(_cameraPath, t);
                else itarget.TargetMissing(_cameraPath, true);
            }

            return t.camera;
        }
        else
            return _camera;
    }

    public bool cameraMatch(AMITarget itarget, Camera camera) {
        return getCamera(itarget) == camera;
    }

    public bool setCamera(AMITarget itarget, Camera camera) {
        if(getCamera(itarget) != camera) {
            if(camera) {
                if(itarget.TargetIsMeta()) {
                    _camera = null;
                    itarget.TargetMissing(_cameraPath, false);
                    _cameraPath = AMUtil.GetPath(itarget.TargetGetRoot(), camera);
                    itarget.TargetSetCache(_cameraPath, camera.transform);

                }
                else {
                    _camera = camera;
                    _cameraPath = "";
                }
            }
            else {
                itarget.TargetMissing(_cameraPath, false);
                _camera = null;
                _cameraPath = "";
            }

            return true;
        }
        return false;
    }

    public Camera getCameraEnd(AMITarget itarget) {
        if(itarget.TargetIsMeta()) {
            Transform t = itarget.TargetGetCache(_cameraEndPath);
            if(!t) {
                t = AMUtil.GetTarget(itarget.TargetGetRoot(), _cameraEndPath);
                if(t) itarget.TargetSetCache(_cameraEndPath, t);
                else itarget.TargetMissing(_cameraEndPath, true);
            }

            return t.camera;
        }
        else
            return _cameraEnd;
    }

    public void setCameraEnd(AMCameraSwitcherKey nextKey) {
        _cameraEnd = nextKey._camera;
        _cameraEndPath = nextKey._cameraPath;
    }

    public override void maintainKey(AMITarget itarget, UnityEngine.Object targetObj) {
        if(itarget.TargetIsMeta()) {
            if(string.IsNullOrEmpty(_cameraPath)) {
                if(_camera) {
                    _cameraPath = AMUtil.GetPath(itarget.TargetGetRoot(), _camera);
                    itarget.TargetSetCache(_cameraPath, _camera.transform);
                }
            }

            if(string.IsNullOrEmpty(_cameraEndPath)) {
                if(_cameraEnd) {
                    _cameraEndPath = AMUtil.GetPath(itarget.TargetGetRoot(), _cameraEnd);
                    itarget.TargetSetCache(_cameraEndPath, _cameraEnd.transform);
                }
            }

            _camera = null;
            _cameraEnd = null;
        }
        else {
            if(!_camera) {
                if(!string.IsNullOrEmpty(_cameraPath)) {
                    Transform t = itarget.TargetGetCache(_cameraPath);
                    if(!t)
                        t = AMUtil.GetTarget(itarget.TargetGetRoot(), _cameraPath);
                    _camera = t ? t.camera : null;
                }
            }

            if(!_cameraEnd) {
                if(!string.IsNullOrEmpty(_cameraEndPath)) {
                    Transform t = itarget.TargetGetCache(_cameraEndPath);
                    if(!t)
                        t = AMUtil.GetTarget(itarget.TargetGetRoot(), _cameraEndPath);
                    _cameraEnd = t ? t.camera : null;
                }
            }

            _cameraPath = "";
            _cameraEndPath = "";
        }
    }

    public override void CopyTo(AMKey key) {
        AMCameraSwitcherKey a = key as AMCameraSwitcherKey;
        a.type = type;
        a._camera = _camera;
        a._cameraPath = _cameraPath;
        a.color = color;
        a.cameraFadeType = cameraFadeType;
        a.cameraFadeParameters = new List<float>(cameraFadeParameters);
        a.irisShape = irisShape;
        a.still = still;
    }

    public override int getNumberOfFrames() {
        return endFrame - frame;
    }

    public float getTime(int frameRate) {
        return (float)getNumberOfFrames() / (float)frameRate;
    }

    public bool hasTargets(AMITarget itarget) {
        if(hasStartTarget(itarget) && hasEndTarget(itarget)) return true;
        return false;
    }
    public bool hasStartTarget(AMITarget itarget) {
        if(type == 0 && !getCamera(itarget)) return false;
        //else if(!startColor) return false;
        return true;
    }
    public bool hasEndTarget(AMITarget itarget) {
        if(endFrame == -1 ||(typeEnd == 0 && !getCameraEnd(itarget))) return false;
        //else if(!endColor) return false;
        return true;
    }
    public bool targetsAreEqual(AMITarget itarget) {
        if(type != typeEnd) return false;
        if(type == 0 && getCamera(itarget) != getCameraEnd(itarget)) return false;
        else if(type == 1 && color != colorEnd) return false;
        return true;
    }
    public string getStartTargetName(AMITarget itarget) {
        if(type == 0) {
            Camera cam = getCamera(itarget);
            if(cam) return cam.name;
            else return "None";
        }
        else
            return "Color";
    }
    public string getEndTargetName(AMITarget itarget) {
        if(typeEnd == 0) {
            Camera cam = getCameraEnd(itarget);
            if(cam) return cam.name;
            else return "None";
        }
        else
            return "Color";
    }

    public bool isReversed() {
        //TODO
        return false;
    }

    public override Tweener buildTweener(AMITarget itarget, Sequence sequence, UnityEngine.Object obj, int frameRate) {
        //TODO
        return null;
    }
}
