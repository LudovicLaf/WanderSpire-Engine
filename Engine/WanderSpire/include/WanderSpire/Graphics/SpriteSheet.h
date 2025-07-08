#pragma once

#include <glm/glm.hpp>

namespace WanderSpire {

    class SpriteSheet {
    public:
        SpriteSheet(int sheetWidth, int sheetHeight, int frameWidth, int frameHeight);

        glm::vec4 GetUVForFrame(int frameIndex) const;

    private:
        int m_SheetWidth;
        int m_SheetHeight;
        int m_FrameWidth;
        int m_FrameHeight;
        int m_Cols;
        int m_Rows;
    };

}
