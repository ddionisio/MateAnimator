using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Core.Enums;
using DG.Tweening.Core.Easing;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;

namespace M8.Animator {
    public enum OrientMode {
        None,
        ThreeDimension,
        TwoDimension, //orient right
        TwoDimensionUp //orient up
    }

    public struct TweenPlugOrient {
        public const float lookAhead = 0.001f;

        /// <summary>
        /// Grab orientation by direction torwards lookatPt (world space), returns rotation in world space.
        /// </summary>
        public static Quaternion GetOrientation(OrientMode orientMode, AxisFlags orientLockAxis, Transform trans, Vector3 lookatPt) {
            switch(orientMode) {
                case OrientMode.ThreeDimension:
                    Vector3 up;
                    ApplyLockAxis(orientLockAxis, trans, ref lookatPt, out up);

                    var dpos = lookatPt - trans.position;
                    if(dpos == Vector3.zero)
                        return trans.rotation;

                    return Quaternion.LookRotation(dpos, up);

                case OrientMode.TwoDimension:
                    var rotZ = Angle2DRight(trans.position, lookatPt);
                    if(rotZ < 0f) rotZ = 360f + rotZ;
                    return Quaternion.Euler(0f, 0f, rotZ);

                case OrientMode.TwoDimensionUp:
                    rotZ = Angle2DUp(trans.position, lookatPt);
                    if(rotZ < 0f) rotZ = 360f + rotZ;
                    return Quaternion.Euler(0f, 0f, rotZ);

                default:
                    return trans.rotation;
            }
        }

        /// <summary>
        /// Grab orientation by looking ahead of path, returns rotation in world space.
        /// </summary>
        public static Quaternion GetOrientation(OrientMode orientMode, AxisFlags orientLockAxis, Transform trans, TweenPlugPath path, float t) {
            var lookatPt = GetLookAhead(trans, t, path);
            if(trans.parent)
                lookatPt = trans.parent.TransformPoint(lookatPt);

            return GetOrientation(orientMode, orientLockAxis, trans, lookatPt);
        }

        /// <summary>
        /// Only uses modes: TwoDimension*. Grab orientation by direction torwards lookatPt (world space), returns rotation (Z) in world space.
        /// </summary>
        public static float GetOrientation2D(OrientMode orientMode, Transform trans, Vector3 lookatPt) {
            switch(orientMode) {                
                case OrientMode.TwoDimension:
                    var rotZ = Angle2DRight(trans.position, lookatPt);
                    if(rotZ < 0f) rotZ = 360f + rotZ;
                    return rotZ;

                case OrientMode.TwoDimensionUp:
                    rotZ = Angle2DUp(trans.position, lookatPt);
                    if(rotZ < 0f) rotZ = 360f + rotZ;
                    return rotZ;

                default:
                    return trans.eulerAngles.z;
            }
        }

        /// <summary>
        /// Only uses modes: TwoDimension*. Grab orientation by looking ahead of path, returns rotation (Z) in world space.
        /// </summary>
        public static float GetOrientation2D(OrientMode orientMode, Transform trans, TweenPlugPath path, float t) {
            var lookatPt = GetLookAhead(trans, t, path);
            if(trans.parent)
                lookatPt = trans.parent.TransformPoint(lookatPt);

            return GetOrientation2D(orientMode, trans, lookatPt);
        }

        private static void ApplyLockAxis(AxisFlags lockAxis, Transform trans, ref Vector3 lookAtPt, out Vector3 up) {
            Vector3 pt = trans.localPosition;

            if(lockAxis != AxisFlags.None) {
                up = Vector3.up;

                if((lockAxis & AxisFlags.X) != AxisFlags.None) {
                    Vector3 v0 = trans.InverseTransformPoint(lookAtPt);
                    v0.y = 0;
                    lookAtPt = trans.TransformPoint(v0);
                }
                if((lockAxis & AxisFlags.Y) != AxisFlags.None) {
                    Vector3 v0 = trans.InverseTransformPoint(lookAtPt);
                    if(v0.z < 0) v0.z = -v0.z; //avoid gimbal lock
                    v0.x = 0;
                    lookAtPt = trans.TransformPoint(v0);

                    up = trans.up;
                }
                if((lockAxis & AxisFlags.Z) != AxisFlags.None) {
                    up = trans.TransformDirection(Vector3.up);
                    up.z = 0f;
                    up.Normalize();
                }
            }
            else
                up = trans.up;
        }

