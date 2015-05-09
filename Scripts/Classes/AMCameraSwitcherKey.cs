using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween;
using Holoville.HOTween.Plugins.Core;
using Holoville.HOTween.Core;

public class AMPlugCameraSwitcher : ABSTweenPlugin {
    private AMCameraSwitcherKey mCamSwitcher;
    private int mFrameRate;
    private AMITarget mITarget;
    private Camera[] mAllCams;
    private float mLastElapsed;

    protected override object startVal { get { return _startVal; } set { _startVal = value; } }

    protected override object endVal { get { return _endVal; } set { _endVal = value; } }

    public AMPlugCameraSwitcher(AMCameraSwitcherKey camSwitcher, int frameRate, AMITarget itarget, Camera[] allCameras)
        : base(null, false) {
        mCamSwitcher = camSwitcher;
        mFrameRate = frameRate;
        mITarget = itarget;
        mAllCams = allCameras;
    }

    protected override float GetSpeedBasedDuration(float p_speed) {
        return p_speed;
    }

    protected override void SetChangeVal() { }

    protected override void SetIncremental(int p_diffIncr) { }
    protected override void SetIncrementalRestart() { }

    protected override void DoUpdate(float p_totElapsed) {
        AMCameraFade cf = AMCameraFade.getCameraFade();

        AMCameraSwitcherKey.PlayParam param = cf.playParam;
        if(param == null) {
            param = cf.playParam = new AMCameraSwitcherKey.PlayParam();
        }
        param.Apply(mCamSwitcher, mFrameRate, mITarget, mAllCams, p_totElapsed - mLastElapsed < 0.0f);

        mLastElapsed = p_totElapsed;

        cf.value = ease(p_totElapsed, 1f, -1f, _duration, tweenObj.easeOvershootOrAmplitude, tweenObj.easePeriod);
        cf.percent = p_totElapsed/_duration;
    }

