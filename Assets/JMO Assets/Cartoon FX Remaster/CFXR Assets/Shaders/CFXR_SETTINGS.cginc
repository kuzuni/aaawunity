//--------------------------------------------------------------------------------------------------------------------------------
// Cartoon FX
// (c) 2012-2025 Jean Moreno
//--------------------------------------------------------------------------------------------------------------------------------

// Global settings for the Cartoon FX Remaster shaders

//--------------------------------------------------------------------------------------------------------------------------------


/* Uncomment this line if you want to globally disable soft particles */
#define GLOBAL_DISABLE_SOFT_PARTICLES   // aaawunity: URP 2D Renderer 는 깊이 텍스처가 없어 소프트 파티클이 이펙트를 지운다 (CFXR README Troubleshooting)

/* Change this value if you want to globally scale the HDR effects */
/* (e.g. if your bloom effect is too strong or too weak on the effects) */
#define GLOBAL_HDR_MULTIPLIER 1

/* Comment this line if you want to disable point lights for lit particles */
#define ENABLE_POINT_LIGHTS


//--------------------------------------------------------------------------------------------------------------------------------