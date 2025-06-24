// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

// IWYU pragma: private, include "SwitchboardEditorSettings.h"
#include "UObject/ObjectMacros.h"
#include "UObject/ScriptMacros.h"

PRAGMA_DISABLE_DEPRECATION_WARNINGS
class USwitchboardEditorSettings;
#ifdef SWITCHBOARDEDITOR_SwitchboardEditorSettings_generated_h
#error "SwitchboardEditorSettings.generated.h already included, missing '#pragma once' in SwitchboardEditorSettings.h"
#endif
#define SWITCHBOARDEDITOR_SwitchboardEditorSettings_generated_h

#define FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardEditorSettings_h_17_RPC_WRAPPERS_NO_PURE_DECLS \
	DECLARE_FUNCTION(execGetSwitchboardEditorSettings);


#define FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardEditorSettings_h_17_INCLASS_NO_PURE_DECLS \
private: \
	static void StaticRegisterNativesUSwitchboardEditorSettings(); \
	friend struct Z_Construct_UClass_USwitchboardEditorSettings_Statics; \
public: \
	DECLARE_CLASS(USwitchboardEditorSettings, UObject, COMPILED_IN_FLAGS(0 | CLASS_Config), CASTCLASS_None, TEXT("/Script/SwitchboardEditor"), NO_API) \
	DECLARE_SERIALIZER(USwitchboardEditorSettings) \
	static const TCHAR* StaticConfigName() {return TEXT("Engine");} \



#define FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardEditorSettings_h_17_ENHANCED_CONSTRUCTORS \
private: \
	/** Private move- and copy-constructors, should never be used */ \
	USwitchboardEditorSettings(USwitchboardEditorSettings&&); \
	USwitchboardEditorSettings(const USwitchboardEditorSettings&); \
public: \
	DECLARE_VTABLE_PTR_HELPER_CTOR(NO_API, USwitchboardEditorSettings); \
	DEFINE_VTABLE_PTR_HELPER_CTOR_CALLER(USwitchboardEditorSettings); \
	DEFINE_DEFAULT_CONSTRUCTOR_CALL(USwitchboardEditorSettings) \
	NO_API virtual ~USwitchboardEditorSettings();


#define FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardEditorSettings_h_14_PROLOG
#define FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardEditorSettings_h_17_GENERATED_BODY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardEditorSettings_h_17_RPC_WRAPPERS_NO_PURE_DECLS \
	FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardEditorSettings_h_17_INCLASS_NO_PURE_DECLS \
	FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardEditorSettings_h_17_ENHANCED_CONSTRUCTORS \
private: \
PRAGMA_ENABLE_DEPRECATION_WARNINGS


template<> SWITCHBOARDEDITOR_API UClass* StaticClass<class USwitchboardEditorSettings>();

#undef CURRENT_FILE_ID
#define CURRENT_FILE_ID FID_Engine_Plugins_VirtualProduction_Switchboard_Source_SwitchboardEditor_Public_SwitchboardEditorSettings_h


PRAGMA_ENABLE_DEPRECATION_WARNINGS
