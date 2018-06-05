using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using DG.Tweening;

namespace M8.Animator {
    [ExecuteInEditMode]
    [AddComponentMenu("M8/Animator")]
    public class Animate : MonoBehaviour, ITarget, ISerializationCallbackReceiver {
        public enum DisableAction {
            None,
            Pause,
            Stop,
            Reset //this guarantees that all tracks are set back to the first frame
        }

        public delegate void OnTake(Animate anim, Take take);
        public delegate void OnTakeTrigger(Animate anim, Take take, Key key, TriggerParam data);

        // show

        [SerializeField]
        List<Take> takeData;

        [SerializeField]
        int playOnStartIndex = -1;

        [SerializeField]
        AnimateMeta _meta; //
        [SerializeField]
        string playOnStartMeta; //used for playing a take from AnimatorMeta

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
        public event OnTakeTrigger takeTriggerCallback;

        public string defaultTakeName {
            get {
                if(_meta)
                    return playOnStartMeta;
                else
                    return playOnStartIndex == -1 ? "" : takeData[playOnStartIndex].name;
            }
            set {
                if(_meta) {
                    playOnStartMeta = value;
                    playOnStartIndex = -1;
                }
                else {
                    playOnStartMeta = "";
                    playOnStartIndex = -1;
                    if(!string.IsNullOrEmpty(value)) {
                        List<Take> _ts = _takes;
                        for(int i = 0; i < _ts.Count; i++) {
                            if(_ts[i].name == value) {
                                playOnStartIndex = i;
                                break;
                            }
                        }
                    }
                    //
                }
            }
        }

        public int defaultTakeIndex { get { return playOnStartIndex; } }

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

        public float runningTime {
            get {
                Sequence seq = currentPlayingSequence;
                return seq != null ? seq.Elapsed() : 0.0f;
            }
        }
        public float totalTime {
            get {
                Take take = mCurrentPlayingTake;
                if(take == null) return 0f;
                else return (take.totalFrames + take.endFramePadding) / (float)take.frameRate;
            }
        }

        [HideInInspector]
        public int codeLanguage = 0;    // 0 = C#, 1 = Javascript
                
        private SequenceControl[] mSequences;

        private int mNowPlayingTakeIndex = -1;
        private int mLastPlayingTakeIndex = -1;

        //private bool isLooping = false;
        //private float takeTime = 0f;
        private bool mStarted = false;

        private float mAnimScale = 1.0f; //NOTE: this is reset during disable

        private Dictionary<string, Transform> mCache;

        private Take mCurrentPlayingTake { get { return mNowPlayingTakeIndex == -1 ? null : mSequences[mNowPlayingTakeIndex].take; } }

        public string currentPlayingTakeName { get { return mNowPlayingTakeIndex == -1 ? "" : mCurrentPlayingTake.name; } }
        public int currentPlayingTakeIndex { get { return mNowPlayingTakeIndex; } }
        public Sequence currentPlayingSequence { get { return mNowPlayingTakeIndex == -1 ? null : mSequences[mNowPlayingTakeIndex].sequence; } }

        public int lastPlayingTakeIndex { get { return mLastPlayingTakeIndex; } }
        public string lastPlayingTakeName { get { return mLastPlayingTakeIndex == -1 ? "" : _takes[mLastPlayingTakeIndex].name; } }

