// Author: Daniele Giardini
//
// Copyright (c) 2012 Daniele Giardini - Holoville - http://www.holoville.com
// Contains code from Andeeee's CRSpline (http://forum.unity3d.com/threads/32954-Waypoints-and-constant-variable-speed-problems?p=213942&viewfull=1#post213942)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using UnityEngine;
using System;
using System.Collections.Generic;

using DG.Tweening;

namespace MateAnimator{
	public class AMPathPreview {
	    // VARS ///////////////////////////////////////////////////

	    public float pathLength; // Stored when storing time and length tables.
	    public float[] waypointsLength; // Length of each waypoint, excluding control points

	    public float[] timesTable; // Connected to lengthsTable, used for constant speed calculations
	    float[] lengthsTable; // Connected to timesTable, used for constant speed calculations

	    internal Vector3[] path;
	    internal bool changed; // Used by incremental loops to tell that drawPs should be recalculated.

	    Vector3[] drawPs; // Used by GizmoDraw to store point only once.
	    PathType pathType;


	    // ***********************************************************************************
	    // CONSTRUCTOR
	    // ***********************************************************************************

	    /// <summary>
	    /// Creates a new <see cref="Path"/> based on the given array of <see cref="Vector3"/> points.
	    /// </summary>
	    /// <param name="p_type">Type of path</param>
	    /// <param name="p_path">
	    /// The <see cref="Vector3"/> array used to create the path.
	    /// </param>
	    public AMPathPreview(PathType p_type, params Vector3[] p_path) {
	        pathType = p_type;
	        path = new Vector3[p_path.Length];
	        Array.Copy(p_path, path, path.Length);
	    }

	    // ===================================================================================
	    // METHODS ---------------------------------------------------------------------------

	    /// <summary>
	    /// Gets the point on the path at the given percentage (0 to 1).
	    /// </summary>
	    /// <param name="t">
	    /// The percentage (0 to 1) at which to get the point.
	    /// </param>
	    public Vector3 GetPoint(float t) {
	        int tmp;
	        return GetPoint(t, out tmp);
	    }

	    /// <summary>
	    /// Gets the point on the path at the given percentage (0 to 1).
	    /// </summary>
	    /// <param name="t">
	    /// The percentage (0 to 1) at which to get the point.
	    /// </param>
	    /// <param name="out_waypointIndex">
	    /// Index of waypoint we're moving to (or where we are). Only used for Linear paths.
	    /// </param>
	    internal Vector3 GetPoint(float t, out int out_waypointIndex) {
	        switch(pathType) {
	            case PathType.Linear:
	                if(t <= 0) {
	                    out_waypointIndex = 1;
	                    return path[1];
	                }
	                else {
	                    int startPIndex = 0;
	                    int endPIndex = 0;
	                    int len = timesTable.Length;
	                    for(int i = 1; i < len; i++) {
	                        if(timesTable[i] >= t) {
	                            startPIndex = i - 1;
	                            endPIndex = i;
	                            break;
	                        }
	                    }
	                    float startPPerc = timesTable[startPIndex];
	                    float partialPerc = timesTable[endPIndex] - timesTable[startPIndex];
	                    partialPerc = t - startPPerc;
	                    float partialLen = pathLength * partialPerc;
	                    Vector3 wp0 = path[startPIndex];
	                    Vector3 wp1 = path[endPIndex];
	                    out_waypointIndex = endPIndex;
	                    return wp0 + Vector3.ClampMagnitude(wp1 - wp0, partialLen);
	                }
	            default: // Curved
	                int numSections = path.Length - 3;
	                int tSec = (int)Math.Floor(t * numSections);
	                int currPt = numSections - 1;
	                if(currPt > tSec) {
	                    currPt = tSec;
	                }
	                float u = t * numSections - currPt;

	                Vector3 a = path[currPt];
	                Vector3 b = path[currPt + 1];
	                Vector3 c = path[currPt + 2];
	                Vector3 d = path[currPt + 3];

	                out_waypointIndex = -1;
	                //                out_waypointIndex = 0 + currPt; // -- ak mod so that we always know the index point // BREAKS constant curved path
	                return .5f * (
	                    (-a + 3f * b - 3f * c + d) * (u * u * u)
	                    + (2f * a - 5f * b + 4f * c - d) * (u * u)
	                    + (-a + c) * u
	                    + 2f * b
	                );
	        }
	    }