        private static float Angle2DRight(Vector3 from, Vector3 to) {
            Vector2 baseDir = Vector2.right;
            to -= from;
            float ang = Vector2.Angle(baseDir, to);
            Vector3 cross = Vector3.Cross(baseDir, to);
            if(cross.z > 0) ang = 360 - ang;
            ang *= -1f;
            return ang;
        }

        private static float Angle2DUp(Vector3 from, Vector3 to) {
            Vector2 baseDir = Vector2.up;
            to -= from;
            float ang = Vector2.Angle(baseDir, to);
            Vector3 cross = Vector3.Cross(baseDir, to);
            if(cross.z > 0) ang = 360 - ang;
            ang *= -1f;
            return ang;
        }

        private static Vector3 GetLookAhead(Transform trans, float t, TweenPlugPath path) {
            if(path.type == TweenPlugPathType.Linear) {
                // Calculate lookAhead so that it doesn't turn until it starts moving on next waypoint
                return trans.localPosition + path.wps[path.linearWPIndex].valueVector3 - path.wps[path.linearWPIndex - 1].valueVector3;
            }
            else {
                float lookAheadPerc = t + lookAhead;
                if(lookAheadPerc > 1) lookAheadPerc = (path.isClosed ? lookAheadPerc - 1 : path.type == TweenPlugPathType.Linear ? 1 : 1.00001f);
                return path.GetPoint(lookAheadPerc).valueVector3;
            }
        }
    }

    public struct TweenPlugPathOrientOptions : IPlugOptions {
        public OrientMode orientMode;
        public AxisFlags lockAxis;

        void IPlugOptions.Reset() {
            orientMode = OrientMode.None;
            lockAxis = AxisFlags.None;
        }
    }

    /// <summary>
    /// Allow orientation to follow the path, this requires a target (transform, rigidbody, or rigidbody2D)
    /// </summary>
    public abstract class TweenPlugPathOrient : TweenPlugPathBase<Vector3, TweenPlugPathOrientOptions> {
        protected override bool ApproximatelyEqual(TweenPlugPathPoint pt, Vector3 curVal) { return pt.Approximately(curVal); }
        protected override bool Equal(TweenPlugPathPoint pt, Vector3 curVal) { return pt.valueVector3 == curVal; }
        protected override TweenPlugPathPoint CreatePoint(Vector3 src) { return new TweenPlugPathPoint(src); }
        protected override Vector3 GetValue(TweenPlugPathPoint pt) { return pt.valueVector3; }

        public override void EvaluateAndApply(TweenPlugPathOrientOptions options, Tween t, bool isRelative, DOGetter<Vector3> getter, DOSetter<Vector3> setter, float elapsed, TweenPlugPath startValue, TweenPlugPath changeValue, float duration, bool usingInversePosition, int newCompletedSteps, UpdateNotice updateNotice) {
            var path = changeValue;

            float pathPerc = EaseManager.Evaluate(t, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod);

            var pathPt = path.GetPoint(pathPerc);
            var pt = pathPt.valueVector3;
            setter(pt);

            if(t.target != null)
                SetOrientation(t.target, options, path, pathPerc);
        }

        protected abstract void SetOrientation(object target, TweenPlugPathOrientOptions options, TweenPlugPath path, float pathPerc);
    }

    /// <summary>
    /// Allow orientation to follow the path. Ensure tween's target is a Transform
    /// </summary>
    public class TweenPlugPathOrientTransform : TweenPlugPathOrient {
        protected override void SetOrientation(object target, TweenPlugPathOrientOptions options, TweenPlugPath path, float pathPerc) {
            var trans = (Transform)target;

            trans.rotation = TweenPlugOrient.GetOrientation(options.orientMode, options.lockAxis, trans, path, pathPerc);
        }

