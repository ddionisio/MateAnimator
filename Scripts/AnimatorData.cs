using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Holoville.HOTween;

[ExecuteInEditMode]
[AddComponentMenu("M8/Animator")]
public class AnimatorData : MonoBehaviour {
    public enum DisableAction {
        None,
        Pause,
        Stop
    }

    public delegate void OnTake(AnimatorData anim, AMTakeData take);

    // show

	// obsolete stuff
	[SerializeField]
    List<AMTake> takes = new List<AMTake>();
	[SerializeField]
    AMTake playOnStart = null;

	public List<AMTakeData> takeData = new List<AMTakeData>();
	public int playOnStartIndex = -1;

    public bool sequenceLoadAll = true;
    public bool sequenceKillWhenDone = false;

    public bool playOnEnable = false;

    public DisableAction onDisableAction = DisableAction.Pause;

    public UpdateType updateType = UpdateType.Update;
    // hide

    public event OnTake takeCompleteCallback;

    public bool isPlaying {
        get {
            Sequence seq = currentPlayingSequence;
            return seq != null && !(seq.isPaused || seq.isComplete);
        }
    }

    public bool isPaused {
        get {
			Sequence seq = currentPlayingSequence;
			return seq != null && seq.isPaused;
        }
    }

    public bool isReversed {
        set {
			Sequence seq = currentPlayingSequence;
			if(seq != null) {
                if(value) {
					if(!seq.isReversed)
						seq.Reverse();
                }
                else {
					if(seq.isReversed)
						seq.Reverse();
                }
            }
        }

        get {
			Sequence seq = currentPlayingSequence;
			return seq != null && seq.isReversed;
        }
    }

    public string takeName {
        get {
			AMTakeData take = currentPlayingTake;
			if(take != null) return take.name;
            return "";
        }
    }

    public float runningTime {
        get {
			Sequence seq = currentPlayingSequence;
			return seq != null ? seq.elapsed : 0.0f;
        }
    }
    public float totalTime {
        get {
			AMTakeData take = currentPlayingTake;
			if(take == null) return 0f;
			else return (float)take.numFrames / (float)take.frameRate;
        }
    }

    [System.NonSerialized]
    public bool isAnimatorOpen = false;
    [System.NonSerialized]
    public bool isInspectorOpen = false;
    [System.NonSerialized]
    public bool inPlayMode = false;
    [HideInInspector]
    public float zoom = 0.4f;
    [HideInInspector]
    public int currentTake;
    [HideInInspector]
    public int codeLanguage = 0; 	// 0 = C#, 1 = Javascript
    [HideInInspector]
    public float gizmo_size = 0.05f;
    [HideInInspector]
    public float width_track = 150f;
    
    [HideInInspector]
    public bool autoKey = false;

    [HideInInspector]
    [SerializeField]
    private GameObject _dataHolder;

	private Sequence[] mSequences;

    private int mNowPlayingTakeIndex = -1;

    //private bool isLooping = false;
    //private float takeTime = 0f;
    private bool mStarted = false;

    private int _prevTake = -1;

    private float mAnimScale = 1.0f; //NOTE: this is reset during disable

	public AMTakeData currentPlayingTake { get { return mNowPlayingTakeIndex == -1 ? null : takeData[mNowPlayingTakeIndex]; } }
	public Sequence currentPlayingSequence { get { return mNowPlayingTakeIndex == -1 ? null : mSequences[mNowPlayingTakeIndex]; } }

    public int prevTake { get { return _prevTake; } }

    public float animScale {
        get { return mAnimScale; }
        set {
            if(mAnimScale != value) {
                mAnimScale = value;
				Sequence seq = currentPlayingSequence;
				if(seq != null)
                    seq.timeScale = mAnimScale;
            }
        }
    }

    public GameObject dataHolder {
        get {
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
                    _dataHolder = new GameObject("_animdata");
                    _dataHolder.transform.parent = transform;
                    _dataHolder.SetActive(false);
                }
            }