	    /// <summary>
	    /// Gets the velocity at the given time position.
	    /// OBSOLETE since path now uses constant velocity.
	    /// </summary>
	    public Vector3 Velocity(float t) {
	        int numSections = path.Length - 3;
	        int tSec = (int)Math.Floor(t * numSections);
	        int currPt = numSections - 1;
	        if(currPt > tSec) {
	            currPt = tSec;
	        }
	        float u = t * numSections - currPt;

	        Vector3 a = path[currPt];
	        Vector3 b = path[currPt + 1];
	        Vector3 c = path[currPt + 2];
	        Vector3 d = path[currPt + 3];

	        return 1.5f * (-a + 3f * b - 3f * c + d) * (u * u)
	                   + (2f * a - 5f * b + 4f * c - d) * u
	                   + .5f * c - .5f * a;
	    }

	    /// <summary>
	    /// Draws the full path.
	    /// </summary>
	    public void GizmoDraw(Transform transform, float ptSize) {
	        GizmoDraw(transform, -1, ptSize, false);
	    }

	    /// <summary>
	    /// Draws the full path, and if <c>t</c> is not -1 also draws the velocity at <c>t</c>.
	    /// </summary>
	    /// <param name="t">
	    /// The point where to calculate velocity and eventual additional trigonometry.
	    /// </param>
	    /// <param name="p_drawTrig">
	    /// If <c>true</c> also draws the normal, tangent, and binormal of t.
	    /// </param>
	    public void GizmoDraw(Transform transform, float t, float ptSize, bool p_drawTrig) {
	        Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);

	        Matrix4x4 mtx = transform ? transform.localToWorldMatrix : Matrix4x4.identity;

	        Vector3 currPt;
	        if(changed || pathType == PathType.CatmullRom && drawPs == null) {
	            changed = false;
                if(pathType == PathType.CatmullRom) {
	                // Store draw points.
	                int subdivisions = path.Length * 10;
	                drawPs = new Vector3[subdivisions + 1];
	                for(int i = 0; i <= subdivisions; ++i) {
	                    float pm = i / (float)subdivisions;
	                    currPt = GetPoint(pm);
	                    drawPs[i] = currPt;
	                }
	            }
	        }
	        // Draw path.
	        Vector3 prevPt;
	        switch(pathType) {
	            case PathType.Linear:
	                prevPt = path[1];
	                int len = path.Length;
	                for(int i = 1; i < len - 1; ++i) {
	                    currPt = path[i];
	                    Gizmos.DrawLine(mtx.MultiplyPoint3x4(currPt), mtx.MultiplyPoint3x4(prevPt));
	                    prevPt = currPt;
	                }
	                break;
	            default: // Curved
	                prevPt = drawPs[0];
	                int drawPsLength = drawPs.Length;
	                for(int i = 1; i < drawPsLength; ++i) {
	                    currPt = drawPs[i];
	                    Gizmos.DrawLine(mtx.MultiplyPoint3x4(currPt), mtx.MultiplyPoint3x4(prevPt));
	                    prevPt = currPt;
	                }
	                break;
	        }
	        // Draw path control points.
	        Gizmos.color = Color.green;
	        int pathLength = path.Length - 1;
	        for(int i = 1; i < pathLength; ++i) {
	            Gizmos.DrawSphere(mtx.MultiplyPoint3x4(path[i]), ptSize);
	        }