        public static TweenPlugPathOrientTransform Get() {
            if(mInstance == null) mInstance = new TweenPlugPathOrientTransform();
            return mInstance;
        }
        private static TweenPlugPathOrientTransform mInstance;
    }

    /// <summary>
    /// Allow orientation to follow the path. Ensure tween's target is a Rigidbody
    /// </summary>
    public class TweenPlugPathOrientRigidbody : TweenPlugPathOrient {
        protected override void SetOrientation(object target, TweenPlugPathOrientOptions options, TweenPlugPath path, float pathPerc) {
            var body = (Rigidbody)target;
            var trans = body.transform;

            var rot = TweenPlugOrient.GetOrientation(options.orientMode, options.lockAxis, trans, path, pathPerc);
            body.MoveRotation(rot);
        }

        public static TweenPlugPathOrientRigidbody Get() {
            if(mInstance == null) mInstance = new TweenPlugPathOrientRigidbody();
            return mInstance;
        }
        private static TweenPlugPathOrientRigidbody mInstance;
    }

    /// <summary>
    /// Allow orientation to follow the path. Ensure tween's target is a Rigidbody2D.
    /// This will only function in 2D mode.
    /// </summary>
    public class TweenPlugPathOrientRigidbody2D : TweenPlugPathOrient {
        protected override void SetOrientation(object target, TweenPlugPathOrientOptions options, TweenPlugPath path, float pathPerc) {
            var body = (Rigidbody2D)target;
            var trans = body.transform;

            var rot = TweenPlugOrient.GetOrientation2D(options.orientMode, trans, path, pathPerc);
            body.MoveRotation(rot);
        }

        public static TweenPlugPathOrientRigidbody2D Get() {
            if(mInstance == null) mInstance = new TweenPlugPathOrientRigidbody2D();
            return mInstance;
        }
        private static TweenPlugPathOrientRigidbody2D mInstance;
    }

    public struct TweenPlugVector3LookAtOptions : IPlugOptions {
        public OrientMode orientMode;
        public AxisFlags lockAxis;

        public Vector3 lookAtPt;
        public bool lookAtIsLocal; //if true, convert lookAtPt to world via target's parent

        void IPlugOptions.Reset() {
            orientMode = OrientMode.None;
            lockAxis = AxisFlags.None;
            lookAtIsLocal = false;
        }
    }

    /// <summary>
    /// Orient towards lookAtPt, this requires a target (transform, rigidbody, or rigidbody2D)
    /// </summary>
    public abstract class TweenPlugVector3LookAt : ABSTweenPlugin<Vector3, Vector3, TweenPlugVector3LookAtOptions> {
        public override void Reset(TweenerCore<Vector3, Vector3, TweenPlugVector3LookAtOptions> t) { }

        public override void SetFrom(TweenerCore<Vector3, Vector3, TweenPlugVector3LookAtOptions> t, bool isRelative) {
            Vector3 prevEndVal = t.endValue;
            t.endValue = t.getter();
            t.startValue = isRelative ? t.endValue + prevEndVal : prevEndVal;
            Vector3 to = t.startValue;
            t.setter(to);
        }
        public override void SetFrom(TweenerCore<Vector3, Vector3, TweenPlugVector3LookAtOptions> t, Vector3 fromValue, bool setImmediately, bool isRelative) {
            if(isRelative) {
                var currVal = t.getter();
                t.endValue += currVal;
                fromValue += currVal;
            }
            t.startValue = fromValue;
            if(setImmediately) {
                Vector3 to = fromValue;
                t.setter(to);
            }
        }

        public override Vector3 ConvertToStartValue(TweenerCore<Vector3, Vector3, TweenPlugVector3LookAtOptions> t, Vector3 value) { return value; }
        public override void SetRelativeEndValue(TweenerCore<Vector3, Vector3, TweenPlugVector3LookAtOptions> t) { t.endValue += t.startValue; }
        public override void SetChangeValue(TweenerCore<Vector3, Vector3, TweenPlugVector3LookAtOptions> t) { t.changeValue = t.endValue - t.startValue; }
        public override float GetSpeedBasedDuration(TweenPlugVector3LookAtOptions options, float unitsXSecond, Vector3 changeValue) { return changeValue.magnitude / unitsXSecond; }

