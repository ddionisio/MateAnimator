using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using DG.Tweening;

namespace M8.Animator {
    [ExecuteInEditMode]
    [AddComponentMenu("M8/Animate")]
    public class Animate : MonoBehaviour, ITarget, ISerializationCallbackReceiver {
        public enum DisableAction {
            None,
            Pause,
            Stop,
            Reset //this guarantees that all tracks are set back to the first frame
        }

        public delegate void OnTake(Animate anim, Take take);

        // show

        [SerializeField]
        List<Take> takeData = new List<Take>();

        [SerializeField]
        string _defaultTakeName = "";

        [SerializeField]
        AnimateMeta _meta; //

        public bool sequenceLoadAll = true;
        public bool sequenceKillWhenDone = false;

        public bool playOnEnable = false;

        public bool isGlobal = false;

        public DisableAction onDisableAction = DisableAction.Pause;

        public UpdateType updateType = UpdateType.Normal;
        public bool updateTimeIndependent = false;
        // hide

        [SerializeField]
        SerializeData serializeData;
        
        public event OnTake takeCompleteCallback;

        public string defaultTakeName {
            get { return _defaultTakeName; }
            set {
                _defaultTakeName = value;
                mDefaultTakeInd = -1;
            }
        }

        public int defaultTakeIndex {
            get {
                if(mDefaultTakeInd == -1 && !string.IsNullOrEmpty(_defaultTakeName)) {
                    var _t = _takes;
                    for(int i = 0; i < _t.Count; i++) {
                        if(_t[i].name == _defaultTakeName) {
                            mDefaultTakeInd = i;
                            break;
                        }
                    }
                }

                return mDefaultTakeInd;
            }

            set {
                if(mDefaultTakeInd != value) {
                    mDefaultTakeInd = value;

                    //refresh _defaultTakeName
                    var _t = _takes;
                    if(mDefaultTakeInd >= 0 && mDefaultTakeInd < _t.Count) {
                        _defaultTakeName = _t[mDefaultTakeInd].name;
                    }
                    else { //invalid
                        mDefaultTakeInd = -1;
                        _defaultTakeName = "";
                    }
                }
            }
        }

        public bool isPlaying {
            get {
                Sequence seq = currentPlayingSequence;
                return seq != null && seq.IsPlaying();
            }
        }

        public bool isPaused {
            get {
                Sequence seq = currentPlayingSequence;
                return seq == null || !seq.IsPlaying();
            }
        }

        public bool isReversed {
            set {
                Sequence seq = currentPlayingSequence;
                if(seq != null) {
                    if(value) {
                        if(!seq.IsBackwards())
                            seq.Flip();
                    }
                    else {
                        if(seq.IsBackwards())
                            seq.Flip();
                    }
                }
            }

            get {
                Sequence seq = currentPlayingSequence;
                return seq != null && seq.IsBackwards();
            }
        }

        public float runningFrame {
            get {
                Sequence seq = currentPlayingSequence;
                if(seq == null)
                    return 0f;
                Take take = currentPlayingTake;
                if(take == null)
                    return 0f;

                return seq.Elapsed(false) * take.frameRate;
            }
        }

        public float runningFullFrame {
            get {
                Sequence seq = currentPlayingSequence;
                if(seq == null)
                    return 0f;
                Take take = currentPlayingTake;
                if(take == null)
                    return 0f;

                return seq.Elapsed(true) * take.frameRate;
            }
        }

        public float runningTime {
            get {
                Sequence seq = currentPlayingSequence;
                return seq != null ? seq.Elapsed() : 0.0f;
            }
        }
        public float runningFullTime {
            get {
                Sequence seq = currentPlayingSequence;
                return seq != null ? seq.Elapsed(true) : 0.0f;
            }
        }
        public float runningTotalTime {
            get {
                return currentPlayingTakeIndex != -1 ? GetTakeTotalTime(currentPlayingTakeIndex) : 0f;
            }
        }

        public int takeCount {
            get { return _takes.Count; }
        }

