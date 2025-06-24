// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

// IWYU pragma: private, include "SharedMemoryMediaCapture.h"
#include "UObject/ObjectMacros.h"
#include "UObject/ScriptMacros.h"

PRAGMA_DISABLE_DEPRECATION_WARNINGS
#ifdef SHAREDMEMORYMEDIA_SharedMemoryMediaCapture_generated_h
#error "SharedMemoryMediaCapture.generated.h already included, missing '#pragma once' in SharedMemoryMediaCapture.h"
#endif
#define SHAREDMEMORYMEDIA_SharedMemoryMediaCapture_generated_h

#define FID_Engine_Plugins_Runtime_nDisplay_Source_SharedMemoryMedia_Public_SharedMemoryMediaCapture_h_37_INCLASS_NO_PURE_DECLS \
private: \
	static void StaticRegisterNativesUSharedMemoryMediaCapture(); \
	friend struct Z_Construct_UClass_USharedMemoryMediaCapture_Statics; \
public: \
	DECLARE_CLASS(USharedMemoryMediaCapture, UMediaCapture, COMPILED_IN_FLAGS(0), CASTCLASS_None, TEXT("/Script/SharedMemoryMedia"), NO_API) \
	DECLARE_SERIALIZER(USharedMemoryMediaCapture)


#define FID_Engine_Plugins_Runtime_nDisplay_Source_SharedMemoryMedia_Public_SharedMemoryMediaCapture_h_37_ENHANCED_CONSTRUCTORS \
	/** Standard constructor, called after all reflected properties have been initialized */ \
	NO_API USharedMemoryMediaCapture(); \
private: \
	/** Private move- and copy-constructors, should never be used */ \
	USharedMemoryMediaCapture(USharedMemoryMediaCapture&&); \
	USharedMemoryMediaCapture(const USharedMemoryMediaCapture&); \
public: \
	DECLARE_VTABLE_PTR_HELPER_CTOR(NO_API, USharedMemoryMediaCapture); \
	DEFINE_VTABLE_PTR_HELPER_CTOR_CALLER(USharedMemoryMediaCapture); \
	DEFINE_DEFAULT_CONSTRUCTOR_CALL(USharedMemoryMediaCapture) \
	NO_API virtual ~USharedMemoryMediaCapture();


#define FID_Engine_Plugins_Runtime_nDisplay_Source_SharedMemoryMedia_Public_SharedMemoryMediaCapture_h_34_PROLOG
#define FID_Engine_Plugins_Runtime_nDisplay_Source_SharedMemoryMedia_Public_SharedMemoryMediaCapture_h_37_GENERATED_BODY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	FID_Engine_Plugins_Runtime_nDisplay_Source_SharedMemoryMedia_Public_SharedMemoryMediaCapture_h_37_INCLASS_NO_PURE_DECLS \
	FID_Engine_Plugins_Runtime_nDisplay_Source_SharedMemoryMedia_Public_SharedMemoryMediaCapture_h_37_ENHANCED_CONSTRUCTORS \
private: \
PRAGMA_ENABLE_DEPRECATION_WARNINGS


template<> SHAREDMEMORYMEDIA_API UClass* StaticClass<class USharedMemoryMediaCapture>();

#undef CURRENT_FILE_ID
#define CURRENT_FILE_ID FID_Engine_Plugins_Runtime_nDisplay_Source_SharedMemoryMedia_Public_SharedMemoryMediaCapture_h


PRAGMA_ENABLE_DEPRECATION_WARNINGS
