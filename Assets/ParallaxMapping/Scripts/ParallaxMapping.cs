using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ParallaxMapping {

    public class ConstantBufferVariable {

        public Matrix4x4 ViewProjMatrix;
        public Matrix4x4 ViewMatrix;
        public Matrix4x4 WorldMatrix;
        public Matrix4x4 NormalMatrix;

        public Matrix4x4 InvViewProjMatrix;
        public Matrix4x4 InvViewMatrix;
        public Matrix4x4 InvWorldMatrix;
        public Matrix4x4 InvNormalMatrix;

        public int   StepCount;
        public int   FrequencyU;
        public int   FrequencyV;
        public float Threshold;
        public float NormalFactor;

        public Vector3 MaterialAlbedo;
        public float   MaterialRoughness;
        public float   MaterialMetalness;

        public Vector3 BoundingBoxMax;
        public Vector3 BoundingBoxMin;
        public Vector2 RenderTargetDim;

        public Vector3 PointLightPosition;
        public Vector3 PointLightColor;
        public float PointLightIntensity;
        public float PointLightRange;

        public static void Apply(ComputeShader shader, ConstantBufferVariable buffer) {

            shader.SetMatrix("ViewProjMatrix", buffer.ViewProjMatrix);
            shader.SetMatrix("ViewMatrix", buffer.ViewMatrix);
            shader.SetMatrix("WorldMatrix", buffer.WorldMatrix);
            shader.SetMatrix("NormalMatrix", buffer.NormalMatrix);

            shader.SetMatrix("InvViewProjMatrix", buffer.InvViewProjMatrix);
            shader.SetMatrix("InvViewMatrix", buffer.InvViewMatrix);
            shader.SetMatrix("InvWorldMatrix", buffer.InvWorldMatrix);
            shader.SetMatrix("InvNormalMatrix", buffer.InvNormalMatrix);

            shader.SetInt("StepCount", buffer.StepCount);
            shader.SetInt("FrequencyU", buffer.FrequencyU);
            shader.SetInt("FrequencyV", buffer.FrequencyV);
            shader.SetFloat("Threshold", buffer.Threshold);
            shader.SetFloat("NormalFactor", buffer.NormalFactor);

            shader.SetVector("MaterialAlbedo", buffer.MaterialAlbedo);
            shader.SetFloat("MaterialRoughness", buffer.MaterialRoughness);
            shader.SetFloat("MaterialMetalness", buffer.MaterialMetalness);


            shader.SetVector("BoundingBoxMin", buffer.BoundingBoxMin);
            shader.SetVector("BoundingBoxMax", buffer.BoundingBoxMax);

            shader.SetVector("RenderTargetDim", buffer.RenderTargetDim);

            shader.SetVector("PointLightPosition", buffer.PointLightPosition);
            shader.SetVector("PointLightColor", buffer.PointLightColor);
            shader.SetFloat("PointLightIntensity", buffer.PointLightIntensity);
            shader.SetFloat("PointLightRange", buffer.PointLightRange);
        }
    }

    [RequireComponent(typeof(Camera)), ExecuteInEditMode, ImageEffectAllowedInSceneView]
    public class ParallaxMapping : MonoBehaviour {

        [Range(1, 512)]      public int   StepCount = 512;
        [Range(0.01f, 0.1f)] public float Threshold = 0.05f;

        public Texture2D TextureHeight;
        public Texture2D TextureEnvironment;
        public ComputeShader ParallaxMappingShader;

        private Camera         m_Camera;
        private ParallaxVolume m_Volume;
        private Light          m_PointLight;

        private RenderTexture m_TextureColor;
        private ConstantBufferVariable m_ConstantBuffer = new ConstantBufferVariable();

        void Start() {

            m_Camera = GetComponent<Camera>();
            m_Volume = GameObject.Find("Volume").GetComponent<ParallaxVolume>();
            m_PointLight = GameObject.Find("Point Light").GetComponent<Light>();

#if UNITY_EDITOR
            EditorApplication.update += Update;
#endif
        }

        private void InitializeRenderTexture(int width, int height) {

            if (m_TextureColor == null || m_TextureColor.width != width || m_TextureColor.height != height) {

                if (m_TextureColor != null)
                    m_TextureColor.Release();

                m_TextureColor = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                m_TextureColor.enableRandomWrite = true;
                m_TextureColor.Create();
            }
        }

        private void OnDestroy() {

#if UNITY_EDITOR

            EditorApplication.update -= Update;
#endif
        }

        void Update() {

            if (m_Volume != null && m_PointLight != null) {

                m_ConstantBuffer.ViewProjMatrix = m_Camera.projectionMatrix * m_Camera.worldToCameraMatrix;
                m_ConstantBuffer.ViewMatrix = m_Camera.worldToCameraMatrix;
                m_ConstantBuffer.WorldMatrix = Matrix4x4.Translate(m_Volume.transform.position) * Matrix4x4.Rotate(m_Volume.transform.rotation);
                m_ConstantBuffer.NormalMatrix = Matrix4x4.Transpose(Matrix4x4.Inverse(m_ConstantBuffer.WorldMatrix));

                m_ConstantBuffer.InvViewProjMatrix = Matrix4x4.Inverse(m_ConstantBuffer.ViewProjMatrix);
                m_ConstantBuffer.InvViewMatrix = Matrix4x4.Inverse(m_ConstantBuffer.ViewMatrix);
                m_ConstantBuffer.InvWorldMatrix = Matrix4x4.Inverse(m_ConstantBuffer.WorldMatrix);
                m_ConstantBuffer.InvNormalMatrix = Matrix4x4.Inverse(m_ConstantBuffer.NormalMatrix);

                m_ConstantBuffer.StepCount = StepCount;
                m_ConstantBuffer.Threshold = Threshold;
                m_ConstantBuffer.FrequencyU = m_Volume.FrequencyU;
                m_ConstantBuffer.FrequencyV = m_Volume.FrequencyV;
                m_ConstantBuffer.NormalFactor = m_Volume.NormalFactor;

                m_ConstantBuffer.MaterialAlbedo = new Vector3(m_Volume.Albedo.r, m_Volume.Albedo.g, m_Volume.Albedo.b);
                m_ConstantBuffer.MaterialMetalness = m_Volume.Metalness;
                m_ConstantBuffer.MaterialRoughness = m_Volume.Roughness;


                m_ConstantBuffer.BoundingBoxMin = Matrix4x4.Scale(m_Volume.transform.localScale) * m_Volume.BoundingBox.min;
                m_ConstantBuffer.BoundingBoxMax = Matrix4x4.Scale(m_Volume.transform.localScale) * m_Volume.BoundingBox.max;
                m_ConstantBuffer.RenderTargetDim = new Vector2(m_Camera.pixelWidth, m_Camera.pixelHeight);

                m_ConstantBuffer.PointLightColor = new Vector3(m_PointLight.color.r, m_PointLight.color.g, m_PointLight.color.b);
                m_ConstantBuffer.PointLightPosition = m_PointLight.transform.position;
                m_ConstantBuffer.PointLightIntensity = m_PointLight.intensity;
                m_ConstantBuffer.PointLightRange = m_PointLight.range;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination) {

            InitializeRenderTexture(m_Camera.pixelWidth, m_Camera.pixelHeight);

            var kernel = ParallaxMappingShader.FindKernel("ParallaxMapping");
            ConstantBufferVariable.Apply(ParallaxMappingShader, m_ConstantBuffer);

            ParallaxMappingShader.SetTexture(kernel, "TextureHeightMap", TextureHeight);
            ParallaxMappingShader.SetTexture(kernel, "TextureColorUAV", m_TextureColor);
            ParallaxMappingShader.Dispatch(kernel, Mathf.CeilToInt(m_Camera.pixelWidth / 8.0f), Mathf.CeilToInt(m_Camera.pixelHeight / 8.0f), 1);

            Graphics.Blit(m_TextureColor, destination);
        }
    }
}
