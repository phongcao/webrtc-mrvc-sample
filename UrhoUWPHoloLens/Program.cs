using System;
using System.Collections.Generic;
using Urho;
using Urho.Shapes;
using Urho.SharpReality;
using Windows.ApplicationModel.Core;

namespace UrhoUWPHoloLens
{
    public class HoloApplication : StereoApplication
    {
        private CallViewModel _callViewModel;

        private Node _earthNode;
        private Vector3 _earthPosBeforManipulations;
        private Material _earthMaterial;
        private float _cloudsOffset;

        public HoloApplication(ApplicationOptions opts) : base(opts) { }

        protected override async void Start()
        {
            // Create a basic scene, see StereoApplication
            base.Start();

            _callViewModel = new CallViewModel(
                CoreApplication.GetCurrentView().Dispatcher);

            // Enable input
            EnableGestureManipulation = true;
            EnableGestureTapped = true;

            // Create a node for the Earth
            _earthNode = Scene.CreateChild();
            _earthNode.Position = new Vector3(0, 0, 1.5f);
            _earthNode.SetScale(0.3f);

            DirectionalLight.Brightness = 1f;
            DirectionalLight.Node.SetDirection(new Vector3(-1, 0, 0.5f));

            var earth = _earthNode.CreateComponent<Sphere>();
            _earthMaterial = ResourceCache.GetMaterial("Materials/Earth.xml");
            earth.SetMaterial(_earthMaterial);

            var moonNode = _earthNode.CreateChild();
            moonNode.SetScale(0.27f);
            moonNode.Position = new Vector3(1.2f, 0, 0);

            var moon = moonNode.CreateComponent<Sphere>();
            moon.SetMaterial(ResourceCache.GetMaterial("Materials/Moon.xml"));

            // Register Cortana commands
            await RegisterCortanaCommands(new Dictionary<string, Action>
            {
				// Play animations using Cortana
                {"Call", () => CallPeer()}
            });

            // Run a few actions to spin the Earth, the Moon and the clouds.
            await TextToSpeech("Hello world from UrhoSharp!");
        }

        private void CallPeer()
        {
            _callViewModel.CallFirstPeer();
        }

        protected override void OnUpdate(float timeStep)
        {
            // Move clouds via CloudsOffset (defined in the material.xml and used in the PS)
            _cloudsOffset += 0.00005f;
            _earthMaterial.SetShaderParameter("CloudsOffset",
                new Vector2(_cloudsOffset, 0));
            //NOTE: this could be done via SetShaderParameterAnimation
        }

        // For HL optical stabilization (optional)
        public override Vector3 FocusWorldPoint => _earthNode.WorldPosition;

        // Handle input
        public override void OnGestureManipulationStarted()
        {
            _earthPosBeforManipulations = _earthNode.Position;
        }

        public override void OnGestureManipulationUpdated(Vector3 relativeHandPosition)
        {
            _earthNode.Position = relativeHandPosition + _earthPosBeforManipulations;
        }

        public override void OnGestureDoubleTapped()
        {
            _earthNode.Scale *= 1.2f;
        }
    }
}