            return _dataHolder;
        }
    }

    void OnDestroy() {
        if(!Application.isPlaying) {
            if(_dataHolder) {
                DestroyImmediate(_dataHolder);
                _dataHolder = null;
            }
        }
        else {
			for(int i = 0; i < mSequences.Length; i++) {
				HOTween.Kill(mSequences[i]);
				mSequences[i] = null;
			}
        }

        takeCompleteCallback = null;
    }

    void OnEnable() {
        if(mStarted) {
            if(playOnEnable) {
                if(mNowPlayingTakeIndex == -1 && playOnStartIndex != -1)
					Play(playOnStartIndex, true, 0f, false);
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
        }

        mAnimScale = 1.0f;
    }

    void Awake() {
		Upgrade();

        if(!Application.isPlaying)
            return;

		mSequences = new Sequence[takeData.Count];
    }

    void Start() {
        if(!Application.isPlaying)
            return;

        mStarted = true;
		if(sequenceLoadAll && takeData != null) {
			string goName = gameObject.name;
			for(int i = 0; i < takeData.Count; i++) {
				mSequences[i] = takeData[i].BuildSequence(goName, sequenceKillWhenDone, updateType, OnTakeSequenceDone);
			}
        }

		if(playOnStartIndex != -1) {
			Play(playOnStartIndex, true, 0.0f, false);
        }
    }

    void OnDrawGizmos() {
        if(!isAnimatorOpen) return;
        takeData[currentTake].drawGizmos(gizmo_size, inPlayMode);
    }

	//returns true if upgraded
	public bool Upgrade() {
		//convert AMTakes
		if(takes != null && takes.Count > 0) {
			if(playOnStart != null) {
				playOnStartIndex = takes.IndexOf(playOnStart);
				playOnStart = null;
			}

			takeData = new List<AMTakeData>(takes.Count);
			foreach(AMTake take in takes) {
				AMTakeData ntake = new AMTakeData();
				ntake.name = take.name;
				ntake.frameRate = take.frameRate;
				ntake.numFrames = take.numFrames;
				ntake.startFrame = take.startFrame;
				ntake.endFrame = take.endFrame;
				ntake.playbackSpeedIndex = take.playbackSpeedIndex;
				ntake.numLoop = take.numLoop;
				ntake.loopMode = take.loopMode;
				ntake.loopBackToFrame = take.loopBackToFrame;
				ntake.selectedTrack = take.selectedTrack;
				ntake.selectedFrame = take.selectedFrame;
				ntake.selectedGroup = take.selectedGroup;
				ntake.trackValues = new List<AMTrack>(take.trackValues.Count);
				foreach(AMTrack track in take.trackValues) ntake.trackValues.Add(track);
				ntake.contextSelection = new List<int>(take.contextSelection);
				ntake.ghostSelection = new List<int>(take.ghostSelection);
				ntake.contextSelectionTracks = new List<int>(take.contextSelectionTracks);
				ntake.track_count = take.track_count;
				ntake.group_count = take.group_count;
				ntake.rootGroup = take.rootGroup != null ? take.rootGroup.duplicate() : null;
				ntake.groupValues = new List<AMGroup>(take.groupValues.Count);
				foreach(AMGroup grp in take.groupValues) ntake.groupValues.Add(grp.duplicate());

				DestroyImmediate(take);

				takeData.Add(ntake);
			}

			takes = null;

			return true;
		}

		return false;
	}

    public string[] GenerateTakeNames(bool firstIndexNone = true) {
		if(takeData != null) {
			string[] ret = firstIndexNone ? new string[takeData.Count + 1] : new string[takeData.Count];

            if(firstIndexNone)
                ret[0] = "None";

			for(int i = 0; i < takeData.Count; i++) {
				ret[firstIndexNone ? i + 1 : i] = takeData[i].name;
			}

            return ret;
        }
        else {
            return firstIndexNone ? new string[] { "None" } : null;
        }
    }

    public void PlayDefault(bool loop = false) {
        if(playOnStartIndex != -1) {
			Play(takeData[playOnStartIndex].name, loop);
		}
    }

    // play take by name
    public void Play(string takeName, bool loop = false) {
		PlayAtFrame(takeName, 0f, loop);
    }

	public void PlayAtFrame(string takeName, float frame, bool loop = false) {
		int ind = getTakeIndex(takeName);
		if(ind == -1) { Debug.LogError("Take not found: "+takeName); return; }
		Play(ind, true, frame, loop);
	}
	
	public void PlayAtTime(string takeName, float time, bool loop = false) {
		int ind = getTakeIndex(takeName);
		if(ind == -1) { Debug.LogError("Take not found: "+takeName); return; }
		Play(ind, false, time, loop);
    }


    public void Pause() {
		AMTakeData take = currentPlayingTake;
		if(take == null) return;
		take.stopAudio();

		Sequence seq = currentPlayingSequence;
		if(seq != null)
			seq.Pause();
    }

    public void Resume() {
		Sequence seq = currentPlayingSequence;
		if(seq != null)
			seq.Play();
    }

    public void Stop() {
		AMTakeData take = currentPlayingTake;
		if(take == null) return;
		take.stopAudio();
		take.stopAnimations();

		Sequence seq = currentPlayingSequence;
		if(seq != null) {
			seq.Pause();
			seq.GoTo(0);
        }

        mNowPlayingTakeIndex = -1;
    }

    public void GotoFrame(float frame) {
		AMTakeData take = currentPlayingTake;
		Sequence seq = currentPlayingSequence;
		if(take != null && seq != null) {
            float t = frame / take.frameRate;
            seq.GoTo(t);
        }
        else {
            Debug.LogWarning("No take playing...");
        }
    }

    public void Reverse() {
		Sequence seq = currentPlayingSequence;
        if(seq != null)
			seq.Reverse();
    }

    // preview a single frame (used for scrubbing)
    public void PreviewFrame(string takeName, float frame) {
        PreviewValue(takeName, true, frame);
    }

    // preview a single time (used for scrubbing)
    public void PreviewTime(string takeName, float time) {
        PreviewValue(takeName, false, time);
    }

    void Play(int index, bool isFrame, float value, bool loop) {
        AMTakeData newPlayTake = takeData[index];

        if(newPlayTake == null) {
            Stop();
            return;
        }

        if(mNowPlayingTakeIndex != index) {
            Pause();
        }

		mNowPlayingTakeIndex = index;

        float startTime = value;
		if(isFrame) startTime /= newPlayTake.frameRate;

        float startFrame = 0;//isFrame ? value : nowPlayingTake.frameRate * value;

		Sequence seq = mSequences[index];

		if(seq == null) {
			newPlayTake.previewFrame(startFrame, false, true);
			seq = mSequences[index] = newPlayTake.BuildSequence(gameObject.name, sequenceKillWhenDone, updateType, OnTakeSequenceDone);
		}
		else {
			//TODO: make this more efficient
			if(value == 0.0f)
				newPlayTake.previewFrame(0, false, true);
		}

		if(seq != null) {
            if(loop) {
				seq.loops = -1;
            }
            else {
				seq.loops = newPlayTake.numLoop;
            }

			seq.GoTo(startTime);
			seq.Play();
			seq.timeScale = mAnimScale;
        }
    }

    void PreviewValue(string take_name, bool isFrame, float value) {
		AMTakeData curTake = currentPlayingTake;
		AMTakeData take = curTake != null && curTake.name == takeName ? curTake : getTake(take_name);
		if(take == null) return;
		float startFrame = value;
		if(!isFrame) startFrame *= take.frameRate;	// convert time to frame
		take.previewFrame(startFrame);
	}

    public int getTakeCount() {
		return takeData.Count;
    }

    public bool setCurrentTakeValue(int _take) {
        if(_take != currentTake) {
            _prevTake = currentTake;

            // reset preview to frame 1
            getCurrentTake().previewFrame(1f);
            // change take
            currentTake = _take;
            return true;
        }
        return false;
    }

    public AMTakeData getCurrentTake() {
		if(takeData == null || currentTake >= takeData.Count || currentTake < 0) return null;
		return takeData[currentTake];
    }

    public AMTakeData getPreviousTake() {
		return takeData != null && _prevTake >= 0 && _prevTake < takeData.Count ? takeData[_prevTake] : null;
    }

	public int getTakeIndex(string takeName) {
		for(int i = 0; i < takeData.Count; i++) {
			if(takeData[i].name == takeName)
				return i;
		}
		return -1;
	}

    public AMTakeData getTake(string takeName) {
		int ind = getTakeIndex(takeName);
		if(ind == -1) {
        	Debug.LogError("Animator: Take '" + takeName + "' not found.");
        	return null;
		}

		return takeData[ind];
    }

    public AMTakeData addTake() {
        string name = "Take" + (takeData.Count + 1);
        AMTakeData a = new AMTakeData();
        // set defaults
        a.name = name;
        makeTakeNameUnique(a);
        
        takeData.Add(a);
		selectTake(takeData.Count - 1);

        return a;
    }

    /// <summary>
    /// This will only duplicate the tracks and groups
    /// </summary>
    /// <param name="take"></param>
    public List<UnityEngine.Object> duplicateTake(AMTakeData dupTake, bool includeKeys) {
		List<UnityEngine.Object> ret = new List<UnityEngine.Object>();

		AMTakeData a = new AMTakeData();

        a.name = dupTake.name;
        makeTakeNameUnique(a);
        a.numLoop = dupTake.numLoop;
        a.loopMode = dupTake.loopMode;
        a.frameRate = dupTake.frameRate;
        a.numFrames = dupTake.numFrames;
        a.startFrame = dupTake.startFrame;
        a.selectedFrame = 1;
        a.selectedTrack = dupTake.selectedTrack;
        a.selectedGroup = dupTake.selectedGroup;
        a.playbackSpeedIndex = 2;
        //a.lsTracks = new List<AMTrack>();
        //a.dictTracks = new Dictionary<int,AMTrack>();

        if(dupTake.rootGroup != null) {
            a.rootGroup = dupTake.rootGroup.duplicate();
        }
        else {
            a.initGroups();
        }

        a.group_count = dupTake.group_count;

        if(dupTake.groupValues != null) {
            a.groupValues = new List<AMGroup>();
            foreach(AMGroup grp in dupTake.groupValues) {
                a.groupValues.Add(grp.duplicate());
            }
        }

        a.track_count = dupTake.track_count;

        if(dupTake.trackValues != null) {
            a.trackValues = new List<AMTrack>();
            foreach(AMTrack track in dupTake.trackValues) {
                AMTrack dupTrack = track.duplicate(dataHolder);

                a.trackValues.Add(dupTrack);

				if(includeKeys) {
					foreach(AMKey key in track.keys) {
						AMKey dupKey = key.CreateClone(dataHolder);
						if(dupKey) {
							dupTrack.keys.Add(dupKey);
							ret.Add(dupKey);
						}
					}

					dupTrack.updateCache();
				}

                ret.Add(dupTrack);
            }
        }
        a.contextSelection = new List<int>();
        a.ghostSelection = new List<int>();
        a.contextSelectionTracks = new List<int>();

		takeData.Add(a);
		selectTake(takeData.Count - 1);

        return ret;
    }

    public void deleteTake(int index) {
        //if(shouldCheckDependencies) shouldCheckDependencies = false;
		if(playOnStartIndex != -1) {
			if(playOnStartIndex == index) playOnStartIndex = -1;
			else if(index < playOnStartIndex) playOnStartIndex--;
		}
		//TODO: destroy tracks, keys
		//takeData[index].destroy();
		takeData.RemoveAt(index);
        if((currentTake >= index) && (currentTake > 0)) currentTake--;
    }

    public void selectTake(int index) {
        if(currentTake != index)
            _prevTake = currentTake;

        currentTake = index;
    }

    public void selectTake(string name) {
		for(int i = 0; i < takeData.Count; i++) {
			if(takeData[i].name == name) {
                selectTake(i);
                break;
            }
		}
    }
    public void makeTakeNameUnique(AMTakeData take) {
        bool loop = false;
        int count = 0;
        do {
            if(loop) loop = false;
			foreach(AMTakeData _take in takeData) {
                if(_take != take && _take.name == take.name) {
                    if(count > 0) take.name = take.name.Substring(0, take.name.Length - 3);
                    count++;
                    take.name += "(" + count + ")";
                    loop = true;
                    break;
                }
            }
        } while(loop);
    }

    public string[] getTakeNames() {
		string[] names = new string[takeData.Count + 2];
		for(int i = 0; i < takeData.Count; i++) {
			names[i] = takeData[i].name;
        }
        names[names.Length - 2] = "Create new...";
        names[names.Length - 1] = "Duplicate current...";
        return names;
    }

    public int getTakeIndex(AMTakeData take) {
		for(int i = 0; i < takeData.Count; i++) {
			if(takeData[i] == take) return i;
        }
        return -1;
    }
    public bool setCodeLanguage(int codeLanguage) {
        if(this.codeLanguage != codeLanguage) {
            this.codeLanguage = codeLanguage;
            return true;
        }
        return false;
    }
    public bool setGizmoSize(float gizmo_size) {
        if(this.gizmo_size != gizmo_size) {
            this.gizmo_size = gizmo_size;
            // update target gizmo size
            foreach(Object target in GameObject.FindObjectsOfType(typeof(AMTarget))) {
                if((target as AMTarget).gizmo_size != gizmo_size) (target as AMTarget).gizmo_size = gizmo_size;
            }
            return true;
        }
        return false;
    }

    /*public bool setShowWarningForLostReferences(bool showWarningForLostReferences) {
        if(this.showWarningForLostReferences != showWarningForLostReferences) {
            this.showWarningForLostReferences = showWarningForLostReferences;
            return true;
        }
        return false;
    }*/

    public void deleteAllTakesExcept(AMTakeData take) {
		for(int index = 0; index < takeData.Count; index++) {
			if(takeData[index] == take) continue;
            deleteTake(index);
            index--;
        }
    }

    public void mergeWith(AnimatorData _aData) {
		foreach(AMTakeData take in _aData.takeData) {
			takeData.Add(take);
            makeTakeNameUnique(take);
        }
    }

    public List<GameObject> getDependencies(AMTakeData _take = null) {
        // if only one take
        if(_take != null) return _take.getDependencies().ToList();

        // if all takes
        List<GameObject> ls = new List<GameObject>();
        foreach(AMTakeData take in takeData) {
            ls = ls.Union(take.getDependencies()).ToList();
        }
        return ls;
    }

    public List<GameObject> updateDependencies(List<GameObject> newReferences, List<GameObject> oldReferences) {
        List<GameObject> lsFlagToKeep = new List<GameObject>();
        foreach(AMTakeData take in takeData) {
            lsFlagToKeep = lsFlagToKeep.Union(take.updateDependencies(newReferences, oldReferences)).ToList();
        }
        return lsFlagToKeep;
    }

    void OnTakeSequenceDone(AMTakeData aTake) {
        if(takeCompleteCallback != null)
            takeCompleteCallback(this, aTake);
    }
}
