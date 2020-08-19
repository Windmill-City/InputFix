#include "TextEdit.h"

TextEdit::TextEdit(HWND hWnd)
{
	m_hWnd = hWnd;

	m_status.dwDynamicFlags = TS_SD_READONLY;
	m_status.dwStaticFlags = 0;
}

void TextEdit::SetEnable(BOOL enable)
{
	m_status.dwDynamicFlags = !enable;
	m_AdviseSink.pTextStoreACPSink->OnStatusChange(!enable);
}

BOOL TextEdit::ClearText()
{
	//can't do this if someone has a lock
	if (_IsLocked(TS_LF_READ))
	{
		return FALSE;
	}

	_LockDocument(TS_LF_READWRITE);

	//empty the text in the edit control, but don't send a change notification
	BOOL    fOldNotify = m_fNotify;
	m_fNotify = FALSE;
	SendMessage(m_hWnd, TF_CLEARTEXT, 0, 0);
	m_fNotify = fOldNotify;

	//update current selection
	m_acpStart = m_acpEnd = 0;

	//notify TSF about the changes
	m_AdviseSink.pTextStoreACPSink->OnSelectionChange();

	_UnlockDocument();

	// make sure to send the OnLayoutChange notification AFTER releasing the lock
	// so clients can do something useful during the notification
	m_AdviseSink.pTextStoreACPSink->OnLayoutChange(TS_LC_CHANGE, EDIT_VIEW_COOKIE);

	return TRUE;
}

void TextEdit::onTextChange()
{
	if (m_fNotify && m_AdviseSink.pTextStoreACPSink && (m_AdviseSink.dwMask & TS_AS_TEXT_CHANGE))
	{
		DWORD           dwFlags;
		TS_TEXTCHANGE   tc;
		ULONG           cch;

		cch = _GetTextLength();

		/*
		dwFlags can be 0 or TS_TC_CORRECTION
		*/
		dwFlags = 0;

		tc.acpStart = 0;
		tc.acpOldEnd = m_cchOldLength;
		tc.acpNewEnd = cch;

		m_AdviseSink.pTextStoreACPSink->OnTextChange(dwFlags, &tc);

		m_cchOldLength = cch;
	}
}

void TextEdit::onSelChange()
{
	if (m_fNotify && m_AdviseSink.pTextStoreACPSink && (m_AdviseSink.dwMask & TS_AS_TEXT_CHANGE))
		m_AdviseSink.pTextStoreACPSink->OnSelectionChange();
}

STDMETHODIMP_(STDMETHODIMP)  TextEdit::QueryInterface(REFIID riid, void** ppvObject)
{
	*ppvObject = NULL;

	//IUnknown
	if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_ITextStoreACP))
	{
		*ppvObject = (ITextStoreACP*)this;
	}
	else if (IsEqualIID(riid, IID_ITfContextOwnerCompositionSink))
	{
		*ppvObject = (ITfContextOwnerCompositionSink*)this;
	}
	else if (IsEqualIID(riid, IID_ITfTransitoryExtensionSink))
	{
		*ppvObject = (ITfTransitoryExtensionSink*)this;
	}
	else if (IsEqualIID(riid, IID_ITfUIElementSink))
	{
		*ppvObject = (ITfUIElementSink*)this;
	}

	if (*ppvObject)
	{
		(*(LPUNKNOWN*)ppvObject)->AddRef();
		return S_OK;
	}

	return E_NOINTERFACE;
}

STDMETHODIMP_(DWORD) TextEdit::AddRef(void)
{
	return ++m_ObjRefCount;
}

STDMETHODIMP_(DWORD) TextEdit::Release(void)
{
	if (--m_ObjRefCount == 0)
	{
		delete this;
		return 0;
	}

	return m_ObjRefCount;
}

STDMETHODIMP TextEdit::OnStartComposition(ITfCompositionView* pComposition, BOOL* pfOk)
{
	OutputDebugString(TEXT("OnStartComposition\n"));
	*pfOk = TRUE;
	SendMessage(m_hWnd, WM_IME_STARTCOMPOSITION, 0, 0);
	return S_OK;
}

STDMETHODIMP TextEdit::OnUpdateComposition(ITfCompositionView* pComposition, ITfRange* pRangeNew)
{
	OutputDebugString(TEXT("OnUpdateComposition\n"));
	SendMessage(m_hWnd, WM_IME_COMPOSITION, 0, 0);
	return S_OK;
}

STDMETHODIMP TextEdit::OnEndComposition(ITfCompositionView* pComposition)
{
	OutputDebugString(TEXT("OnEndComposition\n"));
	SendMessage(m_hWnd, WM_IME_ENDCOMPOSITION, 0, 0);
	return S_OK;
}

HRESULT __stdcall TextEdit::BeginUIElement(DWORD dwUIElementId, BOOL* pbShow)
{
	*pbShow = TRUE;
	return S_OK;
}

HRESULT __stdcall TextEdit::UpdateUIElement(DWORD dwUIElementId)
{
	return S_OK;
}

HRESULT __stdcall TextEdit::EndUIElement(DWORD dwUIElementId)
{
	return S_OK;
}

STDMETHODIMP TextEdit::OnUpdateInfo(void)
{
	return S_OK;
}

STDMETHODIMP TextEdit::OnTransitoryExtensionUpdated(ITfContext* pic, TfEditCookie ecReadOnly, ITfRange* pResultRange, ITfRange* pCompositionRange, BOOL* pfDeleteResultRange)
{
	return S_OK;
}