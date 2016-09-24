using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace M8.Animator {
	public class AnimatorDataEdit {
	    public static void SetDirtyKeys(AMTrack track) {
	        foreach(AMKey key in track.keys)
	            EditorUtility.SetDirty(key);
	    }

	    public static void RecordUndoTrackAndKeys(AMTrack track, bool complete, string label) {
	        if(complete) {
	            Undo.RegisterCompleteObjectUndo(track, label);
	            Undo.RegisterCompleteObjectUndo(track.keys.ToArray(), label);
	        }
	        else {
	            Undo.RecordObject(track, label);
	            Undo.RecordObjects(track.keys.ToArray(), label);
	        }
	    }

	    public static void SetDirtyTracks(AMTakeData take) {
	        foreach(AMTrack track in take.trackValues) {
	            EditorUtility.SetDirty(track);
	        }
	    }

	    public static MonoBehaviour[] GetKeysAndTracks(AMTakeData take) {
	        List<MonoBehaviour> behaviours = new List<MonoBehaviour>();

	        if(take.trackValues != null) {
	            foreach(AMTrack track in take.trackValues) {
	                if(track.keys != null) {
	                    foreach(AMKey key in track.keys)
	                        behaviours.Add(key);
	                }

	                behaviours.Add(track);
	            }
	        }

	        return behaviours.ToArray();
	    }

	#if UNITY_5
	    [DrawGizmo(GizmoType.Active | GizmoType.NotInSelectionHierarchy | GizmoType.InSelectionHierarchy)]
	#else
	    [DrawGizmo(GizmoType.Active | GizmoType.NotSelected | GizmoType.SelectedOrChild)]
	#endif
	    static void DrawGizmos(AnimatorData aData, GizmoType gizmoType) {
	        //check if it's the one opened
	        if(AMTimeline.window != null && AMTimeline.window.aData != null && AMTimeline.window.aData.IsDataMatch(aData)) {
	            AnimatorDataEdit eData = AMTimeline.AnimEdit(aData);

	            List<AMTakeData> _t = eData.takes;

	            if(_t == null || _t.Count == 0) return;
	            if(eData.currentTakeInd < 0) {
	                eData.currentTakeInd = 0;
	            }
	            else if(eData.currentTakeInd >= _t.Count)
	                eData.currentTakeInd = _t.Count - 1;

	            _t[eData.currentTakeInd].drawGizmos(eData.target, AnimatorTimeline.e_gizmoSize, Application.isPlaying);
	        }
	    }

	    public float zoom = 0.4f;
	    public float width_track = 150f;
	    public bool isInspectorOpen = false;
	    public bool autoKey = false;

	    private int mCurrentTakeInd;
	    private int mPrevTakeInd;

	    private AnimatorData mData;
	    private AMITarget mDataTarget;
	    private AMIMeta mMetaHolder;

	    public string name {
	        get { return mData.name; }
	    }

	    public Transform transform {
	        get { return mData.transform; }
	    }

	    public AMTakeData currentTake {
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

	    public AMTakeData prevTake {
	        get {
	            if(mPrevTakeInd != -1 && mPrevTakeInd < takes.Count) {
	                return takes[mPrevTakeInd];
	            }
	            return null;
	        }
	    }

	    public bool currentTakeIsPlayStart {
	        get {
	            AMTakeData t = currentTake;
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

	    public AnimatorMeta meta {
	        get { return mMetaHolder.meta; }
	    }

	    public bool metaCanInstantiatePrefab {
	        get {
	            if(meta) {
	                if(UnityEditor.PrefabUtility.GetPrefabType(meta) == UnityEditor.PrefabType.Prefab) {
	                    GameObject go = UnityEditor.PrefabUtility.FindRootGameObjectWithSameParentPrefab(meta.gameObject);
	                    return go && go.GetComponent<AnimatorMeta>() != null;
	                }
	            }
	            return false;
	        }
	    }

	    public bool metaCanSavePrefabInstance {
	        get {
	            if(meta && UnityEditor.PrefabUtility.GetPrefabType(meta) == UnityEditor.PrefabType.PrefabInstance) {
	                GameObject go = UnityEditor.PrefabUtility.FindRootGameObjectWithSameParentPrefab(meta.gameObject);
	                return go && go.GetComponent<AnimatorMeta>() != null;
	            }
	            return false;
	        }
	    }
	        
	    public AMITarget target { get { return mDataTarget; } }
	    public List<AMTakeData> takes { get { return mDataTarget.takes; } }
	    public bool isGlobal { get { return mData.isGlobal; } set { mData.isGlobal = value; } }
	    public int codeLanguage { get { return mData.codeLanguage; } }

	    public bool isValid { get { return mData != null; } }

	    public AnimatorDataEdit(AnimatorData aData) {
	        SetData(aData);
	    }

	    public void SetData(AnimatorData aData) {
	        if(mData != aData) {
	            mData = aData;
	            mDataTarget = aData as AMITarget;
	            mMetaHolder = aData as AMIMeta;

	            mCurrentTakeInd = mPrevTakeInd = 0;
	        }
	    }

	    public bool IsDataMatch(AnimatorData aData) {
	        return aData == mData;
	    }

	    public void RegisterUndo(string label, bool complete) {
	        if(complete)
	            Undo.RegisterCompleteObjectUndo(mData, label);
	        else
	            Undo.RecordObject(mData, label);
	    }

	    public void SetDirty() {
	        EditorUtility.SetDirty(mData);
	    }

	    public void SetDirtyTakes() {
	        if(meta)
	            UnityEditor.EditorUtility.SetDirty(meta);
	        else
	            UnityEditor.EditorUtility.SetDirty(mData);
	    }

	    public void RegisterTakesUndo(string label, bool complete) {
	        UnityEngine.Object obj = meta ? (UnityEngine.Object)meta : (UnityEngine.Object)mData;

	        if(complete)
	            UnityEditor.Undo.RegisterCompleteObjectUndo(obj, label);
	        else
	            UnityEditor.Undo.RecordObject(obj, label);
	    }

	    public void Refresh() {
	        if(takes == null || takes.Count == 0) {
	            //this is a newly created data

	            // add take
	            AddNewTake();

	            mCurrentTakeInd = 0;

	            // save data
	            SetDirty();
	        }
	        else {
	            foreach(AMTakeData take in takes) {
	                foreach(AMTrack track in take.trackValues)
	                    track.updateCache(mDataTarget);
	            }
	        }
	    }

	    public void RefreshTakes() {
	        bool hasChanges = false;
	        if(takes != null) {
	            foreach(AMTakeData take in takes) {
	                if(take != null) {
	                    if(take.maintainCaches(mDataTarget)) {
	                        SetDirtyTracks(take);
	                        hasChanges = true;
	                    }
	                }
	            }
	        }

	        if(hasChanges)
	            SetDirty();
	    }
	    
	    public AMTakeData AddNewTake() {
	        string name = "Take" + (takes.Count + 1);
	        AMTakeData a = new AMTakeData();
	        // set defaults
	        a.name = name;
	        MakeTakeNameUnique(a);

	        takes.Add(a);

	        return a;
	    }

	    public string[] GetTakeNames(bool includeNone) {
	        List<string> names;

	        //sync names
	        if(includeNone) {
	            names = new List<string>(takes.Count+1);
	            names.Add("(None)");
	        }
	        else
	            names = new List<string>(takes.Count);

	        for(int i = 0; i < takes.Count; i++)
	            names.Add(takes[i].name);

	        return names.ToArray();
	    }

	    public AMTakeData GetTake(string takeName) {
	        int ind = mData.GetTakeIndex(takeName);
	        return ind != -1 ? takes[ind] : takes[ind];
	    }

	    public int GetTakeIndex(AMTakeData take) {
	        List<AMTakeData> _t = takes;
	        for(int i = 0; i < _t.Count; i++) {
	            if(_t[i] == take)
	                return i;
	        }
	        return -1;
	    }

	    /// <summary>
	    /// This will only duplicate the tracks and groups, includeKeys=true to also duplicate keys
	    /// </summary>
	    /// <param name="take"></param>
	    public void DuplicateTake(AMTakeData dupTake, bool includeKeys, bool addCompUndo) {
	        AMTakeData a = new AMTakeData();

	        a.name = dupTake.name;
	        MakeTakeNameUnique(a);
	        a.numLoop = dupTake.numLoop;
	        a.loopMode = dupTake.loopMode;
	        a.frameRate = dupTake.frameRate;
	        a.endFramePadding = dupTake.endFramePadding;
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
	                GameObject holderGO = (this as AMITarget).holder.gameObject;
	                AMTrack dupTrack = (addCompUndo ? UnityEditor.Undo.AddComponent(holderGO, track.GetType()) : holderGO.AddComponent(track.GetType())) as AMTrack;
	                dupTrack.enabled = false;
	                track.CopyTo(dupTrack);
	                a.trackValues.Add(dupTrack);

	                dupTrack.maintainTrack(mDataTarget);

	                Object tgtObj = dupTrack.GetTarget(mDataTarget);

	                //if there's no target, then we can't add the keys for events and properties
	                if(includeKeys && !(tgtObj == null && (dupTrack is AMPropertyTrack || dupTrack is AMEventTrack))) {
	                    foreach(AMKey key in track.keys) {
	                        AMKey dupKey = (addCompUndo ? UnityEditor.Undo.AddComponent(holderGO, key.GetType()) : holderGO.AddComponent(key.GetType())) as AMKey;
	                        if(dupKey) {
	                            key.CopyTo(dupKey);
	                            dupKey.enabled = false;
	                            dupKey.maintainKey(mDataTarget, tgtObj);
	                            dupTrack.keys.Add(dupKey);
	                        }
	                    }

	                    dupTrack.updateCache(mDataTarget);
	                }
	            }
	        }

	        takes.Add(a);
	    }

	    public void MakeTakeNameUnique(AMTakeData take) {
	        bool loop = false;
	        int count = 0;
	        do {
	            if(loop) loop = false;
	            foreach(AMTakeData _take in takes) {
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

	    public void MetaSaveInstantiate() {
	        if(meta && UnityEditor.PrefabUtility.GetPrefabType(meta) == UnityEditor.PrefabType.PrefabInstance) {
	            GameObject instanceGO = meta.gameObject;
	            GameObject prefab = UnityEditor.PrefabUtility.GetPrefabParent(instanceGO) as GameObject;
	            if(prefab) {
	                UnityEditor.Undo.RegisterCompleteObjectUndo(mData, "Save Prefab");

	                UnityEditor.PrefabUtility.ReplacePrefab(instanceGO, prefab);
	                mMetaHolder.meta = prefab.GetComponent<AnimatorMeta>();

	                UnityEditor.Undo.DestroyObjectImmediate(instanceGO);
	            }
	        }
	    }

	    /// <summary>
	    /// For editing the animator meta
	    /// </summary>
	    public bool MetaInstantiatePrefab(string undoLabel) {
	        if(metaCanInstantiatePrefab) {
	            //Debug.Log("instantiating");
	            GameObject go = UnityEditor.PrefabUtility.InstantiatePrefab(meta.gameObject) as GameObject;
	            UnityEditor.Undo.RegisterCreatedObjectUndo(go, undoLabel);
	            UnityEditor.Undo.SetTransformParent(go.transform, mData.transform, undoLabel);
	            UnityEditor.Undo.RegisterCompleteObjectUndo(mData, undoLabel);
	            mMetaHolder.meta = go.GetComponent<AnimatorMeta>();
	            return true;
	        }
	        return false;
	    }

	    /// <summary>
	    /// if copyTakes is true, overrides all takes in newMeta (if null, then to our dataholder) with current data
	    /// </summary>
	    public void MetaSet(AnimatorMeta newMeta, bool copyTakes) {
	        if(mMetaHolder.meta != newMeta) {
	            AnimatorMeta prevMeta = mMetaHolder.meta;
	            List<AMTakeData> prevTakes = new List<AMTakeData>(takes); //preserve the list of takes from previous
	            string prevPlayOnStartName = mData.defaultTakeName;

	            mMetaHolder.meta = newMeta; //this will clear out internal takes list

	            if(mMetaHolder.meta) {
	                if(copyTakes) { //duplicate takes to the new meta
	                    mMetaHolder.meta.takes.Clear();

	                    foreach(AMTakeData take in prevTakes)
	                        DuplicateTake(take, true, true);
	                }

	                //clear out non-meta stuff
	                if(mMetaHolder.dataHolder) {
	                    UnityEditor.Undo.DestroyObjectImmediate(mMetaHolder.dataHolder);
	                    mMetaHolder.dataHolder = null;
	                }
	            }
	            else {
	                //create data holder
	#if MATE_DEBUG_ANIMATOR
	                mMetaHolder.dataHolder = new GameObject("_animdata", typeof(AnimatorDataHolder));
	#else
	                mMetaHolder.dataHolder = new GameObject("_animdata");
	#endif
	                mMetaHolder.dataHolder.transform.parent = mData.transform;

	                UnityEditor.Undo.RegisterCreatedObjectUndo(mMetaHolder.dataHolder, "Set Meta");

	                if(copyTakes) { //duplicate meta to takeData
	                    foreach(AMTakeData take in prevTakes)
	                        DuplicateTake(take, true, false);
	                }
	            }

	            if(takes == null || takes.Count == 0) { //add at least one take
	                AddNewTake();
	            }

	            //get new play on start
	            if(!string.IsNullOrEmpty(prevPlayOnStartName)) {
	                string newPlayOnStart = "";
	                foreach(AMTakeData take in takes) {
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

	            //destroy previous meta if it is not prefab
	            if(prevMeta && UnityEditor.PrefabUtility.GetPrefabType(prevMeta) != UnityEditor.PrefabType.Prefab) {
	                UnityEditor.Undo.DestroyObjectImmediate(prevMeta.gameObject);
	            }
	        }
	    }
	        
	    public AMTakeData GetTakeData(string takeName) {
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
	            foreach(AMTakeData take in takes) {
	                if(take.name == prevDefaultTakeName) {
	                    newPlayOnStart = take.name;
	                    break;
	                }
	            }

	            mData.defaultTakeName = newPlayOnStart;
	        }
	    }

	    public string[] GetTakeNames() {
	        List<AMTakeData> _t = takes;
	        string[] names = new string[_t.Count + 2];
	        for(int i = 0; i < _t.Count; i++) {
	            names[i] = _t[i].name;
	        }
	        names[names.Length - 2] = "Create new...";
	        names[names.Length - 1] = "Duplicate current...";
	        return names;
	    }

	    public bool SetCodeLanguage(int codeLanguage) {
	        if(mData.codeLanguage != codeLanguage) {
	            mData.codeLanguage = codeLanguage;
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

	    public void DeleteAllTakesExcept(AMTakeData take) {
	        List<AMTakeData> _t = takes;
	        for(int index = 0; index < _t.Count; index++) {
	            if(_t[index] == take) continue;
	            DeleteTake(index);
	            index--;
	        }
	    }

	    public void MergeWith(AnimatorDataEdit _aData) {
	        if(meta == null && _aData.meta == null) {
	            foreach(AMTakeData take in _aData.takes) {
	                takes.Add(take);
	                MakeTakeNameUnique(take);
	            }
	        }
	    }

	    public List<GameObject> GetDependencies(AMTakeData take = null) {
	        // if only one take
	        if(take != null) return take.getDependencies(mDataTarget);

	        // if all takes
	        List<GameObject> ls = new List<GameObject>();
	        foreach(AMTakeData t in takes) {
	            ls = ls.Union(t.getDependencies(mDataTarget)).ToList();
	        }
	        return ls;
	    }

	    public List<GameObject> UpdateDependencies(List<GameObject> newReferences, List<GameObject> oldReferences) {
	        List<GameObject> lsFlagToKeep = new List<GameObject>();
	        foreach(AMTakeData take in takes) {
	            lsFlagToKeep = lsFlagToKeep.Union(take.updateDependencies(mDataTarget, newReferences, oldReferences)).ToList();
	        }
	        return lsFlagToKeep;
	    }
	}
}