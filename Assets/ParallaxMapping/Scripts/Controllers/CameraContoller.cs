using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ParallaxMapping {

    [RequireComponent(typeof(Camera))]
    public class CameraContoller : MonoBehaviour {

        static readonly string KeyMouseX = "Mouse X";
        static readonly string KeyMouseY = "Mouse Y";
        static readonly string KeyMouseScroll = "Mouse ScrollWheel";

        [SerializeField, Range(0.1f, 40.0f)] public float ZoomSpeed   = 7.5f, ZoomDelta   = 5.0f;
        [SerializeField, Range(0.1f, 3.0f)]  public float ZoomMin     = 0.1f, ZoomMax     = 1.0f;
        [SerializeField, Range(1.0f, 40.0f)] public float RotateSpeed = 7.5f, RotateDelta = 5.0f;

        private Camera     m_Camera;
        private Quaternion m_LerpRotation;
        private float      m_LerpZoom;

        void Start() {
            m_Camera = GetComponent<Camera>();
            m_LerpRotation = m_Camera.transform.rotation;
            m_LerpZoom = 1.0f;
        }

        void Update() {
            if (Input.GetMouseButton(0)) {
                var mouseX = Input.GetAxis(KeyMouseX) * RotateSpeed;
                var mouseY = Input.GetAxis(KeyMouseY) * RotateSpeed;

                var up = transform.InverseTransformDirection(m_Camera.transform.up);
                var right = transform.InverseTransformDirection(m_Camera.transform.right);

                m_LerpRotation *= Quaternion.AngleAxis(+mouseX, up);
                m_LerpRotation *= Quaternion.AngleAxis(-mouseY, right);
            }

            if (Input.GetKeyDown(KeyCode.F)) {
                Debug.Log("Save frame");
                ScreenCapture.CaptureScreenshot("FrameCapture.png");
            }

            if (Mathf.Abs(Input.GetAxis(KeyMouseScroll)) > 0f) {
                m_LerpZoom -= ZoomSpeed * Input.GetAxis(KeyMouseScroll);
                m_LerpZoom = Mathf.Clamp(m_LerpZoom, ZoomMin, ZoomMax);
            }

            if (Mathf.Abs(m_Camera.orthographicSize - m_LerpZoom) > 0.01f || (m_Camera.transform.rotation.eulerAngles - m_LerpRotation.eulerAngles).magnitude > 0.01f) {
                m_Camera.orthographicSize = Mathf.Lerp(m_Camera.orthographicSize, m_LerpZoom, Time.deltaTime * ZoomDelta);
                m_Camera.transform.rotation = Quaternion.Slerp(m_Camera.transform.rotation, m_LerpRotation, Time.deltaTime * RotateDelta);
                m_Camera.transform.hasChanged = true;
            } else {
                m_Camera.transform.hasChanged = false;
            }
        }
    }
}
