using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.Animator {
    [System.Serializable]
    public class SerializeData {
        [System.Serializable]
        public struct SerializeIndex {
            public SerializeType type;
            public int index;
        }

        [System.Serializable]
        public struct TrackData {
            public SerializeIndex index;
            public SerializeIndex[] keys;
        }

        //add a list for track and key for each type here

        [SerializeField] AudioTrack[] _audioTracks;
        [SerializeField] AudioKey[] _audioKeys;

        [SerializeField] CameraSwitcherTrack[] _cameraSwitcherTracks;
        [SerializeField] CameraSwitcherKey[] _cameraSwitcherKeys;

        [SerializeField] EventTrack[] _eventTracks;
        [SerializeField] EventKey[] _eventKeys;

        [SerializeField] GOSetActiveTrack[] _goSetActiveTracks;
        [SerializeField] GOSetActiveKey[] _goSetActiveKeys;

        [SerializeField] MaterialTrack[] _materialTracks;
        [SerializeField] MaterialKey[] _materialKeys;

        [SerializeField] OrientationTrack[] _orientationTracks;
        [SerializeField] OrientationKey[] _orientationKeys;

        [SerializeField] PropertyTrack[] _propertyTracks;
        [SerializeField] PropertyKey[] _propertyKeys;

        [SerializeField] RotationEulerTrack[] _rotationEulerTracks;
        [SerializeField] RotationEulerKey[] _rotationEulerKeys;

        [SerializeField] RotationTrack[] _rotationTracks;
        [SerializeField] RotationKey[] _rotationKeys;

        [SerializeField] TranslationTrack[] _translationTracks;
        [SerializeField] TranslationKey[] _translationKeys;

        [SerializeField] TriggerTrack[] _triggerTracks;
        [SerializeField] TriggerKey[] _triggerKeys;

        [SerializeField] UnityAnimationTrack[] _unityAnimationTracks;
        [SerializeField] UnityAnimationKey[] _unityAnimationKeys;

        [SerializeField] TrackData[][] _takesTrackLookups;

        public void Serialize(List<Take> takes) {
            var audioTrackList = new List<AudioTrack>();
            var audioKeyList = new List<AudioKey>();

            var cameraSwitcherTrackList = new List<CameraSwitcherTrack>();
            var cameraSwitcherKeyList = new List<CameraSwitcherKey>();

            var eventTrackList = new List<EventTrack>();
            var eventKeyList = new List<EventKey>();

            var goSetActiveTrackList = new List<GOSetActiveTrack>();
            var goSetActiveKeyList = new List<GOSetActiveKey>();

            var materialTrackList = new List<MaterialTrack>();
            var materialKeyList = new List<MaterialKey>();

            var orientationTrackList = new List<OrientationTrack>();
            var orientationKeyList = new List<OrientationKey>();

            var propertyTrackList = new List<PropertyTrack>();
            var propertyKeyList = new List<PropertyKey>();

            var rotationEulerTrackList = new List<RotationEulerTrack>();
            var rotationEulerKeyList = new List<RotationEulerKey>();

            var rotationTrackList = new List<RotationTrack>();
            var rotationKeyList = new List<RotationKey>();

            var translationTrackList = new List<TranslationTrack>();
            var translationKeyList = new List<TranslationKey>();

            var triggerTrackList = new List<TriggerTrack>();
            var triggerKeyList = new List<TriggerKey>();

            var unityAnimationTrackList = new List<UnityAnimationTrack>();
            var unityAnimationKeyList = new List<UnityAnimationKey>();

            _takesTrackLookups = new TrackData[takes.Count][];

            //takes
            for(int takeInd = 0; takeInd < takes.Count; takeInd++) {
                var take = takes[takeInd];

                //tracks
                var trackLookups = new TrackData[take.trackValues.Count];

                for(int trackInd = 0; trackInd < take.trackValues.Count; trackInd++) {
                    var track = take.trackValues[trackInd];

                    //add track to lookups, and grab index
                    int trackLookupIndex = 0;

                    switch(track.serializeType) {
                        case SerializeType.UnityAnimation:
                            trackLookupIndex = unityAnimationTrackList.Count;
                            unityAnimationTrackList.Add((UnityAnimationTrack)track);
                            break;
                        case SerializeType.Audio:
                            trackLookupIndex = audioTrackList.Count;
                            audioTrackList.Add((AudioTrack)track);
                            break;
                        case SerializeType.CameraSwitcher:
                            trackLookupIndex = cameraSwitcherTrackList.Count;
                            cameraSwitcherTrackList.Add((CameraSwitcherTrack)track);
                            break;
                        case SerializeType.Event:
                            trackLookupIndex = eventTrackList.Count;
                            eventTrackList.Add((EventTrack)track);
                            break;
                        case SerializeType.GOSetActive:
                            trackLookupIndex = goSetActiveTrackList.Count;
                            goSetActiveTrackList.Add((GOSetActiveTrack)track);
                            break;
                        case SerializeType.Material:
                            trackLookupIndex = materialTrackList.Count;
                            materialTrackList.Add((MaterialTrack)track);
                            break;
                        case SerializeType.Orientation:
                            trackLookupIndex = orientationTrackList.Count;
                            orientationTrackList.Add((OrientationTrack)track);
                            break;
                        case SerializeType.Property:
                            trackLookupIndex = propertyTrackList.Count;
                            propertyTrackList.Add((PropertyTrack)track);
                            break;
                        case SerializeType.RotationEuler:
                            trackLookupIndex = rotationEulerTrackList.Count;
                            rotationEulerTrackList.Add((RotationEulerTrack)track);
                            break;
                        case SerializeType.Rotation:
                            trackLookupIndex = rotationTrackList.Count;
                            rotationTrackList.Add((RotationTrack)track);
                            break;
                        case SerializeType.Translation:
                            trackLookupIndex = translationTrackList.Count;
                            translationTrackList.Add((TranslationTrack)track);
                            break;
                        case SerializeType.Trigger:
                            trackLookupIndex = triggerTrackList.Count;
                            triggerTrackList.Add((TriggerTrack)track);
                            break;
                    }
                    //
                    
                    var trackLookup = new TrackData { index=new SerializeIndex { index=trackLookupIndex, type=track.serializeType }, keys=new SerializeIndex[track.keys.Count] };

                    //keys
                    for(int keyInd = 0; keyInd < track.keys.Count; keyInd++) {
                        var key = track.keys[keyInd];

                        //add key to lookups, and grab index
                        int keyLookupIndex = 0;

                        switch(key.serializeType) {
                            case SerializeType.UnityAnimation:
                                keyLookupIndex = unityAnimationKeyList.Count;
                                unityAnimationKeyList.Add((UnityAnimationKey)key);
                                break;
                            case SerializeType.Audio:
                                keyLookupIndex = audioKeyList.Count;
                                audioKeyList.Add((AudioKey)key);
                                break;
                            case SerializeType.CameraSwitcher:
                                keyLookupIndex = cameraSwitcherKeyList.Count;
                                cameraSwitcherKeyList.Add((CameraSwitcherKey)key);
                                break;
                            case SerializeType.Event:
                                keyLookupIndex = eventKeyList.Count;
                                eventKeyList.Add((EventKey)key);
                                break;
                            case SerializeType.GOSetActive:
                                keyLookupIndex = goSetActiveKeyList.Count;
                                goSetActiveKeyList.Add((GOSetActiveKey)key);
                                break;
                            case SerializeType.Material:
                                keyLookupIndex = materialKeyList.Count;
                                materialKeyList.Add((MaterialKey)key);
                                break;
                            case SerializeType.Orientation:
                                keyLookupIndex = orientationKeyList.Count;
                                orientationKeyList.Add((OrientationKey)key);
                                break;
                            case SerializeType.Property:
                                keyLookupIndex = propertyKeyList.Count;
                                propertyKeyList.Add((PropertyKey)key);
                                break;
                            case SerializeType.RotationEuler:
                                keyLookupIndex = rotationEulerKeyList.Count;
                                rotationEulerKeyList.Add((RotationEulerKey)key);
                                break;
                            case SerializeType.Rotation:
                                keyLookupIndex = rotationKeyList.Count;
                                rotationKeyList.Add((RotationKey)key);
                                break;
                            case SerializeType.Translation:
                                keyLookupIndex = translationKeyList.Count;
                                translationKeyList.Add((TranslationKey)key);
                                break;
                            case SerializeType.Trigger:
                                keyLookupIndex = triggerKeyList.Count;
                                triggerKeyList.Add((TriggerKey)key);
                                break;
                        }
                        //

                        trackLookup.keys[keyInd] = new SerializeIndex { index=keyLookupIndex, type=key.serializeType };
                    }

                    trackLookups[trackInd] = trackLookup;
                }

                _takesTrackLookups[takeInd] = trackLookups;
            }

            _audioTracks = audioTrackList.ToArray();
            _audioKeys = audioKeyList.ToArray();

            _cameraSwitcherTracks = cameraSwitcherTrackList.ToArray();
            _cameraSwitcherKeys = cameraSwitcherKeyList.ToArray();

            _eventTracks = eventTrackList.ToArray();
            _eventKeys = eventKeyList.ToArray();

            _goSetActiveTracks = goSetActiveTrackList.ToArray();
            _goSetActiveKeys = goSetActiveKeyList.ToArray();

            _materialTracks = materialTrackList.ToArray();
            _materialKeys = materialKeyList.ToArray();

            _orientationTracks = orientationTrackList.ToArray();
            _orientationKeys = orientationKeyList.ToArray();

            _propertyTracks = propertyTrackList.ToArray();
            _propertyKeys = propertyKeyList.ToArray();

            _rotationEulerTracks = rotationEulerTrackList.ToArray();
            _rotationEulerKeys = rotationEulerKeyList.ToArray();

            _rotationTracks = rotationTrackList.ToArray();
            _rotationKeys = rotationKeyList.ToArray();

            _translationTracks = translationTrackList.ToArray();
            _translationKeys = translationKeyList.ToArray();

            _triggerTracks = triggerTrackList.ToArray();
            _triggerKeys = triggerKeyList.ToArray();
        }

        public void Deserialize(List<Take> takes) {
            for(int takeInd = 0; takeInd < takes.Count; takeInd++) {
                var take = takes[takeInd];
                var takeTrackLookups = _takesTrackLookups[takeInd];

                //generate tracks
                var tracks = new List<Track>(takeTrackLookups.Length);

                for(int trackInd = 0; trackInd < takeTrackLookups.Length; trackInd++) {
                    var trackLookup = takeTrackLookups[trackInd];

                    //grab the track
                    Track track = null;
                    
                    switch(trackLookup.index.type) {
                        case SerializeType.UnityAnimation:
                            track = _unityAnimationTracks[trackLookup.index.index];
                            break;
                        case SerializeType.Audio:
                            track = _audioTracks[trackLookup.index.index];
                            break;
                        case SerializeType.CameraSwitcher:
                            track = _cameraSwitcherTracks[trackLookup.index.index];
                            break;
                        case SerializeType.Event:
                            track = _eventTracks[trackLookup.index.index];
                            break;
                        case SerializeType.GOSetActive:
                            track = _goSetActiveTracks[trackLookup.index.index];
                            break;
                        case SerializeType.Material:
                            track = _materialTracks[trackLookup.index.index];
                            break;
                        case SerializeType.Orientation:
                            track = _orientationTracks[trackLookup.index.index];
                            break;
                        case SerializeType.Property:
                            track = _propertyTracks[trackLookup.index.index];
                            break;
                        case SerializeType.RotationEuler:
                            track = _rotationEulerTracks[trackLookup.index.index];
                            break;
                        case SerializeType.Rotation:
                            track = _rotationTracks[trackLookup.index.index];
                            break;
                        case SerializeType.Translation:
                            track = _translationTracks[trackLookup.index.index];
                            break;
                        case SerializeType.Trigger:
                            track = _triggerTracks[trackLookup.index.index];
                            break;
                        default:
                            Debug.LogWarning("Unsupported Type: " + trackLookup.index.type);
                            break;
                    }

                    if(track != null) {
                        //generate keys
                        var keys = new List<Key>(trackLookup.keys.Length);

                        for(int keyInd = 0; keyInd < trackLookup.keys.Length; keyInd++) {
                            var keyLookup = trackLookup.keys[keyInd];

                            //grab the key
                            Key key = null;

                            switch(keyLookup.type) {
                                case SerializeType.UnityAnimation:
                                    key = _unityAnimationKeys[keyLookup.index];
                                    break;
                                case SerializeType.Audio:
                                    key = _audioKeys[keyLookup.index];
                                    break;
                                case SerializeType.CameraSwitcher:
                                    key = _cameraSwitcherKeys[keyLookup.index];
                                    break;
                                case SerializeType.Event:
                                    key = _eventKeys[keyLookup.index];
                                    break;
                                case SerializeType.GOSetActive:
                                    key = _goSetActiveKeys[keyLookup.index];
                                    break;
                                case SerializeType.Material:
                                    key = _materialKeys[keyLookup.index];
                                    break;
                                case SerializeType.Orientation:
                                    key = _orientationKeys[keyLookup.index];
                                    break;
                                case SerializeType.Property:
                                    key = _propertyKeys[keyLookup.index];
                                    break;
                                case SerializeType.RotationEuler:
                                    key = _rotationEulerKeys[keyLookup.index];
                                    break;
                                case SerializeType.Rotation:
                                    key = _rotationKeys[keyLookup.index];
                                    break;
                                case SerializeType.Translation:
                                    key = _translationKeys[keyLookup.index];
                                    break;
                                case SerializeType.Trigger:
                                    key = _triggerKeys[keyLookup.index];
                                    break;
                                default:
                                    Debug.LogWarning("Unsupported Type: " + keyLookup.type);
                                    break;
                            }

                            keys.Add(key);
                        }

                        track.keys = keys;
                    }

                    tracks.Add(track);
                }

                take.trackValues = tracks;
            }
        }
    }
}