## WebRTC Mixed Remote View Compositor
Similar to Mixed Remote View Compositor from [MixedRealityCompanionKit](https://github.com/Microsoft/MixedRealityCompanionKit/tree/master/MixedRemoteViewCompositor), WebRTC Mixed Remote View Compositor (WebRTC MRVC) provides the ability for developers to incorporate near real-time viewing of HoloLens experiences from within a viewing application. This is achieved through low level Media Foundation components that use [WebRTC](https://github.com/webrtc-uwp) to transmit the data from the device to a remote pc viewing application. Through the Media Foundation capture pipeline, WebRTC captures and encodes the live camera data with its associated data. The data is then transmitted to the remote viewing application that will be decoded and displayed with respect to the device transformations. This sample uses [Urho](https://github.com/xamarin/urho) as the rendering engine on both desktop and HoloLens.

![webrtc-mrvc](https://user-images.githubusercontent.com/9741323/44224496-8c6dcc00-a158-11e8-8cac-c2262c0a11f9.gif)

## Prerequisites
* Windows 10 Anniversary Update
* [Visual Studio 2017](https://docs.microsoft.com/en-us/visualstudio/releasenotes/vs2017-relnotes)
* [Windows 10 SDK - 10.0.14393.795](https://developer.microsoft.com/en-us/windows/downloads/sdk-archive)

## How to build
* From your terminal, recursively clone this repo to obtain all the source code needed to build the sample: git clone --recursive https://github.com/phongcao/webrtc-mrvc-sample.git
* Run `.\setup.cmd` from the Windows command line to download and build WebRTC components.
* Open the `WebRTCMRVCSample` solution in Visual Studio, build and deploy `UrhoUWPHoloLens` project for HoloLens and `UrhoUWPDesktop` project for the desktop (the remote pc viewing application).

## How to run
* Connect both HoloLens and desktop apps to the same signaling server.
* Issue voice command `Call` on HoloLens to initialize the capturing process.
