using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.Animator {
    public interface ITarget {
        float animScale { get; }

        Transform root { get; }

        List<Take> takes { get; }

        bool isMeta { get; }

        Transform GetCache(string path);
        void SetCache(string path, Transform obj);
        void SequenceComplete(SequenceControl seq);
        void SequenceTrigger(SequenceControl seq, Key key, TriggerData trigDat);

        string[] GetMissingTargets();
        void MaintainTargetCache(Track track);
        void MaintainTakes();
        void GenerateMissingTargets(string[] missingPaths);
    }
}