        public float animScale {
            get { return mAnimScale; }
            set {
                if(mAnimScale != value) {
                    mAnimScale = value;

                    SequenceControl amSeq;
                    if(mNowPlayingTakeIndex != -1 && (amSeq = mSequences[mNowPlayingTakeIndex]) != null) {
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

        public void PlayDefault(bool loop = false) {
            if(!string.IsNullOrEmpty(defaultTakeName)) {
                Play(defaultTakeName, loop);
            }
        }

        // play take by name
        public void Play(string takeName, bool loop = false) {
            int ind = GetTakeIndex(takeName);
            if(ind == -1) { Debug.LogError("Take not found: " + takeName); return; }
            Play(ind, loop);
        }

        public void Play(int takeIndex, bool loop = false) {
            PlayAtTime(takeIndex, 0f, loop);
        }

        public void PlayAtFrame(string takeName, float frame, bool loop = false) {
            int ind = GetTakeIndex(takeName);
            if(ind == -1) { Debug.LogError("Take not found: " + takeName); return; }
            PlayAtTime(ind, frame / mSequences[ind].take.frameRate, loop);
        }

        public void PlayAtFrame(int takeIndex, float frame, bool loop = false) {
            PlayAtTime(takeIndex, frame / mSequences[takeIndex].take.frameRate, loop);
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
                mSequences[mLastPlayingTakeIndex].take.PlaySwitch(this); //notify take that we are switching

            SequenceControl amSeq = mSequences[index];

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
            mSequences[take].take.Reset(this);
        }

        public void ResetTake(string takeName) {
            int ind = GetTakeIndex(takeName);
            if(ind == -1) { Debug.LogError("Take not found: " + takeName); return; }
            ResetTake(ind);
        }

        public void Pause() {
            Take take = mCurrentPlayingTake;
            if(take == null) return;
            take.Pause(this);

            Sequence seq = currentPlayingSequence;
            if(seq != null)
                seq.Pause();
        }

        public void Resume() {
            Take take = mCurrentPlayingTake;
            if(take == null) return;
            take.Resume(this);

            Sequence seq = currentPlayingSequence;
            if(seq != null)
                seq.Play();
        }

        public void Stop() {
            Take take = mCurrentPlayingTake;
            if(take == null) return;
            take.Stop(this);

            //end camera fade
            if(AMCameraFade.hasInstance()) {
                AMCameraFade cf = AMCameraFade.getCameraFade();
                cf.playParam = null;
            }

            mSequences[mNowPlayingTakeIndex].Reset();

            mLastPlayingTakeIndex = mNowPlayingTakeIndex;
            mNowPlayingTakeIndex = -1;
        }

        public void GotoFrame(float frame) {
            Take take = mCurrentPlayingTake;
            Sequence seq = currentPlayingSequence;
            if(take != null && seq != null) {
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
            Take curTake = mSequences[takeIndex].take;
            curTake.previewFrame(this, frame);
        }

        // preview a single time (used for scrubbing)
        public void PreviewTime(string takeName, float time) {
            PreviewTime(GetTakeIndex(takeName), time);
        }

        public void PreviewTime(int takeIndex, float time) {
            Take curTake = mSequences[takeIndex].take;
            curTake.previewFrame(this, time * curTake.frameRate);
        }

        void OnDestroy() {
            if(mSequences != null) {
                for(int i = 0; i < mSequences.Length; i++)
                    mSequences[i].Destroy();
                mSequences = null;
            }

            takeCompleteCallback = null;
            takeTriggerCallback = null;
        }

        void OnEnable() {
            if(mStarted) {
                if(playOnEnable) {
                    if(mNowPlayingTakeIndex == -1 && !string.IsNullOrEmpty(defaultTakeName))
                        Play(defaultTakeName, false);
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

        void Awake() {
            if(!Application.isPlaying)
                return;
            
            List<Take> _t = _takes;
            mSequences = new SequenceControl[_t.Count];
            for(int i = 0; i < mSequences.Length; i++)
                mSequences[i] = new SequenceControl(this, i, _t[i]);
        }

        void Start() {
            if(!Application.isPlaying)
                return;

            mStarted = true;
            if(sequenceLoadAll && mSequences != null) {
                for(int i = 0; i < mSequences.Length; i++) {
                    if(mSequences[i].sequence == null)
                        mSequences[i].Build(sequenceKillWhenDone, updateType, updateTimeIndependent);
                }
            }

            if(!isPlaying && !string.IsNullOrEmpty(defaultTakeName)) {
                Play(defaultTakeName, false);
            }
        }

        #region ITarget interface
        Transform ITarget.root {
            get { return transform; }
        }

        AnimateMeta ITarget.meta {
            get { return _meta; }
            set { _meta = value; }
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

        void ITarget.SequenceTrigger(SequenceControl seq, Key key, TriggerParam parm) {
            if(takeTriggerCallback != null)
                takeTriggerCallback(this, seq.take, key, parm);
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

                        System.Type compType = track.GetRequiredComponent();
                        if(compType != null) {
                            Component comp = t.gameObject.GetComponent(compType);
                            if(comp == null) {
                                t.gameObject.AddComponent(compType);
                            }
                        }

                        foreach(var key in track.keys) {
                            compType = key.GetRequiredComponent();
                            if(compType != null) {
                                Component comp = t.gameObject.GetComponent(compType);
                                if(comp == null) {
                                    t.gameObject.AddComponent(compType);
                                }
                            }
                        }
                    }
                }

                if(mCache != null)
                    mCache.Clear();
            }
        }
        #endregion

        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            serializeData = new SerializeData();

            if(_meta == null)
                serializeData.Serialize(takeData);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            if(_meta == null)
                serializeData.Deserialize(takeData);
        }
    }
}