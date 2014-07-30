using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Linq;
using System.IO;

public class AMCodeView : EditorWindow {
    public static AMCodeView window = null;

    public AMOptionsFile oData;
    public AnimatorData aData;

    private string[] selStrings = new string[] { "C#", "Javascript" };
    private Vector2 scrollView;
    public static Dictionary<string, string> varNameDictionary;
    private static bool isInspectorOpen = false;
    private string codeCache = "";
    private GUISkin skin = null;
    private string cachedSkinName = null;

    private Texture texRightArrow;// inspector right arrow
    private Texture texLeftArrow;	// inspector left arrow
    private Vector2 scrollPos;
    private static Dictionary<int, bool> dictTracks = new Dictionary<int, bool>();
    private static Dictionary<int, bool> dictGroups = new Dictionary<int, bool>();
    private static bool shouldRefresh = false;
    private Vector2 startScrubMousePosition;
    private Vector2 currentMousePosition;
    private float start_width_inspector_open;
    private static float width_inspector_open = 200f;
    // constants
    private const float width_inspector_open_min = 195f;
    private const float width_code_min = 215f;
    private const float width_inspector_closed = 40f;
    private const float inspector_space = 4f;
    private const float height_label_offset = 3f;
    private const float width_indent = 15f;

    public enum DragType {
        None = -1,
        ResizeInspector = 0
    }
    public enum ElementType {
        None = -1,
        ResizeInspector = 0,
    }

    private int dragType = -1;
    private int mouseOverElement = -1;
    private bool isDragging = false;
    private bool cachedIsDragging = false;

