using UnityEngine;
//using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Holoville.HOTween;

[System.Serializable]
public class AMTakeData {
    public delegate void OnSequenceDone(AMTakeData take);

    #region Declarations
    public string name;					// take name
    public int frameRate = 24;				// frames per second
    public int numFrames = 1440;			// number of frames
    public float startFrame = 1f;				// first frame to render
    public float endFrame = 100f;
    public int playbackSpeedIndex = 2;		// default playback speed x1

    public int numLoop = 1; //number of times this plays before it is done
    public LoopType loopMode = LoopType.Restart;
    public int loopBackToFrame = -1; //set this to loop back at given frame when sequence is complete, make sure numLoop = 1 and loopMode is Restart

    public List<AMTrack> trackValues = new List<AMTrack>();

    public int track_count = 1;		// number of tracks created, used to generate unique track ids
    public int group_count = 0;		// number of groups created, used to generate unique group ids. negative number to differentiate from positive track ids

    public AMGroup rootGroup;
    public List<AMGroup> groupValues = new List<AMGroup>();

    [System.NonSerialized]
    public int selectedFrame = 1;

    public static bool isProLicense = true;

    private bool mTracksSorted = false;

    private AMCameraSwitcherTrack mCameraSwitcher;

    public AMCameraSwitcherTrack cameraSwitcher {
        get {
            if(!mCameraSwitcher) {
                for(int i = 0; i < trackValues.Count; i++) {
                    if(trackValues[i] is AMCameraSwitcherTrack) {
                        mCameraSwitcher = trackValues[i] as AMCameraSwitcherTrack;
                        break;
                    }
                }
            }
            return mCameraSwitcher;
        }
    }

    #endregion

    //Only used by editor
    public void RevertToDefault() {
        trackValues.Clear();
        groupValues.Clear();

        rootGroup = null;
        initGroups();
        name = "Take 1";
        frameRate = 24;
        numFrames = 1440;
        startFrame = 1f;
        endFrame = 100f;
        playbackSpeedIndex = 2;

        numLoop = 1;
        loopMode = LoopType.Restart;
        loopBackToFrame = -1;

        track_count = 1;
        group_count = 0;
    }

    // Adding a new track type
    // =====================
    // Create track class
    // Make sure to add [System.Serializable] to every class
    // Set track class properties
    // Override getTrackType in track class
    // Add track type to showObjectFieldFor in AMTimeline
    // Create an addXTrack method here, and put it in AMTimeline
    // Create track key class, make sure to override CreateClone 
    // Create AMXAction class for track class that overrides execute and implements ToString # TO DO #
    // Override updateCache in track class
    // Create addKey method in track class, and put it in addKey in AMTimeline
    // Add track to timeline action in AMTimeline
    // Add inspector properties to showInspectorPropertiesFor in AMTimeline
    // Override previewFrame method in track class
    // Add track object to timelineSelectObjectFor in AMTimeline (optional)
    // Override getDependencies and updateDependencies in track class
    // Add details to Code View # TO DO #


    #region Tracks

    // add translation track
    public void addTrack(int groupId, AMITarget target, Transform obj, AMTrack a) {
        a.setName(getTrackCount());
        a.id = getUniqueTrackID();
        a.enabled = false;
        a.SetTarget(target, obj);
        addTrack(groupId, a);
        if(a is AMCameraSwitcherTrack) mCameraSwitcher = a as AMCameraSwitcherTrack;
    }

    public void deleteTrack(int trackid, bool deleteFromGroup = true) {
        AMTrack track = getTrack(trackid);
        if(track)
            deleteTrack(track, deleteFromGroup);
    }

    public void deleteTrack(int trackid, bool deleteFromGroup, ref List<MonoBehaviour> modifiedItems) {
        AMTrack track = getTrack(trackid);
        if(track)
            deleteTrack(track, deleteFromGroup, ref modifiedItems);
    }

    public void deleteTrack(AMTrack track, bool deleteFromGroup = true) {
        int id = track.id;
        int index = getTrackIndex(id);
        if(track) {
            if(mCameraSwitcher == track) mCameraSwitcher = null;
            track.destroy();
        }

        trackValues.RemoveAt(index);
        if(deleteFromGroup) deleteTrackFromGroups(id);
    }

