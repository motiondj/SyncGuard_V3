// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

// IWYU pragma: private, include "DisplayClusterLightCardActor.h"
#include "UObject/ObjectMacros.h"
#include "UObject/ScriptMacros.h"

PRAGMA_DISABLE_DEPRECATION_WARNINGS
#ifdef DISPLAYCLUSTER_DisplayClusterLightCardActor_generated_h
#error "DisplayClusterLightCardActor.generated.h already included, missing '#pragma once' in DisplayClusterLightCardActor.h"
#endif
#define DISPLAYCLUSTER_DisplayClusterLightCardActor_generated_h

#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_DisplayClusterLightCardActor_h_38_GENERATED_BODY \
	friend struct Z_Construct_UScriptStruct_FLightCardAlphaGradientSettings_Statics; \
	DISPLAYCLUSTER_API static class UScriptStruct* StaticStruct();


template<> DISPLAYCLUSTER_API UScriptStruct* StaticStruct<struct FLightCardAlphaGradientSettings>();

#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_DisplayClusterLightCardActor_h_60_ACCESSORS \
static void GetbIsUVLightCard_WrapperImpl(const void* Object, void* OutValue); \
static void SetbIsUVLightCard_WrapperImpl(void* Object, const void* InValue); \
static void GetbIsLightCardFlag_WrapperImpl(const void* Object, void* OutValue); \
static void SetbIsLightCardFlag_WrapperImpl(void* Object, const void* InValue);


#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_DisplayClusterLightCardActor_h_60_INCLASS_NO_PURE_DECLS \
private: \
	static void StaticRegisterNativesADisplayClusterLightCardActor(); \
	friend struct Z_Construct_UClass_ADisplayClusterLightCardActor_Statics; \
public: \
	DECLARE_CLASS(ADisplayClusterLightCardActor, AActor, COMPILED_IN_FLAGS(0 | CLASS_Config), CASTCLASS_None, TEXT("/Script/DisplayCluster"), NO_API) \
	DECLARE_SERIALIZER(ADisplayClusterLightCardActor) \
	virtual UObject* _getUObject() const override { return const_cast<ADisplayClusterLightCardActor*>(this); }


#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_DisplayClusterLightCardActor_h_60_ENHANCED_CONSTRUCTORS \
private: \
	/** Private move- and copy-constructors, should never be used */ \
	ADisplayClusterLightCardActor(ADisplayClusterLightCardActor&&); \
	ADisplayClusterLightCardActor(const ADisplayClusterLightCardActor&); \
public: \
	DECLARE_VTABLE_PTR_HELPER_CTOR(NO_API, ADisplayClusterLightCardActor); \
	DEFINE_VTABLE_PTR_HELPER_CTOR_CALLER(ADisplayClusterLightCardActor); \
	DEFINE_DEFAULT_OBJECT_INITIALIZER_CONSTRUCTOR_CALL(ADisplayClusterLightCardActor)


#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_DisplayClusterLightCardActor_h_57_PROLOG
#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_DisplayClusterLightCardActor_h_60_GENERATED_BODY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_DisplayClusterLightCardActor_h_60_ACCESSORS \
	FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_DisplayClusterLightCardActor_h_60_INCLASS_NO_PURE_DECLS \
	FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_DisplayClusterLightCardActor_h_60_ENHANCED_CONSTRUCTORS \
private: \
PRAGMA_ENABLE_DEPRECATION_WARNINGS


template<> DISPLAYCLUSTER_API UClass* StaticClass<class ADisplayClusterLightCardActor>();

#undef CURRENT_FILE_ID
#define CURRENT_FILE_ID FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_DisplayClusterLightCardActor_h


#define FOREACH_ENUM_EDISPLAYCLUSTERLIGHTCARDMASK(op) \
	op(EDisplayClusterLightCardMask::Circle) \
	op(EDisplayClusterLightCardMask::Square) \
	op(EDisplayClusterLightCardMask::UseTextureAlpha) \
	op(EDisplayClusterLightCardMask::Polygon) 

enum class EDisplayClusterLightCardMask : uint8;
template<> struct TIsUEnumClass<EDisplayClusterLightCardMask> { enum { Value = true }; };
template<> DISPLAYCLUSTER_API UEnum* StaticEnum<EDisplayClusterLightCardMask>();

PRAGMA_ENABLE_DEPRECATION_WARNINGS
