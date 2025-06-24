// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

// IWYU pragma: private, include "SwitchboardProjectSettings.h"
#include "UObject/ObjectMacros.h"
#include "UObject/ScriptMacros.h"

PRAGMA_DISABLE_DEPRECATION_WARNINGS
class USwitchboardProjectSettings;
#ifdef SWITCHBOARDEDITOR_SwitchboardProjectSettings_generated_h
#error "SwitchboardProjectSettings.generated.h already included, missing '#pragma once' in SwitchboardProjectSettings.h"
#endif
#define SWITCHBOARDEDITOR_SwitchboardProjectSettings_generated_h

#define FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardProjectSettings_h_17_RPC_WRAPPERS_NO_PURE_DECLS \
	DECLARE_FUNCTION(execGetSwitchboardProjectSettings);


#define FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardProjectSettings_h_17_INCLASS_NO_PURE_DECLS \
private: \
	static void StaticRegisterNativesUSwitchboardProjectSettings(); \
	friend struct Z_Construct_UClass_USwitchboardProjectSettings_Statics; \
public: \
	DECLARE_CLASS(USwitchboardProjectSettings, UObject, COMPILED_IN_FLAGS(0 | CLASS_Config), CASTCLASS_None, TEXT("/Script/SwitchboardEditor"), NO_API) \
	DECLARE_SERIALIZER(USwitchboardProjectSettings) \
	static const TCHAR* StaticConfigName() {return TEXT("Game");} \



#define FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardProjectSettings_h_17_ENHANCED_CONSTRUCTORS \
private: \
	/** Private move- and copy-constructors, should never be used */ \
	USwitchboardProjectSettings(USwitchboardProjectSettings&&); \
	USwitchboardProjectSettings(const USwitchboardProjectSettings&); \
public: \
	DECLARE_VTABLE_PTR_HELPER_CTOR(NO_API, USwitchboardProjectSettings); \
	DEFINE_VTABLE_PTR_HELPER_CTOR_CALLER(USwitchboardProjectSettings); \
	DEFINE_DEFAULT_CONSTRUCTOR_CALL(USwitchboardProjectSettings) \
	NO_API virtual ~USwitchboardProjectSettings();


#define FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardProjectSettings_h_14_PROLOG
#define FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardProjectSettings_h_17_GENERATED_BODY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardProjectSettings_h_17_RPC_WRAPPERS_NO_PURE_DECLS \
	FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardProjectSettings_h_17_INCLASS_NO_PURE_DECLS \
	FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardProjectSettings_h_17_ENHANCED_CONSTRUCTORS \
private: \
PRAGMA_ENABLE_DEPRECATION_WARNINGS


template<> SWITCHBOARDEDITOR_API UClass* StaticClass<class USwitchboardProjectSettings>();

#undef CURRENT_FILE_ID
#define CURRENT_FILE_ID FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardProjectSettings_h


PRAGMA_ENABLE_DEPRECATION_WARNINGS