	        if(p_drawTrig && t != -1) {
	            Vector3 pos = GetPoint(t);
	            Vector3 prevP;
	            Vector3 p = pos;
	            Vector3 nextP;
	            float nextT = t + 0.0001f;
	            if(nextT > 1) {
	                nextP = pos;
	                p = GetPoint(t - 0.0001f);
	                prevP = GetPoint(t - 0.0002f);
	            }
	            else {
	                float prevT = t - 0.0001f;
	                if(prevT < 0) {
	                    prevP = pos;
	                    p = GetPoint(t + 0.0001f);
	                    nextP = GetPoint(t + 0.0002f);
	                }
	                else {
	                    prevP = GetPoint(prevT);
	                    nextP = GetPoint(nextT);
	                }
	            }
	            Vector3 tangent = nextP - p;
	            tangent.Normalize();
	            Vector3 tangent2 = p - prevP;
	            tangent2.Normalize();
	            Vector3 normal = Vector3.Cross(tangent, tangent2);
	            normal.Normalize();
	            Vector3 binormal = Vector3.Cross(tangent, normal);
	            binormal.Normalize();

	            // Draw normal.
	            pos = mtx.MultiplyPoint3x4(pos);

	            Gizmos.color = Color.black;
	            Gizmos.DrawLine(pos, pos + tangent);
	            Gizmos.color = Color.blue;
	            Gizmos.DrawLine(pos, pos + normal);
	            Gizmos.color = Color.red;
	            Gizmos.DrawLine(pos, pos + binormal);
	        }
	    }

	    // ===================================================================================
	    // INTERNAL METHODS ------------------------------------------------------------------

	    /// <summary>
	    /// Returns the point at the given time percentage (0 to 1),
	    /// considering the path at constant speed.
	    /// </summary>
	    /// <param name="t">The time percentage (0 to 1) at which to get the point </param>
	    internal Vector3 GetConstPoint(float t) {
	        switch(pathType) {
	            case PathType.Linear:
	                return GetPoint(t);
	            default: // Curved
	                // Convert time percentage to constant path percentage
	                float pathPerc = GetConstPathPercFromTimePerc(t);
	                return GetPoint(pathPerc);
	        }
	    }
	    /// <summary>
	    /// Returns the point at the given time percentage (0 to 1),
	    /// considering the path at constant speed.
	    /// </summary>
	    /// <param name="t">The time percentage (0 to 1) at which to get the point </param>
	    /// <param name="out_pathPerc">Outputs the calculated path percentage value</param>
	    /// <param name="out_waypointIndex">
	    /// Index of waypoint we're moving to (or where we are). Only used for Linear paths.
	    /// </param>
	    internal Vector3 GetConstPoint(float t, out float out_pathPerc, out int out_waypointIndex) {
	        switch(pathType) {
	            case PathType.Linear:
	                out_pathPerc = t;
	                return GetPoint(t, out out_waypointIndex);
	            default: // Curved
	                // Convert time percentage to constant path percentage
	                float pathPerc = GetConstPathPercFromTimePerc(t);
	                // Update pathPerc.
	                out_pathPerc = pathPerc;
	                out_waypointIndex = -1;
	                return GetPoint(pathPerc);
	            //                return GetPoint(t, out out_waypointIndex); // -- ak mod so that we always know the index point // BREAKS constant curved path
	        }
	    }

