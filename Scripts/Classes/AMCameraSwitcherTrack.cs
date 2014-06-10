using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween.Core;
using Holoville.HOTween;

public class AMCameraSwitcherTrack : AMTrack {
    private Camera[] _cachedAllCameras;
    /*public Camera[] cachedAllCameras {
        get {
            if(_cachedAllCameras == null) {
                _cachedAllCameras = getAllCameras();
            }
            return _cachedAllCameras;
        }
        set {
            _cachedAllCameras = value;
        }
    }*/

    protected override void SetSerializeObject(UnityEngine.Object obj) { }

    protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) { return this; }

    public Camera[] GetCachedCameras(AMITarget itarget) {
        if(_cachedAllCameras == null)
            _cachedAllCameras = getAllCameras(itarget);
        return _cachedAllCameras;
    }

    public void SetCachedCameras(Camera[] cams) {
        _cachedAllCameras = cams;
    }

    public override string getTrackType() {
        return "Camera Switcher";
    }

    // add a new key
    public void addKey(AMITarget itarget, OnAddKey addCall, int _frame, Camera camera = null,/*int? type = null, Camera camera = null, Color? color = null,*/ AMCameraSwitcherKey keyToClone = null) {
        foreach(AMCameraSwitcherKey key in keys) {
            // if key exists on frame, update key
            if(key.frame == _frame) {
                if(camera != null) {
                    key.setCamera(itarget, camera);
                    key.type = 0;
                    updateCache(itarget);
                }
                return;
            }
        }
        AMCameraSwitcherKey a = addCall(gameObject, typeof(AMCameraSwitcherKey)) as AMCameraSwitcherKey;
        if(keyToClone) {
            keyToClone.CopyTo(a);
        }
        else {
            a.type = 0;
            a.still = !AMTakeData.isProLicense;
            a.easeType = (int)EaseType.EaseOutSine;
        }
        a.frame = _frame;
        if(camera != null) {
            a.setCamera(itarget, camera);
            a.type = 0;
        }
        // add a new key
        keys.Add(a);
        // update cache
        updateCache(itarget);
    }

    public override void updateCache(AMITarget itarget) {
        base.updateCache(itarget);

        for(int i = 0; i < keys.Count; i++) {
            AMCameraSwitcherKey key = keys[i] as AMCameraSwitcherKey;

            key.version = version;

            if(keys.Count > i+1) {
                AMCameraSwitcherKey nextKey = keys[i+1] as AMCameraSwitcherKey;
                key.endFrame = nextKey.frame;
                key.typeEnd = nextKey.type;
                if(key.typeEnd == 0) key.setCameraEnd(nextKey);
                else key.colorEnd = nextKey.color;
            }
            else 
                key.endFrame = -1;
        }

        _cachedAllCameras = getAllCameras(itarget);
    }

    public override void previewFrame(AMITarget itarget, float frame, int frameRate, AMTrack extraTrack = null) {
        bool isPreview = !Application.isPlaying;

        AMCameraFade.getCameraFade(isPreview);

        for(int i = 0; i < keys.Count; i++) {
            AMCameraSwitcherKey key = keys[i] as AMCameraSwitcherKey;
            
            //before first frame
            if(frame <= key.frame) {
                AMCameraFade.reset();
                if(!key.hasStartTarget(itarget)) return;

                if(key.type == 0)
                    AMUtil.SetTopCamera(key.getCamera(itarget), GetCachedCameras(itarget));
                else
                    showColor(key.color, isPreview);
                return;
            }
            //between first and last frame
            else if(frame <= key.endFrame) {
                if(!key.hasStartTarget(itarget) || !key.hasEndTarget(itarget)) return;
                //targets are equal
                if(key.targetsAreEqual(itarget)) {
                    AMCameraFade.reset();
                    if(key.type == 0)
                        AMUtil.SetTopCamera(key.getCamera(itarget), GetCachedCameras(itarget));
                    else
                        showColor(key.color, isPreview);
                }
                else {
                    AMCameraFade.clearRenderTexture();
                    previewCameraFade(itarget, frame, key, isPreview);
                }
                return;
            }
        }
    }

    public Camera[] getAllCameras(AMITarget itarget) {
        List<Camera> lsCameras = new List<Camera>();
        foreach(AMCameraSwitcherKey key in keys) {
            Camera cam = key.getCamera(itarget);
            if(key.type == 0 && cam) {
                if(lsCameras.IndexOf(cam) == -1) {
                    lsCameras.Add(cam);
                }
            }
        }
        return lsCameras.ToArray();
    }
        
    private void previewCameraFade(AMITarget itarget, float frame, AMCameraSwitcherKey action, bool isPreview) {
        // if transition is None, show end camera / color
        if(action.cameraFadeType == (int)AMCameraSwitcherKey.Fade.None) {
            // reset camera fade if visible
            // camera
            if(action.typeEnd == 0) {
                Camera endCam = action.getCameraEnd(itarget);
                if(endCam) AMUtil.SetTopCamera(endCam, GetCachedCameras(itarget));
                AMCameraFade.reset();
            }
            else {
                showColor(action.colorEnd, isPreview);
            }
            return;
        }
        // Get camerafade
        AMCameraFade cf = AMCameraFade.getCameraFade(isPreview);
        if(Application.isPlaying) cf.keepAlivePreview = true;
        cf.isReset = false;
        bool isReversed = action.isReversed();
        int firstTargetType = (isReversed ? action.typeEnd : action.type);
        int secondTargetType = (isReversed ? action.type : action.typeEnd);
        // Set render texture or colors if render texture is used
        setRenderTexture(itarget, cf, frame, firstTargetType, secondTargetType, isReversed, action, isPreview);
        setColors(cf, firstTargetType, secondTargetType, isReversed, action);

        if(cf.irisShape != action.irisShape) cf.irisShape = action.irisShape;
        cf.mode = action.cameraFadeType;
        cf.setupMaterials();
        cf.r = action.cameraFadeParameters.ToArray();
                
        float percentage = (float)(frame-action.frame)/(float)(action.endFrame-action.frame);
        float value;

        // calculate and set value
        if(action.hasCustomEase()) {
            value = AMUtil.EaseCustom(1.0f, -1.0f, percentage, action.easeCurve);
        }
        else {
            TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)action.easeType);
            value = ease(percentage, 1.0f, -1.0f, 1.0f, action.amplitude, action.period);
        }

        cf.value = value;
        cf.percent = percentage;
    }

    private void setColors(AMCameraFade cf, int firstTargetType, int secondTargetType, bool isReversed, AMCameraSwitcherKey action) {
        //if(firstTargetType != 1 && secondTargetType != 1) return;
        Color firstColor = (isReversed ? action.colorEnd : action.color);
        Color secondColor = (isReversed ? action.color : action.colorEnd);

        if(firstTargetType == 1) {
            cf.colorTex = firstColor;
            cf.hasColorTex = true;
        }
        else {
            cf.hasColorTex = false;
        }

        if(secondTargetType == 1) {
            cf.colorBG = secondColor;
            cf.hasColorBG = true;
        }
        else {
            cf.hasColorBG = false;
        }

        // send event to game view to repaint OnGUI
        if(!Application.isPlaying && (firstTargetType == 1 || secondTargetType == 1)) {
            cf.transform.position = new Vector3(cf.transform.position.x, cf.transform.position.y, cf.transform.position.z);
        }
    }
    // set render texture or colors if render texture is used (stills handled in AMTake)
    private void setRenderTexture(AMITarget itarget,  AMCameraFade cf, float frame, int firstTargetType, int secondTargetType, bool isReversed, 
        AMCameraSwitcherKey action, bool isPreview) {


        Camera firstCamera = (isReversed ? action.getCameraEnd(itarget) : action.getCamera(itarget));
        Camera secondCamera = (isReversed ? action.getCamera(itarget) : action.getCameraEnd(itarget));

        if(isReversed && frame == action.frame) {
            if(firstTargetType == 0) AMUtil.SetTopCamera(firstCamera, GetCachedCameras(itarget));
        }
        else {
            if(secondTargetType == 0) AMUtil.SetTopCamera(secondCamera, GetCachedCameras(itarget));
        }

        if(action.still || (firstTargetType != 0 && secondTargetType != 0)) return;

        bool isPro = AMTakeData.isProLicense;
        // first target is camera, set render texture
        if(firstTargetType == 0) {
            // if should update render texture
            if(/*!cf.tex ||*/ cf.shouldUpdateRenderTexture || (isPro && (!firstCamera.targetTexture || !cf.isRenderTextureSetupFor(firstCamera)/*|| firstCamera.targetTexture != cf.tex*/))) {
                if(isPro) {
                    cf.setupRenderTexture(firstCamera);

                }
                else {
                    cf.useRenderTexture = false;
                    // show place-holder if non-pro
                    cf.colorTex = Color.white;
                    cf.tex2d = AMCameraFade.placeholderTexture;
                    cf.hasColorTex = false;
                    cf.placeholder = true;
                }
            }
        }

    }

    public struct cfTuple {
        public int frame;
        public int type1;
        public int type2;
        public Camera camera1;
        public Camera camera2;
        public bool isReversed;

        public cfTuple(int _frame, int _type1, int _type2, Camera _camera1, Camera _camera2, bool _isReversed) {
            frame = _frame;
            type1 = _type1;
            type2 = _type2;
            camera1 = _camera1;
            camera2 = _camera2;
            isReversed = _isReversed;
        }

    }
    public cfTuple getCameraFadeTupleForFrame(AMITarget itarget, int frame) {
        if(keys == null || keys.Count <= 0) {
            return new cfTuple(0, 0, 0, null, null, false);
        }
        for(int i=0; i<keys.Count; i++) {
            AMCameraSwitcherKey key = keys[i] as AMCameraSwitcherKey;
            // compact
            if(frame < key.frame) {
                break;
            }
            else if(frame < key.endFrame) {
                if(!key.still || key.cameraFadeType == (int)AMCameraSwitcherKey.Fade.None || key.targetsAreEqual(itarget)) break;
                bool isReversed = key.isReversed();

                if(isReversed) return new cfTuple(key.endFrame, key.typeEnd, key.type, key.getCameraEnd(itarget), key.getCamera(itarget), isReversed);
                else return new cfTuple(key.frame, key.type, key.typeEnd, key.getCamera(itarget), key.getCameraEnd(itarget), isReversed);
                //return new cfTuple((isReversed ? (cache[i] as AMCameraSwitcherAction).endFrame : (cache[i] as AMCameraSwitcherAction).startFrame),(cache[i] as AMCameraSwitcherAction).startCamera,(cache[i] as AMCameraSwitcherAction).endCamera,isReversed);
            }
        }
        return new cfTuple(0, 0, 0, null, null, false);
    }

    private void showColor(Color color, bool isPreview) {
        AMCameraFade cf = AMCameraFade.getCameraFade(isPreview);
        bool shouldRepaint = false;
        if(!cf.hasColorTex || cf.colorTex != color) {
            cf.colorTex = color;
            cf.hasColorTex = true;
            shouldRepaint = true;
        }
        if(cf.isReset) {
            cf.isReset = false;
            shouldRepaint = true;
        }
        if(cf.hasColorBG) {
            cf.hasColorBG = false;
            shouldRepaint = true;
        }
        if(cf.value != 1f) {
            cf.value = 1f;
            cf.percent = 0f;
            shouldRepaint = true;
        }
        if(cf.mode != 0) {
            cf.mode = 0;
            shouldRepaint = true;
        }
        // send event to game view to repaint OnGUI
        if(!Application.isPlaying && shouldRepaint) cf.transform.position = new Vector3(cf.transform.position.x, cf.transform.position.y, cf.transform.position.z);
    }

    public override List<GameObject> getDependencies(AMITarget itarget) {
        List<GameObject> ls = new List<GameObject>();
        foreach(AMCameraSwitcherKey key in keys) {
            Camera cam = key.getCamera(itarget);
            if(key.type == 0 && cam) ls.Add(cam.gameObject);
        }
        return ls;
    }

    public override List<GameObject> updateDependencies(AMITarget itarget, List<GameObject> newReferences, List<GameObject> oldReferences) {
        List<GameObject> lsFlagToKeep = new List<GameObject>();
        for(int i=0; i<oldReferences.Count; i++) {
            foreach(AMCameraSwitcherKey key in keys) {
                if(key.type == 0 && key.camera && oldReferences[i] == key.camera.gameObject) {
                    Camera _camera = (Camera)newReferences[i].GetComponent(typeof(Camera));
                    // missing camera
                    if(!_camera) {
                        Debug.LogWarning("Animator: Camera Switcher component 'Camera' not found on new reference for GameObject '"+key.camera.gameObject.name+"'. Duplicate not replaced.");
                        lsFlagToKeep.Add(oldReferences[i]);
                        continue;
                    }
                    key.setCamera(itarget, _camera);
                }
            }
        }

        return lsFlagToKeep;
    }
}
