using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class AMTransitionPicker : EditorWindow {
    #region Declarations
    // static
    public static AMTransitionPicker window = null;
    public static bool justSet = false;
    public static AMCameraSwitcherKey key = null;
    public static AMTrack track = null;
    public static int selectedTransition;
    public static int selectedSpeedIndex = 0;
    public static List<float> parameters = new List<float>();
    public static Texture2D irisShape;
    private static bool useGameView = false;
    private static bool showTransitionList = true;
    public static List<string> transitionNames = new List<string>();
    // skins
    private GUISkin skin = null;
    private string cachedSkinName = null;
    // resources
    private Texture tex_transition_a;
    private Texture tex_transition_b;
    private Texture tex_default_view;
    private Texture tex_game_view;
    private Texture tex_transition_toggle_bg;
    private Texture tex_transition_toggle_button_bg;
    private Texture texRightArrow;// inspector right arrow
    private Texture texLeftArrow;	// inspector left arrow
    private Texture tex_angle_0;
    private Texture tex_angle_90;
    private Texture tex_angle_180;
    private Texture tex_angle_270;
    private bool texLoaded = false;
    // constants
    private const float width_preview_max = 275f;
    private const float height_preview_max = 180f;
    private const float width_transition_list_open = 476f;
    private const float width_transition_list_closed = 323f;
    private const float height_window = 509f;
    private const float height_parameter_space = 4f;
    private const float height_toggle_button = 44f;
    private const float y_preview_texture = 48f;
    // other variables
    public AnimatorData aData = null;
    private AMOptionsFile oData = null;
    public bool isPlaying = true;
    public enum DragType {
        None = -1,
        Seek = 0,
        MoveFocalPoint = 1
    }
    public enum ElementType {
        None = -1,
        TimeBar = 0,
        FocalPoint = 1
    }
    private Vector2 scrollViewTransitions = new Vector2(0f, 0f);
    private string searchString = "";
    private Vector2 justSetFocalPoint = new Vector2(-1f, -1f);
    private bool didJustSetFocalPoint = false;
    private Vector2 newFocalPoint = new Vector2();
    private bool isDragging = false;
    private bool cachedIsDragging = false;
    private int dragType = -1;
    private int mouseOverElement = -1;
    private string[] speedNames = new string[] { "1x", "2x", ".5x", ".25x" };
    private float[] speedValues = new float[] { 1f, 2f, .5f, .25f };
    public static float waitPercent = 0.2f;
    private float percent = waitPercent * -1f;
    private float _value = 1f;
    #endregion

    #region Main
    void OnEnable() {
        if(!texLoaded) {
            tex_transition_a = AMEditorResource.LoadEditorTexture("am_transition_a");
            tex_transition_b = AMEditorResource.LoadEditorTexture("am_transition_b");
            tex_default_view = AMEditorResource.LoadEditorTexture("am_icon_default_view");
            tex_game_view = AMEditorResource.LoadEditorTexture("am_icon_game_view");
            tex_transition_toggle_bg = AMEditorResource.LoadEditorTexture("am_transition_toggle_bg");
            tex_transition_toggle_button_bg = AMEditorResource.LoadEditorTexture("am_transition_toggle_button_bg");
            texRightArrow = AMEditorResource.LoadEditorTexture("am_nav_right");// inspector right arrow
            texLeftArrow = AMEditorResource.LoadEditorTexture("am_nav_left");	// inspector left arrow
            tex_angle_0 = AMEditorResource.LoadEditorTexture("am_angle_0");
            tex_angle_90 = AMEditorResource.LoadEditorTexture("am_angle_90");
            tex_angle_180 = AMEditorResource.LoadEditorTexture("am_angle_180");
            tex_angle_270 = AMEditorResource.LoadEditorTexture("am_angle_270");

            texLoaded = true;
        }

        window = this;
        setWindowSize();
        this.wantsMouseMove = true;
        loadAnimatorData();
        oData = AMOptionsFile.loadFile();
        // set up here
        transitionNames = new List<string>(AMTween.TransitionNames);
    }

    void OnDisable() {
        window = null;
        justSet = false;
        key = null;
        track = null;
        aData = null;
    }

    void Update() {
        processDragLogic();
        if(isPlaying && dragType != (int)DragType.Seek) {
            percent += 0.002f * speedValues[selectedSpeedIndex];
            if(percent > 1f + waitPercent) percent = waitPercent * -1f;

            if(percent < 0f) _value = 1f;
            else if(percent > 1f) _value = 0f;
            else {
                updateValue();
            }
        }
        this.Repaint();
    }

    void OnGUI() {
        setWindowSize();
        this.title = "Fade: " + (oData.time_numbering ? AMTimeline.frameToTime(key.frame, (float)aData.getCurrentTake().frameRate) + " s" : key.frame.ToString());
        // load skin
        AMTimeline.loadSkin(oData, ref skin, ref cachedSkinName, position);
        EditorGUIUtility.LookLikeControls();
        #region drag logic
        Event e = Event.current;
        Rect rectWindow = new Rect(0f, 0f, position.width, position.height);
        mouseOverElement = (int)ElementType.None;
        bool wasDragging = false;
        if(e.type == EventType.mouseDrag && EditorWindow.mouseOverWindow == this) {
            isDragging = true;
        }
        else if(e.type == EventType.mouseUp || /*EditorWindow.mouseOverWindow!=this*/Event.current.rawType == EventType.MouseUp /*|| e.mousePosition.y < 0f*/) {
            if(isDragging) {
                wasDragging = true;
                isDragging = false;
            }
        }
        // set cursor
        if(dragType == (int)DragType.Seek || dragType == (int)DragType.MoveFocalPoint) EditorGUIUtility.AddCursorRect(rectWindow, MouseCursor.SlideArrow);
        #endregion
        #region temporary variables
        List<string> filteredTransitionNames = filterWithSearchString(transitionNames, searchString);
        GUIStyle stylePadding = new GUIStyle();
        stylePadding.padding = new RectOffset(4, 4, 4, 4);
        #endregion
        #region main horizontal
        GUILayout.BeginHorizontal();
        #region transition details
        GUILayout.BeginVertical();
        Rect rectArea = new Rect(27f - 3f, 0f, 295f - 10f * 2f + 2f, position.height);
        GUILayout.BeginArea(rectArea);
        #region transition title
        GUIStyle labelLarge = new GUIStyle(GUI.skin.label);
        labelLarge.alignment = TextAnchor.MiddleCenter;
        labelLarge.normal.textColor = Color.white;
        labelLarge.fontSize = 22;
        EditorGUI.DropShadowLabel(new Rect(0f, -3f, 295f - 10f * 2f, 50f), getTransitionName(selectedTransition), labelLarge);
        #endregion

        #region preview transition
        Rect rectPreviewTexture = new Rect(0f, y_preview_texture, width_preview_max, height_preview_max);
        Rect rectPreviewTextureFull = new Rect(0f, y_preview_texture, width_preview_max, height_preview_max);
        // set preview texture rect size
        if(useGameView) {
            Vector2 gameViewSize = GetMainGameViewSize();
            float aspect = gameViewSize.x / gameViewSize.y;
            rectPreviewTexture.width = height_preview_max * aspect;
            if(rectPreviewTexture.width > width_preview_max) {
                rectPreviewTexture.width = width_preview_max;
                rectPreviewTexture.height = width_preview_max / aspect;
            }
            rectPreviewTexture.x = (width_preview_max - rectPreviewTexture.width) / 2f;
            rectPreviewTexture.y = y_preview_texture + (height_preview_max - rectPreviewTexture.height) / 2f;
            // game view black background
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, y_preview_texture, width_preview_max, height_preview_max), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
        }
        // draw preview texture
        if(selectedTransition == (int)AMTween.Fade.None) {
            if(percent <= .3f) GUI.DrawTexture(rectPreviewTexture, tex_transition_a);
            else GUI.DrawTexture(rectPreviewTexture, tex_transition_b);
        }
        else {
            bool isReversed = AMTween.isTransitionReversed(selectedTransition, parameters.ToArray());
            GUI.DrawTexture(rectPreviewTexture, (isReversed ? tex_transition_a : tex_transition_b));

            if(AMCameraFade.hasInstance()) {
                AMCameraFade.getCameraFade(false).processCameraFade(rectPreviewTexture, (isReversed ? tex_transition_b : tex_transition_a), selectedTransition, _value, Mathf.Clamp(percent, 0f, 1f), parameters.ToArray(), irisShape, e);
            }
        }
        GUI.color = Color.white;
        #endregion
        #region focal point
        if(justSetFocalPoint.x >= 0f || justSetFocalPoint.y >= 0f) {
            if(dragType == (int)DragType.MoveFocalPoint) {
                float overX = Mathf.Clamp(e.mousePosition.x - rectPreviewTexture.x, 0f, rectPreviewTexture.width);
                float overY = Mathf.Clamp(e.mousePosition.y - rectPreviewTexture.y, 0f, rectPreviewTexture.height);
                bool isOverTexture = (overX >= 0f && overY >= 0f);
                if(isOverTexture) {
                    if(justSetFocalPoint.x >= 0f) {
                        justSetFocalPoint.x = Mathf.Clamp(overX / rectPreviewTexture.width, 0f, 1f);
                    }
                    if(justSetFocalPoint.y >= 0f) {
                        justSetFocalPoint.y = Mathf.Clamp(overY / rectPreviewTexture.height, 0f, 1f);
                    }
                }
                didJustSetFocalPoint = true;
                newFocalPoint = justSetFocalPoint;
            }
            GUI.color = new Color(Color.red.r, Color.red.g, Color.red.b, .8f);
            Rect rectFocalPoint = new Rect(rectPreviewTexture.x + (justSetFocalPoint.x >= 0f ? justSetFocalPoint.x * rectPreviewTexture.width - 1f : 0), rectPreviewTexture.y + (justSetFocalPoint.y >= 0f ? justSetFocalPoint.y * rectPreviewTexture.height - 1f : 0f), (justSetFocalPoint.x >= 0f ? 1f + 2f : rectPreviewTexture.width), (justSetFocalPoint.y >= 0f ? 1f + 2f : rectPreviewTexture.height));
            GUI.DrawTexture(rectFocalPoint, EditorGUIUtility.whiteTexture);
            Rect rectFocalPointButton = new Rect(rectFocalPoint);
            rectFocalPointButton.x -= 2f;
            rectFocalPointButton.width += 4f;
            rectFocalPointButton.y -= 2f;
            rectFocalPointButton.height += 4f;
            if(dragType == (int)DragType.None) EditorGUIUtility.AddCursorRect(rectFocalPointButton, MouseCursor.SlideArrow);
            if(rectFocalPointButton.Contains(e.mousePosition)) mouseOverElement = (int)ElementType.FocalPoint;
            GUI.color = Color.white;
        }
        #endregion
        #region playback controls
        // time bar
        Rect rectTimeBar = new Rect(0f, y_preview_texture + height_preview_max + 2f + 4f, rectArea.width - (18f * 3f + 2f * 4f), 10f);
        GUI.color = GUI.skin.window.hover.textColor;
        GUI.DrawTexture(rectTimeBar, EditorGUIUtility.whiteTexture);
        Rect rectCurrentTime = new Rect(rectTimeBar);
        rectCurrentTime.width = Mathf.Clamp(rectCurrentTime.width * percent, 0f, rectTimeBar.width);
        GUI.color = AMTimeline.getSkinTextureStyleState("properties_bg").textColor;
        GUI.DrawTexture(rectCurrentTime, EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;
        if(GUI.Button(rectTimeBar, "", "label") || dragType == (int)DragType.Seek) {
            percent = (e.mousePosition.x - rectTimeBar.x) / rectTimeBar.width;
            updateValue();
        }
        if(rectTimeBar.Contains(e.mousePosition)) {
            mouseOverElement = (int)ElementType.TimeBar;
        }
        // last frame button
        Rect rectPlaybackButton = new Rect(rectArea.width - 18f - 2f, y_preview_texture + height_preview_max + 2f, 18f, 18f);

        if(GUI.Button(rectPlaybackButton, AMTimeline.getSkinTextureStyleState("nav_skip_forward").background, GUI.skin.GetStyle("ButtonImage"))) {
            percent = 1f;
            updateValue();
        }
        rectPlaybackButton.x -= (18f + 2f);
        // play / pause button
        Texture playToggleTexture;
        if(isPlaying) playToggleTexture = AMTimeline.getSkinTextureStyleState("nav_stop_white").background;
        else playToggleTexture = AMTimeline.getSkinTextureStyleState("nav_play").background;
        if(GUI.Button(rectPlaybackButton, playToggleTexture, GUI.skin.GetStyle("ButtonImage"))) {
            isPlaying = !isPlaying;
        }
        rectPlaybackButton.x -= (18f + 2f);
        // first frame button
        if(GUI.Button(rectPlaybackButton, AMTimeline.getSkinTextureStyleState("nav_skip_back").background, GUI.skin.GetStyle("ButtonImage"))) {
            percent = 0f;
            updateValue();
        }
        #endregion
        #region preview hover controls
        if(dragType == (int)DragType.None && rectPreviewTextureFull.Contains(e.mousePosition)) {
            Vector2 v = new Vector2(-1f, -1f);
            if(justSetFocalPoint.x >= 0f && justSetFocalPoint.y >= 0f) v = new Vector2(rectPreviewTexture.x + justSetFocalPoint.x * rectPreviewTexture.width, rectPreviewTexture.y + justSetFocalPoint.y * rectPreviewTexture.height);
            // check if focal point overlaps selection grid
            Rect rectViewSelGrid = new Rect(rectPreviewTexture.x + 2f, rectPreviewTexture.y + 2f, 26f, 20f);
            if(v.x >= 0f && v.y >= 0f) {
                if(new Rect(rectViewSelGrid.x - 2f, rectViewSelGrid.y - 2f, rectViewSelGrid.width + 4f, rectViewSelGrid.height + 4f).Contains(v)) {
                    rectViewSelGrid.y = rectPreviewTexture.y + rectPreviewTexture.height - rectViewSelGrid.height - 2f;
                }
            }
            // view type selection grid
            if(!rectViewSelGrid.Contains(e.mousePosition)) GUI.color = new Color(1f, 1f, 1f, .5f);
            if(GUI.Button(rectViewSelGrid, (useGameView ? new GUIContent(tex_default_view, "Standard View") : new GUIContent(tex_game_view, "Game View")), GUI.skin.GetStyle("ButtonImage"))) useGameView = !useGameView;
            //useGameView = GUI.SelectionGrid(rectViewSelGrid,(useGameView ? 1 : 0),new GUIContent[]{new GUIContent(tex_default_view,"Standard View"),new GUIContent(tex_game_view,"Game View")},2,GUI.skin.GetStyle("ButtonImage")) == 1 ? true : false;
            GUI.color = Color.white;
            // speed label
            GUIStyle styleLabelRight = new GUIStyle(GUI.skin.label);
            styleLabelRight.alignment = TextAnchor.MiddleRight;
            styleLabelRight.normal.textColor = Color.white;
            Rect rectLabelSpeed = new Rect(rectPreviewTexture.x + rectPreviewTexture.width - 50f - 2f, rectPreviewTexture.y + 2f, 50f, 25f);
            if(v.x >= 0f && v.y >= 0f) {
                if(new Rect(rectLabelSpeed.x - 2f, rectLabelSpeed.y - 2f, rectLabelSpeed.width + 4f, rectLabelSpeed.height + 4f).Contains(v)) {
                    rectLabelSpeed.y = rectPreviewTexture.y + rectPreviewTexture.height - rectLabelSpeed.height - 2f;
                }
            }
            EditorGUI.DropShadowLabel(rectLabelSpeed, speedNames[selectedSpeedIndex], styleLabelRight);
        }
        // speed button
        if(!wasDragging && GUI.Button(rectPreviewTextureFull, "", "label")) {
            selectedSpeedIndex = (selectedSpeedIndex + 1) % speedValues.Length;
            if(isPlaying) percent = waitPercent * -1f;
        }
        #endregion
        #region parameters
        GUILayout.Space(240f + 18f);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical(GUILayout.Width(width_preview_max));
        // ease picker, hide if selected transition is "None"
        if(selectedTransition != (int)AMTween.Fade.None) {
            if(AMTimeline.showEasePicker(track, key, aData)) {
                if(isPlaying) percent = waitPercent * -1f;
            }
        }
        // parameters
        setParametersFor(selectedTransition);
        // empty horizontal to preserve formatting
        GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        #endregion
        GUILayout.EndArea();
        GUILayout.EndVertical();
        #endregion
        #region transition list
        // show/hide list texture
        Rect rectShowHideButton = new Rect(width_transition_list_closed - 36f, 0f, 56f, height_toggle_button);
        bool shouldMakeTransparent = (!showTransitionList && !rectShowHideButton.Contains(e.mousePosition));
        //GUI.color = new Color(31f/255f,37f/255f,45f/255f,(shouldMakeTransparent ? .5f : 1f));
        GUI.color = AMTimeline.getSkinTextureStyleState("properties_bg").textColor;
        GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, (shouldMakeTransparent ? .5f : 1f));
        Rect rectToggleBGFade = new Rect(width_transition_list_closed - 24f, 0f, 27f, position.height);
        GUI.DrawTexture(rectToggleBGFade, tex_transition_toggle_bg);
        GUI.DrawTexture(new Rect(rectToggleBGFade.x + rectToggleBGFade.width, 0f, width_transition_list_open - width_transition_list_closed + 24f - rectToggleBGFade.width, position.height), EditorGUIUtility.whiteTexture);
        Rect rectToggleBG = new Rect(width_transition_list_closed - 71f + 35f, 0f, 56f, height_toggle_button);
        GUI.DrawTexture(rectToggleBG, tex_transition_toggle_button_bg);
        GUI.color = new Color(1f, 1f, 1f, (shouldMakeTransparent ? .5f : 1f));
        GUI.DrawTexture(new Rect(rectToggleBG.x + 8f + (showTransitionList ? 1f : 0f), rectToggleBG.y + rectToggleBG.height / 2f - 19f / 2f, 22f, 19f), showTransitionList ? texLeftArrow : texRightArrow);
        GUI.color = Color.white;
        // show/hide list button
        if(GUI.Button(rectShowHideButton, "", "label")) showTransitionList = !showTransitionList;
        if(showTransitionList) {
            GUILayout.BeginVertical(stylePadding, GUILayout.Width(153f));
            #region search transition list
            GUILayout.BeginVertical(GUILayout.Height(33f));
            GUILayout.Space(5f);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(50f));
            GUILayout.Space(2f);
            GUILayout.Label("Search");
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Space(3f);
            searchString = GUILayout.TextField(searchString, GUILayout.Width(70f));
            GUILayout.EndVertical();
            Rect rectClearButton = new Rect(position.width - 24f, GUILayoutUtility.GetLastRect().y + 4f, 15f, 15f);
            if(GUI.Button(rectClearButton, AMTimeline.getSkinTextureStyleState("x").background, GUI.skin.GetStyle("ButtonImage"))) {
                searchString = "";
            }
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            #endregion
            GUILayout.Space(8f);
            #region transition list
            // set scrollview background
            GUIStyle styleScrollView = new GUIStyle(GUI.skin.scrollView);
            styleScrollView.normal.background = GUI.skin.GetStyle("GroupElementBG").onNormal.background;
            scrollViewTransitions = GUILayout.BeginScrollView(scrollViewTransitions, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, styleScrollView/*, GUILayout.Width(175f)*/);
            GUILayout.Space(2f);
            if(setSelectedTransition(filteredTransitionNames, GUILayout.SelectionGrid(getSelectedTransitionIndex(filteredTransitionNames), filteredTransitionNames.ToArray(), 1, GUILayout.Width(120f)))) {
                // select transition
                setDefaultParametersFor(selectedTransition, ref parameters, ref irisShape, ref justSetFocalPoint);
                percent = waitPercent * -1f;
                if(!isPlaying) isPlaying = true;
                updateValue();
            }
            if(filteredTransitionNames.Count <= 0) {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("No results found");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            #endregion
            GUILayout.EndVertical();
        }
        #endregion
        GUILayout.EndHorizontal();
        #endregion
        #region apply / cancel
        GUILayout.BeginArea(new Rect(0f, position.height - 29f, 322f, 29f));
        GUILayout.BeginHorizontal(stylePadding);
        if(GUILayout.Button("Apply")) {
            key.cameraFadeType = selectedTransition;
            key.cameraFadeParameters = new List<float>(parameters);
            key.irisShape = irisShape;
            // update cache when modifying varaibles
            track.updateCache();
            AMCodeView.refresh();
            // preview frame
            aData.getCurrentTake().previewFrame(aData.getCurrentTake().selectedFrame);
            // save data
            EditorUtility.SetDirty(aData);
            this.Close();
        }
        if(GUILayout.Button("Cancel")) {
            this.Close();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        #endregion
    }

    void OnHierarchyChange() {
        if(!aData) reloadAnimatorData();
    }
    #endregion

    #region Common
    // set and show all parameters for transition
    public void setParametersFor(int index) {
        GUILayout.Space(height_parameter_space);
        switch(index) {
            #region venetian blinds
            case (int)AMTween.Fade.VenetianBlinds:
                // layout, vertical or horizontal
                if(parameters.Count < 1) parameters.Add(AMCameraFade.Defaults.VenetianBlinds.layout);
                else if(parameters[0] != 0f && parameters[0] != 1f) parameters[0] = AMCameraFade.Defaults.VenetianBlinds.layout;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Layout");
                parameters[0] = (float)GUILayout.SelectionGrid((int)parameters[0], new string[] { "Vertical", "Horizontal" }, 2);
                GUILayout.EndHorizontal();
                GUILayout.Space(height_parameter_space);
                // number of blinds
                if(parameters.Count < 2) parameters.Add(AMCameraFade.Defaults.VenetianBlinds.numberOfBlinds);
                parameters[1] = (float)EditorGUILayout.IntField("Number of Blinds", (int)parameters[1]);
                if(parameters[1] < 2f) parameters[1] = 2f;
                else if(parameters[1] > 100f) parameters[1] = 100f;
                break;
            #endregion
            #region doors
            case (int)AMTween.Fade.Doors:
                // layout, vertical or horizontal
                if(parameters.Count < 1) parameters.Add(AMCameraFade.Defaults.Doors.layout);
                else if(parameters[0] != 0f && parameters[0] != 1f) parameters[0] = AMCameraFade.Defaults.Doors.layout;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Layout");
                parameters[0] = (float)GUILayout.SelectionGrid((int)parameters[0], new string[] { "Vertical", "Horizontal" }, 2);
                GUILayout.EndHorizontal();
                GUILayout.Space(height_parameter_space);
                // type, open or close
                if(parameters.Count < 2) parameters.Add(AMCameraFade.Defaults.Doors.type);
                else if(parameters[1] != 0f && parameters[1] != 1f) parameters[1] = AMCameraFade.Defaults.Doors.type;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Type");
                parameters[1] = (float)GUILayout.SelectionGrid((int)parameters[1], new string[] { "Open", "Close" }, 2);
                GUILayout.EndHorizontal();
                GUILayout.Space(height_parameter_space);
                // focal point
                if(parameters.Count < 3) parameters.Add(AMCameraFade.Defaults.Doors.focalXorY);
                else if(parameters[2] < 0f && parameters[2] > 1f) parameters[2] = AMCameraFade.Defaults.Doors.focalXorY;
                parameters[2] = EditorGUILayout.FloatField("Focal " + (parameters[0] == 0f ? "X" : "Y"), parameters[2]);
                if(didJustSetFocalPoint) parameters[2] = (parameters[0] == 0f ? newFocalPoint.x : newFocalPoint.y);
                parameters[2] = Mathf.Clamp(parameters[2], 0f, 1f);
                justSetFocalPoint = new Vector2((parameters[0] == 0f ? parameters[2] : -1f), (parameters[0] == 0f ? -1f : parameters[2]));
                break;
            #endregion
            #region iris box
            case (int)AMTween.Fade.IrisBox:
                // type, shrink or grow
                if(parameters.Count < 1) parameters.Add(AMCameraFade.Defaults.IrisBox.type);
                else if(parameters[0] != 0f && parameters[0] != 1f) parameters[0] = AMCameraFade.Defaults.IrisBox.type;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Type");
                parameters[0] = (float)GUILayout.SelectionGrid((int)parameters[0], new string[] { "Shrink", "Grow" }, 2);
                GUILayout.EndHorizontal();
                GUILayout.Space(height_parameter_space);
                // focal point
                if(parameters.Count < 2) parameters.Add(AMCameraFade.Defaults.IrisBox.focalX);
                if(parameters.Count < 3) parameters.Add(AMCameraFade.Defaults.IrisBox.focalY);
                if(didJustSetFocalPoint) {
                    parameters[1] = newFocalPoint.x;
                    parameters[2] = newFocalPoint.y;
                }
                justSetFocalPoint = EditorGUILayout.Vector2Field("Focal Point", new Vector2(Mathf.Clamp(parameters[1], 0f, 1f), Mathf.Clamp(parameters[2], 0f, 1f)));
                parameters[1] = justSetFocalPoint.x;
                parameters[2] = justSetFocalPoint.y;
                break;
            #endregion
            #region iris round
            case (int)AMTween.Fade.IrisRound:
                // type, shrink or grow
                if(parameters.Count < 1) parameters.Add(AMCameraFade.Defaults.IrisRound.type);
                else if(parameters[0] != 0f && parameters[0] != 1f) parameters[0] = AMCameraFade.Defaults.IrisRound.type;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Type");
                parameters[0] = (float)GUILayout.SelectionGrid((int)parameters[0], new string[] { "Shrink", "Grow" }, 2);
                GUILayout.EndHorizontal();
                GUILayout.Space(height_parameter_space);
                // max scale
                if(parameters.Count < 2) parameters.Add(AMCameraFade.Defaults.IrisRound.maxScale);
                parameters[1] = EditorGUILayout.FloatField("Max Scale", parameters[1]);
                if(parameters[1] <= 0f) parameters[1] = 0.01f;
                GUILayout.Space(height_parameter_space);
                // focal point
                if(parameters.Count < 3) parameters.Add(AMCameraFade.Defaults.IrisRound.focalX);
                if(parameters.Count < 4) parameters.Add(AMCameraFade.Defaults.IrisRound.focalY);
                if(didJustSetFocalPoint) {
                    parameters[2] = newFocalPoint.x;
                    parameters[3] = newFocalPoint.y;
                }
                justSetFocalPoint = EditorGUILayout.Vector2Field("Focal Point", new Vector2(Mathf.Clamp(parameters[2], 0f, 1f), Mathf.Clamp(parameters[3], 0f, 1f)));
                parameters[2] = justSetFocalPoint.x;
                parameters[3] = justSetFocalPoint.y;
                break;
            #endregion
            #region iris shape
            case (int)AMTween.Fade.IrisShape:
                // type, shrink or grow
                if(parameters.Count < 1) parameters.Add(AMCameraFade.Defaults.IrisShape.type);
                else if(parameters[0] != 0f && parameters[0] != 1f) parameters[0] = AMCameraFade.Defaults.IrisShape.type;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Type");
                parameters[0] = (float)GUILayout.SelectionGrid((int)parameters[0], new string[] { "Shrink", "Grow" }, 2);
                GUILayout.EndHorizontal();
                // shape texture
                GUILayout.Space(height_parameter_space);
                GUILayout.Label("Shape");
                GUI.skin = null;
                EditorGUIUtility.LookLikeControls();
                GUILayout.BeginHorizontal();
                irisShape = (Texture2D)EditorGUILayout.ObjectField("", irisShape, typeof(Texture2D), false);
                GUI.skin = skin;
                EditorGUIUtility.LookLikeControls(100f);
                GUILayout.BeginVertical(GUILayout.Height(68f));
                GUILayout.FlexibleSpace();
                // max scale
                if(parameters.Count < 2) parameters.Add(AMCameraFade.Defaults.IrisShape.maxScale);
                parameters[1] = EditorGUILayout.FloatField("Max Scale", parameters[1]);
                if(parameters[1] <= 0f) parameters[1] = 0.01f;
                GUILayout.Space(height_parameter_space);
                // rotate amount
                if(parameters.Count < 3) parameters.Add(AMCameraFade.Defaults.IrisShape.rotateAmount);
                parameters[2] = (float)EditorGUILayout.IntField("Rotate Amount", (int)parameters[2]);
                //GUILayout.Space(height_parameter_space);
                // ease rotation
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                GUILayout.Space(3f);
                GUILayout.Label("Ease Rotation");
                GUILayout.EndVertical();
                GUILayout.BeginVertical();
                // ease rotation
                if(parameters.Count < 4) parameters.Add(AMCameraFade.Defaults.IrisShape.easeRotation);
                bool irisEaseRotation = (parameters[3] == 1f);
                irisEaseRotation = GUILayout.Toggle(irisEaseRotation, "");
                parameters[3] = (irisEaseRotation ? 1f : 0f);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.Space(height_parameter_space);
                // focal point
                if(parameters.Count < 5) parameters.Add(AMCameraFade.Defaults.IrisShape.focalX);
                if(parameters.Count < 6) parameters.Add(AMCameraFade.Defaults.IrisShape.focalY);
                if(didJustSetFocalPoint) {
                    parameters[4] = newFocalPoint.x;
                    parameters[5] = newFocalPoint.y;
                }
                if(didJustSetFocalPoint) {
                    parameters[4] = newFocalPoint.x;
                    parameters[5] = newFocalPoint.y;
                }
                justSetFocalPoint = EditorGUILayout.Vector2Field("Focal Point", new Vector2(Mathf.Clamp(parameters[4], 0f, 1f), Mathf.Clamp(parameters[5], 0f, 1f)));
                parameters[4] = justSetFocalPoint.x;
                parameters[5] = justSetFocalPoint.y;
                GUILayout.Space(height_parameter_space);
                // rotation pivot
                if(parameters.Count < 7) parameters.Add(AMCameraFade.Defaults.IrisShape.pivotX);
                if(parameters.Count < 8) parameters.Add(AMCameraFade.Defaults.IrisShape.pivotY);
                Vector2 irisRotationPivot = new Vector2(parameters[6], parameters[7]);
                irisRotationPivot = EditorGUILayout.Vector2Field("Rotation Pivot", irisRotationPivot);
                parameters[6] = Mathf.Clamp(irisRotationPivot.x, 0f, 1f);
                parameters[7] = Mathf.Clamp(irisRotationPivot.y, 0f, 1f);
                break;
            #endregion
            #region shape wipe
            case (int)AMTween.Fade.ShapeWipe:
                // angle
                if(parameters.Count < 1) parameters.Add(AMCameraFade.Defaults.ShapeWipe.angle);
                parameters[0] = (float)EditorGUILayout.IntField("Angle", (int)parameters[0]);
                GUILayout.Space(height_parameter_space);
                // shape texture
                GUILayout.Label("Shape");
                GUI.skin = null;
                EditorGUIUtility.LookLikeControls();
                GUILayout.BeginHorizontal();
                irisShape = (Texture2D)EditorGUILayout.ObjectField("", irisShape, typeof(Texture2D), false);
                GUI.skin = skin;
                EditorGUIUtility.LookLikeControls(100f);
                GUILayout.BeginVertical(GUILayout.Height(68f));
                GUILayout.FlexibleSpace();
                // scale
                if(parameters.Count < 2) parameters.Add(AMCameraFade.Defaults.ShapeWipe.scale);
                parameters[1] = EditorGUILayout.FloatField("Scale", parameters[1]);
                GUILayout.Space(height_parameter_space);
                // padding
                if(parameters.Count < 3) parameters.Add(AMCameraFade.Defaults.ShapeWipe.padding);
                parameters[2] = EditorGUILayout.FloatField("Padding", parameters[2]);
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.Space(height_parameter_space);
                // offset start
                if(parameters.Count < 4) parameters.Add(AMCameraFade.Defaults.ShapeWipe.offsetStart);
                parameters[3] = EditorGUILayout.FloatField("Offset Start", parameters[3]);
                GUILayout.Space(height_parameter_space);
                // offset end
                if(parameters.Count < 5) parameters.Add(AMCameraFade.Defaults.ShapeWipe.offsetEnd);
                parameters[4] = EditorGUILayout.FloatField("Offset End", parameters[4]);
                break;
            #endregion
            #region linear wipe
            case (int)AMTween.Fade.LinearWipe:
                // angle
                if(parameters.Count < 1) parameters.Add(AMCameraFade.Defaults.LinearWipe.angle);
                parameters[0] = (float)EditorGUILayout.IntField("Angle", (int)parameters[0]);
                GUILayout.Space(height_parameter_space);
                // padding
                if(parameters.Count < 2) parameters.Add(AMCameraFade.Defaults.LinearWipe.padding);
                parameters[1] = EditorGUILayout.FloatField("Padding", parameters[1]);
                break;
            #endregion
            #region radial wipe
            case (int)AMTween.Fade.RadialWipe:
                // direction, clockwise / counterclockwise
                if(parameters.Count < 1) parameters.Add(AMCameraFade.Defaults.RadialWipe.direction);
                else if(parameters[0] != 0f && parameters[0] != 1f) parameters[0] = AMCameraFade.Defaults.RadialWipe.direction;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Direction");
                parameters[0] = (float)GUILayout.SelectionGrid((int)parameters[0], new string[] { "Clockwise", "Counter CW" }, 2);
                GUILayout.EndHorizontal();
                GUILayout.Space(height_parameter_space);
                GUILayout.Space(3f);
                // starting angle
                if(parameters.Count < 2) parameters.Add(AMCameraFade.Defaults.RadialWipe.startingAngle);
                int indexStartAngle = radialWipeAngleToIndex(parameters[1]);
                if(indexStartAngle < 0) {
                    parameters[1] = 0f;
                    indexStartAngle = 0;
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label("Start Angle");
                indexStartAngle = GUILayout.SelectionGrid(indexStartAngle, new GUIContent[] { new GUIContent(tex_angle_0, "0"), new GUIContent(tex_angle_90, "90"), new GUIContent(tex_angle_180, "180"), new GUIContent(tex_angle_270, "270") }, 4, GUI.skin.GetStyle("ButtonImage"));
                GUILayout.EndHorizontal();

                float newAngle = radialWipeIndexToAngle(indexStartAngle);
                if(parameters[1] != newAngle) {
                    parameters[1] = newAngle;
                }
                GUILayout.Space(3f);
                GUILayout.Space(height_parameter_space);
                // focal point
                if(parameters.Count < 3) parameters.Add(AMCameraFade.Defaults.RadialWipe.focalX);
                if(parameters.Count < 4) parameters.Add(AMCameraFade.Defaults.RadialWipe.focalY);
                if(didJustSetFocalPoint) {
                    parameters[2] = newFocalPoint.x;
                    parameters[3] = newFocalPoint.y;
                }
                justSetFocalPoint = EditorGUILayout.Vector2Field("Focal Point", new Vector2(Mathf.Clamp(parameters[2], 0f, 1f), Mathf.Clamp(parameters[3], 0f, 1f)));
                parameters[2] = justSetFocalPoint.x;
                parameters[3] = justSetFocalPoint.y;
                break;
            #endregion
            #region wedge wipe
            case (int)AMTween.Fade.WedgeWipe:
                GUILayout.Space(3f);
                // starting angle
                if(parameters.Count < 1) parameters.Add(AMCameraFade.Defaults.WedgeWipe.startingAngle);
                int _indexStartAngle = radialWipeAngleToIndex(parameters[0]);
                if(_indexStartAngle < 0) {
                    parameters[0] = 0f;
                    _indexStartAngle = 0;
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label("Start Angle");
                _indexStartAngle = GUILayout.SelectionGrid(_indexStartAngle, new GUIContent[] { new GUIContent(tex_angle_0, "0"), new GUIContent(tex_angle_90, "90"), new GUIContent(tex_angle_180, "180"), new GUIContent(tex_angle_270, "270") }, 4, GUI.skin.GetStyle("ButtonImage"));
                GUILayout.EndHorizontal();

                float _newAngle = radialWipeIndexToAngle(_indexStartAngle);
                if(parameters[0] != _newAngle) {
                    parameters[0] = _newAngle;
                }
                GUILayout.Space(3f);
                GUILayout.Space(height_parameter_space);
                // focal point
                if(parameters.Count < 2) parameters.Add(AMCameraFade.Defaults.WedgeWipe.focalX);
                if(parameters.Count < 3) parameters.Add(AMCameraFade.Defaults.WedgeWipe.focalY);
                if(didJustSetFocalPoint) {
                    parameters[1] = newFocalPoint.x;
                    parameters[2] = newFocalPoint.y;
                }
                justSetFocalPoint = EditorGUILayout.Vector2Field("Focal Point", new Vector2(Mathf.Clamp(parameters[1], 0f, 1f), Mathf.Clamp(parameters[2], 0f, 1f)));
                parameters[1] = justSetFocalPoint.x;
                parameters[2] = justSetFocalPoint.y;
                break;
            #endregion
            default:
                break;
        }
        didJustSetFocalPoint = false;
    }

    // set default parameters for transition
    public static void setDefaultParametersFor(int index, ref List<float> parameters, ref Texture2D _irisShape, ref Vector2 justSetFocalPoint) {
        parameters = new List<float>();
        switch(index) {
            #region venetian blinds
            case (int)AMTween.Fade.VenetianBlinds:
                // layout, vertical or horizontal
                parameters.Add(AMCameraFade.Defaults.VenetianBlinds.layout);
                // number of blinds
                parameters.Add(AMCameraFade.Defaults.VenetianBlinds.numberOfBlinds);
                justSetFocalPoint = new Vector2(-1f, -1f);
                break;
            #endregion
            #region doors
            case (int)AMTween.Fade.Doors:
                // layout, vertical or horizontal
                parameters.Add(AMCameraFade.Defaults.Doors.layout);
                // type, open or close
                parameters.Add(AMCameraFade.Defaults.Doors.type);
                // focal point
                parameters.Add(AMCameraFade.Defaults.Doors.focalXorY); // x or y
                justSetFocalPoint = new Vector2(AMCameraFade.Defaults.Doors.focalXorY, -1f);
                break;
            #endregion
            #region iris shape
            case (int)AMTween.Fade.IrisShape:
                // default shape
                _irisShape = (Texture2D)Resources.Load("am_iris_star_1024");
                // type, shrink or grow
                parameters.Add(AMCameraFade.Defaults.IrisShape.type);
                // max scale
                parameters.Add(AMCameraFade.Defaults.IrisShape.maxScale);
                // rotate amount
                parameters.Add(AMCameraFade.Defaults.IrisShape.rotateAmount);
                // ease rotation
                parameters.Add(AMCameraFade.Defaults.IrisShape.easeRotation);
                // focal point
                parameters.Add(AMCameraFade.Defaults.IrisShape.focalX); // x
                parameters.Add(AMCameraFade.Defaults.IrisShape.focalY); // y
                // rotation pivot
                parameters.Add(AMCameraFade.Defaults.IrisShape.pivotX); // x
                parameters.Add(AMCameraFade.Defaults.IrisShape.pivotY); // y

                justSetFocalPoint = new Vector2(AMCameraFade.Defaults.IrisShape.focalX, AMCameraFade.Defaults.IrisShape.focalY);
                break;
            #endregion
            #region iris box
            case (int)AMTween.Fade.IrisBox:
                // type, shrink or grow
                parameters.Add(AMCameraFade.Defaults.IrisBox.type);
                // focal point
                parameters.Add(AMCameraFade.Defaults.IrisBox.focalX); // x
                parameters.Add(AMCameraFade.Defaults.IrisBox.focalY); // y
                justSetFocalPoint = new Vector2(AMCameraFade.Defaults.IrisBox.focalX, AMCameraFade.Defaults.IrisBox.focalY);
                break;
            #endregion
            #region iris round
            case (int)AMTween.Fade.IrisRound:
                // type, shrink or grow
                parameters.Add(AMCameraFade.Defaults.IrisRound.type);
                // max scale
                parameters.Add(AMCameraFade.Defaults.IrisRound.maxScale);
                // focal point
                parameters.Add(AMCameraFade.Defaults.IrisRound.focalX);
                parameters.Add(AMCameraFade.Defaults.IrisRound.focalY);
                justSetFocalPoint = new Vector2(AMCameraFade.Defaults.IrisRound.focalX, AMCameraFade.Defaults.IrisRound.focalY);
                // default shape
                _irisShape = (Texture2D)Resources.Load("am_iris_round_1024");
                break;
            #endregion
            #region shape wipe
            case (int)AMTween.Fade.ShapeWipe:
                // angle
                parameters.Add(AMCameraFade.Defaults.ShapeWipe.angle);
                // default shape
                _irisShape = (Texture2D)Resources.Load("am_wipe_text_1024");
                // scale
                parameters.Add(AMCameraFade.Defaults.ShapeWipe.scale);
                // padding
                parameters.Add(AMCameraFade.Defaults.ShapeWipe.padding);
                // offset start
                parameters.Add(AMCameraFade.Defaults.ShapeWipe.offsetStart);
                // offset end
                parameters.Add(AMCameraFade.Defaults.ShapeWipe.offsetEnd);
                justSetFocalPoint = new Vector2(-1f, -1f);
                break;
            #endregion
            #region linear wipe
            case (int)AMTween.Fade.LinearWipe:
                // angle
                parameters.Add(AMCameraFade.Defaults.LinearWipe.angle);
                // default shape
                _irisShape = (Texture2D)Resources.Load("am_wipe_linear_1024");
                // padding
                parameters.Add(AMCameraFade.Defaults.LinearWipe.padding);
                justSetFocalPoint = new Vector2(-1f, -1f);
                break;
            #endregion
            #region radial wipe
            case (int)AMTween.Fade.RadialWipe:
                //direction
                parameters.Add(AMCameraFade.Defaults.RadialWipe.direction);
                // starting angle
                parameters.Add(AMCameraFade.Defaults.RadialWipe.startingAngle);
                // default shape
                _irisShape = (Texture2D)Resources.Load("am_wipe_linear_1024");
                // focal point
                parameters.Add(AMCameraFade.Defaults.RadialWipe.focalX); // x
                parameters.Add(AMCameraFade.Defaults.RadialWipe.focalY); // y
                justSetFocalPoint = new Vector2(AMCameraFade.Defaults.RadialWipe.focalX, AMCameraFade.Defaults.RadialWipe.focalY);
                break;
            #endregion
            #region wedge wipe
            case (int)AMTween.Fade.WedgeWipe:
                // starting angle
                parameters.Add(AMCameraFade.Defaults.WedgeWipe.startingAngle);
                // default shape
                _irisShape = (Texture2D)Resources.Load("am_wipe_linear_1024");
                // focal point
                parameters.Add(AMCameraFade.Defaults.WedgeWipe.focalX); // x
                parameters.Add(AMCameraFade.Defaults.WedgeWipe.focalY); // y
                justSetFocalPoint = new Vector2(AMCameraFade.Defaults.WedgeWipe.focalX, AMCameraFade.Defaults.WedgeWipe.focalY);
                break;
            #endregion
            default:
                justSetFocalPoint = new Vector2(-1f, -1f);
                break;
        }
    }


    #endregion

    #region Process
    void processDragLogic() {
        bool justStartedDrag = false;
        bool justFinishedDrag = false;
        if(isDragging != cachedIsDragging) {
            if(isDragging) justStartedDrag = true;
            else justFinishedDrag = true;
            cachedIsDragging = isDragging;
        }
        if(justStartedDrag) {
            if(mouseOverElement == (int)ElementType.TimeBar) {
                dragType = (int)DragType.Seek;
            }
            else if(mouseOverElement == (int)ElementType.FocalPoint) {
                dragType = (int)DragType.MoveFocalPoint;
            }
            else {
                // if did not drag from a draggable element
                dragType = (int)DragType.None;
            }
            // reset drag
            justStartedDrag = false;
        }
        else if(justFinishedDrag) {
            if(dragType == (int)DragType.MoveFocalPoint) {
                newFocalPoint = justSetFocalPoint;
                didJustSetFocalPoint = true;
            }
            dragType = (int)DragType.None;
            // reset drag
            justFinishedDrag = false;
        }
    }
    #endregion

    #region Get / Set
    public static void setValues(AMCameraSwitcherKey _key, AMTrack _track) {
        justSet = true;
        key = _key;
        track = _track;

        selectedTransition = key.cameraFadeType;
        parameters = new List<float>(key.cameraFadeParameters);
        irisShape = key.irisShape;
    }

    public static void setDefaultParametersForKey(ref AMCameraSwitcherKey cKey) {
        Vector2 temp = new Vector2(0f, 0f);
        setDefaultParametersFor(cKey.cameraFadeType, ref cKey.cameraFadeParameters, ref cKey.irisShape, ref temp);
    }

    public static Vector2 GetMainGameViewSize() {
        System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        System.Object Res = GetSizeOfMainGameView.Invoke(null, null);
        return (Vector2)Res;
    }

    public void setWindowSize() {
        this.maxSize = new Vector2((showTransitionList ? width_transition_list_open : width_transition_list_closed), height_window);
        this.minSize = this.maxSize;
    }

    public int getSelectedTransitionIndex(List<string> filteredNames) {

        //int index = filteredNames.IndexOf(transitionNames[AMTween.TransitionOrder[selectedTransition]]);
        int index = filteredNames.IndexOf(AMTween.TransitionNamesDict[selectedTransition]);
        return index;
    }

    public string getTransitionName(int index) {
        if(index >= AMTween.TransitionNamesDict.Length) index = 0;
        return AMTween.TransitionNamesDict[index];
    }

    public bool setSelectedTransition(List<string> filteredNames, int index) {

        //Debug.Log("set transition index: "+filteredNames[index]);
        //return false;
        if(index == -1 || index >= filteredNames.Count) return false;
        int _index = AMTween.TransitionOrder[transitionNames.IndexOf(filteredNames[index])];
        if(_index < 0) _index = 0;
        if(_index == selectedTransition) return false;
        else selectedTransition = _index;
        return true;
    }
    #endregion

    #region Other Functions
    public static void refreshValues() {
        if(window == null) return;
        setValues(key, track);
    }

    public void reloadAnimatorData() {
        aData = null;
        loadAnimatorData();
        AMTake take = aData.getCurrentTake();
        // update references for track and key
        bool shouldClose = true;
        foreach(AMTrack _track in take.trackValues) {
            if(track == _track) {
                track = _track;
                foreach(AMCameraSwitcherKey _key in track.keys) {
                    if(key == _key) {
                        key = _key;
                        shouldClose = false;
                    }
                }
            }
        }
        if(shouldClose) this.Close();
    }

    void loadAnimatorData() {
        GameObject go = GameObject.Find("AnimatorData");
        if(go) {
            aData = (AnimatorData)go.GetComponent("AnimatorData");
        }
    }

    void updateValue() {
        AMTween.EasingFunction ease;
        AnimationCurve curve = null;

        if(key.hasCustomEase()) {
            ease = AMTween.customEase;
            curve = key.getCustomEaseCurve();
        }
        else {
            ease = AMTween.GetEasingFunction((AMTween.EaseType)key.easeType);
        }

        _value = ease(1f, 0f, Mathf.Clamp(percent, 0f, 1f), curve);
    }

    // filter string array with search string, not case sensitive
    public List<string> filterWithSearchString(List<string> lsNames, string _searchString) {
        if(_searchString == null || _searchString == "") return lsNames;
        _searchString = _searchString.ToLower();
        List<string> lsFiltered = new List<string>();
        foreach(string s in lsNames) {
            if(s.ToLower().Contains(_searchString))
                lsFiltered.Add(s);
        }
        return lsFiltered;
    }

    private int radialWipeAngleToIndex(float angle) {
        if(angle == 0f) return 0;
        if(angle == 90f) return 1;
        if(angle == 180f) return 2;
        if(angle == 270f) return 3;
        return -1;
    }

    private float radialWipeIndexToAngle(int index) {
        switch(index) {
            case 0:
                return 0f;
            case 1:
                return 90f;
            case 2:
                return 180f;
            case 3:
                return 270f;
            default:
                return -1f;
        }
    }
    #endregion

}
