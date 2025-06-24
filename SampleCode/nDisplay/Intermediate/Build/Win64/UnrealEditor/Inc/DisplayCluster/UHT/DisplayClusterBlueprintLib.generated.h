// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

// IWYU pragma: private, include "Blueprints/DisplayClusterBlueprintLib.h"
#include "UObject/ObjectMacros.h"
#include "UObject/ScriptMacros.h"

PRAGMA_DISABLE_DEPRECATION_WARNINGS
class ADisplayClusterChromakeyCardActor;
class ADisplayClusterLightCardActor;
class ADisplayClusterRootActor;
class IDisplayClusterBlueprintAPI;
class IDisplayClusterClusterEventListener;
enum class EDisplayClusterNodeRole : uint8;
enum class EDisplayClusterOperationMode : uint8;
struct FDisplayClusterClusterEventBinary;
struct FDisplayClusterClusterEventJson;
#ifdef DISPLAYCLUSTER_DisplayClusterBlueprintLib_generated_h
#error "DisplayClusterBlueprintLib.generated.h already included, missing '#pragma once' in DisplayClusterBlueprintLib.h"
#endif
#define DISPLAYCLUSTER_DisplayClusterBlueprintLib_generated_h

#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_Blueprints_DisplayClusterBlueprintLib_h_26_RPC_WRAPPERS_NO_PURE_DECLS \
	DECLARE_FUNCTION(execFindChromakeyCardsForRootActor); \
	DECLARE_FUNCTION(execFindLightCardsForRootActor); \
	DECLARE_FUNCTION(execDuplicateLightCards); \
	DECLARE_FUNCTION(execCreateLightCard); \
	DECLARE_FUNCTION(execSendClusterEventBinaryTo); \
	DECLARE_FUNCTION(execSendClusterEventJsonTo); \
	DECLARE_FUNCTION(execEmitClusterEventBinary); \
	DECLARE_FUNCTION(execEmitClusterEventJson); \
	DECLARE_FUNCTION(execRemoveClusterEventListener); \
	DECLARE_FUNCTION(execAddClusterEventListener); \
	DECLARE_FUNCTION(execGetClusterRole); \
	DECLARE_FUNCTION(execIsBackup); \
	DECLARE_FUNCTION(execIsSecondary); \
	DECLARE_FUNCTION(execIsPrimary); \
	DECLARE_FUNCTION(execGetActiveNodesAmount); \
	DECLARE_FUNCTION(execGetActiveNodeIds); \
	DECLARE_FUNCTION(execGetNodeId); \
	DECLARE_FUNCTION(execGetRootActor); \
	DECLARE_FUNCTION(execGetOperationMode); \
	DECLARE_FUNCTION(execGetAPI);


#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_Blueprints_DisplayClusterBlueprintLib_h_26_INCLASS_NO_PURE_DECLS \
private: \
	static void StaticRegisterNativesUDisplayClusterBlueprintLib(); \
	friend struct Z_Construct_UClass_UDisplayClusterBlueprintLib_Statics; \
public: \
	DECLARE_CLASS(UDisplayClusterBlueprintLib, UBlueprintFunctionLibrary, COMPILED_IN_FLAGS(0), CASTCLASS_None, TEXT("/Script/DisplayCluster"), NO_API) \
	DECLARE_SERIALIZER(UDisplayClusterBlueprintLib)


#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_Blueprints_DisplayClusterBlueprintLib_h_26_ENHANCED_CONSTRUCTORS \
	/** Standard constructor, called after all reflected properties have been initialized */ \
	NO_API UDisplayClusterBlueprintLib(const FObjectInitializer& ObjectInitializer = FObjectInitializer::Get()); \
private: \
	/** Private move- and copy-constructors, should never be used */ \
	UDisplayClusterBlueprintLib(UDisplayClusterBlueprintLib&&); \
	UDisplayClusterBlueprintLib(const UDisplayClusterBlueprintLib&); \
public: \
	DECLARE_VTABLE_PTR_HELPER_CTOR(NO_API, UDisplayClusterBlueprintLib); \
	DEFINE_VTABLE_PTR_HELPER_CTOR_CALLER(UDisplayClusterBlueprintLib); \
	DEFINE_DEFAULT_OBJECT_INITIALIZER_CONSTRUCTOR_CALL(UDisplayClusterBlueprintLib) \
	NO_API virtual ~UDisplayClusterBlueprintLib();


#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_Blueprints_DisplayClusterBlueprintLib_h_22_PROLOG
#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_Blueprints_DisplayClusterBlueprintLib_h_26_GENERATED_BODY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_Blueprints_DisplayClusterBlueprintLib_h_26_RPC_WRAPPERS_NO_PURE_DECLS \
	FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_Blueprints_DisplayClusterBlueprintLib_h_26_INCLASS_NO_PURE_DECLS \
	FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_Blueprints_DisplayClusterBlueprintLib_h_26_ENHANCED_CONSTRUCTORS \
private: \
PRAGMA_ENABLE_DEPRECATION_WARNINGS


template<> DISPLAYCLUSTER_API UClass* StaticClass<class UDisplayClusterBlueprintLib>();

#undef CURRENT_FILE_ID
#define CURRENT_FILE_ID FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayCluster_Public_Blueprints_DisplayClusterBlueprintLib_h


PRAGMA_ENABLE_DEPRECATION_WARNINGS
