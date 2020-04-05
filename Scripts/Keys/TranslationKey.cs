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
    public class TranslationKey : PathKeyBase {
        public override SerializeType serializeType { get { return SerializeType.Translation; } }

        public Vector3 position;
                
        /// <summary>
        /// Grab position within t = [0, 1]. keyInd is the index of this key in the track.
        /// </summary>
        public Vector3 GetPoint(float t) {
            if(path == null) //not tweenable
                return position;

            float finalT;

            if(hasCustomEase())
                finalT = Utility.EaseCustom(0.0f, 1.0f, t, easeCurve);
            else {
                var ease = Utility.GetEasingFunction(easeType);
                finalT = ease(t, 1f, amplitude, period);
                if(float.IsNaN(finalT)) //this really shouldn't happen...
                    return position;
            }

            var pt = path.GetPoint(finalT);

            return pt.valueVector3;
        }

        public void DrawGizmos(TranslationKey nextKey, Transform transform, float ptSize) {
            Matrix4x4 mtx = transform.parent ? transform.parent.localToWorldMatrix : Matrix4x4.identity;

            if(interp == Interpolation.None) {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(mtx.MultiplyPoint3x4(position), ptSize);
            }
            else if(interp == Interpolation.Linear || path == null) {
                if(nextKey == null) {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(mtx.MultiplyPoint3x4(position), ptSize);
                }
                else {
                    Vector3 pt1 = mtx.MultiplyPoint3x4(position), pt2 = mtx.MultiplyPoint3x4(nextKey.position);

                    Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);

                    Gizmos.DrawLine(pt1, pt2);

                    Gizmos.color = Color.green;

                    Gizmos.DrawSphere(pt1, ptSize);
                    Gizmos.DrawSphere(pt2, ptSize);
                }
            }
            else if(interp == Interpolation.Curve) {
                Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);

                int subdivisions = pathResolution * keyCount;
                for(int i = 0; i < subdivisions; i++) {
                    var pt = path.GetPoint(i / (float)subdivisions);
                    var pt1 = pt.valueVector3;

                    pt = path.GetPoint((i + 1) / (float)subdivisions);
                    var pt2 = pt.valueVector3;

                    Gizmos.DrawLine(mtx.MultiplyPoint3x4(pt1), mtx.MultiplyPoint3x4(pt2));
                }

                Gizmos.color = Color.green;

                for(int i = 0; i < path.wps.Length; i++)
                    Gizmos.DrawSphere(mtx.MultiplyPoint3x4(path.wps[i].valueVector3), ptSize);
            }
        }

        // copy properties from key
        public override void CopyTo(Key key) {
            base.CopyTo(key);

            TranslationKey a = (TranslationKey)key;

            a.position = position;
        }

        protected override TweenPlugPathPoint GeneratePathPoint(Track track) {
            return new TweenPlugPathPoint(position);
        }

        #region action

        public override void build(SequenceControl seq, Track track, int index, UnityEngine.Object obj) {
            //allow tracks with just one key
            if(track.keys.Count == 1)
                interp = Interpolation.None;
            else if(canTween) {
                //invalid or in-between keys
                if(endFrame == -1) return;
            }

            var trans = obj as Transform;
            
            Rigidbody body = trans.GetComponent<Rigidbody>();
            Rigidbody2D body2D = !body ? trans.GetComponent<Rigidbody2D>() : null;

            var tTrack = track as TranslationTrack;
            bool pixelSnap = tTrack.pixelSnap;
            float ppu = tTrack.pixelPerUnit;

            int frameRate = seq.take.frameRate;
            float time = getTime(frameRate);

            if(interp == Interpolation.None) {
                //TODO: world position
                Vector3 pos = pixelSnap ? new Vector3(Mathf.Round(position.x * ppu) / ppu, Mathf.Round(position.y * ppu) / ppu, Mathf.Round(position.z * ppu) / ppu) : position;

                TweenerCore<Vector3, Vector3, TWeenPlugNoneOptions> tweener;

                if(body2D)
                    tweener = DOTween.To(TweenPlugValueSet<Vector3>.Get(), () => trans.localPosition, (x) => {
                        var parent = trans.parent;
                        if(parent)
                            body2D.position = parent.TransformPoint(x);
                        else
                            body2D.position = x;
                    }, pos, time); //1.0f / frameRate
                else if(body)
                    tweener = DOTween.To(TweenPlugValueSet<Vector3>.Get(), () => trans.localPosition, (x) => {
                        var parent = trans.parent;
                        if(parent)
                            body.position = parent.TransformPoint(x); 
                        else
                            body.position = x;
                    }, pos, time); //1.0f / frameRate
                else
                    tweener = DOTween.To(TweenPlugValueSet<Vector3>.Get(), () => trans.localPosition, (x) => trans.localPosition = x, pos, time); //1.0f / frameRate

                seq.Insert(this, tweener);
            }
            else if(interp == Interpolation.Linear || path == null) {
                Vector3 endPos = (track.keys[index + 1] as TranslationKey).position;

                DOSetter<Vector3> setter;
                if(body2D) {
                    if(pixelSnap)
                        setter = x => {
                            var parent = trans.parent;
                            if(parent)
                                body2D.MovePosition(parent.TransformPoint(new Vector2(Mathf.Round(x.x * ppu) / ppu, Mathf.Round(x.y * ppu) / ppu)));
                            else
                                body2D.MovePosition(new Vector2(Mathf.Round(x.x * ppu) / ppu, Mathf.Round(x.y * ppu) / ppu));
                        };
                    else
                        setter = x => {
                            var parent = trans.parent;
                            if(parent)
                                body2D.MovePosition(parent.TransformPoint(x)); 
                            else
                                body2D.MovePosition(x);
                        };
                }
                else if(body) {
                    if(pixelSnap)
                        setter = x => {
                            var parent = trans.parent;
                            if(parent)
                                body.MovePosition(parent.TransformPoint(new Vector3(Mathf.Round(x.x * ppu) / ppu, Mathf.Round(x.y * ppu) / ppu, Mathf.Round(x.z * ppu) / ppu)));
                            else
                                body.MovePosition(new Vector3(Mathf.Round(x.x * ppu) / ppu, Mathf.Round(x.y * ppu) / ppu, Mathf.Round(x.z * ppu) / ppu));
                        };
                    else
                        setter = x => {
                            var parent = trans.parent;
                            if(parent)
                                body.MovePosition(parent.TransformPoint(x));
                            else
                                body.MovePosition(x);
                        };
                }
                else {
                    if(pixelSnap)
                        setter = x => trans.localPosition = new Vector3(Mathf.Round(x.x * ppu) / ppu, Mathf.Round(x.y * ppu) / ppu, Mathf.Round(x.z * ppu) / ppu);
                    else
                        setter = x => trans.localPosition = x;
                }

                var linearTween = DOTween.To(TweenPluginFactory.CreateVector3(), () => position, setter, endPos, time);

                if(hasCustomEase())
                    linearTween.SetEase(easeCurve);
                else
                    linearTween.SetEase(easeType, amplitude, period);

                seq.Insert(this, linearTween);
            }
            else if(interp == Interpolation.Curve) {
                var options = new TweenPlugPathOptions { loopType = LoopType.Restart };

                DOSetter<Vector3> setter;
                if(body2D) {
                    if(pixelSnap)
                        setter = x => {
                            var parent = trans.parent;
                            if(parent)
                                body2D.MovePosition(parent.TransformPoint(new Vector2(Mathf.Round(x.x * ppu) / ppu, Mathf.Round(x.y * ppu) / ppu)));
                            else
                                body2D.MovePosition(new Vector2(Mathf.Round(x.x * ppu) / ppu, Mathf.Round(x.y * ppu) / ppu));
                        };
                    else
                        setter = x => {
                            var parent = trans.parent;
                            if(parent)
                                body2D.MovePosition(parent.TransformPoint(x));
                            else
                                body2D.MovePosition(x);
                        };
                }
                else if(body) {
                    if(pixelSnap)
                        setter = x => {
                            var parent = trans.parent;
                            if(parent)
                                body.MovePosition(parent.TransformPoint(new Vector3(Mathf.Round(x.x * ppu) / ppu, Mathf.Round(x.y * ppu) / ppu, Mathf.Round(x.z * ppu) / ppu)));
                            else
                                body.MovePosition(new Vector3(Mathf.Round(x.x * ppu) / ppu, Mathf.Round(x.y * ppu) / ppu, Mathf.Round(x.z * ppu) / ppu));
                        };
                    else
                        setter = x => {
                            var parent = trans.parent;
                            if(parent)
                                body.MovePosition(parent.TransformPoint(x));
                            else
                                body.MovePosition(x);
                        };
                }
                else {
                    if(pixelSnap)
                        setter = x => trans.localPosition = new Vector3(Mathf.Round(x.x * ppu) / ppu, Mathf.Round(x.y * ppu) / ppu, Mathf.Round(x.z * ppu) / ppu);
                    else
                        setter = x => trans.localPosition = x;
                }

                var pathTween = DOTween.To(TweenPlugPathVector3.Get(), () => position, setter, path, time);
                pathTween.plugOptions = options;

                if(hasCustomEase())
                    pathTween.SetEase(easeCurve);
                else
                    pathTween.SetEase(easeType, amplitude, period);

                seq.Insert(this, pathTween);
            }
        }
        #endregion
    }
}