    private void deleteTrack(AMTrack track, bool deleteFromGroup, ref List<MonoBehaviour> modifiedItems) {
        int id = track.id;
        int index = getTrackIndex(id);
        if(track && modifiedItems != null) {
            foreach(AMKey key in track.keys)
                modifiedItems.Add(key);

            modifiedItems.Add(track);
        }
        if(mCameraSwitcher == track) mCameraSwitcher = null;
        trackValues.RemoveAt(index);
        if(deleteFromGroup) deleteTrackFromGroups(id);
    }

    // get track ids from range, exclusive
    public List<int> getTrackIDsForRange(int start_id, int end_id) {
        if(start_id == end_id) return new List<int>();
        int loc1 = getElementLocationIndex(start_id);
        if(loc1 == 0) return new List<int>();	// id not found
        int loc2 = getElementLocationIndex(end_id);
        if(loc2 == 0) return new List<int>();	// id not found
        // if end_id is before start_id, switch position
        if(loc1 > loc2) {
            int temp = start_id;
            start_id = end_id;
            end_id = temp;
        }
        List<int> track_ids = new List<int>();
        bool foundStartID = false;
        bool foundEndID = false;
        track_ids = getTrackIDsForGroup(0, start_id, end_id, ref foundStartID, ref foundEndID);
        return track_ids;
    }

    private List<int> getTrackIDsForGroup(int group_id, int start_id, int end_id, ref bool foundStartID, ref bool foundEndID) {
        List<int> track_ids = new List<int>();
        AMGroup grp = getGroup(group_id);
        for(int i = 0; i < grp.elements.Count; i++) {
            if(grp.elements[i] == start_id) {
                foundStartID = true;
                if(grp.elements[i] > 0) continue;	// if track is start id, continue
            }
            else if(grp.elements[i] == end_id) {
                foundEndID = true;
            }
            if(!foundEndID) {
                if(grp.elements[i] > 0) {
                    if(foundStartID) track_ids.Add(grp.elements[i]);
                }
                else track_ids.AddRange(getTrackIDsForGroup(grp.elements[i], start_id, end_id, ref foundStartID, ref foundEndID));
            }
            if(foundEndID) break;

        }
        return track_ids;
    }

    public int getElementLocationIndex(int element_id) {
        int count = 0;
        bool found = false;
        getElementLocationIndexForGroup(0, element_id, ref count, ref found);
        return count;
    }

    private void getElementLocationIndexForGroup(int group_id, int element_id, ref int count, ref bool found) {
        AMGroup grp = getGroup(group_id);
        foreach(int id in grp.elements) {
            count++;
            if(id == element_id) {
                found = true;
                return;
            }
            if(id <= 0) {
                getElementLocationIndexForGroup(id, element_id, ref count, ref found);
                if(found) return;
            }
        }
    }

    #endregion
    #region Groups

    public void deleteGroup(int group_id, bool deleteContents, ref List<MonoBehaviour> modifiedItems) {
        if(group_id >= 0) return;
        AMGroup grp = getGroup(group_id);
        if(deleteContents) {
            // delete elements
            for(int i = 0; i < grp.elements.Count; i++) {
                if(grp.elements[i] > 0) deleteTrack(grp.elements[i], false, ref modifiedItems);
                else if(grp.elements[i] < 0) deleteGroup(grp.elements[i], deleteContents, ref modifiedItems);
            }
        }
        else {
            AMGroup elementGroup = getGroup(getElementGroup(group_id));
            for(int k = 0; k < elementGroup.elements.Count; k++) {
                if(elementGroup.elements[k] == group_id) {
                    // insert tracks in place of group
                    elementGroup.elements.InsertRange(k, grp.elements);
                    break;
                }
            }
        }

        removeFromGroup(grp.group_id);
        int j = 0;
        for(; j < groupValues.Count; j++) {
            if(groupValues[j] == grp) {
                groupValues.Remove(grp);
                break;
            }
        }

    }

    public int getUniqueTrackID() {

        track_count++;

        foreach(AMTrack track in trackValues)
            if(track.id >= track_count) track_count = track.id + 1;

        return track_count;
    }

    public int getUniqueGroupID() {

        group_count--;

        foreach(AMGroup grp in groupValues) {
            if(grp.group_id <= group_count) group_count = grp.group_id - 1;

        }
        return group_count;
    }

