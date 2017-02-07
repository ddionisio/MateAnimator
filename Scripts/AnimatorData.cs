using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using DG.Tweening;

namespace M8.Animator {
	[ExecuteInEditMode]
	[AddComponentMenu("M8/Animator")]
	public class AnimatorData : MonoBehaviour, AMITarget, AMIMeta {
	    public enum DisableAction {
	        None,
	        Pause,
	        Stop,
            Reset //this guarantees that all tracks are set back to the first frame
	    }

	    public delegate void OnTake(AnimatorData anim, AMTakeData take);
	    public delegate void OnTakeTrigger(AnimatorData anim, AMTakeData take, AMKey key, AMTriggerData data);

	    // show

	    [SerializeField]
	    List<AMTakeData> takeData = new List<AMTakeData>();
	    [SerializeField]
	    int playOnStartIndex = -1;

	    [SerializeField]
	    AnimatorMeta meta; //
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

	    public event OnTake takeCompleteCallback;
	    public event OnTakeTrigger takeTriggerCallback;

	    public string defaultTakeName {
	        get {
	            if(meta)
	                return playOnStartMeta;
	            else
	                return playOnStartIndex == -1 ? "" : takeData[playOnStartIndex].name;
	        }
	        set {
	            if(meta) {
	                playOnStartMeta = value;
	                playOnStartIndex = -1;
	            }
	            else {
	                playOnStartMeta = "";
	                playOnStartIndex = -1;
	                if(!string.IsNullOrEmpty(value)) {
	                    List<AMTakeData> _ts = _takes;
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
	            AMTakeData take = mCurrentPlayingTake;
	            if(take == null) return 0f;
	            else return (take.totalFrames + take.endFramePadding) / (float)take.frameRate;
	        }
	    }

	    [HideInInspector]
	    public int codeLanguage = 0; 	// 0 = C#, 1 = Javascript

	    [HideInInspector]
	    [SerializeField]
	    private GameObject _dataHolder;

	    private AMSequence[] mSequences;

	    private int mNowPlayingTakeIndex = -1;
	    private int mLastPlayingTakeIndex = -1;

	    //private bool isLooping = false;
	    //private float takeTime = 0f;
	    private bool mStarted = false;

	    private float mAnimScale = 1.0f; //NOTE: this is reset during disable

	    private Dictionary<string, Transform> mCache;

	    private AMTakeData mCurrentPlayingTake { get { return mNowPlayingTakeIndex == -1 ? null : mSequences[mNowPlayingTakeIndex].take; } }

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

	                AMSequence amSeq;
	                if(mNowPlayingTakeIndex != -1 && (amSeq = mSequences[mNowPlayingTakeIndex]) != null) {
	                    if(amSeq.sequence != null)
	                        amSeq.sequence.timeScale = mAnimScale;
	                    if(amSeq.take != null)
	                        amSeq.take.SetAnimScale(this, mAnimScale);
	                }
	            }
	        }
	    }

	    List<AMTakeData> _takes {
	        get { return meta ? meta.takes : takeData; }
	    }

	    public bool TakeExists(string takeName) {
	        return GetTakeIndex(takeName) != -1;
	    }

	    public int GetTakeIndex(string takeName) {
	        List<AMTakeData> _t = _takes;
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
	        if(ind == -1) { Debug.LogError("Take not found: "+takeName); return; }
	        Play(ind, loop);
	    }

	    public void Play(int takeIndex, bool loop = false) {
	        PlayAtTime(takeIndex, 0f, loop);
	    }

	    public void PlayAtFrame(string takeName, float frame, bool loop = false) {
	        int ind = GetTakeIndex(takeName);
	        if(ind == -1) { Debug.LogError("Take not found: "+takeName); return; }
	        PlayAtTime(ind, frame/mSequences[ind].take.frameRate, loop);
	    }

	    public void PlayAtFrame(int takeIndex, float frame, bool loop = false) {
	        PlayAtTime(takeIndex, frame/mSequences[takeIndex].take.frameRate, loop);
	    }

	    public void PlayAtTime(string takeName, float time, bool loop = false) {
	        int ind = GetTakeIndex(takeName);
	        if(ind == -1) { Debug.LogError("Take not found: "+takeName); return; }
	        PlayAtTime(ind, time, loop);
	    }

