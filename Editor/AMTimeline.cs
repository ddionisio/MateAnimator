using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System;

using Holoville.HOTween.Core.Easing;

public class AMTimeline : EditorWindow {

    [MenuItem("Window/Animator Timeline Editor")]
    static void Init() {
        EditorWindow.GetWindow(typeof(AMTimeline));
    }

    #region Declarations

    public static AMTimeline window = null;

    const BindingFlags methodFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private AnimatorData _aData;
    public AnimatorData aData {
        get {
            return _aData;
        }
        set {
            if(_aData != value) {
				ClearKeysBuffer();
                contextSelectionTracksBuffer.Clear();
                cachedContextSelection.Clear();

                if(_aData != null) {
                    //Debug.Log("previous data: " + _aData.name + " hash: " + _aData.GetHashCode());

                    _aData.isAnimatorOpen = false;

                    if(aData.takes != null) {
                        foreach(AMTake take in aData.takes) {
                            if(take) {
                                take.maintainCaches();

                                foreach(AMTrack track in take.trackValues) {
                                    if(track) {
                                        setDirtyKeys(track);
                                        EditorUtility.SetDirty(track);
                                    }
                                }

                                EditorUtility.SetDirty(take);
                            }
                        }
                    }

                    EditorUtility.SetDirty(_aData);
                }

                _aData = value;

                if(_aData) {
                    if(window != null) {
                        _aData.isAnimatorOpen = true;
                    }

                    if(_aData.takes == null || _aData.takes.Count == 0) {
                        //this is a newly created data

                        // add take
                        _aData.addTake();
                        // save data
                        setDirtyTakes(_aData.takes);
                    }
                    else {
                        _aData.getCurrentTake().maintainTake();	// upgrade take to current version if necessary
                        // save data
                        EditorUtility.SetDirty(_aData);
                        // preview last selected frame
                        if(!isPlayMode && _aData.getCurrentTake()) _aData.getCurrentTake().previewFrame((float)_aData.getCurrentTake().selectedFrame);
                    }

                    indexMethodInfo = -1;	// re-check for methodinfo

                    //Debug.Log("new data: " + _aData.name + " hash: " + _aData.GetHashCode());

                    ReloadOtherWindows();
                }
                //else
                //Debug.Log("no data");
            }
        }
    }// AnimatorData component, holds all data
    public AMOptionsFile oData;
    private Vector2 scrollViewValue;			// current value in scrollview (vertical)
    private string[] playbackSpeed = { "x.25", "x.5", "x1", "x2", "x4" };
    private float[] playbackSpeedValue = { .25f, .5f, 1f, 2f, 4f };
    private float cachedZoom = 0f;
    private float numFramesToRender;			// number of frames to render, based on window size
    private bool isPlaying;						// is preview player playing
    private float playerStartTime;				// preview player start time
    private int playerStartFrame;				// preview player start frame
    public static string[] easeTypeNames = {
		"linear",
        "easeInSine",
        "easeOutSine",
        "easeInOutSine",
        "easeInQuad",
        "easeOutQuad",
        "easeInOutQuad",
        "easeInCubic",
        "easeOutCubic",
        "easeInOutCubic",
        "easeInQuart",
        "easeOutQuart",
        "easeInOutQuart",
        "easeInQuint",
        "easeOutQuint",
        "easeInOutQuint",
        "easeInExpo",
        "easeOutExpo",
        "easeInOutExpo",
        "easeInCirc",
        "easeOutCirc",
        "easeInOutCirc",
        "easeInElastic",
        "easeOutElastic",
        "easeInOutElastic",
        "easeInBack",
        "easeOutBack",
        "easeInOutBack",
        "easeInBounce",
        "easeOutBounce",
        "easeInOutBounce",
		"Custom"
	};
    private string[] wrapModeNames = {
		"Once",	
		"Loop",
		"ClampForever",
		"PingPong"
	};
    public enum MessageBoxType {
        Info = 0,
        Warning = 1,
        Error = 2
    }
    public enum DragType {
        None = -1,
        ContextSelection = 0,
        MoveSelection = 1,
        TimeScrub = 2,
        GroupElement = 3,
        ResizeTrack = 4,
        FrameScrub = 5,
        TimelineScrub = 8,
        ResizeAction = 9,
        ResizeHScrollbarLeft = 10,
        ResizeHScrollbarRight = 11,
        CursorZoom = 12,
        CursorHand = 13
    }
    public enum ElementType {
        None = -1,
        TimeScrub = 0,
        Group = 1,				// dragged to inside group
        GroupOutside = 2,		// dragged to outside group
        Track = 3,
        ResizeTrack = 4,
        Button = 5,
        Other = 6,
        FrameScrub = 7,
        TimelineScrub = 8,
        ResizeAction = 9,
        TimelineAction = 10,
        ResizeHScrollbarLeft = 11,
        ResizeHScrollbarRight = 12,
        CursorZoom = 13,
        HScrollbarThumb = 14,
        CursorHand = 15
    }
    public enum CursorType {
        None = -1,
        Zoom = 1,
        Hand = 2
    }
    [SerializeField]
    public enum Track {
        Translation = 0,
        Rotation = 1,
        Orientation = 2,
        Animation = 3,
        Audio = 4,
        Property = 5,
        Event = 6,
        GOSetActive = 7
    }

    public static string[] TrackNames = new string[] {
		"Translation",
		"Rotation",
		"Orientation",
		"Animation",
		"Audio",
		"Property",
		"Event",
        "GOSetActive"
	};
    // skins
    public static string global_skin = "am_skin_blue";
    private GUISkin skin = null;
    private string cachedSkinName = null;
    // dimensions
    private float margin = 2f;
    private float width_window_minimum = 810;
    private float padding_track = 3f;
    private float width_track = 150f;
    private float width_track_min = 135f;
    private float height_track = 58f;
    private float height_track_foldin = 20f;
    private float height_action_min = 45f;
    //private float width_frame = 30f;
    //private float height_frame = 60f;
    private float width_inspector_open = 250f;
    private float width_inspector_closed = 40f;
    private float width_take_popup = 200f;
    private float height_menu_bar = /*30f*/20f;
    private float height_control_bar = 20f;
    private float width_playback_speed = 43f;
    private float width_playback_controls = 245f;
    private float height_playback_controls = 22f;
    private float height_indicator_offset_y = 33f;		// indicator vertical offset
    private float width_indicator_line = 3f;
    private float width_indicator_head = 11f;
    private float height_indicator_head = 12f;
    private float height_indicator_footer = 20f;
    public static float width_button_delete = 22f;
    private float height_button_delete = 22f;
    private float height_inspector_space = 6f;
    private float height_group = 20f;
    private float height_element_position = 2f;
    private float width_subtrack_space = 15f;
    private float width_scrub_control = 45f;
    private float width_frame_birdseye_min = 7f;
    // dynamic dimensions
    private float current_width_frame;
    private float current_height_frame;
    // colors
    private Color colBirdsEyeFrames = new Color(210f / 255f, 210f / 255f, 210f / 255f, 1f);
    // textures
    private Texture tex_cursor_zoomin;
    private Texture tex_cursor_zoomout;
    private Texture tex_cursor_zoom_blank;
    private Texture tex_cursor_zoom = null;
    private Texture tex_cursor_grab;
    private Texture tex_icon_track;
    private Texture tex_icon_track_hover;
    private Texture tex_icon_group_closed;
    private Texture tex_icon_group_open;
    private Texture tex_icon_group_hover;
    private Texture tex_element_position;
    private Texture texFrKey;
    private Texture texFrSet;
    //private Texture texFrU;
    //private Texture texFrM;
    //private Texture texFrUS; 
    //private Texture texFrMS; 
    //private Texture texFrUG; 
    private Texture texKeyBirdsEye;
    private Texture texIndLine;
    private Texture texIndHead;
    private Texture texProperties;
    private Texture texRightArrow;// inspector right arrow
    private Texture texLeftArrow;	// inspector left arrow
    private Texture[] texInterpl = new Texture[2];
    private Texture texBoxBorder;
    private Texture texBoxRed;
    //private Texture texBoxBlue;
    private Texture texBoxLightBlue;
    private Texture texBoxDarkBlue;
    private Texture texBoxGreen;
    private Texture texBoxPink;
    private Texture texBoxYellow;
    private Texture texBoxOrange;
    private Texture texIconTranslation;
    private Texture texIconRotation;
    private Texture texIconAnimation;
    private Texture texIconAudio;
    private Texture texIconProperty;
    private Texture texIconEvent;
    private Texture texIconOrientation;
    private bool texLoaded = false;

    // temporary variables
    private bool isPlayMode = false; 		// whether the user is in play mode, used to close AMTimeline when in play mode
    private bool isRenamingTake = false;	// true if the user is renaming the current take
    private int isRenamingTrack = -1;		// the track the is user renaming, -1 if not renaming a track
    private int isRenamingGroup = 0;		// the group that the user is renaming, 0 if not renaming a group
    //private int repaintBuffer = 0;
    //private int repaintRefreshRate = 1;		// repaint every x update frames if necessary
    private static List<MethodInfo> cachedMethodInfo = new List<MethodInfo>();
    private List<string> cachedMethodNames = new List<string>();
    private List<Component> cachedMethodInfoComponents = new List<Component>();
    private int updateRateMethodInfoCache = 2;		// update method info cache every x update frames if necessary
    private int updateMethodInfoCacheBuffer = 0;	// temporary value
    private static int cachedIndexMethodInfo = -1;
    private static int indexMethodInfo {
        get { return cachedIndexMethodInfo; }
        set {
            if(cachedIndexMethodInfo != value) {
                cachedIndexMethodInfo = value;
                cacheSelectedMethodParameterInfos();
            }
            cachedIndexMethodInfo = value;
        }
    }
    private static ParameterInfo[] cachedParameterInfos = new ParameterInfo[] { };
    private static Dictionary<string, bool> arrayFieldFoldout = new Dictionary<string, bool>();	// used to store the foldout values for arrays in event methods
    private Vector2 inspectorScrollView = new Vector2(0f, 0f);
    private FieldInfo undoCallback;
    private GenericMenu menu = new GenericMenu(); 			// add track menu
    private GenericMenu menu_drag = new GenericMenu(); 		// add track menu, on drag to window
    private GenericMenu contextMenu = new GenericMenu();	// context selection menu
    private float height_all_tracks = 0f;

    // context selection variables
    private bool isControlDown = false;
    private bool isShiftDown = false;
    private bool isSpaceBarDown = false;
    private bool isDragging = false;
    private int dragType = -1;
    private bool cachedIsDragging = false;				// used to determine dragging is started or stopped
    private float scrubSpeed = 0f;
    private int startDragFrame = 0;
    private int ghostStartDragFrame = 0;	// temporary value used to drag the ghost selection
    private int endDragFrame = 0;
    private int contextMenuFrame = 0;
    //private int contextSelectionTrack = 0;
    private Vector2 contextSelectionRange = new Vector2(0f, 0f);			// holds the first and last frame of the copied selection
    //private List<AMKey> contextSelectionKeysBuffer = new List<AMKey>();	// stores the copied context selection keys
    private List<List<AMKey>> contextSelectionKeysBuffer = new List<List<AMKey>>();
    private List<AMTrack> contextSelectionTracksBuffer = new List<AMTrack>();
    private List<int> cachedContextSelection = new List<int>();			// cache context selection when user copies, used for paste
    private bool isChangingTimeControl = false;
    private bool isChangingFrameControl = false;
    private int startScrubFrame = 0;
    private Vector2 startScrubMousePosition = new Vector2(0f, 0f);
    private Vector2 startZoomMousePosition = new Vector2(0f, 0f);
    private Vector2 zoomDirectionMousePosition = new Vector2(0f, 0f);
    private Vector2 cachedZoomMousePosition = new Vector2(0f, 0f);
    private Vector2 endHandMousePosition = new Vector2(0f, 0f);
    private int justFinishedHandDragTicker = 0;
    private int handDragAccelaration = 0;
    private bool didPeakZoom = false;
    private bool wasZoomingIn = false;
    private float startZoomValue = 0f;
    private int startZoomXOverFrame = 0;
    private float startResize_width_track = 0f;
    private int mouseOverFrame = 0;					// the frame number that the mouse X and Y is over, 0 if one
    private int mouseXOverFrame = 0;				// the frame number that the mouse X is over, 0 if none
    private int mouseOverTrack = -1;				// mouse over frame track, -1 if no track
    private int mouseOverElement = -1;
    private int mouseXOverHScrollbarFrame = 0;
    private Vector2 mouseOverGroupElement = new Vector2(-1, -1);	// group_id, track id
    private bool mouseOverSelectedFrame = false;
    private Vector2 currentMousePosition = new Vector2(0f, 0f);
    private Vector2 draggingGroupElement = new Vector2(-1, -1);
    private int draggingGroupElementType = -1;
    private bool mouseAboveGroupElements = false;	// is mouse above group elements? used to determine whether a dropped group element should be placed in the first position
    private int ticker = 0;
    private int tickerSpeed = 50;
    private float scrollAmountVertical = 0f;
    private List<GameObject> objects_window = new List<GameObject>();
    private int startResizeActionFrame = -1;
    private int resizeActionFrame = -1;
    private int endResizeActionFrame = -1;
    private float[] arrKeyRatiosLeft;
    private float[] arrKeyRatiosRight;
    AMKey[] arrKeysLeft;
    AMKey[] arrKeysRight;
    private bool justStartedHandGrab = false;
    //double click variables
    private double doubleClickTime = 0.3f;
    private double doubleClickCachedTime = 0f;
    private string doubleClickElementID = null;
    private bool cursorZoom = false;
    private bool cursorHand = false;
    private int tooltipTicker = 0;
    private int tooltipTime = 70;
    private string lastTooltip;
    private string tooltip = "";
    private bool showTooltip = false;
    //private	int cachedGameObjectCount = 0;
    //private int cachedDependenciesCount = 0;
    public static bool shouldCheckDependencies = false;
    // keyboard config
    //KeyCode key_zoom = KeyCode.Z;
    private float height_event_parameters = 0f;

	private GameObject mTempHolder;

    #endregion

    #region Main

    void OnEnable() {
        if(!texLoaded) {
            tex_cursor_zoomin = AMEditorResource.LoadEditorTexture("am_cursor_zoomin");
            tex_cursor_zoomout = AMEditorResource.LoadEditorTexture("am_cursor_zoomout");
            tex_cursor_zoom_blank = AMEditorResource.LoadEditorTexture("am_cursor_zoom_blank");
            tex_cursor_zoom = null;
            tex_cursor_grab = AMEditorResource.LoadEditorTexture("am_cursor_grab");
            tex_icon_track = AMEditorResource.LoadEditorTexture("am_icon_track");
            tex_icon_track_hover = AMEditorResource.LoadEditorTexture("am_icon_track_hover");
            tex_icon_group_closed = AMEditorResource.LoadEditorTexture("am_icon_group_closed");
            tex_icon_group_open = AMEditorResource.LoadEditorTexture("am_icon_group_open");
            tex_icon_group_hover = AMEditorResource.LoadEditorTexture("am_icon_group_hover");
            tex_element_position = AMEditorResource.LoadEditorTexture("am_element_position");
            texFrKey = AMEditorResource.LoadEditorTexture("am_key");
            texFrSet = AMEditorResource.LoadEditorTexture("am_frame_set");
            //texFrU = AMEditorResource.LoadTexture("am_frame");
            //texFrM = AMEditorResource.LoadTexture("am_frame-m");
            //texFrUS = AMEditorResource.LoadTexture("am_frame-s"); 
            //texFrMS = AMEditorResource.LoadTexture("am_frame-m-s"); 
            //texFrUG = AMEditorResource.LoadTexture("am_frame-g"); 
            texKeyBirdsEye = AMEditorResource.LoadEditorTexture("am_key_birdseye");
            texIndLine = AMEditorResource.LoadEditorTexture("am_indicator_line");
            texIndHead = AMEditorResource.LoadEditorTexture("am_indicator_head");
            texProperties = AMEditorResource.LoadEditorTexture("am_information");
            texRightArrow = AMEditorResource.LoadEditorTexture("am_nav_right");// inspector right arrow
            texLeftArrow = AMEditorResource.LoadEditorTexture("am_nav_left");	// inspector left arrow
            texInterpl[0] = AMEditorResource.LoadEditorTexture("am_interpl_curve"); texInterpl[1] = AMEditorResource.LoadEditorTexture("am_interpl_linear");
            texBoxBorder = AMEditorResource.LoadEditorTexture("am_box_border");
            texBoxRed = AMEditorResource.LoadEditorTexture("am_box_red");
            //texBoxBlue = AMEditorResource.LoadTexture("am_box_blue");
            texBoxLightBlue = AMEditorResource.LoadEditorTexture("am_box_lightblue");
            texBoxDarkBlue = AMEditorResource.LoadEditorTexture("am_box_darkblue");
            texBoxGreen = AMEditorResource.LoadEditorTexture("am_box_green");
            texBoxPink = AMEditorResource.LoadEditorTexture("am_box_pink");
            texBoxYellow = AMEditorResource.LoadEditorTexture("am_box_yellow");
            texBoxOrange = AMEditorResource.LoadEditorTexture("am_box_orange");
            texIconTranslation = AMEditorResource.LoadEditorTexture("am_icon_translation");
            texIconRotation = AMEditorResource.LoadEditorTexture("am_icon_rotation");
            texIconAnimation = AMEditorResource.LoadEditorTexture("am_icon_animation");
            texIconAudio = AMEditorResource.LoadEditorTexture("am_icon_audio");
            texIconProperty = AMEditorResource.LoadEditorTexture("am_icon_property");
            texIconEvent = AMEditorResource.LoadEditorTexture("am_icon_event");
            texIconOrientation = AMEditorResource.LoadEditorTexture("am_icon_orientation");

            texLoaded = true;
        }

        this.title = "Animator";
        this.minSize = new Vector2(width_track + width_playback_controls + width_inspector_open + 70f, 190f);
        window = this;
        //this.wantsMouseMove = true;
        // find component
        if(!aData && !EditorApplication.isPlayingOrWillChangePlaymode) {
            GameObject go = Selection.activeGameObject;
            if(go && PrefabUtility.GetPrefabType(go) != PrefabType.Prefab) {
                aData = go.GetComponent<AnimatorData>();
            }
        }

        oData = AMOptionsFile.loadFile();
        // set default current dimensions of frames
        //current_width_frame = width_frame;
        //current_height_frame = height_frame;
        // set is playing to false
        isPlaying = false;

        // add track menu
        buildAddTrackMenu();

        // playmode callback
        EditorApplication.playmodeStateChanged += OnPlayMode;

        // check for pro license
        AMTake.isProLicense = PlayerSettings.advancedLicense;

        //autoRepaintOnSceneChange = true;

		mTempHolder = new GameObject();
		mTempHolder.hideFlags = HideFlags.HideAndDontSave;
    }
    void OnDisable() {
        EditorApplication.playmodeStateChanged -= OnPlayMode;

        window = null;
        if(aData && aData.getCurrentTake() != null) {
            // stop audio if it's playing
            aData.getCurrentTake().stopAudio();

            // preview first frame
            aData.getCurrentTake().previewFrame(1f);

            aData = null;

            // reset property select track
            //aData.propertySelectTrack = null;
        }
    }
	void ClearKeysBuffer() {
		foreach(List<AMKey> keys in contextSelectionKeysBuffer) {
			foreach(AMKey key in keys)
				DestroyImmediate(key);
		}
		contextSelectionKeysBuffer.Clear();
	}
    void ShadyRefresh() {
		ClearKeysBuffer();
        contextSelectionTracksBuffer.Clear();
        cachedContextSelection.Clear();

        GameObject go = _aData.gameObject;
        _aData.isAnimatorOpen = false;
        _aData = null;
        Selection.activeGameObject = go;
    }

