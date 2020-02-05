using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using DG.Tweening;

namespace M8.Animator {
    [System.Serializable]
    public class Take {
        #region Declarations
        public string name;                 // take name
        public int frameRate;				// frames per second
        public int endFramePadding = 0;         // last frame + endFramePadding

        public int numLoop = 1; //number of times this plays before it is done
        public LoopType loopMode = LoopType.Restart;
        public int loopBackToFrame = 0; //set this to loop back at given frame when sequence is complete, make sure numLoop = 1 and loopMode is Restart

        public List<Track> trackValues { get { return mTrackValues; } set { mTrackValues = value; } }
        private List<Track> mTrackValues = new List<Track>();

        public int trackCounter = 1;     // number of tracks created, used to generate unique track ids
        public int groupCounter = 0;     // number of groups created, used to generate unique group ids. negative number to differentiate from positive track ids

        public Group rootGroup;
        public List<Group> groupValues = new List<Group>();

        [System.NonSerialized]
        public int selectedFrame = 1;

        public static bool isProLicense = true;

        private CameraSwitcherTrack mCameraSwitcher;

        public CameraSwitcherTrack cameraSwitcher {
            get {
                if(mCameraSwitcher == null) {
                    for(int i = 0; i < trackValues.Count; i++) {
                        if(trackValues[i] is CameraSwitcherTrack) {
                            mCameraSwitcher = trackValues[i] as CameraSwitcherTrack;
                            break;
                        }
                    }
                }
                return mCameraSwitcher;
            }
        }

        public int totalFrames {
            get {
                int _maxFrame = 0;
                for(int i = 0; i < trackValues.Count; i++) {
                    int frame = trackValues[i].getLastFrame(frameRate);
                    if(frame > _maxFrame)
                        _maxFrame = frame;
                }

                return _maxFrame;
            }
        }

#endregion

        //Only used by editor
        public void RevertToDefault(int fps) {
            trackValues.Clear();
            groupValues.Clear();

            rootGroup = null;
            initGroups();
            name = "Take 1";
            frameRate = fps;
            endFramePadding = 0;

            numLoop = 1;
            loopMode = LoopType.Restart;
            loopBackToFrame = 0;

            trackCounter = 1;
            groupCounter = 0;
        }

        // Adding a new track type
        // =====================
        // Create track class
        // Make sure to add [System.Serializable] to every class
        // Set track class properties
        // Override getTrackType in track class
        // Add track type to showObjectFieldFor in TimelineWindow
        // Create an addXTrack method here, and put it in TimelineWindow
        // Create track key class, make sure to override CreateClone 
        // Create AMXAction class for track class that overrides execute and implements ToString # TO DO #
        // Override updateCache in track class
        // Create addKey method in track class, and put it in addKey in TimelineWindow
        // Add track to timeline action in TimelineWindow
        // Add inspector properties to showInspectorPropertiesFor in TimelineWindow
        // Override previewFrame method in track class
        // Add track object to timelineSelectObjectFor in TimelineWindow (optional)
        // Override getDependencies and updateDependencies in track class
        // Add details to Code View # TO DO #


#region Tracks

        // add translation track
        public void addTrack(int groupId, ITarget target, Transform obj, bool usePath, Track a) {
            a.setName(getTrackCount());
            a.id = getUniqueTrackID();
            a.SetTarget(target, obj, usePath);
            addTrack(groupId, a);
            if(a is CameraSwitcherTrack) mCameraSwitcher = a as CameraSwitcherTrack;
        }

        public void addExistingTrack(int groupID, Track track, bool generateName, bool generateID) {
            if(generateName)
                track.setName(getTrackCount());
            if(generateID)
                track.id = getUniqueTrackID();

            addTrack(groupID, track);
        }

        public void deleteTrack(int trackid, bool deleteFromGroup = true) {
            Track track = getTrack(trackid);
            if(track != null)
                deleteTrack(track, deleteFromGroup);
        }
        
