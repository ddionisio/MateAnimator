using UnityEngine;
//using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Holoville.HOTween;

//Obsolete

[AddComponentMenu("")]
public class AMTake : MonoBehaviour {
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
    #endregion
}