	    // If path is linear, p_subdivisions is ignored,
	    // and waypointsLength are stored here instead than when calling StoreWaypointsLengths
	    internal void StoreTimeToLenTables(int p_subdivisions) {
	        Vector3 prevP;
	        Vector3 currP;
	        float incr;
	        switch(pathType) {
	            case PathType.Linear:
	                pathLength = 0;
	                int pathCount = path.Length;
	                waypointsLength = new float[pathCount];
	                prevP = path[1];
	                for(int i = 1; i < pathCount; i++) {
	                    currP = path[i];
	                    float dist = Vector3.Distance(currP, prevP);
	                    if(i < pathCount - 1) pathLength += dist;
	                    prevP = currP;
	                    waypointsLength[i] = dist;
	                }
	                timesTable = new float[pathCount];
	                float tmpLen = 0;
	                for(int i = 2; i < pathCount; i++) {
	                    tmpLen += waypointsLength[i];
	                    timesTable[i] = tmpLen / pathLength;
	                }
	                break;
	            default: // Curved
	                pathLength = 0;
	                incr = 1f / p_subdivisions;
	                timesTable = new float[p_subdivisions];
	                lengthsTable = new float[p_subdivisions];
	                prevP = GetPoint(0);
	                for(int i = 1; i < p_subdivisions + 1; ++i) {
	                    float perc = incr * i;
	                    currP = GetPoint(perc);
	                    pathLength += Vector3.Distance(currP, prevP);
	                    prevP = currP;
	                    timesTable[i - 1] = perc;
	                    lengthsTable[i - 1] = pathLength;
	                }
	                break;
	        }
	    }

	    // If path is lineas, waypointsLengths were stored when calling StoreTimeToLenTables
	    internal void StoreWaypointsLengths(int p_subdivisions) {
	        // Create a relative path between each waypoint,
	        // with its start and end control lines coinciding with the next/prev waypoints.
	        int len = path.Length - 2;
	        waypointsLength = new float[len];
	        waypointsLength[0] = 0;
	        AMPathPreview partialPath = null;
	        for(int i = 2; i < len + 1; ++i) {
	            // Create partial path
	            Vector3[] pts = new Vector3[4];
	            pts[0] = path[i - 2];
	            pts[1] = path[i - 1];
	            pts[2] = path[i];
	            pts[3] = path[i + 1];
	            if(i == 2) {
	                partialPath = new AMPathPreview(pathType, pts);
	            }
	            else {
	                partialPath.path = pts;
	            }
	            // Calculate length of partial path
	            float partialLen = 0;
	            float incr = 1f / p_subdivisions;
	            Vector3 prevP = partialPath.GetPoint(0);
	            for(int c = 1; c < p_subdivisions + 1; ++c) {
	                float perc = incr * c;
	                Vector3 currP = partialPath.GetPoint(perc);
	                partialLen += Vector3.Distance(currP, prevP);
	                prevP = currP;
	            }
	            waypointsLength[i - 1] = partialLen;
	        }
	    }

	    // ===================================================================================
	    // PRIVATE METHODS -------------------------------------------------------------------

	    /// <summary>
	    /// Gets the constant path percentage for the given time percentage
	    /// that can be used with GetConstPoint.
	    /// </summary>
	    /// <param name="t">The time percentage (0 to 1) to use</param>
	    /// <returns></returns>
	    float GetConstPathPercFromTimePerc(float t) {
	        // Apply constant speed
	        if(t > 0 && t < 1) {
	            float tLen = pathLength * t;
	            // Find point in time/length table.
	            float t0 = 0, l0 = 0, t1 = 0, l1 = 0;
	            int lengthsTableLength = lengthsTable.Length;
	            for(int i = 0; i < lengthsTableLength; ++i) {
	                if(lengthsTable[i] > tLen) {
	                    t1 = timesTable[i];
	                    l1 = lengthsTable[i];
	                    if(i > 0) l0 = lengthsTable[i - 1];
	                    break;
	                }
	                t0 = timesTable[i];
	            }
	            // Find correct time.
	            float dl = l1 - l0;
	            t = dl != 0.0f ? t0 + ((tLen - l0) / dl) * (t1 - t0) : t0;
	        }

	        // Clamp value because path has limited range of 0-1.
	        if(t > 1) t = 1; else if(t < 0) t = 0;

	        return t;
	    }
	}
}