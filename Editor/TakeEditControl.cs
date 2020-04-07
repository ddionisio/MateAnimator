using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace M8.Animator.Edit {
    public class TakeEditControl {
        public int selectedTrack = -1;          // currently selected track index
        public int selectedFrame = 1;           // currently selected frame (frame to preview, not necessarily in context selection)
        public int selectedGroup = 0;

        public int playbackSpeedIndex = 2;		// default playback speed x1

        public float startFrame = 1f;               // first frame to render
        public float endFrame = 100f;

        public List<int> contextSelection = new List<int>();    // list of all frames included in the context selection
        public List<int> ghostSelection = new List<int>();      // list of all frames included in the ghost selection
        public List<int> contextSelectionTrackIds = new List<int>();

        public void Reset() {
            contextSelection.Clear();
            ghostSelection.Clear();
            contextSelectionTrackIds.Clear();

            selectedTrack = -1;
            selectedFrame = 1;
            selectedGroup = 0;
            playbackSpeedIndex = 2;

            startFrame = 1f;
            endFrame = 100f;
        }

        // select a track by index
        public void selectTrack(Take take, int index, bool isShiftDown, bool isControlDown) {
            bool isInContextSelection = contextSelectionTrackIds.Contains(index);
            if(!isShiftDown && !isControlDown) {
                if(selectedTrack != index) {
                    selectedTrack = index;
                    if(!isInContextSelection) {
                        // clear context selection
                        contextSelection = new List<int>();
                        contextSelectionTrackIds = new List<int>();
                    }
                }
                if(index > -1) selectGroup(take, take.getTrackGroup(index), false, false, true);
            }

            if(!isInContextSelection)
                contextSelectionTrackIds.Add(index);
            else if(isControlDown && selectedTrack != index && !isShiftDown) {
                contextSelectionTrackIds.Remove(index);
            }
            // select range
            if((selectedTrack != -1 || selectedGroup != 0) && isShiftDown) {
                List<int> range = take.getTrackIDsForRange((selectedTrack != -1 ? selectedTrack : selectedGroup), index);
                foreach(int track_id in range) {
                    if(!contextSelectionTrackIds.Contains(track_id)) contextSelectionTrackIds.Add(track_id);
                }
            }
        }

        public Track getSelectedTrack(Take take) {
            if(selectedTrack == -1) return null;
            int ind = take.getTrackIndex(selectedTrack);
            return ind == -1 || ind >= take.trackValues.Count ? null : take.trackValues[ind];
        }

        public int getTotalSelectedFrames(Take take) {
            int total = 0;
            for(int i = 0; i < contextSelectionTrackIds.Count; i++) {
                int trackId = contextSelectionTrackIds[i];
                Track track = null;
                for(int j = 0; j < take.trackValues.Count; j++) {
                    if(take.trackValues[j].id == trackId) {
                        track = take.trackValues[j];
                        break;
                    }
                }
                if(track != null) {
                    int lastFrame = track.getLastFrame(take.frameRate);
                    if(lastFrame > total)
                        total = lastFrame;
                }
            }

            return total;
        }

        public List<Track> GetContextSelectionTracks(Take take) {
            var retTracks = new List<Track>();

            for(int i = 0; i < take.trackValues.Count; i++) {
                var track = take.trackValues[i];

                for(int j = 0; j < contextSelectionTrackIds.Count; j++) {
                    if(track.id == contextSelectionTrackIds[j]) {
                        retTracks.Add(track);
                        break;
                    }
                }

                //all tracks accounted for, NOTE: track.id should be unique per track
                if(retTracks.Count == contextSelectionTrackIds.Count)
                    break;
            }

            return retTracks;
        }

        public void addGroup(Take take) {
            take.initGroups();
            Group g = new Group();
            g.init(take.getUniqueGroupID());
            take.groupValues.Add(g);
            take.rootGroup.elements.Add(g.group_id);

            // select new group when it has been created
            selectedGroup = g.group_id;
        }

        public void selectGroup(Take take, int group_id, bool isShiftDown, bool isControlDown, bool softSelect = false) {

            if(isShiftDown || isControlDown) {
                contextSelectGroup(take, group_id, isControlDown);
                // select range
                if((selectedTrack != -1 || selectedGroup != 0) && isShiftDown) {
                    List<int> range = take.getTrackIDsForRange((selectedTrack != -1 ? selectedTrack : selectedGroup), group_id);
                    foreach(int track_id in range) {
                        if(!contextSelectionTrackIds.Contains(track_id)) contextSelectionTrackIds.Add(track_id);
                    }
                }
            }
            else if(!softSelect) {
                if(contextSelectionTrackIds.Count == 1) contextSelectionTrackIds = new List<int>();
            }
            selectedGroup = group_id;
        }

        /*public void contextSelectGroup(int group_id, bool deselect) {
	        Group grp = getGroup(group_id);
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

        public void contextSelectGroup(Take take, int group_id, bool isControlDown) {
            Group grp = take.getGroup(group_id);
            int numTracks = 0;
            bool deselect = isControlDown && isGroupSelected(take, group_id, ref numTracks);
            for(int i = 0; i < grp.elements.Count; i++) {
                // select track
                if(grp.elements[i] > 0) {
                    bool isSelected = contextSelectionTrackIds.Contains(grp.elements[i]);
                    if(deselect) {
                        if(isSelected) contextSelectionTrackIds.Remove(grp.elements[i]);
                    }
                    else {
                        if(!isSelected) contextSelectionTrackIds.Add(grp.elements[i]);
                    }
                }
                else {
                    contextSelectGroup(take, grp.elements[i], deselect);
                }
            }
        }

        public bool isGroupSelected(Take take, int group_id, ref int numTracks) {
            Group grp = take.getGroup(group_id);
            for(int i = 0; i < grp.elements.Count; i++) {
                // select track
                if(grp.elements[i] > 0) {
                    if(!contextSelectionTrackIds.Contains(grp.elements[i])) return false;
                    numTracks++;
                }
                else {
                    if(isGroupSelected(take, grp.elements[i], ref numTracks) == false) return false;
                }
            }
            return true;
        }

        public void deleteSelectedGroup(Take take, bool deleteContents) {
            take.deleteGroup(selectedGroup, deleteContents);
            // select root group
            selectedGroup = 0;

        }

        //frames
        // select a frame
        public void selectFrame(Take take, int track, int num, float numFramesToRender, bool isShiftDown, bool isControlDown) {
            selectedFrame = num;
            selectTrack(take, track, isShiftDown, isControlDown);

            if((selectedFrame < startFrame) || (selectedFrame > endFrame)) {
                startFrame = selectedFrame;
                endFrame = startFrame + (int)numFramesToRender - 1;
            }
            take.selectedFrame = selectedFrame;
        }

        public void shiftOutOfBoundsKeysOnSelectedTrack(Take take, ITarget itarget) {
            int offset = getSelectedTrack(take).shiftOutOfBoundsKeys(itarget);
            if(contextSelection.Count <= 0) return;
            for(int i = 0; i < contextSelection.Count; i++) {
                contextSelection[i] += offset;
            }
            // shift all keys on all tracks
            foreach(Track track in take.trackValues) {
                if(track.id == selectedTrack) continue;
                track.offsetKeysFromBy(itarget, 1, offset);
            }
        }

        public void shiftOutOfBoundsKeysOnTrack(Take take, ITarget itarget, Track _track) {
            int offset = _track.shiftOutOfBoundsKeys(itarget);
            if(contextSelection.Count <= 0) return;
            for(int i = 0; i < contextSelection.Count; i++) {
                contextSelection[i] += offset;
            }
            // shift all keys on all tracks
            foreach(Track track in take.trackValues) {
                if(track.id == _track.id) continue;
                track.offsetKeysFromBy(itarget, 0, offset);
            }
        }

        public Key[] removeSelectedKeysFromTrack(Take take, ITarget itarget, int track_id) {
            List<Key> dkeys = new List<Key>();

            bool didDeleteKeys = false;
            Track track = take.getTrack(track_id);
            for(int i = 0; i < track.keys.Count; i++) {
                if(!isFrameInContextSelection(track.keys[i].frame)) continue;
                dkeys.Add(track.keys[i]);
                track.keys.Remove(track.keys[i]);
                i--;
                didDeleteKeys = true;
            }
            if(didDeleteKeys) track.updateCache(itarget);

            return dkeys.ToArray();
        }

        //context/ghost
        #region Context Selection
        private int ghost_selection_total_offset = 0;

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

        public void contextSelectAllFrames(int numFrames) {
            contextSelection = new List<int>();
            contextSelection.Add(1);
            contextSelection.Add(numFrames);
        }

        public bool contextSelectionHasKeys(Take take) {
            Track track = getSelectedTrack(take);
            if(track != null) {
                foreach(Key key in track.keys) {
                    for(int i = 0; i < contextSelection.Count; i += 2) {
                        // if selection start frame > frame, break out of sorted list
                        if(contextSelection[i] > key.frame) break;
                        if(contextSelection[i] <= key.frame && contextSelection[i + 1] >= key.frame) return true;
                    }
                }
            }
            return false;
        }

        public Key[][] getContextSelectionKeyGroupsForTrack(Track track) {
            if(contextSelection.Count == 0)
                return new Key[0][];

            var keyGroups = new Key[contextSelection.Count / 2][];

            for(int i = 0; i < contextSelection.Count; i += 2) {
                var keys = new List<Key>();

                foreach(Key key in track.keys) {
                    if(contextSelection[i] <= key.frame && contextSelection[i + 1] >= key.frame)
                        keys.Add(key);
                }

                keyGroups[i / 2] = keys.ToArray();
            }

            return keyGroups;
        }

        public Key[] getContextSelectionKeysForTrack(Track track) {
            List<Key> keys = new List<Key>();

            if(contextSelection.Count > 0) {
                foreach(Key key in track.keys) {
                    for(int i = 0; i < contextSelection.Count; i += 2) {
                        // if selection start frame > frame, break out of sorted list
                        if(contextSelection[i] > key.frame) break;
                        if(contextSelection[i] <= key.frame && contextSelection[i + 1] >= key.frame) { keys.Add(key); break; }
                    }
                }
            }
            else {
                foreach(Key key in track.keys) {
                    if(key.frame == this.selectedFrame) {
                        keys.Add(key);
                        break;
                    }
                }
            }

            return keys.ToArray();
        }

        /*public Key[] getContextSelectionKeys() {
	        List<Key> keys = new List<Key>();
	        foreach(Key key in getSelectedTrack().keys) {
	            for(int i=0;i<contextSelection.Count;i+=2) {
	                // if selection start frame > frame, break out of sorted list
	                if(contextSelection[i] > key.frame) break;
	                if(contextSelection[i] <= key.frame && contextSelection[i+1] >= key.frame) keys.Add(key);
	            }
	        }
	        return keys.ToArray();
	    }*/

        // offset context selection frames by an amount. can be positive or negative
        //returns keys that are to be deleted
        public Key[] offsetContextSelectionFramesBy(Take take, ITarget itarget, int offset) {
            if(offset == 0) return new Key[0];
            if(contextSelection.Count <= 0) return new Key[0];

            List<Key> rkeys = new List<Key>();
            List<Key> keysToDelete = new List<Key>();

            foreach(int track_id in contextSelectionTrackIds) {
                bool shouldUpdateCache = false;
                Track _track = take.getTrack(track_id);
                foreach(Key key in _track.keys) {
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
                foreach(Key key in keysToDelete) {
                    _track.keys.Remove(key);
                    rkeys.Add(key);
                }
                keysToDelete.Clear();

                // update cache
                if(shouldUpdateCache) {
                    _track.updateCache(itarget);
                }
            }
            // update context selection
            for(int i = 0; i < contextSelection.Count; i++) {
                // move context selection
                contextSelection[i] += offset;
            }
            // clear ghost selection
            ghostSelection = new List<int>();

            return rkeys.ToArray();
        }

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

        public int[] getKeyFramesInGhostSelection(Take take, int startFrame, int endFrame, int track_id) {
            List<int> key_frames = new List<int>();
            if(track_id <= -1) return key_frames.ToArray();
            foreach(Key key in take.getTrack(track_id).keys) {
                if(key.frame + ghost_selection_total_offset < startFrame) continue;
                if(key.frame + ghost_selection_total_offset > endFrame) break;
                if(isFrameInContextSelection(key.frame)) key_frames.Add(key.frame + ghost_selection_total_offset);
            }
            return key_frames.ToArray();
        }
        #endregion
    }
}
