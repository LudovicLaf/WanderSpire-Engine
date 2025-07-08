#version 300 es

// Precision qualifiers required for OpenGL ES
precision highp float;

// Per-vertex attributes
layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec2 a_TexCoord;

// Per-instance attributes (terrain only)
layout(location = 2) in vec2 a_InstancePos;
layout(location = 3) in vec2 a_InstanceUVOffset;
layout(location = 4) in vec2 a_InstanceUVSize;

// Shared uniforms
uniform mat4  u_ViewProjection;
uniform bool  u_UseInstancing;   // 1 for terrain, 0 for sprites

// Legacy (sprites only)
uniform mat4  u_Model;
uniform vec2  u_UVOffset;
uniform vec2  u_UVSize;

// Instancing helper
uniform float u_TileSize;

// Output to fragment shader
out vec2 v_TexCoord;

void main() {
    vec2 worldPos;
    vec2 uvOff;
    vec2 uvSz;
    
    if (u_UseInstancing) {
        // Terrain path - instanced rendering
        worldPos = a_InstancePos + a_Position.xy * u_TileSize;
        uvOff    = a_InstanceUVOffset;
        uvSz     = a_InstanceUVSize;
    } else {
        // Sprite path - individual quads
        worldPos = (u_Model * vec4(a_Position, 1.0)).xy;
        uvOff    = u_UVOffset;
        uvSz     = u_UVSize;
    }
    
    gl_Position = u_ViewProjection * vec4(worldPos, 0.0, 1.0);
    v_TexCoord  = uvOff + a_TexCoord * uvSz;
}