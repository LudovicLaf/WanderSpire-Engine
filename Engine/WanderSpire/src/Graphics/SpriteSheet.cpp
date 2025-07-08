#include "WanderSpire/Graphics/SpriteSheet.h"

namespace WanderSpire {

    SpriteSheet::SpriteSheet(int sheetWidth, int sheetHeight, int frameWidth, int frameHeight)
        : m_SheetWidth(sheetWidth), m_SheetHeight(sheetHeight),
        m_FrameWidth(frameWidth), m_FrameHeight(frameHeight) {
        m_Cols = sheetWidth / frameWidth;
        m_Rows = sheetHeight / frameHeight;
    }

    glm::vec4 SpriteSheet::GetUVForFrame(int frameIndex) const {
        int col = frameIndex % m_Cols;
        int row = frameIndex / m_Cols;

        float u0 = (col * m_FrameWidth) / static_cast<float>(m_SheetWidth);
        float v0 = (row * m_FrameHeight) / static_cast<float>(m_SheetHeight);
        float u1 = ((col + 1) * m_FrameWidth) / static_cast<float>(m_SheetWidth);
        float v1 = ((row + 1) * m_FrameHeight) / static_cast<float>(m_SheetHeight);

        return glm::vec4(u0, v0, u1, v1);
    }

}
