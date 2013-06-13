using UnityEngine;
using System.Collections;
using System.Reflection;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class AMCameraFade : MonoBehaviour {


    // Adding New Transitions
    // =======================
    // Add Transition to AMTween.Fade Enum & AMTween.TransitionNamesDict; Add to ordered arrays for Transition Picker: AMTween.TransitionNames, AMTween.TransitionOrder
    // Set defaults in Defaults class here
    // Create processFadeName method here and put it in processCameraFade
    // Add transition to isTransitionReversed in AMTween if reversing textures is required (ex: Iris Round -> Grow requires second target as the first texture)
    // If transition uses texture, add to needsTexture here
    // Setup transition in setupMaterials here if necessary
    // Put transition in setParametersFor and setDefaultParametersFor in AMTransitionPicker

    #region Declarations
    // static
    private static AMCameraFade _cf = null;

    public Texture2D blankTexture;

    // materials
    public Material matIris;

    //private static Material matIris = null;
    // show
    public bool deleteMe = false;
    public bool updateTexture = false;

    // hide in inspector
    [HideInInspector]
    public int keepAlives = 0;
    [HideInInspector]
    public bool keepAliveColor = false;
    [HideInInspector]
    public bool keepAlivePreview = false;
    [HideInInspector]
    public bool keepAliveAwake = true;
    [HideInInspector]
    public int mode = 0;
    [HideInInspector]
    public float percent;
    /*[HideInInspector]*/
    public float value;
    [HideInInspector]
    public bool useRenderTexture = false;
    [HideInInspector]
    public Texture2D tex2d;
    [HideInInspector]
    public RenderTexture _tex;
    [HideInInspector]
    private RenderTexture tex {
        get {
            return getRenderTexture();
        }
    }
    [HideInInspector]
    public float[] r;
    [HideInInspector]
    public Camera renderTextureCamera;
    [HideInInspector]
    public bool hasColorTex = false;
    [HideInInspector]
    public bool hasColorBG = false;
    [HideInInspector]
    public Color colorTex;
    [HideInInspector]
    public Color colorBG;
    [HideInInspector]
    public int width = 0;
    [HideInInspector]
    public int height = 0;
    [HideInInspector]
    public bool placeholder = false;
    [HideInInspector]
    public Texture2D irisShape;
    [HideInInspector]
    public Texture background;
    [HideInInspector]
    public int cachedStillFrame = 0;
    [HideInInspector]
    public bool isReset = false;
    [HideInInspector]
    public bool shouldUpdateStill = false;
    [HideInInspector]
    public bool shouldUpdateRenderTexture = false;
    [HideInInspector]
    public bool preview = false;
    #endregion

    #region Main
    void OnDestroy() {
        destroyImmediate(false);
    }

    void Awake() {
        if(Application.isPlaying && preview) destroy();
        else {
            this.camera.clearFlags = CameraClearFlags.Nothing;
            this.camera.cullingMask = 0;
            this.camera.depth = -1;
            keepAliveAwake = true;
        }
    }

    void OnGUI() {

        if(isReset || (!hasColorTex && ((!useRenderTexture && !tex2d) || (useRenderTexture && !renderTextureCamera)))) return;
        GUI.depth = -9999999;

        // background color texture
        if(hasColorBG) {
            GUI.color = new Color(colorBG.r, colorBG.g, colorBG.b, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), blankTexture);
        }
        // foreground color texture
        if(hasColorTex) GUI.color = new Color(colorTex.r, colorTex.g, colorTex.b, 1f);
        else GUI.color = Color.white;
        // process camerafade
        processCameraFade(new Rect(0f, 0f, Screen.width, Screen.height), (hasColorTex ? blankTexture : (useRenderTexture ? (Texture)tex : (Texture)tex2d)), mode, value, percent, r, irisShape, Event.current);
        // editor
        if(!Application.isPlaying) {
            // update textures if aspect has changed
            float aspect = 0f;
            bool wasBadFrame = false;
            if(height > 0f) aspect = (float)width / (float)height;
            else wasBadFrame = true;
            width = Screen.width;
            height = Screen.height;
            float newAspect = (float)width / (float)height;
            if(wasBadFrame || (aspect > 0f && newAspect.ToString("N1") != aspect.ToString("N1"))) doForceUpdate(false);
            // show indie placeholder
            if(!isReset && placeholder) {
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0f, Screen.height - 25f, Screen.width, 25f), tex);
                GUI.color = Color.white;
                GUI.Label(new Rect(5f, Screen.height - 20f - 2f, Screen.width - 10f, 20f), "This is a placeholder. Transitions can only be previewed with a Unity Pro license.");
            }
        }

    }

    void Update() {
        if(deleteMe) Debug.Log("Animator: Deleted AMCameraFade" + (preview ? "Preview" : ""));
        if(!Application.isPlaying) {
            if(deleteMe) destroyImmediate();
            else if(updateTexture) doForceUpdate(true);
            return;
        }
        if(preview || deleteMe || (!keepAliveAwake && !keepAliveColor && keepAlives <= 0 && !keepAlivePreview)) destroy();
    }

    void doForceUpdate(bool showMsg = false) {
        if(useRenderTexture) shouldUpdateRenderTexture = true;
        else doShouldUpdateStill(); //shouldUpdateStill = true;
        updateTexture = false;
        if(showMsg) Debug.Log("Animator: Updated AMCameraFade" + (preview ? "Preview" : "") + " Texture");
    }

    public void destroyImmediate(bool destroySelf=true) {
        AMTween.Stop(this.gameObject);
        if(_tex) {
            if(renderTextureCamera && renderTextureCamera.targetTexture == _tex) {
                renderTextureCamera.targetTexture = null;
            }
            DestroyImmediate(_tex);
        }
        /*if(_matIris) {
            DestroyImmediate(_matIris.shader);
            DestroyImmediate(_matIris);
        }*/
        _cf = null;

        if(destroySelf)
            DestroyImmediate(this.gameObject);

    }

    public void destroy() {
        AMTween.Stop(this.gameObject);
        if(_tex) {
            if(renderTextureCamera && renderTextureCamera.targetTexture == _tex) {
                renderTextureCamera.targetTexture = null;
            }
            Destroy(_tex);
        }
        /*if(_matIris) {
            Destroy(_matIris.shader);
            Destroy(_matIris);
        }*/
        _cf = null;
        Destroy(this.gameObject);

    }
    #endregion

    #region Common
    public static class Defaults {
        public static class Doors {
            public const float layout = 0f;	// vertical / horizontal
            public const float type = 0f; // open / close
            public const float focalXorY = .5f;
        }

        public static class VenetianBlinds {
            public const float layout = 0f; // vertical / horizontal
            public const int numberOfBlinds = 5;
        }

        public static class IrisBox {
            public const float type = 0f;
            public const float focalX = .5f;
            public const float focalY = .5f;
        }

        public static class IrisRound {
            public const float type = 0f;	// shrink / grow
            public const float maxScale = 2.5f;
            public const float focalX = .5f;
            public const float focalY = .5f;
        }

        public static class IrisShape {
            public const float type = 0f;	// shrink / grow
            public const float maxScale = 6f;
            public const int rotateAmount = 50;
            public const float easeRotation = 0f;
            public const float focalX = .5f;
            public const float focalY = .5f;
            public const float pivotX = .5f;
            public const float pivotY = .5f;
        }

        public static class ShapeWipe {
            public const int angle = 0;
            public const float scale = 6f;
            public const float padding = 0f;
            public const float offsetStart = 3f;
            public const float offsetEnd = -3f;
        }

        public static class LinearWipe {
            public const int angle = 0;
            public const float padding = .25f;
        }

        public static class RadialWipe {
            public const float direction = 0f; // clockwise / counterclockwise
            public const int startingAngle = 0;
            public const float focalX = .5f;
            public const float focalY = .5f;
        }

        public static class WedgeWipe {
            public const int startingAngle = 0;
            public const float focalX = .5f;
            public const float focalY = .5f;
        }
    }
    // process camera fade
    public void processCameraFade(Rect rect, Texture tex, int mode, float value, float percent, float[] r, Texture2D _irisShape, Event editorEvent) {
        if(_irisShape != null && matIris.GetTexture("_Mask") != _irisShape) matIris.SetTexture("_Mask", _irisShape);
        switch(mode) {
            #region doors
            case (int)AMTween.Fade.Doors:
                bool doors_open = ((r.Length >= 2 ? r[1] : Defaults.Doors.type) == 0f);
                float doors_focal = (r.Length >= 3 ? Mathf.Clamp(r[2], 0f, 1f) : Defaults.Doors.focalXorY);
                // if vertical
                if(r.Length >= 1 ? r[0] == 0f : Defaults.Doors.type == 0f) {
                    // if open
                    if(doors_open) processDoorsVerticalOpen(rect, tex, value, doors_focal);
                    else processDoorsVerticalClose(rect, tex, value, doors_focal);
                }
                else {
                    // if open
                    if(doors_open) processDoorsHorizontalOpen(rect, tex, value, doors_focal);
                    else processDoorsHorizontalClose(rect, tex, value, doors_focal);
                }
                break;
            #endregion
            #region venetian blinds
            case (int)AMTween.Fade.VenetianBlinds:
                // if vertical
                if((r.Length >= 1 ? r[0] : Defaults.VenetianBlinds.layout) == 0f) {
                    processVenetianBlindsVertical(rect, tex, value, (r.Length >= 2 ? (int)r[1] : Defaults.VenetianBlinds.numberOfBlinds));
                }
                else {
                    processVenetianBlindsHorizontal(rect, tex, value, (int)r[1]);
                }
                break;
            #endregion
            #region iris box
            case (int)AMTween.Fade.IrisBox:
                // if shrink
                if((r.Length >= 1 ? r[0] : Defaults.IrisBox.type) == 0f) {
                    processIrisBoxShrink(rect, tex, value, (r.Length >= 2 ? Mathf.Clamp(r[1], 0f, 1f) : Defaults.IrisBox.focalX), (r.Length >= 3 ? Mathf.Clamp(r[2], 0f, 1f) : Defaults.IrisBox.focalY));
                }
                else {
                    processIrisBoxGrow(rect, tex, value, (r.Length >= 2 ? Mathf.Clamp(r[1], 0f, 1f) : Defaults.IrisBox.focalX), (r.Length >= 3 ? Mathf.Clamp(r[2], 0f, 1f) : Defaults.IrisBox.focalY));
                }
                break;
            #endregion
            #region iris round
            case (int)AMTween.Fade.IrisRound:
                processIrisRound(rect, tex,
                    /* type, shrink/grow*/ ((r.Length >= 1 ? r[0] : Defaults.IrisRound.type) == 0f), value,
                    /* max scale		*/ (r.Length >= 2 ? r[1] : Defaults.IrisRound.maxScale),
                    /* focal x			*/ (r.Length >= 3 ? Mathf.Clamp(r[2], 0f, 1f) : Defaults.IrisRound.focalX),
                    /* focal y			*/ (r.Length >= 4 ? Mathf.Clamp(r[3], 0f, 1f) : Defaults.IrisRound.focalY),
                _irisShape,
                editorEvent);
                break;
            #endregion
            #region iris shape
            case (int)AMTween.Fade.IrisShape:
                processIrisShape(rect, tex,
                    /* type, shrink/grow*/ ((r.Length >= 1 ? r[0] : Defaults.IrisShape.type) == 0f), value, percent,
                    /* focal x			*/ (r.Length >= 5 ? Mathf.Clamp(r[4], 0f, 1f) : Defaults.IrisShape.focalX),
                    /* focal y			*/ (r.Length >= 6 ? Mathf.Clamp(r[5], 0f, 1f) : Defaults.IrisShape.focalY),
                    /* shape			*/ _irisShape,
                    /* max scale		*/ (r.Length >= 2 && r[1] > 0f ? r[1] : Defaults.IrisShape.maxScale),
                    /* pivot x			*/ (r.Length >= 7 ? r[6] : Defaults.IrisShape.pivotX),
                    /* pivot y			*/ (r.Length >= 8 ? r[7] : Defaults.IrisShape.pivotY),
                    /* rotate amount	*/ (r.Length >= 3 ? (int)r[2] : Defaults.IrisShape.rotateAmount),
                    /* ease rotation	*/ ((r.Length >= 4 ? r[3] : Defaults.IrisShape.easeRotation) == 1f ? true : false),
                editorEvent);
                break;
            #endregion
            #region linear wipe
            case (int)AMTween.Fade.LinearWipe:
                processLinearWipe(rect, tex, value, /*angle*/(r.Length >= 1 ? (int)r[0] : Defaults.LinearWipe.angle), /*padding*/(r.Length >= 2 ? r[1] : Defaults.LinearWipe.padding), _irisShape, editorEvent);
                break;
            #endregion
            #region shape wipe
            case (int)AMTween.Fade.ShapeWipe:
                processShapeWipe(rect, tex, value, _irisShape,
                    /* angle			*/ (r.Length >= 1 ? (int)r[0] : Defaults.ShapeWipe.angle),
                    /* scale			*/ (r.Length >= 2 ? r[1] : Defaults.ShapeWipe.scale),
                    /* padding			*/ (r.Length >= 3 ? r[2] : Defaults.ShapeWipe.padding),
                    /* offset start		*/ (r.Length >= 4 ? r[3] : Defaults.ShapeWipe.offsetStart),
                    /* offset end		*/ (r.Length >= 5 ? r[4] : Defaults.ShapeWipe.offsetEnd),
                editorEvent);
                break;
            #endregion
            #region radial wipe
            case (int)AMTween.Fade.RadialWipe:
                processRadialWipe(rect, tex, value,
                    /* direction clockwise / counter */ ((r.Length >= 1 ? r[0] : Defaults.RadialWipe.direction) == 0f),
                    /* starting angle				 */ (r.Length >= 2 ? (int)r[1] : Defaults.RadialWipe.startingAngle),
                    /* focal x						 */ (r.Length >= 3 ? Mathf.Clamp(r[2], 0f, 1f) : Defaults.RadialWipe.focalX),
                    /* focal y						 */ (r.Length >= 4 ? Mathf.Clamp(r[3], 0f, 1f) : Defaults.RadialWipe.focalY),
                editorEvent);
                break;
            #endregion
            #region radial wipe
            case (int)AMTween.Fade.WedgeWipe:
                processWedgeWipe(rect, tex, value,
                    /* starting angle				 */ (r.Length >= 1 ? (int)r[0] : Defaults.WedgeWipe.startingAngle),
                    /* focal x						 */ (r.Length >= 2 ? Mathf.Clamp(r[1], 0f, 1f) : Defaults.WedgeWipe.focalX),
                    /* focal y						 */ (r.Length >= 3 ? Mathf.Clamp(r[2], 0f, 1f) : Defaults.WedgeWipe.focalY),
                editorEvent);
                break;
            #endregion
            #region default / crossfade
            default:
                processAlpha(rect, tex, value);
                break;
            #endregion
        }
    }

    // setup transition materials and matrices
    public void setupMaterials() {
        switch(mode) {
            case (int)AMTween.Fade.IrisRound:
            case (int)AMTween.Fade.IrisShape:
            case (int)AMTween.Fade.LinearWipe:
            case (int)AMTween.Fade.ShapeWipe:
            case (int)AMTween.Fade.RadialWipe:
            case (int)AMTween.Fade.WedgeWipe:
                matIris.SetMatrix("_MatrixTex", Matrix4x4.zero);
                // set mask
                matIris.SetTexture("_Mask", irisShape);
                break;
            default:
                break;
        }
    }

    // does the transition type require a texture
    public static bool needsTexture(int mode) {
        switch(mode) {
            case (int)AMTween.Fade.IrisShape:
            case (int)AMTween.Fade.IrisRound:
            case (int)AMTween.Fade.ShapeWipe:
            case (int)AMTween.Fade.LinearWipe:
            case (int)AMTween.Fade.RadialWipe:
            case (int)AMTween.Fade.WedgeWipe:
                return true;
            default:
                return false;
        }
    }
    #endregion

    #region Process
    public void processAlpha(Rect rect, Texture tex, float value) {
        GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, value);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, rect.height), tex);
    }

    public void processDoorsVerticalOpen(Rect rect, Texture tex, float value, float focalX) {
        float width_a = rect.width * focalX * value;
        float width_b = (rect.width - rect.width * focalX) * value;
        float left_a = 0f;
        float left_b = rect.width - width_b;

        float coords_width_a = focalX * value;
        float coords_width_b = (1f - focalX) * value;
        float coords_left_a = 0f;
        float coords_left_b = 1f - coords_width_b;

        GUI.DrawTextureWithTexCoords(new Rect(rect.x + left_a, rect.y, width_a, rect.height), tex, new Rect(coords_left_a, 0f, coords_width_a, 1f));
        GUI.DrawTextureWithTexCoords(new Rect(rect.x + left_b, rect.y, width_b, rect.height), tex, new Rect(coords_left_b, 0f, coords_width_b, 1f));
    }

    public void processDoorsVerticalClose(Rect rect, Texture tex, float value, float focalX) {
        float width_a = rect.width * focalX * value;
        float width_b = (rect.width - rect.width * focalX) * value;
        float left_a = rect.width * focalX - width_a;
        float left_b = rect.width * focalX;

        float coords_width_a = focalX * value;
        float coords_width_b = (1f - focalX) * value;
        float coords_left_a = focalX - coords_width_a;
        float coords_left_b = focalX;


        GUI.DrawTextureWithTexCoords(new Rect(rect.x + left_a, rect.y, width_a, rect.height), tex, new Rect(coords_left_a, 0f, coords_width_a, 1f));
        GUI.DrawTextureWithTexCoords(new Rect(rect.x + left_b, rect.y, width_b, rect.height), tex, new Rect(coords_left_b, 0f, coords_width_b, 1f));
    }

    public void processDoorsHorizontalOpen(Rect rect, Texture tex, float value, float focalY) {
        float height_a = rect.height * focalY * value;
        float height_b = (rect.height - rect.height * focalY) * value;
        float top_a = 0f;
        float top_b = rect.height - height_b;

        float coords_height_a = focalY * value;
        float coords_height_b = (1f - focalY) * value;
        float coords_bottom_a = 1f - focalY * value;
        float coords_bottom_b = 0f;

        GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y + top_a, rect.width, height_a), tex, new Rect(0f, coords_bottom_a, 1f, coords_height_a));
        GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y + top_b, rect.width, height_b), tex, new Rect(0f, coords_bottom_b, 1f, coords_height_b));
    }

    public void processDoorsHorizontalClose(Rect rect, Texture tex, float value, float focalY) {
        float height_a = rect.height * focalY * value;
        float height_b = (rect.height - rect.height * focalY) * value;
        float top_a = rect.height * focalY - height_a;
        float top_b = rect.height * focalY;

        float coords_height_a = focalY * value;
        float coords_height_b = (1f - focalY) * value;
        float coords_bottom_a = 1f - focalY;
        float coords_bottom_b = (1f - focalY) - coords_height_b;

        GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y + top_a, rect.width, height_a), tex, new Rect(0f, coords_bottom_a, 1f, coords_height_a));
        GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y + top_b, rect.width, height_b), tex, new Rect(0f, coords_bottom_b, 1f, coords_height_b));
    }

    public void processVenetianBlindsVertical(Rect rect, Texture tex, float value, int resolution) {
        for(int i = 0; i < resolution; i++) {
            float left = (rect.width / (float)resolution) * (float)i + (1f - value) * (rect.width / (float)resolution) / 2f;
            float width = (rect.width / (float)resolution) * value;
            float coordsleft = (1f / (float)resolution) * (float)i + (1f - value) * (1f / (float)resolution) / 2f;
            float coordswidth = (1f / (float)resolution) * value;

            GUI.DrawTextureWithTexCoords(new Rect(rect.x + left, rect.y, width, rect.height), tex, new Rect(coordsleft, 0f, coordswidth, 1f));
        }
    }

    public void processVenetianBlindsHorizontal(Rect rect, Texture tex, float value, int resolution) {
        for(int i = 0; i < resolution; i++) {
            float top = rect.height - (rect.height / (float)resolution) * (float)(i + 1) + (1f - value) * (rect.height / (float)resolution) / 2f;
            float height = (rect.height / (float)resolution) * value;
            float coordstop = (1f / (float)resolution) * (float)i + (1f - value) * (1f / (float)resolution) / 2f;
            float coordsheight = (1f / (float)resolution) * value;

            GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y + top, rect.width, height), tex, new Rect(0f, coordstop, 1f, coordsheight));
        }
    }

    public void processIrisBoxGrow(Rect rect, Texture tex, float value, float focalX, float focalY) {
        float left = rect.width * focalX * value;
        float top = rect.height * focalY * value;
        float width = rect.width * (1f - value);
        float height = rect.height * (1f - value);

        float coordsleft = focalX * value;
        float coordsbottom = (1f - focalY) * value;
        float coordswidth = 1f - value;
        float coordsheight = 1f - value;

        GUI.DrawTextureWithTexCoords(new Rect(rect.x + left, rect.y + top, width, height), tex, new Rect(coordsleft, coordsbottom, coordswidth, coordsheight));
    }

    public void processIrisBoxShrink(Rect rect, Texture tex, float value, float focalX, float focalY) {
        float left = rect.width * focalX * (1f - value);
        float top = rect.height * focalY * (1f - value);
        float width = rect.width * value;
        float height = rect.height * value;

        float coordsleft = focalX * (1f - value);
        float coordsbottom = (1f - focalY) * (1f - value);
        float coordswidth = value;
        float coordsheight = value;

        GUI.DrawTextureWithTexCoords(new Rect(rect.x + left, rect.y + top, width, height), tex, new Rect(coordsleft, coordsbottom, coordswidth, coordsheight));
    }

    public void processIrisRound(Rect rect, Texture tex, bool shrink, float value, float maxScale, float focalX, float focalY, Texture2D irisShape, Event editorEvent) {
        processIrisShape(rect, tex, shrink, value, 1f, focalX, focalY, irisShape, maxScale, .5f, .5f, 0, true, editorEvent);
    }

    public void processIrisShape(Rect rect, Texture tex, bool shrink, float value, float percent, float focalX, float focalY, Texture shape, float maxScale, float pivotX, float pivotY, int rotateAmount, bool easeRotation, Event editorEvent) {
        if(editorEvent.type.Equals(EventType.Repaint)) {
            //matIris.SetTexture("_Mask",shape);
            focalY = 1f - focalY;
            float scale = (shrink ? value : 1f - value) * maxScale;
            if(scale <= 0.01f) scale = 0f;
            // set cutoff
            if(scale <= 0.04) matIris.SetFloat("_Cutoff", 0.8f);
            else matIris.SetFloat("_Cutoff", 0.1f);
            float aspect = rect.width / rect.height;
            Vector2 pos = new Vector2(0f, 0f);
            pos.x = focalX - (scale / 2f) / aspect;
            pos.y = focalY - .5f * scale;
            Vector3 _scale = new Vector3((1f / scale) * aspect, (1f / scale), 1f);
            Vector3 _position = new Vector3(-1f * pos.x * _scale.x, -1f * pos.y * _scale.y, 0f);
            float value2 = ((shrink ? 0f : 1f) - value);
            pivotY = 1f - pivotY;
            pivotX = pivotX / 2f + .25f;
            pivotY = pivotY / 2f + .25f;
            Vector2 pivot = new Vector2(pivotX + (.5f - pivotX) * value2, pivotY + (.5f - pivotY) * value2);
            Matrix4x4 t = Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
            float _rotate = (float)rotateAmount * 5f;
            if(easeRotation) _rotate *= value2;
            else _rotate *= percent;
            Quaternion _rotation = Quaternion.Euler(0, 0, _rotate);
            Matrix4x4 r = Matrix4x4.TRS(Vector3.zero, _rotation, Vector3.one);
            Matrix4x4 tInv = Matrix4x4.TRS(pivot, Quaternion.identity, Vector3.one);
            Matrix4x4 f = Matrix4x4.TRS(_position, Quaternion.identity, _scale);
            matIris.SetMatrix("_Matrix", tInv * r * t * f);
            matIris.color = GUI.color;
            Graphics.DrawTexture(new Rect(rect.x, rect.y, rect.width, rect.height), tex, matIris);
        }

    }

    public void processLinearWipe(Rect rect, Texture tex, float value, int angle, float padding, Texture2D irisShape, Event editorEvent) {

        processShapeWipe(rect, tex, value, irisShape, angle, 1f, /*.25f*/padding, 0f, 0f, editorEvent);
    }

    public void processShapeWipe(Rect rect, Texture tex, float value, Texture shape, int angle, float scale, float padding, float offsetStart, float offsetEnd, Event editorEvent) {
        if(editorEvent.type.Equals(EventType.Repaint)) {
            //matIris.SetTexture("_Mask",shape);
            //if(value <= 0.1) matIris.SetFloat("_Cutoff",0.1f);
            int a = angle + 90;
            float aspect = rect.width / rect.height;
            Vector3 _scale = new Vector3((1f / scale) * aspect, (1f / scale), 1f);
            Vector2 startPos = getPositionAtAngle(a, padding);
            Vector2 endPos = getPositionAtAngle((a + 180) % 360, padding);
            Vector2 dir = (endPos - startPos).normalized;
            startPos = startPos + -dir * offsetEnd;
            endPos = endPos + -dir * offsetStart;
            Vector2 pivot = new Vector2(.5f, .5f);
            Vector2 diff = new Vector2(endPos.x - startPos.x, endPos.y - startPos.y);
            Vector2 pos = new Vector2(0f, 0f);
            pos.x = startPos.x + diff.x * value;
            pos.y = startPos.y + diff.y * value;
            Vector2 _position = new Vector3(-pos.x * _scale.x + .5f, -pos.y * _scale.y + .5f);
            Matrix4x4 t = Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
            Quaternion _rotation = Quaternion.Euler(0, 0, a - 90);
            Matrix4x4 r = Matrix4x4.TRS(Vector3.zero, _rotation, Vector3.one);
            Matrix4x4 tInv = Matrix4x4.TRS(pivot, Quaternion.identity, Vector3.one);
            Matrix4x4 f = Matrix4x4.TRS(_position, Quaternion.identity, _scale);
            matIris.SetMatrix("_Matrix", tInv * r * t * f);
            if(matIris.color != GUI.color) matIris.color = GUI.color;
            Graphics.DrawTexture(new Rect(rect.x, rect.y, rect.width, rect.height), tex, matIris);
        }
    }

    public void processRadialWipe(Rect rect, Texture tex, float value, bool clockwise, int startingAngle, float focalX, float focalY, Event editorEvent) {
        if(value <= 0f) return;
        if(value >= 1f) {
            GUI.DrawTexture(rect, tex);
            return;
        }
        if(editorEvent.type.Equals(EventType.Repaint)) {
            if(!clockwise) startingAngle = (startingAngle + 180) % 360;
            // set cutoff
            matIris.SetFloat("_Cutoff", 0f);
            float scale = 1f;
            //float padding = 0f;
            float a = (startingAngle + 90) + 360 * (1f - value) * (clockwise ? 1f : -1f);
            float aspect = rect.width / rect.height;
            Vector3 _scale = new Vector3((1f / scale) * aspect, (1f / scale), 1f);
            Vector2 pivot = new Vector2(.5f, .5f);
            Vector2 pos = new Vector2(focalX, 1f - focalY);
            Vector2 _position = new Vector3(-pos.x * _scale.x + .5f, -pos.y * _scale.y + .5f);
            Matrix4x4 t = Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
            Quaternion _rotation = Quaternion.Euler(0, 0, a - 90);
            Matrix4x4 r = Matrix4x4.TRS(Vector3.zero, _rotation, Vector3.one);
            Matrix4x4 tInv = Matrix4x4.TRS(pivot, Quaternion.identity, Vector3.one);
            Matrix4x4 f = Matrix4x4.TRS(_position, Quaternion.identity, _scale);
            matIris.SetMatrix("_Matrix", tInv * r * t * f);
            if(matIris.color != GUI.color) matIris.color = GUI.color;
            Rect rectSource = new Rect(0f, 0f, 1f, 1f);
            Rect rectSourceHalf = new Rect(0f, 0f, 1f, 1f);
            Rect rectPositionHalf = new Rect(rect);
            bool showHalf = value > 0.5f;
            if(startingAngle == 0f) {
                rectSourceHalf.width = focalX;
                rectPositionHalf.width = rect.width * focalX;
            }
            else if(startingAngle == 90f) {
                rectSourceHalf.y = 1f - focalY;
                rectSourceHalf.height = focalY;
                rectPositionHalf.height = rect.height * focalY;
            }
            else if(startingAngle == 180f) {
                rectSourceHalf.x = focalX;
                rectSourceHalf.width = 1f - focalX;
                rectPositionHalf.x = rect.x + rect.width * focalX;
                rectPositionHalf.width = rect.width * (1f - focalX);
            }
            else if(startingAngle == 270f) {
                rectSourceHalf.height = 1f - focalY;
                rectPositionHalf.y = rect.y + rect.height * focalY;
                rectPositionHalf.height = rect.height * (1f - focalY);
            }
            Graphics.DrawTexture((!showHalf ? rectPositionHalf : rect), tex, (!showHalf ? rectSourceHalf : rectSource), 0, 0, 0, 0, matIris);
            if(showHalf) {
                GUI.DrawTextureWithTexCoords(rectPositionHalf, tex, rectSourceHalf);
            }
        }
    }

    public void processWedgeWipe(Rect rect, Texture tex, float value, int startingAngle, float focalX, float focalY, Event editorEvent) {
        if(value <= 0f) return;
        if(value >= 1f) {
            GUI.DrawTexture(rect, tex);
            return;
        }
        if(editorEvent.type.Equals(EventType.Repaint)) {
            // set cutoff
            matIris.SetFloat("_Cutoff", 0f);
            float scale = 1f;
            float a = (startingAngle + 90) + 360 * (.5f - value / 2f)/*(clockwise ? 1f : -1f)*/;
            float aspect = rect.width / rect.height;
            Vector3 _scale = new Vector3((1f / scale) * aspect, (1f / scale), 1f);
            Vector2 pivot = new Vector2(.5f, .5f);
            Vector2 pos = new Vector2(focalX, 1f - focalY);
            Vector2 _position = new Vector3(-pos.x * _scale.x + .5f, -pos.y * _scale.y + .5f);
            Matrix4x4 t = Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
            Quaternion _rotation = Quaternion.Euler(0, 0, a - 90);
            Matrix4x4 r = Matrix4x4.TRS(Vector3.zero, _rotation, Vector3.one);
            Matrix4x4 tInv = Matrix4x4.TRS(pivot, Quaternion.identity, Vector3.one);
            Matrix4x4 f = Matrix4x4.TRS(_position, Quaternion.identity, _scale);
            matIris.SetMatrix("_Matrix", tInv * r * t * f);
            if(matIris.color != GUI.color) matIris.color = GUI.color;
            Rect rectSourceHalf1 = new Rect(0f, 0f, 1f, 1f);
            Rect rectPositionHalf1 = new Rect(rect);
            Rect rectSourceHalf2 = new Rect(0f, 0f, 1f, 1f);
            Rect rectPositionHalf2 = new Rect(rect);
            if(startingAngle == 0f) {
                rectSourceHalf1.width = focalX;
                rectPositionHalf1.width = rect.width * focalX;
                rectSourceHalf2.x = focalX;
                rectSourceHalf2.width = 1f - focalX;
                rectPositionHalf2.x = rect.x + rect.width * focalX;
                rectPositionHalf2.width = rect.width - rectPositionHalf1.width;
            }
            else if(startingAngle == 90f) {
                rectSourceHalf1.height = focalY;
                rectSourceHalf1.y = 1f - focalY;
                rectPositionHalf1.height = rect.height * focalY;
                rectSourceHalf2.height = 1f - rectSourceHalf1.height;
                rectPositionHalf2.y = rect.y + rect.height * focalY;
                rectPositionHalf2.height = rect.height - rectPositionHalf1.height;
            }
            else if(startingAngle == 180f) {
                rectSourceHalf1.x = focalX;
                rectSourceHalf1.width = 1f - focalX;
                rectPositionHalf1.x = rect.x + rect.width * focalX;
                rectPositionHalf1.width = rect.width * (1f - focalX);
                rectSourceHalf2.width = focalX;
                rectPositionHalf2.width = rect.width * focalX;
            }
            else if(startingAngle == 270f) {
                rectSourceHalf1.height = 1f - focalY;
                rectPositionHalf1.y = rect.y + rect.height * focalY;
                rectPositionHalf1.height = rect.height * (1f - focalY);
                rectSourceHalf2.height = focalY;
                rectSourceHalf2.y = 1f - focalY;
                rectPositionHalf2.height = rect.height * focalY;
            }

            Graphics.DrawTexture(rectPositionHalf2, tex, rectSourceHalf2, 0, 0, 0, 0, matIris);

            startingAngle = (startingAngle + 180) % 360;
            a = (startingAngle + 90) + 360 * (.5f - value / 2f) * -1f;
            _rotation = Quaternion.Euler(0, 0, a - 90);
            r = Matrix4x4.TRS(Vector3.zero, _rotation, Vector3.one);
            matIris.SetMatrix("_Matrix", tInv * r * t * f);

            Graphics.DrawTexture(rectPositionHalf1, tex, rectSourceHalf1, 0, 0, 0, 0, matIris);
        }
    }
    #endregion

    #region Other Functions

    public static AMCameraFade getCameraFade(bool preview = false) {
        if(_cf) {
            _cf.preview = preview;
            return _cf;
        }
        AMCameraFade cf = null;
        GameObject go = GameObject.Find("AMCamera" + (preview ? "FadePreview" : "Fade"));
        if(go) {
            cf = (AMCameraFade)go.GetComponent(typeof(AMCameraFade));
        }
        if(!cf) {
            cf = (AMCameraFade)new GameObject("AMCamera" + (preview ? "FadePreview" : "Fade"), typeof(AMCameraFade)).GetComponent(typeof(AMCameraFade));
        }
        _cf = cf;
        _cf.preview = preview;
        return _cf;
    }


    public void setupRenderTexture(Camera camera) {
        if(renderTextureCamera != camera) {
            if(renderTextureCamera && renderTextureCamera.targetTexture == tex) renderTextureCamera.targetTexture = null;
            camera.targetTexture = tex;
            renderTextureCamera = camera;
            camera.Render();
        }
        useRenderTexture = true;
        colorTex = Color.white;

        hasColorTex = false;
        placeholder = false;
    }

    public bool isRenderTextureSetupFor(Camera camera) {
        if(tex && camera.targetTexture == tex) return true;
        return false;
    }
    public static void reset() {
        if(_cf != null) {
            _cf.isReset = true;
            _cf.value = 0f;
            if(_cf.renderTextureCamera) {
                if(_cf.renderTextureCamera.targetTexture == _cf.tex) _cf.renderTextureCamera.targetTexture = null;
                _cf.renderTextureCamera = null;
            }
        }
    }

    public static void clearRenderTexture() {
        if(_cf != null) {
            _cf.clearTexture();
        }
    }

    public static void destroyAndReload(bool isPreview) {
        if(_cf != null) _cf.destroyImmediate();
        _cf = null;
        getCameraFade(isPreview);
    }

    public static void destroyImmediateInstance() {
        if(_cf != null) _cf.destroyImmediate();
    }

    public static bool hasInstance() {
        if(_cf != null) return true;
        else return false;
    }

    public static bool isPreview() {
        if(_cf != null) return _cf.preview;
        else return false;
    }

    public static void doShouldUpdateStill() {
        if(_cf) {
            _cf.cachedStillFrame = 0;
            _cf.shouldUpdateStill = true;
        }
    }

    public static Vector2 getPositionAtAngle(int a, float padding) {
        a = (a % 360 + 360) % 360;
        Vector2 ps = new Vector2(0f, 0f);
        float xmin = 0f - padding;
        float xmax = 1f + padding;
        float ymin = 0f - padding;
        float ymax = 1 + padding;
        float width = xmax - xmin;
        float height = ymax - ymin;
        if(a >= 0 && a <= 45) {
            ps.x = (a / 45f) * (width / 2f) + width / 2f;
            ps.y = ymax;
        }
        else if(a <= 90) {
            ps.x = xmax;
            ps.y = (1f - (a - 45f) / 45f) * (height / 2f) + height / 2f;
        }
        else if(a <= 135) {
            ps.x = xmax;
            ps.y = (1f - (a - 90f) / 45f) * (height / 2f);
        }
        else if(a <= 180) {
            ps.x = (1f - (a - 135f) / 45f) * (width / 2f) + width / 2f;
            ps.y = ymin;
        }
        else if(a <= 225) {
            ps.x = (1f - (a - 180f) / 45f) * (width / 2f);
            ps.y = ymin;
        }
        else if(a <= 270) {
            ps.x = xmin;
            ps.y = ((a - 225f) / 45f) * (height / 2f);
        }
        else if(a <= 315) {
            ps.x = xmin;
            ps.y = ((a - 270f) / 45f) * (height / 2f) + height / 2f;
        }
        else if(a <= 360) {
            ps.x = ((a - 315f) / 45f) * (width / 2f);
            ps.y = ymax;
        }
        return ps;
    }

    public void repaint() {
        this.OnGUI();
    }

    public void incrementKeepAlives() {
        keepAlives++;
        keepAliveAwake = false;
    }
    public void clearTexture() {
        if(renderTextureCamera) {
            renderTextureCamera.targetTexture = null;
            renderTextureCamera = null;
        }
        //if(tex) {
        //Destroy(tex);
        //tex = null;
        //}
    }

    public void clearTexture2D() {
        if(tex2d) {
            Destroy(tex2d);
            tex2d = null;
        }
    }

    private RenderTexture getRenderTexture() {
        if(!shouldUpdateRenderTexture && _tex) return _tex;
        shouldUpdateRenderTexture = false;
        if(Application.isPlaying) {
            if(_tex) Destroy(_tex);
            width = Screen.width;
            height = Screen.height;
        }
        else {
            if(_tex) DestroyImmediate(_tex);
            if(width <= 0 || height <= 0) {
                try {
                    Rect rectGameView;
                    rectGameView = GetMainGameViewPosition();
                    width = (int)rectGameView.width;
                    height = (int)rectGameView.height;
                }
                catch {
                    width = 200;
                    height = 100;
                    shouldUpdateRenderTexture = true;
                }
            }
        }
        _tex = new RenderTexture(width, height, 24);
        return _tex;
    }
    public static object GetMainGameView() {
        System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        System.Reflection.MethodInfo GetMainGameView = T.GetMethod("GetMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        System.Object Res = GetMainGameView.Invoke(null, null);
        return Res;
    }
    public static Rect GetMainGameViewPosition() {
        object gv = GetMainGameView();
        return (Rect)gv.GetType().GetProperty("position").GetValue(gv, null);
    }
    #endregion
}