        public void deleteTrack(Track track, bool deleteFromGroup = true) {
            int id = track.id;
            int index = getTrackIndex(id);
            if(track != null) {
                if(mCameraSwitcher == track) mCameraSwitcher = null;
                track.destroy();
            }

            trackValues.RemoveAt(index);
            if(deleteFromGroup) deleteTrackFromGroups(id);
            sortTracks();
        }
        
        // get track ids from range, exclusive
        public List<int> getTrackIDsForRange(int start_id, int end_id) {
            if(start_id == end_id) return new List<int>();
            int loc1 = getElementLocationIndex(start_id);
            if(loc1 == 0) return new List<int>();   // id not found
            int loc2 = getElementLocationIndex(end_id);
            if(loc2 == 0) return new List<int>();   // id not found
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
            Group grp = getGroup(group_id);
            for(int i = 0; i < grp.elements.Count; i++) {
                if(grp.elements[i] == start_id) {
                    foundStartID = true;
                    if(grp.elements[i] > 0) continue;   // if track is start id, continue
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
            Group grp = getGroup(group_id);
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

        public void deleteGroup(int group_id, bool deleteContents) {
            if(group_id >= 0) return;
            Group grp = getGroup(group_id);
            if(deleteContents) {
                // delete elements
                for(int i = 0; i < grp.elements.Count; i++) {
                    if(grp.elements[i] > 0) deleteTrack(grp.elements[i], false);
                    else if(grp.elements[i] < 0) deleteGroup(grp.elements[i], deleteContents);
                }
            }
            else {
                Group elementGroup = getGroup(getElementGroup(group_id));
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

            trackCounter++;

            foreach(Track track in trackValues)
                if(track.id >= trackCounter) trackCounter = track.id + 1;

            return trackCounter;
        }

        public int getUniqueGroupID() {

            groupCounter--;

            foreach(Group grp in groupValues) {
                if(grp.group_id <= groupCounter) groupCounter = grp.group_id - 1;

            }
            return groupCounter;
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

        public Track getTrack(int id) {
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
            foreach(Group grp in groupValues) {
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
                Group grp = groupValues[j];
                for(int i = 0; i < grp.elements.Count; i++) {
                    if(grp.elements[i] == source_id) {
                        grp.elements[i] = new_id;
                        return true;
                    }
                }
            }
            return false;
        }

        private void addTrack(int groupId, Track track) {
            trackValues.Add(track);
            addToGroup(track.id, groupId);
            sortTracks();
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
                    shouldRemoveGroup = false;  // should not remove group as it has been replaced

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
            Group grp = getGroup(group_id);
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
            Group grp = getGroup(group_id);
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
                    else rootGroup.elements.Add(source_id); // add to end if last
                    found = true;
                    break;
                }
            }
            // if not found, search rest of groups
            if(!found) {
                foreach(Group grp in groupValues) {
                    for(int i = 0; i < grp.elements.Count; i++) {
                        if(grp.elements[i] == dest_id) {
                            if(i < grp.elements.Count - 1) grp.elements.Insert(i + 1, source_id);
                            else grp.elements.Add(source_id);   // add to end if last
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
        public Group getGroup(int id) {
            initGroups();
            if(id == 0) return rootGroup;
            int index = getGroupIndex(id);

            if(index == -1 || index >= groupValues.Count) {
                Debug.LogError("Animator: Group id " + id + " not found.");
                return new Group();
            }
            return groupValues[index];
        }

        public void initGroups() {
            if(rootGroup == null) {
                Group g = new Group();
                g.init(0);
                rootGroup = g;
            }
        }

        public int getTrackGroup(int track_id) {
            foreach(int id in rootGroup.elements) {
                if(id == track_id) return 0;
            }
            foreach(Group grp in groupValues) {
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
            foreach(Group grp in groupValues) {
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
                Group grp = getGroup(group_id);
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
                foreach(Group grp in groupValues) {
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

        static int TrackCompare(Track t1, Track t2) {
            if(t1 == t2)
                return 0;
            else if(t1 == null)
                return 1;
            else if(t2 == null)
                return -1;

            return t1.order - t2.order;
        }

        void sortTracks() {
            trackValues.Sort(TrackCompare);
        }

        private struct TrackIndex {
            public List<int> groupIndices;
            public int index;

            public static TrackIndex Create() { return new TrackIndex() { groupIndices=new List<int>(), index=0 }; }
        }

        void TrackGetGroupElementIndexRecursive(Group grp, int id, ref TrackIndex trackIndexOutput) {
            int grpElemInd = grp.getItemIndex(id);
            if(grpElemInd != -1) {
                trackIndexOutput.index = grpElemInd;
                return;
            }

            //grab through sub groups
            for(int grpInd = 0; grpInd < grp.elements.Count; grpInd++) {
                int grpElemId = grp.elements[grpInd];
                if(grpElemId < 0) { //is group?
                    Group subGrp = null;
                    for(int i = 0; i < groupValues.Count; i++) {
                        if(groupValues[i].group_id == grpElemId) {
                            subGrp = groupValues[i];
                            break;
                        }
                    }

                    if(subGrp != null) {
                        TrackGetGroupElementIndexRecursive(subGrp, id, ref trackIndexOutput);
                        if(trackIndexOutput.index != -1) {
                            trackIndexOutput.groupIndices.Add(grpInd);
                            break;
                        }
                    }
                }
            }
        }

        int TrackIdGroupOrderCompare(int t1, int t2) {
            TrackIndex t1Ind = TrackIndex.Create(), t2Ind = TrackIndex.Create();

            TrackGetGroupElementIndexRecursive(rootGroup, t1, ref t1Ind);
            TrackGetGroupElementIndexRecursive(rootGroup, t2, ref t2Ind);

            if(t1Ind.groupIndices.Count == t2Ind.groupIndices.Count) {
                return t1Ind.index - t2Ind.index;
            }
            else if(t1Ind.groupIndices.Count < t2Ind.groupIndices.Count) {
                int t2GroupIndex = t1Ind.groupIndices.Count;
                return t1Ind.index - t2Ind.groupIndices[t2GroupIndex];
            }
            else if(t1Ind.groupIndices.Count > t2Ind.groupIndices.Count) {
                int t1GroupIndex = t2Ind.groupIndices.Count;
                return t1Ind.groupIndices[t1GroupIndex] - t2Ind.index;
            }

            return 0;
        }

        /// <summary>
        /// Sort given tracks based on their order in the groups. This assumes tracks are coming from this take.
        /// </summary>
        public void SortTracksByGroupOrder(List<Track> tracks) {
            tracks.Sort(delegate (Track t1, Track t2) {
                return TrackIdGroupOrderCompare(t1.id, t2.id);
            });
        }

        /// <summary>
        /// Sort given tracks based on their order in the groups. This assumes tracks are coming from this take.
        /// </summary>
        public void SortTrackIdsByGroupOrder(List<int> trackIds) {
            trackIds.Sort(delegate (int t1, int t2) {
                return TrackIdGroupOrderCompare(t1, t2);
            });
        }

        // preview a frame
        public void previewFrame(ITarget itarget, float _frame, bool orientationOnly = false, bool renderStill = true, bool play = false, float playSpeed = 1.0f) {
            // render camera switcher still texture if necessary
            if(renderStill) renderCameraSwitcherStill(itarget, _frame);

            if(orientationOnly) {
                foreach(Track track in trackValues) {
                    if(track is OrientationTrack)// || track is RotationTrack)
                        track.previewFrame(itarget, _frame, frameRate, play, playSpeed);
                }
            }
            else {
                foreach(Track track in trackValues)
                    track.previewFrame(itarget, _frame, frameRate, play, playSpeed);
            }
        }

        /// <summary>
        /// Start up tracks, doing one-time initializations and specific setups
        /// </summary>
        public void PlayStart(ITarget itarget, float _frame, float animScale) {
            foreach(Track track in trackValues) //call to start
                track.PlayStart(itarget, _frame, frameRate, animScale);
        }

        public void Reset(ITarget itarget) {
            foreach(Track track in trackValues) //call to start
                track.Reset(itarget, frameRate);
        }

        /// <summary>
        /// Call this when we are switching take
        /// </summary>
        public void PlaySwitch(ITarget itarget) {
            foreach(Track track in trackValues)
                track.PlaySwitch(itarget);
        }

        public void PlayComplete(ITarget itarget) {
            foreach(Track track in trackValues)
                track.PlayComplete(itarget);
        }

        public void Stop(ITarget itarget) {
            foreach(Track track in trackValues)
                track.Stop(itarget);
        }

        public void Pause(ITarget itarget) {
            foreach(Track track in trackValues)
                track.Pause(itarget);
        }

        public void Resume(ITarget itarget) {
            foreach(Track track in trackValues)
                track.Resume(itarget);
        }

        public void SetAnimScale(ITarget itarget, float scale) {
            foreach(Track track in trackValues)
                track.SetAnimScale(itarget, scale);
        }

        private void renderCameraSwitcherStill(ITarget itarget, float _frame) {
            if(cameraSwitcher == null) return;

            CameraSwitcherTrack.cfTuple tuple = cameraSwitcher.getCameraFadeTupleForFrame(itarget, (int)_frame);
            if(tuple.frame != 0) {

                CameraFade cf = CameraFade.getCameraFade();
                cf.isReset = false;
                // create render texture still
                //bool isPro = PlayerSettings.advancedLicense;
                bool isPro = isProLicense;
                if(!cf.screenTex || cf.shouldUpdateStill || (isPro && cf.cachedStillFrame != tuple.frame)) {
                    if(isPro) {
                        int firstTargetType = (tuple.isReversed ? tuple.type2 : tuple.type1);
                        int secondTargetType = (tuple.isReversed ? tuple.type1 : tuple.type2);
                        if(firstTargetType == 0) {
                            if(cf.screenTex) Object.DestroyImmediate(cf.screenTex);
                            previewFrame(itarget, tuple.frame, false, false);
                            // set top camera
                            //bool isReversed = tuple.isReversed;
                            Camera firstCamera = (tuple.isReversed ? tuple.camera2 : tuple.camera1);



                            Utility.SetTopCamera(firstCamera, cameraSwitcher.getAllCameras(itarget));

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

                            cf.refreshScreenTex(firstCamera);
                            cf.cachedStillFrame = tuple.frame;
                            cf.placeholder = false;

                            if(secondTargetType == 0) {
                                Camera secondCamera = (tuple.isReversed ? tuple.camera1 : tuple.camera2);
                                Utility.SetTopCamera(secondCamera, cameraSwitcher.getAllCameras(itarget));
                            }
                        }

                    }
                    else {
                        cf.clearScreenTex();
                        cf.placeholder = true;
                    }

                }
                cf.useRenderTexture = false;
            }

        }

        public Key[] removeKeysAfter(ITarget itarget, int frame) {
            List<Key> dkeys = new List<Key>();
            bool didDeleteKeys;
            foreach(Track track in trackValues) {
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

        public Key[] removeKeysBefore(ITarget itarget, int frame) {
            List<Key> dkeys = new List<Key>();
            bool didDeleteKeys;
            foreach(Track track in trackValues) {
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
            foreach(Track track in trackValues) {
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
            foreach(Track track in trackValues) {
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
            foreach(Track track in trackValues) {
                if(track.keys.Count > 0) {
                    Key key = track.keys[0];
                    if(key.frame > frame)
                        frame = key.frame;
                    numTrackWithKeys++;
                }
            }
            return numTrackWithKeys > 0 ? frame : 1;
        }

        // returns true if autokey successful
        public bool autoKey(ITarget itarget, Transform obj, int frame) {
            if(!obj) return false;
            bool didKey = false;
            foreach(Track track in trackValues) {
                // for each track, if rotation or translation then autokey
                if(track is TranslationTrack) {
                    if((track as TranslationTrack).autoKey(itarget, obj, frame, frameRate)) {
                        if(!didKey) didKey = true;
                        //track.updateCache();
                    }
                }
                else if(track is RotationTrack) {
                    if((track as RotationTrack).autoKey(itarget, obj, frame, frameRate)) {
                        if(!didKey) didKey = true;
                    }
                }
                else if(track is RotationEulerTrack) {
                    if((track as RotationEulerTrack).autoKey(itarget, obj, frame, frameRate)) {
                        if(!didKey) didKey = true;
                    }
                }
                else if(track is ScaleTrack) {
                    if((track as ScaleTrack).autoKey(itarget, obj, frame, frameRate)) {
                        if(!didKey) didKey = true;
                    }
                }
            }
            return didKey;
        }
#endregion

#region Other Fns

        public void undoRedoPerformed() {
            foreach(Track track in trackValues)
                track.undoRedoPerformed();
        }

        public void maintainTake(ITarget itarget) {
            foreach(Track track in trackValues)
                track.maintainTrack(itarget);
        }

        public void drawGizmos(ITarget itarget, float gizmo_size, bool inPlayMode) {
            foreach(Track track in trackValues)
                track.drawGizmos(itarget, gizmo_size, inPlayMode, selectedFrame, frameRate);
        }

        public bool maintainCaches(ITarget itarget) {
            bool ret = false;
            // re-updates cache if there are null values
            if(trackValues != null) {
                foreach(Track track in trackValues) {
                    bool shouldUpdateCache = false;
                    if(track != null && track.keys != null) {
                        foreach(Key key in track.keys) {
                            //update key's interpolation
                            if(key.easeType == Ease.Unset) {
                                key.interp = Key.Interpolation.None;
                                key.easeType = Ease.Linear;
                            }

                            if(key.version != track.version) {
                                key.version = track.version; //will be set to dirty later
                                shouldUpdateCache = true;
                                break;
                            }
                        }
                        if(shouldUpdateCache) {
                            track.updateCache(itarget);
                            ret = true;
                        }
                    }
                }
            }
            return ret;
        }

        public float getElementsHeight(int group_id, float height_track, float height_track_foldin, float height_group) {
            initGroups();
            float height = 0;
            Group grp = getGroup(group_id);

            if(group_id < 0) {

                height += height_group;
                if(grp.elements.Count <= 0 && grp.foldout)  // "no tracks" label height
                    height += height_group;
            }
            if(group_id == 0 || grp.foldout) {
                foreach(int id in grp.elements) {
                    // group within group
                    if(id < 0) {

                        height += getElementsHeight(id, height_track, height_track_foldin, height_group);
                    }
                    else {
                        Track track = getTrack(id);
                        if(track != null && track.foldout) height += height_track;
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
            Group grp = getGroup(group_id);

            if(group_id < 0) {

                if(group_id == element_id) {
                    found = true;
                    return height;
                }
                height += height_group;
                if(grp.elements.Count <= 0 && grp.foldout)  // "no tracks" label height
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
                            return height;  // return height if element is found
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

        public List<GameObject> getDependencies(ITarget itarget) {
            List<GameObject> ls = new List<GameObject>();

            foreach(Track track in trackValues) {
                ls = ls.Union(track.getDependencies(itarget)).ToList();
            }
            return ls;
        }

        // returns list of GameObjects to not delete due to issues resolving references
        public List<GameObject> updateDependencies(ITarget itarget, List<GameObject> newReferences, List<GameObject> oldReferences) {
            List<GameObject> lsFlagToKeep = new List<GameObject>();
            foreach(Track track in trackValues) {
                lsFlagToKeep = lsFlagToKeep.Union(track.updateDependencies(itarget, newReferences, oldReferences)).ToList();
                track.updateCache(itarget);
            }
            return lsFlagToKeep;
        }
    }
}