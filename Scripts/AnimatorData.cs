using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Holoville.HOTween;

[ExecuteInEditMode]
[AddComponentMenu("M8/Animator")]
public class AnimatorData : MonoBehaviour {
    // show
    public List<AMTake> takes = new List<AMTake>();
    public AMTake playOnStart = null;

    public bool sequenceLoadAll = true;
    public bool sequenceKillWhenDone = false;
    public bool playOnEnable = false;
    public UpdateType updateType = UpdateType.Update;
    // hide

    public bool isPlaying {
        get {
            if(nowPlayingTake == null) return false;

            Sequence seq = nowPlayingTake.sequence;
                        
            return seq != null && !(seq.isPaused || seq.isComplete);
        }
    }

    public bool isPaused {
        get {
            return nowPlayingTake != null && nowPlayingTake.sequence != null && nowPlayingTake.sequence.isPaused;
        }
    }

    public bool isReversed {
        get {
            return nowPlayingTake != null && nowPlayingTake.sequence != null && nowPlayingTake.sequence.isReversed;
        }
    }

    public string takeName {
        get {
            if(nowPlayingTake != null) return nowPlayingTake.name;
            return null;
        }
    }

    public float runningTime {
        get {
            if(takeName == null) return 0f;
            else {
                if(nowPlayingTake != null && nowPlayingTake.sequence != null)
                    return nowPlayingTake.sequence.elapsed;
                else
                    return 0.0f;
            }
        }
    }
    public float totalTime {
        get {
            if(takeName == null) return 0f;
            else return (float)nowPlayingTake.numFrames / (float)nowPlayingTake.frameRate;
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
    // temporary variables for selecting a property
    //[HideInInspector] public bool didSelectProperty = false;
    //[HideInInspector] public AMPropertyTrack propertySelectTrack;
    //[HideInInspector] public Component propertyComponent;
    //[HideInInspector] public PropertyInfo propertyInfo;
    //[HideInInspector] public FieldInfo fieldInfo;
    [HideInInspector]
    public bool autoKey = false;

    [HideInInspector]
    [SerializeField]
    private GameObject _dataHolder;

    //[HideInInspector]
    //public float elapsedTime = 0f;
    // private
    private AMTake nowPlayingTake = null;
    //private bool isLooping = false;
    //private float takeTime = 0f;
    private bool mStarted = false;

    private bool mPlayOnEnable = false;

    public AMTake currentPlayingTake { get { return nowPlayingTake; } }

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

    public object Invoker(object[] args) {
        switch((int)args[0]) {
            // check if is playing
            case 0:
                return isPlaying;
            // get take name
            case 1:
                return takeName;
            // play
            case 2:
                Play((string)args[1], true, 0f, (bool)args[2]);
                break;
            // stop
            case 3:
                Stop();
                break;
            // pause
            case 4:
                Pause();
                break;
            // resume
            case 5:
                Resume();
                break;
            // play from time
            case 6:
                Play((string)args[1], false, (float)args[2], (bool)args[3]);
                break;
            // play from frame
            case 7:
                Play((string)args[1], true, (float)((int)args[2]), (bool)args[3]);
                break;
            // preview frame
            case 8:
                PreviewValue((string)args[1], true, (float)args[2]);
                break;
            // preview time
            case 9:
                PreviewValue((string)args[1], false, (float)args[2]);
                break;
            // running time
            case 10:
                return runningTime;
            // total time
            case 11:
                if(takeName == null) return 0f;
                else return (float)nowPlayingTake.numFrames / (float)nowPlayingTake.frameRate;
            case 12:
                return isPaused;
            default:
                break;
        }
        return null;
    }

    void OnDestroy() {
        if(!Application.isPlaying) {
            if(_dataHolder) {
                DestroyImmediate(_dataHolder);
                _dataHolder = null;
            }
        }
        else {
            foreach(AMTake take in takes) {
                take.destroy();
            }
        }

        /*playOnStart = null;

        foreach(AMTake take in takes) {
            take.destroy();
        }

        takes.Clear();*/
    }

    void OnEnable() {
        if(mStarted && playOnEnable) {
            if(isPaused) {
                if(mPlayOnEnable) {
                    Resume();
                }
            }
            else if(playOnStart) {
                Play(playOnStart.name, true, 0f, false);
            }
        }

        mPlayOnEnable = false;
    }

    void OnDisable() {
        if(isPlaying) {
            Pause();
            mPlayOnEnable = true;
        }
    }

    void Awake() {
    }

    void Start() {
        if(!Application.isPlaying)
            return;

        mStarted = true;
        if(sequenceLoadAll && takes != null) {
            foreach(AMTake take in takes)
                take.BuildSequence(gameObject.name, sequenceKillWhenDone, updateType);
        }

        if(playOnStart) {
            Play(playOnStart.name, true, 0.0f, false);
        }
    }

    void OnDrawGizmos() {
        if(!isAnimatorOpen) return;
        takes[currentTake].drawGizmos(gizmo_size, inPlayMode);
    }

    /*void Update() {
        if(_isPaused || nowPlayingTake == null) return;
        elapsedTime += Time.deltaTime;
        if(elapsedTime >= takeTime) {
            nowPlayingTake.stopAudio();
            if(isLooping) Execute(nowPlayingTake);
            else nowPlayingTake = null;
        }
    }*/

    public string[] GenerateTakeNames(bool firstIndexNone = true) {
        if(takes != null) {
            string[] ret = firstIndexNone ? new string[takes.Count + 1] : new string[takes.Count];

            if(firstIndexNone)
                ret[0] = "None";

            for(int i = 0; i < takes.Count; i++) {
                ret[firstIndexNone ? i + 1 : i] = takes[i].name;
            }

            return ret;
        }
        else {
            return firstIndexNone ? new string[] { "None" } : null;
        }
    }

    // play take by name
    public void Play(string takeName, bool loop = false) {
        Play(takeName, true, 0f, loop);
    }

    public void Pause() {
        if(nowPlayingTake == null) return;
        nowPlayingTake.stopAudio();
        //AMTween.Pause();

        if(nowPlayingTake.sequence != null)
            nowPlayingTake.sequence.Pause();

    }

    public void Resume() {
        if(nowPlayingTake == null) return;

        if(nowPlayingTake.sequence != null)
            nowPlayingTake.sequence.Play();
    }

    public void Stop() {
        if(nowPlayingTake == null) return;
        nowPlayingTake.stopAudio();
        nowPlayingTake.stopAnimations();

        if(nowPlayingTake.sequence != null) {
            nowPlayingTake.sequence.GoTo(0.0f, true);
            nowPlayingTake.sequence.Pause();
        }

        nowPlayingTake = null;
    }

    public void Reverse() {
        if(nowPlayingTake == null) return;

        if(nowPlayingTake.sequence != null)
            nowPlayingTake.sequence.Reverse();
    }

    // play take by name from time
    public void PlayFromTime(string takeName, float time, bool loop = false) {
        Play(takeName, false, time, loop);
    }

    // play take by name from frame
    public void PlayFromFrame(string takeName, int frame, bool loop = false) {
        Play(takeName, true, (float)frame, loop);
    }

    // preview a single frame (used for scrubbing)
    public void PreviewFrame(string takeName, float frame) {
        PreviewValue(takeName, true, frame);
    }

    // preview a single time (used for scrubbing)
    public void PreviewTime(string takeName, float time) {
        PreviewValue(takeName, false, time);
    }

    void Play(string take_name, bool isFrame, float value, bool loop) {
        AMTake newPlayTake = getTake(take_name);

        if(newPlayTake != nowPlayingTake) {
            Stop();
        }

        nowPlayingTake = newPlayTake;

        if(nowPlayingTake) {
            if(nowPlayingTake.sequence == null)
                nowPlayingTake.BuildSequence(gameObject.name, sequenceKillWhenDone, updateType);

            if(nowPlayingTake.sequence != null) {
                if(loop) {
                    nowPlayingTake.sequence.loops = -1;
                }
                else {
                    nowPlayingTake.sequence.loops = nowPlayingTake.numLoop;
                }

                float startTime = value;
                if(isFrame) startTime /= nowPlayingTake.frameRate;
                                
                nowPlayingTake.sequence.Play();
                nowPlayingTake.sequence.GoTo(startTime);
            }

            //isLooping = loop;

            //Execute(nowPlayingTake, isFrame, value);
        }
    }

    void PreviewValue(string take_name, bool isFrame, float value) {
        AMTake take;
        if(nowPlayingTake && nowPlayingTake.name == takeName) take = nowPlayingTake;
        else take = getTake(take_name);
        if(!take) return;
        float startFrame = value;
        if(!isFrame) startFrame *= take.frameRate;	// convert time to frame
        take.previewFrame(startFrame);
    }

    void Execute(AMTake take, bool isFrame = true, float value = 0f /* frame or time */) {
        //if(nowPlayingTake != null)
        //AMTween.Stop();
        // delete AMCameraFade
        float startFrame = value;
        float startTime = value;
        if(!isFrame) startFrame *= take.frameRate;	// convert time to frame
        if(isFrame) startTime /= take.frameRate;	// convert frame to time
        take.executeActions(startFrame);
        //elapsedTime = startTime;
        //takeTime = (float)take.numFrames / (float)take.frameRate;
        nowPlayingTake = take;

    }

    public int getCurrentTakeValue() {
        return currentTake;
    }

    public int getTakeCount() {
        return takes.Count;
    }

    public bool setCurrentTakeValue(int _take) {
        if(_take != currentTake) {
            // reset preview to frame 1
            getCurrentTake().previewFrame(1f);
            // change take
            currentTake = _take;
            return true;
        }
        return false;
    }

    public AMTake getCurrentTake() {
        if(takes == null || currentTake >= takes.Count) return null;
        return takes[currentTake];
    }

    public AMTake getTake(string takeName) {
        foreach(AMTake take in takes) {
            if(take.name == takeName) return take;
        }
        Debug.LogError("Animator: Take '" + takeName + "' not found.");
        return new AMTake(null);
    }

    public void addTake() {
        string name = "Take" + (takes.Count + 1);
        AMTake a = AMTake.NewInstance(dataHolder);
        // set defaults
        a.name = name;
        makeTakeNameUnique(a);
        a.numLoop = 1;
        a.loopMode = LoopType.Restart;
        a.frameRate = 24;
        a.numFrames = 1440;
        a.startFrame = 1;
        a.selectedFrame = 1;
        a.selectedTrack = -1;
        a.playbackSpeedIndex = 2;
        //a.lsTracks = new List<AMTrack>();
        //a.dictTracks = new Dictionary<int,AMTrack>();
        a.trackKeys = new List<int>();
        a.trackValues = new List<AMTrack>();
        takes.Add(a);
        selectTake(takes.Count - 1);

    }

    public void deleteTake(int index) {
        //if(shouldCheckDependencies) shouldCheckDependencies = false;
        if(playOnStart == takes[index]) playOnStart = null;
        takes[index].destroy();
        takes.RemoveAt(index);
        if((currentTake >= index) && (currentTake > 0)) currentTake--;
    }

    public void deleteCurrentTake() {
        deleteTake(currentTake);
    }

    public void selectTake(int index) {
        currentTake = index;
    }

    public void selectTake(string name) {
        for(int i = 0; i < takes.Count; i++)
            if(takes[i].name == name) {
                selectTake(i);
                break;
            }
    }
    public void makeTakeNameUnique(AMTake take) {
        bool loop = false;
        int count = 0;
        do {
            if(loop) loop = false;
            foreach(AMTake _take in takes) {
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
        string[] names = new string[takes.Count + 1];
        for(int i = 0; i < takes.Count; i++) {
            names[i] = takes[i].name;
        }
        names[names.Length - 1] = "Create new...";
        return names;
    }

    public int getTakeIndex(AMTake take) {
        for(int i = 0; i < takes.Count; i++) {
            if(takes[i] == take) return i;
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

    public void deleteAllTakesExcept(AMTake take) {
        for(int index = 0; index < takes.Count; index++) {
            if(takes[index] == take) continue;
            deleteTake(index);
            index--;
        }
    }

    public void mergeWith(AnimatorData _aData) {
        foreach(AMTake take in _aData.takes) {
            takes.Add(take);
            makeTakeNameUnique(take);
        }
    }

    public List<GameObject> getDependencies(AMTake _take = null) {
        // if only one take
        if(_take != null) return _take.getDependencies().ToList();

        // if all takes
        List<GameObject> ls = new List<GameObject>();
        foreach(AMTake take in takes) {
            ls = ls.Union(take.getDependencies()).ToList();
        }
        return ls;
    }

    public List<GameObject> updateDependencies(List<GameObject> newReferences, List<GameObject> oldReferences) {
        List<GameObject> lsFlagToKeep = new List<GameObject>();
        foreach(AMTake take in takes) {
            lsFlagToKeep = lsFlagToKeep.Union(take.updateDependencies(newReferences, oldReferences)).ToList();
        }
        return lsFlagToKeep;
    }

}
