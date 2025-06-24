// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

// IWYU pragma: private, include "DisplayClusterNetConnection.h"
#include "UObject/ObjectMacros.h"
#include "UObject/ScriptMacros.h"

PRAGMA_DISABLE_DEPRECATION_WARNINGS
#ifdef DISPLAYCLUSTERREPLICATION_DisplayClusterNetConnection_generated_h
#error "DisplayClusterNetConnection.generated.h already included, missing '#pragma once' in DisplayClusterNetConnection.h"
#endif
#define DISPLAYCLUSTERREPLICATION_DisplayClusterNetConnection_generated_h

#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterReplication_Public_DisplayClusterNetConnection_h_21_INCLASS \
private: \
	static void StaticRegisterNativesUDisplayClusterNetConnection(); \
	friend struct Z_Construct_UClass_UDisplayClusterNetConnection_Statics; \
public: \
	DECLARE_CLASS(UDisplayClusterNetConnection, UIpConnection, COMPILED_IN_FLAGS(0 | CLASS_Transient | CLASS_Config), CASTCLASS_None, TEXT("/Script/DisplayClusterReplication"), NO_API) \
	DECLARE_SERIALIZER(UDisplayClusterNetConnection)


#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterReplication_Public_DisplayClusterNetConnection_h_21_STANDARD_CONSTRUCTORS \
	/** Standard constructor, called after all reflected properties have been initialized */ \
	NO_API UDisplayClusterNetConnection(const FObjectInitializer& ObjectInitializer = FObjectInitializer::Get()); \
	DEFINE_DEFAULT_OBJECT_INITIALIZER_CONSTRUCTOR_CALL(UDisplayClusterNetConnection) \
	DECLARE_VTABLE_PTR_HELPER_CTOR(NO_API, UDisplayClusterNetConnection); \
	DEFINE_VTABLE_PTR_HELPER_CTOR_CALLER(UDisplayClusterNetConnection); \
private: \
	/** Private move- and copy-constructors, should never be used */ \
	UDisplayClusterNetConnection(UDisplayClusterNetConnection&&); \
	UDisplayClusterNetConnection(const UDisplayClusterNetConnection&); \
public: \
	NO_API virtual ~UDisplayClusterNetConnection();


#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterReplication_Public_DisplayClusterNetConnection_h_18_PROLOG
#define FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterReplication_Public_DisplayClusterNetConnection_h_21_GENERATED_BODY_LEGACY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterReplication_Public_DisplayClusterNetConnection_h_21_INCLASS \
	FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterReplication_Public_DisplayClusterNetConnection_h_21_STANDARD_CONSTRUCTORS \
public: \
PRAGMA_ENABLE_DEPRECATION_WARNINGS


template<> DISPLAYCLUSTERREPLICATION_API UClass* StaticClass<class UDisplayClusterNetConnection>();

#undef CURRENT_FILE_ID
#define CURRENT_FILE_ID FID_Engine_Plugins_Runtime_nDisplay_Source_DisplayClusterReplication_Public_DisplayClusterNetConnection_h


PRAGMA_ENABLE_DEPRECATION_WARNINGS