    public int getTrackIndex(int id) {
        int index = -1;
        for(int i = 0; i < trackValues.Count; i++) {
            if(trackValues[i].id == id) {
                index = i;
                break;
            }
        }
        return index;
    }

    public AMTrack getTrack(int id) {
        int index = getTrackIndex(id);

        if(index == -1 || index >= trackValues.Count) {
            Debug.LogError("Animator: Track id " + id + " not found.");
            return null;
        }
        return trackValues[index];
    }

    // get the id of the group the element is in
    public int getElementGroup(int id) {
        foreach(int _id in rootGroup.elements) {
            if(_id == id) return 0;
        }
        foreach(AMGroup grp in groupValues) {
            foreach(int __id in grp.elements) {
                if(__id == id) return grp.group_id;
            }
        }
        Debug.LogError("Animator: Group not found for element " + id);
        return 0;
    }

    // replace an element id with another element id. returns true if successful
    public bool replaceElement(int source_id, int new_id) {
        for(int i = 0; i < rootGroup.elements.Count; i++) {
            if(rootGroup.elements[i] == source_id) {
                rootGroup.elements[i] = new_id;
                return true;
            }
        }
        for(int j = 0; j < groupValues.Count; j++) {
            AMGroup grp = groupValues[j];
            for(int i = 0; i < grp.elements.Count; i++) {
                if(grp.elements[i] == source_id) {
                    grp.elements[i] = new_id;
                    return true;
                }
            }
        }
        return false;
    }

    private void addTrack(int groupId, AMTrack track) {
        trackValues.Add(track);
        addToGroup(track.id, groupId);
    }

    // move element to group, just after the destination track
    public void moveToGroup(int source_id, int dest_group_id, bool first = false, int dest_track_id = -1) {
        initGroups();
        bool shouldRemoveGroup = true;
        // if source is group
        if(source_id < 0) {
            // check if destination group is a child of source group
            int element_root_group = getElementRootGroup(dest_group_id, source_id);
            // if destination group is an indirect child of source group
            if(element_root_group < 1) {

                removeFromGroup(element_root_group);
                replaceElement(source_id, element_root_group);
                shouldRemoveGroup = false;	// should not remove group as it has been replaced

                // if destination group is a direct child of source group
            }
            else if(element_root_group == 2) {
                removeFromGroup(dest_group_id);
                replaceElement(source_id, dest_group_id);
                shouldRemoveGroup = false;
            }
        }

        if(shouldRemoveGroup) removeFromGroup(source_id);

        addToGroup(source_id, dest_group_id, first, dest_track_id);
        // if dest_group_id in source_id, do something
    }

    // returns true if element is in group. sets first_child_group_id to the top level group in group_id that contains id
    public bool isElementInGroup(int id, int group_id) {
        if(group_id > 0) {
            return false;
        }
        AMGroup grp = getGroup(group_id);
        foreach(int _id in grp.elements) {
            if(_id == id) {
                return true;
            }
            else if(isElementInGroup(id, _id)) {
                return true;
            }
        }
        return false;
    }

    // searches for the elements under group_id and returns the root group. returns 1 if element not found, returns 2 if root group is provided group_id
    public int getElementRootGroup(int element_id, int group_id) {
        if(group_id > 0) return 1;
        AMGroup grp = getGroup(group_id);
        foreach(int id in grp.elements) {
            if(id == element_id) return 2;
            if(id <= 0 && isElementInGroup(element_id, id)) {
                return id;
            }
        }
        return 1;
    }

    public void moveGroupElement(int source_id, int dest_id) {
        initGroups();
        // remove element
        removeFromGroup(source_id);
        // add element to position
        bool found = false;
        // search for destination id in root group
        for(int i = 0; i < rootGroup.elements.Count; i++) {
            if(rootGroup.elements[i] == dest_id) {
                if(i < rootGroup.elements.Count) rootGroup.elements.Insert(i + 1, source_id);
                else rootGroup.elements.Add(source_id);	// add to end if last
                found = true;
                break;
            }
        }
        // if not found, search rest of groups
        if(!found) {
            foreach(AMGroup grp in groupValues) {
                for(int i = 0; i < grp.elements.Count; i++) {
                    if(grp.elements[i] == dest_id) {
                        if(i < grp.elements.Count - 1) grp.elements.Insert(i + 1, source_id);
                        else grp.elements.Add(source_id);	// add to end if last
                        found = true;
                        break;
                    }
                }
                if(found) break;
            }
        }
        if(!found) {
            Debug.LogWarning("Animator: No group found for element id " + dest_id);
            rootGroup.elements.Add(source_id);
        }
    }

