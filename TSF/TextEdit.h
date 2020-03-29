#pragma once

#include <msctf.h>
#include <olectl.h>
#include <xstring>
/*
None of the GUIDs in TSATTRS.H are defined in a LIB, so you have to include
INITGUID.H just before the first time you include TSATTRS.H
*/
#include <initguid.h>
#include <tsattrs.h>

/**************************************************************************
   global variables and definitions
**************************************************************************/

#define EDIT_VIEW_COOKIE    0

#define TF_UNLOCKED      0x60F
#define TF_LOCKED      0x606
#define TF_GETTEXTLENGTH    0x060E
#define TF_GETTEXT          0x060D

typedef struct
{
	IUnknown* punkID;
	ITextStoreACPSink* pTextStoreACPSink;
	DWORD                   dwMask;
}ADVISE_SINK, * PADVISE_SINK;
/**************************************************************************

   TextEdit class definition

**************************************************************************/
class TextEdit : public IUnknown, public ITextStoreACP, public ITfContextOwnerCompositionSink, public ITfDisplayAttributeNotifySink, public ITfUIElementSink
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
	//Text
	std::wstring            m_string;
	int                     m_resultstart;
	BOOL                    m_isStartComp;
	WCHAR                   m_lastchar;
public:
	TfEditCookie            editcookie;
	ITfContext*             context;
	ITfProperty*            attr_prop;
	ITfCategoryMgr*			CategoryMgr;
	ITfDisplayAttributeMgr* DispMgr;

	TextEdit(HWND hWnd);
	void ClearText();
	void SetEnable(BOOL enable);
	void SetTextBoxRect(int left, int top, int right, int bottom);
	void SetCaret_X(int x);
	//HandleComposition
	void HandleComposition();

	STDMETHOD(QueryInterface)(REFIID, LPVOID*);
	STDMETHOD_(DWORD, AddRef)();
	STDMETHOD_(DWORD, Release)();
	// 通过 ITextStoreACP 继承
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

	// 通过 ITfContextOwnerCompositionSink 继承
	STDMETHODIMP OnStartComposition(ITfCompositionView* pComposition, BOOL* pfOk);
	STDMETHODIMP OnUpdateComposition(ITfCompositionView* pComposition, ITfRange* pRangeNew);
	STDMETHODIMP OnEndComposition(ITfCompositionView* pComposition);

	// 通过 ITfTransitoryExtensionSink 继承
	STDMETHODIMP OnTransitoryExtensionUpdated(ITfContext* pic, TfEditCookie ecReadOnly, ITfRange* pResultRange, ITfRange* pCompositionRange, BOOL* pfDeleteResultRange);

	// 通过 ITfDisplayAttributeNotifySink 继承
	STDMETHODIMP OnUpdateInfo(void);
private:
	//TextStoreSink
	HRESULT _ClearAdviseSink(PADVISE_SINK pAdviseSink);
	//DocLock
	BOOL _LockDocument(DWORD dwLockFlags);
	void _UnlockDocument();
	BOOL _InternalLockDocument(DWORD dwLockFlags);
	void _InternalUnlockDocument();
	BOOL _IsLocked(DWORD dwLockType);
	//Selection
	BOOL _GetCurrentSelection(void);
	HRESULT _GetText(LPWSTR* ppwsz, LPLONG pcch = NULL);

	// 通过 ITfUIElementSink 继承
	virtual HRESULT __stdcall BeginUIElement(DWORD dwUIElementId, BOOL* pbShow) override;
	virtual HRESULT __stdcall UpdateUIElement(DWORD dwUIElementId) override;
	virtual HRESULT __stdcall EndUIElement(DWORD dwUIElementId) override;
};