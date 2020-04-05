using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;

namespace M8.Animator {
    [System.Serializable]
    public abstract class PathKeyBase : Key {
        public override int keyCount { get { return path != null ? path.wps.Length : 1; } }

        public override bool isValid { get { return endFrame != -1; } }

        public const int pathResolution = 10;

        public int endFrame;

        public bool isConstSpeed = false;

        public TweenPlugPath path { get { return _paths.Length > 0 ? _paths[0] : null; } } //save serialize size by using array (path has a lot of serialized fields)
        [SerializeField] TweenPlugPath[] _paths;

        protected abstract TweenPlugPathPoint GeneratePathPoint(Track track);

        public override void CopyTo(Key key) {
            base.CopyTo(key);

            PathKeyBase a = (PathKeyBase)key;

            a.isConstSpeed = isConstSpeed;
        }

        public override int getNumberOfFrames(int frameRate) {
            if(!canTween && (endFrame == -1 || endFrame == frame))
                return 1;
            else if(endFrame == -1)
                return -1;
            return endFrame - frame;
        }

        /// <summary>
        /// Generate path points and endFrame. keyInd is the index of this key in the track.
        /// </summary>
        public void GeneratePath(Track track, int keyInd) {
            switch(interp) {
                case Interpolation.None:
                    _paths = new TweenPlugPath[0];
                    endFrame = keyInd + 1 < track.keys.Count ? track.keys[keyInd + 1].frame : frame;
                    break;

                case Interpolation.Linear:
                    _paths = new TweenPlugPath[0];

                    if(keyInd + 1 < track.keys.Count) {
                        var nextKey = track.keys[keyInd + 1];
                        endFrame = nextKey.frame;
                    }
                    else { //fail-safe
                        endFrame = -1;
                    }
                    break;

                case Interpolation.Curve:
                    //if there's more than 2 keys, and next key is curve, then it's more than 2 pts.
                    if(keyInd + 2 < track.keys.Count && track.keys[keyInd + 1].interp == Interpolation.Curve) {
                        var pathList = new List<TweenPlugPathPoint>();
                        var timeList = new List<float>();

                        for(int i = keyInd; i < track.keys.Count; i++) {
                            var key = (PathKeyBase)track.keys[i];

                            pathList.Add(key.GeneratePathPoint(track));
                            endFrame = key.frame;

                            if(!isConstSpeed)
                                timeList.Add(key.frame);

                            if(key.interp != Interpolation.Curve)
                                break;
                        }

                        //normalize frames (exclude beginning (0f) and end (1f))
                        if(timeList.Count > 0) {
                            //remove first and last
                            timeList.RemoveAt(timeList.Count - 1); timeList.RemoveAt(0);

                            float frameCount = endFrame - frame;
                            if(frameCount > 0f) {
                                for(int i = 0; i < timeList.Count; i++)
                                    timeList[i] = (timeList[i] - frame) / frameCount;
                            }
                            else { //fail-safe
                                for(int i = 0; i < timeList.Count; i++)
                                    timeList[i] = 0f;
                            }
                        }

                        var newPath = new TweenPlugPath(TweenPlugPathType.CatmullRom, pathList.ToArray(), timeList.ToArray(), isConstSpeed, pathResolution);
                        newPath.Init();

                        _paths = new TweenPlugPath[] { newPath };
                    }
                    else {
                        if(keyInd + 1 < track.keys.Count) {
                            endFrame = track.keys[keyInd + 1].frame;
                            _paths = new TweenPlugPath[0];
                        }
                        else
                            Invalidate();
                    }
                    break;
            }
        }

        public void Invalidate() {
            endFrame = -1;
            _paths = new TweenPlugPath[0];
        }
    }
}