    // get the new index for a new track
    public int getTrackCount() {
        //return lsTracks.Count;
        return trackValues.Count;
    }

    public int getGroupIndex(int id) {
        int index = -1;
        for(int i = 0; i < groupValues.Count; i++) {
            if(groupValues[i].group_id == id) {
                index = i;
                break;
            }
        }
        return index;
    }
    public AMGroup getGroup(int id) {
        initGroups();
        if(id == 0) return rootGroup;
        int index = getGroupIndex(id);

        if(index == -1 || index >= groupValues.Count) {
            Debug.LogError("Animator: Group id " + id + " not found.");
            return new AMGroup();
        }
        return groupValues[index];
    }

    public void initGroups() {
        if(rootGroup == null) {
            AMGroup g = new AMGroup();
            g.init(0);
            rootGroup = g;
        }
    }

    public int getTrackGroup(int track_id) {
        foreach(int id in rootGroup.elements) {
            if(id == track_id) return 0;
        }
        foreach(AMGroup grp in groupValues) {
            foreach(int _id in grp.elements) {
                if(_id == track_id) return grp.group_id;
            }
        }
        Debug.LogWarning("Animator: No group found for Track " + track_id);
        return 0;
    }

    public void removeFromGroup(int source_id) {
        foreach(int id in rootGroup.elements) {
            if(id == source_id) {
                rootGroup.elements.Remove(id);
                return;
            }
        }
        foreach(AMGroup grp in groupValues) {
            foreach(int _id in grp.elements) {
                if(_id == source_id) {
                    grp.elements.Remove(_id);
                    return;
                }
            }
        }
    }

    public void addToGroup(int source_id, int group_id, bool first = false, int dest_track_id = -1) {
        initGroups();
        bool found = false;
        if(group_id == 0) {
            // put after track
            if(dest_track_id != -1) {
                // Count -1, do not check last element due to Insert constraints. Will automatically be added as last element in this case
                for(int i = 0; i < rootGroup.elements.Count - 1; i++) {
                    if(rootGroup.elements[i] == dest_track_id) {
                        rootGroup.elements.Insert(i + 1, source_id);
                        found = true;
                        break;
                    }
                }
            }
            if(!found) {
                if(first) rootGroup.elements.Insert(0, source_id);
                else rootGroup.elements.Add(source_id);
            }
        }
        else {
            int index = getGroupIndex(group_id);
            if(index == -1) {
                Debug.LogError("Animator: Group " + group_id + " not found.");
                return;
            }
            AMGroup grp = getGroup(group_id);
            if(dest_track_id != -1) {
                // Count -1, do not check last element due to Insert constraints. Will automatically be added as last element in this case
                for(int i = 0; i < grp.elements.Count; i++) {
                    if(grp.elements[i] == dest_track_id) {
                        if(i < grp.elements.Count - 1) grp.elements.Insert(i + 1, source_id);
                        else grp.elements.Add(source_id);
                        found = true;
                        break;
                    }
                }
            }
            if(!found) {
                if(first) grp.elements.Insert(0, source_id);
                else grp.elements.Add(source_id);
            }
            if(!grp.foldout) grp.foldout = true;
        }
    }

    public void deleteTrackFromGroups(int _id) {
        bool found = false;
        foreach(int id in rootGroup.elements) {
            if(id == _id) {
                rootGroup.elements.Remove(id);
                found = true;
                break;
            }
        }
        if(!found) {
            foreach(AMGroup grp in groupValues) {
                foreach(int id in grp.elements) {
                    if(id == _id) {
                        grp.elements.Remove(id);
                        found = true;
                        break;
                    }
                }
            }
        }
        if(!found) Debug.LogWarning("Animator: Deleted track " + _id + " not found in groups.");
    }
    #endregion
    #region Frames/Keys

    static int TrackCompare(AMTrack t1, AMTrack t2) {
        if(t1 == t2)
            return 0;
        else if(t1 == null)
            return 1;
        else if(t2 == null)
            return -1;

        return t1.order - t2.order;
    }

