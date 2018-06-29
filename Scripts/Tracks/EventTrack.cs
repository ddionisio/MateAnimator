using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace M8.Animator {
    [System.Serializable]
    public class EventTrack : Track {
        public override SerializeType serializeType { get { return SerializeType.Event; } }

        [SerializeField]
        Object obj;
        [SerializeField]
        string componentName;

        public override bool canTween { get { return false; } }

        protected override void SetSerializeObject(UnityEngine.Object obj) {
            this.obj = obj;

            if(this.obj is Component)
                componentName = this.obj.GetType().Name;
            else
                componentName = "";
        }

        protected override UnityEngine.Object GetSerializeObject(GameObject targetGO) {
            if(targetGO) {
                if(!string.IsNullOrEmpty(componentName)) { //grab directly from targetGO
                    return targetGO.GetComponent(componentName);
                }
                else
                    return targetGO;
            }

            return obj;
        }

        /// <summary>
        /// Set target directly as the GameObject item, rather than an object within the item
        /// </summary>
        public void SetTargetAsGameObject(ITarget target, GameObject item) {
            if(target.meta && item) {
                _targetPath = Utility.GetPath(target.root, item);
                target.SetCache(_targetPath, item.transform);

                obj = null;
            }
            else {
                _targetPath = "";
                obj = item;
            }

            componentName = "";
        }

        /// <summary>
        /// This is to provide proper path to where the component is attached via the given item.  The "comp" is applied as the target object.
        /// </summary>
        public void SetTargetAsComponent(ITarget target, Transform item, Component comp) {
            if(target.meta) {
                if(item) {
                    _targetPath = Utility.GetPath(target.root, item);
                    target.SetCache(_targetPath, item);
                }
                else
                    _targetPath = "";

                obj = null;
            }
            else {
                _targetPath = "";

                obj = comp;
            }

            componentName = comp.GetType().Name;
        }

        /// <summary>
        /// Editor purpose for setting target data directly.
        /// </summary>
        public void SetTargetAsComponentDirect(string targetPath, Component comp, string compTypeName) {
            _targetPath = targetPath;
            obj = comp;
            componentName = compTypeName;
        }

        /// <summary>
        /// This directly sets the target with no path, used for anything that's not a GameObject e.g. ScriptableObject
        /// </summary>
        public void SetTargetAsObject(Object obj) {
            _targetPath = "";
            SetSerializeObject(obj);
        }

        public override string getTrackType() {
            return "Event";
        }

        /*public override System.Type GetRequiredComponent() {
            if(string.IsNullOrEmpty(componentName)) return null;

            System.Type type = System.Type.GetType(componentName);
            if(type == null) {
                int endInd = componentName.IndexOf('.');
                if(endInd != -1) {
                    // Get the name of the assembly (Assumption is that we are using
                    // fully-qualified type names)
                    var assemblyName = componentName.Substring(0, endInd);

                    // Attempt to load the indicated Assembly
                    var assembly = System.Reflection.Assembly.Load(assemblyName);
                    if(assembly != null)
                        // Ask that assembly to return the proper Type
                        type = assembly.GetType(componentName);
                }
            }

            return type;
        }*/

        // add a new key
        public void addKey(ITarget itarget, int _frame) {
            foreach(EventKey key in keys) {
                // if key exists on frame, do nothing
                if(key.frame == _frame) {
                    return;
                }
            }
            EventKey a = new EventKey();
            a.frame = _frame;
            // add a new key
            keys.Add(a);
            // update cache
            updateCache(itarget);
        }
        public bool hasSameEventsAs(ITarget target, EventTrack _track) {
            if(_track != null && _track.GetTarget(target) == GetTarget(target))
                return true;
            return false;
        }

        public override AnimateTimeline.JSONInit getJSONInit(ITarget target) {
            // no initial values to set
            return null;
        }

        public override List<GameObject> getDependencies(ITarget target) {
            GameObject go = GetTarget(target) as GameObject;
            List<GameObject> ls = new List<GameObject>();
            if(go) ls.Add(go);
            foreach(EventKey key in keys) {
                ls = ls.Union(key.getDependencies()).ToList();
            }
            return ls;
        }

        public override List<GameObject> updateDependencies(ITarget target, List<GameObject> newReferences, List<GameObject> oldReferences) {
            GameObject go = GetTarget(target) as GameObject;
            bool didUpdateObj = false;
            bool didUpdateParameter = false;
            if(go) {
                for(int i = 0; i < oldReferences.Count; i++) {
                    if(oldReferences[i] == go) {
                        // check if new GameObject has the required component
                        if(!string.IsNullOrEmpty(componentName)) {
                            var comp = newReferences[i].GetComponent(componentName);
                            if(comp == null) {
                                // missing component
                                Debug.LogWarning("Animator: Event Track component '" + componentName + "' not found on new reference for GameObject '" + obj.name + "'. Duplicate not replaced.");
                                List<GameObject> lsFlagToKeep = new List<GameObject>();
                                lsFlagToKeep.Add(oldReferences[i]);
                                return lsFlagToKeep;
                            }

                            SetTargetAsComponent(target, newReferences[i].transform, comp);
                        }
                        else {
                            SetTarget(target, newReferences[i].transform);
                        }

                        didUpdateObj = true;
                        break;
                    }

                }
            }
            foreach(EventKey key in keys) {
                if(key.updateDependencies(newReferences, oldReferences, didUpdateObj, go) && !didUpdateParameter) didUpdateParameter = true;
            }

            if(didUpdateObj || didUpdateParameter) updateCache(target);

            return new List<GameObject>();
        }

        protected override void DoCopy(Track track) {
            EventTrack nTrack = (EventTrack)track;

            nTrack.obj = obj;
            nTrack.componentName = componentName;
        }
    }
}