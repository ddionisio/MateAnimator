using UnityEngine;
using System.Collections;

public struct AMGizmo {
    public static Color defaultColor = Color.white;

    /// <summary>
    /// When called from an OnDrawGizmos() function it will draw a curved path through the provided array of Vector3s.
    /// </summary>
    /// <param name="path">
    /// A <see cref="Vector3s[]"/>
    /// </param>
    public static void DrawPath(Vector3[] path) {
        if(path.Length > 0) {
            DrawPathHelper(null, path, defaultColor, "gizmos");
        }
    }

    /// <summary>
    /// When called from an OnDrawGizmos() function it will draw a curved path through the provided array of Vector3s.
    /// </summary>
    /// <param name="path">
    /// A <see cref="Vector3s[]"/>
    /// </param>
    /// <param name="color">
    /// A <see cref="Color"/>
    /// </param> 
    public static void DrawPath(Vector3[] path, Color color) {
        if(path.Length > 0) {
            DrawPathHelper(null, path, color, "gizmos");
        }
    }

    public static void DrawPathRelative(Transform trans, Vector3[] path, Color color) {
        if(path.Length > 0) {
            DrawPathHelper(trans, path, color, "gizmos");
        }
    }

    /// <summary>
    /// When called from an OnDrawGizmos() function it will draw a curved path through the provided array of Transforms.
    /// </summary>
    /// <param name="path">
    /// A <see cref="Transform[]"/>
    /// </param>
    public static void DrawPath(Transform[] path) {
        if(path.Length > 0) {
            //create and store path points:
            Vector3[] suppliedPath = new Vector3[path.Length];
            for(int i = 0; i < path.Length; i++) {
                suppliedPath[i] = path[i].position;
            }

            DrawPathHelper(null, suppliedPath, defaultColor, "gizmos");
        }
    }

    /// <summary>
    /// When called from an OnDrawGizmos() function it will draw a curved path through the provided array of Transforms.
    /// </summary>
    /// <param name="path">
    /// A <see cref="Transform[]"/>
    /// </param>
    /// <param name="color">
    /// A <see cref="Color"/>
    /// </param> 
    public static void DrawPath(Transform[] path, Color color) {
        if(path.Length > 0) {
            //create and store path points:
            Vector3[] suppliedPath = new Vector3[path.Length];
            for(int i = 0; i < path.Length; i++) {
                suppliedPath[i] = path[i].position;
            }

            DrawPathHelper(null, suppliedPath, color, "gizmos");
        }
    }

    /// <summary>
    /// Draws a curved path through the provided array of Vector3s with Gizmos.DrawLine().
    /// </summary>
    /// <param name="path">
    /// A <see cref="Vector3s[]"/>
    /// </param>
    public static void DrawPathGizmos(Vector3[] path) {
        if(path.Length > 0) {
            DrawPathHelper(null, path, defaultColor, "gizmos");
        }
    }

    /// <summary>
    /// Draws a curved path through the provided array of Vector3s with Gizmos.DrawLine().
    /// </summary>
    /// <param name="path">
    /// A <see cref="Vector3s[]"/>
    /// </param>
    /// <param name="color">
    /// A <see cref="Color"/>
    /// </param> 
    public static void DrawPathGizmos(Vector3[] path, Color color) {
        if(path.Length > 0) {
            DrawPathHelper(null, path, color, "gizmos");
        }
    }

    /// <summary>
    /// Draws a curved path through the provided array of Transforms with Gizmos.DrawLine().
    /// </summary>
    /// <param name="path">
    /// A <see cref="Transform[]"/>
    /// </param>
    public static void DrawPathGizmos(Transform[] path) {
        if(path.Length > 0) {
            //create and store path points:
            Vector3[] suppliedPath = new Vector3[path.Length];
            for(int i = 0; i < path.Length; i++) {
                suppliedPath[i] = path[i].position;
            }

            DrawPathHelper(null, suppliedPath, defaultColor, "gizmos");
        }
    }

    /// <summary>
    /// Draws a curved path through the provided array of Transforms with Gizmos.DrawLine().
    /// </summary>
    /// <param name="path">
    /// A <see cref="Transform[]"/>
    /// </param>
    /// <param name="color">
    /// A <see cref="Color"/>
    /// </param> 
    public static void DrawPathGizmos(Transform[] path, Color color) {
        if(path.Length > 0) {
            //create and store path points:
            Vector3[] suppliedPath = new Vector3[path.Length];
            for(int i = 0; i < path.Length; i++) {
                suppliedPath[i] = path[i].position;
            }

            DrawPathHelper(null, suppliedPath, color, "gizmos");
        }
    }

    //trans = the transform where path is relative to, null if path is already in world position
    private static void DrawPathHelper(Transform trans, Vector3[] path, Color color, string method) {
        //Line Draw:
		Vector3 prevPt = trans != null ? trans.localToWorldMatrix.MultiplyPoint(AMUtil.Interp(path, 0)) : AMUtil.Interp(path, 0);
        Gizmos.color = color;
        int SmoothAmount = path.Length * 20;
        for(int i = 1; i <= SmoothAmount; i++) {
            float pm = (float)i / SmoothAmount;
			Vector3 currPt = AMUtil.Interp(path, pm);
            if(trans != null)
                currPt = trans.localToWorldMatrix.MultiplyPoint(currPt);
            if(method == "gizmos") {
                Gizmos.DrawLine(currPt, prevPt);
            }
            else if(method == "handles") {
                Debug.LogError("AMTween Error: Drawing a path with Handles is temporarily disabled because of compatability issues with Unity 2.6!");
                //UnityEditor.Handles.DrawLine(currPt, prevPt);
            }
            prevPt = currPt;
        }
    }
}
