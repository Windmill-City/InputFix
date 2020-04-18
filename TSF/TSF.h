#define MANAGED

#ifdef MANAGED
#pragma managed
#else
#pragma once
#endif // MANAGED

#ifdef MANAGED
#define CheckHr(hr,desc) \
if (FAILED(hr))\
	throw gcnew System::Exception(desc);
#define Pin(x,type) pin_ptr<type> p_##x = &x;
#define _Handle System::IntPtr
#define ToHWND(x) (HWND)(int)x
#else
#define CheckHr(hr,desc)
#define Pin(x,type) type* p_##x = &x;
#define _Handle HWND
#define ToHWND(x) x
#endif


#include <msctf.h>
#include <olectl.h>
#include "TextEdit.h"

/**************************************************************************
   global variables and definitions
**************************************************************************/
/**************************************************************************
  TSF definitions
**************************************************************************/
#ifdef MANAGED
#define REFCLASS public ref class
#else
#define REFCLASS class __declspec(dllexport)
#endif // MANAGED

REFCLASS TSF {
    ITfThreadMgr* mgr;
    ITfDocumentMgr* DocMgr;
    ITfContextOwnerCompositionServices* services;
    ITfMessagePump* pump;
    ITfKeystrokeMgr* KeyMgr;
    ITfCategoryMgr* CategoryMgr;
    ITfDisplayAttributeMgr* DispMgr;
    ITfUIElementMgr* UIMgr;

    TfClientId id;
    ITfContext* context;

    TextEdit* edit;

    TfEditCookie EditCookie;
    DWORD attrcookie;
public:
    TSF();
    ~TSF();

    void Active();
    void Deactive();
    void CreateContext(_Handle);
    void PushContext();
    void PopContext();
    void ReleaseContext();
    void SetEnable(bool enable);
    void ClearText();
    void onTextChange();
    void onSelChange();
    void SetFocus();
    void AssociateFocus(_Handle);
    void PumpMsg(_Handle hwnd);

    void TerminateComposition();
};

