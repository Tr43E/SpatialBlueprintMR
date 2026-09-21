using UnityEngine;
using UnityEngine.InputSystem;

namespace SpatialBlueprintMR
{
    /// <summary>Provides intuitive, camera-local movement for the desktop preview.</summary>
    [DisallowMultipleComponent]
    public sealed class DesktopPreviewCameraController : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float movementSpeedMetresPerSecond = 2f;
        [SerializeField, Min(0f)] private float birdsEyePaddingMetres = 2f;
        [SerializeField, Min(0.01f)] private float mouseLookSensitivity = 0.12f;
        [SerializeField, Range(-89f, 0f)] private float minimumPitchDegrees = -80f;
        [SerializeField, Range(0f, 89f)] private float maximumPitchDegrees = 80f;

        public bool IsBirdsEyeView { get; private set; }

        private Camera _camera;
        private Vector3 _savedPosition;
        private Quaternion _savedRotation;
        private float _yawDegrees;
        private float _pitchDegrees;
        private bool _isMouseLooking;

        private void Awake()
        {
            SyncLookAnglesFromTransform();
        }

        private void OnDisable()
        {
            StopMouseLook();
        }

        private void Update()
        {
            HandleMouseLook();

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.bKey.wasPressedThisFrame)
            {
                ToggleBirdsEyeView(FindFirstObjectByType<BlueprintModel>());
                return;
            }

            var input = new Vector2(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var displacement = (right * input.x + forward * input.y) * (movementSpeedMetresPerSecond * Time.deltaTime);
            transform.position += displacement;
        }

        private void HandleMouseLook()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                _isMouseLooking = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                SyncLookAnglesFromTransform();
            }
            if (mouse.rightButton.wasReleasedThisFrame)
            {
                StopMouseLook();
                return;
            }
            if (!_isMouseLooking)
            {
                return;
            }

            var delta = mouse.delta.ReadValue() * mouseLookSensitivity;
            _yawDegrees += delta.x;
            _pitchDegrees = Mathf.Clamp(_pitchDegrees - delta.y, minimumPitchDegrees, maximumPitchDegrees);
            transform.rotation = Quaternion.Euler(_pitchDegrees, _yawDegrees, 0f);
        }

        private void StopMouseLook()
        {
            _isMouseLooking = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void SyncLookAnglesFromTransform()
        {
            var angles = transform.eulerAngles;
            _yawDegrees = angles.y;
            _pitchDegrees = angles.x > 180f ? angles.x - 360f : angles.x;
        }

        /// <summary>Frames the generated plan from above, or restores the camera's prior scene view.</summary>
        public bool ToggleBirdsEyeView(BlueprintModel layout)
        {
            if (IsBirdsEyeView)
            {
                transform.SetPositionAndRotation(_savedPosition, _savedRotation);
                SyncLookAnglesFromTransform();
                IsBirdsEyeView = false;
                return true;
            }

            if (layout == null || !layout.TryGetLayoutBounds(out var bounds))
            {
                Debug.LogWarning("Bird's-eye view needs an imported blueprint with at least one wall.", this);
                return false;
            }

            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            _savedPosition = transform.position;
            _savedRotation = transform.rotation;

            var halfVerticalFovRadians = ((_camera != null ? _camera.fieldOfView : 60f) * Mathf.Deg2Rad) * 0.5f;
            var verticalDistance = (bounds.size.z + birdsEyePaddingMetres * 2f) / (2f * Mathf.Tan(halfVerticalFovRadians));
            var horizontalFovRadians = 2f * Mathf.Atan(Mathf.Tan(halfVerticalFovRadians) * (_camera != null ? _camera.aspect : 1.777f));
            var horizontalDistance = (bounds.size.x + birdsEyePaddingMetres * 2f) / (2f * Mathf.Tan(horizontalFovRadians * 0.5f));
            var height = Mathf.Max(verticalDistance, horizontalDistance, 4f);

            transform.position = new Vector3(bounds.center.x, bounds.max.y + height, bounds.center.z);
            transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            SyncLookAnglesFromTransform();
            IsBirdsEyeView = true;
            return true;
        }
    }
}

