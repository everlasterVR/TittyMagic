using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TittyMagic.Components
{
    public class UIMod : MonoBehaviour
    {
        private readonly List<RectTransformChange> _movedRects = new List<RectTransformChange>();
        private readonly List<GameObject> _inactivatedUIGameObjects = new List<GameObject>();
        private readonly List<GameObject> _customUIGameObjects = new List<GameObject>();

        private Transform _container;
        public string targetName { get; private set; }
        private UnityEventsListener _eventListener;
        private Func<UIMod, IEnumerator> _applyChanges;

        public void Init(Transform container, string targetTransformName, Func<UIMod, IEnumerator> changesFunc)
        {
            _container = container;
            targetName = targetTransformName;
            _applyChanges = changesFunc;
        }

        public void Apply()
        {
            if(_container.gameObject.activeInHierarchy)
            {
                StartCoroutine(DeferApply());
            }
            /* Add listener to wait for the transform to be active in hierarchy */
            else
            {
                _eventListener = _container.gameObject.AddComponent<UnityEventsListener>();
                _eventListener.onEnable.AddListener(() => StartCoroutine(DeferApply()));
            }
        }

        private bool _modified;
        public Transform target { get; private set; }

        private IEnumerator DeferApply()
        {
            if(_modified)
            {
                yield break;
            }

            /* Set modified immediately to prevent double apply in case eventListener
             * gets enabled multiple times in a short period of time
             */
            _modified = true;

            float waited = 0f;
            while(waited < 1)
            {
                waited += 0.1f;
                yield return new WaitForSecondsRealtime(0.1f);
                target = _container.Find(targetName);
            }

            if(target == null)
            {
                Utils.LogError($"Failed to {targetName} UI - could not find transform.");
                yield break;
            }

            yield return _applyChanges(this);
        }

        public void InactivateChildren()
        {
            foreach(Transform child in target)
            {
                if(child.gameObject.activeSelf)
                {
                    _inactivatedUIGameObjects.Add(child.gameObject);
                    child.gameObject.SetActive(false);
                }
            }
        }

        public void AddCustomObject(GameObject go) => _customUIGameObjects.Add(go);

        public void MoveTransform(Transform t, int x, int y) =>
            _movedRects.Add(new RectTransformChange(t.GetComponent<RectTransform>(), new Vector2(x, y)));

        public void Enable()
        {
            if(Script.tittyMagic.initialized && !_modified)
            {
                Apply();
            }
            else
            {
                _inactivatedUIGameObjects.ForEach(go => go.SetActive(false));
                _customUIGameObjects.ForEach(go => go.SetActive(true));
                _movedRects.ForEach(change => change.Apply());
            }
        }

        public void Disable()
        {
            _customUIGameObjects.ForEach(go => go.SetActive(false));
            _inactivatedUIGameObjects.ForEach(go => go.SetActive(true));
            _movedRects.ForEach(change => change.RestoreOriginal());
        }

        public void OnDestroy()
        {
            _customUIGameObjects.ForEach(Destroy);
            DestroyImmediate(_eventListener);
        }
    }
}
