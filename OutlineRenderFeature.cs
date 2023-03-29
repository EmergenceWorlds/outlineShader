using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class OutlineRenderFeature : ScriptableRendererFeature
{
    class OutlineRenderPass : ScriptableRenderPass
    {
        private RenderTargetHandle outlineTexture;
        private RenderTexture cameraColorTextureBuffer;

        private RenderTextureDescriptor descriptor;
        private RenderTargetIdentifier cameraColorTargetIdent;

        private Material outlineMaterial = null, blitMaterial = null, blurMaterial = null;
        
        private FilteringSettings filteringSettings;

        public OutlineRenderPass(RenderQueueRange renderQueueRange, Settings settings)
        {
            filteringSettings = new FilteringSettings(renderQueueRange, settings.layerMask.value);
            outlineMaterial = settings.outlineMaterial;
            blitMaterial = settings.blitMaterial;
            blurMaterial = settings.blurMaterial;

            outlineTexture.Init("_OutlineTexture");
        }

        public void Setup(RenderTextureDescriptor descriptor, RenderTargetIdentifier cameraColorTargetIdent)
        {
            this.descriptor = descriptor;
            this.descriptor.colorFormat = RenderTextureFormat.ARGB32;
            this.cameraColorTargetIdent = cameraColorTargetIdent;
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(outlineTexture.id, descriptor, FilterMode.Bilinear);
            // first texture needs depth, the cam buffer tex doesn't for some reason
            descriptor.depthBufferBits = 0;
            cameraColorTextureBuffer = RenderTexture.GetTemporary(descriptor);
            ConfigureClear(ClearFlag.All, Color.black);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.Clear();

            // draw opqaue pass
            DrawingSettings drawSettings = CreateDrawingSettings(new ShaderTagId("UniversalForward"), ref renderingData, SortingCriteria.RenderQueue);
            drawSettings.perObjectData = PerObjectData.None;

            drawSettings.overrideMaterial = outlineMaterial;
            drawSettings.overrideMaterialPassIndex = 0;

            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);
            ConfigureTarget(outlineTexture.Identifier());

            // perform blur
            blurMaterial.SetInt("_TextureWidth", descriptor.width);
            blurMaterial.SetInt("_TextureHeight", descriptor.height);
            cmd.Blit(outlineTexture.Identifier(), outlineTexture.Identifier(), blurMaterial, 0);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // draw subtraction pass
            drawSettings = CreateDrawingSettings(new ShaderTagId("UniversalForward"), ref renderingData, SortingCriteria.RenderQueue);
            drawSettings.perObjectData = PerObjectData.None;

            drawSettings.overrideMaterial = outlineMaterial;
            drawSettings.overrideMaterialPassIndex = 1;

            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);

            // get current camera color
            Blit(cmd, cameraColorTargetIdent, cameraColorTextureBuffer);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            blitMaterial.SetTexture("_CameraColorTex", cameraColorTextureBuffer);
            Blit(cmd, outlineTexture.Identifier(), cameraColorTargetIdent, blitMaterial, 0);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(outlineTexture.id);
            RenderTexture.ReleaseTemporary(cameraColorTextureBuffer);
        }
    }

    OutlineRenderPass outlineRenderPass;

    [System.Serializable]
    public class Settings
    {
        public LayerMask layerMask;
        public Material outlineMaterial, blurMaterial, blitMaterial;
        public RenderPassEvent renderPassEvent;
    }

    public Settings settings;

    public override void Create()
    {
        outlineRenderPass = new OutlineRenderPass(RenderQueueRange.all, settings);
        outlineRenderPass.renderPassEvent = settings.renderPassEvent;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        outlineRenderPass.Setup(renderingData.cameraData.cameraTargetDescriptor, renderer.cameraColorTarget);
        renderer.EnqueuePass(outlineRenderPass);
    }
}