using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

[AddComponentMenu("")]
public class AMGOSetActiveKey : AMKey {
    public bool setActive;

    public override void destroy() {
        base.destroy();
    }
    // copy properties from key
    public override AMKey CreateClone() {

        AMGOSetActiveKey a = gameObject.AddComponent<AMGOSetActiveKey>();
        a.enabled = false;
        a.frame = frame;
        a.setActive = setActive;
        return a;
    }
}
