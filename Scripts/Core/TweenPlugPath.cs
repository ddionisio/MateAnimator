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
    [System.Serializable]
    public struct TweenPlugPathPoint {
        public float[] vals;

        public float valueFloat { get { return vals[0]; } set { SetSize(1); vals[0] = value; } }
        public Vector2 valueVector2 { get { return new Vector2(vals[0], vals[1]); } set { SetSize(2); vals[0] = value.x; vals[1] = value.y; } }
        public Vector3 valueVector3 { get { return new Vector3(vals[0], vals[1], vals[2]); } set { SetSize(3); vals[0] = value.x; vals[1] = value.y; vals[2] = value.z; } }
        public Vector4 valueVector4 { get { return new Vector4(vals[0], vals[1], vals[2], vals[3]); } set { SetSize(4); vals[0] = value.x; vals[1] = value.y; vals[2] = value.z; vals[3] = value.w; } }
        public Color valueColor { get { return new Color(vals[0], vals[1], vals[2], vals[3]); } set { SetSize(4); vals[0] = value.r; vals[1] = value.g; vals[2] = value.b; vals[3] = value.a; } }
        public Rect valueRect { get { return new Rect(vals[0], vals[1], vals[2], vals[3]); } set { SetSize(4); vals[0] = value.xMin; vals[1] = value.yMin; vals[2] = value.width; vals[3] = value.height; } }

        public TweenPlugPathPoint(int count) {
            vals = new float[count];
        }

        public TweenPlugPathPoint(float val) {
            vals = new float[] { val };
        }

        public TweenPlugPathPoint(Vector2 val) {
            vals = new float[] { val.x, val.y };
        }

        public TweenPlugPathPoint(Vector3 val) {
            vals = new float[] { val.x, val.y, val.z };
        }

        public TweenPlugPathPoint(Vector4 val) {
            vals = new float[] { val.x, val.y, val.z, val.w };
        }

        public TweenPlugPathPoint(Color val) {
            vals = new float[] { val.r, val.g, val.b, val.a };
        }

        public TweenPlugPathPoint(Rect val) {
            vals = new float[] { val.xMin, val.yMin, val.width, val.height };
        }

        public void SetSize(int length) {
            if(vals == null)
                vals = new float[length];
            else if(vals.Length < length)
                System.Array.Resize(ref vals, length);
        }

        public void Copy(TweenPlugPathPoint src, int length) {
            if(vals == null || vals.Length < length)
                vals = new float[length];

            System.Array.Copy(src.vals, vals, length);
        }

        public float Magnitude(int length) {
            if(length == 1)
                return Mathf.Abs(vals[0]);

            float sqrLen = 0f;
            for(int i = 0; i < length; i++) {
                var val = vals[i];
                sqrLen += val* val;
            }

            return Mathf.Sqrt(sqrLen);
        }

        public float MagnitudeSqr(int length) {
            if(length == 1)
                return vals[0] * vals[0];

            float sqrLen = 0f;
            for(int i = 0; i < length; i++) {
                var val = vals[i];
                sqrLen += val * val;
            }

            return sqrLen;
        }

        public void ClampMagnitude(float max, int length) {
            if(length == 1) {
                var magnitude = Mathf.Abs(vals[0]);
                if(magnitude > max)
                    vals[0] = Mathf.Sign(vals[0]) * max;
                return;
            }

            var len = Magnitude(length);
            if(len > max) {
                for(int i = 0; i < length; i++)
                    vals[i] = (vals[i] / len) * max;
            }
        }

        public bool Approximately(float val) { return Mathf.Approximately(valueFloat, val); }
        public bool Approximately(Vector2 val) { return Mathf.Approximately(vals[0], val.x) && Mathf.Approximately(vals[1], val.y); }
        public bool Approximately(Vector3 val) { return Mathf.Approximately(vals[0], val.x) && Mathf.Approximately(vals[1], val.y) && Mathf.Approximately(vals[2], val.z); }
        public bool Approximately(Vector4 val) { return Mathf.Approximately(vals[0], val.x) && Mathf.Approximately(vals[1], val.y) && Mathf.Approximately(vals[2], val.z) && Mathf.Approximately(vals[3], val.w); }
        public bool Approximately(Rect val) { return Mathf.Approximately(vals[0], val.xMin) && Mathf.Approximately(vals[1], val.yMin) && Mathf.Approximately(vals[2], val.width) && Mathf.Approximately(vals[3], val.height); }
        public bool Approximately(TweenPlugPathPoint other, int length) {
            for(int i = 0; i < length; i++) {
                if(!Mathf.Approximately(vals[i], other.vals[i]))
                    return false;
            }
            return true;
        }

        public bool Equal(TweenPlugPathPoint other, int length) {
            for(int i = 0; i < length; i++) {
                if(vals[i] != other.vals[i])
                    return false;
            }
            return true;
        }

        public static float Distance(TweenPlugPathPoint a, TweenPlugPathPoint b, int length) {
            //NOTE: assume b has the same length as a
            if(length == 1)
                return Mathf.Abs(a.vals[0] - b.vals[0]);

            float sqrLen = 0f;
            for(int i = 0; i < length; i++) {
                var delta = a.vals[i] - b.vals[i];
                sqrLen += delta*delta;
            }

            return Mathf.Sqrt(sqrLen);
        }
    }

    [System.Serializable]
    public struct TweenPlugPathControlPoint {
        public TweenPlugPathPoint a, b;

        public TweenPlugPathControlPoint(TweenPlugPathPoint a, TweenPlugPathPoint b) {
            this.a = a;
            this.b = b;
        }
    }

    public enum TweenPlugPathType {
        Linear,
        CatmullRom,
    }

    [System.Serializable]
    public class TweenPlugPath {
        static TweenPlugPathCatmullRomDecoder _catmullRomDecoder;
        static TweenPlugPathLinearDecoder _linearDecoder;

        public TweenPlugPathType type;
        public int subdivisionsXSegment; // Subdivisions x each segment
        public TweenPlugPathPoint[] wps; // Waypoints (modified by PathPlugin when setting relative end/change value or by CubicBezierDecoder) - also modified by DOTweenPathInspector
        public float[] wpsTimeTable; //normalized time per waypoint (exclude start=0f and end=1f)
        public TweenPlugPathControlPoint[] controlPoints; // Control points used by non-linear paths
        public float length; // Unit length of the path

        public float[] timesTable; // Connected to lengthsTable, used for constant speed calculations
        public float[] lengthsTable; // Connected to timesTable, used for constant speed calculations

        [SerializeField] bool _isConstantSpeed;

        public int linearWPIndex { get; set; } = -1; // Waypoint towards which we're moving (only stored for linear paths, when calling GetPoint)

        public int pathPointCount { get { return wps != null && wps.Length > 0 ? wps[0].vals.Length : 0; } }

        public bool isClosed { get { return wps.Length > 1 ? wps[wps.Length - 1].Equal(wps[0], wps[0].vals.Length) : false; } }

        public bool isConstantSpeed { get { return _isConstantSpeed; } }

        TweenPlugPath _incrementalClone; // Last incremental clone. Stored in case of incremental loops, to avoid recreating a new path every time
        int _incrementalIndex = 0;

        private ITweenPlugPathDecoder decoder {
            get {
                if(_decoder == null) {
                    switch(type) {
                        case TweenPlugPathType.Linear:
                            if(_linearDecoder == null) _linearDecoder = new TweenPlugPathLinearDecoder();
                            _decoder = _linearDecoder;
                            break;
                        default: // Catmull-Rom
                            if(_catmullRomDecoder == null) _catmullRomDecoder = new TweenPlugPathCatmullRomDecoder();
                            _decoder = _catmullRomDecoder;
                            break;
                    }
                }

                return _decoder;
            }
        }
        ITweenPlugPathDecoder _decoder;

        public TweenPlugPathPoint cachePoint {
            get {
                if(!_cachePoint.HasValue)
                    _cachePoint = new TweenPlugPathPoint(pathPointCount);
                return _cachePoint.Value;
            }
        }
        TweenPlugPathPoint? _cachePoint;

        public TweenPlugPath(TweenPlugPathType type, TweenPlugPathPoint[] waypoints, float[] waypointNormalTimes, bool isConstantSpeed, int subdivisionsXSegment) {
            this.type = type;
            this.subdivisionsXSegment = subdivisionsXSegment;
            this._isConstantSpeed = isConstantSpeed;

            wps = waypoints;
            wpsTimeTable = waypointNormalTimes;
        }

        public TweenPlugPath() { }

        //get time relative to waypoint time table
        private float GetWPPerc(float t) {
            if(t >= 1f) return t;

            int timeTableCount = wpsTimeTable.Length;
            float waypointLast = wps.Length - 1;
            float _t, time, prevTime;

            for(int i = 0; i < timeTableCount; i++) {
                time = wpsTimeTable[i];
                if(t <= time) {
                    prevTime = i > 0 ? wpsTimeTable[i-1] : 0f;

                    _t = (t - prevTime) / (time - prevTime); //grab percent between previous and current

                    return Mathf.Lerp(i, i + 1, _t) / waypointLast; //grab percent within the waypoint indices (offset by one index to take waypoint 0 (t=0f) into account)
                }
            }

            //compare at end=1f (last waypoint)
            time = 1f;
            prevTime = wpsTimeTable[timeTableCount - 1];

            _t = (t - prevTime) / (time - prevTime); //grab percent between previous and current

            return Mathf.Lerp(wpsTimeTable.Length, wpsTimeTable.Length + 1, _t) / waypointLast; //grab percent within the waypoint indices
        }

        /// <summary>
        /// Gets the point on the path at the given percentage (0 to 1). Returns value generated to its cache point, make sure to just copy these values.
        /// </summary>
        /// <param name="perc">The percentage (0 to 1) at which to get the point</param>
        public TweenPlugPathPoint GetPoint(float perc) {
            if(wpsTimeTable != null && wpsTimeTable.Length > 0)
                perc = GetWPPerc(perc); //get perc based on wp if available

            if(isConstantSpeed) perc = ConvertToConstantPathPerc(perc);

            decoder.GetPoint(cachePoint, perc, wps, this);

            return cachePoint;
        }

        // Converts the given raw percentage to the correct percentage considering constant speed
        private float ConvertToConstantPathPerc(float perc) {
            if(type == TweenPlugPathType.Linear) return perc;

            if(perc > 0 && perc < 1) {
                float tLen = length * perc;
                // Find point in time/length table
                float t0 = 0, l0 = 0, t1 = 0, l1 = 0;
                int count = lengthsTable.Length;
                for(int i = 0; i < count; ++i) {
                    if(lengthsTable[i] > tLen) {
                        t1 = timesTable[i];
                        l1 = lengthsTable[i];
                        if(i > 0) l0 = lengthsTable[i - 1];
                        break;
                    }
                    t0 = timesTable[i];
                }
                // Find correct time
                if(l1 == l0 || t1 == t0) //points are in the same space
                    perc = 0f;
                else
                    perc = t0 + ((tLen - l0) / (l1 - l0)) * (t1 - t0);
            }

            // Clamp value because path has limited range of 0-1
            if(perc > 1) perc = 1;
            else if(perc < 0) perc = 0;

            return perc;
        }

        // Clones this path with the given loop increment
        public TweenPlugPath CloneIncremental(int loopIncrement) {
            if(_incrementalClone != null) {
                if(_incrementalIndex == loopIncrement) return _incrementalClone;
            }

            var _incCachePt = new TweenPlugPathPoint(pathPointCount);

            int wpsLen = wps.Length;
            int ptCount = pathPointCount;

            //diff
            for(int i = 0; i < ptCount; i++)
                _incCachePt.vals[i] = wps[wpsLen - 1].vals[i] - wps[0].vals[i];

            var incrWps = new TweenPlugPathPoint[wps.Length];
            for(int i = 0; i < wpsLen; ++i) {
                incrWps[i] = new TweenPlugPathPoint(ptCount);
                for(int j = 0; j < ptCount; j++)
                    incrWps[i].vals[j] = wps[i].vals[j] + (_incCachePt.vals[j] * loopIncrement); //wps[i] + (diff * loopIncrement)
            }

            int cpsLen = controlPoints.Length;
            var incrCps = new TweenPlugPathControlPoint[cpsLen];
            for(int i = 0; i < cpsLen; ++i) {                
                var a = new TweenPlugPathPoint(ptCount);
                var b = new TweenPlugPathPoint(ptCount);

                //incrCps[i] = controlPoints[i] + (diff * loopIncrement);
                for(int j = 0; j < ptCount; j++) {
                    a.vals[j] = controlPoints[i].a.vals[j] + (_incCachePt.vals[j] * loopIncrement);
                    b.vals[j] = controlPoints[i].b.vals[j] + (_incCachePt.vals[j] * loopIncrement);
                }

                incrCps[i] = new TweenPlugPathControlPoint(a, b);
            }

            _incrementalClone = new TweenPlugPath();
            _incrementalIndex = loopIncrement;
            _incrementalClone.type = type;
            _incrementalClone.subdivisionsXSegment = subdivisionsXSegment;
            _incrementalClone.wps = incrWps;
            _incrementalClone.controlPoints = incrCps;
            _incrementalClone._cachePoint = _incCachePt;

            _incrementalClone.length = length;
            _incrementalClone.timesTable = timesTable;
            _incrementalClone.lengthsTable = lengthsTable;
            _incrementalClone._decoder = _decoder;

            return _incrementalClone;
        }

        public void Init() {
            decoder.FinalizePath(this, wps, isClosed);
        }
    }

    public interface ITweenPlugPathDecoder {
        void FinalizePath(TweenPlugPath p, TweenPlugPathPoint[] wps, bool isClosedPath);
        void GetPoint(TweenPlugPathPoint output, float perc, TweenPlugPathPoint[] wps, TweenPlugPath p);
    }

    public class TweenPlugPathLinearDecoder : ITweenPlugPathDecoder {
        public void FinalizePath(TweenPlugPath p, TweenPlugPathPoint[] wps, bool isClosedPath) {
            p.controlPoints = null;

            var subdivisions = (wps.Length) * p.subdivisionsXSegment; // Unused
            SetTimeToLengthTables(p, subdivisions);
        }

        public void GetPoint(TweenPlugPathPoint output, float perc, TweenPlugPathPoint[] wps, TweenPlugPath p) {
            var ptCount = p.pathPointCount;

            if(perc <= 0) {
                p.linearWPIndex = 1;
                output.Copy(wps[0], ptCount);
                return;
            }

            int startPIndex = 0;
            int endPIndex = 0;
            int count = p.timesTable.Length;
            for(int i = 1; i < count; i++) {
                if(p.timesTable[i] >= perc) {
                    startPIndex = i - 1;
                    endPIndex = i;
                    break;
                }
            }

            float startPPerc = p.timesTable[startPIndex];
            float partialPerc = perc - startPPerc;
            float partialLen = p.length * partialPerc;
            var wp0 = wps[startPIndex];
            var wp1 = wps[endPIndex];
            p.linearWPIndex = endPIndex;

            //output = wp0 + clamp(wp1 - wp0, partialLen)

            for(int i = 0; i < ptCount; i++)
                output.vals[i] = wp1.vals[i] - wp0.vals[i];

            output.ClampMagnitude(partialLen, ptCount);

            for(int i = 0; i < ptCount; i++)
                output.vals[i] += wp0.vals[i];
        }

        // Linear exception: also sets waypoints lengths and doesn't set lengthsTable since it's useless
        void SetTimeToLengthTables(TweenPlugPath p, int subdivisions) {
            float pathLen = 0;
            int wpsLen = p.wps.Length;
            var ptCount = p.pathPointCount;
            var prevP = p.wps[0];
            for(int i = 1; i < wpsLen; i++) {
                var currP = p.wps[i];
                float dist = TweenPlugPathPoint.Distance(currP, prevP, ptCount);
                pathLen += dist;
                prevP = currP;
            }

            // Assign
            p.length = pathLen;
            p.lengthsTable = null;
            p.timesTable = null;
        }
    }

    public class TweenPlugPathCatmullRomDecoder : ITweenPlugPathDecoder {
        // Used for temporary operations
        static readonly TweenPlugPathPoint _emptyPoint = new TweenPlugPathPoint(0);
        static readonly TweenPlugPathPoint _cachePoint1 = new TweenPlugPathPoint(4);
        static readonly TweenPlugPathPoint _cachePoint2 = new TweenPlugPathPoint(4);

        public void FinalizePath(TweenPlugPath p, TweenPlugPathPoint[] wps, bool isClosedPath) {
            // Add starting and ending control points (uses only one vector per control point)
            int wpsLen = wps.Length;
            if(p.controlPoints == null || p.controlPoints.Length != 2) p.controlPoints = new TweenPlugPathControlPoint[2];
            if(isClosedPath) {
                p.controlPoints[0] = new TweenPlugPathControlPoint(wps[wpsLen - 2], _emptyPoint);
                p.controlPoints[1] = new TweenPlugPathControlPoint(wps[1], _emptyPoint);
            }
            else {
                p.controlPoints[0] = new TweenPlugPathControlPoint(wps[1], _emptyPoint);

                var lastP = wps[wpsLen - 1];
                var lastP2 = wps[wpsLen - 2];

                var newPt = new TweenPlugPathPoint(lastP.vals.Length);

                for(int i = 0; i < newPt.vals.Length; i++) {
                    var lastPVal = lastP.vals[i];
                    var diffVVal = lastPVal - lastP2.vals[i];

                    newPt.vals[i] = lastPVal + diffVVal;
                }

                p.controlPoints[1] = new TweenPlugPathControlPoint(newPt, _emptyPoint);
            }

            //            p.subdivisions = (wpsLen + 2) * p.subdivisionsXSegment;
            var subdivisions = wpsLen * p.subdivisionsXSegment;
            if(p.isConstantSpeed) {
                // Store time to len tables
                SetTimeToLengthTables(p, subdivisions);
            }
            else {
                //only generate path length
                SetPathLength(p, subdivisions);
            }
        }

        public void GetPoint(TweenPlugPathPoint output, float perc, TweenPlugPathPoint[] wps, TweenPlugPath p) {
            int numSections = wps.Length - 1; // Considering also control points
            int tSec = (int)Mathf.Floor(perc * numSections);
            int currPt = numSections - 1;
            if(currPt > tSec) currPt = tSec;
            float u = perc * numSections - currPt;

            var aPt = currPt == 0 ? p.controlPoints[0].a : wps[currPt - 1];
            var bPt = wps[currPt];
            var cPt = wps[currPt + 1];
            var dPt = currPt + 2 > wps.Length - 1 ? p.controlPoints[1].a : wps[currPt + 2];

            var ptCount = p.pathPointCount;
            for(int i = 0; i < ptCount; i++) {
                float a = aPt.vals[i], b = bPt.vals[i], c = cPt.vals[i], d = dPt.vals[i];

                output.vals[i] = .5f * (
                  (-a + 3f * b - 3f * c + d) * (u * u * u)
                + (2f * a - 5f * b + 4f * c - d) * (u * u)
                + (-a + c) * u
                + 2f * b);
            }
        }

        void SetTimeToLengthTables(TweenPlugPath p, int subdivisions) {
            TweenPlugPathPoint prevP = _cachePoint1, currP = _cachePoint2;

            var ptCount = p.pathPointCount;

            float pathLen = 0;
            float incr = 1f / subdivisions;
            float[] timesTable = new float[subdivisions];
            float[] lengthsTable = new float[subdivisions];
            GetPoint(prevP, 0, p.wps, p);
            for(int i = 1; i < subdivisions + 1; ++i) {
                float perc = incr * i;
                GetPoint(currP, perc, p.wps, p);
                pathLen += TweenPlugPathPoint.Distance(currP, prevP, ptCount);
                prevP.Copy(currP, ptCount);
                timesTable[i - 1] = perc;
                lengthsTable[i - 1] = pathLen;
            }

            // Assign
            p.length = pathLen;
            p.timesTable = timesTable;
            p.lengthsTable = lengthsTable;
        }

        void SetPathLength(TweenPlugPath p, int subdivisions) {
            TweenPlugPathPoint prevP = _cachePoint1, currP = _cachePoint2;

            var ptCount = p.pathPointCount;

            float pathLen = 0;
            float incr = 1f / subdivisions;
            GetPoint(prevP, 0, p.wps, p);
            for(int i = 1; i < subdivisions + 1; ++i) {
                float perc = incr * i;
                GetPoint(currP, perc, p.wps, p);
                pathLen += TweenPlugPathPoint.Distance(currP, prevP, ptCount);
                prevP.Copy(currP, ptCount);
            }

            // Assign
            p.length = pathLen;
            p.timesTable = null;
            p.lengthsTable = null;
        }
    }
    //TweenPlugPathOptions
    public abstract class TweenPlugPathBase<T, TOptions> : ABSTweenPlugin<T, TweenPlugPath, TOptions> where TOptions : struct, IPlugOptions {
        public const float MinLookAhead = 0.0001f;

        protected abstract bool ApproximatelyEqual(TweenPlugPathPoint pt, T curVal);
        protected abstract bool Equal(TweenPlugPathPoint pt, T curVal);
        protected abstract TweenPlugPathPoint CreatePoint(T src);
        protected abstract T GetValue(TweenPlugPathPoint pt);

        public override void Reset(TweenerCore<T, TweenPlugPath, TOptions> t) {
            t.startValue = t.endValue = t.changeValue = null;
        }

        public override void SetFrom(TweenerCore<T, TweenPlugPath, TOptions> t, bool isRelative) { }
        public override void SetFrom(TweenerCore<T, TweenPlugPath, TOptions> t, TweenPlugPath fromValue, bool setImmediately, bool isRelative) { }

        public override void SetRelativeEndValue(TweenerCore<T, TweenPlugPath, TOptions> t) { }

        public override TweenPlugPath ConvertToStartValue(TweenerCore<T, TweenPlugPath, TOptions> t, T value) {
            // Simply sets the same path as start and endValue
            return t.endValue;
        }

        // Simply setup change value, paths will always be finalized via serialization from animator editor
        public override void SetChangeValue(TweenerCore<T, TweenPlugPath, TOptions> t) {
            // Set changeValue as a reference to endValue
            t.changeValue = t.endValue;
        }

        public override float GetSpeedBasedDuration(TOptions options, float unitsXSecond, TweenPlugPath changeValue) {
            return changeValue.length / unitsXSecond;
        }

        public override void EvaluateAndApply(TOptions options, Tween t, bool isRelative, DOGetter<T> getter, DOSetter<T> setter, float elapsed, TweenPlugPath startValue, TweenPlugPath changeValue, float duration, bool usingInversePosition, int newCompletedSteps, UpdateNotice updateNotice) {            
            //TODO: figure out how to access tween's loopType
            /*if(t.loopType == LoopType.Incremental && !changeValue.isClosed) {
                int increment = (t.IsComplete() ? t.CompletedLoops() - 1 : t.CompletedLoops());
                if(increment > 0) changeValue = changeValue.CloneIncremental(increment);
            }*/

            float pathPerc = EaseManager.Evaluate(t, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod);

            var pt = changeValue.GetPoint(pathPerc);
            var val = GetValue(pt);
            setter(val);
        }
    }

    public class TweenPlugPathFloat : TweenPlugPathBase<float, TWeenPlugNoneOptions> {
        protected override bool ApproximatelyEqual(TweenPlugPathPoint pt, float curVal) { return pt.Approximately(curVal); }
        protected override bool Equal(TweenPlugPathPoint pt, float curVal) { return pt.valueFloat == curVal; }
        protected override TweenPlugPathPoint CreatePoint(float src) { return new TweenPlugPathPoint(src); }
        protected override float GetValue(TweenPlugPathPoint pt) { return pt.valueFloat; }

        public static TweenPlugPathFloat Get() {
            if(mInstance == null) mInstance = new TweenPlugPathFloat();
            return mInstance;
        }
        private static TweenPlugPathFloat mInstance;
    }

    public class TweenPlugPathVector2 : TweenPlugPathBase<Vector2, TWeenPlugNoneOptions> {
        protected override bool ApproximatelyEqual(TweenPlugPathPoint pt, Vector2 curVal) { return pt.Approximately(curVal); }
        protected override bool Equal(TweenPlugPathPoint pt, Vector2 curVal) { return pt.valueVector2 == curVal; }
        protected override TweenPlugPathPoint CreatePoint(Vector2 src) { return new TweenPlugPathPoint(src); }
        protected override Vector2 GetValue(TweenPlugPathPoint pt) { return pt.valueVector2; }

        public static TweenPlugPathVector2 Get() {
            if(mInstance == null) mInstance = new TweenPlugPathVector2();
            return mInstance;
        }
        private static TweenPlugPathVector2 mInstance;
    }

    public class TweenPlugPathVector3 : TweenPlugPathBase<Vector3, TWeenPlugNoneOptions> {
        protected override bool ApproximatelyEqual(TweenPlugPathPoint pt, Vector3 curVal) { return pt.Approximately(curVal); }
        protected override bool Equal(TweenPlugPathPoint pt, Vector3 curVal) { return pt.valueVector3 == curVal; }
        protected override TweenPlugPathPoint CreatePoint(Vector3 src) { return new TweenPlugPathPoint(src); }
        protected override Vector3 GetValue(TweenPlugPathPoint pt) { return pt.valueVector3; }

        public static TweenPlugPathVector3 Get() {
            if(mInstance == null) mInstance = new TweenPlugPathVector3();
            return mInstance;
        }
        private static TweenPlugPathVector3 mInstance;
    }

    public class TweenPlugPathVector4 : TweenPlugPathBase<Vector4, TWeenPlugNoneOptions> {
        protected override bool ApproximatelyEqual(TweenPlugPathPoint pt, Vector4 curVal) { return pt.Approximately(curVal); }
        protected override bool Equal(TweenPlugPathPoint pt, Vector4 curVal) { return pt.valueVector4 == curVal; }
        protected override TweenPlugPathPoint CreatePoint(Vector4 src) { return new TweenPlugPathPoint(src); }
        protected override Vector4 GetValue(TweenPlugPathPoint pt) { return pt.valueVector4; }

        public static TweenPlugPathVector4 Get() {
            if(mInstance == null) mInstance = new TweenPlugPathVector4();
            return mInstance;
        }
        private static TweenPlugPathVector4 mInstance;
    }

    public class TweenPlugPathColor : TweenPlugPathBase<Color, TWeenPlugNoneOptions> {
        protected override bool ApproximatelyEqual(TweenPlugPathPoint pt, Color curVal) { return pt.Approximately(curVal); }
        protected override bool Equal(TweenPlugPathPoint pt, Color curVal) { return pt.valueColor == curVal; }
        protected override TweenPlugPathPoint CreatePoint(Color src) { return new TweenPlugPathPoint(src); }
        protected override Color GetValue(TweenPlugPathPoint pt) { return pt.valueVector4; }

        public static TweenPlugPathColor Get() {
            if(mInstance == null) mInstance = new TweenPlugPathColor();
            return mInstance;
        }
        private static TweenPlugPathColor mInstance;
    }

    public class TweenPlugPathRect : TweenPlugPathBase<Rect, TWeenPlugNoneOptions> {
        protected override bool ApproximatelyEqual(TweenPlugPathPoint pt, Rect curVal) { return pt.Approximately(curVal); }
        protected override bool Equal(TweenPlugPathPoint pt, Rect curVal) { return pt.valueRect == curVal; }
        protected override TweenPlugPathPoint CreatePoint(Rect src) { return new TweenPlugPathPoint(src); }
        protected override Rect GetValue(TweenPlugPathPoint pt) { return pt.valueRect; }

        public static TweenPlugPathRect Get() {
            if(mInstance == null) mInstance = new TweenPlugPathRect();
            return mInstance;
        }
        private static TweenPlugPathRect mInstance;
    }

    public class TweenPlugPathInt : TweenPlugPathBase<int, TWeenPlugNoneOptions> {
        protected override bool ApproximatelyEqual(TweenPlugPathPoint pt, int curVal) { return pt.Approximately(curVal); }
        protected override bool Equal(TweenPlugPathPoint pt, int curVal) { return pt.valueFloat == curVal; }
        protected override TweenPlugPathPoint CreatePoint(int src) { return new TweenPlugPathPoint(src); }
        protected override int GetValue(TweenPlugPathPoint pt) { return Mathf.RoundToInt(pt.valueFloat); }

        public static TweenPlugPathInt Get() {
            if(mInstance == null) mInstance = new TweenPlugPathInt();
            return mInstance;
        }
        private static TweenPlugPathInt mInstance;
    }

    public class TweenPlugPathLong : TweenPlugPathBase<long, TWeenPlugNoneOptions> {
        protected override bool ApproximatelyEqual(TweenPlugPathPoint pt, long curVal) { return pt.Approximately(curVal); }
        protected override bool Equal(TweenPlugPathPoint pt, long curVal) { return pt.valueFloat == curVal; }
        protected override TweenPlugPathPoint CreatePoint(long src) { return new TweenPlugPathPoint(src); }
        protected override long GetValue(TweenPlugPathPoint pt) { return Mathf.RoundToInt(pt.valueFloat); }

        public static TweenPlugPathLong Get() {
            if(mInstance == null) mInstance = new TweenPlugPathLong();
            return mInstance;
        }
        private static TweenPlugPathLong mInstance;
    }

    public class TweenPlugPathDouble : TweenPlugPathBase<double, TWeenPlugNoneOptions> {
        protected override bool ApproximatelyEqual(TweenPlugPathPoint pt, double curVal) { return pt.Approximately((float)curVal); }
        protected override bool Equal(TweenPlugPathPoint pt, double curVal) { return pt.valueFloat == curVal; }
        protected override TweenPlugPathPoint CreatePoint(double src) { return new TweenPlugPathPoint((float)src); }
        protected override double GetValue(TweenPlugPathPoint pt) { return pt.valueFloat; }

        public static TweenPlugPathDouble Get() {
            if(mInstance == null) mInstance = new TweenPlugPathDouble();
            return mInstance;
        }
        private static TweenPlugPathDouble mInstance;
    }

    public class TweenPlugPathEuler : TweenPlugPathBase<Quaternion, TWeenPlugNoneOptions> {
        protected override bool ApproximatelyEqual(TweenPlugPathPoint pt, Quaternion curVal) { return pt.Approximately(curVal.eulerAngles); }
        protected override bool Equal(TweenPlugPathPoint pt, Quaternion curVal) { return pt.valueVector3 == curVal.eulerAngles; }
        protected override TweenPlugPathPoint CreatePoint(Quaternion src) { return new TweenPlugPathPoint(src.eulerAngles); }
        protected override Quaternion GetValue(TweenPlugPathPoint pt) { return Quaternion.Euler(pt.valueVector3); }

        public static TweenPlugPathEuler Get() {
            if(mInstance == null) mInstance = new TweenPlugPathEuler();
            return mInstance;
        }
        private static TweenPlugPathEuler mInstance;
    }
}