        public bool isSerializing { get; private set; }

        private SequenceControl[] mSequenceCtrls;

        private int mNowPlayingTakeIndex = -1;
        private int mLastPlayingTakeIndex = -1;

        //private bool isLooping = false;
        //private float takeTime = 0f;
        private bool mStarted = false;

        private float mAnimScale = 1.0f; //NOTE: this is reset during disable

        private Dictionary<string, Transform> mCache;

        private int mDefaultTakeInd = -1;

        private SequenceControl[] sequenceCtrls {
            get {
                if(mSequenceCtrls == null) {
                    List<Take> _t = _takes;
                    int count = _t.Count;

                    mSequenceCtrls = new SequenceControl[count];
                    for(int i = 0; i < count; i++)
                        mSequenceCtrls[i] = new SequenceControl(this, i, _t[i]);
                }

                return mSequenceCtrls;
            }
        }

        private Take currentPlayingTake { get { return mNowPlayingTakeIndex == -1 ? null : sequenceCtrls[mNowPlayingTakeIndex].take; } }

        public string currentPlayingTakeName { get { return mNowPlayingTakeIndex == -1 ? "" : currentPlayingTake.name; } }
        public int currentPlayingTakeIndex { get { return mNowPlayingTakeIndex; } }
        public Sequence currentPlayingSequence { get { return mNowPlayingTakeIndex == -1 ? null : sequenceCtrls[mNowPlayingTakeIndex].sequence; } }

        public int lastPlayingTakeIndex { get { return mLastPlayingTakeIndex; } }
        public string lastPlayingTakeName { get { return mLastPlayingTakeIndex == -1 ? "" : _takes[mLastPlayingTakeIndex].name; } }

        public float animScale {
            get { return mAnimScale; }
            set {
                if(mAnimScale != value) {
                    mAnimScale = value;

                    SequenceControl amSeq;
                    if(mNowPlayingTakeIndex != -1 && (amSeq = sequenceCtrls[mNowPlayingTakeIndex]) != null) {
                        if(amSeq.sequence != null)
                            amSeq.sequence.timeScale = mAnimScale;
                        if(amSeq.take != null)
                            amSeq.take.SetAnimScale(this, mAnimScale);
                    }
                }
            }
        }

        List<Take> _takes {
            get { return _meta ? _meta.takes : takeData; }
        }

        public bool TakeExists(string takeName) {
            return GetTakeIndex(takeName) != -1;
        }

        public int GetTakeIndex(string takeName) {
            List<Take> _t = _takes;
            for(int i = 0; i < _t.Count; i++) {
                if(_t[i].name == takeName)
                    return i;
            }
            return -1;
        }

        public string GetTakeName(int takeIndex) {
            return _takes[takeIndex].name;
        }

        public float GetTakeTotalTime(string takeName) {
            int ind = GetTakeIndex(takeName);
            if(ind == -1) { Debug.LogError("Take not found: " + takeName); return 0f; }

            return GetTakeTotalTime(ind);
        }

        public float GetTakeTotalTime(int takeIndex) {
            var take = _takes[takeIndex];
            return (take.totalFrames + take.endFramePadding) / (float)take.frameRate;
        }

        public void PlayDefault(bool loop = false) {
            int takeInd = defaultTakeIndex;
            if(takeInd != -1) {
                Play(defaultTakeIndex, loop);
            }
        }

        public void Play(string takeName) {
            int ind = GetTakeIndex(takeName);
            if(ind == -1) { Debug.LogError("Take not found: " + takeName); return; }
            Play(ind, false);
        }

        // play take by name
        public void Play(string takeName, bool loop) {
            int ind = GetTakeIndex(takeName);
            if(ind == -1) { Debug.LogError("Take not found: " + takeName); return; }
            Play(ind, loop);
        }

        public void Play(int takeIndex) {
            PlayAtTime(takeIndex, 0f, false);
        }

        public void Play(int takeIndex, bool loop) {
            PlayAtTime(takeIndex, 0f, loop);
        }