    void sortTracks() {
#if UNITY_EDITOR
        if(!Application.isPlaying)
            mTracksSorted = false;
#endif

        if(!mTracksSorted) {
            trackValues.Sort(TrackCompare);
            mTracksSorted = true;
        }
    }

    // preview a frame
    public void previewFrame(AMITarget itarget, float _frame, bool orientationOnly = false, bool renderStill = true) {
        sortTracks();

        // render camera switcher still texture if necessary
        if(renderStill) renderCameraSwitcherStill(itarget, _frame);

        if(orientationOnly) {
            foreach(AMTrack track in trackValues) {
                if(track is AMOrientationTrack || track is AMRotationTrack)
                    track.previewFrame(itarget, _frame, frameRate);
            }
        }
        else {
            foreach(AMTrack track in trackValues) {
                track.previewFrame(itarget, _frame, frameRate);
            }
        }
    }

    public void runFrame(AMITarget itarget, float _frame, float animScale, bool playAudio, bool noAudioLoop) {
        sortTracks();

        renderCameraSwitcherStill(itarget, _frame);

        foreach(AMTrack track in trackValues) {
            AMAudioTrack audioTrack = track as AMAudioTrack;
            if(audioTrack) {
                if(playAudio) {
                    AudioSource src = audioTrack.sampleAudio(itarget, _frame, animScale, frameRate, false);
                    if(src && noAudioLoop)
                        src.loop = false;
                }
                continue;
            }

            AMAnimatorMateTrack mateAnimTrack = track as AMAnimatorMateTrack;
            if(mateAnimTrack) {
                mateAnimTrack.runFrame(itarget, _frame, frameRate, animScale, playAudio, noAudioLoop);
                continue;
            }

            track.previewFrame(itarget, _frame, frameRate);
        }
    }

    /// <summary>
    /// Only preview tracks that have starting frame > _frame
    /// </summary>
    public void runStartUp(AMITarget itarget, float _frame, float animScale) {
        sortTracks();

        foreach(AMTrack track in trackValues) {
            if(track.keys.Count > 0) {
                if((float)track.keys[0].getStartFrame() > _frame)
                    track.previewFrame(itarget, _frame, frameRate);
                else if(_frame > 0) {
                    //special case for audio when playing at a particular frame
                    AMAudioTrack audioTrack = track as AMAudioTrack;
                    if(audioTrack)
                        audioTrack.sampleAudio(itarget, _frame, itarget.TargetAnimScale()*animScale, frameRate, false);
                }
            }
        }
    }

    private void renderCameraSwitcherStill(AMITarget itarget, float _frame) {
        if(!cameraSwitcher) return;

        AMCameraSwitcherTrack.cfTuple tuple = cameraSwitcher.getCameraFadeTupleForFrame(itarget, (int)_frame);
        if(tuple.frame != 0) {

            AMCameraFade cf = AMCameraFade.getCameraFade(true);
            cf.isReset = false;
            // create render texture still
            //bool isPro = PlayerSettings.advancedLicense;
            bool isPro = isProLicense;
            if(!cf.tex2d || cf.shouldUpdateStill || (isPro && cf.cachedStillFrame != tuple.frame)) {
                if(isPro) {
                    int firstTargetType = (tuple.isReversed ? tuple.type2 : tuple.type1);
                    int secondTargetType = (tuple.isReversed ? tuple.type1 : tuple.type2);
                    if(firstTargetType == 0) {
                        if(cf.tex2d) Object.DestroyImmediate(cf.tex2d);
                        previewFrame(itarget, tuple.frame, false, false);
                        // set top camera
                        //bool isReversed = tuple.isReversed;
                        Camera firstCamera = (tuple.isReversed ? tuple.camera2 : tuple.camera1);



                        AMUtil.SetTopCamera(firstCamera, cameraSwitcher.getAllCameras(itarget));

                        // set cached frame to 0 if bad frame
                        if(cf.width <= 0 || cf.height <= 0) {
                            if(Application.isPlaying) {
                                cf.width = Screen.width;
                                cf.height = Screen.height;
                            }
                            else {
                                cf.width = 200;
                                cf.height = 100;
                                cf.shouldUpdateStill = true;
                            }
                        }
                        else {
                            cf.shouldUpdateStill = false;
                        }
                        // render texture
                        RenderTexture renderTexture = RenderTexture.GetTemporary(cf.width, cf.height, 24);
                        //RenderTexture renderTexture = new RenderTexture(cf.width,cf.height,24);
                        firstCamera.targetTexture = renderTexture;
                        firstCamera.Render();
                        // readpixels from render texture
                        Texture2D tex2d = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
                        RenderTexture.active = renderTexture;
                        tex2d.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
                        tex2d.Apply();
                        // set texture
                        cf.tex2d = tex2d;
                        cf.cachedStillFrame = tuple.frame;
                        // cleanup
                        RenderTexture.active = null;
                        RenderTexture.ReleaseTemporary(renderTexture);
                        firstCamera.targetTexture = null;
                        if(cf.placeholder) cf.placeholder = false;
                        if(secondTargetType == 0) {

                            Camera secondCamera = (tuple.isReversed ? tuple.camera1 : tuple.camera2);
                            AMUtil.SetTopCamera(secondCamera, cameraSwitcher.getAllCameras(itarget));
                        }
                    }

                }
                else {
                    // show placeholder if non-pro
                    cf.tex2d = (Texture2D)Resources.Load("am_indie_placeholder");

                    if(!cf.placeholder) cf.placeholder = true;
                }

            }
            cf.useRenderTexture = false;
        }

    }

