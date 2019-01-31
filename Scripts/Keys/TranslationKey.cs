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
    public class TranslationKey : Key {
        public override SerializeType serializeType { get { return SerializeType.Translation; } }

        public const int pathResolution = 10;
        public const int SUBDIVISIONS_MULTIPLIER = 16;

        public Vector3 position;

        public int endFrame;
        public Vector3[] path;

        public bool isConstSpeed = true;

        public bool isClosed { get { return path.Length > 1 && path[0] == path[path.Length - 1]; } }

        private PathDataPreview mPathPreview;

        public PathDataPreview pathPreview {
            get {
                if(mPathPreview == null) {
                    int indMod = 1;
                    int pAdd = isClosed ? 1 : 0;
                    int len = path.Length;

                    Vector3[] pts = new Vector3[len + 2 + pAdd];
                    for(int i = 0; i < len; ++i)
                        pts[i + indMod] = path[i];

                    len = pts.Length;

                    if(isClosed) {
                        // Close path.
                        pts[len - 2] = pts[1];
                    }

                    // Add control points.
                    if(isClosed) {
                        pts[0] = pts[len - 3];
                        pts[len - 1] = pts[2];
                    }
                    else {
                        pts[0] = pts[1];
                        Vector3 lastP = pts[len - 2];
                        Vector3 diffV = lastP - pts[len - 3];
                        pts[len - 1] = lastP + diffV;
                    }

                    // Create the path.
                    mPathPreview = new PathDataPreview((Interpolation)interp == Interpolation.Curve ? PathType.CatmullRom : PathType.Linear, pts);

                    // Store arc lengths tables for constant speed.
                    mPathPreview.StoreTimeToLenTables(mPathPreview.path.Length * SUBDIVISIONS_MULTIPLIER);
                }

                return mPathPreview;
            }

            set { mPathPreview = value; }
        }

        public Vector3 GetPoint(float t) {
            return isConstSpeed ? pathPreview.GetConstPoint(t) : pathPreview.GetPoint(t);
        }

        // copy properties from key
        public override void CopyTo(Key key) {
            base.CopyTo(key);

            TranslationKey a = key as TranslationKey;

            a.position = position;

            a.isConstSpeed = isConstSpeed;
        }

        #region action
        
        public override int getNumberOfFrames(int frameRate) {
            if(!canTween && (endFrame == -1 || endFrame == frame))
                return 1;
            else if(endFrame == -1)
                return -1;
            return endFrame - frame;
        }
        public override void build(SequenceControl seq, Track track, int index, UnityEngine.Object obj) {
            int frameRate = seq.take.frameRate;

            //allow tracks with just one key
            if(track.keys.Count == 1)
                interp = Interpolation.None;

            var trans = obj as Transform;
            var transParent = trans.parent;

            Rigidbody body = trans.GetComponent<Rigidbody>();
            Rigidbody2D body2D = !body ? trans.GetComponent<Rigidbody2D>() : null;

            var tTrack = track as TranslationTrack;
            bool pixelSnap = tTrack.pixelSnap;
            float ppu = tTrack.pixelPerUnit;

            if(!canTween) {
                //TODO: world position
                Vector3 pos = pixelSnap ? new Vector3(Mathf.Round(position.x * ppu) / ppu, Mathf.Round(position.y * ppu) / ppu, Mathf.Round(position.z * ppu) / ppu) : position;

                TweenerCore<Vector3, Vector3, TweenPlugValueSetOptions> tweener;
                                
                if(body2D)
                    tweener = DOTween.To(new TweenPlugValueSet<Vector3>(), () => pos, (x) => body2D.position = transParent.TransformPoint(x), pos, 1.0f / frameRate); //getTime(frameRate)
                else if(body)
                    tweener = DOTween.To(new TweenPlugValueSet<Vector3>(), () => pos, (x) => body.position = transParent.TransformPoint(x), pos, 1.0f / frameRate); //getTime(frameRate)
                else
                    tweener = DOTween.To(new TweenPlugValueSet<Vector3>(), () => pos, (x) => trans.localPosition = x, pos, 1.0f / frameRate); //getTime(frameRate)

                tweener.plugOptions.SetSequence(seq);

                seq.Insert(this, tweener);
            }
            else {
                if(path.Length <= 1) return;
                if(getNumberOfFrames(seq.take.frameRate) <= 0) return;

                TweenerCore<Vector3, Path, PathOptions> ret = null;

                bool isRelative = false;

                PathType pathType = path.Length == 2 ? PathType.Linear : PathType.CatmullRom;

                var pathTween = new Path(pathType, path, pathResolution);
                var timeTween = getTime(frameRate);

                if(body2D) {
                    if(pixelSnap)
                        ret = DOTween.To(PathPlugin.Get(), () => path[0], x => body2D.MovePosition(transParent.TransformPoint(new Vector2(Mathf.Round(x.x * ppu) / ppu, Mathf.Round(x.y * ppu) / ppu))), pathTween, timeTween).SetTarget(body2D);
                    else
                        ret = DOTween.To(PathPlugin.Get(), () => path[0], x => body2D.MovePosition(transParent.TransformPoint(x)), pathTween, timeTween).SetTarget(body2D);
                }
                else if(body) {
                    if(pixelSnap)
                        ret = DOTween.To(PathPlugin.Get(), () => path[0], x => body.MovePosition(transParent.TransformPoint(new Vector3(Mathf.Round(x.x * ppu) / ppu, Mathf.Round(x.y * ppu) / ppu, Mathf.Round(x.z * ppu) / ppu))), pathTween, timeTween).SetTarget(body);
                    else
                        ret = DOTween.To(PathPlugin.Get(), () => path[0], x => body.MovePosition(transParent.TransformPoint(x)), pathTween, timeTween).SetTarget(body);
                }
                else {
                    if(pixelSnap)
                        ret = DOTween.To(PathPlugin.Get(), () => path[0], x => trans.localPosition = new Vector3(Mathf.Round(x.x * ppu) / ppu, Mathf.Round(x.y * ppu) / ppu, Mathf.Round(x.z * ppu) / ppu), pathTween, timeTween).SetTarget(trans);
                    else
                        ret = DOTween.To(PathPlugin.Get(), () => path[0], x => trans.localPosition = x, pathTween, timeTween).SetTarget(trans);
                }

                ret.SetRelative(isRelative).SetOptions(isClosed);

                if(hasCustomEase())
                    ret.SetEase(easeCurve);
                else
                    ret.SetEase(easeType, amplitude, period);

                seq.Insert(this, ret);
            }
        }
        #endregion
    }
}