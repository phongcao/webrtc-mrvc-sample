#include "pch.h"

#include "BackgroundRenderer.h"
#include "libyuv.h"

using namespace Platform;
using namespace UrhoUWPBackgroundRenderer;

BackgroundRenderer::BackgroundRenderer(int width, int height)
{
    sRGBFrame.resize(width * height * 4);
}

uint64 BackgroundRenderer::ConvertI420ToABGR(
	uint64 texPtr, uint32_t width, uint32_t height, 
    const Array<uint8_t>^ yPlane, uint32_t yPitch,
	const Array<uint8_t>^ uPlane, uint32_t uPitch,
	const Array<uint8_t>^ vPlane, uint32_t vPitch)
{
    
    libyuv::I420ToABGR(
        yPlane->Data,
        yPitch,
        uPlane->Data,
        uPitch,
        vPlane->Data,
        vPitch,
        &sRGBFrame[0],
        width * 4,
        width,
        height
    );

    return (uint64)&sRGBFrame[0];
}
