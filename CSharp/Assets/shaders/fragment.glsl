#version 300 es

// Precision qualifiers required for OpenGL ES
precision highp float;

// Input from vertex shader
in vec2 v_TexCoord;

// Uniforms
uniform sampler2D u_Texture;
uniform vec3      u_Color;
uniform bool      u_UseTexture;

// Explicit output variable (replaces gl_FragColor)
out vec4 FragColor;

void main() {
    if (u_UseTexture) {
        FragColor = texture(u_Texture, v_TexCoord);
    } else {
        FragColor = vec4(u_Color, 1.0);
    }
}