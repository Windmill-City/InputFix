#pragma once

#include <msctf.h>
#include <olectl.h>
#include <thread>

/**************************************************************************

   TfContextOwner struct definition

**************************************************************************/
struct TfContextOwner : ITfContextOwner
{
private:
    DWORD                   m_ObjRefCount;
    HWND                    hwnd;
    RECT                    TextExt;
    RECT                    ScreenExt;
public:
    TfContextOwner(HWND hwnd);
    void SetTextExt(int left, int right, int top, int bottom);
    void SetScreenExt();
    void SetScreenExt(int left, int right, int top, int bottom);
    //IUnKnown
    virtual HRESULT __stdcall QueryInterface(REFIID riid, void** ppvObject) override;
    virtual ULONG __stdcall AddRef(void) override;
    virtual ULONG __stdcall Release(void) override;
    //ITfContextOwner
    virtual HRESULT __stdcall GetACPFromPoint(const POINT* ptScreen, DWORD dwFlags, LONG* pacp) override;
    virtual HRESULT __stdcall GetTextExt(LONG acpStart, LONG acpEnd, RECT* prc, BOOL* pfClipped) override;
    virtual HRESULT __stdcall GetScreenExt(RECT* prc) override;
    virtual HRESULT __stdcall GetStatus(TF_STATUS* pdcs) override;
    virtual HRESULT __stdcall GetWnd(HWND* phwnd) override;
    virtual HRESULT __stdcall GetAttribute(REFGUID rguidAttribute, VARIANT* pvarValue) override;
};
/**************************************************************************

   ITfContextOwnerCompositionSink struct definition

**************************************************************************/
struct TfContextOwnerCompositionSink : ITfContextOwnerCompositionSink
{
private:
    DWORD                        m_ObjRefCount;
    HWND                         hwnd;
    BOOL                         enable;
    TfEditCookie                 cookie;
    BOOL                         shouldExit;
    std::thread                  msg_loop;
public:
    TfContextOwnerCompositionSink(HWND _hwnd);
    ~TfContextOwnerCompositionSink();
    void SetEnable(BOOL _enable);
    //IUnKnown
    virtual HRESULT __stdcall QueryInterface(REFIID riid, void** ppvObject) override;
    virtual ULONG __stdcall AddRef(void) override;
    virtual ULONG __stdcall Release(void) override;

    //ITfContextOwnerCompositionSink
    virtual HRESULT __stdcall OnStartComposition(ITfCompositionView* pComposition, BOOL* pfOk) override;
    virtual HRESULT __stdcall OnUpdateComposition(ITfCompositionView* pComposition, ITfRange* pRangeNew) override;
    virtual HRESULT __stdcall OnEndComposition(ITfCompositionView* pComposition) override;
};
/**************************************************************************

   TfActiveLanguageProfileNotifySink struct definition

**************************************************************************/
struct TfActiveLanguageProfileNotifySink : ITfActiveLanguageProfileNotifySink
{
private:
    DWORD                   m_ObjRefCount;
public:
    virtual HRESULT __stdcall QueryInterface(REFIID riid, void** ppvObject) override;
    virtual ULONG __stdcall AddRef(void) override;
    virtual ULONG __stdcall Release(void) override;
    virtual HRESULT __stdcall OnActivated(REFCLSID clsid, REFGUID guidProfile, BOOL fActivated) override;
};
