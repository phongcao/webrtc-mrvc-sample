#pragma once

#include <collection.h>
#include <vector>

namespace UrhoUWPBackgroundRenderer
{
    public ref class BackgroundRenderer sealed
    {
    public:
        BackgroundRenderer(int width, int height);

        uint64 ConvertI420ToABGR(uint32_t width, uint32_t height,
			const Platform::Array<uint8_t>^ yPlane, uint32_t yPitch,
			const Platform::Array<uint8_t>^ uPlane, uint32_t uPitch,
			const Platform::Array<uint8_t>^ vPlane, uint32_t vPitch);

    private:
        std::vector<uint8> sRGBFrame;
    };
}
