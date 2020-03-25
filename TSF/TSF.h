//#define MANAGED

#ifdef MANAGED
#pragma managed
#else
#pragma once
#endif // MANAGED

#ifdef MANAGED
#define CheckHr(hr,desc) \
if (FAILED(hr))\
	throw gcnew System::Exception(desc);
#define Pin(x,type) (pin_ptr<type>)x
#define Handle System::IntPtr
#define ToHWND(x) (HWND)(int)x
#else
#define CheckHr(hr,desc)
#define Pin(x,type) x
#define Handle HWND
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

    TfClientId id;
    ITfContext* context;

    TextEdit* edit;

    TfEditCookie EditCookie;
public:
    TSF();
    ~TSF();

    void CreateContext(Handle);
    void PushContext();
    void PopContext();
    void ReleaseContext();
    void SetTextExt(int left, int right, int top, int bottom);
    void SetEnable(bool enable);
    void SetFocus();
    void AssociateFocus(Handle);
};

