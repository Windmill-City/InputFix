#include "TextEdit.h"

TextEdit::TextEdit(HWND hWnd)
{
    m_hWnd = hWnd;
    m_rectTextBox = { 0,0,0,0 };

    m_status.dwDynamicFlags = TS_SD_READONLY;
    m_status.dwStaticFlags = 0;
}

void TextEdit::SetEnable(BOOL enable)
{
    m_status.dwDynamicFlags = !enable;
    m_AdviseSink.pTextStoreACPSink->OnStatusChange(!enable);
}

void TextEdit::ClearText()
{
    //can't do this if someone has a lock
    if (_IsLocked(TS_LF_READ))
    {
        return;
    }

    _LockDocument(TS_LF_READWRITE);

    //empty the text in the edit control, but don't send a change notification
    BOOL    fOldNotify = m_fNotify;
    m_fNotify = FALSE;
    
    //set the selection
    ::SendMessage(m_hWnd, EM_SETSEL, m_acpStart, 0);

    m_fNotify = fOldNotify;

    //update current selection
    m_acpStart = m_acpEnd = 0;

    //notify TSF about the changes
    m_AdviseSink.pTextStoreACPSink->OnSelectionChange();

    _UnlockDocument();

    // make sure to send the OnLayoutChange notification AFTER releasing the lock
    // so clients can do something useful during the notification
    m_AdviseSink.pTextStoreACPSink->OnLayoutChange(TS_LC_CHANGE, EDIT_VIEW_COOKIE);
}

void TextEdit::SetTextBoxRect(int left, int top, int right, int bottom)
{
    m_rectTextBox.left = left;
    m_rectTextBox.top = top;
    m_rectTextBox.right = right;
    m_rectTextBox.bottom = bottom;
}

void TextEdit::SetCaret_X(int x)
{
    m_caret_X = x;
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
    *pfOk = TRUE;
    SendMessage(m_hWnd, WM_IME_STARTCOMPOSITION, 0, 0);
    return S_OK;
}

STDMETHODIMP TextEdit::OnUpdateComposition(ITfCompositionView* pComposition, ITfRange* pRangeNew)
{
    SendMessage(m_hWnd, WM_IME_COMPOSITION, 0, 0);
    return S_OK;
}

STDMETHODIMP TextEdit::OnEndComposition(ITfCompositionView* pComposition)
{
    SendMessage(m_hWnd, WM_IME_ENDCOMPOSITION,0,0);
    return S_OK;
}