        public void PlayAtFrame(string takeName, float frame, bool loop = false) {
            int ind = GetTakeIndex(takeName);
            if(ind == -1) { Debug.LogError("Take not found: " + takeName); return; }
            PlayAtTime(ind, frame / sequenceCtrls[ind].take.frameRate, loop);
        }

        public void PlayAtFrame(int takeIndex, float frame, bool loop = false) {
            PlayAtTime(takeIndex, frame / sequenceCtrls[takeIndex].take.frameRate, loop);
        }

        public void PlayAtTime(string takeName, float time, bool loop = false) {
            int ind = GetTakeIndex(takeName);
            if(ind == -1) { Debug.LogError("Take not found: " + takeName); return; }
            PlayAtTime(ind, time, loop);
        }

        public void PlayAtTime(int index, float time, bool loop = false) {
            if(mNowPlayingTakeIndex == index)
                return;

            mLastPlayingTakeIndex = mNowPlayingTakeIndex;

            Pause();

            if(mLastPlayingTakeIndex != -1)
                sequenceCtrls[mLastPlayingTakeIndex].take.PlaySwitch(this); //notify take that we are switching

            SequenceControl amSeq = sequenceCtrls[index];

            Take newPlayTake = amSeq.take;

            Sequence seq = amSeq.sequence;

            if(seq == null) {
                amSeq.Build(sequenceKillWhenDone, updateType, updateTimeIndependent);
                seq = amSeq.sequence;
            }

            mNowPlayingTakeIndex = index;

            newPlayTake.PlayStart(this, newPlayTake.frameRate * time, 1.0f); //notify take that we are playing

            if(seq != null) {
                /*if(loop) {
	                seq.loops = -1;
	            }
	            else {
	                seq.loops = newPlayTake.numLoop;
	            }*/

                seq.timeScale = mAnimScale;
                seq.Goto(time, true);
            }
        }

        /// <summary>
        /// Go to a given time without playing.
        /// </summary>
        public void Goto(string takeName, float time) {
            int ind = GetTakeIndex(takeName);
            if(ind == -1) { Debug.LogError("Take not found: " + takeName); return; }

            Goto(ind, time);
        }

        /// <summary>
        /// Go to a given time without playing.
        /// </summary>
        public void Goto(int takeIndex, float time) {
            Sequence seq;

            if(mNowPlayingTakeIndex != takeIndex) {
                mLastPlayingTakeIndex = mNowPlayingTakeIndex;

                Pause();

                if(mLastPlayingTakeIndex != -1)
                    sequenceCtrls[mLastPlayingTakeIndex].take.PlaySwitch(this); //notify take that we are switching

                SequenceControl amSeq = sequenceCtrls[takeIndex];

                Take newPlayTake = amSeq.take;

                seq = amSeq.sequence;

                if(seq == null) {
                    amSeq.Build(sequenceKillWhenDone, updateType, updateTimeIndependent);
                    seq = amSeq.sequence;
                }

                mNowPlayingTakeIndex = takeIndex;

                newPlayTake.PlayStart(this, newPlayTake.frameRate * time, 1.0f); //notify take that we are playing
            }
            else
                seq = sequenceCtrls[takeIndex].sequence;

            seq.Goto(time);
        }

        /// <summary>
        /// Call this to move current take to given time. Ideally, call Goto(take, time) the first time. This is ideally used to manually move the sequence.
        /// </summary>
        public void Goto(float time) {
            if(mNowPlayingTakeIndex != -1) {
                var seq = sequenceCtrls[mNowPlayingTakeIndex].sequence;
                seq.Goto(time);
            }
            else {
                Debug.LogWarning("No take playing...");
            }
        }

        public IEnumerator PlayWait(string take) {
            WaitForEndOfFrame wait = new WaitForEndOfFrame();
            Play(take);
            while(isPlaying)
                yield return wait;
        }

        public IEnumerator PlayWait(int take) {
            WaitForEndOfFrame wait = new WaitForEndOfFrame();
            Play(take);
            while(isPlaying)
                yield return wait;
        }

