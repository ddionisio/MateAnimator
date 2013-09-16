using UnityEngine;
//using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Holoville.HOTween;

[AddComponentMenu("")]
public class AMTake : MonoBehaviour {
    public delegate void OnSequenceDone(AMTake take);

    #region Declarations

    public new string name;					// take name
    public int frameRate = 24;				// frames per second
    public int numFrames = 1440;			// number of frames
    public float startFrame = 1f;				// first frame to render
    public float endFrame = 100f;
    public int playbackSpeedIndex = 2;		// default playback speed x1

    public int numLoop = 1; //number of times this plays before it is done
    public LoopType loopMode = LoopType.Restart;
    public int loopBackToFrame = -1; //set this to loop back at given frame when sequence is complete, make sure numLoop = 1 and loopMode is Restart
    public bool pausePreviousTake = false;

    public int selectedTrack = -1;			// currently selected track index
    public int selectedFrame = 1;			// currently selected frame (frame to preview, not necessarily in context selection)
    public int selectedGroup = 0;

    public List<AMTrack> trackValues = new List<AMTrack>();

    public List<int> contextSelection = new List<int>();	// list of all frames included in the context selection
    public List<int> ghostSelection = new List<int>();		// list of all frames included in the ghost selection
    public List<int> contextSelectionTracks = new List<int>();

    public int track_count = 1;		// number of tracks created, used to generate unique track ids
    public int group_count = 0;		// number of groups created, used to generate unique group ids. negative number to differentiate from positive track ids

    public AMGroup rootGroup;
    public List<AMGroup> groupValues = new List<AMGroup>();

    public static bool isProLicense = true;

    private Sequence mSequence;
    private bool mTracksSorted = false;

    public Sequence sequence { get { return mSequence; } }
    public event OnSequenceDone sequenceCompleteCallback;

    #endregion

    public static AMTake NewInstance(GameObject holder) {
        AMTake newTake = holder.AddComponent<AMTake>();
        newTake.enabled = false;
        newTake.loopBackToFrame = -1;
        return newTake;
    }

