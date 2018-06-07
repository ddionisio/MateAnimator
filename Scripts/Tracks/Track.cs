using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.Animator {
    public abstract class Track {
        public abstract SerializeType serializeType { get; }
        
        public int id;
        public string name;        
        public bool foldout = true;                         // whether or not to foldout track in timeline GUI

        [SerializeField]
        protected string _targetPath; //for animations saved as meta
                
        public virtual int version { get { return 1; } } //must be at least 1

        public virtual int order { get { return 0; } }

        public List<Key> keys { get { return mKeys; } set { mKeys = value; } }
        private List<Key> mKeys = new List<Key>();

        /// <summary>
        /// If true, then track has settings to display even if there is no key selected.
        /// </summary>
        public virtual bool hasTrackSettings { get { return false; } }

        public virtual bool canTween { get { return true; } }

        public virtual int interpCount { get { return 2; } } //at some point, all tracks can potentially use curve...

        public string targetPath { get { return _targetPath; } }

        // set name based on index
        public void setName(int index) {
            name = "Track" + (index + 1);
        }

        public bool started { get; private set; }

        //TODO: change (Set/Get)SerializeObject to Unity.Engine.Object target { get; set; }

        /// <summary>
        /// Stores the obj to serialized field based on track type, obj is null if _targetPath is used
        /// </summary>
        /// <param name="target">Target.</param>
        protected abstract void SetSerializeObject(UnityEngine.Object obj);

        /// <summary>
        /// Gets the serialize object. If targetGO is not null, return the appropriate object from targetGO (e.g. if it's a specific component).
        /// Otherwise, if targetGO is null, grab from serialized field
        /// </summary>
        /// <returns>The serialize object.</returns>
        /// <param name="go">Go.</param>
        protected abstract UnityEngine.Object GetSerializeObject(GameObject targetGO);

        /// <summary>
        /// Gets the target relative to animator's hierarchy if we are referencing via AnimatorMeta
        /// </summary>
        public UnityEngine.Object GetTarget(ITarget target) {
            UnityEngine.Object ret = null;

            if(target.meta && !string.IsNullOrEmpty(_targetPath)) { //if targetPath is empty, it must be from the project, so grab it directly
                Transform tgt = target.GetCache(_targetPath);
                if(tgt)
                    ret = GetSerializeObject(tgt.gameObject);
                else {
                    tgt = Utility.GetTarget(target.root, _targetPath);
                    target.SetCache(_targetPath, tgt);
                    if(tgt)
                        ret = GetSerializeObject(tgt.gameObject);
                }
            }
            else
                ret = GetSerializeObject(null);

            return ret;
        }

        public virtual System.Type GetRequiredComponent() {
            return null;
        }

        /// <summary>
        /// Check to see if given GameObject has all the required components for this track
        /// </summary>
        public bool VerifyComponents(GameObject go) {
            if(!go) return false;

            System.Type compType = GetRequiredComponent();
            if(compType != null && go.GetComponent(compType) == null)
                return false;

            foreach(Key key in keys) {
                compType = key.GetRequiredComponent();
                if(compType != null && go.GetComponent(compType) == null)
                    return false;
            }

            return true;
        }

        public string GetTargetPath(ITarget target) {
            if(target.meta)
                return _targetPath;
            else
                return Utility.GetPath(target.root, GetSerializeObject(null));
        }

        public virtual void SetTarget(ITarget target, Transform item) {
            if(target.meta && item) {
                _targetPath = Utility.GetPath(target.root, item);
                target.SetCache(_targetPath, item);
                SetSerializeObject(GetSerializeObject(item.gameObject));
            }
            else {
                _targetPath = "";
                SetSerializeObject(item ? GetSerializeObject(item.gameObject) : null);
            }
        }
                
        public virtual bool isTargetEqual(ITarget target, UnityEngine.Object obj) {
            return GetTarget(target) == obj;
        }

        public virtual void maintainTrack(ITarget itarget) {
            Object obj = null;

            //fix the target info
            if(itarget.meta) {
                if(string.IsNullOrEmpty(_targetPath)) {
                    obj = GetSerializeObject(null);
                    if(obj) {
                        _targetPath = Utility.GetPath(itarget.root, obj);
                        itarget.SetCache(_targetPath, Utility.GetTransform(obj));
                    }
                }
                SetSerializeObject(null);
            }
            else {
                obj = GetSerializeObject(null);
                if(obj == null) {
                    if(!string.IsNullOrEmpty(_targetPath)) {
                        Transform tgt = itarget.GetCache(_targetPath);
                        if(tgt == null)
                            tgt = Utility.GetTarget(itarget.root, _targetPath);
                        if(tgt)
                            obj = GetSerializeObject(tgt.gameObject);
                        SetSerializeObject(obj);
                    }
                }
                _targetPath = "";
            }

            //maintain keys
            foreach(Key key in keys)
                key.maintainKey(itarget, obj);
        }

        // set name from string
        public void setName(string name) {
            this.name = name;
        }

        // does track have key on frame
        public bool hasKeyOnFrame(int _frame) {
            foreach(Key key in keys) {
                if(key != null && key.frame == _frame) return true;
            }
            return false;
        }

        // draw track gizmos
        public virtual void drawGizmos(ITarget target, float gizmo_size, bool inPlayMode, int frame) { }

        // preview frame
        public virtual void previewFrame(ITarget target, float frame, int frameRate, bool play, float playSpeed) { }

        // update cache
        public virtual void updateCache(ITarget target) {
            sortKeys();
        }

        /// <summary>
        /// Only called during editor
        /// </summary>
        public virtual void undoRedoPerformed() {

        }

        public virtual void buildSequenceStart(SequenceControl sequence) {
        }

        /// <summary>
        /// Called when about to play during runtime, also use for initializing things (mostly for tweeners)
        /// </summary>
        public virtual void PlayStart(ITarget itarget, float frame, int frameRate, float animScale) {
            if(!started) {
                started = true;

                //preview from starting frame so that the first tweener will grab the appropriate start value
                if(canTween && keys.Count > 1 && keys[0].canTween)
                    previewFrame(itarget, 0f, frameRate, false, 1.0f);
            }
            else {
                //apply first frame if frame > first frame
                if(canTween && keys.Count > 1 && keys[0].canTween && keys[0].frame > Mathf.RoundToInt(frame))
                    previewFrame(itarget, 0f, frameRate, false, 1.0f);
            }
        }

        public void Reset(ITarget itarget, int frameRate) {
            previewFrame(itarget, 0f, frameRate, false, 1.0f);
        }

        /// <summary>
        /// Called when we are switching take
        /// </summary>
        public virtual void PlaySwitch(ITarget itarget) {
        }

        /// <summary>
        /// Called when sequence, or preview during edit, has completed
        /// </summary>
        public virtual void PlayComplete(ITarget itarget) {
        }

        /// <summary>
        /// Called when stopping during runtime or preview in AMTimeline
        /// </summary>
        public virtual void Stop(ITarget itarget) {
        }

        /// <summary>
        /// Called when pausing during runtime
        /// </summary>
        public virtual void Pause(ITarget itarget) {
        }

        /// <summary>
        /// Called when resuming during runtime
        /// </summary>
        public virtual void Resume(ITarget itarget) {

        }

        /// <summary>
        /// Called when setting animation play scale during runtime
        /// </summary>
        public virtual void SetAnimScale(ITarget itarget, float scale) {

        }

        public virtual AnimatorTimeline.JSONInit getJSONInit(ITarget target) {
            Debug.LogWarning("Animator: No override for getJSONInit()");
            return new AnimatorTimeline.JSONInit();
        }

        // get key on frame
        public Key getKeyOnFrame(int _frame, bool showWarning = true) {
            foreach(Key key in keys) {
                if(key.frame == _frame) return key;
            }
            if(showWarning)
                Debug.LogError("Animator: No key found on frame " + _frame);
            return null;
        }

        // track type as string
        public virtual string getTrackType() {
            return "Unknown";
        }

        public void sortKeys() {
            // sort
            keys.Sort((c, d) => c.frame.CompareTo(d.frame));
        }

        public void deleteKeyOnFrame(int frame) {
            for(int i = 0; i < keys.Count; i++) {
                if(keys[i].frame == frame) {
                    keys[i].destroy();
                    keys.RemoveAt(i);
                }
            }
        }

        public Key[] removeKeyOnFrame(int frame) {
            List<Key> rkeys = new List<Key>(keys.Count);
            for(int i = 0; i < keys.Count; i++) {
                if(keys[i].frame == frame) {
                    rkeys.Add(keys[i]);
                    keys.RemoveAt(i);
                }
            }
            return rkeys.ToArray();
        }

        public void deleteDuplicateKeys() {
            sortKeys();
            int lastKey = -1;
            for(int i = 0; i < keys.Count; i++) {
                if(keys[i].frame == lastKey) {
                    keys[i].destroy();
                    keys.RemoveAt(i);
                    i--;
                }
                else {
                    lastKey = keys[i].frame;
                }
            }
        }

        public Key[] removeDuplicateKeys() {
            List<Key> dkeys = new List<Key>();

            sortKeys();
            int lastKey = -1;
            for(int i = 0; i < keys.Count; i++) {
                if(keys[i].frame == lastKey) {
                    dkeys.Add(keys[i]);
                    keys.RemoveAt(i);
                    i--;
                }
                else {
                    lastKey = keys[i].frame;
                }
            }

            return dkeys.ToArray();
        }

        public void deleteAllKeys() {
            foreach(Key key in keys) {
                key.destroy();
            }
            keys = new List<Key>();
        }

        public void deleteKeysAfter(int frame) {

            for(int i = 0; i < keys.Count; i++) {
                if(keys[i].frame > frame) {
                    keys[i].destroy();
                    keys.RemoveAt(i);
                    i--;
                }
            }
        }

        public Key[] removeKeysAfter(int frame) {
            List<Key> dkeys = new List<Key>();
            for(int i = 0; i < keys.Count; i++) {
                if(keys[i].frame > frame) {
                    dkeys.Add(keys[i]);
                    keys.RemoveAt(i);
                    i--;
                }
            }
            return dkeys.ToArray();
        }

        public void destroy() {
            // destroy keys
            if(keys != null) {
                foreach(Key key in keys) {
                    if(key != null)
                        key.destroy();
                }

                keys.Clear();
            }
        }

        public virtual List<GameObject> getDependencies(ITarget target) {
            return new List<GameObject>();
        }

        public virtual List<GameObject> updateDependencies(ITarget target, List<GameObject> newReferences, List<GameObject> oldReferences) {
            return new List<GameObject>();
        }

        public void offsetKeysFromBy(ITarget target, int frame, int amount) {
            if(keys.Count <= 0) return;
            for(int i = 0; i < keys.Count; i++) {
                if(frame <= 0 || keys[i].frame >= frame) keys[i].frame += amount;
            }
            updateCache(target);
        }

        // returns the offset
        public int shiftOutOfBoundsKeys(ITarget target) {
            if(keys.Count <= 0) return 0;
            sortKeys();
            if(keys[0].frame >= 1) return 0;
            int offset = 0;
            offset = Mathf.Abs(keys[0].frame) + 1; // calculate shift: -1 = 1+1 etc
            foreach(Key key in keys) {
                key.frame += offset;
            }
            updateCache(target);
            return offset;
        }

        // get action for frame from cache
        public Key getKeyContainingFrame(int frame) {
            for(int i = keys.Count - 1; i >= 0; i--) {
                if(frame >= keys[i].frame) return keys[i];
            }
            if(keys.Count > 0) return keys[0];  // return first if not greater than any action
            Debug.LogError("Animator: No key found for frame " + frame);
            return null;
        }

        // get action for frame from cache
        public Key getKeyForFrame(int startFrame) {
            foreach(Key key in keys) {
                if(key.frame == startFrame) return key;
            }
            Debug.LogError("Animator: No key found for frame " + startFrame);
            return null;
        }

        public int getKeyIndex(Key key) {
            return keys.IndexOf(key);
        }

        // get index of action for frame
        public int getKeyIndexForFrame(int startFrame) {
            for(int i = 0; i < keys.Count; i++) {
                if(keys[i].frame == startFrame) return i;
            }
            return -1;
        }

        // if whole take is true, when end frame is reached the last keyframe will be returned. This is used for the playback controls
        public int getKeyFrameAfterFrame(int frame, bool wholeTake = true) {
            foreach(Key key in keys) {
                if(key.frame > frame) return key.frame;
            }
            if(!wholeTake) return -1;
            if(keys.Count > 0) return keys[0].frame;
            Debug.LogError("Animator: No key found after frame " + frame);
            return -1;
        }

        // if whole take is true, when start frame is reached the last keyframe will be returned. This is used for the playback controls
        public int getKeyFrameBeforeFrame(int frame, bool wholeTake = true) {

            for(int i = keys.Count - 1; i >= 0; i--) {
                if(keys[i].frame < frame) return keys[i].frame;
            }
            if(!wholeTake) return -1;
            if(keys.Count > 0) return keys[keys.Count - 1].frame;
            Debug.LogError("Animator: No key found before frame " + frame);
            return -1;
        }

        public int getLastFrame(int frameRate) {
            if(keys.Count > 0) {
                int lastNumFrames = keys[keys.Count - 1].getNumberOfFrames(frameRate);
                if(lastNumFrames < 0)
                    return keys[keys.Count - 1].frame;
                return keys[keys.Count - 1].frame + lastNumFrames;
            }
            return 0;
        }

        public Key[] getKeyFramesInBetween(int startFrame, int endFrame) {
            List<Key> lsKeys = new List<Key>();
            if(startFrame <= 0 || endFrame <= 0 || startFrame >= endFrame || !hasKeyOnFrame(startFrame) || !hasKeyOnFrame(endFrame)) return lsKeys.ToArray();
            sortKeys();
            foreach(Key key in keys) {
                if(key.frame >= endFrame) break;
                if(key.frame > startFrame) lsKeys.Add(key);
            }
            return lsKeys.ToArray();
        }

        public float[] getKeyFrameRatiosInBetween(int startFrame, int endFrame) {
            List<float> lsKeyRatios = new List<float>();
            if(startFrame <= 0 || endFrame <= 0 || startFrame >= endFrame || !hasKeyOnFrame(startFrame) || !hasKeyOnFrame(endFrame)) return lsKeyRatios.ToArray();
            sortKeys();
            foreach(Key key in keys) {
                if(key.frame >= endFrame) break;
                if(key.frame > startFrame) lsKeyRatios.Add((float)(key.frame - startFrame) / (float)(endFrame - startFrame));
            }
            return lsKeyRatios.ToArray();
        }

        protected virtual void DoCopy(Track track) {
        }

        public void CopyTo(Track track) {
            track.id = id;
            track.name = name;
            track._targetPath = _targetPath;
            DoCopy(track);
        }
    }
}