    public void sampleAudioAtFrame(AMITarget itarget, int frame, float speed) {
        foreach(AMTrack track in trackValues) {
            AMAudioTrack audioTrack = track as AMAudioTrack;
            if(audioTrack) { audioTrack.sampleAudioAtFrame(itarget, frame, speed, frameRate); continue; }

            AMAnimatorMateTrack mateAnimTrack = track as AMAnimatorMateTrack;
            if(mateAnimTrack) { mateAnimTrack.sampleAudioAtFrame(itarget, frame, speed, frameRate); continue; }
        }
    }

    public AMTranslationTrack getTranslationTrackForTransform(AMITarget itarget, Transform obj) {
        if(!obj) return null;
        foreach(AMTrack track in trackValues) {
            if((track is AMTranslationTrack) && track.isTargetEqual(itarget, obj))
                return track as AMTranslationTrack;
        }
        return null;
    }

    public AMKey[] removeKeysAfter(AMITarget itarget, int frame) {
        List<AMKey> dkeys = new List<AMKey>();
        bool didDeleteKeys;
        foreach(AMTrack track in trackValues) {
            didDeleteKeys = false;
            for(int i = 0; i < track.keys.Count; i++) {
                if(track.keys[i].frame > frame) {
                    // destroy key
                    dkeys.Add(track.keys[i]);
                    // remove from list
                    track.keys.RemoveAt(i);
                    didDeleteKeys = true;
                    i--;
                }
                if(didDeleteKeys) track.updateCache(itarget);
            }
        }
        return dkeys.ToArray();
    }

    public AMKey[] removeKeysBefore(AMITarget itarget, int frame) {
        List<AMKey> dkeys = new List<AMKey>();
        bool didDeleteKeys;
        foreach(AMTrack track in trackValues) {
            didDeleteKeys = false;
            for(int i = 0; i < track.keys.Count; i++) {
                if(track.keys[i].frame < frame) {
                    // destroy key
                    dkeys.Add(track.keys[i]);
                    // remove from list
                    track.keys.RemoveAt(i);
                    didDeleteKeys = true;
                    i--;
                }
                if(didDeleteKeys) track.updateCache(itarget);
            }
        }
        return dkeys.ToArray();
    }

    // does take have keys beyond frame
    public bool hasKeyAfter(int frame) {
        foreach(AMTrack track in trackValues) {
            if(track.keys.Count > 0) {
                // check last key on each track
                if(track.keys[track.keys.Count - 1].frame > frame) return true;
            }
        }
        return false;
    }

    // returns the highest frame value of the last key
    public int getLastFrame() {
        int frame = 0;
        foreach(AMTrack track in trackValues) {
            int trackLastFrame = track.getLastFrame(frameRate);
            if(trackLastFrame > frame)
                frame = trackLastFrame;
        }
        return frame;
    }

