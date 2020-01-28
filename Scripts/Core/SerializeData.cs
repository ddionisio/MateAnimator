using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.Animator {
    [System.Serializable]
    public class SerializeData {        
        [System.Serializable]
        public struct TrackData {
            public SerializeType type;
            public int index;
            public int[] keyIndices;
        }

        public static Track CreateTrack(SerializeType type) {
            switch(type) {
                case SerializeType.UnityAnimation:
                    return new UnityAnimationTrack();
                case SerializeType.Audio:
                    return new AudioTrack();
                case SerializeType.CameraSwitcher:
                    return new CameraSwitcherTrack();
                case SerializeType.Event:
                    return new EventTrack();
                case SerializeType.GOSetActive:
                    return new GOSetActiveTrack();
                case SerializeType.Material:
                    return new MaterialTrack();
                case SerializeType.Orientation:
                    return new OrientationTrack();
                case SerializeType.Property:
                    return new PropertyTrack();
                case SerializeType.RotationEuler:
                    return new RotationEulerTrack();
                case SerializeType.Rotation:
                    return new RotationTrack();
                case SerializeType.Translation:
                    return new TranslationTrack();
                case SerializeType.Scale:
                    return new ScaleTrack();
            }

            return null;
        }

        public static Key CreateKey(SerializeType type) {
            switch(type) {
                case SerializeType.UnityAnimation:
                    return new UnityAnimationKey();
                case SerializeType.Audio:
                    return new AudioKey();
                case SerializeType.CameraSwitcher:
                    return new CameraSwitcherKey();
                case SerializeType.Event:
                    return new EventKey();
                case SerializeType.GOSetActive:
                    return new GOSetActiveKey();
                case SerializeType.Material:
                    return new MaterialKey();
                case SerializeType.Orientation:
                    return new OrientationKey();
                case SerializeType.Property:
                    return new PropertyKey();
                case SerializeType.RotationEuler:
                    return new RotationEulerKey();
                case SerializeType.Rotation:
                    return new RotationKey();
                case SerializeType.Translation:
                    return new TranslationKey();
                case SerializeType.Scale:
                    return new ScaleKey();
            }

            return null;
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
        
        [SerializeField] UnityAnimationTrack[] _unityAnimationTracks;
        [SerializeField] UnityAnimationKey[] _unityAnimationKeys;

        [SerializeField] ScaleTrack[] _scaleTracks;
        [SerializeField] ScaleKey[] _scaleKeys;

        [SerializeField] TrackData[] _trackLookups;
        [SerializeField] int[] _takeTrackCounts;

        public bool isEmpty {
            get {
                int trackCounts = 0;
                if(_takeTrackCounts != null) {
                    for(int i = 0; i < _takeTrackCounts.Length; i++)
                        trackCounts += _takeTrackCounts[i];
                }

                return trackCounts == 0;
            }
        }

        public void Serialize(List<Take> takes) {
            if(takes == null)
                return;

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
            
            var unityAnimationTrackList = new List<UnityAnimationTrack>();
            var unityAnimationKeyList = new List<UnityAnimationKey>();

            var scaleTrackList = new List<ScaleTrack>();
            var scaleKeyList = new List<ScaleKey>();

            var trackLookupList = new List<TrackData>();

            _takeTrackCounts = new int[takes.Count];

            //takes
            for(int takeInd = 0; takeInd < takes.Count; takeInd++) {
                var take = takes[takeInd];

                _takeTrackCounts[takeInd] = take.trackValues.Count;

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
                        case SerializeType.Scale:
                            trackLookupIndex = scaleTrackList.Count;
                            scaleTrackList.Add((ScaleTrack)track);
                            break;
                    }
                    //
                    
                    var trackLookup = new TrackData { type=track.serializeType, index=trackLookupIndex, keyIndices=new int[track.keys.Count] };

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
                            case SerializeType.Scale:
                                keyLookupIndex = scaleKeyList.Count;
                                scaleKeyList.Add((ScaleKey)key);
                                break;
                        }
                        //

                        trackLookup.keyIndices[keyInd] = keyLookupIndex;
                    }

                    trackLookups[trackInd] = trackLookup;
                }

                trackLookupList.AddRange(trackLookups);
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

            _scaleTracks = scaleTrackList.ToArray();
            _scaleKeys = scaleKeyList.ToArray();

            _unityAnimationTracks = unityAnimationTrackList.ToArray();
            _unityAnimationKeys = unityAnimationKeyList.ToArray();
            
            _trackLookups = trackLookupList.ToArray();
        }

        public void Deserialize(List<Take> takes) {
            if(takes == null)
                return;

            int takeTrackLookupInd = 0;

            for(int takeInd = 0; takeInd < takes.Count; takeInd++) {
                var take = takes[takeInd];

                int trackCount = _takeTrackCounts[takeInd];

                //generate tracks
                var tracks = new List<Track>(trackCount);

                for(int trackInd = 0; trackInd < trackCount; trackInd++, takeTrackLookupInd++) {
                    var trackLookup = _trackLookups[takeTrackLookupInd];

                    //grab the track
                    SerializeType trackType = trackLookup.type;
                    Track track = null;
                    
                    switch(trackType) {
                        case SerializeType.UnityAnimation:
                            track = _unityAnimationTracks[trackLookup.index];
                            break;
                        case SerializeType.Audio:
                            track = _audioTracks[trackLookup.index];
                            break;
                        case SerializeType.CameraSwitcher:
                            track = _cameraSwitcherTracks[trackLookup.index];
                            break;
                        case SerializeType.Event:
                            track = _eventTracks[trackLookup.index];
                            break;
                        case SerializeType.GOSetActive:
                            track = _goSetActiveTracks[trackLookup.index];
                            break;
                        case SerializeType.Material:
                            track = _materialTracks[trackLookup.index];
                            break;
                        case SerializeType.Orientation:
                            track = _orientationTracks[trackLookup.index];
                            break;
                        case SerializeType.Property:
                            track = _propertyTracks[trackLookup.index];
                            break;
                        case SerializeType.RotationEuler:
                            track = _rotationEulerTracks[trackLookup.index];
                            break;
                        case SerializeType.Rotation:
                            track = _rotationTracks[trackLookup.index];
                            break;
                        case SerializeType.Translation:
                            track = _translationTracks[trackLookup.index];
                            break;
                        case SerializeType.Scale:
                            track = _scaleTracks[trackLookup.index];
                            break;
                        default:
                            Debug.LogWarning("Unsupported Type: " + trackType);
                            break;
                    }

                    if(track != null) {
                        //generate keys
                        var keys = new List<Key>(trackLookup.keyIndices.Length);

                        for(int keyInd = 0; keyInd < trackLookup.keyIndices.Length; keyInd++) {
                            int keyLookupIndex = trackLookup.keyIndices[keyInd];

                            //grab the key
                            Key key = null;

                            switch(trackType) {
                                case SerializeType.UnityAnimation:
                                    key = _unityAnimationKeys[keyLookupIndex];
                                    break;
                                case SerializeType.Audio:
                                    key = _audioKeys[keyLookupIndex];
                                    break;
                                case SerializeType.CameraSwitcher:
                                    key = _cameraSwitcherKeys[keyLookupIndex];
                                    break;
                                case SerializeType.Event:
                                    key = _eventKeys[keyLookupIndex];
                                    break;
                                case SerializeType.GOSetActive:
                                    key = _goSetActiveKeys[keyLookupIndex];
                                    break;
                                case SerializeType.Material:
                                    key = _materialKeys[keyLookupIndex];
                                    break;
                                case SerializeType.Orientation:
                                    key = _orientationKeys[keyLookupIndex];
                                    break;
                                case SerializeType.Property:
                                    key = _propertyKeys[keyLookupIndex];
                                    break;
                                case SerializeType.RotationEuler:
                                    key = _rotationEulerKeys[keyLookupIndex];
                                    break;
                                case SerializeType.Rotation:
                                    key = _rotationKeys[keyLookupIndex];
                                    break;
                                case SerializeType.Translation:
                                    key = _translationKeys[keyLookupIndex];
                                    break;
                                case SerializeType.Scale:
                                    key = _scaleKeys[keyLookupIndex];
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