    protected override void SetValue(object p_value) { }
    protected override object GetValue() { return AMCameraFade.getCameraFade().value; }
}

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
        if(itarget.isMeta) {
            if(!string.IsNullOrEmpty(_cameraPath)) {
                Transform t = itarget.GetCache(_cameraPath);
                if(t)
                    return t.GetComponent<Camera>();
                else {
                    t = AMUtil.GetTarget(itarget.root, _cameraPath);
                    itarget.SetCache(_cameraPath, t);
                    if(t)
                        return t.GetComponent<Camera>();
                }
            }

            return null;
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
                if(itarget.isMeta) {
                    _camera = null;
                    _cameraPath = AMUtil.GetPath(itarget.root, camera);
                    itarget.SetCache(_cameraPath, camera.transform);

                }
                else {
                    _camera = camera;
                    _cameraPath = "";
                }
            }
            else {
                _camera = null;
                _cameraPath = "";
            }

            return true;
        }
        return false;
    }

    public Camera getCameraEnd(AMITarget itarget) {
        if(itarget.isMeta) {
            if(!string.IsNullOrEmpty(_cameraEndPath)) {
                Transform t = itarget.GetCache(_cameraEndPath);
                if(t)
                    return t.GetComponent<Camera>();
                else {
                    t = AMUtil.GetTarget(itarget.root, _cameraEndPath);
                    itarget.SetCache(_cameraEndPath, t);
                    if(t)
                        return t.GetComponent<Camera>();
                }
            }

            return null;
        }
        else
            return _cameraEnd;
    }

    public void setCameraEnd(AMCameraSwitcherKey nextKey) {
        _cameraEnd = nextKey._camera;
        _cameraEndPath = nextKey._cameraPath;
    }

    public override void maintainKey(AMITarget itarget, UnityEngine.Object targetObj) {
        if(itarget.isMeta) {
            if(string.IsNullOrEmpty(_cameraPath)) {
                if(_camera) {
                    _cameraPath = AMUtil.GetPath(itarget.root, _camera);
                    itarget.SetCache(_cameraPath, _camera.transform);
                }
            }

            if(string.IsNullOrEmpty(_cameraEndPath)) {
                if(_cameraEnd) {
                    _cameraEndPath = AMUtil.GetPath(itarget.root, _cameraEnd);
                    itarget.SetCache(_cameraEndPath, _cameraEnd.transform);
                }
            }

            _camera = null;
            _cameraEnd = null;
        }
        else {
            if(!_camera) {
                if(!string.IsNullOrEmpty(_cameraPath)) {
                    Transform t = itarget.GetCache(_cameraPath);
                    if(!t)
                        t = AMUtil.GetTarget(itarget.root, _cameraPath);
                    _camera = t ? t.GetComponent<Camera>() : null;
                }
            }

            if(!_cameraEnd) {
                if(!string.IsNullOrEmpty(_cameraEndPath)) {
                    Transform t = itarget.GetCache(_cameraEndPath);
                    if(!t)
                        t = AMUtil.GetTarget(itarget.root, _cameraEndPath);
                    _cameraEnd = t ? t.GetComponent<Camera>() : null;
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

    public override int getNumberOfFrames(int frameRate) {
        if(endFrame == -1)
            return -1;
        return endFrame - frame;
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
        return AMUtil.isTransitionReversed(cameraFadeType, cameraFadeParameters.ToArray());
    }

    public override void build(AMSequence seq, AMTrack track, int index, UnityEngine.Object target) {
        // if targets are equal do nothing
        if(endFrame == -1 || !hasTargets(seq.target) || targetsAreEqual(seq.target)) return;

        Camera[] allCameras = (track as AMCameraSwitcherTrack).GetCachedCameras(seq.target);

        //use 'this' with property 'type' as a placeholder since AMPlugCameraSwitcher does not require any property
        seq.Insert(this, HOTween.To(this, getTime(seq.take.frameRate), new TweenParms().Prop("type", 
            new AMPlugCameraSwitcher(this, seq.take.frameRate, seq.target, allCameras))));
    }
        
    void CameraFadeNoneTargets(int typeEnd, Color colorEnd, Camera camEnd, Camera[] allCams) {
        if(typeEnd == 0) {
            if(camEnd) AMUtil.SetTopCamera(camEnd, allCams);
        }
        else {
            ShowColor(colorEnd);
        }
    }

    void ShowColor(Color color) {
        AMCameraFade cf = AMCameraFade.getCameraFade();
        cf.keepAliveColor = true;
        cf.colorTex = color;
        cf.hasColorTex = true;
        cf.hasColorBG = false;
        cf.mode = 0;
        cf.value = 1f;
        cf.isReset = false;
    }

    void CameraGenerateFadeTargets(bool isReversed, Camera cam, Camera camEnd, Camera[] allCams, float[] parameters) {
        AMCameraFade cf = AMCameraFade.getCameraFade();
        cf.incrementKeepAlives(true);

        if(cf.keepAliveColor) cf.keepAliveColor = false;
        cf.isReset = false;

        Camera firstCamera = null;
        Camera secondCamera = null;
        Color? firstColor = null;
        Color? secondColor = null;

        if(isReversed) {
            if(camEnd) firstCamera = camEnd;
            else if(typeEnd == 1) firstColor = colorEnd;
            if(cam) secondCamera = cam;
            else if(type == 1) secondColor = color;
        }
        else {
            if(cam) firstCamera = cam;
            else if(type == 1) firstColor = color;
            if(camEnd) secondCamera = camEnd;
            else if(typeEnd == 1) secondColor = colorEnd;
        }
        // setup first target
        if(firstCamera) {
            // camera
            if(!still) {
                cf.setupRenderTexture(firstCamera);
            }
            else {
                AMUtil.SetTopCamera(firstCamera, allCams);
                firstCamera.Render();
                cf.clearTexture2D();
                cf.tex2d = GetScreenTexture();
                cf.useRenderTexture = false;
                cf.hasColorTex = false;
            }
        }
        else {
            // color
            cf.colorTex = (Color)firstColor;
            cf.hasColorTex = true;
        }
        // setup second target
        if(secondCamera) {
            // camera
            AMUtil.SetTopCamera(secondCamera, allCams);
            cf.hasColorBG = false;
        }
        else {
            // color
            cf.colorBG = (Color)secondColor;
            cf.hasColorBG = true;
        }
        // iris shape
        if(irisShape) {
            cf.irisShape = irisShape;
            //cf.setupMaterials();
        }
        cf.mode = cameraFadeType;
        // setup camera fade
        cf.setupMaterials();
        cf.r = parameters;
        cf.value = 1f;
        cf.percent = 0f;
    }

    Texture2D GetScreenTexture() {
        Texture2D tex2d = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex2d.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
        tex2d.Apply();
        return tex2d;
    }

    void CameraEnd(bool isReversed, Camera cam, Camera camEnd, Camera[] allCams) {
        if(isReversed) {
            //set camEnd to top
            if(camEnd)
                AMUtil.SetTopCamera(cam, allCams);
        }

        if(typeEnd == 0) {
            AMCameraFade.reset();
        }
        else {
            AMCameraFade cf = AMCameraFade.getCameraFade();
            cf.keepAliveColor = true;
            cf.hasColorTex = true;
            cf.hasColorBG = false;
            cf.colorTex = colorEnd;
            cf.mode = 0;
            cf.value = 1.0f;
        }

        if(!still) {
            if(cam) cam.targetTexture = null;
            if(camEnd) camEnd.targetTexture = null;
        }

        if(AMCameraFade.hasInstance()) {
            AMCameraFade cf = AMCameraFade.getCameraFade();
            cf.clearTexture2D();
            cf.clearTexture();
            if(cf.keepAlives > 0) cf.keepAlives--;
        }
    }

    public class PlayParam {
        private AMCameraSwitcherKey mCamSwitcher;
        private bool mIsReversed;
        private Camera mCam;
        private Camera mCamEnd;
        private Camera[] mAllCams;
        private bool mBackwards;
        //private float[] mFadeParams;

        public void Apply(AMCameraSwitcherKey camSwitcher, int frameRate, AMITarget itarget, Camera[] allCameras, bool backwards) {
            if(mCamSwitcher != camSwitcher) {
                mCamSwitcher = camSwitcher;

                float[] fadeParams = mCamSwitcher.cameraFadeParameters.ToArray();
                mIsReversed = AMUtil.isTransitionReversed(mCamSwitcher.type, fadeParams);
                mCam = mCamSwitcher.getCamera(itarget);
                mCamEnd = mCamSwitcher.getCameraEnd(itarget);
                mAllCams = allCameras;

                if(mCamSwitcher.cameraFadeType == (int)Fade.None) {
                    mCamSwitcher.CameraFadeNoneTargets(mCamSwitcher.typeEnd, mCamSwitcher.colorEnd, mCamEnd, mAllCams);
                    mCamSwitcher.CameraEnd(mIsReversed, mCam, mCamEnd, mAllCams);
                }
                else {
                    mCamSwitcher.CameraGenerateFadeTargets(mIsReversed, mCam, mCamEnd, mAllCams, fadeParams);
                }
            }

            mBackwards = backwards;
        }

        public void End() {
            mCamSwitcher.CameraEnd(mBackwards ? !mIsReversed : mIsReversed, mCam, mCamEnd, mAllCams);
        }
    }
}
