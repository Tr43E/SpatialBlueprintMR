using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpatialBlueprintMR
{
    /// <summary>Owns plan transform, scale calibration, and simple local placement persistence.</summary>
    [RequireComponent(typeof(BlueprintModel))]
    public sealed class PlanPlacementController : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float nudgeSpeedMetresPerSecond = 0.8f;
        [SerializeField, Min(1f)] private float turnSpeedDegreesPerSecond = 45f;
        [SerializeField] private bool desktopControlsEnabled = true;

        public bool IsLocked { get; private set; }
        public float CalibrationFactor { get; private set; } = 1f;

        private BlueprintModel _model;

        [Serializable]
        private struct PlacementRecord
        {
            public Vector3 Position;
            public Vector3 EulerAngles;
            public float UniformScale;
            public float CalibrationFactor;
        }

        private void Awake()
        {
            _model = GetComponent<BlueprintModel>();
        }

        private void Update()
        {
            if (!desktopControlsEnabled)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.lKey.wasPressedThisFrame)
            {
                ToggleLock();
            }
            if (keyboard.pKey.wasPressedThisFrame)
            {
                SavePlacement();
            }
            if (keyboard.oKey.wasPressedThisFrame)
            {
                LoadPlacement();
            }
            if (IsLocked)
            {
                return;
            }

            var move = new Vector3(
                (keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard.leftArrowKey.isPressed ? 1f : 0f),
                0f,
                (keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard.downArrowKey.isPressed ? 1f : 0f));
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }
            NudgeFromCamera(move * (nudgeSpeedMetresPerSecond * Time.deltaTime));

            if (keyboard.qKey.isPressed) Rotate(-turnSpeedDegreesPerSecond * Time.deltaTime);
            if (keyboard.eKey.isPressed) Rotate(turnSpeedDegreesPerSecond * Time.deltaTime);
            if (keyboard.pageUpKey.isPressed) Raise(nudgeSpeedMetresPerSecond * Time.deltaTime);
            if (keyboard.pageDownKey.isPressed) Raise(-nudgeSpeedMetresPerSecond * Time.deltaTime);
            if (keyboard.leftBracketKey.wasPressedThisFrame) MultiplyScale(0.9f);
            if (keyboard.rightBracketKey.wasPressedThisFrame) MultiplyScale(1.1f);
        }

        /// <summary>Pass a local-space displacement from an XR controller or UI button.</summary>
        public void Nudge(Vector3 localMetres)
        {
            if (!IsLocked)
            {
                transform.position += transform.TransformDirection(localMetres);
            }
        }

        /// <summary>Moves the plan in screen-relative directions for the desktop preview.</summary>
        private void NudgeFromCamera(Vector3 cameraRelativeMetres)
        {
            var view = Camera.main;
            if (view == null)
            {
                transform.position += cameraRelativeMetres;
                return;
            }

            var forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                transform.position += cameraRelativeMetres;
                return;
            }

            forward.Normalize();
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            transform.position += right * cameraRelativeMetres.x + forward * cameraRelativeMetres.z + Vector3.up * cameraRelativeMetres.y;
        }

        public void Rotate(float degreesAroundUp)
        {
            if (!IsLocked)
            {
                transform.Rotate(Vector3.up, degreesAroundUp, Space.World);
            }
        }

        public void Raise(float metres)
        {
            if (!IsLocked)
            {
                transform.position += Vector3.up * metres;
            }
        }

        /// <summary>Corrects a non-1:1 drawing using a known dimension from its original CAD coordinate system.</summary>
        public void Calibrate(float referenceLengthInCadUnits, float measuredLengthInMetres)
        {
            if (referenceLengthInCadUnits <= 0f || measuredLengthInMetres <= 0f)
            {
                throw new ArgumentOutOfRangeException("Calibration dimensions must be greater than zero.");
            }
            var expectedMetres = referenceLengthInCadUnits * _model.MetresPerCadUnit;
            CalibrationFactor = measuredLengthInMetres / expectedMetres;
            transform.localScale = Vector3.one * CalibrationFactor;
        }

        public void MultiplyScale(float factor)
        {
            if (!IsLocked && factor > 0f)
            {
                CalibrationFactor *= factor;
                transform.localScale = Vector3.one * CalibrationFactor;
            }
        }

        public void LockPlacement() => IsLocked = true;
        public void UnlockPlacement() => IsLocked = false;
        public void ToggleLock() => IsLocked = !IsLocked;

        public void SavePlacement()
        {
            var record = new PlacementRecord
            {
                Position = transform.position,
                EulerAngles = transform.eulerAngles,
                UniformScale = transform.localScale.x,
                CalibrationFactor = CalibrationFactor
            };
            File.WriteAllText(PlacementPath, JsonUtility.ToJson(record));
            Debug.Log($"Saved blueprint placement to {PlacementPath}", this);
        }

        public void LoadPlacement()
        {
            if (!File.Exists(PlacementPath))
            {
                Debug.LogWarning("No saved blueprint placement exists yet.", this);
                return;
            }
            var record = JsonUtility.FromJson<PlacementRecord>(File.ReadAllText(PlacementPath));
            transform.position = record.Position;
            transform.eulerAngles = record.EulerAngles;
            transform.localScale = Vector3.one * record.UniformScale;
            CalibrationFactor = record.CalibrationFactor;
        }

        private string PlacementPath => Path.Combine(Application.persistentDataPath, "spatial-blueprint-placement.json");
    }
}