    // returns the lowest frame value of the first key
    public int getFirstFrame() {
        int numTrackWithKeys = 0;
        int frame = int.MaxValue;
        foreach(AMTrack track in trackValues) {
            if(track.keys.Count > 0) {
                AMKey key = track.keys[0];
                if(key.frame > frame)
                    frame = key.frame;
                numTrackWithKeys++;
            }
        }
        return numTrackWithKeys > 0 ? frame : 1;
    }

    // returns true if autokey successful
    public bool autoKey(AMITarget itarget, AMTrack.OnAddKey addCall, Transform obj, int frame) {
        if(!obj) return false;
        bool didKey = false;
        foreach(AMTrack track in trackValues) {
            // for each track, if rotation or translation then autokey
            if(track is AMTranslationTrack) {
                if((track as AMTranslationTrack).autoKey(itarget, addCall, obj, frame, frameRate)) {
                    if(!didKey) didKey = true;
                    //track.updateCache();
                }
            }
            else if(track is AMRotationTrack) {
                if((track as AMRotationTrack).autoKey(itarget, addCall, obj, frame, frameRate)) {
                    if(!didKey) didKey = true;
                }
            }
        }
        return didKey;
    }
    #endregion

    #region Other Fns

    public void undoRedoPerformed() {
        foreach(AMTrack track in trackValues)
            track.undoRedoPerformed();
    }

    public void maintainTake(AMITarget itarget) {
        foreach(AMTrack track in trackValues)
            track.maintainTrack(itarget);
    }

    public void sampleAudio(AMITarget itarget, float frame, float speed, bool playOneShots) {
        foreach(AMTrack track in trackValues) {
            AMAudioTrack audioTrack = track as AMAudioTrack;
            if(audioTrack) { audioTrack.sampleAudio(itarget, frame, speed, frameRate, playOneShots); continue; }

            AMAnimatorMateTrack mateAnimTrack = track as AMAnimatorMateTrack;
            if(mateAnimTrack) { mateAnimTrack.sampleAudio(itarget, frame, speed, frameRate, playOneShots); continue; }
        }
    }

    public void endAudioLoops(AMITarget itarget) {
        foreach(AMTrack track in trackValues) {
            AMAudioTrack audioTrack = track as AMAudioTrack;
            if(audioTrack) { audioTrack.endAudioLoop(itarget); continue; }

            AMAnimatorMateTrack mateAnimTrack = track as AMAnimatorMateTrack;
            if(mateAnimTrack) { mateAnimTrack.endAudioLoops(itarget); continue; }
        }
    }

    public void stopAudio(AMITarget itarget) {
        foreach(AMTrack track in trackValues) {
            AMAudioTrack audioTrack = track as AMAudioTrack;
            if(audioTrack) { audioTrack.stopAudio(itarget); continue; }

            AMAnimatorMateTrack mateAnimTrack = track as AMAnimatorMateTrack;
            if(mateAnimTrack) { mateAnimTrack.stopAudio(itarget); continue; }
        }
    }

    public void pauseAudio(AMITarget itarget) {
        foreach(AMTrack track in trackValues) {
            AMAudioTrack audioTrack = track as AMAudioTrack;
            if(audioTrack) { audioTrack.pauseAudio(itarget); continue; }

            AMAnimatorMateTrack mateAnimTrack = track as AMAnimatorMateTrack;
            if(mateAnimTrack) { mateAnimTrack.pauseAudio(itarget); continue; }
        }
    }

    public void resumeAudio(AMITarget itarget) {
        foreach(AMTrack track in trackValues) {
            AMAudioTrack audioTrack = track as AMAudioTrack;
            if(audioTrack) { audioTrack.resumeAudio(itarget); continue; }

            AMAnimatorMateTrack mateAnimTrack = track as AMAnimatorMateTrack;
            if(mateAnimTrack) { mateAnimTrack.resumeAudio(itarget); continue; }
        }
    }

    public void setAudioSpeed(AMITarget itarget, float speed) {
        foreach(AMTrack track in trackValues) {
            AMAudioTrack audioTrack = track as AMAudioTrack;
            if(audioTrack) { audioTrack.setAudioSpeed(itarget, speed); continue; }

            AMAnimatorMateTrack mateAnimTrack = track as AMAnimatorMateTrack;
            if(mateAnimTrack) { mateAnimTrack.setAudioSpeed(itarget, speed); continue; }
        }
    }

