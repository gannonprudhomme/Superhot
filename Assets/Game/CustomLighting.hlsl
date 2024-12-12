// Retrieved from: https://github.com/UnityTechnologies/open-project-1/tree/devlogs/1-toon-shading
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow"
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDShadowAlgorithms.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightEvaluation.hlsl"

#ifndef CUSTOM_LIGHTING_INCLUDED // idk what this does
#define CUSTOM_LIGHTING_INCLUDED

#define _MAIN_LIGHT_SHADOWS 1 // TODO: Don't actually do this

void MainLight_float(
    float3 WorldPos,
    float3 WorldNormal,
    float3 WorldView,
    float Smoothness,

    out float3 Direction,
    out float3 Color,
    out float DistanceAtten,

    // TODO: It is apparently really hard to get shadow attenuation in HDRP
    // why the fuck
    // it's apparently possible with forward rendering, though idk if I can use that
    // if I have to go back to URP I swear to god
    out float ShadowAtten,

    out float3 Specular
) {
#ifdef SHADERGRAPH_PREVIEW
    Direction = float3(0.5, 0.5, 0);
    Color = 1;
    DistanceAtten = 1;
    ShadowAtten = 1;
    Specular = float3(1, 1, 1);
#else
    // I need to find an HDRP alternative for this
    // otherwise idk what'll happen
	// float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    float4 shadowCoord = float4(1, 1, 1, 0);
    Smoothness = exp2(10 * Smoothness + 1);

    // I guess we can do something with this?
    HDDirectionalShadowData thing = _HDDirectionalShadowData[0];

    float4 mainLight = _DirectionalLightDatas[0]; // this is suppposed to be a thing but I can't find it
    Direction = mainLight.direction;
    Color = mainLight.color;
    DistanceAtten = mainLight.distanceAttenuation;

	#if !defined(_MAIN_LIGHT_SHADOWS) || defined(_RECEIVE_SHADOWS_OFF)
		ShadowAtten = 1.0h;
	#else
        // ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
        HDDirectionalShadowData shadowSamplingData = _HDDirectionalShadowData[0];
        float shadowStrength = GetMainLightShadowStrength();
        ShadowAtten = SampleShadowmap(
            shadowCoord,
            TEXTURE2D_ARGS(
                _MainLightShadowmapTexture,
                sampler_MainLightShadowmapTexture
            ),
            shadowSamplingData,
            shadowStrength,
            false
        );

        float3 radiance = mainLight.color * (mainLight.distanceAttenuation * ShadowAtten);
        Specular = LightingSpecular(
            radiance,
            mainLight.direction,
            WorldNormal,
            WorldView,
            float4(1, 1, 1, 0),
            Smoothness
        );
    #endif
#endif
}

void AdditionalLights_float(
    float3 SpecColor,
    float Smoothness,
    float3 WorldPosition,
    float3 WorldNormal,
    float3 WorldView,

    out float3 Diffuse,
    out float3 Specular
) {
    float3 diffuseColor = 0;
    float3 specularColor = 0;
#ifndef SHADERGRAPH_PREVIEW
    Smoothness = exp2(10 * Smoothness + 1);
    uint pixelLightCount = GetAdditionalLightsCount();
	// LIGHT_LOOP_BEGIN(pixelLightCount) {
    for(uint lightIndex = 0; lightIndex < pixelLightCount; lightIndex++) {
		Light light = GetAdditionalLight(lightIndex, WorldPosition, 1);
        // Blinn-Phong
        float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
        diffuseColor += LightingLambert(
            attenuatedLightColor,
            light.direction,
            WorldNormal
        );
        specularColor += LightingSpecular(
            attenuatedLightColor,
            light.direction,
            WorldNormal,
            WorldView,
            float4(SpecColor, 0),
            Smoothness
        );
    } //LIGHT_LOOP_END
#endif

	Diffuse = diffuseColor;
	Specular = specularColor;
}

#endif