	    public void PlayAtTime(int index, float time, bool loop = false) {
	        if(mNowPlayingTakeIndex == index)
	            return;

	        mLastPlayingTakeIndex = mNowPlayingTakeIndex;

	        Pause();

	        if(mLastPlayingTakeIndex != -1)
	            mSequences[mLastPlayingTakeIndex].take.PlaySwitch(this); //notify take that we are switching

	        AMSequence amSeq = mSequences[index];

	        AMTakeData newPlayTake = amSeq.take;

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

	    public void Pause() {
	        AMTakeData take = mCurrentPlayingTake;
	        if(take == null) return;
	        take.Pause(this);

	        Sequence seq = currentPlayingSequence;
	        if(seq != null)
	            seq.Pause();
	    }

	    public void Resume() {
	        AMTakeData take = mCurrentPlayingTake;
	        if(take == null) return;
	        take.Resume(this);

	        Sequence seq = currentPlayingSequence;
	        if(seq != null)
	            seq.Play();
	    }

	    public void Stop() {
	        AMTakeData take = mCurrentPlayingTake;
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
	        AMTakeData take = mCurrentPlayingTake;
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
	        AMTakeData curTake = mSequences[takeIndex].take;
	        curTake.previewFrame(this, frame);
	    }

	    // preview a single time (used for scrubbing)
	    public void PreviewTime(string takeName, float time) {
	        PreviewTime(GetTakeIndex(takeName), time);
	    }

	    public void PreviewTime(int takeIndex, float time) {
	        AMTakeData curTake = mSequences[takeIndex].take;
	        curTake.previewFrame(this, time*curTake.frameRate);
	    }

	    void OnDestroy() {
	#if UNITY_EDITOR
	        if(!Application.isPlaying) {
	            if(_dataHolder)
	                UnityEditor.Undo.DestroyObjectImmediate(_dataHolder);
	        }
	#endif

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

	        _dataHolder.SetActive(false);

	        List<AMTakeData> _t = _takes;
	        mSequences = new AMSequence[_t.Count];
	        for(int i = 0; i < mSequences.Length; i++)
	            mSequences[i] = new AMSequence(this, i, _t[i]);
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

	    #region AMITarget interface
	    Transform AMITarget.root {
	        get { return transform; }
	    }

	    Transform AMITarget.holder {
	        get {
	            if(meta) {
	                return meta.transform;
	            }
	            else {
	                if(_dataHolder == null) {
	                    foreach(Transform child in transform) {
	                        if(child.gameObject.name == "_animdata") {
	                            _dataHolder = child.gameObject;
	                            break;
	                        }
	                    }

	                    if(_dataHolder) {
	                        //refresh data?
	                    }
	                    else {
	#if MATE_DEBUG_ANIMATOR
	                        _dataHolder = new GameObject("_animdata", typeof(AnimatorDataHolder));
	#else
	                        _dataHolder = new GameObject("_animdata");
	#endif
	                        _dataHolder.transform.parent = transform;
	                    }
	                }

	                return _dataHolder.transform;
	            }
	        }
	    }

	    bool AMITarget.isMeta {
	        get { return meta != null; }
	    }

	    List<AMTakeData> AMITarget.takes {
	        get { return _takes; }
	    }

	    Transform AMITarget.GetCache(string path) {
	        Transform ret = null;
	        mCache.TryGetValue(path, out ret);
	        return ret;
	    }

	    void AMITarget.SetCache(string path, Transform obj) {
	        if(mCache == null) mCache = new Dictionary<string, Transform>();
	        if(mCache.ContainsKey(path))
	            mCache[path] = obj;
	        else {
	            mCache.Add(path, obj);

	            if(obj == null) //if object is null, report it as missing
	                Debug.LogWarning(name+ " is missing Target: "+path);
	        }
	    }

	    void AMITarget.SequenceComplete(AMSequence seq) {
	        //end camera fade
	        if(AMCameraFade.hasInstance()) {
	            AMCameraFade cf = AMCameraFade.getCameraFade();
	            cf.playParam = null;
	        }

	        mLastPlayingTakeIndex = mNowPlayingTakeIndex;
	        mNowPlayingTakeIndex = -1;

	        if(takeCompleteCallback != null)
	            takeCompleteCallback(this, seq.take);
	    }

	    void AMITarget.SequenceTrigger(AMSequence seq, AMKey key, AMTriggerData trigDat) {
	        if(takeTriggerCallback != null)
	            takeTriggerCallback(this, seq.take, key, trigDat);
	    }

	    string[] AMITarget.GetMissingTargets() {
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

	    void AMITarget.MaintainTargetCache(AMTrack track) {
	        if((this as AMITarget).isMeta && mCache.ContainsKey(track.targetPath)) {
	            UnityEngine.Object obj = track.GetTarget(this);
	            if(obj) {
	                string objPath = AMUtil.GetPath(transform, obj);
	                if(objPath != track.targetPath) {
	                    mCache.Remove(track.targetPath);
	                }
	            }
	        }
	    }

	    void AMITarget.MaintainTakes() {
	        foreach(AMTakeData take in _takes) {
	            take.maintainTake(this);
	        }

	        if(mCache != null)
	            mCache.Clear();
	    }

	    /// <summary>
	    /// attempt to generate the missing targets
	    /// </summary>
	    void AMITarget.GenerateMissingTargets(string[] missingPaths) {
	        if(missingPaths != null && missingPaths.Length > 0) {
	            for(int i = 0; i < missingPaths.Length; i++)
	                AMUtil.CreateTarget(transform, missingPaths[i]);

	            //fill necessary components per track and key
	            foreach(AMTakeData take in _takes) {
	                foreach(AMTrack track in take.trackValues) {
	                    Transform t = AMUtil.GetTarget(transform, track.targetPath);

	                    System.Type compType = track.GetRequiredComponent();
	                    if(compType != null) {
	                        Component comp = t.gameObject.GetComponent(compType);
	                        if(comp == null) {
	                            t.gameObject.AddComponent(compType);
	                        }
	                    }

	                    foreach(AMKey key in track.keys) {
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

	    #region Meta interfaces
	    AnimatorMeta AMIMeta.meta {
	        get { return meta; }
	        set { 
	            meta = value; 

	            if(meta)
	                takeData.Clear(); 
	        }
	    }

	    GameObject AMIMeta.dataHolder {
	        get { return _dataHolder; }
	        set { _dataHolder = value; }
	    }
	    #endregion
	}
}