    public void resetScene(AMITarget itarget) {
        // reset scene, equivalent to previewFrame(1f) without previewing animation
        foreach(AMTrack track in trackValues) {
            if(track is AMAnimationTrack) continue;
            if(track is AMOrientationTrack)
                track.previewFrame(itarget, 1f, frameRate, getTranslationTrackForTransform(itarget, (track as AMOrientationTrack).getTargetForFrame(itarget, 1f)));
            else track.previewFrame(itarget, 1f, frameRate);
        }
    }

    public void drawGizmos(AMITarget itarget, float gizmo_size, bool inPlayMode) {
        foreach(AMTrack track in trackValues) {
            if(track is AMTranslationTrack)
                track.drawGizmos(itarget, gizmo_size);
            else if(track is AMOrientationTrack)
                (track as AMOrientationTrack).drawGizmos(itarget, gizmo_size, inPlayMode, selectedFrame);
        }
    }

    public void maintainCaches(AMITarget itarget) {
        // re-updates cache if there are null values
        if(trackValues != null) {
            foreach(AMTrack track in trackValues) {
                bool shouldUpdateCache = false;
                if(track != null && track.keys != null) {
                    foreach(AMKey key in track.keys) {
                        if(key.version != track.version) {
                            shouldUpdateCache = true;
                            break;
                        }
                    }
                    if(shouldUpdateCache) {
                        track.updateCache(itarget);
                    }
                }
            }
        }
    }

    public float getElementsHeight(int group_id, float height_track, float height_track_foldin, float height_group) {
        initGroups();
        float height = 0;
        AMGroup grp = getGroup(group_id);

        if(group_id < 0) {

            height += height_group;
            if(grp.elements.Count <= 0 && grp.foldout)	// "no tracks" label height
                height += height_group;
        }
        if(group_id == 0 || grp.foldout) {
            foreach(int id in grp.elements) {
                // group within group
                if(id < 0) {

                    height += getElementsHeight(id, height_track, height_track_foldin, height_group);
                }
                else {
                    AMTrack track = getTrack(id);
                    if(track && track.foldout) height += height_track;
                    else height += height_track_foldin;
                }
            }
        }
        return height;
    }

    public float getElementsHeight(int element_id, int group_id, float height_track, float height_track_foldin, float height_group, ref bool found) {
        // get elements height up until element_id. requires reference to boolean "found" with the default value of false
        initGroups();
        float height = 0;
        AMGroup grp = getGroup(group_id);

        if(group_id < 0) {

            if(group_id == element_id) {
                found = true;
                return height;
            }
            height += height_group;
            if(grp.elements.Count <= 0 && grp.foldout)	// "no tracks" label height
                height += height_group;
        }
        if(group_id == 0 || grp.foldout) {
            foreach(int id in grp.elements) {
                if(id == element_id) {
                    found = true;
                    return height;
                }
                // group within group
                if(id < 0) {
                    height += getElementsHeight(element_id, id, height_track, height_track_foldin, height_group, ref found);
                    if(found) {
                        return height;	// return height if element is found
                    }
                }
                else {

                    if(getTrack(id).foldout) height += height_track;
                    else height += height_track_foldin;
                }
            }
        }
        return height;
    }

    public float getElementY(int element_id, float height_track, float height_track_foldin, float height_group) {
        bool found = false;
        return getElementsHeight(element_id, 0, height_track, height_track_foldin, height_group, ref found);

    }
    #endregion

    public List<GameObject> getDependencies(AMITarget itarget) {
        List<GameObject> ls = new List<GameObject>();

        foreach(AMTrack track in trackValues) {
            ls = ls.Union(track.getDependencies(itarget)).ToList();
        }
        return ls;
    }

    // returns list of GameObjects to not delete due to issues resolving references
    public List<GameObject> updateDependencies(AMITarget itarget, List<GameObject> newReferences, List<GameObject> oldReferences) {
        List<GameObject> lsFlagToKeep = new List<GameObject>();
        foreach(AMTrack track in trackValues) {
            lsFlagToKeep = lsFlagToKeep.Union(track.updateDependencies(itarget, newReferences, oldReferences)).ToList();
            track.updateCache(itarget);
        }
        return lsFlagToKeep;
    }
}
