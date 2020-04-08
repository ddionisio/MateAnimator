using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace M8.Animator.Edit {
    public class AnimateEditControl {
        [DrawGizmo(GizmoType.Active | GizmoType.NotInSelectionHierarchy | GizmoType.InSelectionHierarchy)]
        static void DrawGizmos(Animate aData, GizmoType gizmoType) {
            //check if it's the one opened
            if(TimelineWindow.window != null && TimelineWindow.window.aData != null && TimelineWindow.window.aData.IsDataMatch(aData)) {
                AnimateEditControl eData = TimelineWindow.AnimEdit(aData);

                List<Take> _t = eData.takes;

                if(_t == null || _t.Count == 0) return;
                if(eData.currentTakeInd < 0) {
                    eData.currentTakeInd = 0;
                }
                else if(eData.currentTakeInd >= _t.Count)
                    eData.currentTakeInd = _t.Count - 1;

                _t[eData.currentTakeInd].drawGizmos(eData.target, AnimateTimeline.e_gizmoSize, Application.isPlaying);
            }
        }

        public float zoom = 0.4f;
        public float width_track = 150f;
        public bool isInspectorOpen = false;
        public bool autoKey = false;

        private int mCurrentTakeInd;
        private int mPrevTakeInd;

        private Animate mData;
        private ITarget mDataTarget;
        
        public string name {
            get { return mData.name; }
        }

        public Transform transform {
            get { return mData.transform; }
        }

        public Take currentTake {
            get {
                if(mCurrentTakeInd == -1 || mCurrentTakeInd >= takes.Count) mCurrentTakeInd = 0;
                return takes[mCurrentTakeInd];
            }
        }

        public int currentTakeInd {
            get { return mCurrentTakeInd; }
            set {
                if(mCurrentTakeInd != value) {
                    if(mCurrentTakeInd < takes.Count)
                        mPrevTakeInd = mCurrentTakeInd;
                    mCurrentTakeInd = value;
                }
            }
        }

        public Take prevTake {
            get {
                if(mPrevTakeInd != -1 && mPrevTakeInd < takes.Count) {
                    return takes[mPrevTakeInd];
                }
                return null;
            }
        }

        public bool currentTakeIsPlayStart {
            get {
                Take t = currentTake;
                return t != null && t.name == mData.defaultTakeName;
            }
        }

        public string defaultTakeName {
            get { return mData.defaultTakeName; }
            set { mData.defaultTakeName = value; }
        }

        public GameObject gameObject {
            get { return mData ? mData.gameObject : null; }
        }

        public AnimateMeta meta {
            get { return mDataTarget.meta; }
        }

        public ITarget target { get { return mDataTarget; } }
        public List<Take> takes { get { return mDataTarget.takes; } }
        public bool isGlobal { get { return mData.isGlobal; } set { mData.isGlobal = value; } }

        public bool isValid { get { return mData != null; } }

        public bool isPartOfPrefab { get { return PrefabUtility.IsPartOfAnyPrefab(mData); } }

        public AnimateEditControl(Animate aData) {
            SetData(aData);
        }

        public void SetData(Animate aData) {
            if(mData != aData) {
                mData = aData;
                mDataTarget = aData as ITarget;

                mCurrentTakeInd = mPrevTakeInd = 0;
            }
        }

        public void ClearEditCache() {
            if(mDataTarget == null)
                return;

            foreach(var take in takes)
                take.ClearEditCache();
        }

        public bool IsDataMatch(Animate aData) {
            return aData == mData;
        }

        /// <summary>
        /// Preivew frame from current take.
        /// </summary>
        public void PreviewFrame(float _frame, bool orientationOnly = false, bool renderStill = true, bool play = false, float playSpeed = 1.0f) {
            currentTake.previewFrame(target, _frame, orientationOnly, renderStill, play, playSpeed);
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); //TODO: figure out what repaints game view
        }

        public void RegisterUndo(string label, bool complete) {
            if(complete)
                Undo.RegisterCompleteObjectUndo(mData, label);
            else
                Undo.RecordObject(mData, label);
        }

        public void RegisterTakesUndo(string label) {
            if(meta) {
                UnityEditor.Undo.RegisterCompleteObjectUndo(meta, label);

                UnityEditor.EditorUtility.SetDirty(meta);
            }
            else {
                UnityEditor.Undo.RegisterCompleteObjectUndo(mData, label);
            }
        }

        public void RecordTakesChanged() {            
            if(meta) { //no need to apply anything

            }
            else if(isPartOfPrefab) { //tell prefab that changes are made
                PrefabUtility.RecordPrefabInstancePropertyModifications(mData);
            }
        }

        public void SetTakesDirty() {
            if(meta)
                UnityEditor.EditorUtility.SetDirty(meta);
            else
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }

        public void Refresh() {
            bool hasChanges = false;
            if(takes == null || takes.Count == 0) {
                //this is a newly created data

                // add take
                AddNewTake();

                mCurrentTakeInd = 0;
                hasChanges = true;
            }
            else {
                foreach(Take take in takes) {
                    if(take != null) {
                        if(take.maintainCaches(mDataTarget))
                            hasChanges = true;
                    }
                }
            }

            if(hasChanges)
                SetTakesDirty(); //don't really want to undo this
        }

        public Take AddNewTake() {
            string name = "Take" + (takes.Count + 1);
            Take a = new Take();
            // set defaults
            a.name = name;
            MakeTakeNameUnique(a);

            a.frameRate = OptionsFile.instance.framesPerSecondDefault;

            takes.Add(a);

            return a;
        }

        public string[] GetTakeNames(bool includeNone) {
            List<string> names;

            //sync names
            if(includeNone) {
                names = new List<string>(takes.Count + 1);
                names.Add("(None)");
            }
            else
                names = new List<string>(takes.Count);

            for(int i = 0; i < takes.Count; i++)
                names.Add(takes[i].name);

            return names.ToArray();
        }

        public Take GetTake(string takeName) {
            int ind = mData.GetTakeIndex(takeName);
            return ind != -1 ? takes[ind] : takes[ind];
        }

        public int GetTakeIndex(Take take) {
            List<Take> _t = takes;
            for(int i = 0; i < _t.Count; i++) {
                if(_t[i] == take)
                    return i;
            }
            return -1;
        }

        public Track DuplicateTrack(Track srcTrack, bool includeKeys) {
            var dupTrack = SerializeData.CreateTrack(srcTrack.serializeType);
            srcTrack.CopyTo(dupTrack);

            dupTrack.maintainTrack(mDataTarget);

            Object tgtObj = dupTrack.GetTarget(mDataTarget);

            //if there's no target, then we can't add the keys for events and properties
            if(includeKeys && !(tgtObj == null && (dupTrack is PropertyTrack || dupTrack is EventTrack))) {
                foreach(var key in srcTrack.keys) {
                    var dupKey = SerializeData.CreateKey(key.serializeType);
                    if(dupKey != null) {
                        key.CopyTo(dupKey);
                        dupKey.maintainKey(mDataTarget, tgtObj);
                        dupTrack.keys.Add(dupKey);
                    }
                }

                dupTrack.updateCache(mDataTarget);
            }

            return dupTrack;
        }

        /// <summary>
        /// This will only duplicate the tracks and groups, includeKeys=true to also duplicate keys
        /// </summary>
        /// <param name="take"></param>
        public void DuplicateTake(Take dupTake, bool includeKeys) {
            Take a = new Take();

            a.name = dupTake.name;
            MakeTakeNameUnique(a);
            a.numLoop = dupTake.numLoop;
            a.loopMode = dupTake.loopMode;
            a.frameRate = dupTake.frameRate;
            a.endFramePadding = dupTake.endFramePadding;
            //a.lsTracks = new List<Track>();
            //a.dictTracks = new Dictionary<int,Track>();

            if(dupTake.rootGroup != null) {
                a.rootGroup = dupTake.rootGroup.duplicate();
            }
            else {
                a.initGroups();
            }

            a.groupCounter = dupTake.groupCounter;

            if(dupTake.groupValues != null) {
                a.groupValues = new List<Group>();
                foreach(var grp in dupTake.groupValues) {
                    a.groupValues.Add(grp.duplicate());
                }
            }

            a.trackCounter = dupTake.trackCounter;
            
            a.trackValues = new List<Track>();
            foreach(var track in dupTake.trackValues) {
                var dupTrack = DuplicateTrack(track, includeKeys);
                a.trackValues.Add(dupTrack);
            }

            takes.Add(a);
        }

        public void MakeTakeNameUnique(Take take) {
            bool loop = false;
            int count = 0;
            do {
                if(loop) loop = false;
                foreach(var _take in takes) {
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

        /// <summary>
        /// if copyTakes is true, overrides all takes in newMeta (if null, then to our dataholder) with current data
        /// </summary>
        public void MetaSet(AnimateMeta newMeta, bool copyTakes) {
            if(mDataTarget.meta != newMeta) {
                RegisterUndo("Meta Set", true);

                AnimateMeta prevMeta = mDataTarget.meta;
                List<Take> prevTakes = new List<Take>(takes); //preserve the list of takes from previous
                string prevPlayOnStartName = mData.defaultTakeName;

                mDataTarget.meta = newMeta; //this will clear out internal takes list

                if(mDataTarget.meta) {
                    if(copyTakes) { //duplicate takes to the new meta
                        RegisterTakesUndo("Meta Duplicate Takes");

                        mDataTarget.meta.takes.Clear();

                        foreach(Take take in prevTakes)
                            DuplicateTake(take, true);

                        RecordTakesChanged();
                    }
                }
                else {                    
                    if(copyTakes) { //duplicate meta to takeData
                        foreach(Take take in prevTakes)
                            DuplicateTake(take, true);
                    }
                }

                if(takes == null || takes.Count == 0) { //add at least one take
                    RegisterTakesUndo("Meta Add Empty Take");

                    AddNewTake();

                    RecordTakesChanged();
                }

                //get new play on start
                if(!string.IsNullOrEmpty(prevPlayOnStartName)) {
                    string newPlayOnStart = "";
                    foreach(Take take in takes) {
                        if(take.name == prevPlayOnStartName) {
                            newPlayOnStart = take.name;
                            break;
                        }
                    }

                    mData.defaultTakeName = newPlayOnStart;
                }
                else
                    mData.defaultTakeName = "";
                //
            }
        }

        public Take GetTakeData(string takeName) {
            int ind = mData.GetTakeIndex(takeName);
            if(ind == -1) {
                Debug.LogError("Animator: Take '" + takeName + "' not found.");
                return null;
            }

            return takes[ind];
        }

        public void DeleteTake(int index) {
            string prevDefaultTakeName = mData.defaultTakeName;
            //if(shouldCheckDependencies) shouldCheckDependencies = false;

            //TODO: destroy tracks, keys
            //_takes[index].destroy();
            takes.RemoveAt(index);
            if((mCurrentTakeInd >= index) && (mCurrentTakeInd > 0)) mCurrentTakeInd--;

            if(!string.IsNullOrEmpty(prevDefaultTakeName)) {
                string newPlayOnStart = "";
                foreach(Take take in takes) {
                    if(take.name == prevDefaultTakeName) {
                        newPlayOnStart = take.name;
                        break;
                    }
                }

                mData.defaultTakeName = newPlayOnStart;
            }
        }

        public string[] GetTakeNames() {
            List<Take> _t = takes;
            string[] names = new string[_t.Count + 2];
            for(int i = 0; i < _t.Count; i++) {
                names[i] = _t[i].name;
            }
            names[names.Length - 2] = "Create new...";
            names[names.Length - 1] = "Duplicate current...";
            return names;
        }
        
        /*public bool setShowWarningForLostReferences(bool showWarningForLostReferences) {
	        if(this.showWarningForLostReferences != showWarningForLostReferences) {
	            this.showWarningForLostReferences = showWarningForLostReferences;
	            return true;
	        }
	        return false;
	    }*/

        public void DeleteAllTakesExcept(Take take) {
            List<Take> _t = takes;
            for(int index = 0; index < _t.Count; index++) {
                if(_t[index] == take) continue;
                DeleteTake(index);
                index--;
            }
        }

        public void MergeWith(AnimateEditControl _aData) {
            if(meta == null && _aData.meta == null) {
                foreach(var take in _aData.takes) {
                    takes.Add(take);
                    MakeTakeNameUnique(take);
                }
            }
        }

        public List<GameObject> GetDependencies(Take take = null) {
            // if only one take
            if(take != null) return take.getDependencies(mDataTarget);

            // if all takes
            List<GameObject> ls = new List<GameObject>();
            foreach(Take t in takes) {
                ls = ls.Union(t.getDependencies(mDataTarget)).ToList();
            }
            return ls;
        }

        public List<GameObject> UpdateDependencies(List<GameObject> newReferences, List<GameObject> oldReferences) {
            List<GameObject> lsFlagToKeep = new List<GameObject>();
            foreach(Take take in takes) {
                lsFlagToKeep = lsFlagToKeep.Union(take.updateDependencies(mDataTarget, newReferences, oldReferences)).ToList();
            }
            return lsFlagToKeep;
        }
    }
}