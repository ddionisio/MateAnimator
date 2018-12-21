using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

using DG.Tweening;

namespace M8.Animator.Edit {
    public class TimelineWindow : EditorWindow {
        [MenuItem("M8/Animator")]
        static void Init() {
            EditorWindow.GetWindow(typeof(TimelineWindow), false, "Animator Timeline");
        }

        #region Declarations

        public static TimelineWindow window = null;

        const BindingFlags methodFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        private AnimateEditControl _aData;
        public AnimateEditControl aData {
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

                        if(_aData.isValid)
                            _aData.RefreshTakes();
                        else
                            EditDataCleanUp();
                    }

                    _aData = value;

                    if(_aData != null) {
                        _aData.Refresh();

                        //!DEBUG
#if MATE_DEBUG_ANIMATOR
	                    Transform holder = _aData.target.holder;
	                    if(holder.GetComponent<AnimateHolder>() == null)
	                        holder.gameObject.AddComponent<AnimateHolder>();
#endif

                        indexMethodInfo = -1;   // re-check for methodinfo

                        //Debug.Log("new data: " + _aData.name + " hash: " + _aData.GetHashCode());

                        ReloadOtherWindows();
                    }
                    //else
                    //Debug.Log("no data");
                }
            }
        }// Animate component, holds all data

        public OptionsFile oData;
        private Vector2 scrollViewValue;            // current value in scrollview (vertical)
        private string[] playbackSpeed = { "x.25", "x.5", "x1", "x2", "x4" };
        private float[] playbackSpeedValue = { .25f, .5f, 1f, 2f, 4f };
        private float cachedZoom = 0f;
        private float numFramesToRender;            // number of frames to render, based on window size
        private bool isPlaying;                     // is preview player playing
        private float playerStartTime;              // preview player start time
        private int playerStartFrame;               // preview player start frame
        private int playerStartedFrame;
        private int playerCurLoop;                  // current number of loops made
        private bool playerBackward;                // playing backwards

        private static string[] mEaseTypeNames;
        private static int[] mEaseIndices;
        public static string[] easeTypeNames {
            get {
                if(mEaseTypeNames == null)
                    InitEaseTypeNames();
                return mEaseTypeNames;
            }
        }

        public static int GetEaseIndex(int easeTypeNameIndex) {
            if(mEaseIndices == null)
                InitEaseTypeNames();
            return mEaseIndices[easeTypeNameIndex];
        }

        public static int GetEaseTypeNameIndex(Ease easeType) {
            string easeName = easeType == Ease.INTERNAL_Custom ? "Custom" : easeType.ToString();
            return System.Array.IndexOf(easeTypeNames, easeName);
        }

        private static void InitEaseTypeNames() {
            List<string> _namesAdded = new List<string>();
            List<int> _nameIndicesAdded = new List<int>();
            string[] _names = System.Enum.GetNames(typeof(Ease));
            for(int i = 0; i < _names.Length; i++) {
                if(_names[i] == "Unset" || _names[i] == "INTERNAL_Zero")
                    continue;
                else if(_names[i] == "INTERNAL_Custom")
                    _namesAdded.Add("Custom");
                else
                    _namesAdded.Add(_names[i]);

                _nameIndicesAdded.Add(i);
            }

            mEaseTypeNames = _namesAdded.ToArray();
            mEaseIndices = _nameIndicesAdded.ToArray();
        }

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
            Group = 1,              // dragged to inside group
            GroupOutside = 2,       // dragged to outside group
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

        // skins
        public static string global_skin = "am_skin_dark";
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
        private float height_indicator_offset_y = 33f;      // indicator vertical offset
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
        private Color colBirdsEyeFrames;
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
        private Texture texPropertiesTop;
        private Texture texRightArrow;// inspector right arrow
        private Texture texLeftArrow;   // inspector left arrow
        private Texture[] texInterpl3 = new Texture[3];
        private Texture[] texInterpl2 = new Texture[2];
        private Texture texBoxBorder;
        private Texture texBoxRed;
        //private Texture texBoxBlue;
        private Texture texBoxLightBlue;
        private Texture texBoxDarkBlue;
        private Texture texBoxGreen;
        private Texture texBoxPink;
        private Texture texBoxYellow;
        private Texture texBoxOrange;
        private Texture texBoxLightPurple;
        private Texture texBoxPurple;
        private Texture texIconTranslation;
        private Texture texIconRotation;
        private Texture texIconScale;
        private Texture texIconAnimation;
        private Texture texIconAudio;
        private Texture texIconProperty;
        private Texture texIconEvent;
        private Texture texIconOrientation;
        private Texture texIconCameraSwitcher;
        private Texture texIconMaterial;
        public static bool texLoaded = false;

        // temporary variables
        private bool isPlayMode = false;        // whether the user is in play mode, used to close TimelineWindow when in play mode
        private bool isRenamingTake = false;    // true if the user is renaming the current take
        private int isRenamingTrack = -1;       // the track the is user renaming, -1 if not renaming a track
        private int isRenamingGroup = 0;        // the group that the user is renaming, 0 if not renaming a group
                                                //private int repaintBuffer = 0;
                                                //private int repaintRefreshRate = 1;		// repaint every x update frames if necessary
        private static List<MethodInfo> cachedMethodInfo = new List<MethodInfo>();
        private List<string> cachedMethodNames = new List<string>();
        private int updateRateMethodInfoCache = 2;      // update method info cache every x update frames if necessary
        private int updateMethodInfoCacheBuffer = 0;    // temporary value
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
        private static Dictionary<string, bool> arrayFieldFoldout = new Dictionary<string, bool>(); // used to store the foldout values for arrays in event methods
        private Vector2 inspectorScrollView = new Vector2(0f, 0f);
        private GenericMenu menu = new GenericMenu();           // add track menu
        private GenericMenu menu_drag = new GenericMenu();      // add track menu, on drag to window
        private GenericMenu contextMenu = new GenericMenu();    // context selection menu
        private float height_all_tracks = 0f;

        // context selection variables
        private bool isControlDown = false;
        private bool isShiftDown = false;
        private bool isSpaceBarDown = false;
        private bool isDragging = false;
        private int dragType = -1;
        private bool cachedIsDragging = false;              // used to determine dragging is started or stopped
        private float scrubSpeed = 0f;
        private int startDragFrame = 0;
        private int ghostStartDragFrame = 0;    // temporary value used to drag the ghost selection
        private int endDragFrame = 0;
        private int contextMenuFrame = 0;
        //private int contextSelectionTrack = 0;
        private Vector2 contextSelectionRange = new Vector2(0f, 0f);            // holds the first and last frame of the copied selection
                                                                                //private List<Key> contextSelectionKeysBuffer = new List<Key>();	// stores the copied context selection keys
        private List<List<Key>> contextSelectionKeysBuffer = new List<List<Key>>();
        private List<Track> contextSelectionTracksBuffer = new List<Track>();
        private List<int> cachedContextSelection = new List<int>();         // cache context selection when user copies, used for paste
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
        private bool dragPan;
        private float startScrollViewValue;
        private float startFrame;
        private bool didPeakZoom = false;
        private bool wasZoomingIn = false;
        private float startZoomValue = 0f;
        private int startZoomXOverFrame = 0;
        private float startResize_width_track = 0f;
        private int mouseOverFrame = 0;                 // the frame number that the mouse X and Y is over, 0 if one
        private int mouseXOverFrame = 0;                // the frame number that the mouse X is over, 0 if none
        private int mouseOverTrack = -1;                // mouse over frame track, -1 if no track
        private int mouseOverElement = -1;
        private int mouseMoveTrack = -1;                // mouse move frame track, -1 if no track
        private int mouseMoveFrame = 0;                 // mouse move frame, 0 if none
        private int mouseXOverHScrollbarFrame = 0;
        private Vector2 mouseOverGroupElement = new Vector2(-1, -1);    // group_id, track id
        private bool mouseOverSelectedFrame = false;
        private Vector2 currentMousePosition = new Vector2(0f, 0f);
        private Vector2 draggingGroupElement = new Vector2(-1, -1);
        private int draggingGroupElementType = -1;
        private bool mouseAboveGroupElements = false;   // is mouse above group elements? used to determine whether a dropped group element should be placed in the first position
        private int ticker = 0;
        private int tickerSpeed = 50;
        private float scrollAmountVertical = 0f;
        private List<GameObject> objects_window = new List<GameObject>();
        private int startResizeActionFrame = -1;
        private int resizeActionFrame = -1;
        private int endResizeActionFrame = -1;
        private float[] arrKeyRatiosLeft;
        private float[] arrKeyRatiosRight;
        Key[] arrKeysLeft;
        Key[] arrKeysRight;
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

        private AnimateMeta mMeta = null;

        //used when we need to add track during OnGUI (workaround for things like dragging to window Unity bug)
        private int mAddTrackTypeIndexLate = -1;
        private int mAddTrackTypeIndexLateCounter = 0; //this is a cheesy way to allow a new cycle to happen before adding track

        private static Dictionary<Take, TakeEditControl> mTakeEdits = new Dictionary<Take, TakeEditControl>();
        public static TakeEditControl TakeEdit(Take take) {
            TakeEditControl ret = null;
            if(!mTakeEdits.TryGetValue(take, out ret)) {
                ret = new TakeEditControl();
                mTakeEdits.Add(take, ret);
            }
            return ret;
        }

        private static Dictionary<Animate, AnimateEditControl> mAnimEdits = new Dictionary<Animate, AnimateEditControl>();
        public static AnimateEditControl AnimEdit(Animate anim) {
            if(anim == null) return null;

            AnimateEditControl animEdit;
            if(!mAnimEdits.TryGetValue(anim, out animEdit)) {
                mAnimEdits.Add(anim, animEdit = new AnimateEditControl(anim));
            }

            return animEdit;
        }

        public static void EditDataCleanUp() {
            Dictionary<Animate, AnimateEditControl> newAnimEdits = new Dictionary<Animate, AnimateEditControl>(mAnimEdits.Count);
            foreach(var pair in mAnimEdits.Where(pair => pair.Key != null))
                newAnimEdits.Add(pair.Key, pair.Value);
            mAnimEdits = newAnimEdits;

            Dictionary<Take, TakeEditControl> newTakeEdits = new Dictionary<Take, TakeEditControl>(mTakeEdits.Count);
            foreach(var pair in mTakeEdits.Where(pair => pair.Key != null))
                newTakeEdits.Add(pair.Key, pair.Value);
            mTakeEdits = newTakeEdits;
        }

        private GameObject mTempHolder;

        #endregion

        #region Main

        void OnEnable() {
            EditDataCleanUp();

            LoadEditorTextures();
            titleContent = new GUIContent("Mate Animator");
            minSize = new Vector2(width_track + width_playback_controls + width_inspector_open + 70f, 190f);
            wantsMouseMove = true;
            window = this;
            // find component
            if(aData == null && !EditorApplication.isPlayingOrWillChangePlaymode) {
                GameObject go = Selection.activeGameObject;
                if(go && go.scene.IsValid()) {
                    aData = AnimEdit(go.GetComponent<Animate>());
                }
            }

            oData = OptionsFile.loadFile();
            // set default current dimensions of frames
            //current_width_frame = width_frame;
            //current_height_frame = height_frame;
            // set is playing to false
            isPlaying = false;

            // add track menu
            buildAddTrackMenu();

            // playmode callback
            EditorApplication.playModeStateChanged += OnPlayMode;

            // check for pro license (no need for Unity 5)
            Take.isProLicense = true;

            //autoRepaintOnSceneChange = true;

            mTempHolder = new GameObject();
            mTempHolder.hideFlags = HideFlags.HideAndDontSave;

            Undo.undoRedoPerformed += OnUndoRedo;
        }
        void OnDisable() {
            EditorApplication.playModeStateChanged -= OnPlayMode;

            window = null;
            if(aData != null) {
                if(aData.currentTake != null) {
                    // stop audio if it's playing
                    aData.currentTake.Stop(aData.target);

                    if(oData.resetTakeOnClose) {
                        // preview first frame
                        aData.currentTake.previewFrame(aData.target, 1f);
                    }
                }

                aData = null;

                // reset property select track
                //aData.propertySelectTrack = null;
            }

            if(CameraFade.hasInstance() && CameraFade.isPreview())
                CameraFade.destroyImmediateInstance();

            Undo.undoRedoPerformed -= OnUndoRedo;

            EditDataCleanUp();
        }
        void OnUndoRedo() {
            if(aData == null) return;

            foreach(Take take in aData.takes)
                take.undoRedoPerformed();

            ResetDragging();
            Repaint();
        }
        void OnFocus() {
            ResetDragging();
            Repaint();
        }
        void OnLostFocus() {
            ResetDragging();
        }
        void ResetDragging() {
            isDragging = false;
            dragType = (int)DragType.None;
        }

        void LoadEditorTextures() {
            if(!texLoaded) {
                colBirdsEyeFrames = EditorGUIUtility.isProSkin ? new Color(44f / 255f, 43f / 255f, 43f / 255f, 1f) : new Color(210f / 255f, 210f / 255f, 210f / 255f, 1f);
                tex_cursor_zoomin = EditorResource.LoadEditorTexture("am_cursor_zoomin");
                tex_cursor_zoomout = EditorResource.LoadEditorTexture("am_cursor_zoomout");
                tex_cursor_zoom_blank = EditorResource.LoadEditorTexture("am_cursor_zoom_blank");
                tex_cursor_zoom = null;
                tex_cursor_grab = EditorResource.LoadEditorTexture("am_cursor_grab");
                tex_icon_track = EditorResource.LoadEditorTexture("am_icon_track");
                tex_icon_track_hover = EditorResource.LoadEditorTexture("am_icon_track_hover");
                tex_icon_group_closed = EditorResource.LoadEditorTexture("am_icon_group_closed");
                tex_icon_group_open = EditorResource.LoadEditorTexture("am_icon_group_open");
                tex_icon_group_hover = EditorResource.LoadEditorTexture("am_icon_group_hover");
                tex_element_position = EditorResource.LoadEditorTexture("am_element_position");
                texFrKey = EditorResource.LoadEditorTexture(EditorGUIUtility.isProSkin ? "am_key" : "am_key_light");
                texFrSet = EditorResource.LoadEditorTexture(EditorGUIUtility.isProSkin ? "am_frame_set" : "am_frame_set_light");
                texKeyBirdsEye = EditorResource.LoadEditorTexture("am_key_birdseye");
                texIndLine = EditorResource.LoadEditorTexture("am_indicator_line");
                texIndHead = EditorResource.LoadEditorTexture("am_indicator_head");
                texProperties = EditorResource.LoadEditorTexture(EditorGUIUtility.isProSkin ? "am_information" : "am_information_light");
                texRightArrow = EditorResource.LoadEditorTexture(EditorGUIUtility.isProSkin ? "am_nav_right" : "am_nav_right_light");// inspector right arrow
                texLeftArrow = EditorResource.LoadEditorTexture(EditorGUIUtility.isProSkin ? "am_nav_left" : "am_nav_left_light");    // inspector left arrow
                texInterpl3[0] = EditorResource.LoadEditorTexture(EditorGUIUtility.isProSkin ? "am_interpl_curve" : "am_interpl_curve_light");
                texInterpl2[0] = texInterpl3[1] = EditorResource.LoadEditorTexture(EditorGUIUtility.isProSkin ? "am_interpl_linear" : "am_interpl_linear_light");
                texInterpl2[1] = texInterpl3[2] = EditorResource.LoadEditorTexture(EditorGUIUtility.isProSkin ? "am_interpl_none" : "am_interpl_none_light");
                texBoxBorder = EditorResource.LoadEditorTexture("am_box_border");
                texBoxRed = EditorResource.LoadEditorTexture("am_box_red");
                //texBoxBlue = AMEditorResource.LoadTexture("am_box_blue");
                texBoxLightBlue = EditorResource.LoadEditorTexture("am_box_lightblue");
                texBoxDarkBlue = EditorResource.LoadEditorTexture("am_box_darkblue");
                texBoxGreen = EditorResource.LoadEditorTexture("am_box_green");
                texBoxPink = EditorResource.LoadEditorTexture("am_box_pink");
                texBoxYellow = EditorResource.LoadEditorTexture("am_box_yellow");
                texBoxOrange = EditorResource.LoadEditorTexture("am_box_orange");
                texBoxLightPurple = EditorResource.LoadEditorTexture("am_box_lightpurple");
                texBoxPurple = EditorResource.LoadEditorTexture("am_box_purple");
                texIconTranslation = EditorResource.LoadEditorTexture("am_icon_translation");
                texIconScale = EditorResource.LoadEditorTexture("am_icon_scale");
                texIconRotation = EditorResource.LoadEditorTexture("am_icon_rotation");
                texIconAnimation = EditorResource.LoadEditorTexture("am_icon_animation");
                texIconAudio = EditorResource.LoadEditorTexture("am_icon_audio");
                texIconProperty = EditorResource.LoadEditorTexture("am_icon_property");
                texIconEvent = EditorResource.LoadEditorTexture("am_icon_event");
                texIconOrientation = EditorResource.LoadEditorTexture("am_icon_orientation");
                texIconCameraSwitcher = EditorResource.LoadEditorTexture("am_icon_cameraswitcher");
                texIconMaterial = EditorResource.LoadEditorTexture("am_icon_material");

                texLoaded = true;
            }
        }

        TakeEditControl TakeEditCurrent() {
            return TakeEdit(aData.currentTake);
        }
        void TakeEditRemove(Take take) {
            mTakeEdits.Remove(take);
        }
        int GetCurTakeNumFrames() {
            int framesPerPage = oData.framesPerPage;
            int maxPage = oData.maxPage;

            if(aData == null) return framesPerPage * maxPage;

            var curTake = aData.currentTake;

            int totalFrames = curTake.totalFrames;

            int numPage = Mathf.CeilToInt(totalFrames / (float)framesPerPage);

            if(numPage >= maxPage)
                return (Mathf.FloorToInt(totalFrames / (float)framesPerPage) + maxPage) * framesPerPage;
            else
                return framesPerPage * maxPage;

        }
        void ClearKeysBuffer() {
            contextSelectionKeysBuffer.Clear();
        }
        
        void Update() {
            if(aData != null && !aData.isValid) {
                aData = null;
                Repaint();
                return;
            }

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

            if(aData == null) return;
            Take currentTake = aData.currentTake;

            // if preview is playing
            if(isPlaying || dragType == (int)DragType.TimeScrub || dragType == (int)DragType.FrameScrub) {
                int numFrames = GetCurTakeNumFrames();
                int playbackSpeedIndex = TakeEdit(currentTake).playbackSpeedIndex;
                float timeRunning = Time.realtimeSinceStartup - playerStartTime;
                // determine current frame
                float curFrame = currentTake.selectedFrame;
                // if scrubbing
                if(dragType == (int)DragType.TimeScrub || dragType == (int)DragType.FrameScrub) {
                    if(scrubSpeed < 0) scrubSpeed *= 5;
                    curFrame += Mathf.CeilToInt(scrubSpeed);
                    curFrame = Mathf.Clamp(curFrame, 1, numFrames);
                }
                else {
                    // determine speed
                    float speed = (float)currentTake.frameRate * playbackSpeedValue[playbackSpeedIndex];
                    curFrame = playerStartFrame + (playerBackward ? -timeRunning * speed : timeRunning * speed);
                    int curFrameI = Mathf.FloorToInt(curFrame);
                    int lastFrame = currentTake.getLastFrame();
                    if(currentTake.numLoop >= 0 || currentTake.loopBackToFrame <= 0)
                        lastFrame += currentTake.endFramePadding;

                    //reached end?
                    if((playerBackward && curFrameI < 0) || curFrameI >= lastFrame) {
                        bool restart = true;
                        // loop
                        if(currentTake.numLoop > 0) {
                            playerCurLoop++;
                            restart = playerCurLoop < currentTake.numLoop;
                        }

                        int startFrame = 1;
                        if(restart) {
                            if(currentTake.loopMode == LoopType.Yoyo)
                                playerBackward = !playerBackward;

                            if(!playerBackward && currentTake.numLoop < 0 && currentTake.loopBackToFrame > 0)
                                startFrame = currentTake.loopBackToFrame;
                            else
                                startFrame = 1;
                        }

                        if(restart) {
                            playerStartTime = Time.realtimeSinceStartup;

                            if(curFrameI < 0) {
                                curFrame = 1.0f;
                            }
                            else {
                                curFrame = curFrame - (float)lastFrame;
                                if(playerBackward)
                                    curFrame += (float)lastFrame;
                            }

                            playerStartFrame = Mathf.FloorToInt(curFrame);

                            if(playerBackward) {
                                if(playerStartFrame > lastFrame) playerStartFrame = lastFrame;
                            }
                            else {
                                if(playerStartFrame < startFrame) playerStartFrame = startFrame;
                            }
                        }
                        else {
                            isPlaying = false;
                            curFrame = playerStartedFrame;
                        }

                        currentTake.PlayComplete(aData.target);
                    }
                }
                // select the appropriate frame
                bool play = isPlaying && !playerBackward && dragType != (int)DragType.TimeScrub && dragType != (int)DragType.FrameScrub; //only allow play when playing forward
                if(Mathf.FloorToInt(curFrame) != currentTake.selectedFrame) {
                    TakeEditCurrent().selectFrame(currentTake, TakeEditCurrent().selectedTrack, Mathf.FloorToInt(curFrame), numFramesToRender, false, false);
                    this.Repaint();
                }
                currentTake.previewFrame(aData.target, curFrame, false, true, play, playbackSpeedValue[playbackSpeedIndex]);
            }
            else if(mAddTrackTypeIndexLate != -1) { //add track delay
                if(mAddTrackTypeIndexLateCounter < 1) { //TODO: this allows for one cycle to finish, need a better way to do this
                    mAddTrackTypeIndexLateCounter++;
                    return;
                }

                aData.RegisterTakesUndo("New Track", false);
                addTrack(mAddTrackTypeIndexLate);

                mAddTrackTypeIndexLate = -1;
                mAddTrackTypeIndexLateCounter = 0;

                this.Repaint();
            }
            else {
                // autokey
                if(!isDragging && aData != null && aData.autoKey) {
                    aData.RegisterTakesUndo("Auto Key", false);

                    bool autoKeyMade = currentTake.autoKey(aData.target, Selection.activeTransform, currentTake.selectedFrame);

                    if(autoKeyMade) {
                        // preview frame, update orientation only
                        currentTake.previewFrame(aData.target, currentTake.selectedFrame, true, false);

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
            if(aData == null) {
                if(Selection.activeGameObject && Selection.activeGameObject.scene.IsValid()) { //if scene is not valid, it must be from the asset
                    AnimateEditControl newDat = AnimEdit(Selection.activeGameObject.GetComponent<Animate>());
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
                oData = OptionsFile.loadFile();
            }

            if(EditorApplication.isPlayingOrWillChangePlaymode) {
                this.ShowNotification(new GUIContent("Play Mode"));
                return;
            }
            if(EditorApplication.isCompiling) {
                this.ShowNotification(new GUIContent("Code Compiling"));
                return;
            }
            #region no data component
            if(aData == null) {
                // recheck for component
                GameObject go = Selection.activeGameObject;
                if(go) {
                    if(go.scene.IsValid()) { //if scene is not valid, it must be from the asset
                        aData = AnimEdit(go.GetComponent<Animate>());
                    }
                    else
                        go = null;
                }
                if(aData == null) {
                    GUI.skin = null;

                    // no data component message
                    if(go) {
                        MessageBox("Animator requires an Animate component in your game object.", MessageBoxType.Info);
                    }
                    else {
                        MessageBox("Animator requires an Animate in scene.", MessageBoxType.Info);
                    }

                    //meta stuff
                    GUILayout.Label("Use AnimateMeta to create a shared animation data. Leave it blank to have the data stored to the Animate.");

                    GUILayout.BeginHorizontal();

                    mMeta = (AnimateMeta)EditorGUILayout.ObjectField("Meta:", mMeta, typeof(AnimateMeta), false);

                    var prevClr = GUI.color;
                    GUI.color = Color.green;

                    if(GUILayout.Button("Create", GUILayout.Width(100f))) {
                        string path = UnityEditor.EditorUtility.SaveFilePanelInProject("Create AnimateMeta", "New Animate Meta", "asset", "Please enter a file name to save the animator data to");
                        if(!string.IsNullOrEmpty(path)) {
                            bool canCreate = true;

                            //check if path is the same as current
                            if(mMeta) {
                                string curPath = AssetDatabase.GetAssetPath(mMeta);
                                if(path == curPath)
                                    canCreate = false; //don't need to save this since it's already being used.
                            }

                            if(canCreate) {
                                //check if it already exists
                                if(!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path))) {
                                    //load the meta
                                    mMeta = AssetDatabase.LoadAssetAtPath<AnimateMeta>(path);
                                }
                                else {
                                    //create new meta
                                    mMeta = ScriptableObject.CreateInstance<AnimateMeta>();
                                    AssetDatabase.CreateAsset(mMeta, path);
                                    AssetDatabase.SaveAssets();
                                }
                            }
                        }
                    }

                    GUI.color = prevClr;

                    GUILayout.EndHorizontal();

                    EditorUtility.DrawSeparator();
                    //

                    if(GUILayout.Button(go ? "Add Component" : "Create Animate")) {
                        // create component
                        if(!go) {
                            go = new GameObject("Animate");
                            Undo.RegisterCreatedObjectUndo(go, "Create Animate");
                        }

                        AnimateEditControl nData = AnimEdit(Undo.AddComponent<Animate>(go));

                        nData.MetaSet(mMeta, false);
                        aData = nData;
                    }
                }

                if(aData == null) //still no data
                    return;
            }

            TimelineWindow.loadSkin(ref skin, ref cachedSkinName, position);
            LoadEditorTextures();

            #endregion
            #region window resize
            if(!oData.ignoreMinSize && (position.width < width_window_minimum)) {
                MessageBox("Window is too small! Animator requires a width of at least " + width_window_minimum + " pixels to function correctly.", MessageBoxType.Warning);
                GUILayout.BeginHorizontal();
                if(GUILayout.Button("Ignore (Not Recommended)")) {
                    oData.ignoreMinSize = true;
                    UnityEditor.EditorUtility.SetDirty(oData);
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
            EditorUtility.ResetDisplayControls();
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

            if(e.type == EventType.ScrollWheel) {
                scrollViewValue.y -= e.delta.y * 20;
                if(Mathf.Abs(e.delta.y) > 0) {
                    aData.zoom = Mathf.Clamp01(aData.zoom + Mathf.Clamp(Mathf.Abs(e.delta.y) * 0.04f, 0.01f, 0.1f) * Mathf.Sign(e.delta.y));
                }
            }
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
            UnityEngine.Object[] dragItems = null;
            Rect dropArea = new Rect(width_track, height_menu_bar + height_control_bar + 2f, position.width - 5f - 15f - width_track - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed), position.height - height_indicator_footer - height_menu_bar - height_control_bar);
            if(e.type == EventType.MouseDown) {
                dragPan = (e.button == 0);
            }

            if(e.type == EventType.MouseDrag && EditorWindow.mouseOverWindow == this) {
                isDragging = true;
            }
            else if(EditorWindow.mouseOverWindow == this && (e.type == EventType.DragUpdated || e.type == EventType.DragPerform) && DragAndDrop.objectReferences.Length > 0 && dropArea.Contains(currentMousePosition)) {
                ResetDragging();

                //Debug.Log("track: " + mouseMoveTrack + " frame: "+mouseMoveFrame);

                if(e.type == EventType.DragPerform) {
                    DragAndDrop.AcceptDrag();

                    UnityEngine.Object[] objs = DragAndDrop.objectReferences;

                    if(mouseMoveTrack != -1 && mouseMoveFrame > 0) {
                        if(!addSpriteKeysToTrack(objs, mouseMoveTrack, mouseMoveFrame))
                            dragItems = objs;
                        else {
                            Repaint();
                            return;
                        }
                    }
                    else if(mouseMoveTrack == -1) { //check to see if we can create a track
                        if(!addSpriteTrackWithKeyObjects(objs, 1))
                            dragItems = objs;
                        else {
                            Repaint();
                            return;
                        }
                    }
                    else
                        dragItems = objs;
                }

                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            }
            else if((dragType == (int)DragType.CursorZoom && EditorWindow.mouseOverWindow != this) || e.type == EventType.MouseUp || /*EditorWindow.mouseOverWindow!=this*/Event.current.rawType == EventType.MouseUp /*|| e.mousePosition.y < 0f*/) {
                if(isDragging) {
                    wasDragging = true;
                    isDragging = false;
                }
            }

            #endregion

            int numFrames = GetCurTakeNumFrames();

            #region keyboard events
            if(e.Equals(Event.KeyboardEvent("[enter]")) || e.Equals(Event.KeyboardEvent("return"))) {
                // apply renaming when pressing enter
                cancelTextEditting();
                if(isChangingTimeControl) isChangingTimeControl = false;
                if(isChangingFrameControl) isChangingFrameControl = false;
                Repaint();
                e.Use();
                return;
            }
            else if(GUIUtility.keyboardControl == 0 && !isTextEditting()) {
                bool done = false;

                if(e.Equals(Event.KeyboardEvent("delete")) || e.Equals(Event.KeyboardEvent("backspace"))) {
                    deleteSelectedKeys(false);
                    done = true;
                }
                else if(e.Equals(Event.KeyboardEvent("up"))) {
                    done = timelineSelectGroupOrTrackFromCurrent(-1);
                    if(done) {
                        TakeEditControl takeEdit = TakeEditCurrent();
                        takeEdit.contextSelection.Clear();
                        if(takeEdit.selectedTrack != -1)
                            contextSelectFrame(takeEdit.selectedFrame, takeEdit.selectedFrame);
                    }
                }
                else if(e.Equals(Event.KeyboardEvent("down"))) {
                    done = timelineSelectGroupOrTrackFromCurrent(1);
                    if(done) {
                        TakeEditControl takeEdit = TakeEditCurrent();
                        takeEdit.contextSelection.Clear();
                        if(takeEdit.selectedTrack != -1)
                            contextSelectFrame(takeEdit.selectedFrame, takeEdit.selectedFrame);
                    }
                }
                else if(e.Equals(Event.KeyboardEvent("left"))) {
                    TakeEditControl takeEdit = TakeEditCurrent();
                    if(takeEdit.selectedTrack != -1 && takeEdit.selectedFrame > 1) {
                        int f = takeEdit.selectedFrame - 1;
                        takeEdit.contextSelection.Clear();
                        contextSelectFrame(f, f);
                        timelineSelectFrame(takeEdit.selectedTrack, f);
                        done = true;
                    }
                }
                else if(e.Equals(Event.KeyboardEvent("#left"))) {
                    TakeEditControl takeEdit = TakeEditCurrent();
                    if(takeEdit.selectedTrack != -1 && takeEdit.selectedFrame > 1) {
                        int f = takeEdit.selectedFrame;
                        if(takeEdit.isFrameInContextSelection(f - 1) && !takeEdit.isFrameInContextSelection(f + 1))
                            takeEdit.contextSelectFrame(f, true);
                        else
                            contextSelectFrame(f - 1, f - 1);
                        timelineSelectFrame(takeEdit.selectedTrack, f - 1);
                        done = true;
                    }
                }
                else if(e.Equals(Event.KeyboardEvent("right"))) {
                    TakeEditControl takeEdit = TakeEditCurrent();
                    if(takeEdit.selectedTrack != -1 && takeEdit.selectedFrame < numFrames) {
                        int f = takeEdit.selectedFrame + 1;
                        takeEdit.contextSelection.Clear();
                        contextSelectFrame(f, f);
                        timelineSelectFrame(takeEdit.selectedTrack, f);
                        done = true;
                    }
                }
                else if(e.Equals(Event.KeyboardEvent("#right"))) {
                    TakeEditControl takeEdit = TakeEditCurrent();
                    if(takeEdit.selectedTrack != -1 && takeEdit.selectedFrame < numFrames) {
                        int f = takeEdit.selectedFrame;
                        if(takeEdit.isFrameInContextSelection(f + 1) && !takeEdit.isFrameInContextSelection(f - 1))
                            takeEdit.contextSelectFrame(f, true);
                        else
                            contextSelectFrame(f + 1, f + 1);
                        timelineSelectFrame(takeEdit.selectedTrack, f + 1);
                        done = true;
                    }
                }
                //cut/copy/paste/duplicate
                else if(e.type == EventType.ValidateCommand) {
                    if(e.commandName == "Copy") {
                        //are there keys?
                        if(TakeEditCurrent().contextSelectionTrackIds.Count > 1 || TakeEditCurrent().contextSelectionHasKeys(aData.currentTake)) {
                            contextCopyFrames();
                            done = true;
                        }
                    }
                    else if(e.commandName == "Paste") {
                        if(contextSelectionKeysBuffer.Count > 0) {
                            contextMenuFrame = TakeEditCurrent().selectedFrame;
                            contextPasteKeys(false);
                            done = true;
                        }
                    }
                    else if(e.commandName == "Cut") {
                        if(TakeEditCurrent().contextSelectionTrackIds.Count > 1 || TakeEditCurrent().contextSelectionHasKeys(aData.currentTake)) {
                            contextCutKeys();
                            done = true;
                        }
                    }
                    else if(e.commandName == "Duplicate") {
                        TakeEditControl takeEdit = TakeEditCurrent();
                        if(takeEdit.contextSelectionTrackIds.Count > 1 || takeEdit.contextSelectionHasKeys(aData.currentTake)) {
                            contextCopyFrames();
                            contextMenuFrame = takeEdit.contextSelection.Count > 0 ? takeEdit.contextSelection[takeEdit.contextSelection.Count - 1] + 1 : takeEdit.selectedFrame;
                            contextPasteKeys(true);
                            timelineSelectFrame(takeEdit.selectedTrack, contextMenuFrame);
                            done = true;
                        }
                    }
                }

                if(done) {
                    Repaint();
                    e.Use();
                    return;
                }
            }

            // check if control or shift are down
            isControlDown = e.control || e.command;
            isShiftDown = e.shift;
            if(e.type == EventType.KeyDown && e.keyCode == KeyCode.Space) isSpaceBarDown = true;
            else if(e.type == EventType.KeyUp && e.keyCode == KeyCode.Space) isSpaceBarDown = false;
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
#if UNITY_5
	        if(Cursor.visible != showCursor) {
	            Cursor.visible = showCursor;
	        }
#else
            if(Cursor.visible != showCursor) {
                Cursor.visible = showCursor;
            }
#endif
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
            if(aData.currentTake.selectedFrame > numFrames) {
                // select last frame
                timelineSelectFrame(TakeEditCurrent().selectedTrack, numFrames);
            }
            // get number of tracks in current take, use for tracks and keys, disabling zoom slider
            int trackCount = aData.currentTake.getTrackCount();
            #endregion
            #region menu bar
            GUIStyle styleLabelMenu = new GUIStyle(EditorStyles.toolbarButton);
            styleLabelMenu.normal.background = null;
            //GUI.color = new Color(190f/255f,190f/255f,190f/255f,1f);
            GUI.DrawTexture(new Rect(0f, 0f, position.width, height_menu_bar - 2f), EditorStyles.toolbar.normal.background);
            //GUI.color = Color.white;
            #region select name
            Rect rectSelectLabel;

            if(aData.gameObject) {
                GUI.color = Color.white;
                GUIContent selectLabel = new GUIContent(aData.gameObject.name);
                Vector2 selectLabelSize = EditorStyles.toolbarButton.CalcSize(selectLabel);
                rectSelectLabel = new Rect(margin, 0f, selectLabelSize.x, height_button_delete);

                if(GUI.Button(rectSelectLabel, selectLabel, EditorStyles.toolbarButton)) {
                    EditorGUIUtility.PingObject(aData.gameObject);
                    Selection.activeGameObject = aData.gameObject;
                }
                //GUI.color = Color.white;
            }
            else
                rectSelectLabel = new Rect();
            #endregion

            #region options button
            Rect rectBtnOptions = new Rect(rectSelectLabel.x + rectSelectLabel.width + margin, 0f, 60f, height_button_delete);
            if(GUI.Button(rectBtnOptions, "Options", EditorStyles.toolbarButton)) {
                EditorWindow windowOptions = ScriptableObject.CreateInstance<Options>();
                //windowOptions.Show();
                windowOptions.ShowUtility();
                //EditorWindow.GetWindow (typeof (Options));
            }
            if(rectBtnOptions.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) mouseOverElement = (int)ElementType.Button;
            #endregion
            #region refresh button
            bool doRefresh = false;
            Rect rectBtnCodeView = new Rect(rectBtnOptions.x + rectBtnOptions.width + margin, rectBtnOptions.y, 80f, rectBtnOptions.height);
            doRefresh = GUI.Button(rectBtnCodeView, "Refresh", EditorStyles.toolbarButton);
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
                aData.currentTake.name = GUI.TextField(rectRenameTake, aData.currentTake.name, EditorStyles.toolbarTextField);
                GUI.FocusControl("RenameTake");
            }
            else {
                // show popup
                int ntake = EditorGUI.Popup(rectTakePopup, aData.currentTakeInd, aData.GetTakeNames(), EditorStyles.toolbarPopup);
                if(aData.currentTakeInd != ntake) {
                    aData.currentTakeInd = ntake;

                    // take changed
                    // destroy camera fade
                    if(CameraFade.hasInstance()) CameraFade.destroyImmediateInstance();
                    // if not creating new take
                    if(aData.currentTakeInd < aData.takes.Count) {
                        // select current frame
                        timelineSelectFrame(TakeEditCurrent().selectedTrack, aData.currentTake.selectedFrame);
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
                if(!isRenamingTake) aData.RegisterTakesUndo("Rename Take", false);
                GUIUtility.keyboardControl = 0;
                cancelTextEditting(true);   // toggle isRenamingTake
            }
            if(rectBtnRenameTake.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) mouseOverElement = (int)ElementType.Button;
            #endregion
            #region delete take button
            Rect rectBtnDeleteTake = new Rect(rectBtnRenameTake.x + rectBtnRenameTake.width + margin, rectBtnRenameTake.y, width_button_delete, height_button_delete);
            if(GUI.Button(rectBtnDeleteTake, new GUIContent("", "Delete Take"),/*GUI.skin.GetStyle("ButtonImage")*/EditorStyles.toolbarButton)) {
                Take take = aData.currentTake;

                if((UnityEditor.EditorUtility.DisplayDialog("Delete Take", "Are you sure you want to delete take '" + take.name + "'?", "Delete", "Cancel"))) {
                    string label = "Delete Take: " + take.name;

                    aData.RegisterTakesUndo(label, false);

                    if(aData.takes.Count == 1) {                        
                        take = aData.currentTake;
                        take.RevertToDefault();
                        TakeEdit(take).Reset();
                    }
                    else {
                        take = aData.currentTake;
                        if(!string.IsNullOrEmpty(aData.defaultTakeName)) {
                            if(take.name == aData.defaultTakeName)
                                aData.defaultTakeName = "";
                            else {
                                aData.defaultTakeName = aData.defaultTakeName; //this will readjust index if necessary
                            }
                        }

                        int delTakeInd = aData.takes.IndexOf(take);
                        if(aData.currentTakeInd > 0 && delTakeInd <= aData.currentTakeInd)
                            aData.currentTakeInd--;

                        aData.takes.RemoveAt(delTakeInd);
                        TakeEditRemove(take);
                    }
                }
            }
            if(!GUI.enabled) GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0.25f);
            GUI.DrawTexture(new Rect(rectBtnDeleteTake.x + (rectBtnDeleteTake.height - 10f) / 2f, rectBtnDeleteTake.y + (rectBtnDeleteTake.width - 10f) / 2f - 2f, 10f, 10f), (getSkinTextureStyleState((GUI.enabled && rectBtnDeleteTake.Contains(e.mousePosition) ? "delete_hover" : "delete")).background));
            if(rectBtnDeleteTake.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) mouseOverElement = (int)ElementType.Button;
            if(GUI.color.a < 1f) GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 1f);
            #endregion
            #region Create/Duplicate Take
            if(aData.currentTakeInd == aData.takes.Count) {
                isRenamingTake = false;
                cancelTextEditting();

                string label = "New Take";

                aData.RegisterTakesUndo(label, false);

                aData.AddNewTake();

                aData.currentTakeInd = aData.takes.Count - 1;
            }
            else if(aData.currentTakeInd == aData.takes.Count + 1) {
                isRenamingTake = false;
                cancelTextEditting();

                string label = "New Duplicate Take";

                aData.RegisterTakesUndo(label, false);

                if(aData.prevTake != null) {
                    aData.DuplicateTake(aData.prevTake, true);
                }
                else {
                    aData.AddNewTake();
                }

                aData.currentTakeInd = aData.takes.Count - 1;
            }
            #endregion
            #region play on start button
            Rect rectBtnPlayOnStart = new Rect(rectBtnDeleteTake.x + rectBtnDeleteTake.width + margin, rectBtnDeleteTake.y, width_button_delete, height_button_delete);
            GUIStyle styleBtnPlayOnStart = new GUIStyle(/*GUI.skin.GetStyle("ButtonImage")*/EditorStyles.toolbarButton);
            if(aData.currentTakeIsPlayStart) {
                styleBtnPlayOnStart.normal.background = styleBtnPlayOnStart.onNormal.background;
                styleBtnPlayOnStart.hover.background = styleBtnPlayOnStart.onNormal.background;
            }
            if(GUI.Button(rectBtnPlayOnStart, new GUIContent(getSkinTextureStyleState("playonstart").background, "Play On Start"), styleBtnPlayOnStart)) {
                aData.RegisterUndo("Set Play On Start", false);
                if(!aData.currentTakeIsPlayStart) aData.defaultTakeName = aData.currentTake.name;
                else aData.defaultTakeName = "";
            }
            #endregion
            #region settings
            Rect rectLabelSettings = new Rect(rectBtnPlayOnStart.x + rectBtnPlayOnStart.width + margin, rectBtnPlayOnStart.y, 200f, rectLabelCurrentTake.height);

            if(compact) {
                rectLabelSettings.width = GUI.skin.label.CalcSize(new GUIContent("Settings")).x;
                GUI.Label(rectLabelSettings, "Settings", styleLabelMenu);
            }
            else {
                string strSettings = "Settings: " + numFrames + " Frames; " + aData.currentTake.frameRate + " Fps";
                rectLabelSettings.width = GUI.skin.label.CalcSize(new GUIContent(strSettings)).x;
                GUI.Label(rectLabelSettings, strSettings, styleLabelMenu);
            }
            Rect rectBtnModify = new Rect(rectLabelSettings.x + rectLabelSettings.width + margin, rectLabelSettings.y, 60f, rectBtnOptions.height);
            if(GUI.Button(rectBtnModify, "Modify", EditorStyles.toolbarButton)) {
                EditorWindow windowSettings = ScriptableObject.CreateInstance<TakeSettings>();
                windowSettings.ShowUtility();
            }
            if(rectBtnModify.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) mouseOverElement = (int)ElementType.Button;
            #endregion
            #region zoom slider
            if(trackCount <= 0) GUI.enabled = false;    // disable slider if there are no tracks
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
            if(trackCount <= 0 || TakeEditCurrent().selectedTrack == -1) GUI.enabled = false;   // disable key controls if there are no tracks
            #region select previous key
            Rect rectBtnPrevKey = new Rect(rectBtnAutoKey.x + rectBtnAutoKey.width + margin, rectBtnAutoKey.y, 30f, 15f);
            if(GUI.Button(rectBtnPrevKey, new GUIContent((getSkinTextureStyleState("prev_key").background), "Prev. Key"), GUI.skin.GetStyle("ButtonImage"))) {
                timelineSelectPrevKey();
            }
            if(rectBtnPrevKey.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) mouseOverElement = (int)ElementType.Button;
            #endregion
            #region insert key
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
                aData.RegisterTakesUndo("New Group", false);
                cancelTextEditting();
                TakeEditCurrent().addGroup(aData.currentTake);
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
            if(TakeEditCurrent().selectedGroup >= 0) GUI.enabled = false;
            if(TakeEditCurrent().selectedGroup >= 0 && (trackCount <= 0 || (TakeEditCurrent().contextSelectionTrackIds != null && TakeEditCurrent().contextSelectionTrackIds.Count <= 0))) GUI.enabled = false;
            else GUI.enabled = !isPlaying;
            if(!GUI.enabled) GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0.25f);
            GUIContent gcDeleteButton;
            string strTitleDeleteTrack = (TakeEditCurrent().contextSelectionTrackIds != null && TakeEditCurrent().contextSelectionTrackIds.Count > 1 ? "Tracks" : "Track");
            if(!GUI.enabled) gcDeleteButton = new GUIContent("");
            else gcDeleteButton = new GUIContent("", "Delete " + (TakeEditCurrent().contextSelectionTrackIds != null && TakeEditCurrent().contextSelectionTrackIds.Count > 0 ? strTitleDeleteTrack : "Group"));
            if(GUI.Button(rectBtnDeleteElement, gcDeleteButton, "label")) {
                cancelTextEditting();
                if(TakeEditCurrent().contextSelectionTrackIds.Count > 0) {
                    Take curTake = aData.currentTake;
                    Track track = TakeEdit(curTake).getSelectedTrack(curTake);
                    if(track != null) {
                        string strMsgDeleteTrack = (TakeEdit(curTake).contextSelectionTrackIds.Count > 1 ? "multiple tracks" : "track '" + track.name + "'");
                        if((UnityEditor.EditorUtility.DisplayDialog("Delete " + strTitleDeleteTrack, "Are you sure you want to delete " + strMsgDeleteTrack + "?", "Delete", "Cancel"))) {
                            isRenamingTrack = -1;

                            aData.RegisterTakesUndo("Delete Track", true);

                            // delete camera fade
                            if(TakeEdit(curTake).selectedTrack != -1 && track == curTake.cameraSwitcher && CameraFade.hasInstance() && CameraFade.isPreview()) {
                                CameraFade.destroyImmediateInstance();
                            }

                            foreach(int track_id in TakeEdit(curTake).contextSelectionTrackIds) {
                                curTake.deleteTrack(track_id, true);
                            }

                            TakeEdit(curTake).contextSelectionTrackIds = new List<int>();

                            // deselect track
                            TakeEdit(curTake).selectedTrack = -1;
                            // deselect group
                            TakeEdit(curTake).selectedGroup = 0;
                        }
                    }
                    else {
                        // deselect track/group
                        TakeEdit(curTake).selectedTrack = -1;
                        TakeEdit(curTake).selectedGroup = 0;
                    }
                }
                else {
                    bool delete = true;
                    bool deleteContents = false;
                    Take take = aData.currentTake;
                    Group grp = take.getGroup(TakeEdit(take).selectedGroup);

                    if(grp.elements.Count > 0) {
                        int choice = UnityEditor.EditorUtility.DisplayDialogComplex("Delete Contents?", "'" + grp.group_name + "' contains contents that can be deleted with the group.", "Delete Contents", "Keep Contents", "Cancel");
                        if(choice == 2) delete = false;
                        else if(choice == 0) deleteContents = true;
                        if(delete) {
                            if(deleteContents) {
                                aData.RegisterTakesUndo("Delete Group", false);

                                TakeEdit(take).deleteSelectedGroup(take, true);
                            }
                            else {
                                aData.RegisterTakesUndo("Delete Group", false);

                                TakeEdit(take).deleteSelectedGroup(take, false);
                            }
                        }
                    }
                    else { //no tracks inside group
                        aData.RegisterTakesUndo("Delete Group", false);
                        TakeEditCurrent().deleteSelectedGroup(aData.currentTake, false);
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
            GUI.enabled = (aData.currentTake.rootGroup != null && aData.currentTake.rootGroup.elements.Count > 0 ? !isPlaying : false);
            #endregion
            #region select first frame button
            Rect rectBtnSkipBack = new Rect(width_track + margin, margin, 32f, height_playback_controls - margin * 2);
            if(GUI.Button(rectBtnSkipBack, getSkinTextureStyleState("nav_skip_back").background, GUI.skin.GetStyle("ButtonImage"))) timelineSelectFrame(TakeEditCurrent().selectedTrack, 1);
            if(rectBtnSkipBack.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
                mouseOverElement = (int)ElementType.Button;
            }
            #endregion
            #region toggle play button
            // change label if already playing
            Texture playToggleTexture;
            if(isPlaying) playToggleTexture = getSkinTextureStyleState("nav_stop").background;
            else playToggleTexture = getSkinTextureStyleState("nav_play").background;
            GUI.enabled = (aData.currentTake.rootGroup != null && aData.currentTake.rootGroup.elements.Count > 0 ? true : false);
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
            GUI.enabled = (aData.currentTake.rootGroup != null && aData.currentTake.rootGroup.elements.Count > 0 ? !isPlaying : false);
            Rect rectSkipForward = new Rect(rectBtnTogglePlay.x + rectBtnTogglePlay.width + margin, rectBtnTogglePlay.y, rectBtnTogglePlay.width, rectBtnTogglePlay.height);
            if(GUI.Button(rectSkipForward, getSkinTextureStyleState("nav_skip_forward").background, GUI.skin.GetStyle("ButtonImage"))) timelineSelectFrame(TakeEditCurrent().selectedTrack, numFrames);
            if(rectSkipForward.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
                mouseOverElement = (int)ElementType.Button;
            }
            #endregion
            #region playback speed popup
            Rect rectPopupPlaybackSpeed = new Rect(rectSkipForward.x + rectSkipForward.width + margin, height_indicator_footer / 2f - 15f / 2f, width_playback_speed, rectBtnTogglePlay.height);
            TakeEditCurrent().playbackSpeedIndex = EditorGUI.Popup(rectPopupPlaybackSpeed, TakeEditCurrent().playbackSpeedIndex, playbackSpeed);
            #endregion
            #region scrub controls
            GUIStyle styleScrubControl = new GUIStyle(GUI.skin.label);
            string stringTime = frameToTime(aData.currentTake.selectedFrame, (float)aData.currentTake.frameRate).ToString("N2") + " s";
            string stringFrame = aData.currentTake.selectedFrame.ToString() + " fr";
            int timeFontSize = findWidthFontSize(width_scrub_control, styleScrubControl, new GUIContent(stringTime), 8, 11);
            int frameFontSize = findWidthFontSize(width_scrub_control, styleScrubControl, new GUIContent(stringFrame), 8, 11);
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
                if(!isPlaying && aData.currentTake.rootGroup != null && aData.currentTake.rootGroup.elements.Count > 0) EditorGUIUtility.AddCursorRect(rectFrameControl, MouseCursor.SlideArrow);
                // check for drag
                if(rectFrameControl.Contains(e.mousePosition) && aData.currentTake.rootGroup != null && aData.currentTake.rootGroup.elements.Count > 0) {
                    mouseOverElement = (int)ElementType.FrameScrub;
                }
            }
            else {
                // changing frame control
                selectFrame((int)Mathf.Clamp(EditorGUI.FloatField(new Rect(rectFrameControl.x, rectFrameControl.y + 2f, rectFrameControl.width, rectFrameControl.height), aData.currentTake.selectedFrame, GUI.skin.textField/*,styleButtonTimeControlEdit*/), 1, numFrames));
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
                if(!isPlaying && aData.currentTake.rootGroup != null && aData.currentTake.rootGroup.elements.Count > 0) EditorGUIUtility.AddCursorRect(rectTimeControl, MouseCursor.SlideArrow);
                // check for drag
                if(rectTimeControl.Contains(e.mousePosition) && aData.currentTake.rootGroup != null && aData.currentTake.rootGroup.elements.Count > 0) {
                    mouseOverElement = (int)ElementType.TimeScrub;
                }
            }
            else {
                // changing time control
                selectFrame(Mathf.Clamp(timeToFrame(EditorGUI.FloatField(new Rect(rectTimeControl.x, rectTimeControl.y + 2f, rectTimeControl.width, rectTimeControl.height), frameToTime(aData.currentTake.selectedFrame, (float)aData.currentTake.frameRate), GUI.skin.textField/*,styleButtonTimeControlEdit*/), (float)aData.currentTake.frameRate), 1, numFrames));
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
                            TakeEditCurrent().startFrame = Mathf.Clamp(++TakeEditCurrent().startFrame, 1, numFrames);
                            mouseXOverFrame = Mathf.Clamp((int)TakeEditCurrent().startFrame + (int)numFramesToRender, 1, numFrames);
                        }
                        else {
                            mouseXOverFrame = Mathf.Clamp((int)TakeEditCurrent().startFrame + (int)numFramesToRender, 1, numFrames);
                        }
                    }
                    // drag left, over tracks
                }
                else if(globalMousePosition.x <= width_track - 5f) {
                    difference = Mathf.CeilToInt((width_track - 5f) - globalMousePosition.x);
                    tickerSpeed = Mathf.Clamp(50 - Mathf.CeilToInt(difference / 1.5f), 1, 50);
                    if(dragType == (int)DragType.MoveSelection || dragType == (int)DragType.ContextSelection || dragType == (int)DragType.ResizeAction) {
                        if(ticker == 0) {
                            TakeEditCurrent().startFrame = Mathf.Clamp(--TakeEditCurrent().startFrame, 1, numFrames);
                            mouseXOverFrame = Mathf.Clamp((int)TakeEditCurrent().startFrame - 2, 1, numFrames);
                        }
                        else {
                            mouseXOverFrame = Mathf.Clamp((int)TakeEditCurrent().startFrame, 1, numFrames);
                        }
                    }
                }
            }
            Rect rectHScrollbar = new Rect(width_track + width_playback_controls, position.height - height_indicator_footer + 2f, position.width - (width_track + width_playback_controls) - (aData.isInspectorOpen ? width_inspector_open - 4f : width_inspector_closed) - 21f, height_indicator_footer - 2f);
            float frame_width_HScrollbar = ((rectHScrollbar.width - 44f - (aData.isInspectorOpen ? 4f : 0f)) / ((float)numFrames - 1f));
            Rect rectResizeHScrollbarLeft = new Rect(rectHScrollbar.x + 18f + frame_width_HScrollbar * (TakeEditCurrent().startFrame - 1f), rectHScrollbar.y + 2f, 15f, 15f);
            Rect rectResizeHScrollbarRight = new Rect(rectHScrollbar.x + 18f + frame_width_HScrollbar * (TakeEditCurrent().endFrame - 1f) - 3f, rectHScrollbar.y + 2f, 15f, 15f);
            Rect rectHScrollbarThumb = new Rect(rectResizeHScrollbarLeft.x, rectResizeHScrollbarLeft.y - 2f, rectResizeHScrollbarRight.x - rectResizeHScrollbarLeft.x + rectResizeHScrollbarRight.width, rectResizeHScrollbarLeft.height);
            if(!aData.isInspectorOpen) rectHScrollbar.width += 4f;
            // if number of frames fit on screen, disable horizontal scrollbar and set startframe to 1
            if(numFrames < numFramesToRender) {
                GUI.HorizontalScrollbar(rectHScrollbar, 1f, 1f, 1f, 1f);
                TakeEditCurrent().startFrame = 1;
            }
            else {
                bool hideResizeThumbs = false;
                if(rectHScrollbarThumb.width < rectResizeHScrollbarLeft.width * 2) {
                    hideResizeThumbs = true;
                    rectResizeHScrollbarLeft = new Rect(rectHScrollbarThumb.x - 4f, rectResizeHScrollbarLeft.y, rectHScrollbarThumb.width / 2f + 4f, rectResizeHScrollbarLeft.height);
                    rectResizeHScrollbarRight = new Rect(rectHScrollbarThumb.x + rectHScrollbarThumb.width - rectHScrollbarThumb.width / 2f, rectResizeHScrollbarRight.y, rectResizeHScrollbarLeft.width, rectResizeHScrollbarRight.height);
                }
                mouseXOverHScrollbarFrame = Mathf.CeilToInt(numFrames * ((e.mousePosition.x - rectHScrollbar.x - GUI.skin.horizontalScrollbarLeftButton.fixedWidth) / (rectHScrollbar.width - GUI.skin.horizontalScrollbarLeftButton.fixedWidth * 2)));
                if(!rectResizeHScrollbarLeft.Contains(e.mousePosition) && !rectResizeHScrollbarRight.Contains(e.mousePosition) && EditorWindow.mouseOverWindow == this && dragType != (int)DragType.ResizeHScrollbarLeft && dragType != (int)DragType.ResizeHScrollbarRight && mouseOverElement != (int)ElementType.ResizeHScrollbarLeft && mouseOverElement != (int)ElementType.ResizeHScrollbarRight)
                    TakeEditCurrent().startFrame = Mathf.Clamp((int)GUI.HorizontalScrollbar(rectHScrollbar, (float)TakeEditCurrent().startFrame, (int)numFramesToRender - 1f, 1f, numFrames), 1, numFrames);
                else Mathf.Clamp(GUI.HorizontalScrollbar(rectHScrollbar, (float)TakeEditCurrent().startFrame, (int)numFramesToRender - 1f, 1f, numFrames), 1f, numFrames);
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
            TakeEditCurrent().endFrame = TakeEditCurrent().startFrame + (int)numFramesToRender - 1;
            if(rectHScrollbar.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
                mouseOverElement = (int)ElementType.Other;
            }

            #endregion
            #region inspector toggle button
            Rect rectPropertiesButton = new Rect(position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed), height_menu_bar + height_control_bar + 2f, 36, position.height);
            Rect rectPropertiesTop = new Rect(position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed), height_menu_bar - 2f, 251, 24);
            GUI.color = getSkinTextureStyleState("properties_bg").textColor;
            GUI.DrawTexture(rectPropertiesButton, getSkinTextureStyleState("properties_bg").background);
            GUI.DrawTexture(rectPropertiesTop, getSkinTextureStyleState("properties_top").background);
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
            int firstMarkedKey = (int)TakeEditCurrent().startFrame;
            if(firstMarkedKey % key_dist != 0 && firstMarkedKey != 1) {
                firstMarkedKey += key_dist - firstMarkedKey % key_dist;
            }
            float lastNumberX = -1f;
            for(int i = firstMarkedKey; i <= (int)TakeEditCurrent().endFrame; i += key_dist) {
                float newKeyNumberX = width_track + current_width_frame * (i - (int)TakeEditCurrent().startFrame) - 1f;
                string key_number;
                if(oData.time_numbering) key_number = frameToTime(i, (float)aData.currentTake.frameRate).ToString("N2");
                else key_number = i.ToString();
                Rect rectKeyNumber = new Rect(newKeyNumberX, height_menu_bar, GUI.skin.label.CalcSize(new GUIContent(key_number)).x, height_control_bar);
                bool didCutLabel = false;
                if(rectKeyNumber.x + rectKeyNumber.width >= position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed) - 20f) {
                    rectKeyNumber.width = position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed) - 20f - rectKeyNumber.x;
                    didCutLabel = true;
                }
                if(!(didCutLabel && TakeEditCurrent().endFrame == numFrames)) {
                    if(rectKeyNumber.x > lastNumberX + 3f) {
                        GUI.Label(rectKeyNumber, key_number);
                        lastNumberX = rectKeyNumber.x + GUI.skin.label.CalcSize(new GUIContent(key_number)).x;
                    }
                }
                if(i == 1) i--;
            }

            #endregion
            #region main scrollview
            height_all_tracks = aData.currentTake.getElementsHeight(0, height_track, height_track_foldin, height_group);
            float height_scrollview = position.height - (height_control_bar + height_menu_bar) - height_indicator_footer;
            // check if mouse is beyond tracks and dragging group element
            difference = 0;
            // drag up
            if(dragType == (int)DragType.GroupElement && globalMousePosition.y <= height_control_bar + height_menu_bar + 2f) {
                difference = Mathf.CeilToInt((height_control_bar + height_menu_bar + 2f) - globalMousePosition.y);
                scrollAmountVertical = -difference; // set scroll amount
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
            float track_y = 0f;     // the next track's y position
                                    // tracks vertical start
            for(int i = 0; i < aData.currentTake.rootGroup.elements.Count; i++) {
                if(track_y > scrollViewBounds.y) break; // if start y is beyond max y
                int id = aData.currentTake.rootGroup.elements[i];
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
            mouseXOverFrame = (int)TakeEditCurrent().startFrame + Mathf.CeilToInt((e.mousePosition.x - width_track) / current_width_frame) - 1;
            if(dragType == (int)DragType.CursorHand && justStartedHandGrab) {
                startScrubFrame = mouseXOverFrame;
                justStartedHandGrab = false;
            }
            track_y = 0f;   // reset track y
            showFramesForGroup(0, ref track_y, e, birdseye, scrollViewBounds);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUI.EndScrollView();
            #endregion
            #region inspector
            Texture inspectorArrow;

            if(aData.isInspectorOpen) {
                Rect rectInspector = new Rect(position.width - width_inspector_open - 4f + width_inspector_closed, +height_menu_bar + height_control_bar + 6f, width_inspector_open, position.height - height_menu_bar - height_control_bar);
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
                showInspectorPropertiesFor(rectInspector, TakeEditCurrent().selectedTrack, aData.currentTake.selectedFrame, e);
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

            GUI.DrawTexture(new Rect(position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed) + 9f, 47f, inspectorArrow.width, inspectorArrow.height), inspectorArrow);
            GUI.DrawTexture(new Rect(position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed) + 6f, 65f, texProperties.width, texProperties.height), texProperties);
            #endregion
            #region indicator
            if((oData.showFramesForCollapsedTracks || isAnyTrackFoldedOut) && (trackCount > 0)) drawIndicator(aData.currentTake.selectedFrame);
            #endregion
            #region horizontal scrollbar tooltip
            string strHScrollbarLeftTooltip = (oData.time_numbering ? frameToTime((int)TakeEditCurrent().startFrame, (float)aData.currentTake.frameRate).ToString("N2") : TakeEditCurrent().startFrame.ToString());
            string strHScrollbarRightTooltip = (oData.time_numbering ? frameToTime((int)TakeEditCurrent().endFrame, (float)aData.currentTake.frameRate).ToString("N2") : TakeEditCurrent().endFrame.ToString());
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
                if(TakeEditCurrent().contextSelectionTrackIds != null && TakeEditCurrent().contextSelectionTrackIds.Count > 0)
                    TakeEditCurrent().contextSelectionTrackIds = new List<int>();
                if(TakeEditCurrent().contextSelection != null && TakeEditCurrent().contextSelection.Count > 0)
                    TakeEditCurrent().contextSelection = new List<int>();
                if(TakeEditCurrent().ghostSelection != null && TakeEditCurrent().ghostSelection.Count > 0)
                    TakeEditCurrent().ghostSelection = new List<int>();

                if(objects_window.Count > 0) objects_window = new List<GameObject>();
                if(isRenamingGroup < 0) isRenamingGroup = 0;
                if(isRenamingTake) {
                    aData.MakeTakeNameUnique(aData.currentTake);
                    isRenamingTake = false;
                }
                if(isRenamingTrack != -1) isRenamingTrack = -1;
                if(isChangingTimeControl) isChangingTimeControl = false;
                if(isChangingFrameControl) isChangingFrameControl = false;
                // if clicked on inspector, do nothing
                if(e.mousePosition.y > (float)height_menu_bar + (float)height_control_bar && e.mousePosition.x > position.width - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed)) return;
                if(TakeEditCurrent().selectedGroup != 0) timelineSelectGroup(0);
                if(TakeEditCurrent().selectedTrack != -1) TakeEditCurrent().selectedTrack = -1;

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
                    dragElementName = aData.currentTake.getGroup((int)draggingGroupElement.x).group_name;
                    dragElementIcon = tex_icon_group_closed;
                    dragElementIconWidth = 16f;
                }
                else if(draggingGroupElementType == (int)ElementType.Track) {
                    Track dragTrack = aData.currentTake.getTrack((int)draggingGroupElement.y);
                    if(dragTrack != null) {
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
            if(aData.currentTake.rootGroup == null || aData.currentTake.rootGroup.elements.Count <= 0) {

                if(aData.isInspectorOpen) aData.isInspectorOpen = false;
                float width_helpbox = position.width - width_inspector_closed - 28f - width_track;
                EditorGUI.HelpBox(new Rect(width_track + 5f, height_menu_bar + height_control_bar + 7f, width_helpbox, 50f), "Click the track icon below or drag a GameObject here to add a new track.", MessageType.Info);
                //GUI.DrawTexture(new Rect(width_track+75f,height_menu_bar+height_control_bar+19f-(width_helpbox<=355.5f ? 6f: 0f),15f,15f),tex_icon_track);
            }
            #endregion
            #region quick add
            /*GUIStyle styleObjectField = new GUIStyle(EditorStyles.objectField);
	        GUIStyle styleObjectFieldThumb = new GUIStyle(EditorStyles.objectFieldThumb);
	        EditorStyles.objectField.normal.textColor = new Color(0f, 0f, 0f, 0f);
	        EditorStyles.objectField.contentOffset = new Vector2(width_track * -1 - 300f, 0f);
	        EditorStyles.objectField.normal.background = null;
	        EditorStyles.objectField.onNormal.background = null;*/

            GameObject tempGO = null;
            //tempGO = (GameObject)EditorGUI.ObjectField(new Rect(width_track, height_menu_bar + height_control_bar + 2f, position.width - 5f - 15f - width_track - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed), position.height - height_indicator_footer - height_menu_bar - height_control_bar), "", tempGO, typeof(GameObject), true);
            if(dragItems != null && dragItems.Length > 0) {
                tempGO = dragItems[0] as GameObject;
            }

            if(tempGO != null) {
                objects_window = new List<GameObject>();
                objects_window.Add(tempGO);
                buildAddTrackMenu_Drag();
                menu_drag.ShowAsContext();
            }
            /*EditorStyles.objectField.contentOffset = styleObjectField.contentOffset;
	        EditorStyles.objectField.normal = styleObjectField.normal;
	        EditorStyles.objectField.onNormal = styleObjectField.onNormal;
	        EditorStyles.objectFieldThumb.normal = styleObjectFieldThumb.normal;*/
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
            if(isDragging && dragType != (int)DragType.None && e.type != EventType.Layout && e.type != EventType.Repaint)
                e.Use();
            else if(e.type == EventType.MouseMove)
                Repaint();

            if(doRefresh) {
                ClearKeysBuffer();
                contextSelectionTracksBuffer.Clear();
                cachedContextSelection.Clear();

                GameObject go = _aData.gameObject;
                _aData = null;
                Selection.activeGameObject = go;
            }

            if(e.type == EventType.MouseMove || e.type == EventType.DragUpdated) {
                mouseMoveTrack = mouseOverTrack;
                mouseMoveFrame = mouseOverFrame;
            }
        }
        void ReloadOtherWindows() {
            if(Options.window) Options.window.reloadAnimate();
            if(TakeSettings.window) TakeSettings.window.reloadAnimate();
            if(EasePicker.window) EasePicker.window.reloadAnimate();
            if(PropertySelect.window) PropertySelect.window.reloadAnimate();
            if(TakeExport.window) TakeExport.window.reloadAnimate();
            if(MaterialEditor.window) MaterialEditor.window.reloadAnimate();
        }

        void OnPlayMode(PlayModeStateChange state) {
            bool justHitPlay = state == PlayModeStateChange.EnteredPlayMode;
            // entered playmode
            if(justHitPlay) {
                if(dragType == (int)DragType.TimeScrub || dragType == (int)DragType.FrameScrub) dragType = (int)DragType.None;
                // destroy camerafade
                if(CameraFade.hasInstance() && CameraFade.isPreview()) {
                    CameraFade.destroyImmediateInstance();
                }
            }
        }

        #endregion

        #region Functions

        #region Static
        
        public static void MessageBox(string message, MessageBoxType type) {

            MessageType messageType;
            if(type == MessageBoxType.Error) messageType = MessageType.Error;
            else if(type == MessageBoxType.Warning) messageType = MessageType.Warning;
            else messageType = MessageType.Info;

            EditorGUILayout.HelpBox(message, messageType);
        }
        public static void loadSkin(ref GUISkin _skin, ref string skinName, Rect position) {
            string newSkinName = EditorGUIUtility.isProSkin ? "am_skin_dark" : "am_skin_light";
            if(_skin == null || newSkinName != skinName) {
                _skin = (GUISkin)EditorResource.LoadSkin(newSkinName); /*global_skin*/
                skinName = newSkinName;
                TransitionPicker.texLoaded = false;
                texLoaded = false;
            }

            GUI.skin = _skin;
            GUI.color = GUI.skin.window.normal.textColor;
            GUI.DrawTexture(new Rect(0f, 0f, position.width, position.height), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;

        }
        public static GUIStyleState getSkinTextureStyleState(string name) {
            if(name == "properties_bg") return GUI.skin.GetStyle("Textures_1").normal;
            if(name == "properties_top") return GUI.skin.GetStyle("Textures_3").active;
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
                arrayFieldFoldout = new Dictionary<string, bool>(); // reset array foldout dictionary
                return;
            }
            cachedParameterInfos = cachedMethodInfo[indexMethodInfo].GetParameters();
        }

        #endregion

        #region Show/Draw

        bool showGroupElement(int id, int group_lvl, ref float track_y, ref bool isAnyTrackFoldedOut, ref float height_group_elements, Event e, Vector2 scrollViewBounds) {
            // returns true if mouse over track
            if(id >= 0) {
                Track _track = aData.currentTake.getTrack(id);
                return _track != null ? showTrack(_track, id, group_lvl, ref track_y, ref isAnyTrackFoldedOut, ref height_group_elements, e, scrollViewBounds) : false;
            }
            else {
                return showGroup(id, group_lvl, ref track_y, ref isAnyTrackFoldedOut, ref height_group_elements, e, scrollViewBounds);
            }
        }
        bool showGroup(int id, int group_lvl, ref float track_y, ref bool isAnyTrackFoldedOut, ref float height_group_elements, Event e, Vector2 scrollViewBounds) {
            if(track_y > scrollViewBounds.y) return false;  // if beyond lower bound, return
            bool isBeyondUpperBound = (track_y + height_group < scrollViewBounds.x);
            // show group
            float group_x = width_subtrack_space * group_lvl;
            group_lvl++;    // increment group_lvl for sub-elements
            Rect rectGroup = new Rect(group_x, track_y, width_track - group_x, height_group);
            Group grp = aData.currentTake.getGroup(id);
            bool isGroupSelected = (TakeEditCurrent().selectedGroup == id && TakeEditCurrent().selectedTrack == -1);
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
                    if(((grp.foldout && TakeEditCurrent().contextSelectionTrackIds.Count > 1) || (!grp.foldout && TakeEditCurrent().contextSelectionTrackIds.Count > 0)) && TakeEditCurrent().isGroupSelected(aData.currentTake, id, ref numTracks) && numTracks > 0) {
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
                        GUI.Label(new Rect(33f, 0f, rectGroup.width, rectGroup.height), aData.currentTake.getGroup(id).group_name);
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
        bool showTrack(Track _track, int t, int group_level, ref float track_y, ref bool isAnyTrackFoldedOut, ref float height_group_elements, Event e, Vector2 scrollViewBounds) {
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
            bool isTrackSelected = TakeEditCurrent().selectedTrack == t;
            bool isTrackContextSelected = TakeEditCurrent().contextSelectionTrackIds.Contains(t);
            Rect rectTrack;
            string strStyle;
            if(isTrackSelected) {
                if(TakeEditCurrent().contextSelectionTrackIds.Count <= 1)
                    strStyle = "GroupElementActive";
                else
                    strStyle = "GroupElementSelectedActive";
            }
            else if(isTrackContextSelected) strStyle = "GroupElementSelected";
            else strStyle = "GroupElementNormal";

            rectTrack = new Rect(track_x, track_y, width_track - track_x, height_track_foldin);
            // renaming track
            if(isRenamingTrack != t) {
                Rect rectTrackFoldin = new Rect(rectTrack.x, rectTrack.y, rectTrack.width, rectTrack.height);   // used to toggle track foldout
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
                        mouseOverGroupElement = new Vector2((inGroup ? aData.currentTake.getTrackGroup(t) : 0), t);
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
                        mouseOverGroupElement = new Vector2((inGroup ? aData.currentTake.getTrackGroup(t) : 0), t);
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
                    if((_track is PropertyTrack || _track is MaterialTrack) && (trackType == "Not Set"))
                        rectTrackType.width -= 48f;
                    GUI.Label(rectTrackType, trackType);
                    // if property or material track, show set property button
                    if(_track is PropertyTrack || _track is MaterialTrack) {
                        if(!_track.GetTarget(aData.target)) GUI.enabled = false;
                        GUIStyle styleButtonSet = new GUIStyle(GUI.skin.button);
                        styleButtonSet.clipping = TextClipping.Overflow;
                        if(GUI.Button(new Rect(width_track - 48f - width_subtrack_space * group_level - 4f, height_track - 38f, 48f, 15f), "Set", styleButtonSet)) {
                            if(_track is PropertyTrack) { // show property select window
                                PropertySelect.setValues((_track as PropertyTrack));
                                EditorWindow.GetWindow(typeof(PropertySelect));
                            }
                            else if(_track is MaterialTrack)
                                MaterialEditor.Open(_track as MaterialTrack);
                        }
                        GUI.enabled = !isPlaying;
                    }
                    else if(_track is RotationEulerTrack) { //show axis selection
                        var rTrack = (RotationEulerTrack)_track;
                        var nAxis = (AxisFlags)EditorGUI.EnumFlagsField(new Rect(width_track - 48f - width_subtrack_space * group_level - 4f, height_track - 38f, 48f, 15f), rTrack.axis);
                        if(rTrack.axis != nAxis) {
                            aData.RegisterTakesUndo("Change Rotation Euler Axis", false);
                            rTrack.axis = nAxis;
                        }
                    }
                    else if(_track is ScaleTrack) { //show axis selection
                        var sTrack = (ScaleTrack)_track;
                        var nAxis = (AxisFlags)EditorGUI.EnumFlagsField(new Rect(width_track - 48f - width_subtrack_space * group_level - 4f, height_track - 38f, 48f, 15f), sTrack.axis);
                        if(sTrack.axis != nAxis) {
                            aData.RegisterTakesUndo("Change Scale Axis", false);
                            sTrack.axis = nAxis;
                        }
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
            if(track_y > scrollViewBounds.y) return;    // if start y is beyond max y
            Group grp = aData.currentTake.getGroup(group_id);
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
                        if(track_y > scrollViewBounds.y) return;    // if start y is beyond max y
                        showFramesForGroup(id, ref track_y, e, birdseye, scrollViewBounds);
                    }
                    else {
                        if(track_y > scrollViewBounds.y) return;    // if start y is beyond max y
                        Track track = aData.currentTake.getTrack(id);
                        if(track != null)
                            showFrames(track, ref track_y, e, birdseye, scrollViewBounds);
                    }
                }
            }
        }
        void drawBox(int cached_action_startFrame, int cached_action_endFrame, int _startFrame, int _endFrame, Rect rectTimelineActions, float birdsEyeWidth, Texture texBox) {
            if(cached_action_startFrame > 0 && cached_action_endFrame > 0 && cached_action_endFrame > _startFrame && cached_action_startFrame < _endFrame) {
                if(cached_action_startFrame <= _startFrame) {
                    rectTimelineActions.x = 0f;
                }
                else {
                    rectTimelineActions.x = (cached_action_startFrame - _startFrame) * current_width_frame;
                }
                if(cached_action_endFrame >= _endFrame) {
                    rectTimelineActions.width = birdsEyeWidth;//rectFramesBirdsEye.width;
                }
                else {
                    rectTimelineActions.width = (cached_action_endFrame - (_startFrame >= cached_action_startFrame ? _startFrame : cached_action_startFrame) + 1) * current_width_frame;
                }
                // draw timeline action texture
                if(rectTimelineActions.width > 0f) GUI.DrawTextureWithTexCoords(rectTimelineActions, texBox, new Rect(0, 0, rectTimelineActions.width / 32f, rectTimelineActions.height / 64f));
            }
        }
        void showFrames(Track _track, ref float track_y, Event e, bool birdseye, Vector2 scrollViewBounds) {
            //int 
            //string tooltip = "";
            int t = _track.id;
            Take curTake = aData.currentTake;
            TakeEditControl curTakeEdit = TakeEdit(curTake);
            int selectedTrack = curTakeEdit.selectedTrack;
            // frames start
            if(!_track.foldout && !oData.showFramesForCollapsedTracks) {
                track_y += height_track_foldin;
                return;
            }
            int numFrames = GetCurTakeNumFrames();
            float fNumFrames = (numFrames < numFramesToRender ? numFrames : numFramesToRender);
            Rect rectFrames = new Rect(width_track, track_y, current_width_frame * fNumFrames, height_track);
            if(!_track.foldout) track_y += height_track_foldin;
            else track_y += height_track;
            if(track_y < scrollViewBounds.x) return; // if end y is before min y
            float _current_height_frame = (_track.foldout ? current_height_frame : height_track_foldin);
            #region frames
            GUI.BeginGroup(rectFrames);
            // draw frames
            bool selected;
            bool ghost = isDragging && TakeEditCurrent().hasGhostSelection();
            bool isTrackSelected = t == selectedTrack || TakeEditCurrent().contextSelectionTrackIds.Contains(t);
            Rect rectFramesBirdsEye = new Rect(0f, 0f, rectFrames.width, _current_height_frame);
            float width_birdseye = current_height_frame * 0.5f;
            if(birdseye) {
                GUI.color = colBirdsEyeFrames;
                GUI.DrawTexture(rectFramesBirdsEye, EditorGUIUtility.whiteTexture);
            }
            else {
                texFrSet.wrapMode = TextureWrapMode.Repeat;
                float startPos = curTakeEdit.startFrame % 5f;
                GUI.DrawTextureWithTexCoords(rectFramesBirdsEye, texFrSet, new Rect(startPos / 5f, 0f, fNumFrames / 5f, 1f));
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
                    if(birdseye && TakeEditCurrent().contextSelection.Count == 2 && TakeEditCurrent().contextSelection[0] == TakeEditCurrent().contextSelection[1] && _track.hasKeyOnFrame(TakeEditCurrent().contextSelection[0])) {
                        GUI.color = new Color(0f, 0f, 1f, .5f);
                        GUI.DrawTexture(new Rect(current_width_frame * (TakeEditCurrent().ghostSelection[0] - curTakeEdit.startFrame) - width_birdseye / 2f + current_width_frame / 2f, 0f, width_birdseye, _current_height_frame), texKeyBirdsEye);
                        GUI.color = Color.white;
                    }
                    else if(TakeEditCurrent().ghostSelection != null) {
                        // birds eye ghost selection
                        GUI.color = new Color(156f / 255f, 162f / 255f, 216f / 255f, .9f);
                        for(int i = 0; i < TakeEditCurrent().ghostSelection.Count; i += 2) {
                            int contextFrameStart = TakeEditCurrent().ghostSelection[i];
                            int contextFrameEnd = TakeEditCurrent().ghostSelection[i + 1];
                            if(contextFrameStart < (int)curTakeEdit.startFrame) contextFrameStart = (int)curTakeEdit.startFrame;
                            if(contextFrameEnd > (int)curTakeEdit.endFrame) contextFrameEnd = (int)curTakeEdit.endFrame;
                            float contextWidth = (contextFrameEnd - contextFrameStart + 1) * current_width_frame;
                            GUI.DrawTexture(new Rect(rectFramesBirdsEye.x + (contextFrameStart - curTakeEdit.startFrame) * current_width_frame, rectFramesBirdsEye.y + 1f, contextWidth, rectFramesBirdsEye.height - 2f), EditorGUIUtility.whiteTexture);
                        }
                        // draw birds eye ghost key frames
                        GUI.color = new Color(0f, 0f, 1f, .5f);
                        foreach(int _key_frame in TakeEditCurrent().getKeyFramesInGhostSelection(curTake, (int)curTakeEdit.startFrame, (int)curTakeEdit.endFrame, t)) {
                            if(birdseye)
                                GUI.DrawTexture(new Rect(current_width_frame * (_key_frame - curTakeEdit.startFrame) - width_birdseye / 2f + current_width_frame / 2f, 0f, width_birdseye, _current_height_frame), texKeyBirdsEye);
                            else {
                                Rect rectFrame = new Rect(current_width_frame * (_key_frame - curTakeEdit.startFrame), 0f, current_width_frame, _current_height_frame);
                                GUI.DrawTexture(new Rect(rectFrame.x + 2f, rectFrame.y + rectFrame.height - (rectFrame.width - 4f) - 2f, rectFrame.width - 4f, rectFrame.width - 4f), texFrKey);
                            }
                        }
                        GUI.color = Color.white;
                    }
                }
                else if(TakeEditCurrent().contextSelection.Count > 0 && /*do not show single frame selection in birdseye*/!(birdseye && TakeEditCurrent().contextSelection.Count == 2 && TakeEditCurrent().contextSelection[0] == TakeEditCurrent().contextSelection[1])) {
                    // birds eye context selection
                    for(int i = 0; i < TakeEditCurrent().contextSelection.Count; i += 2) {
                        //GUI.color = new Color(121f/255f,127f/255f,184f/255f,(birdseye ? 1f : .9f));
                        GUI.color = new Color(86f / 255f, 95f / 255f, 178f / 255f, .8f);
                        int contextFrameStart = TakeEditCurrent().contextSelection[i];
                        int contextFrameEnd = TakeEditCurrent().contextSelection[i + 1];
                        if(contextFrameStart < (int)curTakeEdit.startFrame) contextFrameStart = (int)curTakeEdit.startFrame;
                        if(contextFrameEnd > (int)curTakeEdit.endFrame) contextFrameEnd = (int)curTakeEdit.endFrame;
                        float contextWidth = (contextFrameEnd - contextFrameStart + 1) * current_width_frame;
                        Rect rectContextSelection = new Rect(rectFramesBirdsEye.x + (contextFrameStart - curTakeEdit.startFrame) * current_width_frame, rectFramesBirdsEye.y + 1f, contextWidth, rectFramesBirdsEye.height - 2f);
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
                foreach(var key in _track.keys) {
                    if(key == null) continue;

                    selected = ((isTrackSelected) && TakeEditCurrent().isFrameSelected(key.frame));
                    //_track.sortKeys();
                    if(key.frame < curTakeEdit.startFrame) continue;
                    if(key.frame > curTakeEdit.endFrame) break;
                    Rect rectKeyBirdsEye = new Rect(current_width_frame * (key.frame - curTakeEdit.startFrame) - width_birdseye / 2f + current_width_frame / 2f, 0f, width_birdseye, _current_height_frame);
                    if(selected) GUI.color = Color.blue;
                    GUI.DrawTexture(rectKeyBirdsEye, texKeyBirdsEye);
                    GUI.color = Color.white;
                    birdseyeKeyFrames.Add(key.frame);
                    birdseyeKeyRects.Add(rectKeyBirdsEye);
                }

                // birds eye buttons
                if(birdseyeKeyFrames.Count > 0) {
                    for(int i = birdseyeKeyFrames.Count - 1; i >= 0; i--) {
                        selected = ((isTrackSelected) && TakeEditCurrent().isFrameSelected(birdseyeKeyFrames[i]));
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
                foreach(var key in _track.keys) {
                    if(key == null) continue;

                    //_track.sortKeys();
                    if(key.frame < curTakeEdit.startFrame) continue;
                    if(key.frame > curTakeEdit.endFrame) break;
                    Rect rectFrame = new Rect(current_width_frame * (key.frame - curTakeEdit.startFrame), 0f, current_width_frame, _current_height_frame);
                    GUI.DrawTexture(new Rect(rectFrame.x + 2f, rectFrame.y + rectFrame.height - (rectFrame.width - 4f) - 2f, rectFrame.width - 4f, rectFrame.width - 4f), texFrKey);
                }
            }
            // click on empty frames
            if(GUI.Button(rectFramesBirdsEye, "", "label") && dragType == (int)DragType.None) {
                int prevFrame = curTake.selectedFrame;
                bool clickedOnBirdsEyeKey = false;
                for(int i = birdseyeKeyFrames.Count - 1; i >= 0; i--) {
                    if(birdseyeKeyFrames[i] > (int)curTakeEdit.endFrame) continue;
                    if(birdseyeKeyFrames[i] < (int)curTakeEdit.startFrame) break;
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
                    int _frame_num_birdseye = (int)curTakeEdit.startFrame + Mathf.CeilToInt(e.mousePosition.x / current_width_frame) - 1;
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
                mouseOverSelectedFrame = ((isTrackSelected) && TakeEditCurrent().isFrameSelected(mouseXOverFrame));
            }
            #endregion
            if(!oData.disableTimelineActions && _track.foldout) {
                #region timeline actions
                // draw each action with seperate textures and buttons for these tracks
                bool drawEachAction = _track is UnityAnimationTrack || _track is AudioTrack;
                int _startFrame = (int)curTakeEdit.startFrame;
                int _endFrame = (int)(_startFrame + fNumFrames - 1);
                int action_startFrame, action_endFrame, renderFrameStart, renderFrameEnd;
                int cached_action_startFrame = -1, cached_action_endFrame = -1;
                Texture texBox = texBoxBorder;
                #region group textures / buttons (performance increase)
                Rect rectTimelineActions = new Rect(0f, _current_height_frame, 0f, height_track - current_height_frame);    // used to group textures into one draw call
                if(!drawEachAction) {
                    if(_track.keys.Count > 0) {
                        if(_track is EventTrack) {
                            texBox = texBoxDarkBlue;

                            for(int i = 0; i < _track.keys.Count; i++) {
                                var key = _track.keys[i];

                                if(key.frame + 1 < _startFrame) continue;
                                else if(key.frame > _endFrame) break;

                                drawBox(key.frame, key.frame, _startFrame, _endFrame, rectTimelineActions, rectFramesBirdsEye.width, texBox);
                            }
                        }
                        else if(_track is PropertyTrack || _track is MaterialTrack) {
                            // event track, from first action start frame to end frame
                            cached_action_startFrame = _track.keys[0].getStartFrame();
                            cached_action_endFrame = _track.keys[_track.keys.Count - 1].getStartFrame();
                            texBox = texBoxLightBlue;
                            drawBox(cached_action_startFrame, cached_action_endFrame, _startFrame, _endFrame, rectTimelineActions, rectFramesBirdsEye.width, texBox);
                        }
                        else if(_track is GOSetActiveTrack) {
                            texBox = texBoxLightBlue;

                            int lastStartInd = -1;

                            for(int i = 0; i < _track.keys.Count; i++) {
                                var key = _track.keys[i] as GOSetActiveKey;

                                bool draw = false;

                                if(key.setActive) {
                                    if(lastStartInd == -1)
                                        lastStartInd = i;

                                    if(i == _track.keys.Count - 1)
                                        draw = true;
                                }
                                else if(lastStartInd != -1 || (i == 0 && (_track as GOSetActiveTrack).startActive)) {
                                    draw = true;
                                }

                                if(draw) {
                                    if(i == 0 && !key.setActive) {
                                        cached_action_startFrame = _startFrame;
                                        cached_action_endFrame = _track.keys[i].getStartFrame() - 1;
                                    }
                                    else {
                                        cached_action_startFrame = _track.keys[lastStartInd].getStartFrame();
                                        cached_action_endFrame = i == _track.keys.Count - 1 && key.setActive ? _endFrame : _track.keys[i].getStartFrame() - 1;
                                    }

                                    drawBox(cached_action_startFrame, cached_action_endFrame, _startFrame, _endFrame, rectTimelineActions, rectFramesBirdsEye.width, texBox);

                                    lastStartInd = -1;
                                    if(cached_action_endFrame >= _endFrame)
                                        break;
                                }
                            }
                        }
                        else {
                            int lastStartInd = -1;

                            for(int i = 0; i < _track.keys.Count; i++) {
                                bool draw = false;

                                if(!_track.keys[i].canTween) {
                                    if(i == 0 || !_track.keys[i - 1].canTween) {
                                        lastStartInd = i;
                                        draw = true;
                                    }
                                    else if(lastStartInd == -1)
                                        continue;
                                    else {
                                        draw = true;
                                    }
                                }
                                else if(lastStartInd == -1) {
                                    lastStartInd = i;
                                }
                                else if(i == _track.keys.Count - 1)
                                    draw = true;

                                if(draw) {
                                    if(_track is TranslationTrack) {
                                        // translation track, from first action frame to end action frame
                                        cached_action_startFrame = _track.keys[lastStartInd].getStartFrame();
                                        cached_action_endFrame = !_track.keys[lastStartInd].canTween ? cached_action_startFrame :
                                            !_track.keys[i].canTween ? _track.keys[i].frame : (_track.keys[i] as TranslationKey).endFrame;
                                        texBox = texBoxGreen;
                                    }
                                    else {
                                        cached_action_startFrame = _track.keys[lastStartInd].getStartFrame();
                                        cached_action_endFrame = _track.keys[i].getStartFrame();

                                        if(_track is RotationTrack || _track is RotationEulerTrack)
                                            texBox = texBoxYellow;
                                        else if(_track is OrientationTrack)
                                            texBox = texBoxOrange;
                                        else if(_track is CameraSwitcherTrack)
                                            texBox = texBoxPurple;
                                        else if(_track is ScaleTrack)
                                            texBox = texBoxLightPurple;
                                    }

                                    drawBox(cached_action_startFrame, cached_action_endFrame, _startFrame, _endFrame, rectTimelineActions, rectFramesBirdsEye.width, texBox);

                                    lastStartInd = -1;
                                    if(cached_action_endFrame >= _endFrame)
                                        break;
                                }
                            }
                        }
                    }
                }
                #endregion
                string txtInfo;
                Rect rectBox;
                // draw box for each action in track
                bool didClampBackwards = false; // whether or not clamped backwards, used to break infinite loop
                int last_action_startFrame = -1;
                for(int i = 0; i < _track.keys.Count; i++) {
                    if(_track.keys[i] == null) continue;

                    #region calculate dimensions
                    int clamped = 0; // 0 = no clamp, -1 = backwards clamp, 1 = forwards clamp
                    if(_track.keys[i].version != _track.version) {
                        // if cache is null, recheck for component and update caches
                        //aData = (Animate)GameObject.Find("Animate").GetComponent("Animate");
                        if(!Application.isPlaying && curTake.maintainCaches(aData.target))
                            //Just set the scenes dirty since we don't want to undo this
                            aData.SetTakesDirty();
                    }
                    if((_track is AudioTrack || _track is UnityAnimationTrack) && _track.keys[i].getNumberOfFrames(curTake.frameRate) > -1 && (_track.keys[i].getStartFrame() + _track.keys[i].getNumberOfFrames(curTake.frameRate) <= numFrames)) {
                        //based on content length
                        action_startFrame = _track.keys[i].getStartFrame();
                        action_endFrame = _track.keys[i].getStartFrame() + _track.keys[i].getNumberOfFrames(curTake.frameRate) - 1;
                        //audioClip = (_track.cache[i] as AMAudioAction).audioClip;
                        // if intersects new audio clip, then cut
                        if(i < _track.keys.Count - 1) {
                            if(action_endFrame > _track.keys[i + 1].getStartFrame()) action_endFrame = _track.keys[i + 1].getStartFrame();
                        }
                    }
                    else if((i == 0) && (!didClampBackwards) && (_track is PropertyTrack || _track is MaterialTrack || _track is GOSetActiveTrack || _track is CameraSwitcherTrack)) {
                        // clamp behind if first action
                        action_startFrame = 1;
                        action_endFrame = _track.keys[0].getStartFrame();
                        i--;
                        didClampBackwards = true;
                        clamped = -1;
                    }
                    else if(_track is UnityAnimationTrack || _track is AudioTrack || _track is PropertyTrack || _track is MaterialTrack || _track is EventTrack || _track is GOSetActiveTrack || _track is CameraSwitcherTrack) {
                        // single frame tracks (clamp box to last frame) (if audio track not set, clamp)
                        action_startFrame = _track.keys[i].getStartFrame();
                        if(i < _track.keys.Count - 1) {
                            action_endFrame = _track.keys[i + 1].getStartFrame();
                        }
                        else {
                            clamped = 1;
                            action_endFrame = _endFrame;
                            if(action_endFrame > numFrames) action_endFrame = numFrames + 1;
                        }
                    }
                    else {
                        // tracks with start frame and end frame (do not clamp box, stop before last key)
                        if(_track.keys[i].getNumberOfFrames(curTake.frameRate) <= 0) continue;
                        action_startFrame = _track.keys[i].getStartFrame();
                        action_endFrame = _track.keys[i].getStartFrame() + _track.keys[i].getNumberOfFrames(curTake.frameRate);
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
                    if(_track is UnityAnimationTrack) texBox = texBoxRed;
                    else if(_track is PropertyTrack || _track is MaterialTrack) texBox = texBoxLightBlue;
                    else if(_track is TranslationTrack) texBox = texBoxGreen;
                    else if(_track is AudioTrack) texBox = texBoxPink;
                    else if(_track is RotationTrack || _track is RotationEulerTrack) texBox = texBoxYellow;
                    else if(_track is OrientationTrack) texBox = texBoxOrange;
                    else if(_track is EventTrack) texBox = texBoxDarkBlue;
                    else if(_track is GOSetActiveTrack) texBox = texBoxDarkBlue;
                    else if(_track is CameraSwitcherTrack) texBox = texBoxPurple;
                    else if(_track is ScaleTrack) texBox = texBoxLightPurple;
                    else texBox = texBoxBorder;
                    if(drawEachAction) {
                        GUI.DrawTextureWithTexCoords(rectBox, texBox, new Rect(0, 0, rectBox.width / 32f, rectBox.height / 64f));
                        //if(audioClip) GUI.DrawTexture(rectBox,AssetPreview.GetAssetPreview(audioClip));
                    }
                    // for audio draw waveform
                    if(_track is AudioTrack) {
                        AudioKey _key = (AudioKey)((i >= 0) ? _track.keys[i] : _track.keys[0]);
                        if(_key.audioClip) {
                            float oneShotLength = _key.audioClip.length * curTake.frameRate;
                            Rect waveFormRect = new Rect(0, 0, 1, 1);
                            float endFrame = _key.getStartFrame() + _key.getNumberOfFrames(curTake.frameRate);
                            if(_key.loop) {
                                endFrame = curTakeEdit.endFrame;
                                waveFormRect.width = (endFrame - _key.getStartFrame()) / oneShotLength;
                            }
                            if(_track.keys.Count > i + 1) {
                                if(_track.keys[i + 1].getStartFrame() < endFrame) {
                                    endFrame = _track.keys[i + 1].getStartFrame();
                                    waveFormRect.width = (_track.keys[i + 1].getStartFrame() - _key.getStartFrame()) / oneShotLength;
                                }
                            }
                            if(_key.getStartFrame() < curTakeEdit.startFrame) {
                                waveFormRect.x = (curTakeEdit.startFrame - _key.getStartFrame()) / oneShotLength;
                                waveFormRect.width -= waveFormRect.x;
                            }
                            if(curTakeEdit.endFrame < endFrame) {
                                waveFormRect.width += (curTakeEdit.endFrame - endFrame) / oneShotLength;
                            }
                            Texture2D waveform = AssetPreview.GetAssetPreview(_key.audioClip);
                            if(waveform) {
                                if(!EditorGUIUtility.isProSkin) GUI.color = Color.black;
                                GUI.DrawTextureWithTexCoords(rectBox, waveform, waveFormRect);
                                GUI.color = Color.white;
                            }
                        }
                    }
                    // info tex label
                    bool hideTxtInfo = (GUI.skin.label.CalcSize(new GUIContent(txtInfo)).x > rectBox.width);
                    GUIStyle styleTxtInfo = new GUIStyle(GUI.skin.label);
                    styleTxtInfo.normal.textColor = Color.white;
                    styleTxtInfo.alignment = (hideTxtInfo ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
                    bool isLastAction;
                    if(_track is PropertyTrack || _track is MaterialTrack || _track is EventTrack || _track is GOSetActiveTrack || _track is CameraSwitcherTrack) isLastAction = (i == _track.keys.Count - 1);
                    else if(_track is AudioTrack || _track is UnityAnimationTrack) isLastAction = false;
                    else isLastAction = (i == _track.keys.Count - 2);
                    if(rectBox.width > 5f) EditorGUI.DropShadowLabel(new Rect(rectBox.x, rectBox.y, rectBox.width - (!isLastAction ? current_width_frame : 0f), rectBox.height), txtInfo, styleTxtInfo);
                    // if clicked on info box, select the starting frame for action. show tooltip if text does not fit
                    if(drawEachAction && GUI.Button(rectBox, /*(hideTxtInfo ? new GUIContent("",txtInfo) : new GUIContent(""))*/"", "label") && dragType != (int)DragType.ResizeAction) {
                        int prevFrame = curTake.selectedFrame;
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
                                    if(_track is UnityAnimationTrack || _track is AudioTrack) {
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
                        int _frame_num_action = (int)curTakeEdit.startFrame + Mathf.CeilToInt(e.mousePosition.x / current_width_frame) - 1;
                        Key _action = _track.getKeyContainingFrame(_frame_num_action);
                        int prevFrame = curTake.selectedFrame;
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
        void _dirtyTrackUpdate(Take ctake, Track sTrack) {
            sTrack.updateCache(aData.target);
            // preview new position
            ctake.previewFrame(aData.target, ctake.selectedFrame);
        }
        void showInspectorPropertiesFor(Rect rect, int _track, int _frame, Event e) {
            Take ctake = aData.currentTake;

            // if there are no tracks, return
            if(ctake.getTrackCount() <= 0) return;
            string track_name = "";
            Track sTrack = null;
            if(_track > -1) {
                int trackInd = ctake.getTrackIndex(_track);
                // get the selected track
                if(trackInd != -1 && trackInd < ctake.trackValues.Count)
                    sTrack = ctake.trackValues[trackInd];
            }
            if(sTrack == null) return;
            track_name = sTrack.name + ", ";
            GUIStyle styleLabelWordwrap = new GUIStyle(GUI.skin.label);
            styleLabelWordwrap.wordWrap = true;
            string strFrameInfo = track_name;
            if(oData.time_numbering) strFrameInfo += "Time " + frameToTime(_frame, (float)ctake.frameRate).ToString("N2") + " s";
            else strFrameInfo += "Frame " + _frame;
            GUI.Label(new Rect(0f, 0f, width_inspector_open - width_inspector_closed - width_button_delete - margin, 20f), strFrameInfo, styleLabelWordwrap);
            Rect rectBtnDeleteKey = new Rect(width_inspector_open - width_inspector_closed - width_button_delete - margin, 0f, width_button_delete, height_button_delete);
            // if frame has no key or isPlaying, return
            if(_track <= -1 || !(sTrack.hasKeyOnFrame(_frame) || sTrack.hasTrackSettings) || isPlaying) {
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

            Key key = sTrack.getKeyOnFrame(_frame, false);
            if(key == null) return;

            //show interpolation if applicable
            if(sTrack.canTween) {
                Rect rectLabelInterp = new Rect(0f, start_y, 50f, 20f);
                GUI.Label(rectLabelInterp, "Interpl.");
                Rect rectSelGrid = new Rect(rectLabelInterp.x + rectLabelInterp.width + margin, rectLabelInterp.y, width_inspector - rectLabelInterp.width - margin * 2f, rectLabelInterp.height);

                int interp = (int)key.interp;
                int nInterp = sTrack.interpCount == 3 ?
                    GUI.SelectionGrid(rectSelGrid, interp, texInterpl3, 3, GUI.skin.GetStyle("ButtonImage")) :
                    GUI.SelectionGrid(rectSelGrid, interp > 0 ? interp - 1 : 0, texInterpl2, 3, GUI.skin.GetStyle("ButtonImage")) + 1;

                if(interp != nInterp) {
                    aData.RegisterTakesUndo("Change Interpolation", false);

                    key.interp = (Key.Interpolation)nInterp;

                    sTrack.updateCache(aData.target);
                    // select the current frame
                    timelineSelectFrame(_track, _frame);
                }

                start_y = rectLabelInterp.max.y + height_inspector_space;
            }

            #region translation inspector
            if(sTrack is TranslationTrack) {
                TranslationTrack tTrack = (TranslationTrack)sTrack;
                Rect rectPosition = new Rect(0f, start_y, 0f, 0f);
                if(sTrack.hasKeyOnFrame(_frame)) {
                    TranslationKey tKey = (TranslationKey)key;

                    // translation position
                    rectPosition = new Rect(0f, start_y, width_inspector - margin, 40f);
                    Vector3 nPos = EditorGUI.Vector3Field(rectPosition, "Position", tKey.position);
                    if(tKey.position != nPos) {
                        aData.RegisterTakesUndo("Change Position", false);
                        tKey.position = nPos;

                        _dirtyTrackUpdate(ctake, sTrack);
                    }

                    // if not only key, show ease
                    bool isTKeyLastFrame = tKey == tTrack.keys[tTrack.keys.Count - 1];

                    if(key.canTween && !isTKeyLastFrame) {
                        rectPosition = new Rect(0f, rectPosition.y + rectPosition.height + height_inspector_space, width_inspector - margin, 0f);
                        if(!isTKeyLastFrame && tKey.interp == Key.Interpolation.Linear)
                            showEasePicker(sTrack, tKey, aData, rectPosition.x, rectPosition.y, rectPosition.width);
                        else
                            showEasePicker(sTrack, tTrack.getKeyStartFor(tKey.frame), aData, rectPosition.x, rectPosition.y, rectPosition.width);

                        rectPosition.height = 80.0f;
                    }
                }

                //display pixel snap option
                rectPosition = new Rect(0f, rectPosition.y + rectPosition.height + height_inspector_space, width_inspector - margin, 20.0f);
                bool nPixelSnap = EditorGUI.Toggle(rectPosition, "Pixel-Snap", tTrack.pixelSnap);
                if(nPixelSnap != tTrack.pixelSnap) {
                    aData.RegisterTakesUndo("Set Pixel-Snap", false);
                    tTrack.pixelSnap = nPixelSnap;
                }
                //display pixel-per-unit
                if(tTrack.pixelSnap) {
                    rectPosition = new Rect(0f, rectPosition.y + rectPosition.height + height_inspector_space, width_inspector - margin, 20.0f);
                    float nppu = EditorGUI.FloatField(rectPosition, "Pixel/Unit", tTrack.pixelPerUnit);

                    rectPosition = new Rect(5f, rectPosition.y + rectPosition.height + height_inspector_space, width_inspector - margin - 10f, 20.0f);
                    if(GUI.Button(rectPosition, "Pixel/Unit Default"))
                        nppu = oData.pixelPerUnitDefault;

                    if(nppu <= 0.0f) nppu = 0.001f;
                    if(tTrack.pixelPerUnit != nppu) {
                        aData.RegisterTakesUndo("Set Pixel-Per-Unit", false);
                        tTrack.pixelPerUnit = nppu;
                    }
                }
                return;
            }
            #endregion
            #region rotation inspector
            if(sTrack is RotationTrack) {
                RotationKey rKey = (RotationKey)key;
                Rect rectQuaternion = new Rect(0f, start_y, width_inspector - margin, 40f);
                // quaternion
                Vector3 rot = rKey.rotation.eulerAngles;
                Vector3 nrot = EditorGUI.Vector3Field(rectQuaternion, "Rotation", rot);
                if(rot != nrot) {
                    aData.RegisterTakesUndo("Change Rotation", false);
                    rKey.rotation = Quaternion.Euler(nrot);

                    _dirtyTrackUpdate(ctake, sTrack);
                }
                // if not last key, show ease
                if(key.canTween && rKey != (sTrack as RotationTrack).keys[(sTrack as RotationTrack).keys.Count - 1]) {
                    Rect recEasePicker = new Rect(0f, rectQuaternion.y + rectQuaternion.height + height_inspector_space, width_inspector - margin, 0f);
                    if((sTrack as RotationTrack).getKeyIndexForFrame(_frame) > -1) {
                        showEasePicker(sTrack, rKey, aData, recEasePicker.x, recEasePicker.y, recEasePicker.width);
                    }
                }
                return;
            }
            #endregion
            #region rotation euler inspector
            if(sTrack is RotationEulerTrack) {
                RotationEulerKey rKey = (RotationEulerKey)key;
                Rect rectQuaternion = new Rect(0f, start_y, width_inspector - margin, 40f);
                // euler
                Vector3 nrot = EditorGUI.Vector3Field(rectQuaternion, "Rotation", rKey.rotation);
                if(rKey.rotation != nrot) {
                    aData.RegisterTakesUndo("Change Rotation", false);
                    rKey.rotation = nrot;

                    _dirtyTrackUpdate(ctake, sTrack);
                }
                // if not last key, show ease
                if(key.canTween && rKey != ((RotationEulerTrack)sTrack).keys[((RotationEulerTrack)sTrack).keys.Count - 1]) {
                    Rect recEasePicker = new Rect(0f, rectQuaternion.y + rectQuaternion.height + height_inspector_space, width_inspector - margin, 0f);
                    if(sTrack.getKeyIndexForFrame(_frame) > -1) {
                        showEasePicker(sTrack, rKey, aData, recEasePicker.x, recEasePicker.y, recEasePicker.width);
                    }
                }
                return;
            }
            #endregion
            #region scale inspector
            if(sTrack is ScaleTrack) {
                var sKey = (ScaleKey)key;
                Rect rectScale = new Rect(0f, start_y, width_inspector - margin, 40f);
                // euler
                var nscale = EditorGUI.Vector3Field(rectScale, "Scale", sKey.scale);
                if(sKey.scale != nscale) {
                    aData.RegisterTakesUndo("Change Scale", false);
                    sKey.scale = nscale;

                    _dirtyTrackUpdate(ctake, sTrack);
                }
                // if not last key, show ease
                if(key.canTween && sKey != ((ScaleTrack)sTrack).keys[((ScaleTrack)sTrack).keys.Count - 1]) {
                    Rect recEasePicker = new Rect(0f, rectScale.y + rectScale.height + height_inspector_space, width_inspector - margin, 0f);
                    if(sTrack.getKeyIndexForFrame(_frame) > -1) {
                        showEasePicker(sTrack, sKey, aData, recEasePicker.x, recEasePicker.y, recEasePicker.width);
                    }
                }
                return;
            }
            #endregion
            #region orientation inspector
            if(sTrack is OrientationTrack) {
                OrientationKey oKey = (OrientationKey)(sTrack as OrientationTrack).getKeyOnFrame(_frame);
                // target
                Rect rectLabelTarget = new Rect(0f, start_y, 50f, 22f);
                GUI.Label(rectLabelTarget, "Target");
                Rect rectObjectTarget = new Rect(rectLabelTarget.x + rectLabelTarget.width + 3f, rectLabelTarget.y + 3f, width_inspector - rectLabelTarget.width - 3f - margin - width_button_delete, 16f);
                Transform ntgt = (Transform)EditorGUI.ObjectField(rectObjectTarget, oKey.GetTarget(aData.target), typeof(Transform), true);
                if(oKey.GetTarget(aData.target) != ntgt) {
                    aData.RegisterTakesUndo("Change Target", false);
                    oKey.SetTarget(aData.target, ntgt);

                    _dirtyTrackUpdate(ctake, sTrack);
                }
                Rect rectNewTarget = new Rect(width_inspector - width_button_delete - margin, rectLabelTarget.y, width_button_delete, width_button_delete);
                if(GUI.Button(rectNewTarget, "+")) {
                    GenericMenu addTargetMenu = new GenericMenu();
                    addTargetMenu.AddItem(new GUIContent("With Translation"), false, addTargetWithTranslationTrack, oKey);
                    addTargetMenu.AddItem(new GUIContent("Without Translation"), false, addTargetWithoutTranslationTrack, oKey);
                    addTargetMenu.ShowAsContext();
                }
                // if not last key, show ease
                if(key.canTween && oKey != (sTrack as OrientationTrack).keys[(sTrack as OrientationTrack).keys.Count - 1]) {
                    int oActionIndex = (sTrack as OrientationTrack).getKeyIndexForFrame(_frame);
                    if(oActionIndex > -1 && oActionIndex + 1 < sTrack.keys.Count && (sTrack.keys[oActionIndex] as OrientationKey).GetTarget(aData.target) != (sTrack.keys[oActionIndex + 1] as OrientationKey).GetTarget(aData.target)) {
                        Rect recEasePicker = new Rect(0f, rectNewTarget.y + rectNewTarget.height + height_inspector_space, width_inspector - margin, 0f);
                        showEasePicker(sTrack, oKey, aData, recEasePicker.x, recEasePicker.y, recEasePicker.width);
                    }
                }
                return;
            }
            #endregion
            #region animation inspector
            if(sTrack is UnityAnimationTrack) {
                UnityAnimationKey aKey = (UnityAnimationKey)(sTrack as UnityAnimationTrack).getKeyOnFrame(_frame);
                // animation clip
                Rect rectLabelAnimClip = new Rect(0f, start_y, 100f, 22f);
                GUI.Label(rectLabelAnimClip, "Animation Clip");
                Rect rectObjectField = new Rect(rectLabelAnimClip.x + rectLabelAnimClip.width + 2f, rectLabelAnimClip.y + 3f, width_inspector - rectLabelAnimClip.width - margin, 16f);
                AnimationClip nclip = (AnimationClip)EditorGUI.ObjectField(rectObjectField, aKey.amClip, typeof(AnimationClip), false);
                if(aKey.amClip != nclip) {
                    aData.RegisterTakesUndo("Change Animation Clip", false);
                    aKey.amClip = nclip;
                    // preview new position
                    ctake.previewFrame(aData.target, ctake.selectedFrame);
                }
                // wrap mode
                Rect rectLabelWrapMode = new Rect(0f, rectLabelAnimClip.y + rectLabelAnimClip.height + height_inspector_space, 85f, 22f);
                GUI.Label(rectLabelWrapMode, "Wrap Mode");
                Rect rectPopupWrapMode = new Rect(rectLabelWrapMode.x + rectLabelWrapMode.width, rectLabelWrapMode.y + 3f, 120f, 22f);
                WrapMode nwrapmode = indexToWrapMode(EditorGUI.Popup(rectPopupWrapMode, wrapModeToIndex(aKey.wrapMode), wrapModeNames));
                if(aKey.wrapMode != nwrapmode) {
                    aData.RegisterTakesUndo("Wrap Mode", false);
                    aKey.wrapMode = nwrapmode;
                    // preview new position
                    ctake.previewFrame(aData.target, ctake.selectedFrame);
                }
                // crossfade
                Rect rectLabelCrossfade = new Rect(0f, rectLabelWrapMode.y + rectPopupWrapMode.height + height_inspector_space, 85f, 22f);
                GUI.Label(rectLabelCrossfade, "Crossfade");
                Rect rectToggleCrossfade = new Rect(rectLabelCrossfade.x + rectLabelCrossfade.width, rectLabelCrossfade.y + 2f, 20f, rectLabelCrossfade.height);
                bool ncrossfade = EditorGUI.Toggle(rectToggleCrossfade, aKey.crossfade);
                if(aKey.crossfade != ncrossfade) {
                    aData.RegisterTakesUndo("Cross Fade", false);
                    aKey.crossfade = ncrossfade;
                    // preview new position
                    ctake.previewFrame(aData.target, ctake.selectedFrame);
                }
                Rect rectLabelCrossFadeTime = new Rect(rectToggleCrossfade.x + rectToggleCrossfade.width + 10f, rectLabelCrossfade.y, 35f, rectToggleCrossfade.height);
                if(!aKey.crossfade) GUI.enabled = false;
                GUI.Label(rectLabelCrossFadeTime, "Time");
                Rect rectFloatFieldCrossFade = new Rect(rectLabelCrossFadeTime.x + rectLabelCrossFadeTime.width + margin, rectLabelCrossFadeTime.y + 3f, 40f, rectLabelCrossFadeTime.height);
                float ncrossfadet = EditorGUI.FloatField(rectFloatFieldCrossFade, aKey.crossfadeTime);
                if(aKey.crossfadeTime != ncrossfadet) {
                    aData.RegisterTakesUndo("Cross Fade Time", false);
                    aKey.crossfadeTime = ncrossfadet;
                }
                Rect rectLabelSeconds = new Rect(rectFloatFieldCrossFade.x + rectFloatFieldCrossFade.width + margin, rectLabelCrossFadeTime.y, 20f, rectLabelCrossFadeTime.height);
                GUI.Label(rectLabelSeconds, "s");
                GUI.enabled = true;
                return;
            }
            #endregion
            #region audio inspector
            if(sTrack is AudioTrack) {
                AudioKey auKey = (AudioKey)(sTrack as AudioTrack).getKeyOnFrame(_frame);
                // audio clip
                Rect rectLabelAudioClip = new Rect(0f, start_y, 80f, 22f);
                GUI.Label(rectLabelAudioClip, "Audio Clip");
                Rect rectObjectField = new Rect(rectLabelAudioClip.x + rectLabelAudioClip.width + margin, rectLabelAudioClip.y + 3f, width_inspector - rectLabelAudioClip.width - margin, 16f);
                AudioClip nclip = (AudioClip)EditorGUI.ObjectField(rectObjectField, auKey.audioClip, typeof(AudioClip), false);
                if(auKey.audioClip != nclip) {
                    aData.RegisterTakesUndo("Set Audio Clip", false);
                    auKey.audioClip = nclip;

                }
                Rect rectLabelLoop = new Rect(0f, rectLabelAudioClip.y + rectLabelAudioClip.height + height_inspector_space, 80f, 22f);
                // loop audio
                GUI.Label(rectLabelLoop, "Loop");
                Rect rectToggleLoop = new Rect(rectLabelLoop.x + rectLabelLoop.width + margin, rectLabelLoop.y + 2f, 22f, 22f);
                bool nloop = EditorGUI.Toggle(rectToggleLoop, auKey.loop);
                if(auKey.loop != nloop) {
                    aData.RegisterTakesUndo("Set Audio Loop", false);
                    auKey.loop = nloop;
                }

                // One Shot?
                Rect rectLabelOneShot = new Rect(0f, rectLabelLoop.y + rectLabelLoop.height, 80f, 22f);
                GUI.Label(rectLabelOneShot, "One Shot");
                Rect rectToggleOneShot = new Rect(rectLabelOneShot.x + rectLabelOneShot.width + margin, rectLabelOneShot.y + 2f, 22f, 22f);
                bool nOneShot = EditorGUI.Toggle(rectToggleOneShot, auKey.oneShot);
                if(auKey.oneShot != nOneShot) {
                    aData.RegisterTakesUndo("Set Audio One Shot", false);
                    auKey.oneShot = nOneShot;
                }
                return;
            }
            #endregion
            #region property inspector
            if(sTrack is PropertyTrack) {
                PropertyTrack pTrack = sTrack as PropertyTrack;
                PropertyKey pKey = (PropertyKey)pTrack.getKeyOnFrame(_frame);
                // value
                string propertyLabel = pTrack.getTrackType();
                Rect rectField = new Rect(0f, start_y, width_inspector - margin, 22f);
                bool isUpdated = false;
                if((pTrack.valueType == PropertyTrack.ValueType.Integer) || (pTrack.valueType == PropertyTrack.ValueType.Long)) {
                    int val = Convert.ToInt32(pKey.val);
                    int nval = EditorGUI.IntField(rectField, propertyLabel, val);
                    if(val != nval) {
                        aData.RegisterTakesUndo("Change Property Value", false);
                        pKey.val = nval;
                        isUpdated = true;
                    }
                }
                else if((pTrack.valueType == PropertyTrack.ValueType.Float) || (pTrack.valueType == PropertyTrack.ValueType.Double)) {
                    float val = (float)pKey.val;
                    float nval = EditorGUI.FloatField(rectField, propertyLabel, val);
                    if(val != nval) {
                        aData.RegisterTakesUndo("Change Property Value", false);
                        pKey.val = nval;
                        isUpdated = true;
                    }
                }
                else if(pTrack.valueType == PropertyTrack.ValueType.Bool) {
                    bool val = pKey.val > 0.0;
                    bool nval = EditorGUI.Toggle(rectField, propertyLabel, val);
                    if(val != nval) {
                        aData.RegisterTakesUndo("Change Property Value", false);
                        pKey.valb = nval;
                        isUpdated = true;
                    }
                }
                else if(pTrack.valueType == PropertyTrack.ValueType.String) {
                    string val = pKey.valString;
                    string nval = EditorGUI.TextField(rectField, propertyLabel, val);
                    if(val != nval) {
                        aData.RegisterTakesUndo("Change Property Value", false);
                        pKey.valString = nval;
                        isUpdated = true;
                    }
                }
                else if(pTrack.valueType == PropertyTrack.ValueType.Vector2) {
                    rectField.height = 40f;
                    Vector2 nval = EditorGUI.Vector2Field(rectField, propertyLabel, pKey.vect2);
                    if(pKey.vect2 != nval) {
                        aData.RegisterTakesUndo("Change Property Value", false);
                        pKey.vect2 = nval;
                        isUpdated = true;
                    }
                }
                else if(pTrack.valueType == PropertyTrack.ValueType.Vector3) {
                    rectField.height = 40f;
                    Vector3 nval = EditorGUI.Vector3Field(rectField, propertyLabel, pKey.vect3);
                    if(pKey.vect3 != nval) {
                        aData.RegisterTakesUndo("Change Property Value", false);
                        pKey.vect3 = nval;
                        isUpdated = true;
                    }
                }
                else if(pTrack.valueType == PropertyTrack.ValueType.Color) {
                    var nclr = RenderColorProperty(rectField, propertyLabel, pKey.color);
                    if(pKey.color != nclr) {
                        aData.RegisterTakesUndo("Change Property Value", false);
                        pKey.color = nclr;
                        isUpdated = true;
                    }
                }
                else if(pTrack.valueType == PropertyTrack.ValueType.Rect) {
                    rectField.height = 60f;
                    Rect nrect = EditorGUI.RectField(rectField, propertyLabel, pKey.rect);
                    if(pKey.rect != nrect) {
                        aData.RegisterTakesUndo("Change Property Value", false);
                        pKey.rect = nrect;
                        isUpdated = true;
                    }
                }
                else if(pTrack.valueType == PropertyTrack.ValueType.Vector4
                    || pTrack.valueType == PropertyTrack.ValueType.Quaternion) {
                    rectField.height = 40f;
                    Vector4 nvec = EditorGUI.Vector4Field(rectField, propertyLabel, pKey.vect4);
                    if(pKey.vect4 != nvec) {
                        aData.RegisterTakesUndo("Change Property Value", false);
                        pKey.vect4 = nvec;
                        isUpdated = true;
                    }
                }
                else if(pTrack.valueType == PropertyTrack.ValueType.Sprite) {
                    UnityEngine.Object val = pKey.valObj;
                    GUI.skin = null; EditorUtility.ResetDisplayControls();
                    rectField.height = 16.0f;
                    UnityEngine.Object nval = EditorGUI.ObjectField(rectField, val, typeof(Sprite), false);
                    GUI.skin = skin; EditorUtility.ResetDisplayControls();
                    if(val != nval) {
                        aData.RegisterTakesUndo("Change Property Value", false);
                        pKey.valObj = nval;
                        isUpdated = true;
                    }
                }
                else if(pTrack.valueType == PropertyTrack.ValueType.Enum) {
                    rectField.height = 20.0f;
                    GUI.Label(rectField, propertyLabel);
                    rectField.y += rectField.height + 4.0f;
                    rectField.height = 16f;
                    Enum curEnum = Enum.ToObject(pTrack.GetCachedInfoType(aData.target), (int)pKey.val) as Enum;
                    Enum newEnum = EditorGUI.EnumPopup(rectField, curEnum);
                    if(curEnum != newEnum) {
                        aData.RegisterTakesUndo("Change Property Value", false);
                        pKey.val = Convert.ToDouble(newEnum);
                        isUpdated = true;
                    }
                }
                if(isUpdated)
                    _dirtyTrackUpdate(ctake, sTrack);

                // property ease, show if not last key (check for action; there is no rotation action for last key). do not show for morph channels, because it is shown before the parameters
                // don't show on non-tweenable
                if(pTrack.canTween && key.canTween && pKey != pTrack.keys[pTrack.keys.Count - 1]) {
                    Rect rectEasePicker = new Rect(0f, rectField.y + rectField.height + height_inspector_space, width_inspector - margin, 0f);
                    showEasePicker(sTrack, pKey, aData, rectEasePicker.x, rectEasePicker.y, rectEasePicker.width);
                }
                return;
            }
            #endregion
            #region event set go active
            if(sTrack is GOSetActiveTrack) {
                GOSetActiveTrack goActiveTrack = sTrack as GOSetActiveTrack;
                GOSetActiveKey pKey = (GOSetActiveKey)goActiveTrack.getKeyOnFrame(_frame);

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
                    aData.RegisterTakesUndo("Set GameObject Start Active", false);
                    goActiveTrack.startActive = newStartVal;
                }

                if(newVal != pKey.setActive) {
                    aData.RegisterTakesUndo("Set GameObject Active", false);
                    pKey.setActive = newVal;

                    _dirtyTrackUpdate(ctake, sTrack);
                }
                return;
            }
            #endregion
            #region event inspector
            if(sTrack is EventTrack) {
                var obj = sTrack.GetTarget(aData.target);
                EventKey eKey = (EventKey)(sTrack as EventTrack).getKeyOnFrame(_frame);
                // value
                if(indexMethodInfo == -1 || cachedMethodInfo.Count <= 0) {
                    Rect rectLabel = new Rect(0f, start_y, width_inspector - margin * 2f - 20f, 22f);
                    GUI.Label(rectLabel, "No usable methods found.");
                    Rect rectButton = new Rect(width_inspector - 20f - margin, start_y + 1f, 20f, 20f);
                    if(GUI.Button(rectButton, "?")) {
                        UnityEditor.EditorUtility.DisplayDialog("Usable Methods", "Methods should be made public and be placed in scripts that are not directly derived from Component or Behaviour to be used in the Event Track (MonoBehaviour is fine).", "Okay");
                    }
                    return;
                }

                bool sendMessageToggleEnabled = true;
                Rect rectPopup = new Rect(0f, start_y, width_inspector - margin, 22f);
                int curIndexMethod = indexMethodInfo;
                indexMethodInfo = EditorGUI.Popup(rectPopup, curIndexMethod, getMethodNames());

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
                        UnityEditor.EditorUtility.DisplayDialog("Use Object[] Parameter Instead", "Array types derived from Object, such as GameObject[], cannot be cast correctly on runtime.\n\nUse UnityEngine.Object[] as a parameter type and then cast to (GameObject[]) in your method.\n\nIf you're trying to pass components" + (showObjectType != typeof(GameObject) ? " (such as " + showObjectType.ToString() + ")" : "") + ", you should get them from the casted GameObjects on runtime.\n\nPlease see the documentation for more information.", "Okay");
                    }
                    return;
                }

                // changed, if index out of range, if no method and there's only one on the list
                bool paramMatched = eKey.isMatch(cachedParameterInfos);
                if((indexMethodInfo != curIndexMethod && indexMethodInfo < cachedMethodInfo.Count) || !paramMatched || (string.IsNullOrEmpty(eKey.methodName) && cachedMethodInfo.Count == 1)) {
                    // process change
                    // update cache when modifying varaibles
                    if(eKey.setMethodInfo(obj, cachedMethodInfo[indexMethodInfo], cachedParameterInfos, !paramMatched, null)) {
                        aData.SetTakesDirty(); //don't really want to undo this

                        // deselect fields
                        GUIUtility.keyboardControl = 0;
                    }
                }
                if(cachedParameterInfos.Length > 1 || !(obj is Component)) {
                    // if method has more than 1 parameter, set sendmessage to false, and disable toggle
                    if(eKey.setUseSendMessage(false, null))
                        aData.SetTakesDirty(); //don't really want to undo this

                    sendMessageToggleEnabled = false;   // disable sendmessage toggle
                }

                GUI.enabled = sendMessageToggleEnabled;
                Rect rectLabelSendMessage = new Rect(0f, rectLabelObjectMessage.y + rectLabelObjectMessage.height + height_inspector_space, 150f, 20f);
                GUI.Label(rectLabelSendMessage, "Use SendMessage");
                Rect rectToggleSendMessage = new Rect(rectLabelSendMessage.x + rectLabelSendMessage.width + margin, rectLabelSendMessage.y, 20f, 20f);
                if(eKey.setUseSendMessage(GUI.Toggle(rectToggleSendMessage, eKey.useSendMessage, ""), delegate (EventKey k) { aData.RegisterTakesUndo("Set Event Key Method Mode", false); }))
                    sTrack.updateCache(aData.target);

                GUI.enabled = !isPlaying;
                Rect rectButtonSendMessageInfo = new Rect(width_inspector - 20f - margin, rectLabelSendMessage.y, 20f, 20f);
                if(GUI.Button(rectButtonSendMessageInfo, "?")) {
                    UnityEditor.EditorUtility.DisplayDialog("SendMessage vs. Invoke", "SendMessage can only be used with methods that have no more than one parameter (which can be an array).\n\nAnimator will use Invoke when SendMessage is disabled, which is slightly faster but requires caching when the take is played. Use SendMessage if caching is a problem.", "Okay");
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
                        showFieldFor(aData.target, eKey, rectField, i.ToString(), cachedParameterInfos[i].Name, eKey.parameters[i], cachedParameterInfos[i].ParameterType, 0, ref height_field);
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
            #region camerea switcher inspector
            if(sTrack is CameraSwitcherTrack) {
                CameraSwitcherKey cKey = (CameraSwitcherKey)(sTrack as CameraSwitcherTrack).getKeyOnFrame(_frame);
                bool showExtras = false;
                bool notLastKey = cKey != (sTrack as CameraSwitcherTrack).keys[(sTrack as CameraSwitcherTrack).keys.Count - 1];
                if(notLastKey) {
                    int cActionIndex = (sTrack as CameraSwitcherTrack).getKeyIndexForFrame(_frame);
                    showExtras = cActionIndex > -1 && !(sTrack.keys[cActionIndex] as CameraSwitcherKey).targetsAreEqual(aData.target);
                }
                float height_cs = 44f + height_inspector_space + (showExtras ? 22f * 3f + height_inspector_space * 3f : 0f);
                Rect rectScrollView = new Rect(0f, start_y, width_inspector - margin, rect.height - start_y);
                Rect rectView = new Rect(0f, 0f, rectScrollView.width - (height_cs > rectScrollView.height ? 20f : 0f), height_cs);
                inspectorScrollView = GUI.BeginScrollView(rectScrollView, inspectorScrollView, rectView);
                Rect rectLabelType = new Rect(0f, 0f, 56f, 22f);
                GUI.Label(rectLabelType, "Type");
                Rect rectSelGridType = new Rect(rectLabelType.x + rectLabelType.width + margin, rectLabelType.y, rectView.width - margin - rectLabelType.width, 22f);
                int newType = GUI.SelectionGrid(rectSelGridType, cKey.type, new string[] { "Camera", "Color" }, 2);
                if(cKey.type != newType) {
                    aData.RegisterTakesUndo("Set Camera Track Type", false);
                    cKey.type = newType;

                    _dirtyTrackUpdate(ctake, sTrack);
                }
                // camera
                Rect rectLabelCameraColor = new Rect(0f, rectLabelType.y + rectLabelType.height + height_inspector_space, 56f, 22f);
                GUI.Label(rectLabelCameraColor, (cKey.type == 0 ? "Camera" : "Color"));
                Rect rectCameraColor = new Rect(rectLabelCameraColor.x + rectLabelCameraColor.width + margin, rectLabelCameraColor.y + 3f, rectView.width - rectLabelCameraColor.width - margin, 16f);
                if(cKey.type == 0) {
                    Camera newCam = (Camera)EditorGUI.ObjectField(rectCameraColor, cKey.GetCamera(aData.target), typeof(Camera), true);
                    if(cKey.GetCamera(aData.target) != newCam) {
                        aData.RegisterTakesUndo("Set Camera Track Camera", false);
                        cKey.SetCamera(aData.target, newCam);

                        _dirtyTrackUpdate(ctake, sTrack);
                    }
                }
                else {
                    Color newColor = EditorGUI.ColorField(rectCameraColor, cKey.color);
                    if(cKey.color != newColor) {
                        aData.RegisterTakesUndo("Set Camera Track Color", false);
                        cKey.color = newColor;

                        _dirtyTrackUpdate(ctake, sTrack);
                    }
                }
                GUI.enabled = true;
                // if not last key, show transition and ease
                if(key.canTween && notLastKey && showExtras) {
                    // transition picker
                    Rect rectTransitionPicker = new Rect(0f, rectLabelCameraColor.y + rectLabelCameraColor.height + height_inspector_space, rectView.width, 22f);
                    showTransitionPicker(sTrack, cKey, rectTransitionPicker.x, rectTransitionPicker.y, rectTransitionPicker.width);
                    if(cKey.cameraFadeType != (int)CameraSwitcherKey.Fade.None) {
                        // ease picker
                        Rect rectEasePicker = new Rect(0f, rectTransitionPicker.y + rectTransitionPicker.height + height_inspector_space, rectView.width, 22f);
                        showEasePicker(sTrack, cKey, aData, rectEasePicker.x, rectEasePicker.y, rectEasePicker.width);
                        // render texture
                        Rect rectLabelRenderTexture = new Rect(0f, rectEasePicker.y + rectEasePicker.height + height_inspector_space + 55.0f, 175f, 22f);
                        GUI.Label(rectLabelRenderTexture, "Render Texture (Pro Only)");
                        Rect rectToggleRenderTexture = new Rect(rectView.width - 22f, rectLabelRenderTexture.y, 22f, 22f);
                        bool newStill = !GUI.Toggle(rectToggleRenderTexture, !cKey.still, "");
                        if(cKey.still != newStill) {
                            aData.RegisterTakesUndo("Set Camera Track Still", false);
                            cKey.still = newStill;

                            _dirtyTrackUpdate(ctake, sTrack);
                        }
                    }
                }
                GUI.EndScrollView();
                return;
            }
            #endregion            
            #region material inspector
            if(sTrack is MaterialTrack) {
                MaterialTrack aTrack = sTrack as MaterialTrack;
                MaterialKey aKey = (MaterialKey)aTrack.getKeyOnFrame(_frame);
                // value
                string propertyLabel = aTrack.getTrackType();
                Rect rectField = new Rect(0f, start_y, width_inspector - margin, 22f);
                bool isUpdated = false;
                if(aTrack.propertyType == MaterialTrack.ValueType.Float) {
                    float fval = EditorGUI.FloatField(rectField, propertyLabel, aKey.val);
                    if(aKey.val != fval) {
                        aData.RegisterTakesUndo("Change Material Property Value", false);
                        aKey.val = fval;
                        isUpdated = true;
                    }
                }
                else if(aTrack.propertyType == MaterialTrack.ValueType.Range) {
                    //grab limiters
                    Material mat = aTrack.GetMaterial(aData.target);
                    Shader shader = mat.shader;
                    for(int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++) { //grab the proper index and get the limiters
                        if(aTrack.property == ShaderUtil.GetPropertyName(shader, i)) {
                            float valMin = ShaderUtil.GetRangeLimits(shader, i, 1);
                            float valMax = ShaderUtil.GetRangeLimits(shader, i, 2);

                            //slider
                            float fval = EditorGUI.Slider(rectField, propertyLabel, aKey.val, valMin, valMax);
                            if(aKey.val != fval) {
                                aData.RegisterTakesUndo("Change Material Property Value", false);
                                aKey.val = fval;
                                isUpdated = true;
                            }
                            break;
                        }
                    }
                }
                else if(aTrack.propertyType == MaterialTrack.ValueType.Vector) {
                    rectField.height = 40f;
                    Vector4 nvec = EditorGUI.Vector4Field(rectField, propertyLabel, aKey.vector);
                    if(aKey.vector != nvec) {
                        aData.RegisterTakesUndo("Change Material Property Value", false);
                        aKey.vector = nvec;
                        isUpdated = true;
                    }
                }
                else if(aTrack.propertyType == MaterialTrack.ValueType.Color) {
                    var nclr = RenderColorProperty(rectField, propertyLabel, aKey.color);
                    if(aKey.color != nclr) {
                        aData.RegisterTakesUndo("Change Material Property Value", false);
                        aKey.color = nclr;
                        isUpdated = true;
                    }
                }
                else if(aTrack.propertyType == MaterialTrack.ValueType.TexEnv) {
                    //texture
                    GUI.skin = null; EditorUtility.ResetDisplayControls();
                    rectField.height = 16.0f;
                    Texture nval = EditorGUI.ObjectField(rectField, aKey.texture, typeof(Texture), false) as Texture;
                    GUI.skin = skin; EditorUtility.ResetDisplayControls();

                    if(aKey.texture != nval) {
                        aData.RegisterTakesUndo("Change Material Property Value", false);
                        aKey.texture = nval;
                        isUpdated = true;
                    }
                }
                else if(aTrack.propertyType == MaterialTrack.ValueType.TexOfs) {
                    rectField.height = 40f;
                    Vector2 nvec = EditorGUI.Vector2Field(rectField, propertyLabel, aKey.texOfs);
                    if(aKey.texOfs != nvec) {
                        aData.RegisterTakesUndo("Change Material Property Value", false);
                        aKey.texOfs = nvec;
                        isUpdated = true;
                    }
                }
                else if(aTrack.propertyType == MaterialTrack.ValueType.TexScale) {
                    rectField.height = 40f;
                    Vector2 nvec = EditorGUI.Vector2Field(rectField, propertyLabel, aKey.texScale);
                    if(aKey.texScale != nvec) {
                        aData.RegisterTakesUndo("Change Material Property Value", false);
                        aKey.texScale = nvec;
                        isUpdated = true;
                    }
                }

                if(isUpdated)
                    _dirtyTrackUpdate(ctake, sTrack);

                // property ease, show if not last key (check for action; there is no rotation action for last key). do not show for morph channels, because it is shown before the parameters
                // don't show on non-tweenable
                if(sTrack.canTween && key.canTween && key != sTrack.keys[sTrack.keys.Count - 1]) {
                    Rect rectEasePicker = new Rect(0f, rectField.y + rectField.height + height_inspector_space, width_inspector - margin, 0f);
                    showEasePicker(sTrack, key, aData, rectEasePicker.x, rectEasePicker.y, rectEasePicker.width);
                }
                return;
            }
            #endregion
        }

        void showFieldFor(ITarget iTarget, EventKey eKey, Rect rect, string id, string name, EventData data, Type t, int level, ref float height_field) {
            rect.x = 5f * level;
            name = typeStringBrief(t) + " " + name;
            if(t.IsArray) {
                if(t.GetElementType().IsArray) {
                    GUI.skin.label.wordWrap = true;
                    rect.height = 40f;
                    height_field += rect.height;
                    GUI.Label(rect, "Multi-dimensional arrays are currently unsupported.");
                }
                if(!arrayFieldFoldout.ContainsKey(id)) arrayFieldFoldout.Add(id, true);
                Rect rectArrayFoldout = new Rect(rect.x, rect.y + 3f, 15f, 15f);
                if(GUI.Button(rectArrayFoldout, "", "label")) arrayFieldFoldout[id] = !arrayFieldFoldout[id];
                GUI.DrawTexture(rectArrayFoldout, (arrayFieldFoldout[id] ? GUI.skin.GetStyle("GroupElementFoldout").normal.background : GUI.skin.GetStyle("GroupElementFoldout").active.background));
                Rect rectLabelArrayName = new Rect(rectArrayFoldout.x + rectArrayFoldout.width + margin, rect.y, rect.width - rectArrayFoldout.width - margin, rect.height);
                GUI.Label(rectLabelArrayName, name);
                height_field += rectLabelArrayName.height;
                if(arrayFieldFoldout[id]) {
                    EventParameter parameter = data as EventParameter;

                    // show elements if folded out
                    if(parameter.lsArray.Count <= 0) {
                        aData.RegisterTakesUndo("Change Event Key Field", false);
                        EventData a = new EventData();
                        a.setValueType(t.GetElementType());
                        parameter.lsArray.Add(a);
                    }
                    Rect rectElement = new Rect(rect);
                    rectElement.y += rect.height + margin;
                    for(int i = 0; i < parameter.lsArray.Count; i++) {
                        float prev_height = height_field;
                        showFieldFor(iTarget, eKey, rectElement, id + "_" + i, "(" + i.ToString() + ")", parameter.lsArray[i], t.GetElementType(), (level + 1), ref height_field);
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
                        aData.RegisterTakesUndo("Change Event Key Field", false);
                        parameter.lsArray.RemoveAt(parameter.lsArray.Count - 1);
                    }
                    Rect rectButtonAddElement = new Rect(rectButtonRemoveElement);
                    rectButtonAddElement.x += rectButtonRemoveElement.width + margin;
                    GUI.enabled = !isPlaying;
                    if(GUI.Button(rectButtonAddElement, "+")) {
                        aData.RegisterTakesUndo("Change Event Key Field", false);
                        EventData a = new EventData();
                        a.setValueType(t.GetElementType());
                        parameter.lsArray.Add(a);
                    }
                }
            }
            else if(t == typeof(bool)) {
                // bool field
                height_field += 20f;
                var val = EditorGUI.Toggle(rect, name, data.val_bool);
                if(data.val_bool != val) { aData.RegisterTakesUndo("Change Event Key Field", false); data.val_bool = val; }
            }
            else if((t == typeof(int)) || (t == typeof(long))) {
                // int field
                height_field += 20f;
                var val = EditorGUI.IntField(rect, name, data.val_int);
                if(data.val_int != val) { aData.RegisterTakesUndo("Change Event Key Field", false); data.val_int = val; }
            }
            else if((t == typeof(float)) || (t == typeof(double))) {
                // float field
                height_field += 20f;
                var val = EditorGUI.FloatField(rect, name, data.val_float);
                if(data.val_float != val) { aData.RegisterTakesUndo("Change Event Key Field", false); data.val_float = val; }
            }
            else if(t == typeof(Vector2)) {
                // vector2 field
                height_field += 40f;
                var val = EditorGUI.Vector2Field(rect, name, data.val_vect2);
                if(data.val_vect2 != val) { aData.RegisterTakesUndo("Change Event Key Field", false); data.val_vect2 = val; }
            }
            else if(t == typeof(Vector3)) {
                // vector3 field
                height_field += 40f;
                var val = EditorGUI.Vector3Field(rect, name, data.val_vect3);
                if(data.val_vect3 != val) { aData.RegisterTakesUndo("Change Event Key Field", false); data.val_vect3 = val; }
            }
            else if(t == typeof(Vector4)) {
                // vector4 field
                height_field += 40f;
                var val = EditorGUI.Vector4Field(rect, name, data.val_vect4);
                if(data.val_vect4 != val) { aData.RegisterTakesUndo("Change Event Key Field", false); data.val_vect4 = val; }
            }
            else if(t == typeof(Color)) {
                // color field
                height_field += 40f;
                var val = EditorGUI.ColorField(rect, name, data.val_color);
                if(data.val_color != val) { aData.RegisterTakesUndo("Change Event Key Field", false); data.val_color = val; }
            }
            else if(t == typeof(Rect)) {
                // rect field
                height_field += 60f;
                var val = EditorGUI.RectField(rect, name, data.val_rect);
                if(data.val_rect != val) { aData.RegisterTakesUndo("Change Event Key Field", false); data.val_rect = val; }
            }
            else if(t == typeof(string)) {
                height_field += 20f;
                // set default
                if(data.val_string == null) data.val_string = "";
                // string field
                var val = EditorGUI.TextField(rect, name, data.val_string);
                if(data.val_string != val) { aData.RegisterTakesUndo("Change Event Key Field", false); data.val_string = val; }
            }
            else if(t == typeof(char)) {
                height_field += 20f;
                // set default
                if(data.val_string == null) data.val_string = "";
                // char (string) field
                Rect rectLabelCharField = new Rect(rect.x, rect.y, 146f, rect.height);
                GUI.Label(rectLabelCharField, name);
                Rect rectTextFieldChar = new Rect(rectLabelCharField.x + rectLabelCharField.width + margin, rectLabelCharField.y, rect.width - rectLabelCharField.width - margin, rect.height);
                var val = GUI.TextField(rectTextFieldChar, data.val_string, 1);
                if(data.val_string != val) { aData.RegisterTakesUndo("Change Event Key Field", false); data.val_string = val; }
            }
            else if(t == typeof(GameObject)) {
                height_field += 40f + margin;
                // label
                Rect rectLabelField = new Rect(rect);
                GUI.Label(rectLabelField, name);
                // GameObject field
                GUI.skin = null;
                Rect rectObjectField = new Rect(rect.x, rectLabelField.y + rectLabelField.height + margin, rect.width, 16f);

                GameObject curGO = data.toObject(iTarget) as GameObject;
                GameObject newGO = EditorGUI.ObjectField(rectObjectField, curGO, typeof(GameObject), true) as GameObject;

                if(curGO != newGO) {
                    aData.RegisterTakesUndo("Change Event Key Field", false);

                    if(newGO) {
                        data.SetAsGameObject(iTarget, newGO, !newGO.scene.IsValid()); //if scene is not valid, it must be from the asset
                    }
                    else
                        data.SetAsGameObject(iTarget, null, false);
                }

                GUI.skin = skin;
            }
            else if(Utility.IsDerivedFrom(t, typeof(Component))) {
                height_field += 40f + margin;
                // label
                Rect rectLabelField = new Rect(rect);
                GUI.Label(rectLabelField, name);
                GUI.skin = null;
                Rect rectObjectField = new Rect(rect.x, rectLabelField.y + rectLabelField.height + margin, rect.width, 16f);
                EditorUtility.ResetDisplayControls();

                UnityEngine.Object curObj = data.toObject(iTarget) as UnityEngine.Object;
                UnityEngine.Object newObj = EditorGUI.ObjectField(rectObjectField, curObj, t, true);

                if(curObj != newObj) {
                    aData.RegisterTakesUndo("Change Event Key Field", false);

                    Component newComp = newObj as Component;
                    if(newComp) {
                        data.SetAsComponent(iTarget, newComp, !newComp.gameObject.scene.IsValid()); //if scene is not valid, it must be from the asset
                    }
                    else
                        data.SetAsComponent(iTarget, null, false);
                }

                GUI.skin = skin;
                EditorUtility.ResetDisplayControls();
                //return;
            }
            else if(t == typeof(UnityEngine.Object)) {
                height_field += 40f + margin;
                Rect rectLabelField = new Rect(rect);
                GUI.Label(rectLabelField, name);
                Rect rectObjectField = new Rect(rect.x, rectLabelField.y + rectLabelField.height + margin, rect.width, 16f);
                GUI.skin = null;
                EditorUtility.ResetDisplayControls();
                var curVal = data.toObject(iTarget) as UnityEngine.Object;
                var newVal = EditorGUI.ObjectField(rectObjectField, curVal, typeof(UnityEngine.Object), true);
                if(curVal != newVal) {
                    aData.RegisterTakesUndo("Change Event Key Field", false);
                    if(newVal is GameObject) { //special case if Object is GameObject (in case it is from scene)
                        if(newVal)
                            data.SetAsGameObject(iTarget, (GameObject)newVal, !((GameObject)newVal).scene.IsValid()); //if scene is not valid, it must be from the asset
                        else
                            data.SetAsGameObject(iTarget, null, false);
                    }
                    else if(newVal is Component) { //special case if Object is Component (in case it is from scene)
                        if(newVal)
                            data.SetAsComponent(iTarget, (Component)newVal, !((Component)newVal).gameObject.scene.IsValid()); //if scene is not valid, it must be from the asset
                        else
                            data.SetAsComponent(iTarget, null, false);
                    }
                    else
                        data.val_obj = newVal;
                }
                GUI.skin = skin;
                EditorUtility.ResetDisplayControls();
            }
            else if(t.IsEnum) {
                height_field += 40f + margin;
                // label
                Rect rectLabelField = new Rect(rect);
                GUI.Label(rectLabelField, name);
                Rect rectObjectField = new Rect(rect.x, rectLabelField.y + rectLabelField.height + margin, rect.width, 16f);
                var val = EditorGUI.EnumPopup(rectObjectField, data.val_enum);
                if(data.val_enum != val) { aData.RegisterTakesUndo("Change Event Key Field", false); data.val_enum = val; }
            }
            else {
                height_field += 20f;
                GUI.skin.label.wordWrap = true;
                GUI.Label(rect, "Unsupported parameter type " + t.ToString() + ".");
                GUI.skin.label.wordWrap = false;
            }
        }

        void showObjectFieldFor(Track amTrack, float width_track, Rect rect) {
            if(rect.width < 22f) return;
            // show object field for track, used in OnGUI. Needs to be updated for every track type.
            GUI.skin = null;
            EditorUtility.ResetDisplayControls();
            // add objectfield for every track type
            // translation/rotation
            if(amTrack is TranslationTrack || amTrack is RotationTrack || amTrack is RotationEulerTrack || amTrack is ScaleTrack) {
                Transform nt = (Transform)EditorGUI.ObjectField(rect, amTrack.GetTarget(aData.target), typeof(Transform), true/*,GUILayout.Width (width_track-padding_track*2)*/);
                if(!amTrack.isTargetEqual(aData.target, nt)) {
                    aData.RegisterTakesUndo("Set Transform", false);
                    amTrack.SetTarget(aData.target, nt);
                }
            }
            // orient
            else if(amTrack is OrientationTrack) {
                OrientationTrack otrack = amTrack as OrientationTrack;
                Transform nt = (Transform)EditorGUI.ObjectField(rect, amTrack.GetTarget(aData.target), typeof(Transform), true);
                if(!otrack.isTargetEqual(aData.target, nt)) {
                    aData.RegisterTakesUndo("Set Transform", false);
                    otrack.SetTarget(aData.target, nt);
                    otrack.updateCache(aData.target);
                }
            }
            // animation/go active
            else if(amTrack is UnityAnimationTrack || amTrack is GOSetActiveTrack) {
                GameObject nobj = (GameObject)EditorGUI.ObjectField(rect, amTrack.GetTarget(aData.target), typeof(GameObject), true);
                if(nobj && amTrack is UnityAnimationTrack && nobj.GetComponent<Animation>() == null) {
                    UnityEditor.EditorUtility.DisplayDialog("No Animation Component", "You must add an Animation component to the GameObject before you can use it in an Animation Track.", "Okay");
                }
                else if(!amTrack.isTargetEqual(aData.target, nobj)) {
                    aData.RegisterTakesUndo("Set GameObject", false);
                    amTrack.SetTarget(aData.target, nobj ? nobj.transform : null);
                }
            }
            // audio
            else if(amTrack is AudioTrack) {
                AudioSource nsrc = (AudioSource)EditorGUI.ObjectField(rect, amTrack.GetTarget(aData.target), typeof(AudioSource), true);
                if(!amTrack.isTargetEqual(aData.target, nsrc)) {
                    aData.RegisterTakesUndo("Set Audio Source", false);
                    if(nsrc != null) nsrc.playOnAwake = false;
                    amTrack.SetTarget(aData.target, nsrc ? nsrc.transform : null);
                }
            }
            // property
            else if(amTrack is PropertyTrack) {
                GameObject ngo = (GameObject)EditorGUI.ObjectField(rect, amTrack.GetTarget(aData.target), typeof(GameObject), true);
                if(!amTrack.isTargetEqual(aData.target, ngo)) {
                    bool changeGO = true;

                    //check if new target has the required components
                    bool componentsMatch = amTrack.VerifyComponents(ngo);

                    if(!componentsMatch && (amTrack.keys.Count > 0) && (!UnityEditor.EditorUtility.DisplayDialog("Data Will Be Lost", "Certain keyframes on track '" + amTrack.name + "' will be removed if you continue.", "Continue Anway", "Cancel"))) {
                        changeGO = false;
                    }
                    if(changeGO) {
                        aData.RegisterTakesUndo("Set GameObject", false);

                        if(!componentsMatch) {
                            if(amTrack is PropertyTrack) { //remove all keys if required component is missing
                                if(amTrack.keys.Count > 0) {
                                    amTrack.keys = new List<Key>();
                                    ((PropertyTrack)amTrack).clearInfo();
                                }
                            }
                        }

                        amTrack.SetTarget(aData.target, ngo ? ngo.transform : null);

                        amTrack.updateCache(aData.target);
                    }
                }
            }
            //Event
            else if(amTrack is EventTrack) {
                var eventTrack = (EventTrack)amTrack;
                var prevObj = amTrack.GetTarget(aData.target);
                var nobj = EditorGUI.ObjectField(rect, prevObj, typeof(UnityEngine.Object), true);
                if(!amTrack.isTargetEqual(aData.target, nobj)) {
                    bool changeObj = true;

                    //check if new target has the required components
                    var go = nobj as GameObject;
                    var comp = nobj as Component;
                    bool isMatched = prevObj && nobj && prevObj.GetType() == nobj.GetType();

                    if(!isMatched && (amTrack.keys.Count > 0) && (!UnityEditor.EditorUtility.DisplayDialog("Data Will Be Lost", "Certain keyframes on track '" + amTrack.name + "' will be removed if you continue.", "Continue Anway", "Cancel"))) {
                        changeObj = false;
                    }
                    if(changeObj) {
                        aData.RegisterTakesUndo("Set Event Target", false);

                        if(!isMatched) {
                            //delete keys
                            amTrack.keys = new List<Key>();
                        }

                        if(comp)
                            eventTrack.SetTargetAsComponent(aData.target, comp.transform, comp);
                        else if(go)
                            eventTrack.SetTargetAsGameObject(aData.target, go);
                        else
                            eventTrack.SetTargetAsObject(nobj);

                        amTrack.updateCache(aData.target);
                    }
                }
            }
            //Material
            else if(amTrack is MaterialTrack) {
                Renderer render = (Renderer)EditorGUI.ObjectField(rect, amTrack.GetTarget(aData.target), typeof(Renderer), true);
                if(!amTrack.isTargetEqual(aData.target, render)) {
                    aData.RegisterTakesUndo("Set Renderer", false);
                    amTrack.SetTarget(aData.target, render ? render.transform : null);
                }
            }

            GUI.skin = skin;
            EditorUtility.ResetDisplayControls();
        }
        void showAlertMissingObjectType(string type) {
            UnityEditor.EditorUtility.DisplayDialog("Missing " + type, "You must add a " + type + " to the track before you can add keys.", "Okay");
        }
        void showTransitionPicker(Track track, CameraSwitcherKey key, float x = -1f, float y = -1f, float width = -1f) {
            if(x >= 0f && y >= 0f && width >= 0f) {
                width--;
                float height = 22f;
                Rect rectLabel = new Rect(x, y - 1f, 40f, height);
                int index = 0;

                GUI.Label(rectLabel, "Fade");
                Rect rectPopup = new Rect(rectLabel.x + rectLabel.width + 2f, y + 3f, width - rectLabel.width - width_button_delete - 3f, height);
                for(int i = 0; i < TransitionPicker.TransitionOrder.Length; i++) {
                    if(TransitionPicker.TransitionOrder[i] == key.cameraFadeType) {
                        index = i;
                        break;
                    }
                }
                int newIndex = EditorGUI.Popup(rectPopup, index, TransitionPicker.TransitionNames);
                if(key.cameraFadeType != TransitionPicker.TransitionOrder[newIndex]) {
                    aData.RegisterTakesUndo("Set Camera Track Fade Type", false);
                    key.cameraFadeType = TransitionPicker.TransitionOrder[newIndex];
                    // reset parameters
                    TransitionPicker.setDefaultParametersForKey(ref key);
                    // update cache when modifying variables
                    track.updateCache(aData.target);
                    // preview current frame
                    aData.currentTake.previewFrame(aData.target, aData.currentTake.selectedFrame);
                    // refresh values
                    TransitionPicker.refreshValues();
                }
                Rect rectButton = new Rect(width - width_button_delete + 1f, y, width_button_delete, width_button_delete);
                if(GUI.Button(rectButton, getSkinTextureStyleState("popup").background, GUI.skin.GetStyle("ButtonImage"))) {
                    TransitionPicker.setValues(key, track);
                    EditorWindow.GetWindow(typeof(TransitionPicker));
                }
            }
            else {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                GUILayout.Space(1f);
                GUILayout.Label("Fade");
                GUILayout.EndVertical();
                GUILayout.BeginVertical();
                GUILayout.Space(3f);
                int index = 0;
                for(int i = 0; i < TransitionPicker.TransitionOrder.Length; i++) {
                    if(TransitionPicker.TransitionOrder[i] == key.cameraFadeType) {
                        index = i;
                        break;
                    }
                }
                int newIndex = EditorGUILayout.Popup(index, TransitionPicker.TransitionNames);
                if(key.cameraFadeType != TransitionPicker.TransitionOrder[newIndex]) {
                    aData.RegisterTakesUndo("Set Camera Track Fade Type", false);
                    key.cameraFadeType = TransitionPicker.TransitionOrder[newIndex];
                    // reset parameters
                    TransitionPicker.setDefaultParametersForKey(ref key);
                    // update cache when modifying variables
                    track.updateCache(aData.target);
                    // preview current frame
                    aData.currentTake.previewFrame(aData.target, aData.currentTake.selectedFrame);
                    // refresh values
                    TransitionPicker.refreshValues();
                }

                GUILayout.EndVertical();
                if(GUILayout.Button(getSkinTextureStyleState("popup").background, GUI.skin.GetStyle("ButtonImage"), GUILayout.Width(width_button_delete), GUILayout.Height(width_button_delete))) {
                    TransitionPicker.setValues(key, track);
                    EditorWindow.GetWindow(typeof(TransitionPicker));
                }
                GUILayout.Space(1f);
                GUILayout.EndHorizontal();
            }
        }
        public static bool showEasePicker(Track track, Key key, AnimateEditControl aData, float x = -1f, float y = -1f, float width = -1f) {
            bool didUpdate = false;
            if(x >= 0f && y >= 0f && width >= 0f) {
                width--;
                float height = 22f;
                Rect rectLabel = new Rect(x, y - 1f, 40f, height);
                GUI.Label(rectLabel, "Ease");
                Rect rectPopup = new Rect(rectLabel.x + rectLabel.width + 2f, y + 3f, width - rectLabel.width - width_button_delete - 3f, height);

                int easeInd = (int)key.easeType;
                int nease = GetEaseIndex(EditorGUI.Popup(rectPopup, GetEaseTypeNameIndex(key.easeType), easeTypeNames));
                if(easeInd != nease) {
                    aData.RegisterTakesUndo("Change Ease", false);
                    key.setEaseType((Ease)nease);
                    // update cache when modifying varaibles
                    track.updateCache(aData.target);
                    // preview new position
                    window.aData.currentTake.previewFrame(aData.target, window.aData.currentTake.selectedFrame);
                    // refresh component
                    didUpdate = true;
                    // refresh values
                    EasePicker.refreshValues();
                }

                Rect rectButton = new Rect(width - width_button_delete + 1f, y, width_button_delete, width_button_delete);
                if(GUI.Button(rectButton, getSkinTextureStyleState("popup").background, GUI.skin.GetStyle("ButtonImage"))) {
                    EasePicker.setValues(/*aData,*/key, track);
                    EditorWindow.GetWindow(typeof(EasePicker));
                }

                //display specific variable for certain tweens
                //TODO: only show this for specific tweens
                if(!key.hasCustomEase()) {
                    y += rectButton.height + 4;
                    Rect rectAmp = new Rect(x, y, 200f, height);

                    float namp = EditorGUI.FloatField(rectAmp, "Amplitude", key.amplitude);
                    if(key.amplitude != namp) {
                        aData.RegisterTakesUndo("Change Amplitude", false);
                        key.amplitude = namp;
                    }

                    y += rectAmp.height + 4;
                    Rect rectPer = new Rect(x, y, 200f, height);
                    float nperiod = EditorGUI.FloatField(rectPer, "Period", key.period);
                    if(key.period != nperiod) {
                        aData.RegisterTakesUndo("Change Period", false);
                        key.period = nperiod;
                    }
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
                int easeInd = (int)key.easeType;
                int nease = GetEaseIndex(EditorGUILayout.Popup(GetEaseTypeNameIndex(key.easeType), easeTypeNames));
                if(easeInd != nease) {
                    aData.RegisterTakesUndo("Change Ease", false);
                    key.setEaseType((Ease)nease);

                    window._dirtyTrackUpdate(window.aData.currentTake, track);

                    // refresh component
                    didUpdate = true;
                    // refresh values
                    EasePicker.refreshValues();
                }
                GUILayout.EndVertical();
                if(GUILayout.Button(getSkinTextureStyleState("popup").background, GUI.skin.GetStyle("ButtonImage"), GUILayout.Width(width_button_delete), GUILayout.Height(width_button_delete))) {
                    EasePicker.setValues(/*aData,*/key, track);
                    EditorWindow.GetWindow(typeof(EasePicker));
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
            int _startFrame = (int)TakeEditCurrent().startFrame;
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
                    if(mouseOverElement == (int)ElementType.Group) timelineSelectGroup((int)mouseOverGroupElement.x);
                    else timelineSelectTrack((int)mouseOverGroupElement.y);
                }
                #endregion
                #region frame
                // if dragged from frame
                else if(mouseOverFrame != 0) {
                    // change track if necessary
                    if(!TakeEditCurrent().contextSelectionTrackIds.Contains(mouseOverTrack) && TakeEditCurrent().selectedTrack != mouseOverTrack) timelineSelectTrack(mouseOverTrack);

                    // if dragged from selected frame, move
                    if(mouseOverSelectedFrame) {
                        dragType = (int)DragType.MoveSelection;
                        TakeEditCurrent().setGhostSelection();
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
                    if(TakeEditCurrent().selectedTrack != mouseOverTrack) timelineSelectTrack(mouseOverTrack);
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
                    startFrame = TakeEditCurrent().startFrame;
                    startScrollViewValue = scrollViewValue.y;
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
                    aData.RegisterTakesUndo("Move Keys", false);
                    
                    Take take = aData.currentTake;

                    //Key[] dKeys = 
                    TakeEdit(take).offsetContextSelectionFramesBy(take, aData.target, endDragFrame - startDragFrame);

                    // preview selected frame
                    take.previewFrame(aData.target, take.selectedFrame);

                    TakeEdit(take).ghostSelection.Clear();
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
                    aData.RegisterTakesUndo("Move Element", false);
                    processDropGroupElement(draggingGroupElementType, draggingGroupElement, mouseOverElement, mouseOverGroupElement);
                }
                else if(dragType == (int)DragType.ResizeAction) {
                    aData.RegisterTakesUndo("Resize Action", false);
                    Track _track = TakeEditCurrent().getSelectedTrack(aData.currentTake);
                    //Key[] dKeys = 
                    _track.removeDuplicateKeys();
                    
                    // preview selected frame
                    aData.currentTake.previewFrame(aData.target, aData.currentTake.selectedFrame);
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
                        TakeEditCurrent().offsetGhostSelectionBy(endDragFrame - ghostStartDragFrame);
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
                    if(frame < (int)TakeEditCurrent().startFrame) frame = (int)TakeEditCurrent().startFrame;
                    else if(frame > (int)TakeEditCurrent().endFrame) frame = (int)TakeEditCurrent().endFrame;
                    selectFrame(frame);
                    #endregion
                    #region resize action
                    // resize action
                }
                else if(dragType == (int)DragType.ResizeAction && mouseXOverFrame > 0) {
                    Track selTrack = TakeEditCurrent().getSelectedTrack(aData.currentTake);
                    if(selTrack.hasKeyOnFrame(resizeActionFrame)) {
                        Key selKey = selTrack.getKeyOnFrame(resizeActionFrame);
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
                                            if(arrKeysLeft[i].frame >= resizeActionFrame) arrKeysLeft[i].frame = resizeActionFrame - 1;         // after last
                                            lastFrame = arrKeysLeft[i].frame;
                                        }

                                        // update right
                                        lastFrame = resizeActionFrame;
                                        for(int i = 0; i < arrKeysRight.Length; i++) {
                                            arrKeysRight[i].frame = Mathf.FloorToInt((endResizeActionFrame - resizeActionFrame) * arrKeyRatiosRight[i] + resizeActionFrame);
                                            if(arrKeysRight[i].frame <= lastFrame) {
                                                arrKeysRight[i].frame = lastFrame + 1;
                                            }
                                            if(arrKeysRight[i].frame >= endResizeActionFrame) arrKeysRight[i].frame = endResizeActionFrame - 1; // after last
                                            lastFrame = arrKeysRight[i].frame;
                                        }
                                    }
                                    // update cache
                                    selTrack.updateCache(aData.target);
                                }
                            }
                        }
                    }
                    #endregion
                    #region resize horizontal scrollbar left
                }
                else if(dragType == (int)DragType.ResizeHScrollbarLeft) {
                    int numFrames = GetCurTakeNumFrames();
                    if(mouseXOverHScrollbarFrame <= 0) TakeEditCurrent().startFrame = 1;
                    else if(mouseXOverHScrollbarFrame > numFrames) TakeEditCurrent().startFrame = numFrames;
                    else TakeEditCurrent().startFrame = mouseXOverHScrollbarFrame;
                    #endregion
                    #region resize horizontal scrollbar right
                }
                else if(dragType == (int)DragType.ResizeHScrollbarRight) {
                    int numFrames = GetCurTakeNumFrames();
                    if(mouseXOverHScrollbarFrame <= 0) TakeEditCurrent().endFrame = 1;
                    else if(mouseXOverHScrollbarFrame > numFrames) TakeEditCurrent().endFrame = numFrames;
                    else TakeEditCurrent().endFrame = mouseXOverHScrollbarFrame;
                    int min = Mathf.FloorToInt((position.width - width_track - 18f - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed)) / (height_track - height_action_min));
                    if(TakeEditCurrent().startFrame > TakeEditCurrent().endFrame - min) TakeEditCurrent().startFrame = TakeEditCurrent().endFrame - min;
                    #endregion
                    #region cursor zoom
                }
                else if(dragType == (int)DragType.CursorZoom) {
                    if(dragPan) {
                        if(didPeakZoom) {
                            if(wasZoomingIn && currentMousePosition.x <= cachedZoomMousePosition.x) {
                                startFrame = TakeEditCurrent().startFrame;
                                zoomDirectionMousePosition.x = currentMousePosition.x;
                            }
                            else if(!wasZoomingIn && currentMousePosition.x >= cachedZoomMousePosition.x) {
                                startFrame = TakeEditCurrent().startFrame;
                                zoomDirectionMousePosition.x = currentMousePosition.x;
                            }
                            didPeakZoom = false;
                        }
                        float frameDiff = TakeEditCurrent().endFrame - TakeEditCurrent().startFrame;
                        float framesInView = (TakeEditCurrent().endFrame - TakeEditCurrent().startFrame);
                        float areaWidth = position.width - width_track - (aData.isInspectorOpen ? width_inspector_open - 4f : width_inspector_closed) - 21f;
                        float currFrame = startFrame + (zoomDirectionMousePosition.x - currentMousePosition.x) * (framesInView / areaWidth);
                        if(currFrame <= 1) {
                            wasZoomingIn = false;
                            cachedZoomMousePosition = currentMousePosition;
                            currFrame = 1f;
                            didPeakZoom = true;
                        }
                        int numFrames = GetCurTakeNumFrames();
                        if(currFrame + frameDiff > numFrames) {
                            wasZoomingIn = true;
                            cachedZoomMousePosition = currentMousePosition;
                            zoomDirectionMousePosition.x = currentMousePosition.x;
                            currFrame = numFrames - frameDiff;
                            didPeakZoom = true;
                        }

                        TakeEditCurrent().startFrame = currFrame;
                        TakeEditCurrent().endFrame = TakeEditCurrent().startFrame + frameDiff;
                        scrollViewValue.y = startScrollViewValue + (zoomDirectionMousePosition.y - currentMousePosition.y);
                    }
                    else {
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
                }
                #endregion
            }
            #endregion
        }
        void processUpdateMethodInfoCache(bool now = false) {
            if(now) updateMethodInfoCacheBuffer = 0;
            // update methodinfo cache if necessary
            if(aData == null || aData.currentTake == null) return;
            if(aData.currentTake.getTrackCount() <= 0) return;
            if(TakeEditCurrent().selectedTrack <= -1) return;
            if(updateMethodInfoCacheBuffer > 0) updateMethodInfoCacheBuffer--;
            if((indexMethodInfo == -1) && (updateMethodInfoCacheBuffer <= 0)) {

                // if track is event
                Track selectedTrack = TakeEditCurrent().getSelectedTrack(aData.currentTake);
                if(TakeEditCurrent().selectedTrack > -1 && selectedTrack is EventTrack) {
                    // if event track has key on selected frame
                    if(selectedTrack.hasKeyOnFrame(aData.currentTake.selectedFrame)) {
                        // update methodinfo cache
                        updateCachedMethodInfoFromObject(selectedTrack.GetTarget(aData.target));
                        updateMethodInfoCacheBuffer = updateRateMethodInfoCache;
                        // set index to method info index

                        if(cachedMethodInfo.Count > 0) {
                            EventKey eKey = (selectedTrack.getKeyOnFrame(aData.currentTake.selectedFrame) as EventKey);
                            MethodInfo m = eKey.getMethodInfo(selectedTrack.GetTarget(aData.target));
                            if(m != null) {
                                for(int i = 0; i < cachedMethodInfo.Count; i++) {
                                    if(cachedMethodInfo[i] == m) {
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
                int numFrames = GetCurTakeNumFrames();
                if(TakeEditCurrent().endFrame < numFrames) {
                    TakeEditCurrent().startFrame += speed;
                    TakeEditCurrent().endFrame += speed;
                    if(ticker % 2 == 0) handDragAccelaration--;
                }
                else {
                    handDragAccelaration = 0;
                }
            }
            else if(handDragAccelaration < 0) {
                if(TakeEditCurrent().startFrame > 1f) {
                    TakeEditCurrent().startFrame -= speed;
                    TakeEditCurrent().endFrame -= speed;
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
                    aData.currentTake.moveToGroup((int)sourceElement.y, (int)destElement.x, true);
                }
                else if(sourceType == (int)ElementType.Group) {
                    // drop group on group
                    if((int)sourceElement.x != (int)destElement.x)
                        aData.currentTake.moveToGroup((int)sourceElement.x, (int)destElement.x, true);
                }
                // dropped outside group
            }
            else if(destType == (int)ElementType.GroupOutside) {
                if(sourceType == (int)ElementType.Track) {
                    // drop track on group

                    aData.currentTake.moveGroupElement((int)sourceElement.y, (int)destElement.x);
                }
                else if(sourceType == (int)ElementType.Group) {
                    // drop group on group
                    //int destGroup = aData.getCurrentTake().getElementGroup((int)destElement.x);
                    //if((int)sourceElement.x != destGroup)
                    //aData.getCurrentTake().moveToGroup((int)sourceElement.x,destGroup);
                    if((int)sourceElement.x != (int)destElement.x)
                        aData.currentTake.moveGroupElement((int)sourceElement.x, (int)destElement.x);

                }
                // dropped on track
            }
            else if(destType == (int)ElementType.Track) {
                if(sourceType == (int)ElementType.Track) {
                    // drop track on track
                    if((int)sourceElement.y != (int)destElement.y)
                        aData.currentTake.moveToGroup((int)sourceElement.y, (int)destElement.x, false, (int)destElement.y);
                }
                else if(sourceType == (int)ElementType.Group) {
                    // drop group on track
                    if((int)destElement.x == 0 || (int)sourceElement.x != (int)destElement.x)
                        aData.currentTake.moveToGroup((int)sourceElement.x, (int)destElement.x, false, (int)destElement.y);
                }
            }
            else {
                // drop on window, move to root group
                if(sourceType == (int)ElementType.Track) {
                    aData.currentTake.moveToGroup((int)sourceElement.y, 0, mouseAboveGroupElements);
                }
                else if(sourceType == (int)ElementType.Group) {
                    // move group to last position
                    aData.currentTake.moveToGroup((int)sourceElement.x, 0, mouseAboveGroupElements);
                }
            }
            // re-select track to update selected group
            if(sourceType == (int)ElementType.Track) timelineSelectTrack((int)sourceElement.y);
            // scroll to the track
            float scrollTo = -1f;
            scrollTo = aData.currentTake.getElementY((sourceType == (int)ElementType.Group ? (int)sourceElement.x : (int)sourceElement.y), height_track, height_track_foldin, height_group);
            setScrollViewValue(scrollTo);
        }
        public static void recalculateNumFramesToRender() {
            if(window) window.cachedZoom = -1f;
        }
        void calculateNumFramesToRender(bool clickedZoom, Event e) {
            if(aData.currentTake == null) return;
            int numFrames = GetCurTakeNumFrames();
            int min = Mathf.FloorToInt((position.width - width_track - 18f - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed)) / (oData.disableTimelineActions ? height_track / 2f : height_track - height_action_min));
            int _mouseXOverFrame = (int)TakeEditCurrent().startFrame + Mathf.CeilToInt((e.mousePosition.x - width_track) / current_width_frame) - 1;
            // move frames with hand cursor
            if(dragType == (int)DragType.CursorHand && !justStartedHandGrab) {
                if(_mouseXOverFrame != startScrubFrame) {
                    float fNumFrames = TakeEditCurrent().endFrame - TakeEditCurrent().startFrame;
                    float dist_hand_drag = startScrubFrame - _mouseXOverFrame;
                    TakeEditCurrent().startFrame += dist_hand_drag;
                    TakeEditCurrent().endFrame += dist_hand_drag;
                    if(TakeEditCurrent().startFrame < 1f) {
                        TakeEditCurrent().startFrame = 1f;
                        TakeEditCurrent().endFrame += fNumFrames - (TakeEditCurrent().endFrame - TakeEditCurrent().startFrame);
                    }
                    else if(TakeEditCurrent().endFrame > numFrames) {
                        TakeEditCurrent().endFrame = numFrames;
                        TakeEditCurrent().startFrame -= fNumFrames - (TakeEditCurrent().endFrame - TakeEditCurrent().startFrame);
                    }
                }
                // calculate the number of frames to render based on zoom
            }
            else if(aData.zoom != cachedZoom && dragType != (int)DragType.ResizeHScrollbarLeft && dragType != (int)DragType.ResizeHScrollbarRight) {
                //numFramesToRender
                if(oData.scrubby_zoom_slider) numFramesToRender = Mathf.Lerp(0.0f, 1.0f, aData.zoom) * ((float)numFrames - min) + min;
                else numFramesToRender = Utility.GetEasingFunction(Ease.InExpo)(aData.zoom, 1.0f, 0.0f, 0.0f) * ((float)numFrames - min) + min;
                // frame dimensions
                current_width_frame = Mathf.Clamp((position.width - width_track - 18f - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed)) / numFramesToRender, 0f, (oData.disableTimelineActions ? height_track / 2f : height_track - height_action_min));
                current_height_frame = Mathf.Clamp(current_width_frame * 2f, 20f, (oData.disableTimelineActions ? height_track : 40f));
                float half = 0f;
                // zoom out			
                if(TakeEditCurrent().endFrame - TakeEditCurrent().startFrame + 1 < Mathf.FloorToInt(numFramesToRender)) {
                    if((oData.scrubby_zoom_cursor && dragType == (int)DragType.CursorZoom) /*|| (aData.scrubby_zoom_slider && dragType != (int)DragType.CursorZoom)*/) {
                        int newPosFrame = (int)TakeEditCurrent().startFrame + Mathf.CeilToInt((startZoomMousePosition.x - width_track) / current_width_frame) - 1;
                        int _diff = startZoomXOverFrame - newPosFrame;
                        TakeEditCurrent().startFrame += _diff;
                        TakeEditCurrent().endFrame += _diff;
                    }
                    else {

                        half = (((int)numFramesToRender - (TakeEditCurrent().endFrame - TakeEditCurrent().startFrame + 1)) / 2f);
                        TakeEditCurrent().startFrame -= Mathf.FloorToInt(half);
                        TakeEditCurrent().endFrame += Mathf.CeilToInt(half);
                        // clicked zoom out
                        if(clickedZoom) {
                            int newPosFrame = (int)TakeEditCurrent().startFrame + Mathf.CeilToInt((e.mousePosition.x - width_track) / current_width_frame) - 1;
                            int _diff = _mouseXOverFrame - newPosFrame;
                            TakeEditCurrent().startFrame += _diff;
                            TakeEditCurrent().endFrame += _diff;
                        }
                    }
                    // zoom in
                }
                else if(TakeEditCurrent().endFrame - TakeEditCurrent().startFrame + 1 > Mathf.FloorToInt(numFramesToRender)) {
                    //targetPos = ((float)startZoomXOverFrame)/((float)aData.getCurrentTake().endFrame);
                    half = (((TakeEditCurrent().endFrame - TakeEditCurrent().startFrame + 1) - numFramesToRender) / 2f);
                    //float scrubby_startframe = (float)aData.getCurrentTake().startFrame+half;
                    TakeEditCurrent().startFrame += Mathf.FloorToInt(half);
                    TakeEditCurrent().endFrame -= Mathf.CeilToInt(half);
                    int targetFrame = 0;
                    // clicked zoom in
                    if(clickedZoom) {
                        int newPosFrame = (int)TakeEditCurrent().startFrame + Mathf.CeilToInt((e.mousePosition.x - width_track) / current_width_frame) - 1;
                        int _diff = _mouseXOverFrame - newPosFrame;
                        TakeEditCurrent().startFrame += _diff;
                        TakeEditCurrent().endFrame += _diff;
                        // scrubby zoom in
                    }
                    else if((oData.scrubby_zoom_cursor && dragType == (int)DragType.CursorZoom) || (oData.scrubby_zoom_slider && dragType != (int)DragType.CursorZoom)) {
                        if(dragType != (int)DragType.CursorZoom) {
                            // scrubby zoom slider to indicator
                            targetFrame = aData.currentTake.selectedFrame;
                            float dist_scrubbyzoom = Mathf.Round(targetFrame - Mathf.FloorToInt(TakeEditCurrent().startFrame + numFramesToRender / 2f));
                            int offset = Mathf.RoundToInt(dist_scrubbyzoom * (1f - Mathf.Lerp(0.0f, 1.0f, aData.zoom)));
                            TakeEditCurrent().startFrame += offset;
                            TakeEditCurrent().endFrame += offset;
                        }
                        else {
                            // scrubby zoom cursor to mouse position
                            int newPosFrame = (int)TakeEditCurrent().startFrame + Mathf.CeilToInt((startZoomMousePosition.x - width_track) / current_width_frame) - 1;
                            int _diff = startZoomXOverFrame - newPosFrame;
                            TakeEditCurrent().startFrame += _diff;
                            TakeEditCurrent().endFrame += _diff;
                        }
                    }

                }
                // if beyond boundaries, adjust
                int diff = 0;
                if(TakeEditCurrent().endFrame > numFrames) {
                    diff = (int)TakeEditCurrent().endFrame - numFrames;
                    TakeEditCurrent().endFrame -= diff;
                    TakeEditCurrent().startFrame += diff;
                }
                else if(TakeEditCurrent().startFrame < 1) {
                    diff = 1 - (int)TakeEditCurrent().startFrame;
                    TakeEditCurrent().startFrame -= diff;
                    TakeEditCurrent().endFrame += diff;
                }
                if(half * 2 < (int)numFramesToRender) TakeEditCurrent().endFrame++;
                cachedZoom = aData.zoom;
                return;
            }
            // calculates the number of frames to render based on window width
            if(TakeEditCurrent().startFrame < 1) TakeEditCurrent().startFrame = 1;

            if(TakeEditCurrent().endFrame < TakeEditCurrent().startFrame + min) TakeEditCurrent().endFrame = TakeEditCurrent().startFrame + min;
            if(TakeEditCurrent().endFrame > numFrames) TakeEditCurrent().endFrame = numFrames;
            if(TakeEditCurrent().startFrame > TakeEditCurrent().endFrame - min) TakeEditCurrent().startFrame = TakeEditCurrent().endFrame - min;
            numFramesToRender = TakeEditCurrent().endFrame - TakeEditCurrent().startFrame + 1;
            current_width_frame = Mathf.Clamp((position.width - width_track - 18f - (aData.isInspectorOpen ? width_inspector_open : width_inspector_closed)) / numFramesToRender, 0f, (oData.disableTimelineActions ? height_track / 2f : height_track - height_action_min));
            current_height_frame = Mathf.Clamp(current_width_frame * 2f, 20f, (oData.disableTimelineActions ? height_track : 40f));
            if(dragType == (int)DragType.ResizeHScrollbarLeft || dragType == (int)DragType.ResizeHScrollbarRight) {
                if(oData.scrubby_zoom_slider) aData.zoom = Mathf.Lerp(0f, 1f, (numFramesToRender - min) / ((float)numFrames - min));
                else aData.zoom = Utility.EaseInExpoReversed(0f, 1f, (numFramesToRender - min) / ((float)numFrames - min));
                cachedZoom = aData.zoom;
            }
        }

        #endregion

        #region Timeline/Timeline Manipulation

        void timelineSelectTrack(int _track) {
            // select a track from the timeline
            cancelTextEditting();

            if(aData.currentTake.getTrackCount() <= 0) return;

            // select track
            TakeEditCurrent().selectTrack(aData.currentTake, _track, isShiftDown, isControlDown);

            Track track = aData.currentTake.getTrack(_track);

            // check if target's path has been changed, if so, clear cache
            aData.target.MaintainTargetCache(track);

            // set active object
            if(track.GetTarget(aData.target) == null) {
                if(!string.IsNullOrEmpty(track.targetPath)) {
                    Selection.activeObject = aData.gameObject;
                    Debug.LogWarning("Missing target: " + track.GetTargetPath(aData.target));
                }
            }
            else
                timelineSelectObjectFor(track);
        }
        bool timelineSelectGroupOrTrackFromCurrent(int dir) {
            Take take = aData.currentTake;
            TakeEditControl takeEdit = TakeEditCurrent();

            int parentGrpId = 0;
            int id = 0;
            bool proceed = false;

            //Track 
            if(takeEdit.selectedTrack != -1) {
                id = takeEdit.selectedTrack;
                parentGrpId = take.getElementGroup(takeEdit.selectedTrack);
                proceed = true;
            }
            else if(takeEdit.selectedGroup != 0) {
                id = takeEdit.selectedGroup;

                //if we are moving down, just select the first element
                if(dir > 0) {
                    Group grp = take.getGroup(id);
                    if(grp.elements.Count > 0 && grp.foldout) {
                        int nextId = grp.elements[0];
                        if(nextId < 0) //select group
                            timelineSelectGroup(nextId);
                        else
                            timelineSelectTrack(nextId);
                        return true;
                    }
                }

                parentGrpId = take.getElementGroup(takeEdit.selectedGroup);
                proceed = true;
            }

            if(proceed) {
                //Debug.Log("grp: "+parentGrpId);
                Group grp = take.getGroup(parentGrpId);

                int curInd = grp.getItemIndex(id);
                int nextInd = curInd + dir;
                if(curInd != -1) {
                    int nextId;
                    if(nextInd < 0 || nextInd >= grp.elements.Count) {
                        if(parentGrpId == 0) {
                            if(id < 0) {
                                if(dir > 0) {
                                    nextInd = -1;
                                    while(id < 0) {
                                        grp = take.getGroup(id);
                                        if(grp.elements.Count > 0) {
                                            if(grp.foldout) {
                                                nextInd = 0;
                                                break;
                                            }
                                        }
                                        id = take.getElementGroup(id);
                                    }
                                    if(nextInd != -1) {
                                        nextId = grp.elements[nextInd];
                                    }
                                    else
                                        return false;
                                }
                                else
                                    return false;
                            }
                            else
                                return false;
                        }
                        else {
                            if(dir < 0)
                                nextId = parentGrpId;
                            else { //moving down, check the next element up the hierarchy
                                   //see if we can get the first element if id is group
                                if(id < 0 && (grp = take.getGroup(id)).elements.Count > 0 && grp.foldout) {
                                    nextInd = 0;
                                }
                                else {
                                    do {
                                        id = parentGrpId;
                                        parentGrpId = take.getElementGroup(id);
                                        grp = take.getGroup(parentGrpId);
                                        curInd = grp.getItemIndex(id);
                                        if(curInd == -1) break;
                                        nextInd = curInd + dir;
                                    } while(parentGrpId < 0 && nextInd >= grp.elements.Count);
                                    if(curInd == -1 || nextInd >= grp.elements.Count)
                                        return false;
                                }
                                nextId = grp.elements[nextInd];
                            }
                        }
                    }
                    else {
                        nextId = grp.elements[nextInd];
                        if(nextId < 0 && dir < 0) { //if next is a group, see if we can select its last element
                            grp = take.getGroup(nextId);
                            if(grp.elements.Count > 0 && grp.foldout) {
                                nextId = grp.elements[grp.elements.Count - 1];
                            }
                        }
                    }

                    if(nextId < 0) //select group
                        timelineSelectGroup(nextId);
                    else
                        timelineSelectTrack(nextId);
                }
                else
                    proceed = false;
            }

            return proceed;
        }
        void timelineSelectGroup(int group_id) {
            cancelTextEditting();
            TakeEditCurrent().selectGroup(aData.currentTake, group_id, isShiftDown, isControlDown);
            TakeEditCurrent().selectedTrack = -1;
        }
        void timelineSelectFrame(int _track, int _frame, bool deselectKeyboardFocus = true) {
            // select a frame from the timeline
            cancelTextEditting();
            indexMethodInfo = -1;   // reset methodinfo index to update caches
            if(aData.currentTake.getTrackCount() <= 0) return;
            // select frame
            TakeEditCurrent().selectFrame(aData.currentTake, _track, _frame, numFramesToRender, isShiftDown, isControlDown);
            // preview frame
            aData.currentTake.previewFrame(aData.target, _frame);
            // set active object
            if(_track > -1) timelineSelectObjectFor(aData.currentTake.getTrack(_track));
            // deselect keyboard focus
            if(deselectKeyboardFocus)
                GUIUtility.keyboardControl = 0;
        }
        void timelineSelectObjectFor(Track track) {
            // translation/rot/anim/property obj
            if(track.GetType() == typeof(TranslationTrack) || track.GetType() == typeof(RotationTrack) || track.GetType() == typeof(RotationEulerTrack) || track.GetType() == typeof(ScaleTrack)
                || track.GetType() == typeof(UnityAnimationTrack) || track.GetType() == typeof(PropertyTrack)
                || track.GetType() == typeof(MaterialTrack))
                Selection.activeObject = track.GetTarget(aData.target);
        }
        void timelineSelectNextKey() {
            // select next key
            if(aData.currentTake.getTrackCount() <= 0) return;
            if(aData.currentTake.getTrack(TakeEditCurrent().selectedTrack).keys.Count <= 0) return;
            int frame = aData.currentTake.getTrack(TakeEditCurrent().selectedTrack).getKeyFrameAfterFrame(aData.currentTake.selectedFrame);
            if(frame <= -1) return;
            timelineSelectFrame(TakeEditCurrent().selectedTrack, frame);

        }
        void timelineSelectPrevKey() {
            // select previous key
            if(aData.currentTake.getTrackCount() <= 0) return;
            if(aData.currentTake.getTrack(TakeEditCurrent().selectedTrack).keys.Count <= 0) return;
            int frame = aData.currentTake.getTrack(TakeEditCurrent().selectedTrack).getKeyFrameBeforeFrame(aData.currentTake.selectedFrame);
            if(frame <= -1) return;
            timelineSelectFrame(TakeEditCurrent().selectedTrack, frame);

        }
        public void selectFrame(int frame) {
            if(aData.currentTake.selectedFrame != frame) {
                timelineSelectFrame(TakeEditCurrent().selectedTrack, frame, false);
            }
        }
        void playerTogglePlay() {
            GUIUtility.keyboardControl = 0;
            cancelTextEditting();
            // toggle player off if is playing
            if(isPlaying) {
                isPlaying = false;
                // select where stopped
                timelineSelectFrame(TakeEditCurrent().selectedTrack, aData.currentTake.selectedFrame);
                aData.currentTake.Stop(aData.target);
                return;
            }
            // set preview player variables
            playerStartTime = Time.realtimeSinceStartup;
            playerStartedFrame = playerStartFrame = aData.currentTake.selectedFrame;
            // start playing
            isPlaying = true;
            playerCurLoop = 0;
            playerBackward = false;
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
        void setScrubSpeed() {
            scrubSpeed = ((currentMousePosition.x - startScrubMousePosition.x)) / 30;

        }
        void setScrollViewValue(float val) {
            val = Mathf.Clamp(val, 0f, maxScrollView());
            if(val < scrollViewValue.y || val > scrollViewValue.y + position.height - 66f)
                scrollViewValue.y = Mathf.Clamp(val, 0f, maxScrollView());
        }
        // timeline action info
        string getInfoTextForAction(Track _track, Key _key, bool brief, int clamped) {
            int easeInd = (int)_key.easeType;
            int easeTypeNameInd = GetEaseTypeNameIndex(_key.easeType);
            if(easeInd < 0 || easeInd > (int)Ease.INTERNAL_Custom) {
                _key.easeType = Ease.Linear;

                aData.SetTakesDirty(); //don't really want to undo this
            }

            // get text for track type
            #region translation/rotation
            if(_key is TranslationKey || _key is RotationKey || _key is RotationEulerKey || _key is ScaleKey) {
                if(!_key.canTween) { return ""; }
                return easeTypeNames[easeTypeNameInd];
            }
            #endregion
            #region animation
            else if(_key is UnityAnimationKey) {
                if(!(_key as UnityAnimationKey).amClip) return "Not Set";
                return (_key as UnityAnimationKey).amClip.name + "\n" + ((_key as UnityAnimationKey).wrapMode).ToString();
            }
            #endregion
            #region audio
            else if(_key is AudioKey) {
                if(!(_key as AudioKey).audioClip) return "Not Set";
                return (_key as AudioKey).audioClip.name;
            }
            #endregion
            #region property
            else if(_key is PropertyKey) {
                int keyInd = _track.getKeyIndex(_key);
                PropertyTrack propTrack = _track as PropertyTrack;
                PropertyKey propkey = _key as PropertyKey;
                PropertyKey nextKey = keyInd >= 0 && keyInd + 1 < _track.keys.Count ? _track.keys[keyInd + 1] as PropertyKey : null;

                string info = propTrack.getTrackType() + "\n";
                if(propkey.targetsAreEqual(propTrack.valueType, nextKey) || !_key.canTween || !propTrack.canTween) brief = true;
                if(!brief && propkey.endFrame != -1 && _key.canTween) {
                    info += easeTypeNames[easeTypeNameInd] + ": ";
                }
                string detail = propkey.getValueString(propTrack.GetCachedInfoType(aData.target), propTrack.valueType, nextKey, brief); // extra details such as integer values ex. 1 -> 12
                if(detail != null) info += detail;
                return info;
            }
            #endregion
            #region event
            else if(_key is EventKey) {
                if(string.IsNullOrEmpty((_key as EventKey).methodName)) {
                    return "Not Set";
                }
                string txtInfoEvent = (_key as EventKey).methodName;
                // include parameters
                if((_key as EventKey).parameters != null) {
                    txtInfoEvent += "(";
                    for(int i = 0; i < (_key as EventKey).parameters.Count; i++) {
                        if((_key as EventKey).parameters[i] == null) txtInfoEvent += "";
                        else txtInfoEvent += (_key as EventKey).parameters[i].getStringValue();
                        if(i < (_key as EventKey).parameters.Count - 1) txtInfoEvent += ", ";
                    }

                    txtInfoEvent += ")";
                    return txtInfoEvent;
                }
                return (_key as EventKey).methodName;
            }
            #endregion
            #region orientation
            else if(_key is OrientationKey) {
                if(!_key.canTween) { return ""; }

                Transform target = (_key as OrientationKey).GetTarget(aData.target);

                if(!target) return "No Target";

                int indNext = _track.getKeyIndex(_key) + 1;
                Transform targetNext = indNext < _track.keys.Count ? (_track.keys[indNext] as OrientationKey).GetTarget(aData.target) : null;

                string txtInfoOrientation = null;
                if(!targetNext || target == targetNext) {
                    txtInfoOrientation = (_key as OrientationKey).GetTarget(aData.target).name;
                    return txtInfoOrientation;
                }
                txtInfoOrientation = target.name +
                    " -> " + (targetNext ? targetNext.name : "No Target");
                txtInfoOrientation += "\n" + easeTypeNames[easeTypeNameInd];
                return txtInfoOrientation;
            }
            #endregion
            #region camera switcher
            else if(_key is CameraSwitcherKey) {
                if(!(_key as CameraSwitcherKey).hasStartTarget(aData.target)) return "None";
                string txtInfoCameraSwitcher = null;
                if((_key as CameraSwitcherKey).targetsAreEqual(aData.target) || clamped != 0) {
                    txtInfoCameraSwitcher = (_key as CameraSwitcherKey).getStartTargetName(aData.target);
                    return txtInfoCameraSwitcher;
                }
                txtInfoCameraSwitcher = (_key as CameraSwitcherKey).getStartTargetName(aData.target) +
                " -> " + (_key as CameraSwitcherKey).getEndTargetName(aData.target);
                txtInfoCameraSwitcher += "\n" + TransitionPicker.TransitionNamesDict[((_key as CameraSwitcherKey).cameraFadeType > TransitionPicker.TransitionNamesDict.Length ? 0 : (_key as CameraSwitcherKey).cameraFadeType)];
                if((_key as CameraSwitcherKey).cameraFadeType != (int)CameraSwitcherKey.Fade.None) txtInfoCameraSwitcher += ": " + easeTypeNames[GetEaseTypeNameIndex((_key as CameraSwitcherKey).easeType)];
                return txtInfoCameraSwitcher;
            }
            #endregion
            #region goactive
            else if(_key is GOSetActiveKey) {
                return "";
            }
            #endregion
            #region material
            else if(_key is MaterialKey) {
                int keyInd = _track.getKeyIndex(_key);
                MaterialTrack propTrack = _track as MaterialTrack;
                MaterialKey propkey = _key as MaterialKey;
                MaterialKey nextKey = keyInd >= 0 && keyInd + 1 < _track.keys.Count ? _track.keys[keyInd + 1] as MaterialKey : null;

                string info = propTrack.getTrackType() + "\n";
                if(propkey.targetsAreEqual(propTrack.propertyType, nextKey) || !_key.canTween || !propTrack.canTween) brief = true;
                if(!brief && propkey.endFrame != -1 && _key.canTween) {
                    info += easeTypeNames[easeTypeNameInd] + ": ";
                }
                string detail = propkey.getValueString(propTrack.propertyType, nextKey, brief); // extra details such as integer values ex. 1 -> 12
                if(detail != null) info += detail;
                return info;
            }
            #endregion

            return "Unknown";
        }
        public string getMethodInfoSignature(MethodInfo methodInfo) {
            ParameterInfo[] parameters = methodInfo.GetParameters();
            // loop through parameters, add them to signature
            string methodString = methodInfo.Name + " (";
            for(int i = 0; i < parameters.Length; i++) {
                //unsupported param type
                if(EventParameter.GetValueType(parameters[i].ParameterType) == EventData.ValueType.Invalid) return "";

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
        public Texture getTrackIconTexture(Track _track) {
            if(_track is UnityAnimationTrack) return texIconAnimation;
            else if(_track is EventTrack) return texIconEvent;
            else if(_track is PropertyTrack) return texIconProperty;
            else if(_track is TranslationTrack) return texIconTranslation;
            else if(_track is AudioTrack) return texIconAudio;
            else if(_track is RotationTrack || _track is RotationEulerTrack) return texIconRotation;
            else if(_track is OrientationTrack) return texIconOrientation;
            else if(_track is GOSetActiveTrack) return texIconProperty;
            else if(_track is CameraSwitcherTrack) return texIconCameraSwitcher;
            else if(_track is MaterialTrack) return texIconMaterial;
            else if(_track is RotationTrack || _track is ScaleTrack) return texIconScale;

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
            aData.RegisterTakesUndo("Add Target", false);

            Track sTrack = TakeEditCurrent().getSelectedTrack(aData.currentTake);

            OrientationKey oKey = key as OrientationKey;
            // create target
            GameObject target = new GameObject(getNewTargetName());
            Transform t = sTrack.GetTarget(aData.target) as Transform;
            target.transform.parent = aData.transform;
            target.transform.position = t.position + t.forward * 5f;
            target.transform.rotation = Quaternion.identity;
            target.transform.localScale = Vector3.one;
            // set target
            oKey.SetTarget(aData.target, target.transform);

            _dirtyTrackUpdate(aData.currentTake, sTrack);

            // add to translation track
            if(withTranslationTrack) {
                objects_window = new List<GameObject>();
                objects_window.Add(target);
                addTrack((int)SerializeType.Translation);
            }
        }
        void addTrack(object trackType) {
            Take take = aData.currentTake;

            // add one null GameObject if no GameObject dragged onto timeline (when clicked on new track button)
            if(objects_window.Count <= 0) {
                objects_window.Add(null);
            }
            foreach(GameObject object_window in objects_window) {
                addTrackWithGameObject(trackType, object_window);
            }
            objects_window = new List<GameObject>();
            timelineSelectTrack(take.trackCounter);
            // move scrollview to last created track
            setScrollViewValue(take.getElementY(TakeEdit(take).selectedTrack, height_track, height_track_foldin, height_group));
        }

        T _addTrack<T>(GameObject object_window) where T : Track {
            T t = System.Activator.CreateInstance<T>();
            T track = t;
            aData.currentTake.addTrack(TakeEditCurrent().selectedGroup, aData.target, object_window ? object_window.transform : null, track);
            return track;
        }

        void addTrackWithGameObject(object trackType, GameObject object_window) {
            // add track based on index
            switch((SerializeType)trackType) {
                case SerializeType.Translation:
                    TranslationTrack ttrack = _addTrack<TranslationTrack>(object_window);
                    ttrack.pixelPerUnit = oData.pixelPerUnitDefault;
                    break;
                case SerializeType.Rotation:
                    _addTrack<RotationTrack>(object_window);
                    break;
                case SerializeType.RotationEuler:
                    _addTrack<RotationEulerTrack>(object_window);
                    break;
                case SerializeType.Scale:
                    _addTrack<ScaleTrack>(object_window);
                    break;
                case SerializeType.Orientation:
                    _addTrack<OrientationTrack>(object_window);
                    break;
                case SerializeType.UnityAnimation:
                    _addTrack<UnityAnimationTrack>(object_window);
                    break;
                case SerializeType.Audio:
                    _addTrack<AudioTrack>(object_window);
                    break;
                case SerializeType.Property:
                    _addTrack<PropertyTrack>(object_window);
                    break;
                case SerializeType.Event:
                    _addTrack<EventTrack>(object_window);
                    break;
                case SerializeType.CameraSwitcher:
                    if(GameObject.FindObjectsOfType<Camera>().Length <= 1)
                        UnityEditor.EditorUtility.DisplayDialog("Cannot add Camera Switcher", "You need at least 2 cameras in your scene to start using the Camera Switcher track.", "Okay");
                    else if(aData.currentTake.cameraSwitcher != null) // already exists
                        UnityEditor.EditorUtility.DisplayDialog("Camera Switcher Already Exists", "You can only have one Camera Switcher track. Transition between cameras by adding keyframes to the track.", "Okay");
                    else {
                        _addTrack<CameraSwitcherTrack>(object_window);
                        // preview selected frame
                        if(object_window != null)
                            aData.currentTake.previewFrame(aData.target, TakeEditCurrent().selectedFrame);
                    }
                    break;
                case SerializeType.GOSetActive:
                    _addTrack<GOSetActiveTrack>(object_window);
                    break;
                case SerializeType.Material:
                    _addTrack<MaterialTrack>(object_window);
                    break;
                default:
                    int combo_index = (int)trackType - 100;
                    if(combo_index >= oData.quickAdd_Combos.Count)
                        Debug.LogError("Animator: Track type '" + (int)trackType + "' not found.");
                    else {
                        foreach(int _etrack in oData.quickAdd_Combos[combo_index])
                            addTrackWithGameObject((int)_etrack, object_window);
                    }
                    break;
            }
        }

        public bool addSpriteKeysToTrack(UnityEngine.Object[] objs, int trackId, int frame) {
            var track = aData.currentTake.getTrack(trackId) as PropertyTrack;
            if(track != null && track.getTrackType() == "sprite") {
                List<Sprite> sprites = EditorUtility.GetSprites(objs);
                if(sprites.Count > 0) {
                    //prepare data
                    const string label = "Add Sprite Keys";

                    aData.RegisterTakesUndo(label, false);

                    //insert keys
                    Take take = aData.currentTake;

                    if(sprites.Count > 1) {
                        float sprFPS = oData.spriteInsertFramePerSecond;

                        int spriteNumFrames = Mathf.RoundToInt(((float)sprites.Count / sprFPS) * (float)take.frameRate);

                        track.offsetKeysFromBy(aData.target, frame, spriteNumFrames);

                        for(int i = 0; i < sprites.Count; i++) {
                            PropertyKey key = track.addKey(aData.target, frame);
                            key.valObj = sprites[i];

                            int nframe = Mathf.RoundToInt((((float)frame / (float)take.frameRate) + (1.0f / sprFPS)) * (float)take.frameRate);
                            frame = Mathf.Max(nframe, frame + 1);

                            //add final key if it's the last in track
                            if(i == sprites.Count - 1 && track.keys[track.keys.Count - 1] == key) {
                                key = track.addKey(aData.target, frame);
                                key.valObj = sprites[i];
                            }
                        }
                    }
                    else { //just set key
                        Key key = track.getKeyOnFrame(frame, false);
                        if(key == null)
                            key = track.addKey(aData.target, frame);

                        (key as PropertyKey).valObj = sprites[0];
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if track added successfully
        /// </summary>
        public bool addSpriteTrackWithKeyObjects(UnityEngine.Object[] objs, int startFrame) {
            List<Sprite> sprites = EditorUtility.GetSprites(objs);
            if(sprites.Count > 0) {
                sprites.Sort(delegate (Sprite obj1, Sprite obj2) { return obj1.name.CompareTo(obj2.name); });

                //create go
                const string label = "Add Sprite Track";
                GameObject newGO = new GameObject("sprite", typeof(SpriteRenderer));
                Undo.RegisterCreatedObjectUndo(newGO, label);
                Undo.SetTransformParent(newGO.transform, aData.target.root, label);
                newGO.transform.localPosition = Vector3.zero;

                //prepare animator data
                aData.RegisterTakesUndo("Add Sprite Track", false);

                //add track
                addTrack((int)SerializeType.Property);

                Take take = aData.currentTake;
                timelineSelectTrack(aData.currentTake.trackCounter);

                PropertyTrack newTrack = TakeEdit(take).getSelectedTrack(take) as PropertyTrack;
                newTrack.SetTarget(aData.target, newGO.transform);

                //insert keys
                float sprFPS = oData.spriteInsertFramePerSecond;

                SpriteRenderer comp = newGO.GetComponent<SpriteRenderer>();
                newTrack.setComponent(aData.target, comp);
                newTrack.setPropertyInfo(comp.GetType().GetProperty("sprite"));

                int frame = startFrame;
                for(int i = 0; i < sprites.Count; i++) {
                    PropertyKey key = newTrack.addKey(aData.target, frame);
                    key.valObj = sprites[i];

                    int nframe = Mathf.RoundToInt((((float)frame / (float)take.frameRate) + (1.0f / sprFPS)) * (float)take.frameRate);
                    frame = Mathf.Max(nframe, frame + 1);
                }

                //add last frame
                PropertyKey lastKey = newTrack.addKey(aData.target, frame);
                lastKey.valObj = sprites[sprites.Count - 1];

                comp.sprite = sprites[0];

                return true;
            }

            return false;
        }
        void addKeyToFrame(int frame) {
            // add key if there are tracks
            if(aData.currentTake.getTrackCount() > 0) {
                // add a key
                addKey(TakeEditCurrent().selectedTrack, frame);
                // preview current frame
                //aData.getCurrentTake().previewFrame(aData.target, aData.getCurrentTake().selectedFrame);
            }
            timelineSelectFrame(TakeEditCurrent().selectedTrack, aData.currentTake.selectedFrame, false);
        }
        void addKeyToSelectedFrame() {
            // add key if there are tracks
            if(aData.currentTake.getTrackCount() > 0) {
                // add a key
                addKey(TakeEditCurrent().selectedTrack, aData.currentTake.selectedFrame);
                // preview current frame
                aData.currentTake.previewFrame(aData.target, aData.currentTake.selectedFrame);
            }
            timelineSelectFrame(TakeEditCurrent().selectedTrack, aData.currentTake.selectedFrame, false);
        }
        void addKey(int _track, int _frame) {
            // add a key to the track number and frame, used in OnGUI. Needs to be updated for every track type.
            aData.RegisterTakesUndo("New Key", false);

            Track amTrack = aData.currentTake.getTrack(_track);
            
            // translation
            if(amTrack is TranslationTrack) {
                Transform t = amTrack.GetTarget(aData.target) as Transform;
                // if missing object, return
                if(!t) {
                    showAlertMissingObjectType("Transform");
                    return;
                }
                (amTrack as TranslationTrack).addKey(aData.target, _frame, t.localPosition);
            }
            else if(amTrack is RotationTrack) {
                // rotation
                Transform t = amTrack.GetTarget(aData.target) as Transform;
                // if missing object, return
                if(!t) {
                    showAlertMissingObjectType("Transform");
                    return;
                }
                // add key to rotation track
                (amTrack as RotationTrack).addKey(aData.target, _frame, t.localRotation);
            }
            else if(amTrack is RotationEulerTrack) {
                // rotation
                Transform t = amTrack.GetTarget(aData.target) as Transform;
                // if missing object, return
                if(!t) {
                    showAlertMissingObjectType("Transform");
                    return;
                }
                // add key to rotation track
                (amTrack as RotationEulerTrack).addKey(aData.target, _frame, t.localEulerAngles);
            }
            else if(amTrack is ScaleTrack) {
                // scale
                Transform t = amTrack.GetTarget(aData.target) as Transform;
                // if missing object, return
                if(!t) {
                    showAlertMissingObjectType("Transform");
                    return;
                }
                // add key to rotation track
                (amTrack as ScaleTrack).addKey(aData.target, _frame, t.localScale);
            }
            else if(amTrack is OrientationTrack) {
                // orientation

                // if missing object, return
                if(!amTrack.GetTarget(aData.target)) {
                    showAlertMissingObjectType("Transform");
                    return;
                }
                // add key to orientation track
                Transform last_target = null;
                int last_key = (amTrack as OrientationTrack).getKeyFrameBeforeFrame(_frame, false);
                if(last_key == -1) last_key = (amTrack as OrientationTrack).getKeyFrameAfterFrame(_frame, false);
                if(last_key != -1) {
                    OrientationKey _oKey = ((amTrack as OrientationTrack).getKeyOnFrame(last_key) as OrientationKey);
                    last_target = _oKey.GetTarget(aData.target);
                }
                (amTrack as OrientationTrack).addKey(aData.target, _frame, last_target);
            }
            else if(amTrack is UnityAnimationTrack) {
                // animation
                GameObject go = amTrack.GetTarget(aData.target) as GameObject;
                // if missing object, return
                if(!go) {
                    showAlertMissingObjectType("GameObject");
                    return;
                }
                // add key to animation track
                (amTrack as UnityAnimationTrack).addKey(aData.target, _frame, go.GetComponent<Animation>().clip, WrapMode.Once);
            }
            else if(amTrack is AudioTrack) {
                // audio
                AudioSource a = amTrack.GetTarget(aData.target) as AudioSource;
                // if missing object, return
                if(!a) {
                    showAlertMissingObjectType("AudioSource");
                    return;
                }
                // add key to animation track
                (amTrack as AudioTrack).addKey(aData.target, _frame, null, false);

            }
            else if(amTrack is PropertyTrack) {
                // property
                GameObject go = amTrack.GetTarget(aData.target) as GameObject;
                // if missing object, return
                if(!go) {
                    showAlertMissingObjectType("GameObject");
                    return;
                }
                // if missing property, return
                if(!(amTrack as PropertyTrack).isPropertySet()) {
                    UnityEditor.EditorUtility.DisplayDialog("Property Not Set", "You must set the track property before you can add keys.", "Okay");
                    return;
                }
                (amTrack as PropertyTrack).addKey(aData.target, _frame);
            }
            else if(amTrack is EventTrack) {
                // event
                var obj = amTrack.GetTarget(aData.target);
                // if missing object, return
                if(!obj) {
                    showAlertMissingObjectType("Object");
                    return;
                }
                // add key to event track
                (amTrack as EventTrack).addKey(aData.target, _frame);
            }
            else if(amTrack is CameraSwitcherTrack) {
                // camera switcher
                CameraSwitcherKey _cKey = null;
                int last_key = (amTrack as CameraSwitcherTrack).getKeyFrameBeforeFrame(_frame, false);
                if(last_key == -1) last_key = (amTrack as CameraSwitcherTrack).getKeyFrameAfterFrame(_frame, false);
                if(last_key != -1) {
                    _cKey = ((amTrack as CameraSwitcherTrack).getKeyOnFrame(last_key) as CameraSwitcherKey);
                }
                // add key to camera switcher
                (amTrack as CameraSwitcherTrack).addKey(aData.target, _frame, null, _cKey);
            }
            else if(amTrack is GOSetActiveTrack) {
                // go set active
                GameObject go = amTrack.GetTarget(aData.target) as GameObject;
                // if missing object, return
                if(!go) {
                    showAlertMissingObjectType("GameObject");
                    return;
                }
                // add key to go active track
                (amTrack as GOSetActiveTrack).addKey(aData.target, _frame);
            }
            else if(amTrack is MaterialTrack) {
                Renderer render = amTrack.GetTarget(aData.target) as Renderer;
                if(!render) {
                    showAlertMissingObjectType("Renderer");
                    return;
                }

                (amTrack as MaterialTrack).addKey(aData.target, _frame);
            }

            //if the added key is the last key, set its ease type to the one before it
            if(amTrack.keys.Count > 1 && amTrack.keys[amTrack.keys.Count - 1].frame == _frame) {
                amTrack.keys[amTrack.keys.Count - 1].easeType = amTrack.keys[amTrack.keys.Count - 2].easeType;
            }
        }
        void deleteKeyFromSelectedFrame() {
            aData.RegisterTakesUndo("Clear Frame", false);

            Track amTrack = TakeEditCurrent().getSelectedTrack(aData.currentTake);

            //Key[] dkeys = 
            amTrack.removeKeyOnFrame(aData.currentTake.selectedFrame);

            amTrack.updateCache(aData.target);

            // select current frame
            timelineSelectFrame(TakeEditCurrent().selectedTrack, TakeEditCurrent().selectedFrame);
        }
        void deleteSelectedKeys(bool showWarning) {
            var takeEditCurrent = TakeEditCurrent();
            bool shouldClearFrames = true;
            if(showWarning) {
                if(takeEditCurrent.contextSelectionTrackIds.Count > 1) {
                    if(!UnityEditor.EditorUtility.DisplayDialog("Clear From Multiple Tracks?", "Are you sure you want to clear the selected frames from all of the selected tracks?", "Clear Frames", "Cancel")) {
                        shouldClearFrames = false;
                    }
                }
            }
            if(shouldClearFrames) {
                aData.RegisterTakesUndo("Clear Frame", false);

                foreach(int track_id in takeEditCurrent.contextSelectionTrackIds) {
                    int trackInd = aData.currentTake.getTrackIndex(track_id);
                    if(trackInd == -1) continue;

                    Track amTrack = aData.currentTake.trackValues[trackInd];

                    takeEditCurrent.removeSelectedKeysFromTrack(aData.currentTake, aData.target, track_id);
                }
            }

            // select current frame
            timelineSelectFrame(TakeEditCurrent().selectedTrack, TakeEditCurrent().selectedFrame, false);
        }

        #endregion

        #region Menus/Context

        void addTrackFromMenu(object type) {
            aData.RegisterTakesUndo("New Track", false);
            addTrack(type);
        }
        void addTrackFromMenuLate(object type) {
            mAddTrackTypeIndexLate = (int)type;
            mAddTrackTypeIndexLateCounter = 0; //reset counter to allow one cycle to happen before adding track
        }
        void buildAddTrackMenu() {
            menu.AddItem(new GUIContent("Translation"), false, addTrackFromMenu, (int)SerializeType.Translation);
            menu.AddItem(new GUIContent("Rotation/Quaternion"), false, addTrackFromMenu, (int)SerializeType.Rotation);
            menu.AddItem(new GUIContent("Rotation/Euler"), false, addTrackFromMenu, (int)SerializeType.RotationEuler);
            menu.AddItem(new GUIContent("Scale"), false, addTrackFromMenu, (int)SerializeType.Scale);
            menu.AddItem(new GUIContent("Orientation"), false, addTrackFromMenu, (int)SerializeType.Orientation);
            menu.AddItem(new GUIContent("Animation"), false, addTrackFromMenu, (int)SerializeType.UnityAnimation);
            menu.AddItem(new GUIContent("Audio"), false, addTrackFromMenu, (int)SerializeType.Audio);
            menu.AddItem(new GUIContent("Property"), false, addTrackFromMenu, (int)SerializeType.Property);
            menu.AddItem(new GUIContent("Material"), false, addTrackFromMenu, (int)SerializeType.Material);
            menu.AddItem(new GUIContent("Event"), false, addTrackFromMenu, (int)SerializeType.Event);
            menu.AddItem(new GUIContent("GO Active"), false, addTrackFromMenu, (int)SerializeType.GOSetActive);
            menu.AddItem(new GUIContent("Camera Switcher"), false, addTrackFromMenu, (int)SerializeType.CameraSwitcher);
        }
        void buildAddTrackMenu_Drag() {
            bool hasTransform = true;
            bool hasAnimation = true;
            bool hasAudioSource = true;
            bool hasCamera = true;
            bool hasAnimate = true;
            bool hasRenderer = true;
            AnimateEditControl animDat;

            foreach(GameObject g in objects_window) {
                // break loop when all variables are false
                if(!hasTransform && !hasAnimation && !hasAudioSource && !hasCamera && !hasRenderer) break;

                if(hasTransform && !g.GetComponent(typeof(Transform)))
                    hasTransform = false;
                if(hasAnimation && !g.GetComponent(typeof(Animation)))
                    hasAnimation = false;
                if(hasAudioSource && !g.GetComponent(typeof(AudioSource)))
                    hasAudioSource = false;
                if(hasCamera && !g.GetComponent(typeof(Camera)))
                    hasCamera = false;
                if(hasAnimate && ((animDat = AnimEdit(g.GetComponent<Animate>())) == null || animDat == aData))
                    hasAnimate = false;
                if(hasRenderer && !g.GetComponent(typeof(Renderer)))
                    hasRenderer = false;
            }
            // add track menu
            menu_drag = new GenericMenu();

            // Translation/Rotation/Orientation
            if(hasTransform) {
                menu_drag.AddItem(new GUIContent("Translation"), false, addTrackFromMenuLate, (int)SerializeType.Translation);
                menu_drag.AddItem(new GUIContent("Rotation/Quaternion"), false, addTrackFromMenuLate, (int)SerializeType.Rotation);
                menu_drag.AddItem(new GUIContent("Rotation/Euler"), false, addTrackFromMenuLate, (int)SerializeType.RotationEuler);
                menu_drag.AddItem(new GUIContent("Scale"), false, addTrackFromMenuLate, (int)SerializeType.Scale);
                menu_drag.AddItem(new GUIContent("Orientation"), false, addTrackFromMenuLate, (int)SerializeType.Orientation);
            }
            else {
                menu_drag.AddDisabledItem(new GUIContent("Translation"));
                menu_drag.AddDisabledItem(new GUIContent("Rotation/Quaterion"));
                menu_drag.AddDisabledItem(new GUIContent("Rotation/Euler"));
                menu_drag.AddDisabledItem(new GUIContent("Scale"));
                menu_drag.AddDisabledItem(new GUIContent("Orientation"));
            }
            // Animation
            if(hasAnimation) menu_drag.AddItem(new GUIContent("Animation"), false, addTrackFromMenuLate, (int)SerializeType.UnityAnimation);
            else menu_drag.AddDisabledItem(new GUIContent("Animation"));
            // Audio
            if(hasAudioSource) menu_drag.AddItem(new GUIContent("Audio"), false, addTrackFromMenuLate, (int)SerializeType.Audio);
            else menu_drag.AddDisabledItem(new GUIContent("Audio"));
            // Property
            menu_drag.AddItem(new GUIContent("Property"), false, addTrackFromMenuLate, (int)SerializeType.Property);
            // Material
            if(hasRenderer) menu_drag.AddItem(new GUIContent("Material"), false, addTrackFromMenuLate, (int)SerializeType.Material);
            else menu_drag.AddDisabledItem(new GUIContent("Material"));
            // Event
            menu_drag.AddItem(new GUIContent("Event"), false, addTrackFromMenuLate, (int)SerializeType.Event);
            // GO Active
            menu_drag.AddItem(new GUIContent("GO Active"), false, addTrackFromMenuLate, (int)SerializeType.GOSetActive);
            // Camera Switcher
            if(hasCamera && aData.currentTake.cameraSwitcher == null) menu_drag.AddItem(new GUIContent("Camera Switcher"), false, addTrackFromMenuLate, (int)SerializeType.CameraSwitcher);
            else menu_drag.AddDisabledItem(new GUIContent("Camera Switcher"));

            if(oData.quickAdd_Combos.Count > 0) {
                // multiple tracks
                menu_drag.AddSeparator("");
                foreach(List<int> combo in oData.quickAdd_Combos) {
                    string combo_name = "";
                    for(int i = 0; i < combo.Count; i++) {
                        //combo_name += Enum.GetName(typeof(Track),combo[i])+" ";
                        combo_name += SerializeTypeEditor.TrackNames[combo[i]] + " " + (combo[i] == (int)SerializeType.CameraSwitcher && aData.currentTake.cameraSwitcher != null ? "(Key) " : "");
                        if(i < combo.Count - 1) combo_name += "+ ";
                    }
                    if(canQuickAddCombo(combo, hasTransform, hasAnimation, hasAudioSource, hasCamera, hasAnimate, hasRenderer))
                        menu_drag.AddItem(new GUIContent(combo_name), false, addTrackFromMenuLate, 100 + oData.quickAdd_Combos.IndexOf(combo));
                    else menu_drag.AddDisabledItem(new GUIContent(combo_name));
                }
            }

        }
        bool canQuickAddCombo(List<int> combo, bool hasTransform, bool hasAnimation, bool hasAudioSource, bool hasCamera, bool hasAnimate, bool hasRenderer) {
            foreach(int _track in combo) {
                if(!hasTransform && (_track == (int)SerializeType.Translation || _track == (int)SerializeType.Rotation || _track == (int)SerializeType.RotationEuler || _track == (int)SerializeType.Scale || _track == (int)SerializeType.Orientation))
                    return false;
                else if(!hasAnimation && _track == (int)SerializeType.UnityAnimation)
                    return false;
                else if(!hasAudioSource && _track == (int)SerializeType.Audio)
                    return false;
                else if(!hasCamera && _track == (int)SerializeType.CameraSwitcher)
                    return false;
                else if(!hasRenderer && _track == (int)SerializeType.Material)
                    return false;
            }
            return true;
        }
        bool IsTrackMatch(Track track1, Track track2) {
            bool isMatch = false;

            if(track1 is PropertyTrack) {
                // if pasting into property track
                if(track2 is PropertyTrack) // if property tracks have the same property
                    isMatch = (track1 as PropertyTrack).hasSamePropertyAs(aData.target, (track2 as PropertyTrack));
            }
            else if(track1 is EventTrack) { // if origin is event track
                                                   // if pasting into event track
                if(track2 is EventTrack) {
                    // if event tracks are compaitable
                    if((track1 as EventTrack).hasSameEventsAs(aData.target, (track2 as EventTrack))) {
                        isMatch = true;
                    }
                }
            }
            // if origin is material track
            else if(track1 is MaterialTrack) {
                // if pasting into property track
                if(track2 is MaterialTrack) //has the same material and property
                    isMatch = (track1 as MaterialTrack).hasSamePropertyAs(aData.target, (track2 as MaterialTrack));
            }
            else if(track1 != null && track2 != null) {
                if(track1.getTrackType() == track2.getTrackType()) {
                    isMatch = true;
                }
            }

            return isMatch;
        }
        void buildContextMenu(int frame) {
            TakeEditControl takeEdit = TakeEditCurrent();
            contextMenuFrame = frame;
            contextMenu = new GenericMenu();
            bool selectionHasKeys = takeEdit.contextSelectionTrackIds.Count > 1 || takeEdit.contextSelectionHasKeys(aData.currentTake);
            bool _canPaste = contextSelectionKeysBuffer.Count > 0;

            contextMenu.AddItem(new GUIContent("Insert Keyframe"), false, invokeContextMenuItem, 0);
            contextMenu.AddSeparator("");
            if(selectionHasKeys) {
                contextMenu.AddItem(new GUIContent("Cut Frames"), false, invokeContextMenuItem, 1);
                contextMenu.AddItem(new GUIContent("Copy Frames"), false, invokeContextMenuItem, 2);
                if(_canPaste) contextMenu.AddItem(new GUIContent("Paste Frames"), false, invokeContextMenuItem, 3);
                else contextMenu.AddDisabledItem(new GUIContent("Paste Frames"));
                contextMenu.AddItem(new GUIContent("Clear Frames"), false, invokeContextMenuItem, 4);
            }
            else {
                contextMenu.AddDisabledItem(new GUIContent("Cut Frames"));
                contextMenu.AddDisabledItem(new GUIContent("Copy Frames"));
                if(_canPaste) contextMenu.AddItem(new GUIContent("Paste Frames"), false, invokeContextMenuItem, 3);
                else contextMenu.AddDisabledItem(new GUIContent("Paste Frames"));
                contextMenu.AddDisabledItem(new GUIContent("Clear Frames"));
            }
            contextMenu.AddItem(new GUIContent("Select All Frames"), false, invokeContextMenuItem, 5);
            if(selectionHasKeys) {
                contextMenu.AddSeparator("");
                contextMenu.AddItem(new GUIContent("Reverse Key Order"), false, invokeContextMenuItem, 6);
            }
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
            else if(index == 3) contextPasteKeys(false);
            else if(index == 4) deleteSelectedKeys(true);
            else if(index == 5) contextSelectAllFrames();
            else if(index == 6) contextReverseKeys();
        }
        void contextReverseKeys() {
            aData.RegisterTakesUndo("Reverse Key Order", false);

            TakeEditControl takeEdit = TakeEditCurrent();
            Take take = aData.currentTake;
            foreach(int track_id in takeEdit.contextSelectionTrackIds) {
                Track track = take.getTrack(track_id);

                Key[] keys = takeEdit.getContextSelectionKeysForTrack(take.getTrack(track_id));
                for(int i = 0; i < keys.Length / 2; i++) {
                    Key key = keys[i];
                    Key rkey = keys[keys.Length - 1 - i];

                    int frame = key.frame; key.frame = rkey.frame; rkey.frame = frame;
                    Ease easeType = key.easeType; key.easeType = rkey.easeType; rkey.easeType = easeType;
                    AnimationCurve easeCurve = key.easeCurve; key.easeCurve = rkey.easeCurve; rkey.easeCurve = easeCurve;
                    List<float> customEase = key.customEase; key.customEase = rkey.customEase; rkey.customEase = customEase;
                    float amp = key.amplitude; key.amplitude = rkey.amplitude; rkey.amplitude = amp;
                    float period = key.period; key.period = rkey.period; rkey.period = period;
                }

                track.updateCache(aData.target);
            }
        }
        void contextCutKeys() {
            contextCopyFrames();
            deleteSelectedKeys(false);
        }
        void contextPasteKeys(bool applyOffsetKeys) {
            if(contextSelectionKeysBuffer == null || contextSelectionKeysBuffer.Count == 0) return;
            
            bool isUndoRegistered = false;

            int offset = (int)contextSelectionRange.y - (int)contextSelectionRange.x + 1;

            var takeEditCur = TakeEditCurrent();
            var curTake = aData.currentTake;

            //single track copy/paste, allow paste on multiple selected tracks
            if(contextSelectionKeysBuffer.Count == 1) {
                var trackBuffer = contextSelectionTracksBuffer[0]; //this ought to be the same count
                var keyBuffer = contextSelectionKeysBuffer[0];

                //paste to all matching selected tracks
                var trackSelects = takeEditCur.GetContextSelectionTracks(curTake);

                for(int i = 0; i < trackSelects.Count; i++) {
                    var track = trackSelects[i];
                    if(IsTrackMatch(track, trackBuffer)) {
                        var newKeys = new List<Key>(keyBuffer.Count);

                        //copy keys
                        foreach(var a in keyBuffer) {
                            if(a != null) {
                                var newKey = (Key)System.Activator.CreateInstance(a.GetType());
                                a.CopyTo(newKey);
                                newKey.frame += (contextMenuFrame - (int)contextSelectionRange.x);
                                newKeys.Add(newKey);
                            }
                        }

                        if(newKeys.Count > 0) {
                            //record undo
                            if(!isUndoRegistered) {
                                aData.RegisterTakesUndo("Paste Frames", false);
                                isUndoRegistered = true;
                            }

                            if(applyOffsetKeys) {
                                track.offsetKeysFromBy(aData.target, contextMenuFrame, offset);
                            }
                            else {
                                //override keys with the same frame
                                for(int newKeyInd = newKeys.Count - 1; newKeyInd >= 0; newKeyInd--) {
                                    var newKey = newKeys[newKeyInd];

                                    foreach(var key in track.keys) {
                                        if(key.frame == newKey.frame) {
                                            newKey.CopyTo(key);
                                            newKeys.RemoveAt(newKeyInd);
                                            break;
                                        }
                                    }
                                }
                            }

                            track.keys.AddRange(newKeys);

                            track.updateCache(aData.target);
                        }
                    }
                }
            }
            //multi track copy/paste, match copied track types to selected track types
            else {
                //sort out takes for buffer and selection based on their order in groups
                curTake.SortTracksByGroupOrder(contextSelectionTracksBuffer);

                var trackSelects = takeEditCur.GetContextSelectionTracks(curTake);
                curTake.SortTracksByGroupOrder(trackSelects);

                //match buffered tracks to selected tracks
                for(int trackBuffInd = 0; trackBuffInd < contextSelectionTracksBuffer.Count; trackBuffInd++) {
                    var trackBuffer = contextSelectionTracksBuffer[trackBuffInd];
                    var keyBuffer = contextSelectionKeysBuffer[trackBuffInd]; //assume this is aligned to track buffer order

                    //grab the matching track, remove from trackSelect to avoid multiple copying
                    Track trackMatch = null;
                    for(int trackSelectInd = 0; trackSelectInd < trackSelects.Count; trackSelectInd++) {
                        var trackSelect = trackSelects[trackSelectInd];
                        if(IsTrackMatch(trackSelect, trackBuffer)) {
                            trackMatch = trackSelect;
                            trackSelects.RemoveAt(trackSelectInd);
                            break;
                        }
                    }

                    //no match
                    if(trackMatch == null)
                        continue;

                    //copy keys
                    var newKeys = new List<Key>(keyBuffer.Count);

                    //copy keys
                    foreach(var a in keyBuffer) {
                        if(a != null) {
                            var newKey = (Key)System.Activator.CreateInstance(a.GetType());
                            a.CopyTo(newKey);
                            newKey.frame += (contextMenuFrame - (int)contextSelectionRange.x);
                            newKeys.Add(newKey);
                        }
                    }

                    if(newKeys.Count > 0) {
                        //record undo
                        if(!isUndoRegistered) {
                            aData.RegisterTakesUndo("Paste Frames", false);
                            isUndoRegistered = true;
                        }

                        if(applyOffsetKeys) {
                            trackMatch.offsetKeysFromBy(aData.target, contextMenuFrame, offset);
                        }
                        else {
                            //override keys with the same frame
                            for(int newKeyInd = newKeys.Count - 1; newKeyInd >= 0; newKeyInd--) {
                                var newKey = newKeys[newKeyInd];

                                foreach(var key in trackMatch.keys) {
                                    if(key.frame == newKey.frame) {
                                        newKey.CopyTo(key);
                                        newKeys.RemoveAt(newKeyInd);
                                        break;
                                    }
                                }
                            }
                        }

                        trackMatch.keys.AddRange(newKeys);

                        trackMatch.updateCache(aData.target);
                    }
                }
            }
        }
        void contextSaveKeysToBuffer() {
            var curTake = aData.currentTake;
            var curTakeEdit = TakeEditCurrent();

            if(curTakeEdit.contextSelection.Count <= 0) {
                contextSelectionRange.x = curTakeEdit.selectedFrame;
                contextSelectionRange.y = curTakeEdit.selectedFrame;
            }
            else {
                // sort
                curTakeEdit.contextSelection.Sort();
                // set selection range
                contextSelectionRange.x = curTakeEdit.contextSelection[0];
                contextSelectionRange.y = curTakeEdit.contextSelection[curTakeEdit.contextSelection.Count - 1];
            }
            // set selection track
            //contextSelectionTrack = aData.getCurrentTake().selectedTrack;

            ClearKeysBuffer();
            contextSelectionTracksBuffer.Clear();

            foreach(int track_id in curTakeEdit.contextSelectionTrackIds) {
                contextSelectionTracksBuffer.Add(aData.currentTake.getTrack(track_id));
            }
            foreach(var track in contextSelectionTracksBuffer) {
                contextSelectionKeysBuffer.Add(new List<Key>());
                foreach(var key in curTakeEdit.getContextSelectionKeysForTrack(track)) {
                    var nkey = (Key)System.Activator.CreateInstance(key.GetType());
                    key.CopyTo(nkey);
                    contextSelectionKeysBuffer[contextSelectionKeysBuffer.Count - 1].Add(nkey);
                }
            }
        }

        void contextCopyFrames() {
            cachedContextSelection.Clear();
            // cache context selection
            foreach(int frame in TakeEditCurrent().contextSelection) {
                cachedContextSelection.Add(frame);
            }
            // save keys
            contextSaveKeysToBuffer();
        }
        void contextSelectAllFrames() {
            aData.RegisterTakesUndo("Select All Frames", false);
            var curTake = aData.currentTake;
            var curTakeEdit = TakeEdit(curTake);
            int totalFrames = curTakeEdit.getTotalSelectedFrames(curTake);
            curTakeEdit.contextSelectAllFrames(totalFrames > 0 ? totalFrames : curTake.totalFrames);
        }
        public void contextSelectFrame(int frame, int prevFrame) {
            // select range if shift down
            if(isShiftDown) {
                // if control is down, toggle
                TakeEditCurrent().contextSelectFrameRange(prevFrame, frame);
                return;
            }
            // clear context selection if control is not down
            if(!isControlDown) TakeEditCurrent().contextSelection.Clear();
            // select single, toggle if control is down
            TakeEditCurrent().contextSelectFrame(frame, isControlDown);
            //contextSelectFrameRange(frame,frame);
        }
        public void contextSelectFrameRange(int startFrame, int endFrame) {
            // clear context selection if control is not down
            if(isShiftDown) {
                TakeEditCurrent().contextSelectFrameRange(aData.currentTake.selectedFrame, endFrame);
                return;
            }
            if(!isControlDown) TakeEditCurrent().contextSelection.Clear();

            TakeEditCurrent().contextSelectFrameRange(startFrame, endFrame);
        }
        #endregion

        #region Other Fns

        public void Reload() {
            ResetDragging();
            OnDisable();
            OnEnable();
            Repaint();
        }
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
        public void updateCachedMethodInfoFromObject(UnityEngine.Object obj) {
            if(!obj) return;
            cachedMethodInfo = new List<MethodInfo>();
            cachedMethodNames = new List<string>();

            MethodInfo[] methodInfos = obj.GetType().GetMethods(methodFlags);
            foreach(MethodInfo methodInfo in methodInfos) {
                if((methodInfo.Name == "Start") || (methodInfo.Name == "Update") || (methodInfo.Name == "Main")) continue;
                string methodSig = getMethodInfoSignature(methodInfo);
                if(!string.IsNullOrEmpty(methodSig)) {
                    cachedMethodNames.Add(methodSig);
                    cachedMethodInfo.Add(methodInfo);
                }
            }
        }

        void resetPreview() {
            // reset all object transforms to frame 1
            aData.currentTake.previewFrame(aData.target, 1f);
        }
        bool isTextEditting() {
            return isRenamingTrack != -1 || isRenamingTake || isRenamingGroup != 0;
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
                aData.MakeTakeNameUnique(aData.currentTake);
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

        Color RenderColorProperty(Rect rectField, string propertyLabel, Color color) {
            GUI.skin = null; EditorUtility.ResetDisplayControls();
            rectField.height = 22f;
            Color nclr = EditorGUI.ColorField(rectField, propertyLabel, color);
            GUI.skin = skin; EditorUtility.ResetDisplayControls();
            return nclr;
        }

        #endregion

        #endregion
    }
}
