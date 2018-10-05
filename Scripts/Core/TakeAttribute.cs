using UnityEngine;

namespace M8.Animator {
    /// <summary>
    /// Drop down selection of take from given animatorField
    /// </summary>
    public class TakeSelectorAttribute : PropertyAttribute {        
        public string animatorField = "animator";
    }
}
