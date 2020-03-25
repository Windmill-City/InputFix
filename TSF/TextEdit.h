#pragma once

#include <msctf.h>
#include <olectl.h>

/**************************************************************************
   global variables and definitions
**************************************************************************/

#define EDIT_VIEW_COOKIE    0

typedef struct
{
	IUnknown* punkID;
	ITextStoreACPSink* pTextStoreACPSink;
	DWORD                   dwMask;
}ADVISE_SINK, * PADVISE_SINK;
/**************************************************************************

   TextEdit class definition

**************************************************************************/
class TextEdit : public ITextStoreACP
{
private:
	DWORD                   m_ObjRefCount;

	HWND                    m_hWnd;
	RECT                    m_rectTextBox;
	//TextStore
	LONG                    m_acpStart;
	LONG                    m_acpEnd;
	BOOL                    m_fInterimChar;
	TsActiveSelEnd          m_ActiveSelEnd;
	//TextStoreSink
	ADVISE_SINK             m_AdviseSink;
	ITextStoreACPServices*  m_pServices;
	BOOL                    m_fNotify;
	//DocLock
	BOOL                    m_fLocked;
	DWORD                   m_dwLockType;
	BOOL                    m_fPendingLockUpgrade;
	DWORD                   m_dwInternalLockType;
	//TextBox
	TS_STATUS               m_status;
	int                     m_caret_X;
	BOOL                    m_fLayoutChanged;
public:
	TextEdit(HWND hWnd);

	void SetEnable(BOOL enable);
	void SetTextBoxRect(int left, int top, int right, int bottom);
	void SetCaret_X(int x);

	STDMETHOD(QueryInterface)(REFIID, LPVOID*);
	STDMETHOD_(DWORD, AddRef)();
	STDMETHOD_(DWORD, Release)();
	// Í¨¹ý ITextStoreACP ¼Ì³Ð
	STDMETHODIMP AdviseSink(REFIID riid, IUnknown* punk, DWORD dwMask);
	STDMETHODIMP UnadviseSink(IUnknown* punk);
	STDMETHODIMP RequestLock(DWORD dwLockFlags, HRESULT* phrSession);
	STDMETHODIMP GetStatus(TS_STATUS* pdcs);
	STDMETHODIMP QueryInsert(LONG acpTestStart, LONG acpTestEnd, ULONG cch, LONG* pacpResultStart, LONG* pacpResultEnd);
	STDMETHODIMP GetSelection(ULONG ulIndex, ULONG ulCount, TS_SELECTION_ACP* pSelection, ULONG* pcFetched);
	STDMETHODIMP SetSelection(ULONG ulCount, const TS_SELECTION_ACP* pSelection);
	STDMETHODIMP GetText(LONG acpStart, LONG acpEnd, WCHAR* pchPlain, ULONG cchPlainReq, ULONG* pcchPlainRet, TS_RUNINFO* prgRunInfo, ULONG cRunInfoReq, ULONG* pcRunInfoRet, LONG* pacpNext);
	STDMETHODIMP SetText(DWORD dwFlags, LONG acpStart, LONG acpEnd, const WCHAR* pchText, ULONG cch, TS_TEXTCHANGE* pChange);
	STDMETHODIMP GetFormattedText(LONG acpStart, LONG acpEnd, IDataObject** ppDataObject);
	STDMETHODIMP GetEmbedded(LONG acpPos, REFGUID rguidService, REFIID riid, IUnknown** ppunk);
	STDMETHODIMP QueryInsertEmbedded(const GUID* pguidService, const FORMATETC* pFormatEtc, BOOL* pfInsertable);
	STDMETHODIMP InsertEmbedded(DWORD dwFlags, LONG acpStart, LONG acpEnd, IDataObject* pDataObject, TS_TEXTCHANGE* pChange);
	STDMETHODIMP InsertTextAtSelection(DWORD dwFlags, const WCHAR* pchText, ULONG cch, LONG* pacpStart, LONG* pacpEnd, TS_TEXTCHANGE* pChange);
	STDMETHODIMP InsertEmbeddedAtSelection(DWORD dwFlags, IDataObject* pDataObject, LONG* pacpStart, LONG* pacpEnd, TS_TEXTCHANGE* pChange);
	STDMETHODIMP RequestSupportedAttrs(DWORD dwFlags, ULONG cFilterAttrs, const TS_ATTRID* paFilterAttrs);
	STDMETHODIMP RequestAttrsAtPosition(LONG acpPos, ULONG cFilterAttrs, const TS_ATTRID* paFilterAttrs, DWORD dwFlags);
	STDMETHODIMP RequestAttrsTransitioningAtPosition(LONG acpPos, ULONG cFilterAttrs, const TS_ATTRID* paFilterAttrs, DWORD dwFlags);
	STDMETHODIMP FindNextAttrTransition(LONG acpStart, LONG acpHalt, ULONG cFilterAttrs, const TS_ATTRID* paFilterAttrs, DWORD dwFlags, LONG* pacpNext, BOOL* pfFound, LONG* plFoundOffset);
	STDMETHODIMP RetrieveRequestedAttrs(ULONG ulCount, TS_ATTRVAL* paAttrVals, ULONG* pcFetched);
	STDMETHODIMP GetEndACP(LONG* pacp);
	STDMETHODIMP GetActiveView(TsViewCookie* pvcView);
	STDMETHODIMP GetACPFromPoint(TsViewCookie vcView, const POINT* ptScreen, DWORD dwFlags, LONG* pacp);
	STDMETHODIMP GetTextExt(TsViewCookie vcView, LONG acpStart, LONG acpEnd, RECT* prc, BOOL* pfClipped);
	STDMETHODIMP GetScreenExt(TsViewCookie vcView, RECT* prc);
	STDMETHODIMP GetWnd(TsViewCookie vcView, HWND* phwnd);
private:
	//TextStoreSink
	HRESULT _ClearAdviseSink(PADVISE_SINK pAdviseSink);
	//DocLock
	BOOL _LockDocument(DWORD dwLockFlags);
	void _UnlockDocument();
	BOOL _InternalLockDocument(DWORD dwLockFlags);
	void _InternalUnlockDocument();
	BOOL _IsLocked(DWORD dwLockType);
};