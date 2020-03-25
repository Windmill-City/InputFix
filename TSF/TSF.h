#pragma managed

#include <msctf.h>
#include <olectl.h>
#include "TextEdit.h"

/**************************************************************************
   global variables and definitions
**************************************************************************/
/**************************************************************************
  TSF definitions
**************************************************************************/
public ref class TSF {
    ITfThreadMgr* mgr;
    ITfDocumentMgr* DocMgr;

    TfClientId id;
    ITfContext* context;

    TextEdit* edit;

    TfEditCookie EditCookie;
public:
    TSF();
    ~TSF();

    void CreateContext(System::IntPtr);
    void PushContext();
    void PopContext();
    void ReleaseContext();
    void SetTextExt(int left, int right, int top, int bottom);
    void SetEnable(bool enable);
    void SetFocus();
    void AssociateFocus(System::IntPtr);
};

