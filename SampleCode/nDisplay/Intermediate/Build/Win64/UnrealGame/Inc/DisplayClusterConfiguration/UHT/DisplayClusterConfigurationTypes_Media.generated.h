// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

// IWYU pragma: private, include "DisplayClusterConfigurationTypes_Media.h"
#include "UObject/ObjectMacros.h"
#include "UObject/ScriptMacros.h"

PRAGMA_DISABLE_DEPRECATION_WARNINGS
#ifdef DISPLAYCLUSTERCONFIGURATION_DisplayClusterConfigurationTypes_Media_generated_h
#error "DisplayClusterConfigurationTypes_Media.generated.h already included, missing '#pragma once' in DisplayClusterConfigurationTypes_Media.h"
#endif
#define DISPLAYCLUSTERCONFIGURATION_DisplayClusterConfigurationTypes_Media_generated_h

#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterConfiguration_Public_DisplayClusterConfigurationTypes_Media_h_38_GENERATED_BODY \
	friend struct Z_Construct_UScriptStruct_FDisplayClusterConfigurationMediaInput_Statics; \
	static class UScriptStruct* StaticStruct();


template<> DISPLAYCLUSTERCONFIGURATION_API UScriptStruct* StaticStruct<struct FDisplayClusterConfigurationMediaInput>();

#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterConfiguration_Public_DisplayClusterConfigurationTypes_Media_h_53_GENERATED_BODY \
	friend struct Z_Construct_UScriptStruct_FDisplayClusterConfigurationMediaOutput_Statics; \
	static class UScriptStruct* StaticStruct();


template<> DISPLAYCLUSTERCONFIGURATION_API UScriptStruct* StaticStruct<struct FDisplayClusterConfigurationMediaOutput>();

#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterConfiguration_Public_DisplayClusterConfigurationTypes_Media_h_71_GENERATED_BODY \
	friend struct Z_Construct_UScriptStruct_FDisplayClusterConfigurationMediaNodeBackbuffer_Statics; \
	static class UScriptStruct* StaticStruct();


template<> DISPLAYCLUSTERCONFIGURATION_API UScriptStruct* StaticStruct<struct FDisplayClusterConfigurationMediaNodeBackbuffer>();

#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterConfiguration_Public_DisplayClusterConfigurationTypes_Media_h_97_GENERATED_BODY \
	friend struct Z_Construct_UScriptStruct_FDisplayClusterConfigurationMediaViewport_Statics; \
	static class UScriptStruct* StaticStruct();


template<> DISPLAYCLUSTERCONFIGURATION_API UScriptStruct* StaticStruct<struct FDisplayClusterConfigurationMediaViewport>();

#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterConfiguration_Public_DisplayClusterConfigurationTypes_Media_h_151_GENERATED_BODY \
	friend struct Z_Construct_UScriptStruct_FDisplayClusterConfigurationMediaInputGroup_Statics; \
	static class UScriptStruct* StaticStruct(); \
	typedef FDisplayClusterConfigurationMediaInput Super;


template<> DISPLAYCLUSTERCONFIGURATION_API UScriptStruct* StaticStruct<struct FDisplayClusterConfigurationMediaInputGroup>();

#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterConfiguration_Public_DisplayClusterConfigurationTypes_Media_h_167_GENERATED_BODY \
	friend struct Z_Construct_UScriptStruct_FDisplayClusterConfigurationMediaOutputGroup_Statics; \
	static class UScriptStruct* StaticStruct(); \
	typedef FDisplayClusterConfigurationMediaOutput Super;


template<> DISPLAYCLUSTERCONFIGURATION_API UScriptStruct* StaticStruct<struct FDisplayClusterConfigurationMediaOutputGroup>();

#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterConfiguration_Public_DisplayClusterConfigurationTypes_Media_h_182_GENERATED_BODY \
	friend struct Z_Construct_UScriptStruct_FDisplayClusterConfigurationMediaUniformTileInput_Statics; \
	static class UScriptStruct* StaticStruct();


template<> DISPLAYCLUSTERCONFIGURATION_API UScriptStruct* StaticStruct<struct FDisplayClusterConfigurationMediaUniformTileInput>();

#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterConfiguration_Public_DisplayClusterConfigurationTypes_Media_h_201_GENERATED_BODY \
	friend struct Z_Construct_UScriptStruct_FDisplayClusterConfigurationMediaUniformTileOutput_Statics; \
	static class UScriptStruct* StaticStruct();


template<> DISPLAYCLUSTERCONFIGURATION_API UScriptStruct* StaticStruct<struct FDisplayClusterConfigurationMediaUniformTileOutput>();

#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterConfiguration_Public_DisplayClusterConfigurationTypes_Media_h_220_GENERATED_BODY \
	friend struct Z_Construct_UScriptStruct_FDisplayClusterConfigurationMediaTiledInputGroup_Statics; \
	static class UScriptStruct* StaticStruct();


template<> DISPLAYCLUSTERCONFIGURATION_API UScriptStruct* StaticStruct<struct FDisplayClusterConfigurationMediaTiledInputGroup>();

#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterConfiguration_Public_DisplayClusterConfigurationTypes_Media_h_239_GENERATED_BODY \
	friend struct Z_Construct_UScriptStruct_FDisplayClusterConfigurationMediaTiledOutputGroup_Statics; \
	static class UScriptStruct* StaticStruct();


template<> DISPLAYCLUSTERCONFIGURATION_API UScriptStruct* StaticStruct<struct FDisplayClusterConfigurationMediaTiledOutputGroup>();

#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterConfiguration_Public_DisplayClusterConfigurationTypes_Media_h_262_GENERATED_BODY \
	friend struct Z_Construct_UScriptStruct_FDisplayClusterConfigurationMediaICVFX_Statics; \
	static class UScriptStruct* StaticStruct();


template<> DISPLAYCLUSTERCONFIGURATION_API UScriptStruct* StaticStruct<struct FDisplayClusterConfigurationMediaICVFX>();

#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterConfiguration_Public_DisplayClusterConfigurationTypes_Media_h_379_GENERATED_BODY \
	friend struct Z_Construct_UScriptStruct_FDisplayClusterConfigurationGlobalMediaSettings_Statics; \
	static class UScriptStruct* StaticStruct();


template<> DISPLAYCLUSTERCONFIGURATION_API UScriptStruct* StaticStruct<struct FDisplayClusterConfigurationGlobalMediaSettings>();

#undef CURRENT_FILE_ID
#define CURRENT_FILE_ID FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterConfiguration_Public_DisplayClusterConfigurationTypes_Media_h


#define FOREACH_ENUM_EDISPLAYCLUSTERCONFIGURATIONMEDIASPLITTYPE(op) \
	op(EDisplayClusterConfigurationMediaSplitType::FullFrame) \
	op(EDisplayClusterConfigurationMediaSplitType::UniformTiles) 

enum class EDisplayClusterConfigurationMediaSplitType : uint8;
template<> struct TIsUEnumClass<EDisplayClusterConfigurationMediaSplitType> { enum { Value = true }; };
template<> DISPLAYCLUSTERCONFIGURATION_API UEnum* StaticEnum<EDisplayClusterConfigurationMediaSplitType>();

PRAGMA_ENABLE_DEPRECATION_WARNINGS