        public void ResetTake(int take) {
            sequenceCtrls[take].take.Reset(this);
        }

        public void ResetTake(string takeName) {
            int ind = GetTakeIndex(takeName);
            if(ind == -1) { Debug.LogError("Take not found: " + takeName); return; }
            ResetTake(ind);
        }

        public void Pause() {
            Take take = currentPlayingTake;
            if(take == null) return;
            take.Pause(this);

            Sequence seq = currentPlayingSequence;
            if(seq != null)
                seq.Pause();
        }

        public void Resume() {
            Take take = currentPlayingTake;
            if(take == null) return;
            take.Resume(this);

            Sequence seq = currentPlayingSequence;
            if(seq != null)
                seq.Play();
        }

        public void Stop() {
            Take take = currentPlayingTake;
            if(take == null) return;
            take.Stop(this);

            //end camera fade
            if(CameraFade.hasInstance()) {
                CameraFade cf = CameraFade.getCameraFade();
                cf.playParam = null;
            }

            sequenceCtrls[mNowPlayingTakeIndex].Reset();

            mLastPlayingTakeIndex = mNowPlayingTakeIndex;
            mNowPlayingTakeIndex = -1;
        }

        /// <summary>
        /// Go to a given frame without playing.
        /// </summary>
        public void GotoFrame(string takeName, float frame) {
            int ind = GetTakeIndex(takeName);
            if(ind == -1) { Debug.LogError("Take not found: " + takeName); return; }

            float t = frame / _takes[ind].frameRate;
            Goto(ind, t);
        }

        /// <summary>
        /// Go to a given frame without playing.
        /// </summary>
        public void GotoFrame(int takeIndex, float frame) {
            float t = frame / _takes[takeIndex].frameRate;
            Goto(takeIndex, t);
        }

        /// <summary>
        /// Call this to move current take to given frame. Ideally, call GotoFrame(take, frame) the first time. This is ideally used to manually move the sequence.
        /// </summary>
        public void GotoFrame(float frame) {            
            if(mNowPlayingTakeIndex != -1) {
                Take take = currentPlayingTake;
                Sequence seq = currentPlayingSequence;

                float t = frame / take.frameRate;
                seq.Goto(t);
            }
            else {
                Debug.LogWarning("No take playing...");
            }
        }

        public void Reverse() {
            Sequence seq = currentPlayingSequence;
            if(seq != null)
                seq.Flip();
        }

        // preview a single frame (used for scrubbing)
        public void PreviewFrame(string takeName, float frame) {
            PreviewFrame(GetTakeIndex(takeName), frame);
        }

        public void PreviewFrame(int takeIndex, float frame) {
            Take curTake = sequenceCtrls[takeIndex].take;
            curTake.previewFrame(this, frame);
        }

        // preview a single time (used for scrubbing)
        public void PreviewTime(string takeName, float time) {
            PreviewTime(GetTakeIndex(takeName), time);
        }

        public void PreviewTime(int takeIndex, float time) {
            Take curTake = sequenceCtrls[takeIndex].take;
            curTake.previewFrame(this, time * curTake.frameRate);
        }

        void OnDestroy() {
            if(mSequenceCtrls != null) {
                for(int i = 0; i < mSequenceCtrls.Length; i++)
                    mSequenceCtrls[i].Destroy();
            }

            takeCompleteCallback = null;
        }

        void OnEnable() {
            if(mStarted) {
                if(playOnEnable) {
                    if(mNowPlayingTakeIndex == -1 && defaultTakeIndex != -1)
                        Play(defaultTakeIndex, false);
                    else
                        Resume();
                }
                //else if(playOnStart) {
                //Play(playOnStart.name, true, 0f, false);
                //}
            }
        }

        void OnDisable() {
            switch(onDisableAction) {
                case DisableAction.Pause:
                    Pause();
                    break;
                case DisableAction.Stop:
                    Stop();
                    break;
                case DisableAction.Reset:
                    if(mNowPlayingTakeIndex != -1)
                        Stop();
                    if(mLastPlayingTakeIndex != -1)
                        ResetTake(mLastPlayingTakeIndex);
                    break;
            }

            mAnimScale = 1.0f;
        }