    void OnDestroy() {
        if(mSequence != null) {
            HOTween.Kill(mSequence);
            mSequence = null;
        }

        sequenceCompleteCallback = null;
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
    // select a track by index
    public void selectTrack(int index, bool isShiftDown, bool isControlDown) {
        bool isInContextSelection = contextSelectionTracks.Contains(index);
        if(!isShiftDown && !isControlDown) {
            if(selectedTrack != index) {
                selectedTrack = index;
                if(!isInContextSelection) {
                    // clear context selection
                    contextSelection = new List<int>();
                    contextSelectionTracks = new List<int>();
                }
            }
            if(index > -1) selectGroup(getTrackGroup(index), false, false, true);
        }

        if(!isInContextSelection)
            contextSelectionTracks.Add(index);
        else if(isControlDown && selectedTrack != index && !isShiftDown) {
            contextSelectionTracks.Remove(index);
        }
        // select range
        if((selectedTrack != -1 || selectedGroup != 0) && isShiftDown) {
            List<int> range = getTrackIDsForRange((selectedTrack != -1 ? selectedTrack : selectedGroup), index);
            foreach(int track_id in range) {
                if(!contextSelectionTracks.Contains(track_id)) contextSelectionTracks.Add(track_id);
            }
        }
    }

    public AMTrack getSelectedTrack() {
        return getTrack(selectedTrack);
    }

    // add translation track
    public AMTrack addTranslationTrack(GameObject obj) {
        AMTranslationTrack a = gameObject.AddComponent<AMTranslationTrack>();
        a.enabled = false;
        a.setName(getTrackCount());
        a.id = getUniqueTrackID();
        if(obj) a.obj = obj.transform;
        a.isLocal = true;
        addTrack(a);
        return a;
    }

    // add rotation track
    public AMTrack addRotationTrack(GameObject obj) {
        AMRotationTrack a = gameObject.AddComponent<AMRotationTrack>();
        a.enabled = false;
        a.setName(getTrackCount());
        a.id = getUniqueTrackID();
        if(obj) a.obj = obj.transform;
        a.isLocal = true;
        addTrack(a);
        return a;
    }

    // add orientation track
    public AMTrack addOrientationTrack(GameObject obj) {
        AMOrientationTrack a = gameObject.AddComponent<AMOrientationTrack>();
        a.enabled = false;
        a.setName(getTrackCount());
        a.id = getUniqueTrackID();
        if(obj) a.obj = obj.transform;
        addTrack(a);
        return a;
    }

    // add animation track
    public AMTrack addAnimationTrack(GameObject obj) {
        AMAnimationTrack a = gameObject.AddComponent<AMAnimationTrack>();
        a.enabled = false;
        a.setName(getTrackCount());
        a.id = getUniqueTrackID();
        if(obj && obj.GetComponent(typeof(Animation))) a.obj = obj;
        addTrack(a);
        return a;
    }

    // add audio track
    public AMTrack addAudioTrack(GameObject obj) {
        AMAudioTrack a = gameObject.AddComponent<AMAudioTrack>();
        a.enabled = false;
        a.setName(getTrackCount());
        a.id = getUniqueTrackID();
        if(obj && obj.GetComponent(typeof(AudioSource))) a.audioSource = (AudioSource)obj.GetComponent(typeof(AudioSource));
        addTrack(a);
        return a;
    }

    // add property track
    public AMTrack addPropertyTrack(GameObject obj) {
        AMPropertyTrack a = gameObject.AddComponent<AMPropertyTrack>();
        a.enabled = false;
        a.setName(getTrackCount());
        a.id = getUniqueTrackID();
        if(obj) a.obj = obj;
        addTrack(a);
        return a;
    }

    // add event track
    public AMTrack addEventTrack(GameObject obj) {
        AMEventTrack a = gameObject.AddComponent<AMEventTrack>();
        a.enabled = false;
        a.setName(getTrackCount());
        a.id = getUniqueTrackID();
        if(obj) a.obj = obj;
        addTrack(a);
        return a;
    }

    // add go set active track
    public AMTrack addGOSetActiveTrack(GameObject obj) {
        AMGOSetActiveTrack a = gameObject.AddComponent<AMGOSetActiveTrack>();
        a.enabled = false;
        a.setName(getTrackCount());
        a.id = getUniqueTrackID();
        if(obj) a.setObject(obj);
        addTrack(a);
        return a;
    }

    public void deleteTrack(int trackid, bool deleteFromGroup = true) {
        AMTrack track = getTrack(trackid);
        if(track)
            deleteTrack(track, deleteFromGroup);
    }

    public void deleteTrack(AMTrack track, bool deleteFromGroup = true) {
        int id = track.id;
        int index = getTrackIndex(id);
        if(track) {
            track.destroy();
        }

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
    public void selectGroup(int group_id, bool isShiftDown, bool isControlDown, bool softSelect = false) {

        if(isShiftDown || isControlDown) {
            contextSelectGroup(group_id, isControlDown);
            // select range
            if((selectedTrack != -1 || selectedGroup != 0) && isShiftDown) {
                List<int> range = getTrackIDsForRange((selectedTrack != -1 ? selectedTrack : selectedGroup), group_id);
                foreach(int track_id in range) {
                    if(!contextSelectionTracks.Contains(track_id)) contextSelectionTracks.Add(track_id);
                }
            }
        }
        else if(!softSelect) {
            if(contextSelectionTracks.Count == 1) contextSelectionTracks = new List<int>();
        }
        selectedGroup = group_id;
    }

    /*public void contextSelectGroup(int group_id, bool deselect) {
        AMGroup grp = getGroup(group_id);
        for(int i=0;i<grp.elements.Count;i++) {
            // select track
            if(grp.elements[i] > 0) {
                bool isSelected = contextSelectionTracks.Contains(grp.elements[i]);
                if(deselect) {
                    if(isSelected) contextSelectionTracks.Remove(grp.elements[i]);
                } else {
                    if(!isSelected) contextSelectionTracks.Add(grp.elements[i]);
                }
            } else {
                contextSelectGroup(grp.elements[i],deselect);	
            }
        }
    }*/

    public void contextSelectGroup(int group_id, bool isControlDown) {
        AMGroup grp = getGroup(group_id);
        int numTracks = 0;
        bool deselect = isControlDown && isGroupSelected(group_id, ref numTracks);
        for(int i = 0; i < grp.elements.Count; i++) {
            // select track
            if(grp.elements[i] > 0) {
                bool isSelected = contextSelectionTracks.Contains(grp.elements[i]);
                if(deselect) {
                    if(isSelected) contextSelectionTracks.Remove(grp.elements[i]);
                }
                else {
                    if(!isSelected) contextSelectionTracks.Add(grp.elements[i]);
                }
            }
            else {
                contextSelectGroup(grp.elements[i], deselect);
            }
        }
    }

    public bool isGroupSelected(int group_id, ref int numTracks) {
        AMGroup grp = getGroup(group_id);
        for(int i = 0; i < grp.elements.Count; i++) {
            // select track
            if(grp.elements[i] > 0) {
                if(!contextSelectionTracks.Contains(grp.elements[i])) return false;
                numTracks++;
            }
            else {
                if(isGroupSelected(grp.elements[i], ref numTracks) == false) return false;
            }
        }
        return true;
    }

    public void deleteGroup(int group_id, bool deleteContents) {
        if(group_id >= 0) return;
        AMGroup grp = getGroup(group_id);
        if(deleteContents) {
            // delete elements
            for(int i = 0; i < grp.elements.Count; i++) {
                if(grp.elements[i] > 0) deleteTrack(grp.elements[i], false);
                else if(grp.elements[i] < 0) deleteGroup(grp.elements[i], deleteContents);
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

    public void deleteSelectedGroup(bool deleteContents) {
        deleteGroup(selectedGroup, deleteContents);
        // select root group
        selectedGroup = 0;

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

    private void addTrack(AMTrack track) {
        trackValues.Add(track);
        addToGroup(track.id, selectedGroup);
        track.parentTake = this;
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

    public void addGroup() {
        initGroups();
        AMGroup g = new AMGroup();
        g.init(getUniqueGroupID());
        groupValues.Add(g);
        rootGroup.elements.Add(g.group_id);

        // select new group when it has been created
        selectedGroup = g.group_id;
    }

    public AMTake(string name) {
        this.name = name;
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
    // select a frame
    public void selectFrame(int track, int num, float numFramesToRender, bool isShiftDown, bool isControlDown) {
        selectedFrame = num;
        selectTrack(track, isShiftDown, isControlDown);

        if((selectedFrame < startFrame) || (selectedFrame > endFrame)) {
            startFrame = selectedFrame;
            endFrame = startFrame + (int)numFramesToRender - 1;
        }
    }

    static int TrackCompare(AMTrack t1, AMTrack t2) {
        if(t1 == t2)
            return 0;
        else if(t1 == null)
            return 1;
        else if(t2 == null)
            return -1;

        return t1.order - t2.order;
    }

    // preview a frame
    public void previewFrame(float _frame, bool orientationOnly = false, bool quickPreview = false /* do not preview properties to execute */) {
#if UNITY_EDITOR
        if(!Application.isPlaying)
            mTracksSorted = false;
#endif

        if(!mTracksSorted) {
            trackValues.Sort(TrackCompare);
            mTracksSorted = true;
        }

        if(orientationOnly) {
            foreach(AMTrack track in trackValues) {
                if(track is AMOrientationTrack || track is AMRotationTrack)
                    track.previewFrame(_frame);
            }
        }
        else {
            foreach(AMTrack track in trackValues) {
                if(track is AMAnimationTrack) (track as AMAnimationTrack).previewFrame(_frame, frameRate);
                else if(track is AMPropertyTrack) (track as AMPropertyTrack).previewFrame(_frame, quickPreview);
                else track.previewFrame(_frame);
            }
        }
    }

    public void sampleAudioAtFrame(int frame, float speed) {
        foreach(AMTrack track in trackValues) {
            if(!(track is AMAudioTrack)) continue;
            (track as AMAudioTrack).sampleAudioAtFrame(frame, speed, frameRate);
        }
    }

    public AMTranslationTrack getTranslationTrackForTransform(Transform obj) {
        if(!obj) return null;
        foreach(AMTrack track in trackValues) {
            if((track is AMTranslationTrack) && (track as AMTranslationTrack).isObjectEqual(obj))
                return track as AMTranslationTrack;
        }
        return null;
    }

    // delete keys after a frame
    public void deleteKeysAfter(int frame) {
        bool didDeleteKeys;
        foreach(AMTrack track in trackValues) {
            didDeleteKeys = false;
            for(int i = 0; i < track.keys.Count; i++) {
                if(track.keys[i].frame > frame) {
                    // destroy key
                    track.keys[i].destroy();
                    // remove from list
                    track.keys.RemoveAt(i);
                    didDeleteKeys = true;
                    i--;
                }
                if(didDeleteKeys) track.updateCache();
            }
        }

    }

    // delete keys after a frame
    public void deleteKeysBefore(int frame) {
        bool didDeleteKeys;
        foreach(AMTrack track in trackValues) {
            didDeleteKeys = false;
            for(int i = 0; i < track.keys.Count; i++) {
                if(track.keys[i].frame < frame) {
                    // destroy key
                    track.keys[i].destroy();
                    // remove from list
                    track.keys.RemoveAt(i);
                    didDeleteKeys = true;
                    i--;
                }
                if(didDeleteKeys) track.updateCache();
            }
        }

    }

    public void shiftOutOfBoundsKeysOnSelectedTrack() {
        int offset = getSelectedTrack().shiftOutOfBoundsKeys();
        if(contextSelection.Count <= 0) return;
        for(int i = 0; i < contextSelection.Count; i++) {
            contextSelection[i] += offset;
        }
        // shift all keys on all tracks
        foreach(AMTrack track in trackValues) {
            if(track.id == selectedTrack) continue;
            track.offsetKeysFromBy(1, offset);
        }
    }

    public void shiftOutOfBoundsKeysOnTrack(AMTrack _track) {
        int offset = _track.shiftOutOfBoundsKeys();
        if(contextSelection.Count <= 0) return;
        for(int i = 0; i < contextSelection.Count; i++) {
            contextSelection[i] += offset;
        }
        // shift all keys on all tracks
        foreach(AMTrack track in trackValues) {
            if(track.id == _track.id) continue;
            track.offsetKeysFromBy(0, offset);
        }
    }

    /*public void deleteSelectedKeys() {
        bool didDeleteKeys = false;
        AMTrack track = getSelectedTrack();
        for(int i=0; i<track.keys.Count;i++) {
            if(!isFrameInContextSelection(track.keys[i].frame)) continue;
            track.keys[i].destroy();
            track.keys.Remove(track.keys[i]);
            i--;
            didDeleteKeys = true;
        }
        if(didDeleteKeys) getSelectedTrack().updateCache();
    }*/
    public void deleteSelectedKeysFromTrack(int track_id) {
        bool didDeleteKeys = false;
        AMTrack track = getTrack(track_id);
        for(int i = 0; i < track.keys.Count; i++) {
            if(!isFrameInContextSelection(track.keys[i].frame)) continue;
            track.keys[i].destroy();
            track.keys.Remove(track.keys[i]);
            i--;
            didDeleteKeys = true;
        }
        if(didDeleteKeys) track.updateCache();
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

    // returns true if autokey successful
    public bool autoKey(Transform obj, int frame, AMTrack.OnKey addCallback) {
        if(!obj) return false;
        bool didKey = false;
        foreach(AMTrack track in trackValues) {
            // for each track, if rotation or translation then autokey
            if(track is AMTranslationTrack) {
                if((track as AMTranslationTrack).autoKey(obj, frame, addCallback)) {
                    if(!didKey) didKey = true;
                    //track.updateCache();
                }
            }
            else if(track is AMRotationTrack) {
                if((track as AMRotationTrack).autoKey(obj, frame, addCallback)) {
                    if(!didKey) didKey = true;
                }
            }
        }
        return didKey;
    }
    #endregion
    #region Context Selection
    public bool isFrameInContextSelection(int frame) {
        for(int i = 0; i < contextSelection.Count; i += 2) {
            if(frame >= contextSelection[i] && frame <= contextSelection[i + 1]) return true;
        }
        return false;
    }

    public bool isFrameInGhostSelection(int frame) {
        if(ghostSelection == null) return false;
        for(int i = 0; i < ghostSelection.Count; i += 2) {
            if(frame >= ghostSelection[i] && frame <= ghostSelection[i + 1]) return true;
        }
        return false;
    }

    public bool isFrameSelected(int frame) {
        if(hasGhostSelection()) {
            return isFrameInGhostSelection(frame);
        }
        return isFrameInContextSelection(frame);
    }

    public void contextSelectFrame(int frame, bool toggle) {
        // if already exists return, toggle if true
        for(int i = 0; i < contextSelection.Count; i += 2) {
            if(frame >= contextSelection[i] && frame <= contextSelection[i + 1]) {
                if(toggle) {
                    if(frame == contextSelection[i] && frame == contextSelection[i + 1]) {
                        // remove single frame range
                        contextSelection.RemoveAt(i);
                        contextSelection.RemoveAt(i);
                    }
                    else if(frame == contextSelection[i]) {
                        // shift first frame forward
                        contextSelection[i]++;
                    }
                    else if(frame == contextSelection[i + 1]) {
                        // shift last frame backwards
                        contextSelection[i + 1]++;
                    }
                    else {
                        // split range
                        int start = contextSelection[i];
                        int end = contextSelection[i + 1];
                        // remove range
                        contextSelection.RemoveAt(i);
                        contextSelection.RemoveAt(i);
                        // add range left
                        contextSelection.Add(start);
                        contextSelection.Add(frame - 1);
                        // add range right
                        contextSelection.Add(frame + 1);
                        contextSelection.Add(end);
                        contextSelection.Sort();
                    }
                }
                return;
            }
        }
        // add twice, as a range
        contextSelection.Add(frame);
        contextSelection.Add(frame);
        contextSelection.Sort();
    }

    // make selection from start frame to end frame
    public void contextSelectFrameRange(int startFrame, int endFrame) {
        // if selected only one frame
        if(startFrame == endFrame) {
            contextSelectFrame(endFrame, false);
            return;
        }
        int _endFrame = endFrame;
        if(endFrame < startFrame) {
            endFrame = startFrame;
            startFrame = _endFrame;
        }
        // check for previous selection
        for(int i = 0; i < contextSelection.Count; i += 2) {
            // new selection engulfs previous selection
            if(startFrame <= contextSelection[i] && endFrame >= contextSelection[i + 1]) {
                // remove previous selection
                contextSelection.RemoveAt(i);
                contextSelection.RemoveAt(i);
                i -= 2;
                // previous selection engulfs new selection
            }
            else if(contextSelection[i] <= startFrame && contextSelection[i + 1] >= endFrame) {
                // do nothing
                return;
            }
        }
        // add new selection
        contextSelection.Add(startFrame);
        contextSelection.Add(endFrame);
        contextSelection.Sort();
    }

    public void contextSelectAllFrames() {
        contextSelection = new List<int>();
        contextSelection.Add(1);
        contextSelection.Add(numFrames);
    }

    public bool contextSelectionHasKeys() {
        foreach(AMKey key in getSelectedTrack().keys) {
            for(int i = 0; i < contextSelection.Count; i += 2) {
                // if selection start frame > frame, break out of sorted list
                if(contextSelection[i] > key.frame) break;
                if(contextSelection[i] <= key.frame && contextSelection[i + 1] >= key.frame) return true;
            }
        }
        return false;
    }

    public AMKey[] getContextSelectionKeysForTrack(AMTrack track) {
        List<AMKey> keys = new List<AMKey>();
        foreach(AMKey key in track.keys) {
            for(int i = 0; i < contextSelection.Count; i += 2) {
                // if selection start frame > frame, break out of sorted list
                if(contextSelection[i] > key.frame) break;
                if(contextSelection[i] <= key.frame && contextSelection[i + 1] >= key.frame) keys.Add(key);
            }
        }
        return keys.ToArray();
    }

    /*public AMKey[] getContextSelectionKeys() {
        List<AMKey> keys = new List<AMKey>();
        foreach(AMKey key in getSelectedTrack().keys) {
            for(int i=0;i<contextSelection.Count;i+=2) {
                // if selection start frame > frame, break out of sorted list
                if(contextSelection[i] > key.frame) break;
                if(contextSelection[i] <= key.frame && contextSelection[i+1] >= key.frame) keys.Add(key);
            }
        }
        return keys.ToArray();
    }*/

    // offset context selection frames by an amount. can be positive or negative
    public void offsetContextSelectionFramesBy(int offset) {
        if(offset == 0) return;
        if(contextSelection.Count <= 0) return;
        foreach(int track_id in contextSelectionTracks) {
            bool shouldUpdateCache = false;
            List<AMKey> keysToDelete = new List<AMKey>();
            AMTrack _track = getTrack(track_id);
            foreach(AMKey key in _track.keys) {
                for(int i = 0; i < contextSelection.Count; i += 2) {
                    // move context selection
                    if(contextSelection[i] <= key.frame && contextSelection[i + 1] >= key.frame) {
                        // if there is already a key in the new frame position, mark for deletion
                        bool keyToOverwriteInContextSelection = false;
                        if(_track.hasKeyOnFrame(key.frame + offset)) {
                            // check if the key is in the selection
                            for(int j = 0; j < contextSelection.Count; j += 2) {
                                if(contextSelection[j] <= (key.frame + offset) && contextSelection[j + 1] >= (key.frame + offset)) {
                                    keyToOverwriteInContextSelection = true;
                                    break;
                                }
                            }
                            // if not key is not in selection, mark for deletion
                            if(!keyToOverwriteInContextSelection) keysToDelete.Add(_track.getKeyOnFrame(key.frame + offset));
                        }
                        key.frame += offset;
                        if(!shouldUpdateCache) shouldUpdateCache = true;
                        break;
                    }
                }

            }
            // delete keys that were overwritten
            foreach(AMKey key in keysToDelete) {
                _track.keys.Remove(key);
                key.destroy();
            }
            // release references
            keysToDelete = new List<AMKey>();
            // update cache
            if(shouldUpdateCache) {
                _track.updateCache();
            }
        }
        // update context selection
        for(int i = 0; i < contextSelection.Count; i++) {
            // move context selection
            contextSelection[i] += offset;
        }
        // clear ghost selection
        ghostSelection = new List<int>();

    }

    private int ghost_selection_total_offset = 0;
    public void offsetGhostSelectionBy(int offset) {
        // update ghost selection
        for(int i = 0; i < ghostSelection.Count; i++) {
            // move ghost selection
            ghostSelection[i] += offset;
        }
        ghost_selection_total_offset += offset;
    }

    // copy the values from the context selection to the ghost selection
    public void setGhostSelection() {
        ghostSelection = new List<int>();
        ghost_selection_total_offset = 0;
        foreach(int frame in contextSelection) ghostSelection.Add(frame);
    }

    public bool hasGhostSelection() {
        if(ghostSelection == null || ghostSelection.Count > 0) return true;
        return false;
    }

    public int[] getKeyFramesInGhostSelection(int startFrame, int endFrame, int track_id) {
        List<int> key_frames = new List<int>();
        if(track_id <= -1) return key_frames.ToArray();
        foreach(AMKey key in getTrack(track_id).keys) {
            if(key.frame + ghost_selection_total_offset < startFrame) continue;
            if(key.frame + ghost_selection_total_offset > endFrame) break;
            if(isFrameInContextSelection(key.frame)) key_frames.Add(key.frame + ghost_selection_total_offset);
        }
        return key_frames.ToArray();
    }
    #endregion
    #region Other Fns

    public void maintainTake() {
        foreach(AMTrack track in trackValues) {
            if(!track.parentTake) track.parentTake = this;
        }
    }

    public void sampleAudio(float frame, float speed) {
        foreach(AMTrack track in trackValues) {
            if(!(track is AMAudioTrack)) continue;
            (track as AMAudioTrack).sampleAudio(frame, speed, frameRate);
        }
    }

    public void stopAudio() {
        foreach(AMTrack track in trackValues) {
            if(!(track is AMAudioTrack)) continue;
            (track as AMAudioTrack).stopAudio();
        }
    }

    public void stopAnimations() {
        foreach(AMTrack track in trackValues) {
            if(!(track is AMAnimationTrack)) continue;
            if((track as AMAnimationTrack).obj) {
                (track as AMAnimationTrack).obj.animation.Stop();
            }
        }
    }

    public void resetScene() {
        // reset scene, equivalent to previewFrame(1f) without previewing animation
        foreach(AMTrack track in trackValues) {
            if(track is AMAnimationTrack) continue;
            if(track is AMOrientationTrack) track.previewFrame(1f, getTranslationTrackForTransform((track as AMOrientationTrack).getTargetForFrame(1f)));
            else track.previewFrame(1f);
        }
    }

    public void drawGizmos(float gizmo_size, bool inPlayMode) {
        foreach(AMTrack track in trackValues) {
            if(track is AMTranslationTrack)
                track.drawGizmos(gizmo_size);
            else if(track is AMOrientationTrack)
                (track as AMOrientationTrack).drawGizmos(gizmo_size, inPlayMode, selectedFrame);
        }
    }

    public void maintainCaches() {
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
                        track.updateCache();
                    }
                }
            }
        }
    }

    public void BuildSequence(string goName, bool autoKill, UpdateType updateType, float startFrame = float.PositiveInfinity) {
        if(mSequence == null) {
            mSequence = new Sequence(
                new SequenceParms()
                .Id(string.Format("{0}:{1}", goName, name))
                .UpdateType(updateType)
                .AutoKill(autoKill)
                .Loops(numLoop, loopMode)
                .OnComplete(OnSequenceComplete, null));

            maintainCaches();

            if(!float.IsInfinity(startFrame)) {
                if(startFrame > mSequence.duration * frameRate)
                    startFrame /= mSequence.duration * frameRate;

                previewFrame(startFrame, false, true);
            }

            int numTweensAdded = 0;

            foreach(AMTrack track in trackValues) {
                track.buildSequenceStart(mSequence, frameRate);

                foreach(AMKey key in track.keys) {
                    Tweener tween = key.buildTweener(mSequence, frameRate);
                    if(tween != null) {
                        mSequence.Insert(key.getWaitTime(frameRate, 0.0f), tween);
                        numTweensAdded++;
                    }
                }
            }

            if(numTweensAdded == 0) {
                HOTween.Kill(mSequence);
                mSequence = null;
            }
        }
    }

    void OnSequenceComplete(TweenEvent dat) {
        stopAudio();
        stopAnimations();

        if(dat.tween.autoKillOnComplete)
            mSequence = null;
        else if(loopBackToFrame >= 0 && dat.tween.loops == 1 && dat.tween.loopType == LoopType.Restart) {
            mSequence.GoTo(((float)loopBackToFrame) / ((float)frameRate));
            mSequence.Play();
            return;
        }

        if(sequenceCompleteCallback != null) {
            sequenceCompleteCallback(this);
        }
    }

    public void executeActions(float fromFrame = 0f) {
        Debug.LogError("need implement");
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

    public void destroy() {
        foreach(AMTrack track in trackValues) {
            track.destroy();
        }

        groupValues.Clear();
        rootGroup = null;

        if(mSequence != null) {
            HOTween.Kill(mSequence);
            mSequence = null;
        }

        sequenceCompleteCallback = null;

        DestroyImmediate(this);
    }

    public List<GameObject> getDependencies() {
        List<GameObject> ls = new List<GameObject>();

        foreach(AMTrack track in trackValues) {
            ls = ls.Union(track.getDependencies()).ToList();
        }
        return ls;
    }

    // returns list of GameObjects to not delete due to issues resolving references
    public List<GameObject> updateDependencies(List<GameObject> newReferences, List<GameObject> oldReferences) {
        List<GameObject> lsFlagToKeep = new List<GameObject>();
        foreach(AMTrack track in trackValues) {
            lsFlagToKeep = lsFlagToKeep.Union(track.updateDependencies(newReferences, oldReferences)).ToList();
            track.updateCache();
        }
        return lsFlagToKeep;
    }

    public bool CheckNulls(ref int numKeys) {
        foreach(AMTrack track in trackValues) {
            if(track) {
                if(!track.CheckNullKeys())
                    return false;

                numKeys += track.keys.Count;
            }
            else {
                return false;
            }
        }

        return true;
    }
}