    void OnEnable() {
        texRightArrow = AMEditorResource.LoadEditorTexture("am_nav_right");// inspector right arrow
        texLeftArrow = AMEditorResource.LoadEditorTexture("am_nav_left");	// inspector left arrow

        window = this;
        this.title = "Code View";
        this.minSize = new Vector2(/*380f*/width_code_min + width_inspector_open_min, 102f);
        this.scrollView = new Vector2(0f, 0f);
        oData = AMOptionsFile.loadFile();
        loadAnimatorData();
        refreshCode();

    }
    void OnDisable() {
        window = null;
    }
    void OnHierarchyChange() {
        if(!aData) loadAnimatorData();
    }
    public void reloadAnimatorData() {
        aData = null;
        loadAnimatorData();
    }
    void loadAnimatorData() {
        if(AMTimeline.window != null) {
            aData = AMTimeline.window.aData;
        }
    }
    void Update() {
        processDragLogic();
        /*codeRefreshBuffer--;
        if(aData && codeRefreshBuffer <= 0) {
            codeCache = getCode();	
            codeRefreshBuffer = codeRefreshRate;
            this.Repaint();
        }*/
        //if(EditorWindow.mouseOverWindow==this) this.Repaint();
    }
    void OnGUI() {
        AMTimeline.loadSkin(ref skin, ref cachedSkinName, position);
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.wordWrap = false;
        textStyle.alignment = TextAnchor.UpperLeft;
        textStyle.padding = new RectOffset(0, 0, 10, 0);
        GUIStyle styleLabelCentered = new GUIStyle(GUI.skin.label);
        styleLabelCentered.alignment = TextAnchor.MiddleCenter;
        if(!aData) {
            AMTimeline.MessageBox("Animator requires an AnimatorData component in your scene. Launch Animator to add the component.", AMTimeline.MessageBoxType.Warning);
            return;
        }
        if(!oData) oData = AMOptionsFile.loadFile();
        #region drag logic
        Event e = Event.current;
        currentMousePosition = e.mousePosition;
        Rect rectWindow = new Rect(0f, 0f, position.width, position.height);
        mouseOverElement = (int)ElementType.None;
        //bool wasDragging = false;
        if(e.type == EventType.mouseDrag && EditorWindow.mouseOverWindow == this) {
            isDragging = true;
        }
        else if(e.type == EventType.mouseUp || /*EditorWindow.mouseOverWindow!=this*/Event.current.rawType == EventType.MouseUp /*|| e.mousePosition.y < 0f*/) {
            if(isDragging) {
                //wasDragging = true;
                isDragging = false;
            }
        }
        // set cursor
        if(dragType == (int)DragType.ResizeInspector) EditorGUIUtility.AddCursorRect(rectWindow, MouseCursor.ResizeHorizontal);
        #endregion
        #region resize inspector
        if(dragType == (int)DragType.ResizeInspector) {
            width_inspector_open = start_width_inspector_open + (startScrubMousePosition.x - e.mousePosition.x);
        }
        width_inspector_open = Mathf.Clamp(width_inspector_open, width_inspector_open_min, position.width - width_code_min);
        #endregion
        GUILayout.BeginHorizontal();
        #region code vertical
        GUILayout.BeginVertical(GUILayout.Height(position.height));
        GUILayout.Space(3f);
        if(aData.e_setCodeLanguage(GUILayout.SelectionGrid(aData.codeLanguage, selStrings, 2/*,styleSelGrid*/))) {
            // save data	
            EditorUtility.SetDirty(aData);
            refreshCode();
        }
        GUILayout.Space(3f);
        // set scrollview background
        GUIStyle styleScrollView = new GUIStyle(GUI.skin.scrollView);
        styleScrollView.normal.background = GUI.skin.GetStyle("GroupElementBG").onNormal.background;
        scrollView = EditorGUILayout.BeginScrollView(scrollView, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, styleScrollView);
        Vector2 textSize = textStyle.CalcSize(new GUIContent(codeCache));
        GUILayout.BeginHorizontal();
        GUILayout.Space(10f);
        EditorGUILayout.SelectableLabel(codeCache, textStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true), GUILayout.MinWidth(textSize.x + 30f), GUILayout.MinHeight(textSize.y + 30f));
        GUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
        GUILayout.Space(3f);
        GUILayout.BeginHorizontal();
        // refresh button
        if(shouldRefresh) GUI.color = Color.green;
        if(GUILayout.Button(""/*,styleButton*/)) {
            refreshCode();
        }
        GUI.color = Color.white;
        GUI.Label(GUILayoutUtility.GetLastRect(), "Refresh", styleLabelCentered);
        // copy to clipboard button
        if(GUILayout.Button("Copy to Clipboard")) {
            ClipboardHelper.clipBoard = codeCache;
            GUIUtility.keyboardControl = 0;
            GUIUtility.hotControl = 0;
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(3f);
        GUILayout.EndVertical();
        #endregion
        #region track list vertical
        GUILayout.BeginHorizontal(GUILayout.Width((isInspectorOpen ? width_inspector_open : width_inspector_closed)));
        //if(GUILayout.Button("O/C",GUILayout.Width(width_inspector_closed),GUILayout.Height(position.height))) {
        //isInspectorOpen = !isInspectorOpen;
        //}
        // properties button
        GUILayout.BeginVertical(GUILayout.Width(width_inspector_closed));
        GUILayout.Space(width_inspector_closed);
        Rect rectPropertiesButton = new Rect(position.width - (isInspectorOpen ? width_inspector_open : width_inspector_closed) - 1f, 0f, width_inspector_closed, position.height - 28f);
        if(GUI.Button(rectPropertiesButton, "", "label")) {
            isInspectorOpen = !isInspectorOpen;
        }
        GUI.color = AMTimeline.getSkinTextureStyleState("properties_bg").textColor;
        GUI.DrawTexture(rectPropertiesButton, AMTimeline.getSkinTextureStyleState("properties_bg").background);
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(rectPropertiesButton.x + 8f + (isInspectorOpen ? 1f : 0f), 12f, 22f, 19f), (isInspectorOpen ? texRightArrow : texLeftArrow));
        if(!isInspectorOpen) {
            int numSelected = 0;
            foreach(var pair in dictTracks) if(pair.Value == true) numSelected++;
            if(numSelected < dictTracks.Count) GUI.color = Color.red;
            GUI.Label(new Rect(rectPropertiesButton.x, rectPropertiesButton.y + rectPropertiesButton.height, rectPropertiesButton.width, 28f), numSelected + "/" + dictTracks.Count, styleLabelCentered);
            GUI.color = Color.white;
        }
        GUILayout.EndVertical();
        if(isInspectorOpen) {
            GUILayout.BeginVertical(GUILayout.Width(width_inspector_open - width_inspector_closed));
            GUILayout.Space(inspector_space);
            GUILayout.Label("Track Selection");
            GUILayout.Space(inspector_space);
            GUILayout.BeginHorizontal();
            //GUILayout.Space(inspector_space);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos/*,styleScrollView*/);
            for(int i = 0; i < AMTimeline.window.currentTake.rootGroup.elements.Count; i++) {
                int id = AMTimeline.window.currentTake.rootGroup.elements[i];
                //float height_group_elements = 0f;
                showGroupElement(id, 0);
            }
            GUILayout.EndScrollView();
            GUILayout.Space(inspector_space);
            GUILayout.EndHorizontal();
            // buttons
            GUILayout.Space(inspector_space);
            GUILayout.BeginHorizontal();
            Rect rectResizeInspector = new Rect(rectPropertiesButton.x, position.height - 15f - 8f, 15f, 15f);
            GUI.Button(rectResizeInspector, "", GUI.skin.GetStyle("ResizeTrackThumb"));
            EditorGUIUtility.AddCursorRect(rectResizeInspector, MouseCursor.ResizeHorizontal);
            if(rectResizeInspector.Contains(e.mousePosition) && mouseOverElement == (int)ElementType.None) {
                mouseOverElement = (int)ElementType.ResizeInspector;
            }

            if(GUILayout.Button("All", GUILayout.Width(42f))) {
                foreach(var key in dictTracks.Keys.ToList()) {
                    dictTracks[key] = true;
                }
                refreshCode();
            }
            if(GUILayout.Button("None", GUILayout.Width(42f))) {
                foreach(var key in dictTracks.Keys.ToList()) {
                    dictTracks[key] = false;
                }
                refreshCode();
            }
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("JSON...", GUILayout.Width(58f))) {
                // export json
                exportJSON();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(inspector_space);
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
        #endregion
        GUILayout.EndHorizontal();
    }