        public override void EvaluateAndApply(TweenPlugVector3LookAtOptions options, Tween t, bool isRelative, DOGetter<Vector3> getter, DOSetter<Vector3> setter, float elapsed, Vector3 startValue, Vector3 changeValue, float duration, bool usingInversePosition, int newCompletedSteps, UpdateNotice updateNotice) {
            /*if(t.loopType == LoopType.Incremental) startValue += changeValue * (t.isComplete ? t.completedLoops - 1 : t.completedLoops);
            if(t.isSequenced && t.sequenceParent.loopType == LoopType.Incremental) {
                startValue += changeValue * (t.loopType == LoopType.Incremental ? t.loops : 1)
                    * (t.sequenceParent.isComplete ? t.sequenceParent.completedLoops - 1 : t.sequenceParent.completedLoops);
            }*/

        float easeVal = EaseManager.Evaluate(t, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod);

            startValue.x += changeValue.x * easeVal;
            startValue.y += changeValue.y * easeVal;
            startValue.z += changeValue.z * easeVal;

            setter(startValue);

            SetOrientation(t.target, options);
        }

        protected abstract void SetOrientation(object target, TweenPlugVector3LookAtOptions options);
    }

    /// <summary>
    /// Orient towards lookAtPt, this requires a Transform target
    /// </summary>
    public class TweenPlugVector3LookAtTransform : TweenPlugVector3LookAt {
        protected override void SetOrientation(object target, TweenPlugVector3LookAtOptions options) {
            var trans = (Transform)target;

            var lookAtPt = options.lookAtPt;
            if(options.lookAtIsLocal && trans.parent)
                lookAtPt = trans.parent.TransformPoint(lookAtPt);

            trans.rotation = TweenPlugOrient.GetOrientation(options.orientMode, options.lockAxis, trans, lookAtPt);
        }

        public static TweenPlugVector3LookAtTransform Get() {
            if(mInstance == null) mInstance = new TweenPlugVector3LookAtTransform();
            return mInstance;
        }
        private static TweenPlugVector3LookAtTransform mInstance;
    }

    /// <summary>
    /// Orient towards lookAtPt, this requires a Rigidbody target
    /// </summary>
    public class TweenPlugVector3LookAtRigidbody : TweenPlugVector3LookAt {
        protected override void SetOrientation(object target, TweenPlugVector3LookAtOptions options) {
            var body = (Rigidbody)target;
            var trans = body.transform;

            var lookAtPt = options.lookAtPt;
            if(options.lookAtIsLocal && trans.parent)
                lookAtPt = trans.parent.TransformPoint(lookAtPt);

            var rot = TweenPlugOrient.GetOrientation(options.orientMode, options.lockAxis, trans, lookAtPt);
            body.MoveRotation(rot);
        }

        public static TweenPlugVector3LookAtRigidbody Get() {
            if(mInstance == null) mInstance = new TweenPlugVector3LookAtRigidbody();
            return mInstance;
        }
        private static TweenPlugVector3LookAtRigidbody mInstance;
    }

    /// <summary>
    /// Orient towards lookAtPt, this requires a Rigidbody target
    /// </summary>
    public class TweenPlugVector3LookAtRigidbody2D : TweenPlugVector3LookAt {
        protected override void SetOrientation(object target, TweenPlugVector3LookAtOptions options) {
            var body = (Rigidbody2D)target;
            var trans = body.transform;

            var lookAtPt = options.lookAtPt;
            if(options.lookAtIsLocal && trans.parent)
                lookAtPt = trans.parent.TransformPoint(lookAtPt);

            var rot = TweenPlugOrient.GetOrientation2D(options.orientMode, trans, lookAtPt);
            body.MoveRotation(rot);
        }

        public static TweenPlugVector3LookAtRigidbody2D Get() {
            if(mInstance == null) mInstance = new TweenPlugVector3LookAtRigidbody2D();
            return mInstance;
        }
        private static TweenPlugVector3LookAtRigidbody2D mInstance;
    }
}