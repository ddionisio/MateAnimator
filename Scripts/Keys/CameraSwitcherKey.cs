using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DG.Tweening;
using DG.Tweening.Plugins;

namespace M8.Animator {
    [System.Serializable]
    public class CameraSwitcherKey : Key {
        public override SerializeType serializeType { get { return SerializeType.CameraSwitcher; } }

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

        public int type = 0;        // 0 = camera, 1 = color
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
        public bool still = false;  // is it still or does it use render texture

        public int endFrame;

        public Camera getCamera(ITarget itarget) {
            if(itarget.isMeta) {
                if(!string.IsNullOrEmpty(_cameraPath)) {
                    Transform t = itarget.GetCache(_cameraPath);
                    if(t)
                        return t.GetComponent<Camera>();
                    else {
                        t = Utility.GetTarget(itarget.root, _cameraPath);
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

        public bool cameraMatch(ITarget itarget, Camera camera) {
            return getCamera(itarget) == camera;
        }

        public bool setCamera(ITarget itarget, Camera camera) {
            if(getCamera(itarget) != camera) {
                if(camera) {
                    if(itarget.isMeta) {
                        _camera = null;
                        _cameraPath = Utility.GetPath(itarget.root, camera);
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

        public Camera getCameraEnd(ITarget itarget) {
            if(itarget.isMeta) {
                if(!string.IsNullOrEmpty(_cameraEndPath)) {
                    Transform t = itarget.GetCache(_cameraEndPath);
                    if(t)
                        return t.GetComponent<Camera>();
                    else {
                        t = Utility.GetTarget(itarget.root, _cameraEndPath);
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

        public void setCameraEnd(CameraSwitcherKey nextKey) {
            _cameraEnd = nextKey._camera;
            _cameraEndPath = nextKey._cameraPath;
        }

        public override void maintainKey(ITarget itarget, UnityEngine.Object targetObj) {
            if(itarget.isMeta) {
                if(string.IsNullOrEmpty(_cameraPath)) {
                    if(_camera) {
                        _cameraPath = Utility.GetPath(itarget.root, _camera);
                        itarget.SetCache(_cameraPath, _camera.transform);
                    }
                }

                if(string.IsNullOrEmpty(_cameraEndPath)) {
                    if(_cameraEnd) {
                        _cameraEndPath = Utility.GetPath(itarget.root, _cameraEnd);
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
                            t = Utility.GetTarget(itarget.root, _cameraPath);
                        _camera = t ? t.GetComponent<Camera>() : null;
                    }
                }

                if(!_cameraEnd) {
                    if(!string.IsNullOrEmpty(_cameraEndPath)) {
                        Transform t = itarget.GetCache(_cameraEndPath);
                        if(!t)
                            t = Utility.GetTarget(itarget.root, _cameraEndPath);
                        _cameraEnd = t ? t.GetComponent<Camera>() : null;
                    }
                }

                _cameraPath = "";
                _cameraEndPath = "";
            }
        }

        public override void CopyTo(Key key) {
            base.CopyTo(key);

            CameraSwitcherKey a = key as CameraSwitcherKey;
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

        public bool hasTargets(ITarget itarget) {
            if(hasStartTarget(itarget) && hasEndTarget(itarget)) return true;
            return false;
        }
        public bool hasStartTarget(ITarget itarget) {
            if(type == 0 && !getCamera(itarget)) return false;
            //else if(!startColor) return false;
            return true;
        }
        public bool hasEndTarget(ITarget itarget) {
            if(endFrame == -1 || (typeEnd == 0 && !getCameraEnd(itarget))) return false;
            //else if(!endColor) return false;
            return true;
        }
        public bool targetsAreEqual(ITarget itarget) {
            if(type != typeEnd) return false;
            if(type == 0 && getCamera(itarget) != getCameraEnd(itarget)) return false;
            else if(type == 1 && color != colorEnd) return false;
            return true;
        }
        public string getStartTargetName(ITarget itarget) {
            if(type == 0) {
                Camera cam = getCamera(itarget);
                if(cam) return cam.name;
                else return "None";
            }
            else
                return "Color";
        }
        public string getEndTargetName(ITarget itarget) {
            if(typeEnd == 0) {
                Camera cam = getCameraEnd(itarget);
                if(cam) return cam.name;
                else return "None";
            }
            else
                return "Color";
        }

        public bool isReversed() {
            return Utility.isTransitionReversed(cameraFadeType, cameraFadeParameters.ToArray());
        }

        public override void build(SequenceControl seq, Track track, int index, UnityEngine.Object target) {
            // if targets are equal do nothing
            if(endFrame == -1 || !hasTargets(seq.target) || targetsAreEqual(seq.target)) return;

            Camera[] allCameras = (track as CameraSwitcherTrack).GetCachedCameras(seq.target);
            int frameRate = seq.take.frameRate;
            float frameCount = getNumberOfFrames(frameRate);
            var itarget = seq.target;
            var _seq = seq.sequence;

            var tween = DOTween.To(new FloatPlugin(), () => 0f, (x) => {
                CameraFade cf = CameraFade.getCameraFade();

                PlayParam param = cf.playParam;
                if(param == null) {
                    param = cf.playParam = new PlayParam();
                }
                param.Apply(this, frameRate, itarget, allCameras, _seq.IsBackwards());

                cf.percent = x / frameCount;
                cf.value = 1.0f - cf.percent;
            }, frameCount, frameCount / frameRate);

            if(hasCustomEase())
                tween.SetEase(easeCurve);
            else
                tween.SetEase(easeType, amplitude, period);

            seq.Insert(this, tween);
        }

        void CameraFadeNoneTargets(int typeEnd, Color colorEnd, Camera camEnd, Camera[] allCams) {
            if(typeEnd == 0) {
                if(camEnd) Utility.SetTopCamera(camEnd, allCams);
            }
            else {
                ShowColor(colorEnd);
            }
        }

        void ShowColor(Color color) {
            CameraFade cf = CameraFade.getCameraFade();
            cf.keepAliveColor = true;
            cf.colorTex = color;
            cf.hasColorTex = true;
            cf.hasColorBG = false;
            cf.mode = 0;
            cf.value = 1f;
            cf.isReset = false;
        }

        void CameraGenerateFadeTargets(bool isReversed, Camera cam, Camera camEnd, Camera[] allCams, float[] parameters) {
            CameraFade cf = CameraFade.getCameraFade();
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
                    Utility.SetTopCamera(firstCamera, allCams);
                    firstCamera.Render();
                    cf.refreshScreenTex();
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
                Utility.SetTopCamera(secondCamera, allCams);
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

        void CameraEnd(bool isReversed, Camera cam, Camera camEnd, Camera[] allCams) {
            if(isReversed) {
                //set camEnd to top
                if(camEnd)
                    Utility.SetTopCamera(cam, allCams);
            }

            if(typeEnd == 0) {
                CameraFade.reset();
            }
            else {
                CameraFade cf = CameraFade.getCameraFade();
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

            if(CameraFade.hasInstance()) {
                CameraFade cf = CameraFade.getCameraFade();
                cf.clearScreenTex();
                cf.clearTexture();
                if(cf.keepAlives > 0) cf.keepAlives--;
            }
        }

        public class PlayParam {
            private CameraSwitcherKey mCamSwitcher;
            private bool mIsReversed;
            private Camera mCam;
            private Camera mCamEnd;
            private Camera[] mAllCams;
            private bool mBackwards;
            //private float[] mFadeParams;

            public void Apply(CameraSwitcherKey camSwitcher, int frameRate, ITarget itarget, Camera[] allCameras, bool backwards) {
                if(mCamSwitcher != camSwitcher) {
                    mCamSwitcher = camSwitcher;

                    float[] fadeParams = mCamSwitcher.cameraFadeParameters.ToArray();
                    mIsReversed = Utility.isTransitionReversed(mCamSwitcher.type, fadeParams);
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
}