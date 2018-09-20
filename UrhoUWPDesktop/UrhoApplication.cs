using System;
using Urho;
using Urho.Resources;
using Urho.Shapes;
using Urho.Urho2D;
using UrhoUWPBackgroundRenderer;

namespace UrhoUWPDesktop
{
    public class UrhoApplication : Application
    {
        public const int VIDEO_WIDTH_LOW = 896;

        public const int VIDEO_HEIGHT_LOW = 504;

        protected const float TouchSensitivity = 2;

        protected float Yaw { get; set; }

        protected float Pitch { get; set; }

        protected bool TouchEnabled { get; set; }

        protected Node CameraNode { get; set; }

        protected MonoDebugHud debugHud { get; set; }

        private VideoPlayer _videoPlayer;

        private Camera _camera;

        [Preserve]
        public UrhoApplication(ApplicationOptions options = null) : base(options) { }

        protected unsafe override void Start()
        {
            // Create scene
            var scene = new Scene();
            scene.CreateComponent<Octree>();

            // Create a node for the Earth
            var earthNode = scene.CreateChild();
            earthNode.Position = new Vector3(0, 0, 1.5f);
            earthNode.SetScale(0.3f);

            // Add light
            var lightNode = scene.CreateChild(name: "light");
            lightNode.SetDirection(new Vector3(0.6f, -1.0f, 0.8f));
            lightNode.CreateComponent<Light>();

            var earth = earthNode.CreateComponent<Sphere>();
            var earthMaterial = ResourceCache.GetMaterial("Materials/Earth.xml");
            earth.SetMaterial(earthMaterial);

            var moonNode = earthNode.CreateChild();
            moonNode.SetScale(0.27f);
            moonNode.Position = new Vector3(1.2f, 0, 0);

            var moon = moonNode.CreateComponent<Sphere>();
            moon.SetMaterial(ResourceCache.GetMaterial("Materials/Moon.xml"));

            // Camera
            this.CameraNode = scene.CreateChild(name: "camera");
            _camera = this.CameraNode.CreateComponent<Camera>();
            _camera.Fov = 27.15f;
            _camera.NearClip = 0.1f;
            _camera.FarClip = 1000.0f;
            _camera.AspectRatio = (float)VIDEO_WIDTH_LOW / VIDEO_HEIGHT_LOW;

            // Viewport
            var viewport = new Viewport(scene, _camera, null);
            var renderPathFile = ResourceCache.GetXmlFile("RenderPaths/CustomRenderPath.xml");
            viewport.SetRenderPath(renderPathFile);
            Renderer.SetViewport(0, viewport);

            // Video feed background
            _videoPlayer = new VideoPlayer();
            var videoTextureY = _videoPlayer.TextureY;
            videoTextureY.Name = "video_feed_y";

            var videoTextureV = _videoPlayer.TextureV;
            videoTextureV.Name = "video_feed_v";

            var videoTextureU = _videoPlayer.TextureU;
            videoTextureU.Name = "video_feed_u";

            // Add render texture to res cache so it's available to render path
            ResourceCache.AddManualResource(videoTextureY);
            ResourceCache.AddManualResource(videoTextureV);
            ResourceCache.AddManualResource(videoTextureU);

            // DebugHud
            debugHud = new MonoDebugHud(this);
            debugHud.Show();
        }

        protected override void OnUpdate(float timeStep)
        {
            base.OnUpdate(timeStep);
        }

        public void RawVideoSource_OnRawVideoFrame(
            uint width, uint height,
            byte[] yPlane, uint yPitch,
            byte[] vPlane, uint vPitch,
            byte[] uPlane, uint uPitch,
            float xPos, float yPos, float zPos,
            float xRot, float yRot, float zRot, float wRot)
        {
            Application.InvokeOnMain(() =>
            {
                _videoPlayer.OnRawVideoFrame(width, height, yPlane, yPitch, vPlane,vPitch, 
                    uPlane, uPitch);

                this.CameraNode.SetWorldPosition(new Vector3(xPos, yPos, -zPos));
                this.CameraNode.SetWorldRotation(new Quaternion(-xRot, -yRot, zRot, wRot));
            });
        }
    }

    public class VideoPlayer : Component
    {
        public Texture2D TextureY { private set; get; }
        public Texture2D TextureV { private set; get; }
        public Texture2D TextureU { private set; get; }

        public VideoPlayer()
        {
            TextureY = CreateComponentTexture();
            TextureY.SetSize(
                UrhoApplication.VIDEO_WIDTH_LOW, UrhoApplication.VIDEO_HEIGHT_LOW,
                Graphics.LuminanceFormat, TextureUsage.Rendertarget);

            TextureV = CreateComponentTexture();
            TextureV.SetSize(
                UrhoApplication.VIDEO_WIDTH_LOW / 2, UrhoApplication.VIDEO_HEIGHT_LOW / 2,
                Graphics.LuminanceFormat, TextureUsage.Rendertarget);

            TextureU = CreateComponentTexture();
            TextureU.SetSize(
                UrhoApplication.VIDEO_WIDTH_LOW / 2, UrhoApplication.VIDEO_HEIGHT_LOW / 2,
                Graphics.LuminanceFormat, TextureUsage.Rendertarget);
        }

        private Texture2D CreateComponentTexture()
        {
            Texture2D tex = new Texture2D();
            tex.FilterMode = TextureFilterMode.Nearest;
            //tex.RenderSurface.UpdateMode = RenderSurfaceUpdateMode.Updatealways;

            return tex;
        }

        public unsafe void OnRawVideoFrame(
            uint width, uint height,
            byte[] yPlane, uint yPitch,
            byte[] vPlane, uint vPitch,
            byte[] uPlane, uint uPitch)
        {
            // TODO: adjust size for pitch (stride) if it's possible for it to be non-zero, and handle stride in shader
            TextureY.SetData(0, 0, 0, (int)width, (int)height, yPlane); 
            TextureV.SetData(0, 0, 0, (int)width / 2, (int)height / 2, vPlane);
            TextureU.SetData(0, 0, 0, (int)width / 2, (int)height / 2, uPlane);
        }
    }
}