    void showGroupElement(int id, int group_lvl) {
        // returns true if mouse over track
        if(id >= 0) {
            AMTrack _track = AMTimeline.window.currentTake.getTrack(id);
            showTrack(_track, id, group_lvl);
        }
        else {
            showGroup(id, group_lvl);
        }
    }

    void showGroup(int id, int group_lvl) {
        // show group
        //float group_x = width_subtrack_space*group_lvl;

        //Rect rectGroup = new Rect(group_x,track_y,width_track-group_x,height_group);
        if(!dictGroups.ContainsKey(id)) dictGroups.Add(id, true);
        AMGroup grp = AMTimeline.window.currentTake.getGroup(id);
        GUILayout.BeginHorizontal();
        GUILayout.Space(width_indent * (group_lvl));	// indent
        // foldout
        GUILayout.BeginVertical(GUILayout.Width(15f));
        GUILayout.Space(height_label_offset - 1f);
        if(GUILayout.Button("", "label", GUILayout.Width(15f))) dictGroups[id] = !dictGroups[id];
        GUI.DrawTexture(GUILayoutUtility.GetLastRect(), (dictGroups[id] ? GUI.skin.GetStyle("GroupElementFoldout").normal.background : GUI.skin.GetStyle("GroupElementFoldout").active.background));
        GUILayout.EndVertical();
        // select children button
        if(hasTracks(grp)) {
            GUILayout.BeginVertical(GUILayout.Width(13f));
            GUILayout.Space(height_label_offset + 1f);
            if(GUILayout.Button("", GUILayout.Width(13f), GUILayout.Height(15f))) {
                bool? newValue = null;
                toggleChildren(grp, ref newValue);
                refreshCode();
                /*if(gameObjsSelected[i] != null) {
                    gameObjsSelected[i] = !gameObjsSelected[i];
                    setAllChildrenSelection((bool)gameObjsSelected[i], i, gameObjsDepth[i]);
                }*/

            }
            Rect rectSelectAllTexture = GUILayoutUtility.GetLastRect();
            GUILayout.EndVertical();

            rectSelectAllTexture.x += 3f;
            rectSelectAllTexture.y += 4f;
            rectSelectAllTexture.width = 7f;
            rectSelectAllTexture.height = 8f;
            if(!GUI.enabled) GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0.25f);
            GUI.DrawTexture(rectSelectAllTexture, AMTimeline.getSkinTextureStyleState("select_all").background);
            GUI.color = Color.white;
        }
        GUILayout.BeginVertical();
        GUILayout.Space(height_label_offset);
        GUILayout.Label(grp.group_name);
        GUILayout.EndHorizontal();
        GUILayout.EndHorizontal();
        group_lvl++;	// increment group_lvl for sub-elements
        if(dictGroups[id]) {
            for(int j = 0; j < grp.elements.Count; j++) {
                int _id = grp.elements[j];
                showGroupElement(_id, group_lvl);
            }
        }
    }
    bool hasTracks(AMGroup grp) {
        foreach(int id in grp.elements) {
            if(id >= 0) return true;
            else if(hasTracks(AMTimeline.window.currentTake.getGroup(id))) return true;
        }
        return false;
    }
    void toggleChildren(AMGroup grp, ref bool? newValue) {
        foreach(int _id in grp.elements) {
            if(_id < 0) toggleChildren(AMTimeline.window.currentTake.getGroup(_id), ref newValue);
            else {
                if(newValue == null) newValue = !dictTracks[_id];
                dictTracks[_id] = (bool)newValue;
            }

        }
    }
    void showTrack(AMTrack _track, int id, int group_level) {
        if(!dictTracks.ContainsKey(id)) dictTracks.Add(id, true);

        GUILayout.BeginHorizontal();
        GUILayout.Space(width_indent * group_level);
        bool prev = dictTracks[id];
        dictTracks[id] = GUILayout.Toggle(dictTracks[id], "");
        if(dictTracks[id] != prev) {
            // changed
            refreshCode();
        }
        GUILayout.BeginVertical();
        GUILayout.Space(height_label_offset);
        GUILayout.Label(_track.name);
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }
    string getCode() {
        if(!aData) return "";
        varNameDictionary = new Dictionary<string, string>();
        string code = "// " + AMTimeline.window.currentTake.name;
        code += "\n";
        appendGroupCode(AMTimeline.window.currentTake.getGroup(0), ref code);
        return code;
    }

    void refreshCode(bool resetControls = true) {
        if(shouldRefresh) shouldRefresh = false;
        codeCache = getCode();
        if(resetControls) {
            try {
                GUIUtility.keyboardControl = 0;
                GUIUtility.hotControl = 0;
            }
            catch {
                // do nothing
            }
        }
        this.Repaint();
    }

    void appendGroupCode(AMGroup _grp, ref string code) {
        foreach(int element_id in _grp.elements) {
            // track
            if(element_id > 0) {
                if(!dictTracks.ContainsKey(element_id)) dictTracks.Add(element_id, true);
                if(dictTracks[element_id] == true) {
                    AMTrack track = AMTimeline.window.currentTake.getTrack(element_id);
                    appendTrackCode(track, ref code);
                }
            }
            // group
            else if(element_id < 0) {
                AMGroup grp = AMTimeline.window.currentTake.getGroup(element_id);
                //code += "\n// "+grp.group_name+"\n";
                appendGroupCode(grp, ref code);
            }
        }
    }

    void appendTrackCode(AMTrack track, ref string code) {
        code = "//TODO: write code";
    }
    string getInitialPropertiesFor(int codeLanguage, AMTrack _track, string varName) {
        // initialize the starting variables (such as initial position and rotation), codeLanguage 0=C#, 1=JS
        if(_track is AMTranslationTrack) {
            // translation, set initial position
            Vector3 _position = (_track as AMTranslationTrack).getInitialPosition();
            if(codeLanguage == 0) return varName + ".transform.position = new Vector3(" + _position.x + "f, " + _position.y + "f, " + _position.z + "f); // Set Initial Position\n";
            else return varName + ".transform.position = Vector3(" + _position.x + ", " + _position.y + ", " + _position.z + "); // Set Initial Position\n";
        }
        else if(_track is AMRotationTrack) {
            // rotation, set initial rotation
            Vector4 _rotation = (_track as AMRotationTrack).getInitialRotation();
            if(codeLanguage == 0) return varName + ".transform.rotation = new Quaternion(" + _rotation.x + "f, " + _rotation.y + "f, " + _rotation.z + "f, " + _rotation.w + "f); // Set Initial Rotation\n";
            else return varName + ".transform.rotation = Quaternion(" + _rotation.x + ", " + _rotation.y + ", " + _rotation.z + ", " + _rotation.w + "); // Set Initial Rotation\n";
        }
        else if(_track is AMOrientationTrack && (_track as AMOrientationTrack).getInitialTarget(aData)) {
            // orientation, set initial look
            Transform _target = (_track as AMOrientationTrack).getInitialTarget(aData);
            int start_frame = 0;
            AMTrack _translation_track = null;
            if((_track as AMOrientationTrack).keys.Count > 0) start_frame = (_track as AMOrientationTrack).keys[0].frame;
            if(start_frame > 0) _translation_track = AMTimeline.window.currentTake.getTranslationTrackForTransform(aData, _target);
            Vector3 _lookv3 = _target.transform.position;
            if(_translation_track) _lookv3 = (_translation_track as AMTranslationTrack).getPositionAtFrame((_translation_track as AMTranslationTrack).GetTarget(aData) as Transform, start_frame, AMTimeline.window.currentTake.frameRate, false);

            if(codeLanguage == 0) return varName + ".transform.LookAt (new Vector3(" + _lookv3.x + "f, " + _lookv3.y + "f, " + _lookv3.z + "f)); // Set Initial Orientation\n";
            else return varName + ".transform.LookAt (Vector3(" + _lookv3.x + ", " + _lookv3.y + ", " + _lookv3.z + ")); // Set Initial Orientation\n";
        }
        else if(_track is AMPropertyTrack) {
            // property, set initial value	
            return (_track as AMPropertyTrack).getValueInitialization(codeLanguage, varName) + " // Set Initial Value\n";
        }
        return null;
    }
    string[] getMethodInfoInitialization(Component component, string methodName, string varName, int codeLanguage) {
        // string initialization, get array of strings. string[0] is component init, string[1] is methodinfo init
        if(!component) return new string[] { null, null };
        string[] _out = new string[2];
        // component
        string comp_start = "";
        if(codeLanguage == 0) comp_start += "Component ";
        else comp_start += "var ";
        comp_start += varName + "CMP = ";
        string comp_new = "GameObject.Find(\"" + component.gameObject.name + "\").GetComponent(\"" + component.GetType().Name + "\")";
        _out[0] = comp_start + getDictionaryValue(comp_new, varName + "CMP", false);
        //method info
        string methodinfo_start = "";
        if(codeLanguage == 0) methodinfo_start += "MethodInfo ";
        else methodinfo_start += "var ";
        methodinfo_start += varName + " = ";
        string methodinfo_new = "GameObject.Find(\"" + component.gameObject.name + "\").GetComponent(\"" + component.GetType().Name + "\").GetType().GetMethod(\"" + methodName + "\")";
        _out[1] = methodinfo_start + getDictionaryValue(methodinfo_new, varName, false);
        return _out;
    }
    string getObjectInitialization(string type, string varName, string gameObjectName, int codeLanguage, string memberInfoName, string memberInfoComponentName) {
        string s = "", sNew = "";
        if(codeLanguage == 0) {
            //if((type == "FieldInfo")||(type == "PropertyInfo")||(type == "MethodInfo")) s += "System.Reflection.";
            s += type;
            s += " ";
        }
        else s += "var ";

        // find new
        if((type == "FieldInfo") || (type == "PropertyInfo") || (type == "MethodInfo"))
            s += varName + "Property = ";
        else if(type == "AudioClip") {
            s += varName + " = ";
            if(codeLanguage == 0) sNew += "(AudioClip) ";
            sNew += "Resources.Load(\"" + gameObjectName + "\"); // Put AudioClips in Resources Folder";
        }
        else {
            s += varName + " = ";
            sNew = "GameObject.Find(\"" + gameObjectName + "\")";

        }

        if(type == "AudioClip" || type == "GameObject") {
            // do nothing, to avoid else statement where GetComponent is appended
        }
        else if(type == "FieldInfo") {
            sNew += varName + ".GetType().GetField(\"" + memberInfoName + "\")";
        }
        else if(type == "PropertyInfo") {
            sNew += varName + ".GetType().GetProperty(\"" + memberInfoName + "\")";
        }
        else if(type == "MethodInfo") {
            if(memberInfoName == "Morph") {
                //typeMegaMorph.GetMethod("SetPercent",new Type[]{typeof(int),typeof(float)})
                sNew += varName + ".GetType().GetMethod(\"SetPercent\", ";
                if(codeLanguage == 0) sNew += "new System.Type[]{typeof(int), typeof(float)}";
                else sNew += "[typeof(int), typeof(float)]";
                sNew += ")";
            }
            else {
                sNew += varName + ".GetType().GetMethod(\"" + memberInfoName + "\")";
            }
        }
        else {
            sNew += ".GetComponent(\"";
            if(memberInfoComponentName != null) sNew += memberInfoComponentName;
            else sNew += type;
            sNew += "\")";
            if(codeLanguage == 0 && type != "Component") sNew = "(" + type + ") " + sNew;
        }
        sNew = getDictionaryValue(sNew, varName, false);

        s += sNew + "\n";

        return s;

    }
    public static string getDictionaryValue(string sNew, string varName, bool? brief) {
        // if already in dictionary, retrieve variable name
        if(varNameDictionary.ContainsKey(sNew)) {
            if(brief == true) return varNameDictionary[sNew] + " /* or use " + sNew + " */";
            else if(brief == false) return varNameDictionary[sNew] + "; // or use " + sNew + ";";
            else return varNameDictionary[sNew];	// brief is null, no comment

        }
        if(varName == null) return sNew;
        // add to dictionary
        varNameDictionary.Add(sNew, varName);
        // if starts with GameObject, check dictionary
        string[] _components = getComponentsAsStrings(sNew);
        // minus 2 toskip last component as it has been checked
        for(int i = _components.Length - 2; i >= 0; i--) {
            string s_prefix = "";
            for(int j = 0; j <= i; j++) {
                if(j > 0) s_prefix += ".";
                s_prefix += _components[j];
            }
            if(varNameDictionary.ContainsKey(s_prefix)) {

                string s_final = "";
                // get suffix
                for(int k = i + 1; k < _components.Length; k++) s_final += "." + _components[k];

                // append with prefix
                s_final = varNameDictionary[s_prefix] + s_final;
                if(brief == true) return s_final + " /* or use " + sNew + " */";
                else if(brief == false) return s_final + "; // or use " + sNew + ";";
                else return s_final;
            }
        }
        // if expression ends with ')' then end line with ';' (AudioClips end with a comment and are therefore not appended)
        if(brief != null && sNew[sNew.Length - 1] == ')') sNew += ";";
        return sNew;
    }
    public static string[] getComponentsAsStrings(string s) {
        string[] _s = s.Split(')');
        string[] _out = new string[_s.Length - 1];
        for(int i = 0; i < _s.Length - 1; i++) {
            _s[i] += ')';
            if(_s[i][0] == '.') _s[i] = _s[i].Substring(1);
            _out[i] = (_s[i]);
        }
        return _out;
    }
    public static string ReverseString(string s) {
        char[] arr = s.ToCharArray();
        Array.Reverse(arr);
        return new string(arr);
    }

    public static void resetTrackDictionary() {
        dictTracks = new Dictionary<int, bool>();
        dictGroups = new Dictionary<int, bool>();
        if(window) window.refreshCode(false);
    }

    public static void refresh() {
        AMCodeView.shouldRefresh = true;
        if(window) window.Repaint();
    }

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
            if(mouseOverElement == (int)ElementType.ResizeInspector) {
                dragType = (int)DragType.ResizeInspector;
                startScrubMousePosition = currentMousePosition;
                start_width_inspector_open = width_inspector_open;
            }
            else {
                // if did not drag from a draggable element
                dragType = (int)DragType.None;
            }
            // reset drag
            justStartedDrag = false;
        }
        else if(justFinishedDrag) {
            dragType = (int)DragType.None;
            // reset drag
            justFinishedDrag = false;
        }
    }
    #endregion

    // JSON



    void exportJSON() {

    }
}