        void Start() {
            if(!Application.isPlaying)
                return;

            mStarted = true;
            if(sequenceLoadAll && sequenceCtrls != null) {
                for(int i = 0; i < sequenceCtrls.Length; i++) {
                    if(sequenceCtrls[i].sequence == null)
                        sequenceCtrls[i].Build(sequenceKillWhenDone, updateType, updateTimeIndependent);
                }
            }

            if(!isPlaying && defaultTakeIndex != -1) {
                Play(defaultTakeIndex, false);
            }
        }

        #region ITarget interface
        Transform ITarget.root {
            get { return transform; }
        }

        AnimateMeta ITarget.meta {
            get { return _meta; }
            set {
                if(_meta != value) {
                    _meta = value;

                    if(_meta)
                        takeData.Clear();

                    mDefaultTakeInd = -1;
                }
            }
        }

        List<Take> ITarget.takes {
            get { return _takes; }
        }

        Transform ITarget.GetCache(string path) {
            if(mCache == null) return null;

            Transform ret = null;
            mCache.TryGetValue(path, out ret);
            return ret;
        }

        void ITarget.SetCache(string path, Transform obj) {
            if(mCache == null) mCache = new Dictionary<string, Transform>();
            if(mCache.ContainsKey(path))
                mCache[path] = obj;
            else {
                mCache.Add(path, obj);

                if(obj == null) //if object is null, report it as missing
                    Debug.LogWarning(name + " is missing Target: " + path);
            }
        }

        void ITarget.SequenceComplete(SequenceControl seq) {
            //end camera fade
            if(CameraFade.hasInstance()) {
                var cf = CameraFade.getCameraFade();
                cf.playParam = null;
            }

            mLastPlayingTakeIndex = mNowPlayingTakeIndex;
            mNowPlayingTakeIndex = -1;

            if(takeCompleteCallback != null)
                takeCompleteCallback(this, seq.take);
        }

        string[] ITarget.GetMissingTargets() {
            //check cache
            if(mCache != null) {
                List<string> missings = new List<string>();
                foreach(var pair in mCache) {
                    if(pair.Value == null)
                        missings.Add(pair.Key);
                }
                return missings.ToArray();
            }

            return null;
        }

        void ITarget.MaintainTargetCache(Track track) {
            if(_meta && mCache.ContainsKey(track.targetPath)) {
                UnityEngine.Object obj = track.GetTarget(this);
                if(obj) {
                    string objPath = Utility.GetPath(transform, obj);
                    if(objPath != track.targetPath) {
                        mCache.Remove(track.targetPath);
                    }
                }
            }
        }

        void ITarget.MaintainTakes() {
            foreach(var take in _takes) {
                take.maintainTake(this);
            }

            if(mCache != null)
                mCache.Clear();
        }

        /// <summary>
        /// attempt to generate the missing targets
        /// </summary>
        void ITarget.GenerateMissingTargets(string[] missingPaths) {
            if(missingPaths != null && missingPaths.Length > 0) {
                for(int i = 0; i < missingPaths.Length; i++)
                    Utility.CreateTarget(transform, missingPaths[i]);

                //fill necessary components per track and key
                foreach(var take in _takes) {
                    foreach(var track in take.trackValues) {
                        Transform t = Utility.GetTarget(transform, track.targetPath);
                        if(!t)
                            continue;

                        if(!track.CheckComponent(t.gameObject))
                            track.AddComponent(t.gameObject);
                    }
                }

                if(mCache != null)
                    mCache.Clear();
            }
        }
        #endregion

        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            isSerializing = true;

            serializeData = new SerializeData();

            if(_meta == null)
                serializeData.Serialize(takeData);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            if(_meta == null)
                serializeData.Deserialize(takeData);

            isSerializing = false;
        }
    }
}