    void OnHierarchyChange() {
        if(isPlayMode) return;

        if(_aData && !_aData.CheckIntegrity()) {
            ShadyRefresh();
        }
    }
    void OnAutoKey(AMTrack track, AMKey key) {
        if(key != null) {
            Undo.RegisterCreatedObjectUndo(key, "Auto Key");
		}
    }
    void Update() {
        isPlayMode = EditorApplication.isPlayingOrWillChangePlaymode;

        if(isPlayMode) return;
        // drag logic
        if(!isPlaying) {

            processDragLogic();
            // show warning for lost references
            /*if(oData.showWarningForLostReferences) {
                int newGameObjectCount = GameObject.FindObjectsOfType(typeof(GameObject)).Length;
                int newDependencyCount = aData.getDependencies().Count;
                if(newGameObjectCount < cachedGameObjectCount && shouldCheckDependencies) {
                    if(newDependencyCount < cachedDependenciesCount) {
                        int total = (cachedDependenciesCount-newDependencyCount);
                        Debug.LogWarning("Animator: "+total+" GameObject reference"+(total > 1 ? "s" : "")+" lost. Undo to avoid possible issues.");
                    }
                }
                cachedDependenciesCount = newDependencyCount;
                cachedGameObjectCount = newGameObjectCount;
                if(!shouldCheckDependencies) shouldCheckDependencies = true;
            }*/
            // show or hide tooltip
            if(tooltip != "" && lastTooltip == tooltip && !showTooltip) {
                tooltipTicker++;
                if(tooltipTicker >= tooltipTime) {
                    showTooltip = true;
                    tooltipTicker = 0;
                }
            }
            else if(showTooltip && lastTooltip != tooltip) {
                showTooltip = false;
            }
            lastTooltip = tooltip;
        }
        /*if(repaintBuffer>0) {
            repaintBuffer--;
            this.Repaint();	
        }*/


        // if preview is playing
        if(isPlaying || dragType == (int)DragType.TimeScrub || dragType == (int)DragType.FrameScrub) {
            float timeRunning = Time.realtimeSinceStartup - playerStartTime;
            // determine current frame
            float curFrame = aData.getCurrentTake().selectedFrame;
            // if scrubbing
            if(dragType == (int)DragType.TimeScrub || dragType == (int)DragType.FrameScrub) {
                if(scrubSpeed < 0) scrubSpeed *= 5;
                curFrame += Mathf.CeilToInt(scrubSpeed);
                curFrame = Mathf.Clamp(curFrame, 1, aData.getCurrentTake().numFrames);
            }
            else {
                // determine speed
                float speed = (float)aData.getCurrentTake().frameRate * playbackSpeedValue[aData.getCurrentTake().playbackSpeedIndex];
                curFrame = playerStartFrame + timeRunning * speed;
                if(Mathf.FloorToInt(curFrame) > aData.getCurrentTake().numFrames) {
                    // loop
                    playerStartTime = Time.realtimeSinceStartup;
                    curFrame = curFrame - aData.getCurrentTake().numFrames;
                    playerStartFrame = Mathf.FloorToInt(curFrame);
                    if(playerStartFrame <= 0) playerStartFrame = 1;
                    // stop audio
                    aData.getCurrentTake().stopAudio();
                }
            }
            // select the appropriate frame
            if(Mathf.FloorToInt(curFrame) != aData.getCurrentTake().selectedFrame) {
                aData.getCurrentTake().selectFrame(aData.getCurrentTake().selectedTrack, Mathf.FloorToInt(curFrame), numFramesToRender, false, false);
                if(dragType != (int)DragType.TimeScrub && dragType != (int)DragType.FrameScrub) {
                    // sample audio
                    aData.getCurrentTake().sampleAudioAtFrame(Mathf.FloorToInt(curFrame), playbackSpeedValue[aData.getCurrentTake().playbackSpeedIndex]);
                }
                this.Repaint();
            }
            aData.getCurrentTake().previewFrame(curFrame);
        }
        else {
            // autokey
            if(!isDragging && aData != null && aData.autoKey) {
				AMTake take = aData.getCurrentTake();

				//NOTE: may need to selectively gather which ones to record if there are too many tracks, for now this is guaranteed
				MonoBehaviour[] dats = getKeysAndTracks(take);
				Undo.RecordObjects(dats, "Auto Key");

                bool autoKeyMade = aData.getCurrentTake().autoKey(Selection.activeTransform, aData.getCurrentTake().selectedFrame, OnAutoKey);

                if(autoKeyMade) {
                    // preview frame, update orientation only
                    aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame, true);
                    
					foreach(MonoBehaviour dat in dats)
						EditorUtility.SetDirty(dat);

                    // repaint
                    this.Repaint();
                }
            }
            // process property if it has been selected
            //processSelectProperty();
            // update methodinfo cache if necessary, used for event track inspector
            processUpdateMethodInfoCache();
        }
    }
    void OnSelectionChange() {
        if(!aData) {
            if(Selection.activeGameObject && PrefabUtility.GetPrefabType(Selection.activeGameObject) != PrefabType.Prefab) {
                AnimatorData newDat = Selection.activeGameObject.GetComponent<AnimatorData>();
                if(newDat != aData) {
                    aData = newDat;
                    Repaint();
                }
            }
        }
    }
    void OnGUI() {
        /*if(Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout) {
            Debug.Log("event type: " + Event.current.type);
        }*/
        if(!oData) {
            oData = AMOptionsFile.loadFile();
        }

        AMTimeline.loadSkin(oData, ref skin, ref cachedSkinName, position);
        if(EditorApplication.isPlayingOrWillChangePlaymode) {
            this.ShowNotification(new GUIContent("Play Mode"));
            return;
        }
        if(EditorApplication.isCompiling) {
            this.ShowNotification(new GUIContent("Code Compiling"));
            return;
        }
        #region no data component
        if(!aData) {
            // recheck for component
            GameObject go = Selection.activeGameObject;
            if(go) {
                if(PrefabUtility.GetPrefabType(go) != PrefabType.Prefab) {
                    aData = go.GetComponent<AnimatorData>();
                }
                else
                    go = null;
            }
            if(!aData) {
                // no data component message
                if(go)
                    MessageBox("Animator requires an AnimatorData component in your game object.", MessageBoxType.Info);
                else
                    MessageBox("Animator requires an AnimatorData in scene.", MessageBoxType.Info);

                if(GUILayout.Button(go ? "Add Component" : "Create AnimatorData")) {
                    // create component
                    if(!go) go = new GameObject("AnimatorData");
                    aData = go.AddComponent<AnimatorData>();
                }
            }

            if(!aData) //still no data
                return;
        }
        /*else {
            if(!aData.CheckNulls()) {
                aData = null;
                Repaint();
                return;
            }
        }*/

        if(!aData.getCurrentTake()) { Repaint(); return; } //????

        //retain animator open, this is mostly when recompiling AnimatorData
        aData.isAnimatorOpen = true;

        #endregion
        #region window resize
        if(!oData.ignoreMinSize && (position.width < width_window_minimum)) {
            MessageBox("Window is too small! Animator requires a width of at least " + width_window_minimum + " pixels to function correctly.", MessageBoxType.Warning);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Ignore (Not Recommended)")) {
                oData.ignoreMinSize = true;
                // save
                EditorUtility.SetDirty(aData);
                // repaint
                this.Repaint();
            }
            if(GUILayout.Button("Resize")) {
                Rect rectDimensions = position;
                rectDimensions.width = width_window_minimum + 1f;
                position = rectDimensions;
                GUIUtility.ExitGUI();
            }

            GUILayout.EndHorizontal();
            return;
        }
        #endregion

        if(tickerSpeed <= 0) tickerSpeed = 1;
        ticker = (ticker + 1) % tickerSpeed;
        EditorGUIUtility.LookLikeControls();
        // reset mouse over element
        mouseOverElement = (int)ElementType.None;
        mouseOverFrame = 0;
        mouseXOverFrame = 0;
        mouseOverTrack = -1;
        mouseOverGroupElement = new Vector2(0, 0);
        tooltip = "";
        int difference = 0;

        //if(oData.disableTimelineActions) current_height_frame = height_track;
        //else current_height_frame = height_frame;
        if(oData.disableTimelineActions) height_action_min = 0f;
        else height_action_min = 45f;

        #region temporary variables
        Rect rectWindow = new Rect(0f, 0f, position.width, position.height);
        Event e = Event.current;
        // get global mouseposition
        Vector2 globalMousePosition = getGlobalMousePosition(e);
        // resize track
        if(dragType == (int)DragType.ResizeTrack) {
            aData.width_track = startResize_width_track + e.mousePosition.x - startScrubMousePosition.x;
        }
        width_track = Mathf.Clamp(aData.width_track, width_track_min, position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed) - width_playback_controls - 70f);
        if(aData.width_track != width_track) aData.width_track = width_track;

        bool compact = ((oData.ignoreMinSize) && (position.width < width_window_minimum)); // when true, display compact GUI
        currentMousePosition = e.mousePosition;
        bool clickedZoom = false;
        #endregion
        #region drag logic events
        bool wasDragging = false;
        if(e.type == EventType.mouseDrag && EditorWindow.mouseOverWindow == this) {
            isDragging = true;
        }
        else if((dragType == (int)DragType.CursorZoom && EditorWindow.mouseOverWindow != this) || e.type == EventType.mouseUp || /*EditorWindow.mouseOverWindow!=this*/Event.current.rawType == EventType.MouseUp /*|| e.mousePosition.y < 0f*/) {
            if(isDragging) {
                wasDragging = true;
                isDragging = false;
            }
        }
        #endregion
        #region keyboard events
        if(e.Equals(Event.KeyboardEvent("[enter]")) || e.Equals(Event.KeyboardEvent("return"))) {
            // apply renaming when pressing enter
            cancelTextEditting();
            if(isChangingTimeControl) isChangingTimeControl = false;
            if(isChangingFrameControl) isChangingFrameControl = false;
            // deselect keyboard focus
            GUIUtility.keyboardControl = 0;
            GUIUtility.ExitGUI();
        }
        // check if control or shift are down
        isControlDown = e.control || e.command;
        isShiftDown = e.shift;
        if(e.type == EventType.keyDown && e.keyCode == KeyCode.Space) isSpaceBarDown = true;
        else if(e.type == EventType.keyUp && e.keyCode == KeyCode.Space) isSpaceBarDown = false;
        #endregion
        #region set cursor
        int customCursor = (int)CursorType.None;
        bool showCursor = true;
        if(!isRenamingTake && isRenamingGroup >= 0 && isRenamingTrack <= -1 && (dragType == (int)DragType.CursorHand || (!cursorZoom && isSpaceBarDown && EditorWindow.mouseOverWindow == this))) {
            cursorHand = true;
            showCursor = false;
            customCursor = (int)CursorType.Hand;
            mouseOverElement = (int)ElementType.CursorHand;
            // unused button to catch clicks
            GUI.Button(rectWindow, "", "label");
        }
        else if(dragType == (int)DragType.CursorZoom || (!cursorHand && e.alt && EditorWindow.mouseOverWindow == this)) {
            cursorZoom = true;
            showCursor = false;
            customCursor = (int)CursorType.Zoom;
            if(!isDragging) {
                if(isControlDown) tex_cursor_zoom = tex_cursor_zoomout;
                else tex_cursor_zoom = tex_cursor_zoomin;
            }
            mouseOverElement = (int)ElementType.CursorZoom;
            if(!wasDragging) {
                if(GUI.Button(rectWindow, "", "label")) {
                    if(isControlDown) {
                        if(aData.zoom < 1f) {
                            aData.zoom += 0.2f;
                            if(aData.zoom > 1f) aData.zoom = 1f;
                            clickedZoom = true;
                        }
                    }
                    else {
                        if(aData.zoom > 0f) {
                            aData.zoom -= 0.2f;
                            if(aData.zoom < 0f) aData.zoom = 0f;
                            clickedZoom = true;
                        }
                    }

                }
            }
        }
        else {
            if(!showCursor) showCursor = true;
            cursorHand = false;
            cursorZoom = false;
        }
        if(Screen.showCursor != showCursor) {
            Screen.showCursor = showCursor;
        }
        if(isRenamingTake || isRenamingTrack != -1 || isRenamingGroup < 0) EditorGUIUtility.AddCursorRect(rectWindow, MouseCursor.Text);
        else if(dragType == (int)DragType.TimeScrub || dragType == (int)DragType.FrameScrub || dragType == (int)DragType.MoveSelection) EditorGUIUtility.AddCursorRect(rectWindow, MouseCursor.SlideArrow);
        else if(dragType == (int)DragType.ResizeTrack || dragType == (int)DragType.ResizeAction || dragType == (int)DragType.ResizeHScrollbarLeft || dragType == (int)DragType.ResizeHScrollbarRight) EditorGUIUtility.AddCursorRect(rectWindow, MouseCursor.ResizeHorizontal);
        #endregion
        #region calculations
        processHandDragAcceleration();
        // calculate number of frames to render
        calculateNumFramesToRender(clickedZoom, e);
        //current_height_frame = (oData.disableTimelineActions ? height_track : height_frame);
        // if is playing, disable all gui elements
        GUI.enabled = !(isPlaying);
        // if selected frame is out of range
        if(aData.getCurrentTake().selectedFrame > aData.getCurrentTake().numFrames) {
            // select last frame
            timelineSelectFrame(aData.getCurrentTake().selectedTrack, aData.getCurrentTake().numFrames);
        }
        // get number of tracks in current take, use for tracks and keys, disabling zoom slider
        int trackCount = aData.getCurrentTake().getTrackCount();
        #endregion
        #region menu bar
        GUIStyle styleLabelMenu = new GUIStyle(EditorStyles.toolbarButton);
        styleLabelMenu.normal.background = null;
        //GUI.color = new Color(190f/255f,190f/255f,190f/255f,1f);
        GUI.DrawTexture(new Rect(0f, 0f, position.width, height_menu_bar - 2f), EditorStyles.toolbar.normal.background);
        //GUI.color = Color.white;
        #region select name
        GUIContent selectLabel = new GUIContent(aData.gameObject.name);
        Vector2 selectLabelSize = EditorStyles.toolbarButton.CalcSize(selectLabel);
        Rect rectSelectLabel = new Rect(margin, 0f, selectLabelSize.x, height_button_delete);

        if(GUI.Button(rectSelectLabel, selectLabel, EditorStyles.toolbarButton)) {
            EditorGUIUtility.PingObject(aData.gameObject);
        }
        #endregion

        #region options button
        Rect rectBtnOptions = new Rect(rectSelectLabel.x + rectSelectLabel.width + margin, 0f, 60f, height_button_delete);
        if(GUI.Button(rectBtnOptions, "Options", EditorStyles.toolbarButton)) {
            EditorWindow windowOptions = ScriptableObject.CreateInstance<AMOptions>();
            //windowOptions.Show();
            windowOptions.ShowUtility();
            //EditorWindow.GetWindow (typeof (AMOptions));
        }
        if(rectBtnOptions.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) mouseOverElement = (int)ElementType.Button;
        #endregion
        #region refresh button
        bool doRefresh = false;
        Rect rectBtnCodeView = new Rect(rectBtnOptions.x + rectBtnOptions.width + margin, rectBtnOptions.y, 80f, rectBtnOptions.height);
        if(GUI.Button(rectBtnCodeView, "Refresh", EditorStyles.toolbarButton)) {
            //EditorWindow.GetWindow(typeof(AMCodeView));
            doRefresh = true;
        }
        if(rectBtnCodeView.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
            mouseOverElement = (int)ElementType.Button;
        }
        #endregion
        #region take popup, change take or create new take
        Rect rectLabelCurrentTake = new Rect(rectBtnCodeView.x, rectBtnCodeView.y, rectBtnCodeView.width, rectBtnCodeView.height);
        if(!compact) {
            rectLabelCurrentTake = new Rect(rectBtnCodeView.x + rectBtnCodeView.width + margin, rectBtnCodeView.y, 80f, rectBtnCodeView.height);
            GUI.Label(rectLabelCurrentTake, "Current Take:", styleLabelMenu);
        }
        Rect rectTakePopup = new Rect(rectLabelCurrentTake.x + rectLabelCurrentTake.width + margin, rectLabelCurrentTake.y/*+3f*/, width_take_popup, 20f);
        // if renaming take, show textfield
        if(isRenamingTake) {
            GUI.SetNextControlName("RenameTake");
            Rect rectRenameTake = new Rect(rectTakePopup);
            rectRenameTake.x += 4f;
            rectRenameTake.width -= 4f;
            rectRenameTake.y += 3f;
            aData.getCurrentTake().name = GUI.TextField(rectRenameTake, aData.getCurrentTake().name, EditorStyles.toolbarTextField);
            GUI.FocusControl("RenameTake");
        }
        else {
            // show popup
            if(aData.setCurrentTakeValue(EditorGUI.Popup(rectTakePopup, aData.currentTake, aData.getTakeNames(), EditorStyles.toolbarPopup))) {
                // take changed

                // reset code view dictionaries
                AMCodeView.resetTrackDictionary();
                // if not creating new take
                if(aData.currentTake < aData.takes.Count) {
                    // select current frame
                    timelineSelectFrame(aData.getCurrentTake().selectedTrack, aData.getCurrentTake().selectedFrame);
                    // save data
                    EditorUtility.SetDirty(aData);
                }
            }
        }
        #endregion
        #region rename take button
        Texture texRenameTake;
        if(isRenamingTake) texRenameTake = getSkinTextureStyleState("accept").background;
        else texRenameTake = getSkinTextureStyleState("rename").background;
        Rect rectBtnRenameTake = new Rect(rectTakePopup.x + rectTakePopup.width + margin, rectLabelCurrentTake.y, width_button_delete, height_button_delete);
        // button
        if(GUI.Button(rectBtnRenameTake, new GUIContent(texRenameTake, (isRenamingTake ? "Accept" : "Rename Take")),/*GUI.skin.GetStyle("ButtonImage")*/EditorStyles.toolbarButton)) {
            if(!isRenamingTake) Undo.RecordObject(aData.getCurrentTake(), "Rename Take");
            GUIUtility.keyboardControl = 0;
            cancelTextEditting(true);	// toggle isRenamingTake
			EditorUtility.SetDirty(aData.getCurrentTake());
        }
        if(rectBtnRenameTake.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) mouseOverElement = (int)ElementType.Button;
        #endregion
        #region delete take button
        Rect rectBtnDeleteTake = new Rect(rectBtnRenameTake.x + rectBtnRenameTake.width + margin, rectBtnRenameTake.y, width_button_delete, height_button_delete);
        if(GUI.Button(rectBtnDeleteTake, new GUIContent("", "Delete Take"),/*GUI.skin.GetStyle("ButtonImage")*/EditorStyles.toolbarButton)) {
			AMTake take = aData.getCurrentTake();

			if((EditorUtility.DisplayDialog("Delete Take", "Are you sure you want to delete take '" + take.name + "'?", "Delete", "Cancel"))) {
				string label = "Delete Take: "+take.name;
				Undo.RegisterCompleteObjectUndo(aData, label);

				if(aData.takes.Count == 1) {
					Undo.RecordObject(take, label);

					MonoBehaviour[] behaviours = getKeysAndTracks(take);

					//just delete the tracks and keys
					foreach(MonoBehaviour b in behaviours)
						Undo.DestroyObjectImmediate(b);

					take.RevertToDefault();

					EditorUtility.SetDirty(take);
				}
				else {
					if(take == aData.playOnStart)
						aData.playOnStart = null;

					if(aData.currentTake > 0)
						aData.currentTake--;

					List<AMTake> nTakes = new List<AMTake>(aData.takes);
					nTakes.Remove(take);
					aData.takes = nTakes;

					MonoBehaviour[] behaviours = getKeysAndTracks(take);
					foreach(MonoBehaviour b in behaviours)
						Undo.DestroyObjectImmediate(b);

					Undo.DestroyObjectImmediate(take);
				}

                AMCodeView.resetTrackDictionary();
                // save data
                EditorUtility.SetDirty(aData);
            }
        }
        if(!GUI.enabled) GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0.25f);
        GUI.DrawTexture(new Rect(rectBtnDeleteTake.x + (rectBtnDeleteTake.height - 10f) / 2f, rectBtnDeleteTake.y + (rectBtnDeleteTake.width - 10f) / 2f - 2f, 10f, 10f), (getSkinTextureStyleState((GUI.enabled && rectBtnDeleteTake.Contains(e.mousePosition) ? "delete_hover" : "delete")).background));
        if(rectBtnDeleteTake.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) mouseOverElement = (int)ElementType.Button;
        if(GUI.color.a < 1f) GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 1f);
        #endregion
        #region Create/Duplicate Take
		if(aData.currentTake == aData.takes.Count) {
			isRenamingTake = false;
			cancelTextEditting();

			aData.currentTake = aData.takes.Count - 1; // decrement for undo

			string label = "New Take";

			Undo.RecordObject(aData, label);

			aData.currentTake = aData.takes.Count;
			
			AMTake newTake = aData.addTake();
			Undo.RegisterCreatedObjectUndo(newTake, label);
			
			// save data
			EditorUtility.SetDirty(aData);
			// refresh component
			refreshGizmos();
		}
        else if(aData.currentTake == aData.takes.Count + 1) {
			isRenamingTake = false;
			cancelTextEditting();

            aData.currentTake = aData.takes.Count - 1; // decrement for undo
                        
			string label = "New Duplicate Take";

			Undo.RecordObject(aData, label);

			AMTake prevTake = aData.getPreviousTake(); //if(takes == null || currentTake >= takes.Count) return null;
									            
            if(prevTake != null) {
                List<UnityEngine.Object> ret = aData.duplicateTake(prevTake);
                foreach(UnityEngine.Object newObj in ret)
					Undo.RegisterCreatedObjectUndo(newObj, label);
            }
            else {
                AMTake newTake = aData.addTake();
				Undo.RegisterCreatedObjectUndo(newTake, label);
            }

            aData.currentTake = aData.takes.Count - 1;

            // save data
            EditorUtility.SetDirty(aData);
        }
        #endregion
        #region play on start button
        Rect rectBtnPlayOnStart = new Rect(rectBtnDeleteTake.x + rectBtnDeleteTake.width + margin, rectBtnDeleteTake.y, width_button_delete, height_button_delete);
        bool isPlayOnStart = aData.playOnStart == aData.getCurrentTake();
        GUIStyle styleBtnPlayOnStart = new GUIStyle(/*GUI.skin.GetStyle("ButtonImage")*/EditorStyles.toolbarButton);
        if(isPlayOnStart) {
            styleBtnPlayOnStart.normal.background = styleBtnPlayOnStart.onNormal.background;
            styleBtnPlayOnStart.hover.background = styleBtnPlayOnStart.onNormal.background;
        }
        if(GUI.Button(rectBtnPlayOnStart, new GUIContent(getSkinTextureStyleState("playonstart").background, "Play On Start"), styleBtnPlayOnStart)) {
            if(!isPlayOnStart) aData.playOnStart = aData.getCurrentTake();
            else aData.playOnStart = null;
            EditorUtility.SetDirty(aData);
        }
        #endregion
        #region settings
        Rect rectLabelSettings = new Rect(rectBtnPlayOnStart.x + rectBtnPlayOnStart.width + margin, rectBtnPlayOnStart.y, 200f, rectLabelCurrentTake.height);

        if(compact) {
            rectLabelSettings.width = GUI.skin.label.CalcSize(new GUIContent("Settings")).x;
            GUI.Label(rectLabelSettings, "Settings", styleLabelMenu);
        }
        else {
            string strSettings = "Settings: " + aData.getCurrentTake().numFrames + " Frames; " + aData.getCurrentTake().frameRate + " Fps";
            rectLabelSettings.width = GUI.skin.label.CalcSize(new GUIContent(strSettings)).x;
            GUI.Label(rectLabelSettings, strSettings, styleLabelMenu);
        }
        Rect rectBtnModify = new Rect(rectLabelSettings.x + rectLabelSettings.width + margin, rectLabelSettings.y, 60f, rectBtnOptions.height);
        if(GUI.Button(rectBtnModify, "Modify", EditorStyles.toolbarButton)) {
            EditorWindow windowSettings = ScriptableObject.CreateInstance<AMSettings>();
            windowSettings.ShowUtility();
        }
        if(rectBtnModify.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) mouseOverElement = (int)ElementType.Button;
        #endregion
        #region zoom slider
        if(trackCount <= 0) GUI.enabled = false;	// disable slider if there are no tracks
        // adjust zoom slider width
        float width_zoom_slider_dynamic = Mathf.Clamp(position.width - (rectBtnModify.x + rectBtnModify.width) - margin - 25f, 0f, 250f);
        Rect rectZoomSlider = new Rect(position.width - 25f - width_zoom_slider_dynamic + 5f, rectBtnModify.y, width_zoom_slider_dynamic - 5f, 20f);
        if(dragType != (int)DragType.CursorZoom) aData.zoom = GUI.HorizontalSlider(rectZoomSlider, aData.zoom, 1f, 0f);
        else GUI.HorizontalSlider(rectZoomSlider, aData.zoom, 1f, 0f);
        if(rectZoomSlider.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
            mouseOverElement = (int)ElementType.Other;
        }
        GUI.enabled = !isPlaying;

        bool birdseye = (current_width_frame <= width_frame_birdseye_min ? true : false);
        // show or hide zoom texture
        if(position.width > 788 || (compact)) {
            GUI.DrawTexture(new Rect(position.width - 25f, 0f, 20f, 20f), getSkinTextureStyleState("zoom").background);
        }
        #endregion
        #endregion
        #region control bar
        #region auto-key button
        GUIStyle styleBtnAutoKey = new GUIStyle(GUI.skin.button);
        styleBtnAutoKey.clipping = TextClipping.Overflow;
        if(aData.autoKey) {
            styleBtnAutoKey.normal.background = GUI.skin.button.active.background;
            styleBtnAutoKey.normal.textColor = Color.red;
            styleBtnAutoKey.hover.background = GUI.skin.button.active.background;
            styleBtnAutoKey.hover.textColor = Color.red;
        }
        Rect rectBtnAutoKey = new Rect(margin, height_menu_bar + margin, 40f, 15f);
        if(GUI.Button(rectBtnAutoKey, new GUIContent("Auto", "Auto-Key"), styleBtnAutoKey)) aData.autoKey = !aData.autoKey;
        if(rectBtnAutoKey.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) mouseOverElement = (int)ElementType.Button;
        #endregion
        if(trackCount <= 0 || aData.getCurrentTake().selectedTrack == -1) GUI.enabled = false;	// disable key controls if there are no tracks
        #region select previous key
        Rect rectBtnPrevKey = new Rect(rectBtnAutoKey.x + rectBtnAutoKey.width + margin, rectBtnAutoKey.y, 30f, 15f);
        if(GUI.Button(rectBtnPrevKey, new GUIContent((getSkinTextureStyleState("prev_key").background), "Prev. Key"), GUI.skin.GetStyle("ButtonImage"))) {
            timelineSelectPrevKey();
        }
        if(rectBtnPrevKey.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) mouseOverElement = (int)ElementType.Button;
        #endregion
        # region insert key
        Rect rectBtnInsertKey = new Rect(rectBtnPrevKey.x + rectBtnPrevKey.width + margin, rectBtnPrevKey.y, 23f, 15f);
        if(GUI.Button(rectBtnInsertKey, new GUIContent("K", "Insert Key"))) {
            addKeyToSelectedFrame();
        }
        if(rectBtnInsertKey.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) mouseOverElement = (int)ElementType.Button;
        #endregion
        #region select next key
        Rect rectBtnNextKey = new Rect(rectBtnInsertKey.x + rectBtnInsertKey.width + margin, rectBtnInsertKey.y, 30f, 15f);
        if(GUI.Button(rectBtnNextKey, new GUIContent((getSkinTextureStyleState("next_key").background), "Next Key"), GUI.skin.GetStyle("ButtonImage"))) {
            timelineSelectNextKey();
        }
        if(rectBtnNextKey.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) mouseOverElement = (int)ElementType.Button;
        #endregion
        GUI.enabled = !isPlaying;
        #endregion
        #region playback controls
        Rect rectAreaPlaybackControls = new Rect(0f, position.height - height_indicator_footer, width_track + width_playback_controls, height_playback_controls);
        GUI.BeginGroup(rectAreaPlaybackControls);
        #region new track button
        Rect rectNewTrack = new Rect(5f, height_indicator_footer / 2f - 15f / 2f, 15f, 15f);
        Rect rectBtnNewTrack = new Rect(rectNewTrack.x, 0f, rectNewTrack.width, height_indicator_footer);
        if(GUI.Button(rectBtnNewTrack, new GUIContent("", "New Track"), "label")) {
            if(objects_window.Count > 0) objects_window = new List<GameObject>();
            if(menu.GetItemCount() <= 0) buildAddTrackMenu();
            menu.ShowAsContext();
        }
        GUI.DrawTexture(rectNewTrack, (rectBtnNewTrack.Contains(e.mousePosition) ? tex_icon_track_hover : tex_icon_track));
        if(rectBtnNewTrack.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
            mouseOverElement = (int)ElementType.Button;
        }
        #endregion
        #region new group button
        Rect rectNewGroup = new Rect(rectNewTrack.x + rectNewTrack.width + 5f, height_indicator_footer / 2f - 15f / 2f, 15f, 15f);
        Rect rectBtnNewGroup = new Rect(rectNewGroup.x, 0f, rectNewGroup.width, height_indicator_footer);
        if(GUI.Button(rectBtnNewGroup, new GUIContent("", "New Group"), "label")) {
			Undo.RecordObject(aData.getCurrentTake(), "New Group");
            cancelTextEditting();
            aData.getCurrentTake().addGroup();
			EditorUtility.SetDirty(aData.getCurrentTake());
            setScrollViewValue(maxScrollView());
        }
        GUI.DrawTexture(rectNewGroup, (rectBtnNewGroup.Contains(e.mousePosition) ? tex_icon_group_hover : tex_icon_group_closed));
        if(rectBtnNewGroup.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
            mouseOverElement = (int)ElementType.Button;
        }
        #endregion
        #region delete track button
        Rect rectDeleteElement = new Rect(rectNewGroup.x + rectNewGroup.width + 5f + 1f, height_indicator_footer / 2f - 11f / 2f, 11f, 11f);
        Rect rectBtnDeleteElement = new Rect(rectDeleteElement.x, 0f, rectDeleteElement.width, height_indicator_footer);
        if(aData.getCurrentTake().selectedGroup >= 0) GUI.enabled = false;
        if(aData.getCurrentTake().selectedGroup >= 0 && (trackCount <= 0 || (aData.getCurrentTake().contextSelectionTracks != null && aData.getCurrentTake().contextSelectionTracks.Count <= 0))) GUI.enabled = false;
        else GUI.enabled = !isPlaying;
        if(!GUI.enabled) GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0.25f);
        GUIContent gcDeleteButton;
        string strTitleDeleteTrack = (aData.getCurrentTake().contextSelectionTracks != null && aData.getCurrentTake().contextSelectionTracks.Count > 1 ? "Tracks" : "Track");
        if(!GUI.enabled) gcDeleteButton = new GUIContent("");
        else gcDeleteButton = new GUIContent("", "Delete " + (aData.getCurrentTake().contextSelectionTracks != null && aData.getCurrentTake().contextSelectionTracks.Count > 0 ? strTitleDeleteTrack : "Group"));
        if(GUI.Button(rectBtnDeleteElement, gcDeleteButton, "label")) {
            cancelTextEditting();
            if(aData.getCurrentTake().contextSelectionTracks.Count > 0) {
                string strMsgDeleteTrack = (aData.getCurrentTake().contextSelectionTracks.Count > 1 ? "multiple tracks" : "track '" + aData.getCurrentTake().getSelectedTrack().name + "'");

                if((EditorUtility.DisplayDialog("Delete " + strTitleDeleteTrack, "Are you sure you want to delete " + strMsgDeleteTrack + "?", "Delete", "Cancel"))) {
                    isRenamingTrack = -1;

                    AMTake curTake = aData.getCurrentTake();

					Undo.RegisterCompleteObjectUndo(curTake, "Delete Track");

					List<MonoBehaviour> items = new List<MonoBehaviour>();

                    foreach(int track_id in curTake.contextSelectionTracks) {
						curTake.deleteTrack(track_id, true, ref items);
                    }

					foreach(MonoBehaviour item in items)
						Undo.DestroyObjectImmediate(item);

                    curTake.contextSelectionTracks = new List<int>();

                    // deselect track
                    curTake.selectedTrack = -1;
                    // deselect group
                    curTake.selectedGroup = 0;

                    // save data
					EditorUtility.SetDirty(curTake);

                    AMCodeView.refresh();
                }
            }
            else {
                bool delete = true;
                bool deleteContents = false;
				AMTake take = aData.getCurrentTake();
				AMGroup grp = take.getGroup(take.selectedGroup);
				List<MonoBehaviour> items = null;

                if(grp.elements.Count > 0) {
                    int choice = EditorUtility.DisplayDialogComplex("Delete Contents?", "'" + grp.group_name + "' contains contents that can be deleted with the group.", "Delete Contents", "Keep Contents", "Cancel");
                    if(choice == 2) delete = false;
                    else if(choice == 0) deleteContents = true;
                    if(delete) {
						if(deleteContents) {
							Undo.RegisterCompleteObjectUndo(take, "Delete Group");

							items = new List<MonoBehaviour>();
							take.deleteSelectedGroup(true, ref items);

							foreach(MonoBehaviour item in items)
								Undo.DestroyObjectImmediate(item);
						}
						else {
							Undo.RecordObject(take, "Delete Group");

							take.deleteSelectedGroup(false, ref items);
						}

						EditorUtility.SetDirty(take);
                        AMCodeView.refresh();
                    }
                }
                else { //no tracks inside group
					Undo.RecordObject(take, "Delete Group");
                    aData.getCurrentTake().deleteSelectedGroup(false, ref items);
                }
            }
        }
        GUI.DrawTexture(rectDeleteElement, (getSkinTextureStyleState((GUI.enabled && rectBtnDeleteElement.Contains(e.mousePosition) ? "delete_hover" : "delete")).background));
        if(rectBtnDeleteElement.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
            mouseOverElement = (int)ElementType.Button;
        }
        if(GUI.color.a < 1f) GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 1f);
        GUI.enabled = !isPlaying;
        #endregion
        #region resize track
        Rect rectResizeTrack = new Rect(width_track - 5f - 10f, height_indicator_footer / 2f - 10f / 2f - 4f, 15f, 15f);
        GUI.Button(rectResizeTrack, "", GUI.skin.GetStyle("ResizeTrackThumb"));
        if(rectResizeTrack.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
            mouseOverElement = (int)ElementType.ResizeTrack;
        }
        if(GUI.enabled) EditorGUIUtility.AddCursorRect(rectResizeTrack, MouseCursor.ResizeHorizontal);
        GUI.enabled = (aData.getCurrentTake().rootGroup != null && aData.getCurrentTake().rootGroup.elements.Count > 0 ? !isPlaying : false);
        #endregion
        #region select first frame button
        Rect rectBtnSkipBack = new Rect(width_track + margin, margin, 32f, height_playback_controls - margin * 2);
        if(GUI.Button(rectBtnSkipBack, getSkinTextureStyleState("nav_skip_back").background, GUI.skin.GetStyle("ButtonImage"))) timelineSelectFrame(aData.getCurrentTake().selectedTrack, 1);
        if(rectBtnSkipBack.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
            mouseOverElement = (int)ElementType.Button;
        }
        #endregion
        #region toggle play button
        // change label if already playing
        Texture playToggleTexture;
        if(isPlaying) playToggleTexture = getSkinTextureStyleState("nav_stop").background;
        else playToggleTexture = getSkinTextureStyleState("nav_play").background;
        GUI.enabled = (aData.getCurrentTake().rootGroup != null && aData.getCurrentTake().rootGroup.elements.Count > 0 ? true : false);
        Rect rectBtnTogglePlay = new Rect(rectBtnSkipBack.x + rectBtnSkipBack.width + margin, rectBtnSkipBack.y, rectBtnSkipBack.width, rectBtnSkipBack.height);
        if(GUI.Button(rectBtnTogglePlay, playToggleTexture, GUI.skin.GetStyle("ButtonImage"))) {
            if(isChangingTimeControl) isChangingTimeControl = false;
            if(isChangingFrameControl) isChangingFrameControl = false;
            // cancel renaming
            cancelTextEditting();
            // toggle play
            playerTogglePlay();
        }
        if(rectBtnTogglePlay.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
            mouseOverElement = (int)ElementType.Button;
        }
        #endregion
        #region select last frame button
        GUI.enabled = (aData.getCurrentTake().rootGroup != null && aData.getCurrentTake().rootGroup.elements.Count > 0 ? !isPlaying : false);
        Rect rectSkipForward = new Rect(rectBtnTogglePlay.x + rectBtnTogglePlay.width + margin, rectBtnTogglePlay.y, rectBtnTogglePlay.width, rectBtnTogglePlay.height);
        if(GUI.Button(rectSkipForward, getSkinTextureStyleState("nav_skip_forward").background, GUI.skin.GetStyle("ButtonImage"))) timelineSelectFrame(aData.getCurrentTake().selectedTrack, aData.getCurrentTake().numFrames);
        if(rectSkipForward.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
            mouseOverElement = (int)ElementType.Button;
        }
        #endregion
        #region playback speed popup
        Rect rectPopupPlaybackSpeed = new Rect(rectSkipForward.x + rectSkipForward.width + margin, height_indicator_footer / 2f - 15f / 2f, width_playback_speed, rectBtnTogglePlay.height);
        aData.takes[aData.currentTake].playbackSpeedIndex = EditorGUI.Popup(rectPopupPlaybackSpeed, aData.takes[aData.currentTake].playbackSpeedIndex, playbackSpeed);
        #endregion
        #region scrub controls
        GUIStyle styleScrubControl = new GUIStyle(GUI.skin.label);
        string stringTime = frameToTime(aData.getCurrentTake().selectedFrame, (float)aData.getCurrentTake().frameRate).ToString("N2") + " s";
        string stringFrame = aData.getCurrentTake().selectedFrame.ToString() + " fr";
        int timeFontSize = findWidthFontSize(width_scrub_control, styleScrubControl, new GUIContent(stringTime), 8, 14);
        int frameFontSize = findWidthFontSize(width_scrub_control, styleScrubControl, new GUIContent(stringFrame), 8, 14);
        styleScrubControl.fontSize = (timeFontSize <= frameFontSize ? timeFontSize : frameFontSize);
        #region frame control
        Rect rectFrameControl = new Rect(rectPopupPlaybackSpeed.x + rectPopupPlaybackSpeed.width + margin, 1f, width_scrub_control, height_indicator_footer);
        // frame control button
        if(!isChangingFrameControl) {
            // set time control font size
            if(GUI.Button(rectFrameControl, stringFrame, styleScrubControl)) {
                if(dragType != (int)DragType.FrameScrub) {
                    if(isChangingTimeControl) isChangingTimeControl = false;
                    cancelTextEditting();
                    isChangingFrameControl = true;

                }
            }
            // scrubbing cursor
            if(!isPlaying && aData.getCurrentTake().rootGroup != null && aData.getCurrentTake().rootGroup.elements.Count > 0) EditorGUIUtility.AddCursorRect(rectFrameControl, MouseCursor.SlideArrow);
            // check for drag
            if(rectFrameControl.Contains(e.mousePosition) && aData.getCurrentTake().rootGroup != null && aData.getCurrentTake().rootGroup.elements.Count > 0) {
                mouseOverElement = (int)ElementType.FrameScrub;
            }
        }
        else {
            // changing frame control
            selectFrame((int)Mathf.Clamp(EditorGUI.FloatField(new Rect(rectFrameControl.x, rectFrameControl.y + 2f, rectFrameControl.width, rectFrameControl.height), aData.getCurrentTake().selectedFrame, GUI.skin.textField/*,styleButtonTimeControlEdit*/), 1, aData.getCurrentTake().numFrames));
            if(rectFrameControl.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
                mouseOverElement = (int)ElementType.Other;
            }
        }
        #endregion
        #region time control
        Rect rectTimeControl = new Rect(rectFrameControl.x + rectFrameControl.width + margin, rectFrameControl.y, rectFrameControl.width, rectFrameControl.height);
        if(!isChangingTimeControl) {
            // set time control font size
            if(GUI.Button(rectTimeControl, stringTime, styleScrubControl)) {
                if(dragType != (int)DragType.TimeScrub) {
                    if(isChangingFrameControl) isChangingFrameControl = false;
                    cancelTextEditting();
                    isChangingTimeControl = true;
                }
            }
            // scrubbing cursor
            if(!isPlaying && aData.getCurrentTake().rootGroup != null && aData.getCurrentTake().rootGroup.elements.Count > 0) EditorGUIUtility.AddCursorRect(rectTimeControl, MouseCursor.SlideArrow);
            // check for drag
            if(rectTimeControl.Contains(e.mousePosition) && aData.getCurrentTake().rootGroup != null && aData.getCurrentTake().rootGroup.elements.Count > 0) {
                mouseOverElement = (int)ElementType.TimeScrub;
            }
        }
        else {
            // changing time control
            selectFrame(Mathf.Clamp(timeToFrame(EditorGUI.FloatField(new Rect(rectTimeControl.x, rectTimeControl.y + 2f, rectTimeControl.width, rectTimeControl.height), frameToTime(aData.getCurrentTake().selectedFrame, (float)aData.getCurrentTake().frameRate), GUI.skin.textField/*,styleButtonTimeControlEdit*/), (float)aData.getCurrentTake().frameRate), 1, aData.getCurrentTake().numFrames));
            if(rectTimeControl.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
                mouseOverElement = (int)ElementType.Other;
            }
        }
        GUI.enabled = !isPlaying;
        #endregion
        #endregion
        GUI.EndGroup();
        Rect rectFooter = new Rect(rectAreaPlaybackControls.x, rectAreaPlaybackControls.y, position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed) - 5f, rectAreaPlaybackControls.height);
        if(rectFooter.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
            mouseOverElement = (int)ElementType.Other;
        }
        #endregion
        #region horizontal scrollbar
        // check if mouse is over inspector and scroll if dragging
        if(globalMousePosition.y >= (height_control_bar + height_menu_bar + 2f)) {
            difference = 0;
            // drag right, over inspector
            if(globalMousePosition.x >= position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed) - 5f) {
                difference = Mathf.CeilToInt(globalMousePosition.x - (position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed) - 5f));
                tickerSpeed = Mathf.Clamp(50 - Mathf.CeilToInt(difference / 1.5f), 1, 50);
                if(!aData.isInspectorOpen) tickerSpeed /= 10;
                // if mouse over inspector, set mouseOverElement to Other
                mouseOverElement = (int)ElementType.Other;
                if(dragType == (int)DragType.MoveSelection || dragType == (int)DragType.ContextSelection || dragType == (int)DragType.ResizeAction) {
                    if(ticker == 0) {
                        aData.getCurrentTake().startFrame = Mathf.Clamp(++aData.getCurrentTake().startFrame, 1, aData.getCurrentTake().numFrames);
                        mouseXOverFrame = Mathf.Clamp((int)aData.getCurrentTake().startFrame + (int)numFramesToRender, 1, aData.getCurrentTake().numFrames);
                    }
                    else {
                        mouseXOverFrame = Mathf.Clamp((int)aData.getCurrentTake().startFrame + (int)numFramesToRender, 1, aData.getCurrentTake().numFrames);
                    }
                }
                // drag left, over tracks
            }
            else if(globalMousePosition.x <= width_track - 5f) {
                difference = Mathf.CeilToInt((width_track - 5f) - globalMousePosition.x);
                tickerSpeed = Mathf.Clamp(50 - Mathf.CeilToInt(difference / 1.5f), 1, 50);
                if(dragType == (int)DragType.MoveSelection || dragType == (int)DragType.ContextSelection || dragType == (int)DragType.ResizeAction) {
                    if(ticker == 0) {
                        aData.getCurrentTake().startFrame = Mathf.Clamp(--aData.getCurrentTake().startFrame, 1, aData.getCurrentTake().numFrames);
                        mouseXOverFrame = Mathf.Clamp((int)aData.getCurrentTake().startFrame - 2, 1, aData.getCurrentTake().numFrames);
                    }
                    else {
                        mouseXOverFrame = Mathf.Clamp((int)aData.getCurrentTake().startFrame, 1, aData.getCurrentTake().numFrames);
                    }
                }
            }
        }
        Rect rectHScrollbar = new Rect(width_track + width_playback_controls, position.height - height_indicator_footer + 2f, position.width - (width_track + width_playback_controls) - (aData.isInspectorOpen ? width_inspector_open - 4f : width_inspector_closed) - 21f, height_indicator_footer - 2f);
        float frame_width_HScrollbar = ((rectHScrollbar.width - 44f - (aData.isInspectorOpen ? 4f : 0f)) / ((float)aData.getCurrentTake().numFrames - 1f));
        Rect rectResizeHScrollbarLeft = new Rect(rectHScrollbar.x + 18f + frame_width_HScrollbar * (aData.getCurrentTake().startFrame - 1f), rectHScrollbar.y + 2f, 15f, 15f);
        Rect rectResizeHScrollbarRight = new Rect(rectHScrollbar.x + 18f + frame_width_HScrollbar * (aData.getCurrentTake().endFrame - 1f) - 3f, rectHScrollbar.y + 2f, 15f, 15f);
        Rect rectHScrollbarThumb = new Rect(rectResizeHScrollbarLeft.x, rectResizeHScrollbarLeft.y - 2f, rectResizeHScrollbarRight.x - rectResizeHScrollbarLeft.x + rectResizeHScrollbarRight.width, rectResizeHScrollbarLeft.height);
        if(!aData.isInspectorOpen) rectHScrollbar.width += 4f;
        // if number of frames fit on screen, disable horizontal scrollbar and set startframe to 1
        if(aData.getCurrentTake().numFrames < numFramesToRender) {
            GUI.HorizontalScrollbar(rectHScrollbar, 1f, 1f, 1f, 1f);
            aData.getCurrentTake().startFrame = 1;
        }
        else {
            bool hideResizeThumbs = false;
            if(rectHScrollbarThumb.width < rectResizeHScrollbarLeft.width * 2) {
                hideResizeThumbs = true;
                rectResizeHScrollbarLeft = new Rect(rectHScrollbarThumb.x - 4f, rectResizeHScrollbarLeft.y, rectHScrollbarThumb.width / 2f + 4f, rectResizeHScrollbarLeft.height);
                rectResizeHScrollbarRight = new Rect(rectHScrollbarThumb.x + rectHScrollbarThumb.width - rectHScrollbarThumb.width / 2f, rectResizeHScrollbarRight.y, rectResizeHScrollbarLeft.width, rectResizeHScrollbarRight.height);
            }
            mouseXOverHScrollbarFrame = Mathf.CeilToInt(aData.getCurrentTake().numFrames * ((e.mousePosition.x - rectHScrollbar.x - GUI.skin.horizontalScrollbarLeftButton.fixedWidth) / (rectHScrollbar.width - GUI.skin.horizontalScrollbarLeftButton.fixedWidth * 2)));
            if(!rectResizeHScrollbarLeft.Contains(e.mousePosition) && !rectResizeHScrollbarRight.Contains(e.mousePosition) && EditorWindow.mouseOverWindow == this && dragType != (int)DragType.ResizeHScrollbarLeft && dragType != (int)DragType.ResizeHScrollbarRight && mouseOverElement != (int)ElementType.ResizeHScrollbarLeft && mouseOverElement != (int)ElementType.ResizeHScrollbarRight)
                aData.getCurrentTake().startFrame = Mathf.Clamp((int)GUI.HorizontalScrollbar(rectHScrollbar, (float)aData.getCurrentTake().startFrame, (int)numFramesToRender - 1f, 1f, aData.getCurrentTake().numFrames), 1, aData.getCurrentTake().numFrames);
            else Mathf.Clamp(GUI.HorizontalScrollbar(rectHScrollbar, (float)aData.getCurrentTake().startFrame, (int)numFramesToRender - 1f, 1f, aData.getCurrentTake().numFrames), 1f, aData.getCurrentTake().numFrames);
            // scrollbar bg overlay (used to hide inconsistent thumb)
            GUI.Box(new Rect(rectHScrollbar.x + 18f, rectHScrollbar.y, rectHScrollbar.width - 18f * 2f, rectHScrollbar.height), "", GUI.skin.horizontalScrollbar);
            // scrollbar thumb overlay (used to hide inconsistent thumb)
            GUI.Box(rectHScrollbarThumb, "", GUI.skin.horizontalScrollbarThumb);


            if(!hideResizeThumbs) {
                if(!GUI.enabled) GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0.25f);
                GUI.DrawTexture(rectResizeHScrollbarLeft, GUI.skin.GetStyle("ResizeTrackThumb").normal.background);
                GUI.DrawTexture(rectResizeHScrollbarRight, GUI.skin.GetStyle("ResizeTrackThumb").normal.background);
                GUI.color = Color.white;
            }
            if(GUI.enabled && !isDragging) {
                EditorGUIUtility.AddCursorRect(rectResizeHScrollbarLeft, MouseCursor.ResizeHorizontal);
                EditorGUIUtility.AddCursorRect(rectResizeHScrollbarRight, MouseCursor.ResizeHorizontal);
            }
            // show horizontal scrollbar
            if(rectResizeHScrollbarLeft.Contains(e.mousePosition) && customCursor == (int)CursorType.None) {

                mouseOverElement = (int)ElementType.ResizeHScrollbarLeft;
            }
            else if(rectResizeHScrollbarRight.Contains(e.mousePosition) && customCursor == (int)CursorType.None) {
                mouseOverElement = (int)ElementType.ResizeHScrollbarRight;
            }
        }
        aData.getCurrentTake().endFrame = aData.getCurrentTake().startFrame + (int)numFramesToRender - 1;
        if(rectHScrollbar.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
            mouseOverElement = (int)ElementType.Other;
        }

        #endregion
        #region inspector toggle button
        Rect rectPropertiesButton = new Rect(position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed) - 1f, height_menu_bar + height_control_bar + 2f, width_inspector_closed, position.height);
        GUI.color = getSkinTextureStyleState("properties_bg").textColor;
        GUI.DrawTexture(rectPropertiesButton, getSkinTextureStyleState("properties_bg").background);
        GUI.color = Color.white;
        // inspector toggle button
        if(GUI.Button(rectPropertiesButton, "", "label")) {
            aData.isInspectorOpen = !aData.isInspectorOpen;
        }
        if(rectPropertiesButton.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
            mouseOverElement = (int)ElementType.Button;
        }
        #endregion
        #region key numbering
        Rect rectKeyNumbering = new Rect(width_track, height_control_bar + height_menu_bar + 2f - 22f, position.width - width_track - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed) - 20f, 20f);
        if(rectKeyNumbering.Contains(e.mousePosition) && (mouseOverElement == (int)ElementType.None)) {
            mouseOverElement = (int)ElementType.TimelineScrub;
        }
        int key_dist = 5;
        if(numFramesToRender >= 100) key_dist = Mathf.FloorToInt(numFramesToRender / 100) * 10;
        int firstMarkedKey = (int)aData.getCurrentTake().startFrame;
        if(firstMarkedKey % key_dist != 0 && firstMarkedKey != 1) {
            firstMarkedKey += key_dist - firstMarkedKey % key_dist;
        }
        float lastNumberX = -1f;
        for(int i = firstMarkedKey; i <= (int)aData.getCurrentTake().endFrame; i += key_dist) {
            float newKeyNumberX = width_track + current_width_frame * (i - (int)aData.getCurrentTake().startFrame) - 1f;
            string key_number;
            if(oData.time_numbering) key_number = frameToTime(i, (float)aData.getCurrentTake().frameRate).ToString("N2");
            else key_number = i.ToString();
            Rect rectKeyNumber = new Rect(newKeyNumberX, height_menu_bar, GUI.skin.label.CalcSize(new GUIContent(key_number)).x, height_control_bar);
            bool didCutLabel = false;
            if(rectKeyNumber.x + rectKeyNumber.width >= position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed) - 20f) {
                rectKeyNumber.width = position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed) - 20f - rectKeyNumber.x;
                didCutLabel = true;
            }
            if(!(didCutLabel && aData.getCurrentTake().endFrame == aData.getCurrentTake().numFrames)) {
                if(rectKeyNumber.x > lastNumberX + 3f) {
                    GUI.Label(rectKeyNumber, key_number);
                    lastNumberX = rectKeyNumber.x + GUI.skin.label.CalcSize(new GUIContent(key_number)).x;
                }
            }
            if(i == 1) i--;
        }
        #endregion
        #region main scrollview
        height_all_tracks = aData.getCurrentTake().getElementsHeight(0, height_track, height_track_foldin, height_group);
        float height_scrollview = position.height - (height_control_bar + height_menu_bar) - height_indicator_footer;
        // check if mouse is beyond tracks and dragging group element
        difference = 0;
        // drag up
        if(dragType == (int)DragType.GroupElement && globalMousePosition.y <= height_control_bar + height_menu_bar + 2f) {
            difference = Mathf.CeilToInt((height_control_bar + height_menu_bar + 2f) - globalMousePosition.y);
            scrollAmountVertical = -difference;	// set scroll amount
            // drag down
        }
        else if(dragType == (int)DragType.GroupElement && globalMousePosition.y >= position.height - height_playback_controls) {
            difference = Mathf.CeilToInt(globalMousePosition.y - (position.height - height_playback_controls));
            scrollAmountVertical = difference; // set scroll amount
        }
        else {
            scrollAmountVertical = 0f;
        }
        // frames bg
        GUI.DrawTexture(new Rect(0f, height_control_bar + height_menu_bar + 2f, position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed) - 5f, position.height - height_control_bar - height_menu_bar - height_indicator_footer), GUI.skin.GetStyle("GroupElementBG").onNormal.background);
        // tracks bg
        GUI.Box(new Rect(0f, height_control_bar + height_menu_bar + 2f, width_track, position.height - height_control_bar - height_menu_bar - height_indicator_footer), "", GUI.skin.GetStyle("GroupElementBG"));
        Rect rectScrollView = new Rect(0f, height_control_bar + height_menu_bar + 2f, position.width - (aData.isInspectorOpen ? width_inspector_open/*+3f*/ : width_inspector_closed), height_scrollview);
        Rect rectView = new Rect(0f, 0f, rectScrollView.width - 20f, (height_all_tracks > rectScrollView.height ? height_all_tracks : rectScrollView.height));
        scrollViewValue = GUI.BeginScrollView(rectScrollView, scrollViewValue, rectView, false, true);
        scrollViewValue.y = Mathf.Clamp(scrollViewValue.y, 0f, height_all_tracks - height_scrollview);
        Vector2 scrollViewBounds = new Vector2(scrollViewValue.y, scrollViewValue.y + height_scrollview); // min and max y displayed onscreen
        bool isAnyTrackFoldedOut = false;
        GUILayout.BeginHorizontal(GUILayout.Height(height_all_tracks));
        GUILayout.BeginVertical(GUILayout.Width(width_track));
        float track_y = 0f;		// the next track's y position
        // tracks vertical start
        for(int i = 0; i < aData.getCurrentTake().rootGroup.elements.Count; i++) {
            if(track_y > scrollViewBounds.y) break;	// if start y is beyond max y
            int id = aData.getCurrentTake().rootGroup.elements[i];
            float height_group_elements = 0f;
            showGroupElement(id, 0, ref track_y, ref isAnyTrackFoldedOut, ref height_group_elements, e, scrollViewBounds);
        }
        // draw element position indicator
        if(dragType == (int)DragType.GroupElement) {
            if(mouseOverElement != (int)ElementType.Group && mouseOverElement != (int)ElementType.GroupOutside && mouseOverElement != (int)ElementType.Track) {
                float element_position_y;
                if(e.mousePosition.y < (height_menu_bar + height_control_bar)) element_position_y = 2f;
                else element_position_y = track_y;
                GUI.DrawTexture(new Rect(0f, element_position_y - height_element_position, width_track, height_element_position), tex_element_position);
            }
        }
        GUILayout.EndVertical();
        GUILayout.BeginVertical();
        // frames vertical	
        GUILayout.BeginHorizontal(GUILayout.Height(height_track));
        mouseXOverFrame = (int)aData.getCurrentTake().startFrame + Mathf.CeilToInt((e.mousePosition.x - width_track) / current_width_frame) - 1;
        if(dragType == (int)DragType.CursorHand && justStartedHandGrab) {
            startScrubFrame = mouseXOverFrame;
            justStartedHandGrab = false;
        }
        track_y = 0f;	// reset track y
        showFramesForGroup(0, ref track_y, e, birdseye, scrollViewBounds);
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
        #endregion
        #region inspector
        Texture inspectorArrow;
        if(aData.isInspectorOpen) {
            Rect rectInspector = new Rect(position.width - width_inspector_open - 4f + width_inspector_closed, height_control_bar + height_menu_bar + 2f, width_inspector_open, position.height - height_menu_bar - height_control_bar);
            GUI.BeginGroup(rectInspector);
            // inspector vertical
            GUI.enabled = true;
            GUI.enabled = !isPlaying;
            // backup editor styles
            GUIStyle styleEditorTextField = new GUIStyle(EditorStyles.textField);
            GUIStyle styleEditorLabel = new GUIStyle(EditorStyles.label);
            // modify editor styles
            EditorStyles.textField.normal = GUI.skin.textField.normal;
            EditorStyles.textField.focused = GUI.skin.textField.focused;
            EditorStyles.label.normal = GUI.skin.label.normal;
            showInspectorPropertiesFor(rectInspector, aData.getCurrentTake().selectedTrack, aData.getCurrentTake().selectedFrame, e);
            // reset editor styles
            EditorStyles.textField.normal = styleEditorTextField.normal;
            EditorStyles.textField.focused = styleEditorTextField.focused;
            EditorStyles.label.normal = styleEditorLabel.normal;
            inspectorArrow = texRightArrow;
            GUI.EndGroup();
        }
        else {
            GUI.enabled = true;
            GUI.enabled = !isPlaying;
            inspectorArrow = texLeftArrow;
        }
        GUI.DrawTexture(new Rect(position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed) + 4f, 60f, 22f, 19f), inspectorArrow);
        GUI.DrawTexture(new Rect(position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed) - 8f, 73f, 48f, 48f), texProperties);
        #endregion
        #region indicator
        if((oData.showFramesForCollapsedTracks || isAnyTrackFoldedOut) && (trackCount > 0)) drawIndicator(aData.getCurrentTake().selectedFrame);
        #endregion
        #region horizontal scrollbar tooltip
        string strHScrollbarLeftTooltip = (oData.time_numbering ? frameToTime((int)aData.getCurrentTake().startFrame, (float)aData.getCurrentTake().frameRate).ToString("N2") : aData.getCurrentTake().startFrame.ToString());
        string strHScrollbarRightTooltip = (oData.time_numbering ? frameToTime((int)aData.getCurrentTake().endFrame, (float)aData.getCurrentTake().frameRate).ToString("N2") : aData.getCurrentTake().endFrame.ToString());
        GUIStyle styleLabelCenter = new GUIStyle(GUI.skin.label);
        styleLabelCenter.alignment = TextAnchor.MiddleCenter;
        Vector2 _label_size;
        if(customCursor == (int)CursorType.None && ((mouseOverElement == (int)ElementType.ResizeHScrollbarLeft && !isDragging) || dragType == (int)DragType.ResizeHScrollbarLeft) && (dragType != (int)DragType.ResizeHScrollbarRight)) {
            _label_size = GUI.skin.button.CalcSize(new GUIContent(strHScrollbarLeftTooltip));
            _label_size.x += 2f;
            GUI.Label(new Rect(rectResizeHScrollbarLeft.x + rectResizeHScrollbarLeft.width / 2f - _label_size.x / 2f, rectResizeHScrollbarLeft.y - 22f, _label_size.x, 20f), strHScrollbarLeftTooltip, GUI.skin.button);
        }
        if(customCursor == (int)CursorType.None && ((mouseOverElement == (int)ElementType.ResizeHScrollbarRight && !isDragging) || dragType == (int)DragType.ResizeHScrollbarRight) && (dragType != (int)DragType.ResizeHScrollbarLeft)) {
            _label_size = GUI.skin.button.CalcSize(new GUIContent(strHScrollbarRightTooltip));
            _label_size.x += 2f;
            GUI.Label(new Rect(rectResizeHScrollbarRight.x + rectResizeHScrollbarRight.width / 2f - _label_size.x / 2f, rectResizeHScrollbarRight.y - 22f, _label_size.x, 20f), strHScrollbarRightTooltip, GUI.skin.button);
        }
        #endregion
        #region click window
        if(GUI.Button(new Rect(0f, 0f, position.width, position.height), "", "label") && dragType != (int)DragType.TimelineScrub && dragType != (int)DragType.ResizeAction) {
            bool didRegisterUndo = false;

            if(aData.getCurrentTake().contextSelectionTracks != null && aData.getCurrentTake().contextSelectionTracks.Count > 0) {
				Undo.RecordObject(aData.getCurrentTake(), "Deselect Tracks");
                didRegisterUndo = true;
                aData.getCurrentTake().contextSelectionTracks = new List<int>();
            }
            if(aData.getCurrentTake().contextSelection != null && aData.getCurrentTake().contextSelection.Count > 0) {
				if(!didRegisterUndo) Undo.RecordObject(aData.getCurrentTake(), "Deselect Frames");
                didRegisterUndo = true;
                aData.getCurrentTake().contextSelection = new List<int>();
            }
            if(aData.getCurrentTake().ghostSelection != null && aData.getCurrentTake().ghostSelection.Count > 0) {
                aData.getCurrentTake().ghostSelection = new List<int>();
            }

            if(objects_window.Count > 0) objects_window = new List<GameObject>();
            if(isRenamingGroup < 0) isRenamingGroup = 0;
            if(isRenamingTake) {
                aData.makeTakeNameUnique(aData.getCurrentTake());
                isRenamingTake = false;
            }
            if(isRenamingTrack != -1) isRenamingTrack = -1;
            if(isChangingTimeControl) isChangingTimeControl = false;
            if(isChangingFrameControl) isChangingFrameControl = false;
            // if clicked on inspector, do nothing
            if(e.mousePosition.y > (float)height_menu_bar + (float)height_control_bar && e.mousePosition.x > position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed)) return;
            if(aData.getCurrentTake().selectedGroup != 0) timelineSelectGroup(0);
            if(aData.getCurrentTake().selectedTrack != -1) aData.getCurrentTake().selectedTrack = -1;

            if(objects_window.Count > 0) objects_window = new List<GameObject>();

        }
        #endregion
        #region drag logic
        if(dragType == (int)DragType.GroupElement) {
            // show element near cursor
            Rect rectDragElement = new Rect(e.mousePosition.x + 20f, e.mousePosition.y, 90f, 20f);
            string dragElementName = "Unknown";
            Texture dragElementIcon = null;
            float dragElementIconWidth = 12f;
            if(draggingGroupElementType == (int)ElementType.Group) {
                dragElementName = aData.getCurrentTake().getGroup((int)draggingGroupElement.x).group_name;
                dragElementIcon = tex_icon_group_closed;
                dragElementIconWidth = 16f;
            }
            else if(draggingGroupElementType == (int)ElementType.Track) {
                AMTrack dragTrack = aData.getCurrentTake().getTrack((int)draggingGroupElement.y);
                if(dragTrack) {
                    dragElementName = dragTrack.name;
                    dragElementIcon = getTrackIconTexture(dragTrack);
                }
            }
            GUI.DrawTexture(rectDragElement, GUI.skin.GetStyle("GroupElementActive").normal.background);
            dragElementName = trimString(dragElementName, 8);
            if(dragElementIcon) GUI.DrawTexture(new Rect(rectDragElement.x + 3f + (draggingGroupElementType == (int)ElementType.Track ? 1.45f : 0f), rectDragElement.y + rectDragElement.height / 2 - dragElementIconWidth / 2, dragElementIconWidth, dragElementIconWidth), dragElementIcon);
            GUI.Label(new Rect(rectDragElement.x + 15f + 4f, rectDragElement.y, rectDragElement.width - 15f - 4f, rectDragElement.height), dragElementName);
        }
        mouseAboveGroupElements = e.mousePosition.y < (height_menu_bar + height_control_bar);
        if(aData.getCurrentTake().rootGroup == null || aData.getCurrentTake().rootGroup.elements.Count <= 0) {

            if(aData.isInspectorOpen) aData.isInspectorOpen = false;
            float width_helpbox = position.width - width_inspector_closed - 28f - width_track;
            EditorGUI.HelpBox(new Rect(width_track + 5f, height_menu_bar + height_control_bar + 7f, width_helpbox, 50f), "Click the track icon below or drag a GameObject here to add a new track.", MessageType.Info);
            //GUI.DrawTexture(new Rect(width_track+75f,height_menu_bar+height_control_bar+19f-(width_helpbox<=355.5f ? 6f: 0f),15f,15f),tex_icon_track);
        }
        #endregion
        #region quick add
        GUIStyle styleObjectField = new GUIStyle(EditorStyles.objectField);
        GUIStyle styleObjectFieldThumb = new GUIStyle(EditorStyles.objectFieldThumb);
        EditorStyles.objectField.normal.textColor = new Color(0f, 0f, 0f, 0f);
        EditorStyles.objectField.contentOffset = new Vector2(width_track * -1 - 300f, 0f);
        EditorStyles.objectField.normal.background = null;
        EditorStyles.objectField.onNormal.background = null;

        GameObject tempGO = null;
        tempGO = (GameObject)EditorGUI.ObjectField(new Rect(width_track, height_menu_bar + height_control_bar + 2f, position.width - 5f - 15f - width_track - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed), position.height - height_indicator_footer - height_menu_bar - height_control_bar), "", tempGO, typeof(GameObject), true);

        if(tempGO != null) {
            objects_window = new List<GameObject>();
            if(Selection.gameObjects.Length <= 0) objects_window.Add(tempGO);
            else objects_window.AddRange(Selection.gameObjects);
            buildAddTrackMenu_Drag();
            menu_drag.ShowAsContext();
        }
        EditorStyles.objectField.contentOffset = styleObjectField.contentOffset;
        EditorStyles.objectField.normal = styleObjectField.normal;
        EditorStyles.objectField.onNormal = styleObjectField.onNormal;
        EditorStyles.objectFieldThumb.normal = styleObjectFieldThumb.normal;
        #endregion
        #region tooltip

        if(!oData.disableTimelineActions && !oData.disableTimelineActionsTooltip && dragType == (int)DragType.None && showTooltip && tooltip != "") {
            Vector2 tooltipSize = GUI.skin.label.CalcSize(new GUIContent(tooltip));
            tooltipSize.x += GUI.skin.button.padding.left + GUI.skin.button.padding.right;
            tooltipSize.y += GUI.skin.button.padding.top + GUI.skin.button.padding.bottom;
            Rect rectTooltip = new Rect(e.mousePosition.x - tooltipSize.x / 2f, (e.mousePosition.y + 30f + tooltipSize.y <= position.height - height_playback_controls ? e.mousePosition.y + 30f : e.mousePosition.y - 12f - tooltipSize.y), tooltipSize.x, tooltipSize.y);
            GUI.Box(rectTooltip, tooltip, GUI.skin.button);
        }
        #endregion
        #region custom cursor
        if(customCursor != (int)CursorType.None) {
            if(customCursor == (int)CursorType.Zoom) {
                if(!tex_cursor_zoom) tex_cursor_zoom = tex_cursor_zoomin;
                if(tex_cursor_zoom == tex_cursor_zoomin && aData.zoom <= 0f) tex_cursor_zoom = tex_cursor_zoom_blank;
                else if(tex_cursor_zoom == tex_cursor_zoomout && aData.zoom >= 1f) tex_cursor_zoom = tex_cursor_zoom_blank;
                GUI.DrawTexture(new Rect(e.mousePosition.x - 6f, e.mousePosition.y - 5f, 16f, 16f), tex_cursor_zoom);
            }
            else if(customCursor == (int)CursorType.Hand) {
                GUI.DrawTexture(new Rect(e.mousePosition.x - 8f, e.mousePosition.y - 7f, 16f, 16f), (tex_cursor_grab));
            }
        }
        #endregion
        if(e.alt && !isDragging) startZoomXOverFrame = mouseXOverFrame;
        e.Use();

        if(doRefresh)
            ShadyRefresh();
    }
    void ReloadOtherWindows() {
        if(AMOptions.window) AMOptions.window.reloadAnimatorData();
        if(AMSettings.window) AMSettings.window.reloadAnimatorData();
        if(AMCodeView.window) AMCodeView.window.reloadAnimatorData();
        if(AMEasePicker.window) AMEasePicker.window.reloadAnimatorData();
        if(AMPropertySelect.window) AMPropertySelect.window.reloadAnimatorData();
        if(AMTakeExport.window) AMTakeExport.window.reloadAnimatorData();
    }

    void OnUndoRedo() {
        if(isPlaying) isPlaying = false;

        if(_aData != null)
            _aData = _aData.gameObject.GetComponent<AnimatorData>();

        Repaint();

        // recheck for component
        /*GameObject go = GameObject.Find("AnimatorData");
        if(go) {
            aData = (AnimatorData)go.GetComponent("AnimatorData");
        }
        else {
            aData = null;
        }*/
        // repaint
        //this.Repaint();
        //repaintBuffer = repaintRefreshRate;

        // reload AnimatorData for other windows
        ReloadOtherWindows();

    }

    void OnPlayMode() {
        bool justHitPlay = EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying;
        // entered playmode
        if(justHitPlay) {
            Selection.activeGameObject = null;
            aData = null;
            if(dragType == (int)DragType.TimeScrub || dragType == (int)DragType.FrameScrub) dragType = (int)DragType.None;
        }

        /*bool justHitPlay = EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying;
        // entered playmode
        if(justHitPlay) {
            if(aData) {
                AnimatorData _playModeDataHolder = aData;
                aData = null;
                aData = _playModeDataHolder;
                aData.inPlayMode = true;
            }
            //aData = null; //?????
            //repaintBuffer = 0;	// used to repaint after user exits play mode
            if(dragType == (int)DragType.TimeScrub || dragType == (int)DragType.FrameScrub) dragType = (int)DragType.None;

            // exit playmode
        }
        else if(!EditorApplication.isPlayingOrWillChangePlaymode) {
            if(aData) {
                aData.inPlayMode = false;

                //this.Repaint();
                //repaintBuffer = repaintRefreshRate;
                // reset inspector selected methodinfo
                indexMethodInfo = -1;
                // preview selected frame
                if(aData.getCurrentTake()) aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
            }
            else {
                GameObject go = Selection.activeGameObject;
                if(go) {
                    aData = go.GetComponent<AnimatorData>();
                    //maintainCachesIn = 10;
                }
            }

            ReloadOtherWindows();
                        
            // check for pro license
            AMTake.isProLicense = PlayerSettings.advancedLicense;
        }*/
    }

    #endregion

    #region Functions

    #region Static

	public static MonoBehaviour[] getKeysAndTracks(AMTake take) {
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

    public static void MessageBox(string message, MessageBoxType type) {

        MessageType messageType;
        if(type == MessageBoxType.Error) messageType = MessageType.Error;
        else if(type == MessageBoxType.Warning) messageType = MessageType.Warning;
        else messageType = MessageType.Info;

        EditorGUILayout.HelpBox(message, messageType);
    }
    public static void loadSkin(AMOptionsFile oData, ref GUISkin _skin, ref string skinName, Rect position) {
        if(_skin == null || skinName == null || skinName != oData.skin/*global_skin*/) {
            //
            _skin = (GUISkin)AMEditorResource.LoadSkin(oData.skin); /*global_skin*/
            skinName = oData.skin/*global_skin*/;
        }
        GUI.skin = _skin;
        GUI.color = GUI.skin.window.normal.textColor;
        GUI.DrawTexture(new Rect(0f, 0f, position.width, position.height), EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;
    }
    public static GUIStyleState getSkinTextureStyleState(string name) {
        if(name == "properties_bg") return GUI.skin.GetStyle("Textures_1").normal;
        if(name == "delete") return GUI.skin.GetStyle("Textures_1").hover;
        if(name == "rename") return GUI.skin.GetStyle("Textures_1").active;
        if(name == "zoom") return GUI.skin.GetStyle("Textures_1").focused;
        if(name == "nav_skip_back") return GUI.skin.GetStyle("Textures_1").onNormal;
        if(name == "nav_play") return GUI.skin.GetStyle("Textures_1").onHover;
        if(name == "nav_skip_forward") return GUI.skin.GetStyle("Textures_1").onActive;
        if(name == "next_key") return GUI.skin.GetStyle("Textures_1").onFocused;
        if(name == "prev_key") return GUI.skin.GetStyle("Textures_2").normal;
        if(name == "nav_stop") return GUI.skin.GetStyle("Textures_2").hover;
        if(name == "accept") return GUI.skin.GetStyle("Textures_2").focused;
        if(name == "delete_hover") return GUI.skin.GetStyle("Textures_2").active;
        if(name == "popup") return GUI.skin.GetStyle("Textures_2").onNormal;
        if(name == "playonstart") return GUI.skin.GetStyle("Textures_2").onHover;
        if(name == "nav_stop_white") return GUI.skin.GetStyle("Textures_2").onActive;
        if(name == "select_all") return GUI.skin.GetStyle("Textures_2").onFocused;
        if(name == "x") return GUI.skin.GetStyle("Textures_3").normal;
        if(name == "select_this") return GUI.skin.GetStyle("Textures_3").hover;
        //if(name == "select_exclusive") return GUI.skin.GetStyle("Textures_3").active;
        Debug.LogWarning("Animator: Skin texture " + name + " not found.");
        return GUI.skin.label.normal;
    }
    public static void resetIndexMethodInfo() {
        indexMethodInfo = -1;
    }
    public static float frameToTime(int frame, float frameRate) {
        return (float)Math.Round((float)frame / frameRate, 2);
    }
    public static int timeToFrame(float time, float frameRate) {
        return Mathf.FloorToInt(time * frameRate);
    }
    private static void cacheSelectedMethodParameterInfos() {
        if(cachedMethodInfo == null || indexMethodInfo == -1 || (indexMethodInfo >= cachedMethodInfo.Count)) {
            cachedParameterInfos = new ParameterInfo[] { };
            arrayFieldFoldout = new Dictionary<string, bool>();	// reset array foldout dictionary
            return;
        }
        cachedParameterInfos = cachedMethodInfo[indexMethodInfo].GetParameters();
    }

    #endregion

    #region Show/Draw

    bool showGroupElement(int id, int group_lvl, ref float track_y, ref bool isAnyTrackFoldedOut, ref float height_group_elements, Event e, Vector2 scrollViewBounds) {
        // returns true if mouse over track
        if(id >= 0) {
            AMTrack _track = aData.getCurrentTake().getTrack(id);
            return _track ? showTrack(_track, id, group_lvl, ref track_y, ref isAnyTrackFoldedOut, ref height_group_elements, e, scrollViewBounds) : false;
        }
        else {
            return showGroup(id, group_lvl, ref track_y, ref isAnyTrackFoldedOut, ref height_group_elements, e, scrollViewBounds);
        }
    }
    bool showGroup(int id, int group_lvl, ref float track_y, ref bool isAnyTrackFoldedOut, ref float height_group_elements, Event e, Vector2 scrollViewBounds) {
        if(track_y > scrollViewBounds.y) return false;	// if beyond lower bound, return
        bool isBeyondUpperBound = (track_y + height_group < scrollViewBounds.x);
        // show group
        float group_x = width_subtrack_space * group_lvl;
        group_lvl++;	// increment group_lvl for sub-elements
        Rect rectGroup = new Rect(group_x, track_y, width_track - group_x, height_group);
        AMGroup grp = aData.getCurrentTake().getGroup(id);
        bool isGroupSelected = (aData.getCurrentTake().selectedGroup == id && aData.getCurrentTake().selectedTrack == -1);
        float local_height_group_elements = height_group;
        if(!isBeyondUpperBound) {
            if(rectGroup.width > 4f) {
                if(isRenamingGroup != id) {
                    if(GUI.Button(new Rect(group_x + 15f, track_y, width_track - 15f, height_group), "", "label")) {
                        timelineSelectGroup(id);
                        if(didDoubleClick("group" + id + "foldout")) {
                            //grp.foldout = !grp.foldout;
                            isRenamingGroup = id;
                        }
                    }
                }
                //foldout button
                if(GUI.Button(new Rect(group_x, track_y, 15f, height_group), "", "label")) {
                    grp.foldout = !grp.foldout;

                    timelineSelectGroup(id);
                    if(!grp.foldout) {
                        //bool found = false;
                        foreach(int __id in grp.elements) {
                            if(isRenamingTrack == __id) {
                                isRenamingTrack = -1;
                                break;
                            }
                        }
                    }
                }
                string strStyle;
                int numTracks = 0;
                if(((grp.foldout && aData.getCurrentTake().contextSelectionTracks.Count > 1) || (!grp.foldout && aData.getCurrentTake().contextSelectionTracks.Count > 0)) && aData.getCurrentTake().isGroupSelected(id, ref numTracks) && numTracks > 0) {
                    if(isGroupSelected) strStyle = "GroupElementSelectedActive";
                    else strStyle = "GroupElementSelected";
                }
                else {
                    if(isGroupSelected) strStyle = "GroupElementActive";
                    else strStyle = "GroupElementNormal";
                }
                // group element
                GUI.BeginGroup(rectGroup, GUI.skin.GetStyle(strStyle));
                if(isRenamingGroup == id) {
                    GUI.SetNextControlName("RenameGroup" + id);
                    grp.group_name = GUI.TextField(new Rect(33f, 2f, rectGroup.width, rectGroup.height), grp.group_name);
                    GUI.FocusControl("RenameGroup" + id);
                }
                else {
                    GUI.Label(new Rect(33f, 0f, rectGroup.width, rectGroup.height), aData.getCurrentTake().getGroup(id).group_name);
                }
                GUI.EndGroup();
                if(rectGroup.width >= 15f) GUI.DrawTexture(new Rect(group_x, track_y + (height_group - 16f) / 2f, 16f, 16f), (grp.foldout ? GUI.skin.GetStyle("GroupElementFoldout").normal.background : GUI.skin.GetStyle("GroupElementFoldout").active.background));
                if(rectGroup.width >= 32f) GUI.DrawTexture(new Rect(group_x + 15f, track_y + (height_group - 16f) / 2f, 16f, 16f), (grp.foldout ? tex_icon_group_open : tex_icon_group_closed));
                // mouse over
                Rect rectGroupNoFoldout = new Rect(rectGroup.x + 15f, rectGroup.y, rectGroup.width - 15f, rectGroup.height);
                if(rectGroupNoFoldout.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
                    mouseOverElement = (int)ElementType.Group;
                    mouseOverGroupElement = new Vector2(id, -1);
                }
            }
            else {
                // tooltip hidden group
                GUI.enabled = false;
                GUI.Button(new Rect(width_track - 80f, track_y, 80f, height_group), trimString(grp.group_name, 8));
                GUI.enabled = !isPlaying;
            }
        }
        Rect rectGroupOutside = new Rect(group_x, track_y, 15f, height_group);
        track_y += height_group;
        if(grp.foldout) {

            if(grp.elements.Count <= 0) {
                Rect rectGroupEmpty = new Rect(group_x + width_subtrack_space, track_y, width_track - group_x - width_subtrack_space, height_group);
                local_height_group_elements += rectGroupEmpty.height;
                track_y += height_group;
                // if "no tracks" label in bounds, show
                if(track_y > scrollViewBounds.x && track_y - height_group < scrollViewBounds.y) {
                    GUI.Label(rectGroupEmpty, "No Tracks");
                }

                if(rectGroupEmpty.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
                    mouseOverElement = (int)ElementType.Group;
                    mouseOverGroupElement = new Vector2(id, -1);
                }
            }
            else {
                for(int j = 0; j < grp.elements.Count; j++) {
                    int _id = grp.elements[j];
                    showGroupElement(_id, group_lvl, ref track_y, ref isAnyTrackFoldedOut, ref local_height_group_elements, e, scrollViewBounds);
                }
            }
            rectGroupOutside.height = local_height_group_elements;

        }
        // mouse over group outside
        if(rectGroupOutside.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
            mouseOverElement = (int)ElementType.GroupOutside;
            mouseOverGroupElement = new Vector2(id, -1);
        }
        // draw element position indicator after group
        if(dragType == (int)DragType.GroupElement) {
            if(mouseOverGroupElement.x == id && mouseOverElement == (int)ElementType.GroupOutside) {
                GUI.DrawTexture(new Rect(rectGroup.x, track_y - height_element_position, rectGroup.width, height_element_position), tex_element_position);
            }
            else {
                if(mouseOverGroupElement.x == id && mouseOverElement == (int)ElementType.Group) {
                    GUI.DrawTexture(new Rect(rectGroup.x + 15f, rectGroup.y + rectGroup.height - height_element_position, rectGroup.width - 15f, height_element_position), tex_element_position);
                }
            }
        }
        height_group_elements += local_height_group_elements;
        return false;
    }
    bool showTrack(AMTrack _track, int t, int group_level, ref float track_y, ref bool isAnyTrackFoldedOut, ref float height_group_elements, Event e, Vector2 scrollViewBounds) {
        // track is beyond bounds
        if(track_y + (_track.foldout ? height_track : height_track_foldin) < scrollViewBounds.x || track_y > scrollViewBounds.y) {
            if(_track.foldout) {
                track_y += height_track;
                isAnyTrackFoldedOut = true;
            }
            else {
                track_y += height_track_foldin;
            }
            return false;
        }
        // returns true if mouse over track
        bool mouseOverTrack = false;
        float track_x = width_subtrack_space * group_level;
        bool inGroup = group_level > 0;
        bool isTrackSelected = aData.getCurrentTake().selectedTrack == t;
        bool isTrackContextSelected = aData.getCurrentTake().contextSelectionTracks.Contains(t);
        Rect rectTrack;
        string strStyle;
        if(isTrackSelected) {
            if(aData.getCurrentTake().contextSelectionTracks.Count <= 1)
                strStyle = "GroupElementActive";
            else
                strStyle = "GroupElementSelectedActive";
        }
        else if(isTrackContextSelected) strStyle = "GroupElementSelected";
        else strStyle = "GroupElementNormal";

        rectTrack = new Rect(track_x, track_y, width_track - track_x, height_track_foldin);
        // renaming track
        if(isRenamingTrack != t) {
            Rect rectTrackFoldin = new Rect(rectTrack.x, rectTrack.y, rectTrack.width, rectTrack.height);	// used to toggle track foldout
            if(_track.foldout) {
                rectTrackFoldin.width -= 55f;
            }
            if(GUI.Button(new Rect(rectTrack.x, rectTrack.y, 15f, rectTrack.height), "", "label")) {
                _track.foldout = !_track.foldout;
                timelineSelectTrack(t);
            }
            if(GUI.Button(new Rect(rectTrack.x + 15f, rectTrack.y, rectTrack.width - 15f, rectTrack.height), "", "label")) {
                timelineSelectTrack(t);
                if(didDoubleClick("track" + t + "foldout")) {
                    cancelTextEditting();
                    //_track.foldout = !_track.foldout;
                    isRenamingTrack = t;

                    if(!_track.foldout) _track.foldout = true;
                }
            }
        }
        // set track icon texture
        Texture texIcon = getTrackIconTexture(_track);
        // track start, foldin
        if(!_track.foldout) {
            if(rectTrack.width > 4f) {
                if(rectTrack.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
                    mouseOverTrack = true;
                    mouseOverElement = (int)ElementType.Track;
                    mouseOverGroupElement = new Vector2((inGroup ? aData.getCurrentTake().getTrackGroup(t) : 0), t);
                }
                GUI.BeginGroup(rectTrack, GUI.skin.GetStyle(strStyle));
                GUI.DrawTexture(new Rect(17f, (height_track_foldin - 12f) / 2f, 12f, 12f), texIcon);
                GUI.Label(new Rect(17f + 12f + 2f, 0f, rectTrack.width - (12f + 15f + 2f), height_track_foldin), _track.name);
                GUI.EndGroup();
                // draw foldout
                if(rectTrack.width >= 10f) GUI.DrawTexture(new Rect(track_x, track_y + (height_group - 16f) / 2f, 16f, 16f), (_track.foldout ? GUI.skin.GetStyle("GroupElementFoldout").normal.background : GUI.skin.GetStyle("GroupElementFoldout").active.background));
            }
            else {
                // tooltip hidden track, foldin
                GUI.enabled = false;
                GUI.Button(new Rect(width_track - 80f, track_y, 80f, height_group), trimString(_track.name, 8));
                GUI.enabled = !isPlaying;
            }
            track_y += height_track_foldin;
        }
        else {
            // track start, foldout
            // track rect
            rectTrack = new Rect(track_x, track_y, width_track - track_x, height_track);
            if(rectTrack.width > 4f) {
                // select track texture
                if(rectTrack.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
                    mouseOverTrack = true;
                    mouseOverElement = (int)ElementType.Track;
                    mouseOverGroupElement = new Vector2((inGroup ? aData.getCurrentTake().getTrackGroup(t) : 0), t);
                }
                // draw track texture
                GUI.BeginGroup(rectTrack, GUI.skin.GetStyle(strStyle));
                // track name
                if(isRenamingTrack == t) {
                    GUI.SetNextControlName("RenameTrack" + t);
                    _track.name = GUI.TextField(new Rect(15f, 2f, rectTrack.width - 15f, 20f), _track.name, 20);
                    GUI.FocusControl("RenameTrack" + t);

                }
                else {
                    GUI.Label(new Rect(15f, 0f, rectTrack.width - 15f, 20f), _track.name);
                }
                // track type
                Rect rectTrackIcon = new Rect(4f, 20f, 12f, 12f);
                GUI.Box(rectTrackIcon, texIcon);
                string trackType = _track.getTrackType();
                Rect rectTrackType = new Rect(rectTrackIcon.x + rectTrackIcon.width + 2f, height_track - 39f, rectTrack.width - 20f, 15f);
                if((_track is AMPropertyTrack) && (trackType == "Not Set"))
                    rectTrackType.width -= 48f;
                GUI.Label(rectTrackType, trackType);
                // if property track, show set property button
                if(_track is AMPropertyTrack) {
                    if(!(_track as AMPropertyTrack).obj) GUI.enabled = false;
                    GUIStyle styleButtonSet = new GUIStyle(GUI.skin.button);
                    styleButtonSet.clipping = TextClipping.Overflow;
                    if(GUI.Button(new Rect(width_track - 48f - width_subtrack_space * group_level - 4f, height_track - 38f, 48f, 15f), "Set", styleButtonSet)) {
                        // show property select window 
                        AMPropertySelect.setValues((_track as AMPropertyTrack));
                        EditorWindow.GetWindow(typeof(AMPropertySelect));
                    }
                    GUI.enabled = !isPlaying;
                }
                // track object
                float width_object_field = width_track - track_x;
                showObjectFieldFor(_track, width_object_field, new Rect(padding_track, 39f, width_track - width_subtrack_space * group_level - padding_track * 2, 16f));
                GUI.EndGroup();
            }
            else {
                // tooltip hidden track, foldout
                GUI.enabled = false;
                GUI.Button(new Rect(width_track - 80f, track_y, 80f, height_group), trimString(_track.name, 8));
                GUI.enabled = !isPlaying;
            }
            // track button
            if(GUI.Button(rectTrack, "", "label")) {
                timelineSelectTrack(t);
            }
            // draw foldout
            if(rectTrack.width >= 15f) GUI.DrawTexture(new Rect(track_x, track_y + (height_group - 16f) / 2f, 16f, 16f), (_track.foldout ? GUI.skin.GetStyle("GroupElementFoldout").normal.background : GUI.skin.GetStyle("GroupElementFoldout").active.background));
            track_y += height_track;
            isAnyTrackFoldedOut = true;
            // track end
        }
        // draw element position texture after track
        if(dragType == (int)DragType.GroupElement) {
            if(mouseOverElement == (int)ElementType.Track && mouseOverGroupElement.y == t) {
                GUI.DrawTexture(new Rect(rectTrack.x, rectTrack.y + rectTrack.height - height_element_position, rectTrack.width, height_element_position), tex_element_position);
            }
        }
        height_group_elements += rectTrack.height;
        return mouseOverTrack;
    }
    void showFramesForGroup(int group_id, ref float track_y, Event e, bool birdseye, Vector2 scrollViewBounds) {
        if(track_y > scrollViewBounds.y) return;	// if start y is beyond max y
        AMGroup grp = aData.getCurrentTake().getGroup(group_id);
        // add to track y
        if(group_id < 0) {
            // group height
            track_y += height_group;
            // "no tracks" label
            if(grp.foldout && grp.elements.Count <= 0) {
                track_y += height_group;
            }
        }
        if(group_id == 0 || grp.foldout) {
            foreach(int id in grp.elements) {
                if(id <= 0) {
                    if(track_y > scrollViewBounds.y) return;	// if start y is beyond max y
                    showFramesForGroup(id, ref track_y, e, birdseye, scrollViewBounds);
                }
                else {
                    if(track_y > scrollViewBounds.y) return;	// if start y is beyond max y
                    AMTrack track = aData.getCurrentTake().getTrack(id);
                    if(track)
                        showFrames(track, ref track_y, e, birdseye, scrollViewBounds);
                }
            }
        }
    }
    void showFrames(AMTrack _track, ref float track_y, Event e, bool birdseye, Vector2 scrollViewBounds) {
        //string tooltip = "";
        int t = _track.id;
        int selectedTrack = aData.getCurrentTake().selectedTrack;
        // frames start
        if(!_track.foldout && !oData.showFramesForCollapsedTracks) {
            track_y += height_track_foldin;
            return;
        }
        float numFrames = (aData.getCurrentTake().numFrames < numFramesToRender ? aData.getCurrentTake().numFrames : numFramesToRender);
        Rect rectFrames = new Rect(width_track, track_y, current_width_frame * numFrames, height_track);
        if(!_track.foldout) track_y += height_track_foldin;
        else track_y += height_track;
        if(track_y < scrollViewBounds.x) return; // if end y is before min y
        float _current_height_frame = (_track.foldout ? current_height_frame : height_track_foldin);
        #region frames
        GUI.BeginGroup(rectFrames);
        // draw frames
        bool selected;
        bool ghost = isDragging && aData.getCurrentTake().hasGhostSelection();
        bool isTrackSelected = t == selectedTrack || aData.getCurrentTake().contextSelectionTracks.Contains(t);
        Rect rectFramesBirdsEye = new Rect(0f, 0f, rectFrames.width, _current_height_frame);
        float width_birdseye = current_height_frame * 0.5f;
        if(birdseye) {
            GUI.color = colBirdsEyeFrames;
            GUI.DrawTexture(rectFramesBirdsEye, EditorGUIUtility.whiteTexture);
        }
        else {
            texFrSet.wrapMode = TextureWrapMode.Repeat;
            float startPos = aData.getCurrentTake().startFrame % 5f;
            GUI.DrawTextureWithTexCoords(rectFramesBirdsEye, texFrSet, new Rect(startPos / 5f, 0f, numFrames / 5f, 1f));
            float birdsEyeFadeAlpha = (1f - (current_width_frame - width_frame_birdseye_min)) / 1.2f;
            if(birdsEyeFadeAlpha > 0f) {
                GUI.color = new Color(colBirdsEyeFrames.r, colBirdsEyeFrames.g, colBirdsEyeFrames.b, birdsEyeFadeAlpha);
                GUI.DrawTexture(rectFramesBirdsEye, EditorGUIUtility.whiteTexture);
            }
        }
        GUI.color = new Color(72f / 255f, 72f / 255f, 72f / 255f, 1f);
        GUI.DrawTexture(new Rect(rectFramesBirdsEye.x, rectFramesBirdsEye.y, rectFramesBirdsEye.width, 1f), EditorGUIUtility.whiteTexture);
        GUI.DrawTexture(new Rect(rectFramesBirdsEye.x, rectFramesBirdsEye.y + rectFramesBirdsEye.height - 1f, rectFramesBirdsEye.width, 1f), EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;
        // draw birds eye selection
        if(isTrackSelected) {
            if(ghost) {
                // dragging only one frame that has a key. do not show ghost selection
                if(birdseye && aData.getCurrentTake().contextSelection.Count == 2 && aData.getCurrentTake().contextSelection[0] == aData.getCurrentTake().contextSelection[1] && _track.hasKeyOnFrame(aData.getCurrentTake().contextSelection[0])) {
                    GUI.color = new Color(0f, 0f, 1f, .5f);
                    GUI.DrawTexture(new Rect(current_width_frame * (aData.getCurrentTake().ghostSelection[0] - aData.getCurrentTake().startFrame) - width_birdseye / 2f + current_width_frame / 2f, 0f, width_birdseye, _current_height_frame), texKeyBirdsEye);
                    GUI.color = Color.white;
                }
                else if(aData.getCurrentTake().ghostSelection != null) {
                    // birds eye ghost selection
                    GUI.color = new Color(156f / 255f, 162f / 255f, 216f / 255f, .9f);
                    for(int i = 0; i < aData.getCurrentTake().ghostSelection.Count; i += 2) {
                        int contextFrameStart = aData.getCurrentTake().ghostSelection[i];
                        int contextFrameEnd = aData.getCurrentTake().ghostSelection[i + 1];
                        if(contextFrameStart < (int)aData.getCurrentTake().startFrame) contextFrameStart = (int)aData.getCurrentTake().startFrame;
                        if(contextFrameEnd > (int)aData.getCurrentTake().endFrame) contextFrameEnd = (int)aData.getCurrentTake().endFrame;
                        float contextWidth = (contextFrameEnd - contextFrameStart + 1) * current_width_frame;
                        GUI.DrawTexture(new Rect(rectFramesBirdsEye.x + (contextFrameStart - aData.getCurrentTake().startFrame) * current_width_frame, rectFramesBirdsEye.y + 1f, contextWidth, rectFramesBirdsEye.height - 2f), EditorGUIUtility.whiteTexture);
                    }
                    // draw birds eye ghost key frames
                    GUI.color = new Color(0f, 0f, 1f, .5f);
                    foreach(int _key_frame in aData.getCurrentTake().getKeyFramesInGhostSelection((int)aData.getCurrentTake().startFrame, (int)aData.getCurrentTake().endFrame, t)) {
                        if(birdseye)
                            GUI.DrawTexture(new Rect(current_width_frame * (_key_frame - aData.getCurrentTake().startFrame) - width_birdseye / 2f + current_width_frame / 2f, 0f, width_birdseye, _current_height_frame), texKeyBirdsEye);
                        else {
                            Rect rectFrame = new Rect(current_width_frame * (_key_frame - aData.getCurrentTake().startFrame), 0f, current_width_frame, _current_height_frame);
                            GUI.DrawTexture(new Rect(rectFrame.x + 2f, rectFrame.y + rectFrame.height - (rectFrame.width - 4f) - 2f, rectFrame.width - 4f, rectFrame.width - 4f), texFrKey);
                        }
                    }
                    GUI.color = Color.white;
                }
            }
            else if(aData.getCurrentTake().contextSelection.Count > 0 && /*do not show single frame selection in birdseye*/!(birdseye && aData.getCurrentTake().contextSelection.Count == 2 && aData.getCurrentTake().contextSelection[0] == aData.getCurrentTake().contextSelection[1])) {
                // birds eye context selection
                for(int i = 0; i < aData.getCurrentTake().contextSelection.Count; i += 2) {
                    //GUI.color = new Color(121f/255f,127f/255f,184f/255f,(birdseye ? 1f : .9f));
                    GUI.color = new Color(86f / 255f, 95f / 255f, 178f / 255f, .8f);
                    int contextFrameStart = aData.getCurrentTake().contextSelection[i];
                    int contextFrameEnd = aData.getCurrentTake().contextSelection[i + 1];
                    if(contextFrameStart < (int)aData.getCurrentTake().startFrame) contextFrameStart = (int)aData.getCurrentTake().startFrame;
                    if(contextFrameEnd > (int)aData.getCurrentTake().endFrame) contextFrameEnd = (int)aData.getCurrentTake().endFrame;
                    float contextWidth = (contextFrameEnd - contextFrameStart + 1) * current_width_frame;
                    Rect rectContextSelection = new Rect(rectFramesBirdsEye.x + (contextFrameStart - aData.getCurrentTake().startFrame) * current_width_frame, rectFramesBirdsEye.y + 1f, contextWidth, rectFramesBirdsEye.height - 2f);
                    GUI.DrawTexture(rectContextSelection, EditorGUIUtility.whiteTexture);
                    if(dragType != (int)DragType.ContextSelection) EditorGUIUtility.AddCursorRect(rectContextSelection, MouseCursor.SlideArrow);
                }
                GUI.color = Color.white;
            }
        }
        // birds eye keyframe information, used to draw buttons in proper order
        List<int> birdseyeKeyFrames = new List<int>();
        List<Rect> birdseyeKeyRects = new List<Rect>();
        if(birdseye) {
            // draw birds eye keyframe textures, prepare button rects
            foreach(AMKey key in _track.keys) {
                if(!key) continue;

                selected = ((isTrackSelected) && aData.getCurrentTake().isFrameSelected(key.frame));
                //_track.sortKeys();
                if(key.frame < aData.getCurrentTake().startFrame) continue;
                if(key.frame > aData.getCurrentTake().endFrame) break;
                Rect rectKeyBirdsEye = new Rect(current_width_frame * (key.frame - aData.getCurrentTake().startFrame) - width_birdseye / 2f + current_width_frame / 2f, 0f, width_birdseye, _current_height_frame);
                if(selected) GUI.color = Color.blue;
                GUI.DrawTexture(rectKeyBirdsEye, texKeyBirdsEye);
                GUI.color = Color.white;
                birdseyeKeyFrames.Add(key.frame);
                birdseyeKeyRects.Add(rectKeyBirdsEye);
            }

            // birds eye buttons
            if(birdseyeKeyFrames.Count > 0) {
                for(int i = birdseyeKeyFrames.Count - 1; i >= 0; i--) {
                    selected = ((isTrackSelected) && aData.getCurrentTake().isFrameSelected(birdseyeKeyFrames[i]));
                    if(dragType != (int)DragType.MoveSelection && dragType != (int)DragType.ContextSelection && !isRenamingTake && isRenamingTrack == -1 && mouseOverFrame == 0 && birdseyeKeyRects[i].Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
                        mouseOverFrame = birdseyeKeyFrames[i];
                        mouseOverTrack = t;
                        mouseOverSelectedFrame = (selected);
                    }
                    if(selected && dragType != (int)DragType.ContextSelection) EditorGUIUtility.AddCursorRect(birdseyeKeyRects[i], MouseCursor.SlideArrow);
                }
            }
        }
        else {
            selected = (isTrackSelected);
            foreach(AMKey key in _track.keys) {
                if(!key) continue;

                //_track.sortKeys();
                if(key.frame < aData.getCurrentTake().startFrame) continue;
                if(key.frame > aData.getCurrentTake().endFrame) break;
                Rect rectFrame = new Rect(current_width_frame * (key.frame - aData.getCurrentTake().startFrame), 0f, current_width_frame, _current_height_frame);
                GUI.DrawTexture(new Rect(rectFrame.x + 2f, rectFrame.y + rectFrame.height - (rectFrame.width - 4f) - 2f, rectFrame.width - 4f, rectFrame.width - 4f), texFrKey);
            }
        }
        // click on empty frames
        if(GUI.Button(rectFramesBirdsEye, "", "label") && dragType == (int)DragType.None) {
            int prevFrame = aData.getCurrentTake().selectedFrame;
            bool clickedOnBirdsEyeKey = false;
            for(int i = birdseyeKeyFrames.Count - 1; i >= 0; i--) {
                if(birdseyeKeyFrames[i] > (int)aData.getCurrentTake().endFrame) continue;
                if(birdseyeKeyFrames[i] < (int)aData.getCurrentTake().startFrame) break;
                if(birdseyeKeyRects[i].Contains(e.mousePosition)) {
                    clickedOnBirdsEyeKey = true;
                    // left click
                    if(e.button == 0) {
                        // select the frame
                        timelineSelectFrame(t, birdseyeKeyFrames[i]);
                        // add frame to context selection
                        contextSelectFrame(birdseyeKeyFrames[i], prevFrame);
                        // right click
                    }
                    else if(e.button == 1) {

                        // select track
                        timelineSelectTrack(t);
                        // if context selection is empty, select frame
                        buildContextMenu(birdseyeKeyFrames[i]);
                        // show context menu
                        contextMenu.ShowAsContext();
                    }
                    break;
                }
            }
            if(!clickedOnBirdsEyeKey) {
                int _frame_num_birdseye = (int)aData.getCurrentTake().startFrame + Mathf.CeilToInt(e.mousePosition.x / current_width_frame) - 1;
                // left click
                if(e.button == 0) {
                    // select the frame
                    timelineSelectFrame(t, _frame_num_birdseye);
                    // add frame to context selection
                    contextSelectFrame(_frame_num_birdseye, prevFrame);
                    // right click
                }
                else if(e.button == 1) {
                    timelineSelectTrack(t);
                    // if context selection is empty, select frame
                    buildContextMenu(_frame_num_birdseye);
                    // show context menu
                    contextMenu.ShowAsContext();
                }
            }
        }
        if(!isRenamingTake && isRenamingTrack == -1 && mouseOverFrame == 0 && e.mousePosition.x >= rectFramesBirdsEye.x && e.mousePosition.x <= (rectFramesBirdsEye.x + rectFramesBirdsEye.width)) {
            if(rectFramesBirdsEye.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
                mouseOverFrame = mouseXOverFrame;
                mouseOverTrack = t;
            }
            mouseOverSelectedFrame = ((isTrackSelected) && aData.getCurrentTake().isFrameSelected(mouseXOverFrame));
        }
        #endregion
        if(!oData.disableTimelineActions && _track.foldout) {
            #region timeline actions
            //AudioClip audioClip = null;
            bool drawEachAction = false;
            if(_track is AMAnimationTrack || _track is AMAudioTrack) drawEachAction = true;	// draw each action with seperate textures and buttons for these tracks
            int _startFrame = (int)aData.getCurrentTake().startFrame;
            int _endFrame = (int)(_startFrame + numFrames - 1);
            int action_startFrame, action_endFrame, renderFrameStart, renderFrameEnd;
            int cached_action_startFrame = -1, cached_action_endFrame = -1;
            Texture texBox = texBoxBorder;
            #region group textures / buttons (performance increase)
            Rect rectTimelineActions = new Rect(0f, _current_height_frame, 0f, height_track - current_height_frame);	// used to group textures into one draw call
            if(!drawEachAction) {
                if(_track.keys.Count > 0) {
                    if(_track is AMTranslationTrack && _track.keys.Count > 1 && _track.keys[0] && _track.keys[_track.keys.Count - 1]) {
                        // translation track, from first action frame to end action frame
                        cached_action_startFrame = _track.keys[0].getStartFrame();
                        cached_action_endFrame = (_track.keys[_track.keys.Count - 1] as AMTranslationKey).endFrame;
                        texBox = texBoxGreen;
                    }
                    else if(_track is AMRotationTrack && _track.keys.Count > 1 && _track.keys[0] && _track.keys[_track.keys.Count - 1]) {
                        // rotation track, from first action start frame to last action start frame
                        cached_action_startFrame = _track.keys[0].getStartFrame();
                        cached_action_endFrame = _track.keys[_track.keys.Count - 1].getStartFrame();
                        texBox = texBoxYellow;
                    }
                    else if(_track is AMOrientationTrack && _track.keys.Count > 1 && _track.keys[0] && _track.keys[_track.keys.Count - 1]) {
                        // orientation track, from first action start frame to last action start frame
                        cached_action_startFrame = _track.keys[0].getStartFrame();
                        cached_action_endFrame = _track.keys[_track.keys.Count - 1].getStartFrame();
                        texBox = texBoxOrange;
                    }
                    else if(_track is AMPropertyTrack) {
                        // property track, full track width
                        cached_action_startFrame = _startFrame;
                        cached_action_endFrame = _endFrame;
                        texBox = texBoxLightBlue;
                    }
                    else if(_track is AMEventTrack && _track.keys[0]) {
                        // event track, from first action start frame to end frame
                        cached_action_startFrame = _track.keys[0].getStartFrame();
                        cached_action_endFrame = _endFrame;
                        texBox = texBoxDarkBlue;
                    }
                    else if(_track is AMGOSetActiveTrack && _track.keys[0]) {
                        // go set active track, from first action start frame to end frame
                        cached_action_startFrame = _track.keys[0].getStartFrame();
                        cached_action_endFrame = _endFrame;
                        texBox = texBoxDarkBlue;
                    }
                }
                if(cached_action_startFrame > 0 && cached_action_endFrame > 0) {
                    if(cached_action_startFrame <= _startFrame) {
                        rectTimelineActions.x = 0f;
                    }
                    else {
                        rectTimelineActions.x = (cached_action_startFrame - _startFrame) * current_width_frame;
                    }
                    if(cached_action_endFrame >= _endFrame) {
                        rectTimelineActions.width = rectFramesBirdsEye.width;
                    }
                    else {
                        rectTimelineActions.width = (cached_action_endFrame - (_startFrame >= cached_action_startFrame ? _startFrame : cached_action_startFrame) + 1) * current_width_frame;
                    }
                    // draw timeline action texture

                    if(rectTimelineActions.width > 0f) GUI.DrawTexture(rectTimelineActions, texBox);
                }

            }
            #endregion
            string txtInfo;
            Rect rectBox;
            // draw box for each action in track
            bool didClampBackwards = false;	// whether or not clamped backwards, used to break infinite loop
            int last_action_startFrame = -1;
            for(int i = 0; i < _track.keys.Count; i++) {
                if(_track.keys[i] == null) continue;

                #region calculate dimensions
                int clamped = 0; // 0 = no clamp, -1 = backwards clamp, 1 = forwards clamp
                if(_track.keys[i].version != _track.version) {
                    // if cache is null, recheck for component and update caches
                    //aData = (AnimatorData)GameObject.Find("AnimatorData").GetComponent("AnimatorData");
                    aData.getCurrentTake().maintainCaches();
                }
                if((_track is AMAudioTrack) && ((_track.keys[i] as AMAudioKey).getNumberOfFrames(aData.getCurrentTake().frameRate)) > -1 && (_track.keys[i].getStartFrame() + (_track.keys[i] as AMAudioKey).getNumberOfFrames(aData.getCurrentTake().frameRate) <= aData.getCurrentTake().numFrames)) {
                    // based on audio clip length
                    action_startFrame = _track.keys[i].getStartFrame();
                    action_endFrame = _track.keys[i].getStartFrame() + (_track.keys[i] as AMAudioKey).getNumberOfFrames(aData.getCurrentTake().frameRate);
                    //audioClip = (_track.cache[i] as AMAudioAction).audioClip;
                    // if intersects new audio clip, then cut
                    if(i < _track.keys.Count - 1) {
                        if(action_endFrame > _track.keys[i + 1].getStartFrame()) action_endFrame = _track.keys[i + 1].getStartFrame();
                    }
                }
                else if((_track is AMAnimationTrack) && ((_track.keys[i] as AMAnimationKey).getNumberOfFrames(aData.getCurrentTake().frameRate)) > -1 && (_track.keys[i].getStartFrame() + (_track.keys[i] as AMAnimationKey).getNumberOfFrames(aData.getCurrentTake().frameRate) <= aData.getCurrentTake().numFrames)) {
                    // based on animation clip length
                    action_startFrame = _track.keys[i].getStartFrame();
                    action_endFrame = _track.keys[i].getStartFrame() + (_track.keys[i] as AMAnimationKey).getNumberOfFrames(aData.getCurrentTake().frameRate);
                    // if intersects new animation clip, then cut
                    if(i < _track.keys.Count - 1) {
                        if(action_endFrame > _track.keys[i + 1].getStartFrame()) action_endFrame = _track.keys[i + 1].getStartFrame();
                    }
                }
                else if((i == 0) && (!didClampBackwards) && (_track is AMPropertyTrack || _track is AMGOSetActiveTrack)) {
                    // clamp behind if first action
                    action_startFrame = 1;
                    action_endFrame = _track.keys[0].getStartFrame();
                    i--;
                    didClampBackwards = true;
                    clamped = -1;
                }
                else if((_track is AMAnimationTrack) || (_track is AMAudioTrack) || (_track is AMPropertyTrack) || (_track is AMEventTrack) || (_track is AMGOSetActiveTrack)) {
                    // single frame tracks (clamp box to last frame) (if audio track not set, clamp)
                    action_startFrame = _track.keys[i].getStartFrame();
                    if(i < _track.keys.Count - 1) {
                        action_endFrame = _track.keys[i + 1].getStartFrame();
                    }
                    else {
                        clamped = 1;
                        action_endFrame = _endFrame;
                        if(action_endFrame > aData.getCurrentTake().numFrames) action_endFrame = aData.getCurrentTake().numFrames + 1;
                    }
                }
                else {
                    // tracks with start frame and end frame (do not clamp box, stop before last key)
                    if(_track.keys[i].getNumberOfFrames() <= 0) continue;
                    action_startFrame = _track.keys[i].getStartFrame();
                    action_endFrame = _track.keys[i].getStartFrame() + _track.keys[i].getNumberOfFrames();
                }
                if(action_startFrame > _endFrame) {
                    last_action_startFrame = action_startFrame;
                    continue;
                }
                if(action_endFrame < _startFrame) {
                    last_action_startFrame = action_startFrame;
                    continue;
                }
                if(i >= 0) txtInfo = getInfoTextForAction(_track, _track.keys[i], false, clamped);
                else txtInfo = getInfoTextForAction(_track, _track.keys[0], true, clamped);
                float rectLeft, rectWidth; ;
                float rectTop = current_height_frame;
                float rectHeight = height_track - current_height_frame;
                // set info box position and dimensions
                bool showLeftAnchor = true;
                bool showRightAnchor = true;
                if(action_startFrame < _startFrame) {
                    rectLeft = 0f;
                    renderFrameStart = _startFrame;
                    showLeftAnchor = false;
                }
                else {
                    rectLeft = (action_startFrame - _startFrame) * current_width_frame;
                    renderFrameStart = action_startFrame;
                }
                if(action_endFrame > _endFrame) {
                    renderFrameEnd = _endFrame;
                    showRightAnchor = false;
                }
                else {
                    renderFrameEnd = action_endFrame;
                }
                rectWidth = (renderFrameEnd - renderFrameStart + 1) * current_width_frame;
                rectBox = new Rect(rectLeft, rectTop, rectWidth, rectHeight);
                #endregion
                #region draw action
                if(_track is AMAnimationTrack) texBox = texBoxRed;
                else if(_track is AMPropertyTrack) texBox = texBoxLightBlue;
                else if(_track is AMTranslationTrack) texBox = texBoxGreen;
                else if(_track is AMAudioTrack) texBox = texBoxPink;
                else if(_track is AMRotationTrack) texBox = texBoxYellow;
                else if(_track is AMOrientationTrack) texBox = texBoxOrange;
                else if(_track is AMEventTrack) texBox = texBoxDarkBlue;
                else if(_track is AMGOSetActiveTrack) texBox = texBoxDarkBlue;
                else texBox = texBoxBorder;
                if(drawEachAction) {
                    GUI.DrawTexture(rectBox, texBox);
                    //if(audioClip) GUI.DrawTexture(rectBox,AssetPreview.GetAssetPreview(audioClip));
                }
                // info tex label
                bool hideTxtInfo = (GUI.skin.label.CalcSize(new GUIContent(txtInfo)).x > rectBox.width);
                GUIStyle styleTxtInfo = new GUIStyle(GUI.skin.label);
                styleTxtInfo.normal.textColor = Color.white;
                styleTxtInfo.alignment = (hideTxtInfo ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
                bool isLastAction;
                if(_track is AMPropertyTrack || _track is AMEventTrack || _track is AMGOSetActiveTrack) isLastAction = (i == _track.keys.Count - 1);
                else if(_track is AMAudioTrack || _track is AMAnimationTrack) isLastAction = false;
                else isLastAction = (i == _track.keys.Count - 2);
                if(rectBox.width > 5f) EditorGUI.DropShadowLabel(new Rect(rectBox.x, rectBox.y, rectBox.width - (!isLastAction ? current_width_frame : 0f), rectBox.height), txtInfo, styleTxtInfo);
                // if clicked on info box, select the starting frame for action. show tooltip if text does not fit
                if(drawEachAction && GUI.Button(rectBox, /*(hideTxtInfo ? new GUIContent("",txtInfo) : new GUIContent(""))*/"", "label") && dragType != (int)DragType.ResizeAction) {
                    int prevFrame = aData.getCurrentTake().selectedFrame;
                    // timeline select
                    timelineSelectFrame(t, (clamped == -1 ? action_endFrame : action_startFrame));
                    // clear and add frame to context selection
                    contextSelectFrame((clamped == -1 ? action_endFrame : action_startFrame), prevFrame);
                }
                if(rectBox.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
                    mouseOverElement = (int)ElementType.TimelineAction;
                    mouseOverTrack = t;
                    if(hideTxtInfo) tooltip = txtInfo;
                }
                #endregion
                #region draw anchors
                if(showLeftAnchor) {
                    Rect rectBoxAnchorLeft = new Rect(rectBox.x - 1f, rectBox.y, 2f, rectBox.height);
                    GUI.DrawTexture(rectBoxAnchorLeft, texBoxBorder);
                    Rect rectBoxAnchorLeftOffset = new Rect(rectBoxAnchorLeft);
                    rectBoxAnchorLeftOffset.width += 6f;
                    rectBoxAnchorLeftOffset.x -= 3f;
                    // info box anchor cursor 
                    if(i >= 0) {
                        EditorGUIUtility.AddCursorRect(new Rect(rectBoxAnchorLeftOffset.x + 1f, rectBoxAnchorLeftOffset.y, rectBoxAnchorLeftOffset.width - 2f, rectBoxAnchorLeftOffset.height), MouseCursor.ResizeHorizontal);
                        if(rectBoxAnchorLeftOffset.Contains(e.mousePosition) && (mouseOverElement == (int)ElementType.None || mouseOverElement == (int)ElementType.TimelineAction)) {
                            mouseOverElement = (int)ElementType.ResizeAction;
                            if(dragType == (int)DragType.None) {
                                if(_track.hasKeyOnFrame(last_action_startFrame)) startResizeActionFrame = last_action_startFrame;
                                else startResizeActionFrame = -1;
                                resizeActionFrame = action_startFrame;
                                if(_track is AMAnimationTrack || _track is AMAudioTrack) {
                                    endResizeActionFrame = _track.getKeyFrameAfterFrame(action_startFrame, false);
                                }
                                else endResizeActionFrame = action_endFrame;
                                mouseOverTrack = t;
                                arrKeyRatiosLeft = _track.getKeyFrameRatiosInBetween(startResizeActionFrame, resizeActionFrame);
                                arrKeyRatiosRight = _track.getKeyFrameRatiosInBetween(resizeActionFrame, endResizeActionFrame);
                                arrKeysLeft = _track.getKeyFramesInBetween(startResizeActionFrame, resizeActionFrame);
                                arrKeysRight = _track.getKeyFramesInBetween(resizeActionFrame, endResizeActionFrame);
                            }
                        }
                    }
                }
                // draw right anchor if last timeline action
                if(showRightAnchor && isLastAction) {
                    Rect rectBoxAnchorRight = new Rect(rectBox.x + rectBox.width - 1f, rectBox.y, 2f, rectBox.height);
                    GUI.DrawTexture(rectBoxAnchorRight, texBoxBorder);
                    Rect rectBoxAnchorRightOffset = new Rect(rectBoxAnchorRight);
                    rectBoxAnchorRightOffset.width += 6f;
                    rectBoxAnchorRightOffset.x -= 3f;
                    EditorGUIUtility.AddCursorRect(new Rect(rectBoxAnchorRightOffset.x + 1f, rectBoxAnchorRightOffset.y, rectBoxAnchorRightOffset.width - 2f, rectBoxAnchorRightOffset.height), MouseCursor.ResizeHorizontal);
                    if(rectBoxAnchorRightOffset.Contains(e.mousePosition) && (mouseOverElement == (int)ElementType.None || mouseOverElement == (int)ElementType.TimelineAction)) {
                        mouseOverElement = (int)ElementType.ResizeAction;
                        if(dragType == (int)DragType.None) {
                            startResizeActionFrame = action_startFrame;
                            resizeActionFrame = action_endFrame;
                            endResizeActionFrame = -1;
                            mouseOverTrack = t;
                            arrKeyRatiosLeft = _track.getKeyFrameRatiosInBetween(startResizeActionFrame, resizeActionFrame);
                            arrKeyRatiosRight = _track.getKeyFrameRatiosInBetween(resizeActionFrame, endResizeActionFrame);
                            arrKeysLeft = _track.getKeyFramesInBetween(startResizeActionFrame, resizeActionFrame);
                            arrKeysRight = _track.getKeyFramesInBetween(resizeActionFrame, endResizeActionFrame);
                        }
                    }
                }
                #endregion
                last_action_startFrame = action_startFrame;
            }
            if(!drawEachAction) {
                // timeline action button
                if(GUI.Button(rectTimelineActions,/*new GUIContent("",tooltip)*/"", "label") && dragType == (int)DragType.None) {
                    int _frame_num_action = (int)aData.getCurrentTake().startFrame + Mathf.CeilToInt(e.mousePosition.x / current_width_frame) - 1;
                    AMKey _action = _track.getKeyContainingFrame(_frame_num_action);
                    int prevFrame = aData.getCurrentTake().selectedFrame;
                    // timeline select
                    timelineSelectFrame(t, _action.getStartFrame());
                    // clear and add frame to context selection
                    contextSelectFrame(_action.getStartFrame(), prevFrame);
                }
            }


            #endregion
        }
        GUI.EndGroup();
    }
    void showInspectorPropertiesFor(Rect rect, int _track, int _frame, Event e) {
        // if there are no tracks, return
        if(aData.getCurrentTake().getTrackCount() <= 0) return;
        string track_name = "";
        AMTrack sTrack = null;
        if(_track > -1) {
            // get the selected track
            sTrack = aData.getCurrentTake().getTrack(_track);
            track_name = sTrack.name + ", ";
        }
        GUIStyle styleLabelWordwrap = new GUIStyle(GUI.skin.label);
        styleLabelWordwrap.wordWrap = true;
        string strFrameInfo = track_name;
        if(oData.time_numbering) strFrameInfo += "Time " + frameToTime(_frame, (float)aData.getCurrentTake().frameRate).ToString("N2") + " s";
        else strFrameInfo += "Frame " + _frame;
        GUI.Label(new Rect(0f, 0f, width_inspector_open - width_inspector_closed - width_button_delete - margin, 20f), strFrameInfo, styleLabelWordwrap);
        Rect rectBtnDeleteKey = new Rect(width_inspector_open - width_inspector_closed - width_button_delete - margin, 0f, width_button_delete, height_button_delete);
        // if frame has no key or isPlaying, return
        if(_track <= -1 || !sTrack.hasKeyOnFrame(_frame) || isPlaying) {
            GUI.enabled = false;
            // disabled delete key button
            GUI.Button(rectBtnDeleteKey, (getSkinTextureStyleState("delete").background), GUI.skin.GetStyle("ButtonImage"));
            GUI.enabled = !isPlaying;
            return;
        }
        // delete key button
        if(GUI.Button(rectBtnDeleteKey, new GUIContent("", "Delete Key"), GUI.skin.GetStyle("ButtonImage"))) {
            deleteKeyFromSelectedFrame();
            return;
        }
        GUI.DrawTexture(new Rect(rectBtnDeleteKey.x + (rectBtnDeleteKey.height - 10f) / 2f, rectBtnDeleteKey.y + (rectBtnDeleteKey.width - 10f) / 2f, 10f, 10f), (getSkinTextureStyleState((rectBtnDeleteKey.Contains(e.mousePosition) ? "delete_hover" : "delete")).background));
        float width_inspector = width_inspector_open - width_inspector_closed;
        float start_y = 30f + height_inspector_space;
        #region translation inspector
        if(sTrack is AMTranslationTrack) {
            AMTranslationKey tKey = (AMTranslationKey)(sTrack as AMTranslationTrack).getKeyOnFrame(_frame);
            // translation interpolation
            Rect rectLabelInterp = new Rect(0f, start_y, 50f, 20f);
            GUI.Label(rectLabelInterp, "Interpl.");
            Rect rectSelGrid = new Rect(rectLabelInterp.x + rectLabelInterp.width + margin, rectLabelInterp.y, width_inspector - rectLabelInterp.width - margin * 2f, rectLabelInterp.height);
            if(tKey.setInterpolation(GUI.SelectionGrid(rectSelGrid, tKey.interp, texInterpl, 2, GUI.skin.GetStyle("ButtonImage")))) {
                sTrack.updateCache();
                AMCodeView.refresh();
                // select the current frame
                timelineSelectFrame(aData.getCurrentTake().selectedTrack, aData.getCurrentTake().selectedFrame);
                // save data
                setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                // refresh component
                refreshGizmos();
            }
            // translation position
            Rect rectPosition = new Rect(0f, rectSelGrid.y + rectSelGrid.height + height_inspector_space, width_inspector - margin, 40f);
            if(tKey.setPosition(EditorGUI.Vector3Field(rectPosition, "Position", tKey.position))) {
                // update cache when modifying varaibles
                sTrack.updateCache();
                AMCodeView.refresh();
                // preview new position
                aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
                // save data
                setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                // refresh component
                refreshGizmos();
            }

            // if not only key, show ease
            bool isTKeyLastFrame = tKey == (sTrack as AMTranslationTrack).keys[(sTrack as AMTranslationTrack).keys.Count - 1];

            if(!isTKeyLastFrame) {
                Rect recEasePicker = new Rect(0f, rectPosition.y + rectPosition.height + height_inspector_space, width_inspector - margin, 0f);
                if(!isTKeyLastFrame && tKey.interp == (int)AMTranslationKey.Interpolation.Linear) {
                    showEasePicker(sTrack, tKey, aData, recEasePicker.x, recEasePicker.y, recEasePicker.width);
                }
                else {
                    showEasePicker(sTrack, (sTrack as AMTranslationTrack).getKeyStartFor(tKey.frame), aData, recEasePicker.x, recEasePicker.y, recEasePicker.width);
                }
            }
            return;
        }
        #endregion
        #region rotation inspector
        if(sTrack is AMRotationTrack) {
            AMRotationKey rKey = (AMRotationKey)(sTrack as AMRotationTrack).getKeyOnFrame(_frame);
            Rect rectQuaternion = new Rect(0f, start_y, width_inspector - margin, 40f);
            // quaternion
            if(rKey.setRotationQuaternion(EditorGUI.Vector4Field(rectQuaternion, "Quaternion", rKey.getRotationQuaternion()))) {
                // update cache when modifying varaibles
                sTrack.updateCache();
                AMCodeView.refresh();
                // preview new position
                aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
                // save data
                setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                // refresh component
                refreshGizmos();
            }
            // if not last key, show ease
            if(rKey != (sTrack as AMRotationTrack).keys[(sTrack as AMRotationTrack).keys.Count - 1]) {
                Rect recEasePicker = new Rect(0f, rectQuaternion.y + rectQuaternion.height + height_inspector_space, width_inspector - margin, 0f);
                if((sTrack as AMRotationTrack).getKeyIndexForFrame(_frame) > -1) {
                    showEasePicker(sTrack, rKey, aData, recEasePicker.x, recEasePicker.y, recEasePicker.width);
                }
            }
            return;
        }
        #endregion
        #region orientation inspector
        if(sTrack is AMOrientationTrack) {
            AMOrientationKey oKey = (AMOrientationKey)(sTrack as AMOrientationTrack).getKeyOnFrame(_frame);
            // target
            Rect rectLabelTarget = new Rect(0f, start_y, 50f, 22f);
            GUI.Label(rectLabelTarget, "Target");
            Rect rectObjectTarget = new Rect(rectLabelTarget.x + rectLabelTarget.width + 3f, rectLabelTarget.y + 3f, width_inspector - rectLabelTarget.width - 3f - margin - width_button_delete, 16f);
            if(oKey.setTarget((Transform)EditorGUI.ObjectField(rectObjectTarget, oKey.target, typeof(Transform), true))) {
                // update cache when modifying varaibles
                (sTrack as AMOrientationTrack).updateCache();
                AMCodeView.refresh();
                // preview
                aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
                // save data
                setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                // refresh component
                refreshGizmos();
            }
            Rect rectNewTarget = new Rect(width_inspector - width_button_delete - margin, rectLabelTarget.y, width_button_delete, width_button_delete);
            if(GUI.Button(rectNewTarget, "+")) {
                GenericMenu addTargetMenu = new GenericMenu();
                addTargetMenu.AddItem(new GUIContent("With Translation"), false, addTargetWithTranslationTrack, oKey);
                addTargetMenu.AddItem(new GUIContent("Without Translation"), false, addTargetWithoutTranslationTrack, oKey);
                addTargetMenu.ShowAsContext();
            }
            // if not last key, show ease
            if(oKey != (sTrack as AMOrientationTrack).keys[(sTrack as AMOrientationTrack).keys.Count - 1]) {
                int oActionIndex = (sTrack as AMOrientationTrack).getKeyIndexForFrame(_frame);
                if(oActionIndex > -1 && (sTrack.keys[oActionIndex] as AMOrientationKey).target != (sTrack.keys[oActionIndex] as AMOrientationKey).endTarget) {
                    Rect recEasePicker = new Rect(0f, rectNewTarget.y + rectNewTarget.height + height_inspector_space, width_inspector - margin, 0f);
                    showEasePicker(sTrack, oKey, aData, recEasePicker.x, recEasePicker.y, recEasePicker.width);
                }
            }
            return;
        }
        #endregion
        #region animation inspector
        if(sTrack is AMAnimationTrack) {
            AMAnimationKey aKey = (AMAnimationKey)(sTrack as AMAnimationTrack).getKeyOnFrame(_frame);
            // animation clip
            Rect rectLabelAnimClip = new Rect(0f, start_y, 100f, 22f);
            GUI.Label(rectLabelAnimClip, "Animation Clip");
            Rect rectObjectField = new Rect(rectLabelAnimClip.x + rectLabelAnimClip.width + 2f, rectLabelAnimClip.y + 3f, width_inspector - rectLabelAnimClip.width - margin, 16f);
            if(aKey.setAmClip((AnimationClip)EditorGUI.ObjectField(rectObjectField, aKey.amClip, typeof(AnimationClip), false))) {
                // update cache when modifying varaibles
                sTrack.updateCache();
                AMCodeView.refresh();
                // preview new position
                aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
                // save data
                setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                // refresh component
                refreshGizmos();
            }
            // wrap mode
            Rect rectLabelWrapMode = new Rect(0f, rectLabelAnimClip.y + rectLabelAnimClip.height + height_inspector_space, 85f, 22f);
            GUI.Label(rectLabelWrapMode, "Wrap Mode");
            Rect rectPopupWrapMode = new Rect(rectLabelWrapMode.x + rectLabelWrapMode.width, rectLabelWrapMode.y + 3f, 120f, 22f);
            if(aKey.setWrapMode(indexToWrapMode(EditorGUI.Popup(rectPopupWrapMode, wrapModeToIndex(aKey.wrapMode), wrapModeNames)))) {
                // update cache when modifying varaibles
                sTrack.updateCache();
                AMCodeView.refresh();
                // preview new position
                aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
                // save data
                setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                // refresh component
                refreshGizmos();
            }
            // crossfade
            Rect rectLabelCrossfade = new Rect(0f, rectLabelWrapMode.y + rectPopupWrapMode.height + height_inspector_space, 85f, 22f);
            GUI.Label(rectLabelCrossfade, "Crossfade");
            Rect rectToggleCrossfade = new Rect(rectLabelCrossfade.x + rectLabelCrossfade.width, rectLabelCrossfade.y + 2f, 20f, rectLabelCrossfade.height);
            if(aKey.setCrossFade(EditorGUI.Toggle(rectToggleCrossfade, aKey.crossfade))) {
                // update cache when modifying varaibles
                sTrack.updateCache();
                AMCodeView.refresh();
                // preview new position
                aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
                // save data
                setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                // refresh component
                refreshGizmos();
            }
            Rect rectLabelCrossFadeTime = new Rect(rectToggleCrossfade.x + rectToggleCrossfade.width + 10f, rectLabelCrossfade.y, 35f, rectToggleCrossfade.height);
            if(!aKey.crossfade) GUI.enabled = false;
            GUI.Label(rectLabelCrossFadeTime, "Time");
            Rect rectFloatFieldCrossFade = new Rect(rectLabelCrossFadeTime.x + rectLabelCrossFadeTime.width + margin, rectLabelCrossFadeTime.y + 3f, 40f, rectLabelCrossFadeTime.height);
            if(aKey.setCrossfadeTime(EditorGUI.FloatField(rectFloatFieldCrossFade, aKey.crossfadeTime))) {
                // update cache when modifying varaibles
                sTrack.updateCache();
                AMCodeView.refresh();
                // save data
                setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                // refresh component
                refreshGizmos();
            }
            Rect rectLabelSeconds = new Rect(rectFloatFieldCrossFade.x + rectFloatFieldCrossFade.width + margin, rectLabelCrossFadeTime.y, 20f, rectLabelCrossFadeTime.height);
            GUI.Label(rectLabelSeconds, "s");
            GUI.enabled = true;
        }
        #endregion
        #region audio inspector
        if(sTrack is AMAudioTrack) {
            AMAudioKey auKey = (AMAudioKey)(sTrack as AMAudioTrack).getKeyOnFrame(_frame);
            // audio clip
            Rect rectLabelAudioClip = new Rect(0f, start_y, 80f, 22f);
            GUI.Label(rectLabelAudioClip, "Audio Clip");
            Rect rectObjectField = new Rect(rectLabelAudioClip.x + rectLabelAudioClip.width + margin, rectLabelAudioClip.y + 3f, width_inspector - rectLabelAudioClip.width - margin, 16f);
            if(auKey.setAudioClip((AudioClip)EditorGUI.ObjectField(rectObjectField, auKey.audioClip, typeof(AudioClip), false))) {
                // update cache when modifying varaibles
                sTrack.updateCache();
                AMCodeView.refresh();
                // save data
                setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                // refresh component
                refreshGizmos();

            }
            Rect rectLabelLoop = new Rect(0f, rectLabelAudioClip.y + rectLabelAudioClip.height + height_inspector_space, 80f, 22f);
            // loop audio
            GUI.Label(rectLabelLoop, "Loop");
            Rect rectToggleLoop = new Rect(rectLabelLoop.x + rectLabelLoop.width + margin, rectLabelLoop.y + 2f, 22f, 22f);
            if(auKey.setLoop(EditorGUI.Toggle(rectToggleLoop, auKey.loop))) {
                // update cache when modifying varaibles
                sTrack.updateCache();
                AMCodeView.refresh();
                // save data
                setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                // refresh component
                refreshGizmos();
            }
            return;
        }
        #endregion
        #region property inspector
        if(sTrack is AMPropertyTrack) {
            AMPropertyKey pKey = (AMPropertyKey)(sTrack as AMPropertyTrack).getKeyOnFrame(_frame);
            // value
            string propertyLabel = (sTrack as AMPropertyTrack).getTrackType();
            Rect rectField = new Rect(0f, start_y, width_inspector - margin, 22f);

            if(((sTrack as AMPropertyTrack).valueType == (int)AMPropertyTrack.ValueType.Integer) || ((sTrack as AMPropertyTrack).valueType == (int)AMPropertyTrack.ValueType.Long)) {
                if(pKey.setValue(EditorGUI.IntField(rectField, propertyLabel, Convert.ToInt32(pKey.val)))) {
                    // update cache when modifying varaibles
                    sTrack.updateCache();
                    AMCodeView.refresh();
                    // preview new value
                    aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
                    // save data
                    setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                    // refresh component
                    refreshGizmos();
                }
            }
            else if(((sTrack as AMPropertyTrack).valueType == (int)AMPropertyTrack.ValueType.Float) || ((sTrack as AMPropertyTrack).valueType == (int)AMPropertyTrack.ValueType.Double)) {
                if(pKey.setValue(EditorGUI.FloatField(rectField, propertyLabel, (float)(pKey.val)))) {
                    // update cache when modifying varaibles
                    sTrack.updateCache();
                    AMCodeView.refresh();
                    // preview new value
                    aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
                    // save data
                    setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                    // refresh component
                    refreshGizmos();
                }
            }
            else if((sTrack as AMPropertyTrack).valueType == (int)AMPropertyTrack.ValueType.Vector2) {
                rectField.height = 40f;
                if(pKey.setValue(EditorGUI.Vector2Field(rectField, propertyLabel, pKey.vect2))) {
                    // update cache when modifying varaibles
                    sTrack.updateCache();
                    AMCodeView.refresh();
                    // preview new value
                    aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
                    // save data
                    setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                    // refresh component
                    refreshGizmos();
                }
            }
            else if((sTrack as AMPropertyTrack).valueType == (int)AMPropertyTrack.ValueType.Vector3) {
                rectField.height = 40f;
                if(pKey.setValue(EditorGUI.Vector3Field(rectField, propertyLabel, pKey.vect3))) {
                    // update cache when modifying varaibles
                    sTrack.updateCache();
                    AMCodeView.refresh();
                    // preview new value
                    aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
                    // save data
                    setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                    // refresh component
                    refreshGizmos();
                }
            }
            else if((sTrack as AMPropertyTrack).valueType == (int)AMPropertyTrack.ValueType.Color) {
                rectField.height = 22f;
                if(pKey.setValue(EditorGUI.ColorField(rectField, propertyLabel, pKey.color))) {
                    // update cache when modifying varaibles
                    sTrack.updateCache();
                    AMCodeView.refresh();
                    // preview new value
                    aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
                    // save data
                    setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                    // refresh component
                    refreshGizmos();
                }
            }
            else if((sTrack as AMPropertyTrack).valueType == (int)AMPropertyTrack.ValueType.Rect) {
                rectField.height = 60f;
                if(pKey.setValue(EditorGUI.RectField(rectField, propertyLabel, pKey.rect))) {
                    // update cache when modifying varaibles
                    sTrack.updateCache();
                    AMCodeView.refresh();
                    // preview new value
                    aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
                    // save data
                    setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                    // refresh component
                    refreshGizmos();
                }
            }
            else if((sTrack as AMPropertyTrack).valueType == (int)AMPropertyTrack.ValueType.Vector4
                || (sTrack as AMPropertyTrack).valueType == (int)AMPropertyTrack.ValueType.Quaternion) {
                rectField.height = 40f;
                if(pKey.setValue(EditorGUI.Vector4Field(rectField, propertyLabel, pKey.vect4))) {
                    // update cache when modifying varaibles
                    sTrack.updateCache();
                    AMCodeView.refresh();
                    // preview new value
                    aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
                    // save data
                    setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                    // refresh component
                    refreshGizmos();
                }
            }
            // property ease, show if not last key (check for action; there is no rotation action for last key). do not show for morph channels, because it is shown before the parameters
            if(pKey != (sTrack as AMPropertyTrack).keys[(sTrack as AMPropertyTrack).keys.Count - 1]) {
                Rect rectEasePicker = new Rect(0f, rectField.y + rectField.height + height_inspector_space, width_inspector - margin, 0f);
                showEasePicker(sTrack, pKey, aData, rectEasePicker.x, rectEasePicker.y, rectEasePicker.width);
            }
            return;
        }
        #endregion
        #region event set go active
        if(sTrack is AMGOSetActiveTrack) {
            AMGOSetActiveTrack goActiveTrack = sTrack as AMGOSetActiveTrack;
            AMGOSetActiveKey pKey = (AMGOSetActiveKey)goActiveTrack.getKeyOnFrame(_frame);

            bool doRefreshGizmo = false;

            bool newStartVal;

            // value
            if(sTrack.keys[0] == pKey) {
                Rect rectStartField = new Rect(0f, start_y, width_inspector - margin, 22f);

                newStartVal = EditorGUI.Toggle(rectStartField, "Default Active", goActiveTrack.startActive);

                start_y += rectStartField.height + 4.0f;
            }
            else
                newStartVal = goActiveTrack.startActive;

            Rect rectField = new Rect(0f, start_y, width_inspector - margin, 22f);

            bool newVal = EditorGUI.Toggle(rectField, sTrack.getTrackType(), pKey.setActive);

            if(newStartVal != goActiveTrack.startActive) {
                goActiveTrack.startActive = newStartVal;

                EditorUtility.SetDirty(goActiveTrack);

                doRefreshGizmo = true;
            }

            if(newVal != pKey.setActive) {
                pKey.setActive = newVal;

                // update cache when modifying varaibles
                sTrack.updateCache();
                // preview new value
                aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
                // save data
                setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));

                doRefreshGizmo = true;
            }

            if(doRefreshGizmo) {
                AMCodeView.refresh();

                // refresh component
                refreshGizmos();
            }

            return;
        }
        #endregion
        #region event inspector
        if(sTrack is AMEventTrack) {
            AMEventKey eKey = (AMEventKey)(sTrack as AMEventTrack).getKeyOnFrame(_frame);
            // value
            if(indexMethodInfo == -1 || cachedMethodInfo.Count <= 0) {
                Rect rectLabel = new Rect(0f, start_y, width_inspector - margin * 2f - 20f, 22f);
                GUI.Label(rectLabel, "No usable methods found.");
                Rect rectButton = new Rect(width_inspector - 20f - margin, start_y + 1f, 20f, 20f);
                if(GUI.Button(rectButton, "?")) {
                    EditorUtility.DisplayDialog("Usable Methods", "Methods should be made public and be placed in scripts that are not directly derived from Component or Behaviour to be used in the Event Track (MonoBehaviour is fine).", "Okay");
                }
                return;
            }
            bool sendMessageToggleEnabled = true;
            Rect rectPopup = new Rect(0f, start_y, width_inspector - margin, 22f);
            indexMethodInfo = EditorGUI.Popup(rectPopup, indexMethodInfo, getMethodNames());
            // if index out of range
            bool paramMatched = eKey.isMatch(cachedParameterInfos);
            if((indexMethodInfo < cachedMethodInfo.Count) || !paramMatched) {
                // process change
                // update cache when modifying varaibles
                if(eKey.setMethodInfo(cachedMethodInfoComponents[indexMethodInfo], cachedMethodInfo[indexMethodInfo], cachedParameterInfos, !paramMatched)) {
                    sTrack.updateCache();
                    AMCodeView.refresh();
                    // save data
                    setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                    // deselect fields
                    GUIUtility.keyboardControl = 0;
                }
            }
            if(cachedParameterInfos.Length > 1) {
                // if method has more than 1 parameter, set sendmessage to false, and disable toggle
                if(eKey.setUseSendMessage(false)) {
                    sTrack.updateCache();
                    AMCodeView.refresh();
                    // save data
                    setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                }
                sendMessageToggleEnabled = false;	// disable sendmessage toggle
            }
            bool showObjectMessage = false;
            Type showObjectType = null;
            foreach(ParameterInfo p in cachedParameterInfos) {
                Type elemType = p.ParameterType.GetElementType();
                if(elemType != null && (elemType.BaseType == typeof(UnityEngine.Object) || elemType.BaseType == typeof(UnityEngine.Behaviour))) {
                    showObjectMessage = true;
                    showObjectType = elemType;
                    break;
                }
            }
            Rect rectLabelObjectMessage = new Rect(0f, rectPopup.y + rectPopup.height, width_inspector - margin * 2f - 20f, 0f);
            if(showObjectMessage) {
                rectLabelObjectMessage.height = 22f;
                Rect rectButton = new Rect(width_inspector - 20f - margin, rectLabelObjectMessage.y + 1f, 20f, 20f);
                GUI.color = Color.red;
                GUI.Label(rectLabelObjectMessage, "* Use Object[] instead!");
                GUI.color = Color.white;
                if(GUI.Button(rectButton, "?")) {
                    EditorUtility.DisplayDialog("Use Object[] Parameter Instead", "Array types derived from Object, such as GameObject[], cannot be cast correctly on runtime.\n\nUse UnityEngine.Object[] as a parameter type and then cast to (GameObject[]) in your method.\n\nIf you're trying to pass components" + (showObjectType != typeof(GameObject) ? " (such as " + showObjectType.ToString() + ")" : "") + ", you should get them from the casted GameObjects on runtime.\n\nPlease see the documentation for more information.", "Okay");
                }

            }
            //
            Rect rectLabelFrameLimit = new Rect(0f, rectLabelObjectMessage.y + rectLabelObjectMessage.height + height_inspector_space, 150f, 20f);
            GUI.Label(rectLabelFrameLimit, "Frame Limit");
            Rect rectToggleFrameLimit = new Rect(rectLabelFrameLimit.x + rectLabelFrameLimit.width + margin, rectLabelFrameLimit.y, 20f, 20f);
            if(eKey.setFrameLimit(GUI.Toggle(rectToggleFrameLimit, eKey.frameLimit, ""))) {
                sTrack.updateCache();
                AMCodeView.refresh();
                // save data
                setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
            }
            //
            GUI.enabled = sendMessageToggleEnabled;
            Rect rectLabelSendMessage = new Rect(0f, rectLabelFrameLimit.y + rectLabelFrameLimit.height + height_inspector_space, 150f, 20f);
            GUI.Label(rectLabelSendMessage, "Use SendMessage");
            Rect rectToggleSendMessage = new Rect(rectLabelSendMessage.x + rectLabelSendMessage.width + margin, rectLabelSendMessage.y, 20f, 20f);
            if(eKey.setUseSendMessage(GUI.Toggle(rectToggleSendMessage, eKey.useSendMessage, ""))) {
                sTrack.updateCache();
                AMCodeView.refresh();
                // save data
                setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
            }
            GUI.enabled = !isPlaying;
            Rect rectButtonSendMessageInfo = new Rect(width_inspector - 20f - margin, rectLabelSendMessage.y, 20f, 20f);
            if(GUI.Button(rectButtonSendMessageInfo, "?")) {
                EditorUtility.DisplayDialog("SendMessage vs. Invoke", "SendMessage can only be used with methods that have no more than one parameter (which can be an array).\n\nAnimator will use Invoke when SendMessage is disabled, which is slightly faster but requires caching when the take is played. Use SendMessage if caching is a problem.", "Okay");
            }
            if(cachedParameterInfos.Length > 0) {
                // show method parameters
                float scrollview_y = rectLabelSendMessage.y + rectLabelSendMessage.height + height_inspector_space;
                Rect rectScrollView = new Rect(0f, scrollview_y, width_inspector - margin, rect.height - scrollview_y);
                float width_view = width_inspector - margin - (height_event_parameters > rectScrollView.height ? 20f + margin : 0f);
                Rect rectView = new Rect(0f, 0f, width_view, height_event_parameters);
                inspectorScrollView = GUI.BeginScrollView(rectScrollView, inspectorScrollView, rectView);
                Rect rectField = new Rect(0f, 0f, width_view, 20f);
                float height_all_fields = 0f;
                // there are parameters
                for(int i = 0; i < cachedParameterInfos.Length; i++) {
                    rectField.y += height_inspector_space;
                    if(i > 0) height_all_fields += height_inspector_space;
                    // show field for each parameter
                    float height_field = 0f;
                    if(showFieldFor(rectField, i.ToString(), cachedParameterInfos[i].Name, eKey.parameters[i], cachedParameterInfos[i].ParameterType, 0, ref height_field)) {
                        sTrack.updateCache();
                        AMCodeView.refresh();
                        // save data
                        setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));
                    }
                    rectField.y += height_field;
                    height_all_fields += height_field;
                }
                GUI.EndScrollView();
                height_all_fields += height_inspector_space;
                if(height_event_parameters != height_all_fields) height_event_parameters = height_all_fields;
            }
            return;
        }
        #endregion
    }

    bool showFieldFor(Rect rect, string id, string name, AMEventParameter parameter, Type t, int level, ref float height_field) {
        rect.x = 5f * level;
        name = typeStringBrief(t) + " " + name;
        bool saveChanges = false;
        if(t.IsArray) {
            if(t.GetElementType().IsArray) {
                GUI.skin.label.wordWrap = true;
                rect.height = 40f;
                height_field += rect.height;
                GUI.Label(rect, "Multi-dimensional arrays are currently unsupported.");
                return false;
            }
            if(!arrayFieldFoldout.ContainsKey(id)) arrayFieldFoldout.Add(id, true);
            Rect rectArrayFoldout = new Rect(rect.x, rect.y + 3f, 15f, 15f);
            if(GUI.Button(rectArrayFoldout, "", "label")) arrayFieldFoldout[id] = !arrayFieldFoldout[id];
            GUI.DrawTexture(rectArrayFoldout, (arrayFieldFoldout[id] ? GUI.skin.GetStyle("GroupElementFoldout").normal.background : GUI.skin.GetStyle("GroupElementFoldout").active.background));
            Rect rectLabelArrayName = new Rect(rectArrayFoldout.x + rectArrayFoldout.width + margin, rect.y, rect.width - rectArrayFoldout.width - margin, rect.height);
            GUI.Label(rectLabelArrayName, name);
            height_field += rectLabelArrayName.height;
            if(arrayFieldFoldout[id]) {
                // show elements if folded out
                if(parameter.lsArray.Count <= 0) {
                    AMEventParameter a = new AMEventParameter();
                    a.setValueType(t.GetElementType());
                    parameter.lsArray.Add(a);
                    saveChanges = true;
                }
                Rect rectElement = new Rect(rect);
                rectElement.y += rect.height + margin;
                for(int i = 0; i < parameter.lsArray.Count; i++) {
                    float prev_height = height_field;
                    if((showFieldFor(rectElement, id + "_" + i, "(" + i.ToString() + ")", parameter.lsArray[i], t.GetElementType(), (level + 1), ref height_field)) && !saveChanges) saveChanges = true;
                    rectElement.y += height_field - prev_height;
                }
                // add to array button
                Rect rectLabelElement = new Rect(rect.x, rectElement.y, rect.width - 40f - margin * 2f, 25f);
                height_field += rectLabelElement.height;
                GUIStyle styleLabelRight = new GUIStyle(GUI.skin.label);
                styleLabelRight.alignment = TextAnchor.MiddleRight;
                GUI.Label(rectLabelElement, typeStringBrief(t.GetElementType()), styleLabelRight);
                if(parameter.lsArray.Count <= 1) GUI.enabled = false;
                Rect rectButtonRemoveElement = new Rect(rect.x + rect.width - 40f, rectLabelElement.y, 20f, 20f);
                if(GUI.Button(rectButtonRemoveElement, "-")) {
                    parameter.lsArray[parameter.lsArray.Count - 1].destroy();
                    parameter.lsArray.RemoveAt(parameter.lsArray.Count - 1);
                    saveChanges = true;
                }
                Rect rectButtonAddElement = new Rect(rectButtonRemoveElement);
                rectButtonAddElement.x += rectButtonRemoveElement.width + margin;
                GUI.enabled = !isPlaying;
                if(GUI.Button(rectButtonAddElement, "+")) {
                    AMEventParameter a = new AMEventParameter();
                    a.setValueType(t.GetElementType());
                    parameter.lsArray.Add(a);
                    saveChanges = true;
                }
            }
        }
        else if(t == typeof(bool)) {
            // int field
            height_field += 20f;
            if(parameter.setBool(EditorGUI.Toggle(rect, name, parameter.val_bool))) saveChanges = true;
        }
        else if((t == typeof(int)) || (t == typeof(long))) {
            // int field
            height_field += 20f;
            if(parameter.setInt(EditorGUI.IntField(rect, name, (int)parameter.val_int))) saveChanges = true;
        }
        else if((t == typeof(float)) || (t == typeof(double))) {
            // float field
            height_field += 20f;
            if(parameter.setFloat(EditorGUI.FloatField(rect, name, (float)parameter.val_float))) saveChanges = true;
        }
        else if(t == typeof(Vector2)) {
            // vector2 field
            height_field += 40f;
            if(parameter.setVector2(EditorGUI.Vector2Field(rect, name, (Vector2)parameter.val_vect2))) saveChanges = true;
        }
        else if(t == typeof(Vector3)) {
            // vector3 field
            height_field += 40f;
            if(parameter.setVector3(EditorGUI.Vector3Field(rect, name, (Vector3)parameter.val_vect3))) saveChanges = true;
        }
        else if(t == typeof(Vector4)) {
            // vector4 field
            height_field += 40f;
            if(parameter.setVector4(EditorGUI.Vector4Field(rect, name, (Vector4)parameter.val_vect4))) saveChanges = true;
        }
        else if(t == typeof(Color)) {
            // color field
            height_field += 40f;
            if(parameter.setColor(EditorGUI.ColorField(rect, name, (Color)parameter.val_color))) saveChanges = true;
        }
        else if(t == typeof(Rect)) {
            // rect field
            height_field += 60f;
            if(parameter.setRect(EditorGUI.RectField(rect, name, (Rect)parameter.val_rect))) saveChanges = true;
        }
        else if(t == typeof(string)) {
            height_field += 20f;
            // set default
            if(parameter.val_string == null) parameter.val_string = "";
            // string field
            if(parameter.setString(EditorGUI.TextField(rect, name, (string)parameter.val_string))) saveChanges = true;
        }
        else if(t == typeof(char)) {
            height_field += 20f;
            // set default
            if(parameter.val_string == null) parameter.val_string = "";
            // char (string) field
            Rect rectLabelCharField = new Rect(rect.x, rect.y, 146f, rect.height);
            GUI.Label(rectLabelCharField, name);
            Rect rectTextFieldChar = new Rect(rectLabelCharField.x + rectLabelCharField.width + margin, rectLabelCharField.y, rect.width - rectLabelCharField.width - margin, rect.height);
            if(parameter.setString(GUI.TextField(rectTextFieldChar, parameter.val_string, 1))) saveChanges = true;
        }
        else if(t == typeof(GameObject)) {
            height_field += 40f + margin;
            // label
            Rect rectLabelField = new Rect(rect);
            GUI.Label(rectLabelField, name);
            // GameObject field
            GUI.skin = null;
            EditorGUIUtility.LookLikeControls();
            Rect rectObjectField = new Rect(rect.x, rectLabelField.y + rectLabelField.height + margin, rect.width, 16f);
            if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(GameObject), true))) saveChanges = true;
            GUI.skin = skin;
            EditorGUIUtility.LookLikeControls();
        }
        else if(t.BaseType == typeof(Behaviour) || t.BaseType == typeof(Component)) {
            height_field += 40f + margin;
            // label
            Rect rectLabelField = new Rect(rect);
            GUI.Label(rectLabelField, name);
            GUI.skin = null;
            Rect rectObjectField = new Rect(rect.x, rectLabelField.y + rectLabelField.height + margin, rect.width, 16f);
            EditorGUIUtility.LookLikeControls();
            // field
            if(t == typeof(Transform)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(Transform), true))) saveChanges = true; }
            else if(t == typeof(MeshFilter)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(MeshFilter), true))) saveChanges = true; }
            else if(t == typeof(TextMesh)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(TextMesh), true))) saveChanges = true; }
            else if(t == typeof(MeshRenderer)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(MeshRenderer), true))) saveChanges = true; }
            //else if(t == typeof(ParticleSystem)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField,parameter.val_obj,typeof(ParticleSystem),true))) saveChanges = true; }
            else if(t == typeof(TrailRenderer)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(TrailRenderer), true))) saveChanges = true; }
            else if(t == typeof(LineRenderer)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(LineRenderer), true))) saveChanges = true; }
            else if(t == typeof(LensFlare)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(LensFlare), true))) saveChanges = true; }
            // halo
            else if(t == typeof(Projector)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(Projector), true))) saveChanges = true; }
            else if(t == typeof(Rigidbody)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(Rigidbody), true))) saveChanges = true; }
            else if(t == typeof(CharacterController)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(CharacterController), true))) saveChanges = true; }
            else if(t == typeof(BoxCollider)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(BoxCollider), true))) saveChanges = true; }
            else if(t == typeof(SphereCollider)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(SphereCollider), true))) saveChanges = true; }
            else if(t == typeof(CapsuleCollider)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(CapsuleCollider), true))) saveChanges = true; }
            else if(t == typeof(MeshCollider)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(MeshCollider), true))) saveChanges = true; }
            else if(t == typeof(WheelCollider)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(WheelCollider), true))) saveChanges = true; }
            else if(t == typeof(TerrainCollider)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(TerrainCollider), true))) saveChanges = true; }
            else if(t == typeof(InteractiveCloth)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(InteractiveCloth), true))) saveChanges = true; }
            else if(t == typeof(SkinnedCloth)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(SkinnedCloth), true))) saveChanges = true; }
            else if(t == typeof(ClothRenderer)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(ClothRenderer), true))) saveChanges = true; }
            else if(t == typeof(HingeJoint)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(HingeJoint), true))) saveChanges = true; }
            else if(t == typeof(FixedJoint)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(FixedJoint), true))) saveChanges = true; }
            else if(t == typeof(SpringJoint)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(SpringJoint), true))) saveChanges = true; }
            else if(t == typeof(CharacterJoint)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(CharacterJoint), true))) saveChanges = true; }
            else if(t == typeof(ConfigurableJoint)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(ConfigurableJoint), true))) saveChanges = true; }
            else if(t == typeof(ConstantForce)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(ConstantForce), true))) saveChanges = true; }
            //else if(t == typeof(NavMeshAgent)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField,parameter.val_obj,typeof(NavMeshAgent),true))) saveChanges = true; }
            //else if(t == typeof(OffMeshLink)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField,parameter.val_obj,typeof(OffMeshLink),true))) saveChanges = true; }
            else if(t == typeof(AudioListener)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(AudioListener), true))) saveChanges = true; }
            else if(t == typeof(AudioSource)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(AudioSource), true))) saveChanges = true; }
            else if(t == typeof(AudioReverbZone)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(AudioReverbZone), true))) saveChanges = true; }
            else if(t == typeof(AudioLowPassFilter)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(AudioLowPassFilter), true))) saveChanges = true; }
            else if(t == typeof(AudioHighPassFilter)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(AudioHighPassFilter), true))) saveChanges = true; }
            else if(t == typeof(AudioEchoFilter)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(AudioEchoFilter), true))) saveChanges = true; }
            else if(t == typeof(AudioDistortionFilter)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(AudioDistortionFilter), true))) saveChanges = true; }
            else if(t == typeof(AudioReverbFilter)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(AudioReverbFilter), true))) saveChanges = true; }
            else if(t == typeof(AudioChorusFilter)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(AudioChorusFilter), true))) saveChanges = true; }
            else if(t == typeof(Camera)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(Camera), true))) saveChanges = true; }
            else if(t == typeof(Skybox)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(Skybox), true))) saveChanges = true; }
            // flare layer
            else if(t == typeof(GUILayer)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(GUILayer), true))) saveChanges = true; }
            else if(t == typeof(Light)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(Light), true))) saveChanges = true; }
            //else if(t == typeof(LightProbeGroup)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField,parameter.val_obj,typeof(LightProbeGroup),true))) saveChanges = true; }
            else if(t == typeof(OcclusionArea)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(OcclusionArea), true))) saveChanges = true; }
            //else if(t == typeof(OcclusionPortal)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField,parameter.val_obj,typeof(OcclusionPortal),true))) saveChanges = true; }
            //else if(t == typeof(LODGroup)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField,parameter.val_obj,typeof(LODGroup),true))) saveChanges = true; }
            else if(t == typeof(GUITexture)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(GUITexture), true))) saveChanges = true; }
            else if(t == typeof(GUIText)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(GUIText), true))) saveChanges = true; }
            else if(t == typeof(Animation)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(Animation), true))) saveChanges = true; }
            else if(t == typeof(NetworkView)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(NetworkView), true))) saveChanges = true; }
            // wind zone
            else {

                if(t.BaseType == typeof(Behaviour)) { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(Behaviour), true))) saveChanges = true; }
                else { if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(Component), true))) saveChanges = true; }

            }
            GUI.skin = skin;
            EditorGUIUtility.LookLikeControls();
            //return;
        }
        else if(t == typeof(UnityEngine.Object)) {
            height_field += 40f + margin;
            Rect rectLabelField = new Rect(rect);
            GUI.Label(rectLabelField, name);
            Rect rectObjectField = new Rect(rect.x, rectLabelField.y + rectLabelField.height + margin, rect.width, 16f);
            GUI.skin = null;
            EditorGUIUtility.LookLikeControls();
            if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(UnityEngine.Object), true))) saveChanges = true;
            GUI.skin = skin;
            EditorGUIUtility.LookLikeControls();
        }
        else if(t == typeof(Component)) {
            Rect rectLabelField = new Rect(rect);
            GUI.Label(rectLabelField, name);
            Rect rectObjectField = new Rect(rect.x, rectLabelField.y + rectLabelField.height + margin, rect.width, 16f);
            GUI.skin = null;
            GUI.skin = null;
            EditorGUIUtility.LookLikeControls();
            if(parameter.setObject(EditorGUI.ObjectField(rectObjectField, parameter.val_obj, typeof(Component), true))) saveChanges = true;
            GUI.skin = skin;
            EditorGUIUtility.LookLikeControls();
        }
        else {
            height_field += 20f;
            GUI.skin.label.wordWrap = true;
            GUI.Label(rect, "Unsupported parameter type " + t.ToString() + ".");
        }

        return saveChanges;

    }
    void showObjectFieldFor(AMTrack amTrack, float width_track, Rect rect) {
        if(rect.width < 22f) return;
        // show object field for track, used in OnGUI. Needs to be updated for every track type.
        GUI.skin = null;
        EditorGUIUtility.LookLikeControls();
        // add objectfield for every track type
        // translation
        if(amTrack is AMTranslationTrack) {
            (amTrack as AMTranslationTrack).obj = (Transform)EditorGUI.ObjectField(rect, amTrack.genericObj, typeof(Transform), true/*,GUILayout.Width (width_track-padding_track*2)*/);
        }
        // rotation
        else if(amTrack is AMRotationTrack) {
            (amTrack as AMRotationTrack).obj = (Transform)EditorGUI.ObjectField(rect, amTrack.genericObj, typeof(Transform), true);
        }
        // rotation
        else if(amTrack is AMOrientationTrack) {
            if((amTrack as AMOrientationTrack).setObject((Transform)EditorGUI.ObjectField(rect, (amTrack as AMOrientationTrack).obj, typeof(Transform), true))) {
                amTrack.updateCache();
                AMCodeView.refresh();
            }
        }
        // animation
        else if(amTrack is AMAnimationTrack) {
            //GameObject _old = (amTrack as AMAnimationTrack).obj;
            if((amTrack as AMAnimationTrack).setObject((GameObject)EditorGUI.ObjectField(rect, (amTrack as AMAnimationTrack).obj, typeof(GameObject), true))) {
                //Animation _temp = (Animation)(amTrack as AMAnimationTrack).obj.GetComponent("Animation");
                if((amTrack as AMAnimationTrack).obj != null) {
                    if((amTrack as AMAnimationTrack).obj.animation == null) {
                        (amTrack as AMAnimationTrack).obj = null;
                        EditorUtility.DisplayDialog("No Animation Component", "You must add an Animation component to the GameObject before you can use it in an Animation Track.", "Okay");
                    }
                }
            }
        }
        // audio
        else if(amTrack is AMAudioTrack) {
            if((amTrack as AMAudioTrack).setAudioSource((AudioSource)EditorGUI.ObjectField(rect, (amTrack as AMAudioTrack).audioSource, typeof(AudioSource), true))) {
                if((amTrack as AMAudioTrack).audioSource != null) {
                    (amTrack as AMAudioTrack).audioSource.playOnAwake = false;
                }
            }
        }
        // property
        else if(amTrack is AMPropertyTrack) {
            GameObject propertyGameObject = (GameObject)EditorGUI.ObjectField(rect, (amTrack as AMPropertyTrack).obj, typeof(GameObject), true);
            if((amTrack as AMPropertyTrack).isObjectUnique(propertyGameObject)) {
                bool changePropertyGameObject = true;
                if((amTrack.keys.Count > 0) && (!EditorUtility.DisplayDialog("Data Will Be Lost", "You will lose all of the keyframes on track '" + amTrack.name + "' if you continue.", "Continue Anway", "Cancel"))) {
                    changePropertyGameObject = false;
                }
                if(changePropertyGameObject) {
					Undo.RecordObject(amTrack, "Set GameObject");

                    // delete all keys
                    if(amTrack.keys.Count > 0) {
						foreach(AMKey key in amTrack.keys)
							Undo.DestroyObjectImmediate(key);

						amTrack.keys = new List<AMKey>();
                        amTrack.updateCache();
                        AMCodeView.refresh();
                    }
                    (amTrack as AMPropertyTrack).setObject(propertyGameObject);

					EditorUtility.SetDirty(amTrack);
                }
            }
        }
        // event
        else if(amTrack is AMEventTrack) {
            GameObject eventGameObject = (GameObject)EditorGUI.ObjectField(rect, (amTrack as AMEventTrack).obj, typeof(GameObject), true);
            if((amTrack as AMEventTrack).isObjectUnique(eventGameObject)) {
                bool changeEventGameObject = true;
                if((amTrack.keys.Count > 0) && (!EditorUtility.DisplayDialog("Data Will Be Lost", "You will lose all of the keyframes on track '" + amTrack.name + "' if you continue.", "Continue Anway", "Cancel"))) {
                    changeEventGameObject = false;
                }

                if(changeEventGameObject) {
					Undo.RecordObject(amTrack, "Set GameObject");

                    // delete all keys
                    if(amTrack.keys.Count > 0) {
						foreach(AMKey key in amTrack.keys)
							Undo.DestroyObjectImmediate(key);
						
						amTrack.keys = new List<AMKey>();
						amTrack.updateCache();
                        AMCodeView.refresh();
                    }
                    (amTrack as AMEventTrack).setObject(eventGameObject);

					EditorUtility.SetDirty(amTrack);
                }
            }
        }
        // set go active
        else if(amTrack is AMGOSetActiveTrack) {
            AMGOSetActiveTrack goActiveTrack = amTrack as AMGOSetActiveTrack;
            goActiveTrack.setObject((GameObject)EditorGUI.ObjectField(rect, goActiveTrack.genericObj, typeof(GameObject), true/*,GUILayout.Width (width_track-padding_track*2)*/));
        }

        GUI.skin = skin;
        EditorGUIUtility.LookLikeControls();
    }
    void showAlertMissingObjectType(string type) {
        EditorUtility.DisplayDialog("Missing " + type, "You must add a " + type + " to the track before you can add keys.", "Okay");
    }

    public static bool showEasePicker(AMTrack track, AMKey key, AnimatorData aData, float x = -1f, float y = -1f, float width = -1f) {
        bool didUpdate = false;
        if(x >= 0f && y >= 0f && width >= 0f) {
            width--;
            float height = 22f;
            Rect rectLabel = new Rect(x, y - 1f, 40f, height);
            GUI.Label(rectLabel, "Ease");
            Rect rectPopup = new Rect(rectLabel.x + rectLabel.width + 2f, y + 3f, width - rectLabel.width - width_button_delete - 3f, height);
            if(key.setEaseType(EditorGUI.Popup(rectPopup, key.easeType, easeTypeNames))) {
                // update cache when modifying varaibles
                track.updateCache();
                AMCodeView.refresh();
                // preview new position
                aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
                // save data
                EditorUtility.SetDirty(aData);
                // refresh component
                didUpdate = true;
                // refresh values
                AMEasePicker.refreshValues();
            }
            Rect rectButton = new Rect(width - width_button_delete + 1f, y, width_button_delete, width_button_delete);
            if(GUI.Button(rectButton, getSkinTextureStyleState("popup").background, GUI.skin.GetStyle("ButtonImage"))) {
                AMEasePicker.setValues(/*aData,*/key, track);
                EditorWindow.GetWindow(typeof(AMEasePicker));
            }

            //display specific variable for certain tweens
            //TODO: only show this for specific tweens
            if(!key.hasCustomEase()) {
                y += rectButton.height + 4;
                Rect rectAmp = new Rect(x, y, 200f, height);
                key.amplitude = EditorGUI.FloatField(rectAmp, "Amplitude", key.amplitude);

                y += rectAmp.height + 4;
                Rect rectPer = new Rect(x, y, 200f, height);
                key.period = EditorGUI.FloatField(rectPer, "Period", key.period);
            }
        }
        else {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Space(1f);
            GUILayout.Label("Ease");
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Space(3f);
            if(key.setEaseType(EditorGUILayout.Popup(key.easeType, easeTypeNames))) {
                // update cache when modifying varaibles
                track.updateCache();
                AMCodeView.refresh();
                // preview new position
                aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
                // save data
                EditorUtility.SetDirty(aData);
                // refresh component
                didUpdate = true;
                // refresh values
                AMEasePicker.refreshValues();
            }
            GUILayout.EndVertical();
            if(GUILayout.Button(getSkinTextureStyleState("popup").background, GUI.skin.GetStyle("ButtonImage"), GUILayout.Width(width_button_delete), GUILayout.Height(width_button_delete))) {
                AMEasePicker.setValues(/*aData,*/key, track);
                EditorWindow.GetWindow(typeof(AMEasePicker));
            }
            GUILayout.Space(1f);
            GUILayout.EndHorizontal();

            //display specific variable for certain tweens
            //TODO: only show this for specific tweens
            if(!key.hasCustomEase()) {
                key.amplitude = EditorGUILayout.FloatField("Amplitude", key.amplitude);

                key.period = EditorGUILayout.FloatField("Period", key.period);
            }
        }
        return didUpdate;
    }

    void drawIndicator(int frame) {
        // draw the indicator texture on the timeline
        int _startFrame = (int)aData.getCurrentTake().startFrame;
        // abort if frame not rendered
        if(frame < _startFrame) return;
        if(frame > (_startFrame + numFramesToRender - 1)) return;
        // offset frame based on render start frame
        frame -= _startFrame;
        // draw textures
        GUI.DrawTexture(new Rect(width_track + (frame) * current_width_frame + (current_width_frame / 2) - width_indicator_head / 2 - 1f, height_indicator_offset_y, width_indicator_head, height_indicator_head), texIndHead);
        GUI.DrawTexture(new Rect(width_track + (frame) * current_width_frame + (current_width_frame / 2) - width_indicator_line / 2 - 1f, height_indicator_offset_y + height_indicator_head, width_indicator_line, position.height - (height_indicator_offset_y + height_indicator_head) - height_indicator_footer + 2f), texIndLine);
    }

    #endregion

    #region Process/Calculate

    void processDragLogic() {
        #region hand tool acceleration
        if(justFinishedHandDragTicker > 0) {
            justFinishedHandDragTicker--;
            if(justFinishedHandDragTicker <= 0) {
                handDragAccelaration = (int)((endHandMousePosition.x - currentMousePosition.x) * 1.5f);
            }
        }
        #endregion
        bool justStartedDrag = false;
        bool justFinishedDrag = false;
        if(isDragging != cachedIsDragging) {
            if(isDragging) justStartedDrag = true;
            else justFinishedDrag = true;
            cachedIsDragging = isDragging;
        }
        #region just started drag
        // set start and end drag frames
        if(justStartedDrag) {
            if(isRenamingTrack != -1 || isRenamingTake || isRenamingGroup != 0) return;
            #region track / group
            if(mouseOverElement == (int)ElementType.Group || mouseOverElement == (int)ElementType.Track) {
                draggingGroupElement = mouseOverGroupElement;
                draggingGroupElementType = mouseOverElement;
                dragType = (int)DragType.GroupElement;
                if(mouseOverElement == (int)ElementType.Group) timelineSelectGroup((int)mouseOverGroupElement.x, false);
                else timelineSelectTrack((int)mouseOverGroupElement.y, false);
            }
            #endregion
            #region frame
            // if dragged from frame
            else if(mouseOverFrame != 0) {
                // change track if necessary
                if(!aData.getCurrentTake().contextSelectionTracks.Contains(mouseOverTrack) && aData.getCurrentTake().selectedTrack != mouseOverTrack) timelineSelectTrack(mouseOverTrack);

                // if dragged from selected frame, move
                if(mouseOverSelectedFrame) {
                    dragType = (int)DragType.MoveSelection;
                    aData.getCurrentTake().setGhostSelection();
                }
                else {
                    // else, start context selection
                    dragType = (int)DragType.ContextSelection;
                }
                startDragFrame = mouseOverFrame;
                ghostStartDragFrame = startDragFrame;
                endDragFrame = mouseOverFrame;
            #endregion
                #region time scrub
                // if dragged from time scrub
            }
            else if(mouseOverElement == (int)ElementType.TimeScrub) {
                // set start scrub mouse position
                startScrubMousePosition = currentMousePosition;
                dragType = (int)DragType.TimeScrub;
                #endregion
                #region frame scrub
            }
            else if(mouseOverElement == (int)ElementType.FrameScrub) {
                // set start scrub mouse position
                startScrubMousePosition = currentMousePosition;
                dragType = (int)DragType.FrameScrub;
                #endregion
                #region resize track
            }
            else if(mouseOverElement == (int)ElementType.ResizeTrack) {
                startScrubMousePosition = currentMousePosition;
                startResize_width_track = aData.width_track;
                dragType = (int)DragType.ResizeTrack;
                #endregion
                #region timeline scrub
            }
            else if(mouseOverElement == (int)ElementType.TimelineScrub) {
                dragType = (int)DragType.TimelineScrub;
                #endregion
                #region resize action
            }
            else if(mouseOverElement == (int)ElementType.ResizeAction) {
                if(aData.getCurrentTake().selectedTrack != mouseOverTrack) timelineSelectTrack(mouseOverTrack);
                dragType = (int)DragType.ResizeAction;
                #endregion
                #region resize horizontal scrollbar left
            }
            else if(mouseOverElement == (int)ElementType.ResizeHScrollbarLeft) {
                dragType = (int)DragType.ResizeHScrollbarLeft;
                #endregion
                #region resize horizontal scrollbar right
            }
            else if(mouseOverElement == (int)ElementType.ResizeHScrollbarRight) {
                dragType = (int)DragType.ResizeHScrollbarRight;
                #endregion
                #region cursor zoom
            }
            else if(mouseOverElement == (int)ElementType.CursorZoom) {
                startZoomMousePosition = currentMousePosition;
                zoomDirectionMousePosition = currentMousePosition;
                startZoomValue = aData.zoom;
                dragType = (int)DragType.CursorZoom;
                didPeakZoom = false;
                #endregion
                #region cursor hand
            }
            else if(mouseOverElement == (int)ElementType.CursorHand) {
                //startScrubFrame = mouseXOverFrame;
                justStartedHandGrab = true;
                dragType = (int)DragType.CursorHand;
                #endregion
            }
            else {
                // if did not drag from a draggable element
                dragType = (int)DragType.None;
            }
            // reset drag
            justStartedDrag = false;
        #endregion
            #region just finished drag
            // if finished drag
        }
        else if(justFinishedDrag) {
            // if finished drag onto frame x, update end drag frame
            if(mouseXOverFrame != 0) {
                endDragFrame = mouseXOverFrame;
            }
            // if finished move selection
            if(dragType == (int)DragType.MoveSelection) {
				AMTake take = aData.getCurrentTake();
				Undo.RegisterCompleteObjectUndo(getKeysAndTracks(take), "Move Keys");

				AMKey[] dKeys = take.offsetContextSelectionFramesBy(endDragFrame - startDragFrame);

				foreach(AMKey dkey in dKeys)
					Undo.DestroyObjectImmediate(dkey);

				dKeys = checkForOutOfBoundsFramesOnSelectedTrack();

				foreach(AMKey dkey in dKeys)
					Undo.DestroyObjectImmediate(dkey);

                // preview selected frame
				take.previewFrame(take.selectedFrame);
                AMCodeView.refresh();
                // if finished context selection

				setDirtyTracks(take);
				foreach(AMTrack track in take.trackValues)
					setDirtyKeys(track);

				take.ghostSelection.Clear();
            }
            else if(dragType == (int)DragType.ContextSelection) {
                contextSelectFrameRange(startDragFrame, endDragFrame);
                // if finished timeline scrub
            }
            else if(dragType == (int)DragType.TimeScrub || dragType == (int)DragType.FrameScrub) {
                // do nothing
                // if finished dragging group element
            }
            else if(dragType == (int)DragType.GroupElement) {
				AMTake take = aData.getCurrentTake();
				Undo.RecordObject(take, "Move Element");
                processDropGroupElement(draggingGroupElementType, draggingGroupElement, mouseOverElement, mouseOverGroupElement);
				EditorUtility.SetDirty(take);
            }
            else if(dragType == (int)DragType.ResizeAction) {
				Undo.RegisterCompleteObjectUndo(aData.getCurrentTake().getSelectedTrack(), "Resize Action");
                AMKey[] dKeys = aData.getCurrentTake().getSelectedTrack().removeDuplicateKeys();
				foreach(AMKey dkey in dKeys)
					Undo.DestroyObjectImmediate(dkey);
                // preview selected frame
                aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
            }
            else if(dragType == (int)DragType.CursorZoom) {
                tex_cursor_zoom = null;
            }
            else if(dragType == (int)DragType.CursorHand) {
                endHandMousePosition = currentMousePosition;
                justFinishedHandDragTicker = 1;
                //startScrubFrame = 0;	
            }
            dragType = (int)DragType.None;
            // reset drag
            justFinishedDrag = false;

            #endregion
            #region is dragging
            // if is dragging
        }
        else if(isDragging) {
            #region move selection
            // if moving selection, offset selection
            if(dragType == (int)DragType.MoveSelection) {
                if(mouseXOverFrame != endDragFrame) {
                    endDragFrame = mouseXOverFrame;
                    aData.getCurrentTake().offsetGhostSelectionBy(endDragFrame - ghostStartDragFrame);
                    ghostStartDragFrame = endDragFrame;
                }
            #endregion
                #region group element
            }
            else if(dragType == (int)DragType.GroupElement) {
                scrollViewValue.y += scrollAmountVertical / 6f;
                #endregion
                #region context selection
            }
            else if(dragType == (int)DragType.ContextSelection) {
                if(mouseXOverFrame != 0) {
                    endDragFrame = mouseXOverFrame;
                }
                contextSelectFrameRange(startDragFrame, endDragFrame);
                #endregion
                #region time scrub / frame scrub
                // if is dragging time or frame scrub, set scrub speed
            }
            else if(dragType == (int)DragType.TimeScrub || dragType == (int)DragType.FrameScrub) {
                setScrubSpeed();
                #endregion
                #region timeline scrub
            }
            else if(dragType == (int)DragType.TimelineScrub) {
                int frame = mouseXOverFrame;
                if(frame < (int)aData.getCurrentTake().startFrame) frame = (int)aData.getCurrentTake().startFrame;
                else if(frame > (int)aData.getCurrentTake().endFrame) frame = (int)aData.getCurrentTake().endFrame;
                selectFrame(frame);
                #endregion
                #region resize action
                // resize action
            }
            else if(dragType == (int)DragType.ResizeAction && mouseXOverFrame > 0) {
                AMTrack selTrack = aData.getCurrentTake().getSelectedTrack();
                if(selTrack.hasKeyOnFrame(resizeActionFrame)) {
                    AMKey selKey = selTrack.getKeyOnFrame(resizeActionFrame);
                    if((startResizeActionFrame == -1 || mouseXOverFrame > startResizeActionFrame) && (endResizeActionFrame == -1 || mouseXOverFrame < endResizeActionFrame)) {
                        if(selKey.frame != mouseXOverFrame) {

                            if(arrKeysLeft.Length > 0 && (mouseXOverFrame - startResizeActionFrame - 1) < arrKeysLeft.Length) {
                                // do nothing
                            }
                            else if(arrKeysRight.Length > 0 && (endResizeActionFrame - mouseXOverFrame - 1) < arrKeysRight.Length) {
                                // do nothing
                            }
                            else {
                                selKey.frame = mouseXOverFrame;
                                resizeActionFrame = mouseXOverFrame;
                                if(arrKeysLeft.Length > 0 && (mouseXOverFrame - startResizeActionFrame - 1) <= arrKeysLeft.Length) {
                                    for(int i = 0; i < arrKeysLeft.Length; i++) {
                                        arrKeysLeft[i].frame = startResizeActionFrame + i + 1;
                                    }
                                }
                                else if(arrKeysRight.Length > 0 && (endResizeActionFrame - mouseXOverFrame - 1) <= arrKeysRight.Length) {
                                    for(int i = 0; i < arrKeysRight.Length; i++) {
                                        arrKeysRight[i].frame = resizeActionFrame + i + 1;
                                    }
                                }
                                else {
                                    // update left
                                    int lastFrame = startResizeActionFrame;
                                    for(int i = 0; i < arrKeysLeft.Length; i++) {
                                        arrKeysLeft[i].frame = Mathf.FloorToInt((resizeActionFrame - startResizeActionFrame) * arrKeyRatiosLeft[i] + startResizeActionFrame);
                                        if(arrKeysLeft[i].frame <= lastFrame) {
                                            arrKeysLeft[i].frame = lastFrame + 1;

                                        }
                                        if(arrKeysLeft[i].frame >= resizeActionFrame) arrKeysLeft[i].frame = resizeActionFrame - 1;			// after last
                                        lastFrame = arrKeysLeft[i].frame;
                                    }

                                    // update right
                                    lastFrame = resizeActionFrame;
                                    for(int i = 0; i < arrKeysRight.Length; i++) {
                                        arrKeysRight[i].frame = Mathf.FloorToInt((endResizeActionFrame - resizeActionFrame) * arrKeyRatiosRight[i] + resizeActionFrame);
                                        if(arrKeysRight[i].frame <= lastFrame) {
                                            arrKeysRight[i].frame = lastFrame + 1;
                                        }
                                        if(arrKeysRight[i].frame >= endResizeActionFrame) arrKeysRight[i].frame = endResizeActionFrame - 1;	// after last
                                        lastFrame = arrKeysRight[i].frame;
                                    }
                                }
                                // update cache
                                selTrack.updateCache();
                                AMCodeView.refresh();
                            }

                        }
                    }
                }
                #endregion
                #region resize horizontal scrollbar left
            }
            else if(dragType == (int)DragType.ResizeHScrollbarLeft) {
                if(mouseXOverHScrollbarFrame <= 0) aData.getCurrentTake().startFrame = 1;
                else if(mouseXOverHScrollbarFrame > aData.getCurrentTake().numFrames) aData.getCurrentTake().startFrame = aData.getCurrentTake().numFrames;
                else aData.getCurrentTake().startFrame = mouseXOverHScrollbarFrame;
                #endregion
                #region resize horizontal scrollbar right
            }
            else if(dragType == (int)DragType.ResizeHScrollbarRight) {
                if(mouseXOverHScrollbarFrame <= 0) aData.getCurrentTake().endFrame = 1;
                else if(mouseXOverHScrollbarFrame > aData.getCurrentTake().numFrames) aData.getCurrentTake().endFrame = aData.getCurrentTake().numFrames;
                else aData.getCurrentTake().endFrame = mouseXOverHScrollbarFrame;
                int min = Mathf.FloorToInt((position.width - width_track - 18f - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed)) / (height_track - height_action_min));
                if(aData.getCurrentTake().startFrame > aData.getCurrentTake().endFrame - min) aData.getCurrentTake().startFrame = aData.getCurrentTake().endFrame - min;
                #endregion
                #region cursor zoom
            }
            else if(dragType == (int)DragType.CursorZoom) {
                if(didPeakZoom) {
                    if(wasZoomingIn && currentMousePosition.x <= cachedZoomMousePosition.x) {
                        // direction change	
                        startZoomValue = aData.zoom;
                        zoomDirectionMousePosition = currentMousePosition;
                    }
                    else if(!wasZoomingIn && currentMousePosition.x >= cachedZoomMousePosition.x) {
                        // direction change	
                        startZoomValue = aData.zoom;
                        zoomDirectionMousePosition = currentMousePosition;
                    }
                    didPeakZoom = false;
                }
                float zoomValue = startZoomValue + (zoomDirectionMousePosition.x - currentMousePosition.x) / 300f;
                if(zoomValue < 0f) {
                    zoomValue = 0f;
                    cachedZoomMousePosition = currentMousePosition;
                    wasZoomingIn = true;
                    didPeakZoom = true;
                }
                else if(zoomValue > 1f) {
                    zoomValue = 1f;
                    cachedZoomMousePosition = currentMousePosition;
                    wasZoomingIn = false;
                    didPeakZoom = true;
                }
                if(zoomValue < aData.zoom) tex_cursor_zoom = tex_cursor_zoomin;
                else if(zoomValue > aData.zoom) tex_cursor_zoom = tex_cursor_zoomout;
                aData.zoom = zoomValue;
            }
                #endregion
        }
            #endregion
    }
    void processUpdateMethodInfoCache(bool now = false) {
        if(now) updateMethodInfoCacheBuffer = 0;
        // update methodinfo cache if necessary
        if(!aData || !aData.getCurrentTake()) return;
        if(aData.getCurrentTake().getTrackCount() <= 0) return;
        if(aData.getCurrentTake().selectedTrack <= -1) return;
        if(updateMethodInfoCacheBuffer > 0) updateMethodInfoCacheBuffer--;
        if((indexMethodInfo == -1) && (updateMethodInfoCacheBuffer <= 0)) {

            // if track is event
            if(aData.getCurrentTake().selectedTrack > -1 && aData.getCurrentTake().getSelectedTrack() is AMEventTrack) {
                // if event track has key on selected frame
                AMTrack selectedTrack = aData.getCurrentTake().getSelectedTrack();
                if(selectedTrack.hasKeyOnFrame(aData.getCurrentTake().selectedFrame)) {
                    // update methodinfo cache
                    updateCachedMethodInfo((selectedTrack as AMEventTrack).obj);
                    updateMethodInfoCacheBuffer = updateRateMethodInfoCache;
                    // set index to method info index

                    if(cachedMethodInfo.Count > 0) {
                        AMEventKey eKey = (selectedTrack.getKeyOnFrame(aData.getCurrentTake().selectedFrame) as AMEventKey);

                        if(eKey.methodInfo != null) {
                            for(int i = 0; i < cachedMethodInfo.Count; i++) {
                                if(cachedMethodInfo[i] == eKey.methodInfo) {
                                    indexMethodInfo = i;
                                    return;
                                }
                            }
                        }
                        indexMethodInfo = 0;
                    }
                }
            }
        }
    }
    void processHandDragAcceleration() {
        float speed = (int)((Mathf.Clamp(Mathf.Abs(handDragAccelaration), 0, 200) / 12) * (aData.zoom + 0.2f));
        if(handDragAccelaration > 0) {

            if(aData.getCurrentTake().endFrame < aData.getCurrentTake().numFrames) {
                aData.getCurrentTake().startFrame += speed;
                aData.getCurrentTake().endFrame += speed;
                if(ticker % 2 == 0) handDragAccelaration--;
            }
            else {
                handDragAccelaration = 0;
            }
        }
        else if(handDragAccelaration < 0) {
            if(aData.getCurrentTake().startFrame > 1f) {
                aData.getCurrentTake().startFrame -= speed;
                aData.getCurrentTake().endFrame -= speed;
                if(ticker % 2 == 0) handDragAccelaration++;
            }
            else {
                handDragAccelaration = 0;
            }
        }
    }
    void processDropGroupElement(int sourceType, Vector2 sourceElement, int destType, Vector2 destElement) {
        // dropped inside group
        if(destType == (int)ElementType.Group) {
            if(sourceType == (int)ElementType.Track) {
                // drop track on group
                aData.getCurrentTake().moveToGroup((int)sourceElement.y, (int)destElement.x, true);
            }
            else if(sourceType == (int)ElementType.Group) {
                // drop group on group
                if((int)sourceElement.x != (int)destElement.x)
                    aData.getCurrentTake().moveToGroup((int)sourceElement.x, (int)destElement.x, true);
            }
            // dropped outside group
        }
        else if(destType == (int)ElementType.GroupOutside) {
            if(sourceType == (int)ElementType.Track) {
                // drop track on group

                aData.getCurrentTake().moveGroupElement((int)sourceElement.y, (int)destElement.x);
            }
            else if(sourceType == (int)ElementType.Group) {
                // drop group on group
                //int destGroup = aData.getCurrentTake().getElementGroup((int)destElement.x);
                //if((int)sourceElement.x != destGroup)
                //aData.getCurrentTake().moveToGroup((int)sourceElement.x,destGroup);
                if((int)sourceElement.x != (int)destElement.x)
                    aData.getCurrentTake().moveGroupElement((int)sourceElement.x, (int)destElement.x);

            }
            // dropped on track
        }
        else if(destType == (int)ElementType.Track) {
            if(sourceType == (int)ElementType.Track) {
                // drop track on track
                if((int)sourceElement.y != (int)destElement.y)
                    aData.getCurrentTake().moveToGroup((int)sourceElement.y, (int)destElement.x, false, (int)destElement.y);
            }
            else if(sourceType == (int)ElementType.Group) {
                // drop group on track
                if((int)destElement.x == 0 || (int)sourceElement.x != (int)destElement.x)
                    aData.getCurrentTake().moveToGroup((int)sourceElement.x, (int)destElement.x, false, (int)destElement.y);
            }
        }
        else {
            // drop on window, move to root group
            if(sourceType == (int)ElementType.Track) {
                aData.getCurrentTake().moveToGroup((int)sourceElement.y, 0, mouseAboveGroupElements);
            }
            else if(sourceType == (int)ElementType.Group) {
                // move group to last position
                aData.getCurrentTake().moveToGroup((int)sourceElement.x, 0, mouseAboveGroupElements);
            }
        }
        // re-select track to update selected group
        if(sourceType == (int)ElementType.Track) timelineSelectTrack((int)sourceElement.y, false);
        // scroll to the track
        float scrollTo = -1f;
        scrollTo = aData.getCurrentTake().getElementY((sourceType == (int)ElementType.Group ? (int)sourceElement.x : (int)sourceElement.y), height_track, height_track_foldin, height_group);
        setScrollViewValue(scrollTo);
    }
    public static void recalculateNumFramesToRender() {
        if(window) window.cachedZoom = -1f;
    }
    void calculateNumFramesToRender(bool clickedZoom, Event e) {
        if(!aData.getCurrentTake()) return;

        int min = Mathf.FloorToInt((position.width - width_track - 18f - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed)) / (oData.disableTimelineActions ? height_track / 2f : height_track - height_action_min));
        int _mouseXOverFrame = (int)aData.getCurrentTake().startFrame + Mathf.CeilToInt((e.mousePosition.x - width_track) / current_width_frame) - 1;
        // move frames with hand cursor
        if(dragType == (int)DragType.CursorHand && !justStartedHandGrab) {
            if(_mouseXOverFrame != startScrubFrame) {
                float numFrames = aData.getCurrentTake().endFrame - aData.getCurrentTake().startFrame;
                float dist_hand_drag = startScrubFrame - _mouseXOverFrame;
                aData.getCurrentTake().startFrame += dist_hand_drag;
                aData.getCurrentTake().endFrame += dist_hand_drag;
                if(aData.getCurrentTake().startFrame < 1f) {
                    aData.getCurrentTake().startFrame = 1f;
                    aData.getCurrentTake().endFrame += numFrames - (aData.getCurrentTake().endFrame - aData.getCurrentTake().startFrame);
                }
                else if(aData.getCurrentTake().endFrame > aData.getCurrentTake().numFrames) {
                    aData.getCurrentTake().endFrame = aData.getCurrentTake().numFrames;
                    aData.getCurrentTake().startFrame -= numFrames - (aData.getCurrentTake().endFrame - aData.getCurrentTake().startFrame);
                }
            }
            // calculate the number of frames to render based on zoom
        }
        else if(aData.zoom != cachedZoom && dragType != (int)DragType.ResizeHScrollbarLeft && dragType != (int)DragType.ResizeHScrollbarRight) {
            //numFramesToRender
            if(oData.scrubby_zoom_slider) numFramesToRender = Mathf.Lerp(0.0f, 1.0f, aData.zoom) * ((float)aData.getCurrentTake().numFrames - min) + min;
            else numFramesToRender = Expo.EaseIn(aData.zoom, 0.0f, 1.0f, 1.0f, 0.0f, 0.0f) * ((float)aData.getCurrentTake().numFrames - min) + min;
            // frame dimensions
            current_width_frame = Mathf.Clamp((position.width - width_track - 18f - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed)) / numFramesToRender, 0f, (oData.disableTimelineActions ? height_track / 2f : height_track - height_action_min));
            current_height_frame = Mathf.Clamp(current_width_frame * 2f, 20f, (oData.disableTimelineActions ? height_track : 40f));
            float half = 0f;
            // zoom out			
            if(aData.getCurrentTake().endFrame - aData.getCurrentTake().startFrame + 1 < Mathf.FloorToInt(numFramesToRender)) {
                if((oData.scrubby_zoom_cursor && dragType == (int)DragType.CursorZoom) /*|| (aData.scrubby_zoom_slider && dragType != (int)DragType.CursorZoom)*/) {
                    int newPosFrame = (int)aData.getCurrentTake().startFrame + Mathf.CeilToInt((startZoomMousePosition.x - width_track) / current_width_frame) - 1;
                    int _diff = startZoomXOverFrame - newPosFrame;
                    aData.getCurrentTake().startFrame += _diff;
                    aData.getCurrentTake().endFrame += _diff;
                }
                else {

                    half = (((int)numFramesToRender - (aData.getCurrentTake().endFrame - aData.getCurrentTake().startFrame + 1)) / 2f);
                    aData.getCurrentTake().startFrame -= Mathf.FloorToInt(half);
                    aData.getCurrentTake().endFrame += Mathf.CeilToInt(half);
                    // clicked zoom out
                    if(clickedZoom) {
                        int newPosFrame = (int)aData.getCurrentTake().startFrame + Mathf.CeilToInt((e.mousePosition.x - width_track) / current_width_frame) - 1;
                        int _diff = _mouseXOverFrame - newPosFrame;
                        aData.getCurrentTake().startFrame += _diff;
                        aData.getCurrentTake().endFrame += _diff;
                    }
                }
                // zoom in
            }
            else if(aData.getCurrentTake().endFrame - aData.getCurrentTake().startFrame + 1 > Mathf.FloorToInt(numFramesToRender)) {
                //targetPos = ((float)startZoomXOverFrame)/((float)aData.getCurrentTake().endFrame);
                half = (((aData.getCurrentTake().endFrame - aData.getCurrentTake().startFrame + 1) - numFramesToRender) / 2f);
                //float scrubby_startframe = (float)aData.getCurrentTake().startFrame+half;
                aData.getCurrentTake().startFrame += Mathf.FloorToInt(half);
                aData.getCurrentTake().endFrame -= Mathf.CeilToInt(half);
                int targetFrame = 0;
                // clicked zoom in
                if(clickedZoom) {
                    int newPosFrame = (int)aData.getCurrentTake().startFrame + Mathf.CeilToInt((e.mousePosition.x - width_track) / current_width_frame) - 1;
                    int _diff = _mouseXOverFrame - newPosFrame;
                    aData.getCurrentTake().startFrame += _diff;
                    aData.getCurrentTake().endFrame += _diff;
                    // scrubby zoom in
                }
                else if((oData.scrubby_zoom_cursor && dragType == (int)DragType.CursorZoom) || (oData.scrubby_zoom_slider && dragType != (int)DragType.CursorZoom)) {
                    if(dragType != (int)DragType.CursorZoom) {
                        // scrubby zoom slider to indicator
                        targetFrame = aData.getCurrentTake().selectedFrame;
                        float dist_scrubbyzoom = Mathf.Round(targetFrame - Mathf.FloorToInt(aData.getCurrentTake().startFrame + numFramesToRender / 2f));
                        int offset = Mathf.RoundToInt(dist_scrubbyzoom * (1f - Mathf.Lerp(0.0f, 1.0f, aData.zoom)));
                        aData.getCurrentTake().startFrame += offset;
                        aData.getCurrentTake().endFrame += offset;
                    }
                    else {
                        // scrubby zoom cursor to mouse position
                        int newPosFrame = (int)aData.getCurrentTake().startFrame + Mathf.CeilToInt((startZoomMousePosition.x - width_track) / current_width_frame) - 1;
                        int _diff = startZoomXOverFrame - newPosFrame;
                        aData.getCurrentTake().startFrame += _diff;
                        aData.getCurrentTake().endFrame += _diff;
                    }
                }

            }
            // if beyond boundaries, adjust
            int diff = 0;
            if(aData.getCurrentTake().endFrame > aData.getCurrentTake().numFrames) {
                diff = (int)aData.getCurrentTake().endFrame - aData.getCurrentTake().numFrames;
                aData.getCurrentTake().endFrame -= diff;
                aData.getCurrentTake().startFrame += diff;
            }
            else if(aData.getCurrentTake().startFrame < 1) {
                diff = 1 - (int)aData.getCurrentTake().startFrame;
                aData.getCurrentTake().startFrame -= diff;
                aData.getCurrentTake().endFrame += diff;
            }
            if(half * 2 < (int)numFramesToRender) aData.getCurrentTake().endFrame++;
            cachedZoom = aData.zoom;
            return;
        }
        // calculates the number of frames to render based on window width
        if(aData.getCurrentTake().startFrame < 1) aData.getCurrentTake().startFrame = 1;

        if(aData.getCurrentTake().endFrame < aData.getCurrentTake().startFrame + min) aData.getCurrentTake().endFrame = aData.getCurrentTake().startFrame + min;
        if(aData.getCurrentTake().endFrame > aData.getCurrentTake().numFrames) aData.getCurrentTake().endFrame = aData.getCurrentTake().numFrames;
        if(aData.getCurrentTake().startFrame > aData.getCurrentTake().endFrame - min) aData.getCurrentTake().startFrame = aData.getCurrentTake().endFrame - min;
        numFramesToRender = aData.getCurrentTake().endFrame - aData.getCurrentTake().startFrame + 1;
        current_width_frame = Mathf.Clamp((position.width - width_track - 18f - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed)) / numFramesToRender, 0f, (oData.disableTimelineActions ? height_track / 2f : height_track - height_action_min));
        current_height_frame = Mathf.Clamp(current_width_frame * 2f, 20f, (oData.disableTimelineActions ? height_track : 40f));
        if(dragType == (int)DragType.ResizeHScrollbarLeft || dragType == (int)DragType.ResizeHScrollbarRight) {
            if(oData.scrubby_zoom_slider) aData.zoom = Mathf.Lerp(0f, 1f, (numFramesToRender - min) / ((float)aData.getCurrentTake().numFrames - min));
            else aData.zoom = AMUtil.EaseInExpoReversed(0f, 1f, (numFramesToRender - min) / ((float)aData.getCurrentTake().numFrames - min));
            cachedZoom = aData.zoom;
        }
    }

    #endregion

    #region Timeline/Timeline Manipulation

    void timelineSelectTrack(int _track, bool undo = true) {
        // select a track from the timeline
        cancelTextEditting();
        if(aData.getCurrentTake().getTrackCount() <= 0) return;
        if(undo) {
			Undo.RecordObject(aData.getCurrentTake(), "Select Track");
		}
        // select track
        aData.getCurrentTake().selectTrack(_track, isShiftDown, isControlDown);
        // set active object
        timelineSelectObjectFor(aData.getCurrentTake().getTrack(_track));
    }
    void timelineSelectGroup(int group_id, bool undo = true) {
        cancelTextEditting();
        if(undo)
			Undo.RecordObject(aData.getCurrentTake(), "Select Group");

        aData.getCurrentTake().selectGroup(group_id, isShiftDown, isControlDown);
        aData.getCurrentTake().selectedTrack = -1;
    }
    void timelineSelectFrame(int _track, int _frame, bool deselectKeyboardFocus = true) {
        // select a frame from the timeline
        cancelTextEditting();
        indexMethodInfo = -1;	// reset methodinfo index to update caches
        if(aData.getCurrentTake().getTrackCount() <= 0) return;
        // select frame
        aData.getCurrentTake().selectFrame(_track, _frame, numFramesToRender, isShiftDown, isControlDown);
        // preview frame
        aData.getCurrentTake().previewFrame(_frame);
        // set active object
        if(_track > -1) timelineSelectObjectFor(aData.getCurrentTake().getTrack(_track));
        // deselect keyboard focus
        if(deselectKeyboardFocus)
            GUIUtility.keyboardControl = 0;
    }
    void timelineSelectObjectFor(AMTrack track) {
        // translation obj
        if(track.GetType() == typeof(AMTranslationTrack))
            Selection.activeObject = track.genericObj;
        // rotation obj
        else if(track.GetType() == typeof(AMRotationTrack))
            Selection.activeObject = track.genericObj;
        else if(track.GetType() == typeof(AMAnimationTrack))
            Selection.activeObject = (track as AMAnimationTrack).obj;
    }
    void timelineSelectNextKey() {
        // select next key
        if(aData.getCurrentTake().getTrackCount() <= 0) return;
        if(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack).keys.Count <= 0) return;
        int frame = aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack).getKeyFrameAfterFrame(aData.getCurrentTake().selectedFrame);
        if(frame <= -1) return;
        timelineSelectFrame(aData.getCurrentTake().selectedTrack, frame);

    }
    void timelineSelectPrevKey() {
        // select previous key
        if(aData.getCurrentTake().getTrackCount() <= 0) return;
        if(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack).keys.Count <= 0) return;
        int frame = aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack).getKeyFrameBeforeFrame(aData.getCurrentTake().selectedFrame);
        if(frame <= -1) return;
        timelineSelectFrame(aData.getCurrentTake().selectedTrack, frame);

    }
    public void selectFrame(int frame) {
        if(aData.getCurrentTake().selectedFrame != frame) {
            timelineSelectFrame(aData.getCurrentTake().selectedTrack, frame, false);
        }
    }
    void playerTogglePlay() {
        GUIUtility.keyboardControl = 0;
        cancelTextEditting();
        // toggle player off if is playing
        if(isPlaying) {
            isPlaying = false;
            // select where stopped
            timelineSelectFrame(aData.getCurrentTake().selectedTrack, aData.getCurrentTake().selectedFrame);
            aData.getCurrentTake().stopAudio();
            return;
        }
        // set preview player variables
        playerStartTime = Time.realtimeSinceStartup;
        playerStartFrame = aData.getCurrentTake().selectedFrame;
        // start playing
        isPlaying = true;
        // sample audio from current frame
        aData.getCurrentTake().sampleAudio((float)aData.getCurrentTake().selectedFrame, playbackSpeedValue[aData.getCurrentTake().playbackSpeedIndex]);
    }

    #endregion

    #region Set/Get

    string getNewTargetName() {
        int count = 1;
        while(true) {
            if(GameObject.Find("Target" + count)) count++;
            else break;
        }
        return "Target" + count;
    }
    void setDirtyKeys(AMTrack track) {
        foreach(AMKey key in track.keys) {
            EditorUtility.SetDirty(key);
        }
    }
    void setDirtyTakes(List<AMTake> takes) {
        foreach(AMTake take in takes) {
            EditorUtility.SetDirty(take);
        }
    }
    void setDirtyTracks(AMTake take) {
        foreach(AMTrack track in aData.getCurrentTake().trackValues) {
            EditorUtility.SetDirty(track);
        }
    }
    void setScrubSpeed() {
        scrubSpeed = ((currentMousePosition.x - startScrubMousePosition.x)) / 30;

    }
    void setScrollViewValue(float val) {
        val = Mathf.Clamp(val, 0f, maxScrollView());
        if(val < scrollViewValue.y || val > scrollViewValue.y + position.height - 66f)
            scrollViewValue.y = Mathf.Clamp(val, 0f, maxScrollView());
    }
    // timeline action info
    string getInfoTextForAction(AMTrack _track, AMKey _key, bool brief, int clamped) {
        // get text for track type
        #region translation
        if(_key is AMTranslationKey) {
            return easeTypeNames[(_key as AMTranslationKey).easeType];
        #endregion
            #region rotation
        }
        else if(_key is AMRotationKey) {
            return easeTypeNames[(_key as AMRotationKey).easeType];
            #endregion
            #region animation
        }
        else if(_key is AMAnimationKey) {
            if(!(_key as AMAnimationKey).amClip) return "Not Set";
            return (_key as AMAnimationKey).amClip.name + "\n" + ((WrapMode)(_key as AMAnimationKey).wrapMode).ToString();
            #endregion
            #region audio
        }
        else if(_key is AMAudioKey) {
            if(!(_key as AMAudioKey).audioClip) return "Not Set";
            return (_key as AMAudioKey).audioClip.name;
            #endregion
            #region property
        }
        else if(_key is AMPropertyKey) {
            string info = (_key as AMPropertyKey).getName() + "\n";
            if((_key as AMPropertyKey).targetsAreEqual()) brief = true;
            if(!brief && (_key as AMPropertyKey).endFrame != -1) {
                info += easeTypeNames[(_key as AMPropertyKey).easeType] + ": ";
            }
            string detail = (_key as AMPropertyKey).getValueString(brief);	// extra details such as integer values ex. 1 -> 12
            if(detail != null) info += detail;
            return info;
            #endregion
            #region event
        }
        else if(_key is AMEventKey) {
            if((_key as AMEventKey).methodInfo == null) {
                return "Not Set";
            }
            string txtInfoEvent = (_key as AMEventKey).methodName;
            // include parameters
            if((_key as AMEventKey).parameters != null) {
                txtInfoEvent += "(";
                for(int i = 0; i < (_key as AMEventKey).parameters.Count; i++) {
                    if((_key as AMEventKey).parameters[i] == null) txtInfoEvent += "";
                    else txtInfoEvent += (_key as AMEventKey).parameters[i].getStringValue();
                    if(i < (_key as AMEventKey).parameters.Count - 1) txtInfoEvent += ", ";
                }

                txtInfoEvent += ")";
                return txtInfoEvent;
            }
            return (_key as AMEventKey).methodName;
            #endregion
            #region orientation
        }
        else if(_key is AMOrientationKey) {
            if(!(_key as AMOrientationKey).target) return "No Target";
            string txtInfoOrientation = null;
            if((_key as AMOrientationKey).isLookFollow()) {
                txtInfoOrientation = (_key as AMOrientationKey).target.gameObject.name;
                return txtInfoOrientation;
            }
            txtInfoOrientation = (_key as AMOrientationKey).target.gameObject.name +
            " -> " + ((_key as AMOrientationKey).endTarget ? (_key as AMOrientationKey).endTarget.gameObject.name : "No Target");
            txtInfoOrientation += "\n" + easeTypeNames[(_key as AMOrientationKey).easeType];
            return txtInfoOrientation;
            #endregion
            #region goactive
        }
        else if(_key is AMGOSetActiveKey) {
            AMGOSetActiveKey act = _key as AMGOSetActiveKey;
            if(!act.go) return "No GameObject";

            if(brief)
                return string.Format("{0}.SetActive({1})", act.go.name, (_track as AMGOSetActiveTrack).startActive);
            else
                return string.Format("{0}.SetActive({1})", act.go.name, act.setActive);
            #endregion
        }

        return "Unknown";
    }
    public string getMethodInfoSignature(MethodInfo methodInfo) {
        ParameterInfo[] parameters = methodInfo.GetParameters();
        // loop through parameters, add them to signature
        string methodString = methodInfo.Name + " (";
        for(int i = 0; i < parameters.Length; i++) {
            methodString += typeStringBrief(parameters[i].ParameterType);
            if(i < parameters.Length - 1) methodString += ", ";
        }
        methodString += ")";
        return methodString;
    }
    public string[] getMethodNames() {
        // get all method names from every comonent on GameObject, and update methodinfo cache
        return cachedMethodNames.ToArray();
    }
    public Texture getTrackIconTexture(AMTrack _track) {
        if(_track is AMAnimationTrack) return texIconAnimation;
        else if(_track is AMEventTrack) return texIconEvent;
        else if(_track is AMPropertyTrack) return texIconProperty;
        else if(_track is AMTranslationTrack) return texIconTranslation;
        else if(_track is AMAudioTrack) return texIconAudio;
        else if(_track is AMRotationTrack) return texIconRotation;
        else if(_track is AMOrientationTrack) return texIconOrientation;
        else if(_track is AMGOSetActiveTrack) return texIconProperty;

        Debug.LogWarning("Animator: Icon texture not found for track " + _track.getTrackType());
        return null;
    }
    Vector2 getGlobalMousePosition(Event e) {
        Vector2 convertedGUIPos = GUIUtility.GUIToScreenPoint(e.mousePosition);
        convertedGUIPos.x -= position.x;
        convertedGUIPos.y -= position.y;
        return convertedGUIPos;
    }
    public static EditorWindow GetMainGameView() {
        System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        System.Reflection.MethodInfo GetMainGameView = T.GetMethod("GetMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        System.Object Res = GetMainGameView.Invoke(null, null);
        return (EditorWindow)Res;
    }
    public static Rect GetMainGameViewPosition() {
        return GetMainGameView().position;
    }

    #endregion

    #region Add/Delete

    void addTargetWithTranslationTrack(object key) {
        addTarget(key, true);
    }
    void addTargetWithoutTranslationTrack(object key) {
        addTarget(key, false);
    }
    void addTarget(object key, bool withTranslationTrack) {
        AMTrack sTrack = aData.getCurrentTake().getSelectedTrack();
        AMOrientationKey oKey = key as AMOrientationKey;
        // create target
        GameObject target = new GameObject(getNewTargetName());
        target.transform.position = (sTrack as AMOrientationTrack).obj.position + (sTrack as AMOrientationTrack).obj.forward * 5f;
        target.AddComponent(typeof(AMTarget));
        // set target
        oKey.setTarget(target.transform);
        // update cache
        sTrack.updateCache();
        AMCodeView.refresh();
        // preview new frame
        aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
        // save data
        setDirtyKeys(aData.getCurrentTake().getTrack(aData.getCurrentTake().selectedTrack));

        // add to translation track
        if(withTranslationTrack) {
            objects_window = new List<GameObject>();
            objects_window.Add(target);
            addTrack((int)Track.Translation);
        }
    }
    void addTrack(object trackType) {
		AMTake take = aData.getCurrentTake();

		Undo.RecordObject(take, "New Track");

        // add one null GameObject if no GameObject dragged onto timeline (when clicked on new track button)
        if(objects_window.Count <= 0) {
            objects_window.Add(null);
        }
        foreach(GameObject object_window in objects_window) {
            addTrackWithGameObject(trackType, object_window);
        }
        objects_window = new List<GameObject>();
		timelineSelectTrack(take.track_count, false);
        // move scrollview to last created track
		setScrollViewValue(take.getElementY(take.selectedTrack, height_track, height_track_foldin, height_group));

		EditorUtility.SetDirty(take);
        AMCodeView.refresh();
    }

    void addTrackWithGameObject(object trackType, GameObject object_window) {
        AMTrack ntrack;

        // add track based on index
        switch((int)trackType) {
            case (int)Track.Translation:
                ntrack = aData.getCurrentTake().addTranslationTrack(object_window);
                Undo.RegisterCreatedObjectUndo(ntrack, "New Translation Track");
                break;
            case (int)Track.Rotation:
                ntrack = aData.getCurrentTake().addRotationTrack(object_window);
                Undo.RegisterCreatedObjectUndo(ntrack, "New Rotation Track");
                break;
            case (int)Track.Orientation:
                ntrack = aData.getCurrentTake().addOrientationTrack(object_window);
                Undo.RegisterCreatedObjectUndo(ntrack, "New Orientation Track");
                break;
            case (int)Track.Animation:
                ntrack = aData.getCurrentTake().addAnimationTrack(object_window);
                Undo.RegisterCreatedObjectUndo(ntrack, "New Animation Track");
                break;
            case (int)Track.Audio:
                ntrack = aData.getCurrentTake().addAudioTrack(object_window);
                Undo.RegisterCreatedObjectUndo(ntrack, "New Audio Track");
                break;
            case (int)Track.Property:
                ntrack = aData.getCurrentTake().addPropertyTrack(object_window);
                Undo.RegisterCreatedObjectUndo(ntrack, "New Property Track");
                break;
            case (int)Track.Event:
                ntrack = aData.getCurrentTake().addEventTrack(object_window);
                Undo.RegisterCreatedObjectUndo(ntrack, "New Event Track");
                break;
            case (int)Track.GOSetActive:
                ntrack = aData.getCurrentTake().addGOSetActiveTrack(object_window);
                Undo.RegisterCreatedObjectUndo(ntrack, "New GO Set Active Track");
                break;
            default:
                int combo_index = (int)trackType - 100;
                if(combo_index >= oData.quickAdd_Combos.Count) {
                    Debug.LogError("Animator: Track type '" + (int)trackType + "' not found.");
                }
                else {
                    foreach(int _etrack in oData.quickAdd_Combos[combo_index]) {
                        addTrackWithGameObject((int)_etrack, object_window);
                    }
                }
                break;
        }
    }
    void addKeyToFrame(int frame) {
        // add key if there are tracks
        if(aData.getCurrentTake().getTrackCount() > 0) {
            // add a key
            addKey(aData.getCurrentTake().selectedTrack, frame);
            // preview current frame
            //aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
        }
        timelineSelectFrame(aData.getCurrentTake().selectedTrack, aData.getCurrentTake().selectedFrame, false);
    }
    void addKeyToSelectedFrame() {
        // add key if there are tracks
        if(aData.getCurrentTake().getTrackCount() > 0) {
            // add a key
            addKey(aData.getCurrentTake().selectedTrack, aData.getCurrentTake().selectedFrame);
            // preview current frame
            aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
        }
        timelineSelectFrame(aData.getCurrentTake().selectedTrack, aData.getCurrentTake().selectedFrame, false);
    }
    void addKey(int _track, int _frame) {
        // add a key to the track number and frame, used in OnGUI. Needs to be updated for every track type.
        AMTrack amTrack = aData.getCurrentTake().getTrack(_track);
        AMKey newKey = null;

		List<MonoBehaviour> stuff = new List<MonoBehaviour>(amTrack.keys.Count + 1);
		stuff.Add(amTrack);
		foreach(AMKey key in amTrack.keys)
			stuff.Add(key);
		Undo.RecordObjects(stuff.ToArray(), "New Key");

        // translation
        if(amTrack is AMTranslationTrack) {
            // if missing object, return
            if(!amTrack.genericObj) {
                showAlertMissingObjectType("Transform");
                return;
            }
            newKey = (amTrack as AMTranslationTrack).addKey(_frame, (amTrack as AMTranslationTrack).position, null);
        }
        else if(amTrack is AMRotationTrack) {
            // rotation

            // if missing object, return
            if(!amTrack.genericObj) {
                showAlertMissingObjectType("Transform");
                return;
            }
            // add key to rotation track
            newKey = (amTrack as AMRotationTrack).addKey(_frame, (amTrack as AMRotationTrack).rotation, null);
        }
        else if(amTrack is AMOrientationTrack) {
            // orientation

            // if missing object, return
            if(!(amTrack as AMOrientationTrack).obj) {
                showAlertMissingObjectType("Transform");
                return;
            }
            // add key to orientation track
            Transform last_target = null;
            int last_key = (amTrack as AMOrientationTrack).getKeyFrameBeforeFrame(_frame, false);
            if(last_key == -1) last_key = (amTrack as AMOrientationTrack).getKeyFrameAfterFrame(_frame, false);
            if(last_key != -1) {
                AMOrientationKey _oKey = ((amTrack as AMOrientationTrack).getKeyOnFrame(last_key) as AMOrientationKey);
                last_target = _oKey.target;
            }
            newKey = (amTrack as AMOrientationTrack).addKey(_frame, last_target);
        }
        else if(amTrack is AMAnimationTrack) {
            // animation

            // if missing object, return
            if(!(amTrack as AMAnimationTrack).obj) {
                showAlertMissingObjectType("GameObject");
                return;
            }
            // add key to animation track
            newKey = (amTrack as AMAnimationTrack).addKey(_frame, (amTrack as AMAnimationTrack).obj.animation.clip, WrapMode.Once);
        }
        else if(amTrack is AMAudioTrack) {
            // audio

            // if missing object, return
            if(!(amTrack as AMAudioTrack).audioSource) {
                showAlertMissingObjectType("AudioSource");
                return;
            }
            // add key to animation track
            newKey = (amTrack as AMAudioTrack).addKey(_frame, null, false);

        }
        else if(amTrack is AMPropertyTrack) {
            // property

            // if missing object, return
            if(!(amTrack as AMPropertyTrack).obj) {
                showAlertMissingObjectType("GameObject");
                return;
            }
            // if missing property, return
            if(!(amTrack as AMPropertyTrack).isPropertySet()) {
                EditorUtility.DisplayDialog("Property Not Set", "You must set the track property before you can add keys.", "Okay");
                return;
            }
            newKey = (amTrack as AMPropertyTrack).addKey(_frame);
        }
        else if(amTrack is AMEventTrack) {
            // event

            // if missing object, return
            if(!(amTrack as AMEventTrack).obj) {
                showAlertMissingObjectType("GameObject");
                return;
            }
            // add key to event track
            newKey = (amTrack as AMEventTrack).addKey(_frame);
        }
        else if(amTrack is AMGOSetActiveTrack) {
            // go set active

            // if missing object, return
            if(!amTrack.genericObj) {
                showAlertMissingObjectType("GameObject");
                return;
            }
            // add key to go active track
            newKey = (amTrack as AMGOSetActiveTrack).addKey(_frame);
        }

        if(newKey != null)
            Undo.RegisterCreatedObjectUndo(newKey, "New Key");

		EditorUtility.SetDirty(aData.getCurrentTake().getSelectedTrack());
        setDirtyKeys(aData.getCurrentTake().getSelectedTrack());
        
        AMCodeView.refresh();
    }
    void deleteKeyFromSelectedFrame() {
		AMTrack amTrack = aData.getCurrentTake().getSelectedTrack();
		
		List<MonoBehaviour> stuff = new List<MonoBehaviour>(amTrack.keys.Count + 1);
		stuff.Add(amTrack);
		foreach(AMKey key in amTrack.keys)
			stuff.Add(key);
		Undo.RecordObjects(stuff.ToArray(), "Clear Frame");
		
		AMKey[] dkeys = amTrack.removeKeyOnFrame(aData.getCurrentTake().selectedFrame);
		foreach(AMKey dkey in dkeys)
			Undo.DestroyObjectImmediate(dkey);

		amTrack.updateCache();
        
        // save data
		EditorUtility.SetDirty(amTrack);
		setDirtyKeys(amTrack);

		// select current frame
        timelineSelectFrame(aData.getCurrentTake().selectedTrack, aData.getCurrentTake().selectedFrame);

        AMCodeView.refresh();
    }
    void deleteSelectedKeys(bool showWarning) {
        bool shouldClearFrames = true;
        if(showWarning) {
            if(aData.getCurrentTake().contextSelectionTracks.Count > 1) {
                if(!EditorUtility.DisplayDialog("Clear From Multiple Tracks?", "Are you sure you want to clear the selected frames from all of the selected tracks?", "Clear Frames", "Cancel")) {
                    shouldClearFrames = false;
                }
            }
        }
        if(shouldClearFrames) {
            foreach(int track_id in aData.getCurrentTake().contextSelectionTracks) {
				AMTrack amTrack = aData.getCurrentTake().getTrack(track_id);
				List<MonoBehaviour> stuff = new List<MonoBehaviour>(amTrack.keys.Count + 1);
				stuff.Add(amTrack);
				foreach(AMKey key in amTrack.keys)
					stuff.Add(key);

				Undo.RegisterCompleteObjectUndo(stuff.ToArray(), "Clear Frames");

				AMKey[] dkeys = aData.getCurrentTake().removeSelectedKeysFromTrack(track_id);
				foreach(AMKey dkey in dkeys)
					Undo.DestroyObjectImmediate(dkey);

				EditorUtility.SetDirty(amTrack);
				setDirtyKeys(amTrack);
            }
        }

        // select current frame
        timelineSelectFrame(aData.getCurrentTake().selectedTrack, aData.getCurrentTake().selectedFrame, false);
        // refresh gizmos
        refreshGizmos();

        AMCodeView.refresh();
    }

    #endregion

    #region Menus/Context

    void addTrackFromMenu(object type) {
        addTrack((int)type);
    }
    void buildAddTrackMenu() {
        menu.AddItem(new GUIContent("Translation"), false, addTrackFromMenu, (int)Track.Translation);
        menu.AddItem(new GUIContent("Rotation"), false, addTrackFromMenu, (int)Track.Rotation);
        menu.AddItem(new GUIContent("Orientation"), false, addTrackFromMenu, (int)Track.Orientation);
        menu.AddItem(new GUIContent("Animation"), false, addTrackFromMenu, (int)Track.Animation);
        menu.AddItem(new GUIContent("Audio"), false, addTrackFromMenu, (int)Track.Audio);
        menu.AddItem(new GUIContent("Property"), false, addTrackFromMenu, (int)Track.Property);
        menu.AddItem(new GUIContent("Event"), false, addTrackFromMenu, (int)Track.Event);
        menu.AddItem(new GUIContent("GO Active"), false, addTrackFromMenu, (int)Track.GOSetActive);
    }
    void buildAddTrackMenu_Drag() {
        bool hasTransform = true;
        bool hasAnimation = true;
        bool hasAudioSource = true;
        bool hasCamera = true;

        foreach(GameObject g in objects_window) {
            // break loop when all variables are false
            if(!hasTransform && !hasAnimation && !hasAudioSource && !hasCamera) break;

            if(hasTransform && !g.GetComponent(typeof(Transform))) {
                hasTransform = false;
            }
            if(hasAnimation && !g.GetComponent(typeof(Animation))) {
                hasAnimation = false;
            }
            if(hasAudioSource && !g.GetComponent(typeof(AudioSource))) {
                hasAudioSource = false;
            }
            if(hasCamera && !g.GetComponent(typeof(Camera))) {
                hasCamera = false;
            }
        }
        // add track menu
        menu_drag = new GenericMenu();

        // Translation
        if(hasTransform) { menu_drag.AddItem(new GUIContent("Translation"), false, addTrackFromMenu, (int)Track.Translation); }
        else { menu_drag.AddDisabledItem(new GUIContent("Translation")); }
        // Rotation
        if(hasTransform) { menu_drag.AddItem(new GUIContent("Rotation"), false, addTrackFromMenu, (int)Track.Rotation); }
        else { menu_drag.AddDisabledItem(new GUIContent("Rotation")); }
        // Orientation
        if(hasTransform) menu_drag.AddItem(new GUIContent("Orientation"), false, addTrackFromMenu, (int)Track.Orientation);
        else menu_drag.AddDisabledItem(new GUIContent("Orientation"));
        // Animation
        if(hasAnimation) menu_drag.AddItem(new GUIContent("Animation"), false, addTrackFromMenu, (int)Track.Animation);
        else menu_drag.AddDisabledItem(new GUIContent("Animation"));
        // Audio
        if(hasAudioSource) menu_drag.AddItem(new GUIContent("Audio"), false, addTrackFromMenu, (int)Track.Audio);
        else menu_drag.AddDisabledItem(new GUIContent("Audio"));
        // Property
        menu_drag.AddItem(new GUIContent("Property"), false, addTrackFromMenu, (int)Track.Property);
        // Event
        menu_drag.AddItem(new GUIContent("Event"), false, addTrackFromMenu, (int)Track.Event);
        // GO Active
        menu_drag.AddItem(new GUIContent("GO Active"), false, addTrackFromMenu, (int)Track.GOSetActive);

        if(oData.quickAdd_Combos.Count > 0) {
            // multiple tracks
            menu_drag.AddSeparator("");
            foreach(List<int> combo in oData.quickAdd_Combos) {
                string combo_name = "";
                for(int i = 0; i < combo.Count; i++) {
                    //combo_name += Enum.GetName(typeof(Track),combo[i])+" ";
                    combo_name += TrackNames[combo[i]] + " ";
                    if(i < combo.Count - 1) combo_name += "+ ";
                }
                if(canQuickAddCombo(combo, hasTransform, hasAnimation, hasAudioSource, hasCamera)) menu_drag.AddItem(new GUIContent(combo_name), false, addTrackFromMenu, 100 + oData.quickAdd_Combos.IndexOf(combo));
                else menu_drag.AddDisabledItem(new GUIContent(combo_name));
            }
        }

    }
    bool canQuickAddCombo(List<int> combo, bool hasTransform, bool hasAnimation, bool hasAudioSource, bool hasCamera) {
        foreach(int _track in combo) {
            if(!hasTransform && (_track == (int)Track.Translation || _track == (int)Track.Rotation || _track == (int)Track.Orientation))
                return false;
            else if(!hasAnimation && _track == (int)Track.Animation)
                return false;
            else if(!hasAudioSource && _track == (int)Track.Audio)
                return false;
        }
        return true;
    }
    void buildContextMenu(int frame) {
        contextMenuFrame = frame;
        contextMenu = new GenericMenu();
        bool selectionHasKeys = aData.getCurrentTake().contextSelectionTracks.Count > 1 || aData.getCurrentTake().contextSelectionHasKeys();
        bool copyBufferNotEmpty = (contextSelectionKeysBuffer.Count > 0);
        bool canPaste = false;
        bool singleTrack = contextSelectionKeysBuffer.Count == 1;
        AMTrack selectedTrack = aData.getCurrentTake().getSelectedTrack();
        if(copyBufferNotEmpty) {
            if(singleTrack) {
                // if origin is property track
                if(selectedTrack is AMPropertyTrack) {
                    // if pasting into property track
                    if(contextSelectionTracksBuffer[0] is AMPropertyTrack) {
                        // if property tracks have the same property
                        if((selectedTrack as AMPropertyTrack).hasSamePropertyAs((contextSelectionTracksBuffer[0] as AMPropertyTrack))) {
                            canPaste = true;
                        }
                    }
                    // if origin is event track
                }
                else if(selectedTrack is AMEventTrack) {
                    // if pasting into event track
                    if(contextSelectionTracksBuffer[0] is AMEventTrack) {
                        // if event tracks are compaitable
                        if((selectedTrack as AMEventTrack).hasSameEventsAs((contextSelectionTracksBuffer[0] as AMEventTrack))) {
                            canPaste = true;
                        }
                    }
                }
                else {
                    if(selectedTrack.getTrackType() == contextSelectionTracksBuffer[0].getTrackType()) {
                        canPaste = true;
                    }
                }
            }
            else {
                // to do
                if(contextSelectionTracksBuffer.Contains(selectedTrack)) canPaste = true;
            }
        }
        contextMenu.AddItem(new GUIContent("Insert Keyframe"), false, invokeContextMenuItem, 0);
        contextMenu.AddSeparator("");
        if(selectionHasKeys) {
            contextMenu.AddItem(new GUIContent("Cut Frames"), false, invokeContextMenuItem, 1);
            contextMenu.AddItem(new GUIContent("Copy Frames"), false, invokeContextMenuItem, 2);
            if(canPaste) contextMenu.AddItem(new GUIContent("Paste Frames"), false, invokeContextMenuItem, 3);
            else contextMenu.AddDisabledItem(new GUIContent("Paste Frames"));
            contextMenu.AddItem(new GUIContent("Clear Frames"), false, invokeContextMenuItem, 4);
        }
        else {
            contextMenu.AddDisabledItem(new GUIContent("Cut Frames"));
            contextMenu.AddDisabledItem(new GUIContent("Copy Frames"));
            if(canPaste) contextMenu.AddItem(new GUIContent("Paste Frames"), false, invokeContextMenuItem, 3);
            else contextMenu.AddDisabledItem(new GUIContent("Paste Frames"));
            contextMenu.AddDisabledItem(new GUIContent("Clear Frames"));
        }
        contextMenu.AddItem(new GUIContent("Select All Frames"), false, invokeContextMenuItem, 5);
    }
    void invokeContextMenuItem(object _index) {
        int index = (int)_index;
        // insert keyframe
        if(index == 0) {
            addKeyToFrame(contextMenuFrame);
            selectFrame(contextMenuFrame);
        }
        else if(index == 1) contextCutKeys();
        else if(index == 2) contextCopyFrames();
        else if(index == 3) contextPasteKeys();
        else if(index == 4) deleteSelectedKeys(true);
        else if(index == 5) contextSelectAllFrames();
    }
    void contextCutKeys() {
        contextCopyFrames();
        deleteSelectedKeys(false);
    }
    void contextPasteKeys() {
        if(contextSelectionKeysBuffer == null || contextSelectionKeysBuffer.Count == 0) return;
		        
        bool singleTrack = contextSelectionKeysBuffer.Count == 1;
        int offset = (int)contextSelectionRange.y - (int)contextSelectionRange.x + 1;

        if(singleTrack) {
			AMTrack track = aData.getCurrentTake().getSelectedTrack();

			List<MonoBehaviour> stuffs = new List<MonoBehaviour>();
			stuffs.Add(track);
			foreach(AMKey k in track.keys)
				stuffs.Add(k);
			Undo.RegisterCompleteObjectUndo(stuffs.ToArray(), "Paste Frames");

            List<AMKey> newKeys = new List<AMKey>(contextSelectionKeysBuffer[0].Count);
            foreach(AMKey a in contextSelectionKeysBuffer[0]) {
                if(a != null) {
                    AMKey newKey = a.CreateClone(track.gameObject);
                    Undo.RegisterCreatedObjectUndo(newKey, "Paste Frames");
                    newKey.frame += (contextMenuFrame - (int)contextSelectionRange.x);
                    newKeys.Add(newKey);
                }
            }

			track.offsetKeysFromBy(contextMenuFrame, offset);

			track.keys.AddRange(newKeys);

			EditorUtility.SetDirty(track);
			setDirtyKeys(track);
        }
        else {
            List<AMKey> newKeys = new List<AMKey>();

            for(int i = 0; i < contextSelectionTracksBuffer.Count; i++) {
				AMTrack track = contextSelectionTracksBuffer[i];
				
				List<MonoBehaviour> stuffs = new List<MonoBehaviour>();
				stuffs.Add(track);
				foreach(AMKey k in track.keys)
					stuffs.Add(k);
				Undo.RegisterCompleteObjectUndo(stuffs.ToArray(), "Paste Frames");

                foreach(AMKey a in contextSelectionKeysBuffer[i]) {
                    if(a != null) {
                        AMKey newKey = a.CreateClone(track.gameObject);
                        Undo.RegisterCreatedObjectUndo(newKey, "Paste Frames");
                        newKey.frame += (contextMenuFrame - (int)contextSelectionRange.x);
                        newKeys.Add(newKey);
                    }
                }

                // offset all keys beyond paste
				track.offsetKeysFromBy(contextMenuFrame, offset);

				track.keys.AddRange(newKeys);

				EditorUtility.SetDirty(track);
				setDirtyKeys(track);

                newKeys.Clear();
            }
        }


        // show message if there are out of bounds keys
        checkForOutOfBoundsFramesOnSelectedTrack();
        // update cache
        if(singleTrack) {
            aData.getCurrentTake().getSelectedTrack().updateCache();
        }
        else {
            for(int i = 0; i < contextSelectionTracksBuffer.Count; i++) {
                contextSelectionTracksBuffer[i].updateCache();
            }
        }
        AMCodeView.refresh();
        // clear buffer
		ClearKeysBuffer();
        contextSelectionTracksBuffer.Clear();
        // update selection
        //   retrieve cached context selection 
        aData.getCurrentTake().contextSelection = new List<int>();
        foreach(int frame in cachedContextSelection) {
            aData.getCurrentTake().contextSelection.Add(frame);
        }
        // offset selection
        for(int i = 0; i < aData.getCurrentTake().contextSelection.Count; i++) {
            aData.getCurrentTake().contextSelection[i] += (contextMenuFrame - (int)contextSelectionRange.x);

        }
        // copy again for multiple pastes
        contextCopyFrames();
    }
    void contextSaveKeysToBuffer() {
        if(aData.getCurrentTake().contextSelection.Count <= 0) return;
        // sort
        aData.getCurrentTake().contextSelection.Sort();
        // set selection range
        contextSelectionRange.x = aData.getCurrentTake().contextSelection[0];
        contextSelectionRange.y = aData.getCurrentTake().contextSelection[aData.getCurrentTake().contextSelection.Count - 1];
        // set selection track
        //contextSelectionTrack = aData.getCurrentTake().selectedTrack;

		ClearKeysBuffer();
        aData.getCurrentTake().contextSelectionTracks.Sort();
        contextSelectionTracksBuffer.Clear();

        foreach(int track_id in aData.getCurrentTake().contextSelectionTracks) {
            contextSelectionTracksBuffer.Add(aData.getCurrentTake().getTrack(track_id));
        }
        foreach(AMTrack track in contextSelectionTracksBuffer) {
            contextSelectionKeysBuffer.Add(new List<AMKey>());
            foreach(AMKey key in aData.getCurrentTake().getContextSelectionKeysForTrack(track)) {
                contextSelectionKeysBuffer[contextSelectionKeysBuffer.Count - 1].Add(key.CreateClone(mTempHolder));
            }
        }
    }

    void contextCopyFrames() {
        cachedContextSelection.Clear();
        // cache context selection
        foreach(int frame in aData.getCurrentTake().contextSelection) {
            cachedContextSelection.Add(frame);
        }
        // save keys
        contextSaveKeysToBuffer();
    }
    void contextSelectAllFrames() {
		Undo.RecordObject(aData.getCurrentTake(), "Select All Frames");
        aData.getCurrentTake().contextSelectAllFrames();
    }
    public void contextSelectFrame(int frame, int prevFrame) {
        // select range if shift down
        if(isShiftDown) {
            // if control is down, toggle
            aData.getCurrentTake().contextSelectFrameRange(prevFrame, frame);
            return;
        }
        // clear context selection if control is not down
        if(!isControlDown) aData.getCurrentTake().contextSelection = new List<int>();
        // select single, toggle if control is down
        aData.getCurrentTake().contextSelectFrame(frame, isControlDown);
        //contextSelectFrameRange(frame,frame);
    }
    public void contextSelectFrameRange(int startFrame, int endFrame) {
        // clear context selection if control is not down
        if(isShiftDown) {
            aData.getCurrentTake().contextSelectFrameRange(aData.getCurrentTake().selectedFrame, endFrame);
            return;
        }
        if(!isControlDown) aData.getCurrentTake().contextSelection = new List<int>();

        aData.getCurrentTake().contextSelectFrameRange(startFrame, endFrame);
    }
    public void clearContextSelection() {
        aData.getCurrentTake().contextSelection = new List<int>();
    }

    #endregion

    #region Other Fns

    public int findWidthFontSize(float width, GUIStyle style, GUIContent content, int min = 8, int max = 15) {
        // finds the largest font size that can fit in the given style. max 15
        style.fontSize = max;
        while(style.CalcSize(content).x > width) {
            style.fontSize--;
            if(style.fontSize <= min) break;
        }
        return style.fontSize;
    }
    public int findHeightFontSize(float height, GUIStyle style, GUIContent content, int min = 8, int max = 15) {
        style.fontSize = max;
        while(style.CalcSize(content).y > height) {
            style.fontSize--;
            if(style.fontSize <= min) break;
        }
        return style.fontSize;
    }
    public bool rectContainsMouse(Rect rect, Vector2 mousePosition) {
        if(mousePosition.x < rect.x || mousePosition.x > rect.x + rect.width) return false;
        if(mousePosition.y < rect.y || mousePosition.y > rect.y + rect.height) return false;
        return true;
    }
    public bool didDoubleClick(string elementID) {
        if(doubleClickElementID != elementID) {
            doubleClickCachedTime = EditorApplication.timeSinceStartup;
            doubleClickElementID = elementID;
            return false;
        }
        if(EditorApplication.timeSinceStartup - doubleClickCachedTime <= doubleClickTime) {
            doubleClickElementID = null;
            return true;
        }
        else {
            doubleClickCachedTime = EditorApplication.timeSinceStartup;
            return false;
        }
    }
    public void updateCachedMethodInfo(GameObject go) {
        if(!go) return;
        cachedMethodInfo = new List<MethodInfo>();
        cachedMethodNames = new List<string>();
        cachedMethodInfoComponents = new List<Component>();
        Component[] arrComponents = go.GetComponents(typeof(Component));
        foreach(Component c in arrComponents) {
            if(c.GetType().BaseType == typeof(Component) || c.GetType().BaseType == typeof(Behaviour)) continue;
            MethodInfo[] methodInfos = c.GetType().GetMethods(methodFlags);
            foreach(MethodInfo methodInfo in methodInfos) {
                if((methodInfo.Name == "Start") || (methodInfo.Name == "Update") || (methodInfo.Name == "Main")) continue;
                cachedMethodNames.Add(getMethodInfoSignature(methodInfo));
                cachedMethodInfo.Add(methodInfo);
                cachedMethodInfoComponents.Add(c);
            }
        }
    }
	//returns deleted keys
    AMKey[] checkForOutOfBoundsFramesOnSelectedTrack() {
		AMTake take = aData.getCurrentTake();
		List<AMKey> dkeys = new List<AMKey>();
        List<AMTrack> selectedTracks = new List<AMTrack>();
        int shift = 1;
        AMTrack _track_shift = null;
        int increase = 0;
        AMTrack _track_increase = null;
		foreach(int track_id in take.contextSelectionTracks) {
			AMTrack track = take.getTrack(track_id);
            selectedTracks.Add(track);
            if(track.keys.Count >= 1) {
                if(track.keys[0].frame < shift) {
                    shift = track.keys[0].frame;
                    _track_shift = track;
                }
				if(track.keys[track.keys.Count - 1].frame - take.numFrames > increase) {
					increase = track.keys[track.keys.Count - 1].frame - take.numFrames;
                    _track_increase = track;
                }
            }
        }
        if(_track_shift != null) {
            if(EditorUtility.DisplayDialog("Shift Frames?", "Keyframes have been moved out of bounds before the first frame. Some data will be lost if frames are not shifted.", "Shift", "No")) {
				take.shiftOutOfBoundsKeysOnTrack(_track_shift);
            }
            else {
                // delete all keys beyond last frame
				dkeys.AddRange(take.removeKeysBefore(1));
            }
        }
        if(_track_increase != null) {
            if(EditorUtility.DisplayDialog("Increase Number of Frames?", "Keyframes have been pushed out of bounds beyond the last frame. Some data will be lost if the number of frames is not increased.", "Increase", "No")) {
				take.numFrames = _track_increase.keys[_track_increase.keys.Count - 1].frame;
            }
            else {
                // delete all keys beyond last frame
                foreach(AMTrack track in selectedTracks)
					dkeys.AddRange(track.removeKeysAfter(take.numFrames));
            }
        }

		return dkeys.ToArray();
    }
    void checkForOutOfBoundsFramesOnTrack(int track_id) {
        AMTrack _track = aData.getCurrentTake().getTrack(track_id);
        if(_track.keys.Count <= 0) return;
        bool needsShift = false;
        bool needsIncrease = false;
        _track.sortKeys();
        // if first key less than 1
        if(_track.keys.Count >= 1 && _track.keys[0].frame < 1) {
            needsShift = true;
        }
        if(needsShift) {
            if(EditorUtility.DisplayDialog("Shift Frames?", "Keyframes have been moved out of bounds before the first frame. Some data will be lost if frames are not shifted.", "Shift", "No")) {
                aData.getCurrentTake().shiftOutOfBoundsKeysOnSelectedTrack();
            }
            else {
                // delete all keys beyond last frame
                aData.getCurrentTake().deleteKeysBefore(1);
            }
        }
        // if last key is beyond last frame
        if(_track.keys.Count >= 1 && _track.keys[_track.keys.Count - 1].frame > aData.getCurrentTake().numFrames) {
            needsIncrease = true;
        }
        if(needsIncrease) {
            if(EditorUtility.DisplayDialog("Increase Number of Frames?", "Keyframes have been pushed out of bounds beyond the last frame. Some data will be lost if the number of frames is not increased.", "Increase", "No")) {
                aData.getCurrentTake().numFrames = _track.keys[_track.keys.Count - 1].frame;
            }
            else {
                // delete all keys beyond last frame
                _track.deleteKeysAfter(aData.getCurrentTake().numFrames);
            }
        }
    }
    void resetPreview() {
        // reset all object transforms to frame 1
        aData.getCurrentTake().previewFrame(1f);
    }
    void refreshGizmos() {
        EditorUtility.SetDirty(aData);
    }
    void cancelTextEditting(bool toggleIsRenamingTake = false) {
        if(!isChangingTimeControl && !isChangingFrameControl) {
            try {
                if(GUIUtility.keyboardControl != 0) GUIUtility.keyboardControl = 0;
            }
            catch {

            }
        }
        if(isRenamingTrack != -1) {
            isRenamingTrack = -1;
        }
        if(isRenamingTake) {
            aData.makeTakeNameUnique(aData.getCurrentTake());
        }
        if(toggleIsRenamingTake) {
            isRenamingTake = !isRenamingTake;
        }
        else {
            if(isRenamingTake) isRenamingTake = false;
        }
        if(isRenamingGroup != 0) isRenamingGroup = 0;
    }
    WrapMode indexToWrapMode(int index) {
        switch(index) {
            case 0:
                return WrapMode.Once;
            case 1:
                return WrapMode.Loop;
            case 2:
                return WrapMode.ClampForever;
            case 3:
                return WrapMode.PingPong;
            default:
                Debug.LogError("Animator: No Wrap Mode found for index " + index);
                return WrapMode.Default;
        }
    }
    string trimString(string _str, int max_chars) {
        if(_str.Length <= max_chars) return _str;
        return _str.Substring(0, max_chars) + "...";
    }
    string typeStringBrief(Type t) {
        if(t.IsArray) return typeStringBrief(t.GetElementType()) + "[]";
        if(t == typeof(int)) return "int";
        if(t == typeof(long)) return "long";
        if(t == typeof(float)) return "float";
        if(t == typeof(double)) return "double";
        if(t == typeof(Vector2)) return "Vector2";
        if(t == typeof(Vector3)) return "Vector3";
        if(t == typeof(Vector4)) return "Vector4";
        if(t == typeof(Color)) return "Color";
        if(t == typeof(Rect)) return "Rect";
        if(t == typeof(string)) return "string";
        if(t == typeof(char)) return "char";
        return t.Name;
    }
    int wrapModeToIndex(WrapMode wrapMode) {
        switch(wrapMode) {
            case WrapMode.Once:
                return 0;
            case WrapMode.Loop:
                return 1;
            case WrapMode.ClampForever:
                return 2;
            case WrapMode.PingPong:
                return 3;
            default:
                Debug.LogError("Animator: No Index found for WrapMode " + wrapMode.ToString());
                return -1;
        }
    }
    float maxScrollView() {
        return height_all_tracks;
    }

    #endregion

    #endregion
}

