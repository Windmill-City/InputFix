#include "TextEdit.h"

STDMETHODIMP TextEdit::AdviseSink(REFIID riid, IUnknown* punk, DWORD dwMask)
{
    HRESULT     hr;
    IUnknown* punkID;
    //Get the "real" IUnknown pointer. This needs to be done for comparison purposes.
    hr = punk->QueryInterface(IID_IUnknown, (LPVOID*)&punkID);
    if (FAILED(hr))
    {
        return hr;
    }

    hr = E_INVALIDARG;

    //see if this advise sink already exists
    if (punkID == m_AdviseSink.punkID)
    {
        //this is the same advise sink, so just update the advise mask
        m_AdviseSink.dwMask = dwMask;

        hr = S_OK;
    }
    else if (NULL != m_AdviseSink.punkID)
    {
        //only one advise sink is allowed at a time
        hr = CONNECT_E_ADVISELIMIT;
    }
    else if (IsEqualIID(riid, IID_ITextStoreACPSink))
    {
        //set the advise mask
        m_AdviseSink.dwMask = dwMask;

        /*
        Set the IUnknown pointer. This is used for comparison in
        UnadviseSink and future calls to this method.
        */
        m_AdviseSink.punkID = punkID;

        //AddRef this because it will get released below and it needs to be kept
        punkID->AddRef();

        //get the ITextStoreACPSink interface
        punk->QueryInterface(IID_ITextStoreACPSink, (LPVOID*)&m_AdviseSink.pTextStoreACPSink);

        //get the ITextStoreACPServices interface
        punk->QueryInterface(IID_ITextStoreACPServices, (LPVOID*)&m_pServices);

        hr = S_OK;
    }

    //this isn't needed anymore
    punkID->Release();

    return hr;
}

STDMETHODIMP TextEdit::UnadviseSink(IUnknown* punk)
{
    HRESULT     hr;
    IUnknown* punkID;

    /*
    Get the "real" IUnknown pointer. This needs to be done for comparison
    purposes.
    */
    hr = punk->QueryInterface(IID_IUnknown, (LPVOID*)&punkID);
    if (FAILED(hr))
    {
        return hr;
    }

    //find the advise sink
    if (punkID == m_AdviseSink.punkID)
    {
        //remove the advise sink from the list
        _ClearAdviseSink(&m_AdviseSink);

        if (m_pServices)
        {
            m_pServices->Release();
            m_pServices = NULL;
        }

        hr = S_OK;
    }
    else
    {
        hr = CONNECT_E_NOCONNECTION;
    }

    punkID->Release();

    return hr;
}

STDMETHODIMP TextEdit::RequestLock(DWORD dwLockFlags, HRESULT* phrSession)
{
    if (NULL == m_AdviseSink.pTextStoreACPSink)
    {
        return E_UNEXPECTED;
    }

    if (NULL == phrSession)
    {
        return E_INVALIDARG;
    }

    *phrSession = E_FAIL;

    if (m_fLocked)
    {
        //the document is locked

        if (dwLockFlags & TS_LF_SYNC)
        {
            /*
            The caller wants an immediate lock, but this cannot be granted because
            the document is already locked.
            */
            *phrSession = TS_E_SYNCHRONOUS;
            return S_OK;
        }
        else
        {
            //the request is asynchronous 

            /*
            The only type of asynchronous lock request this application
            supports while the document is locked is to upgrade from a read
            lock to a read/write lock. This scenario is referred to as a lock
            upgrade request.
            */
            if (((m_dwLockType & TS_LF_READWRITE) == TS_LF_READ) &&
                ((dwLockFlags & TS_LF_READWRITE) == TS_LF_READWRITE))
            {
                m_fPendingLockUpgrade = TRUE;

                *phrSession = TS_S_ASYNC;

                return S_OK;
            }

        }
        return E_FAIL;
    }

    //lock the document
    _LockDocument(dwLockFlags);

    //call OnLockGranted
    *phrSession = m_AdviseSink.pTextStoreACPSink->OnLockGranted(dwLockFlags);

    //unlock the document
    _UnlockDocument();

    return S_OK;
}

STDMETHODIMP TextEdit::GetStatus(TS_STATUS* pdcs)
{
    if (NULL == pdcs)
    {
        return E_INVALIDARG;
    }

    /*
    Can be zero or:
    TS_SD_READONLY  // if set, document is read only; writes will fail
    TS_SD_LOADING   // if set, document is loading, expect additional inserts
    */
    pdcs->dwDynamicFlags = m_status.dwDynamicFlags;

    /*
    Can be zero or:
    TS_SS_DISJOINTSEL   // if set, the document supports multiple selections
    TS_SS_REGIONS       // if clear, the document will never contain multiple regions
    TS_SS_TRANSITORY    // if set, the document is expected to have a short lifespan
    TS_SS_NOHIDDENTEXT  // if set, the document will never contain hidden text (for perf)
    */
    pdcs->dwStaticFlags = m_status.dwStaticFlags;

    return S_OK;
}

STDMETHODIMP TextEdit::QueryInsert(LONG acpTestStart, LONG acpTestEnd, ULONG cch, LONG* pacpResultStart, LONG* pacpResultEnd)
{
    *pacpResultStart = acpTestStart;
    *pacpResultEnd = acpTestEnd;
    return S_OK;
}

STDMETHODIMP_(HRESULT __stdcall) TextEdit::GetSelection(ULONG ulIndex, ULONG ulCount, TS_SELECTION_ACP* pSelection, ULONG* pcFetched)
{
    //verify pSelection
    if (NULL == pSelection)
    {
        return E_INVALIDARG;
    }

    //verify pcFetched
    if (NULL == pcFetched)
    {
        return E_INVALIDARG;
    }

    *pcFetched = 0;

    //does the caller have a lock
    if (!_IsLocked(TS_LF_READ))
    {
        //the caller doesn't have a lock
        return TS_E_NOLOCK;
    }

    //check the requested index
    if (TF_DEFAULT_SELECTION == ulIndex)
    {
        ulIndex = 0;
    }
    else if (ulIndex > 1)
    {
        /*
        The index is too high. This app only supports one selection.
        */
        return E_INVALIDARG;
    }

    pSelection[0].acpStart = m_acpStart;
    pSelection[0].acpEnd = m_acpEnd;

    *pcFetched = 1;

    return S_OK;
}

STDMETHODIMP TextEdit::SetSelection(ULONG ulCount, const TS_SELECTION_ACP* pSelection)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextEdit::GetText(LONG acpStart, LONG acpEnd, WCHAR* pchPlain, ULONG cchPlainReq, ULONG* pcchPlainRet, TS_RUNINFO* prgRunInfo, ULONG cRunInfoReq, ULONG* pcRunInfoRet, LONG* pacpNext)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextEdit::SetText(DWORD dwFlags, LONG acpStart, LONG acpEnd, const WCHAR* pchText, ULONG cch, TS_TEXTCHANGE* pChange)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextEdit::GetFormattedText(LONG acpStart, LONG acpEnd, IDataObject** ppDataObject)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextEdit::GetEmbedded(LONG acpPos, REFGUID rguidService, REFIID riid, IUnknown** ppunk)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextEdit::QueryInsertEmbedded(const GUID* pguidService, const FORMATETC* pFormatEtc, BOOL* pfInsertable)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextEdit::InsertEmbedded(DWORD dwFlags, LONG acpStart, LONG acpEnd, IDataObject* pDataObject, TS_TEXTCHANGE* pChange)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextEdit::InsertTextAtSelection(DWORD dwFlags, const WCHAR* pchText, ULONG cch, LONG* pacpStart, LONG* pacpEnd, TS_TEXTCHANGE* pChange)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextEdit::InsertEmbeddedAtSelection(DWORD dwFlags, IDataObject* pDataObject, LONG* pacpStart, LONG* pacpEnd, TS_TEXTCHANGE* pChange)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextEdit::RequestSupportedAttrs(DWORD dwFlags, ULONG cFilterAttrs, const TS_ATTRID* paFilterAttrs)
{
    return S_OK;
}

STDMETHODIMP TextEdit::RequestAttrsAtPosition(LONG acpPos, ULONG cFilterAttrs, const TS_ATTRID* paFilterAttrs, DWORD dwFlags)
{
    return S_OK;
}

STDMETHODIMP TextEdit::RequestAttrsTransitioningAtPosition(LONG acpPos, ULONG cFilterAttrs, const TS_ATTRID* paFilterAttrs, DWORD dwFlags)
{
    return S_OK;
}

STDMETHODIMP TextEdit::FindNextAttrTransition(LONG acpStart, LONG acpHalt, ULONG cFilterAttrs, const TS_ATTRID* paFilterAttrs, DWORD dwFlags, LONG* pacpNext, BOOL* pfFound, LONG* plFoundOffset)
{
    return S_OK;
}

STDMETHODIMP TextEdit::RetrieveRequestedAttrs(ULONG ulCount, TS_ATTRVAL* paAttrVals, ULONG* pcFetched)
{
    return S_OK;
}

STDMETHODIMP TextEdit::GetEndACP(LONG* pacp)
{
    //does the caller have a lock
    if (!_IsLocked(TS_LF_READWRITE))
    {
        //the caller doesn't have a lock
        return TS_E_NOLOCK;
    }

    if (NULL == pacp)
    {
        return E_INVALIDARG;
    }

    *pacp = m_acpEnd;
}

STDMETHODIMP TextEdit::GetActiveView(TsViewCookie* pvcView)
{
    //this app only supports one view, so this can be constant
    *pvcView = EDIT_VIEW_COOKIE;

    return S_OK;
}

STDMETHODIMP TextEdit::GetACPFromPoint(TsViewCookie vcView, const POINT* ptScreen, DWORD dwFlags, LONG* pacp)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextEdit::GetTextExt(TsViewCookie vcView, LONG acpStart, LONG acpEnd, RECT* prc, BOOL* pfClipped)
{
    if (NULL == prc || NULL == pfClipped)
    {
        return E_INVALIDARG;
    }

    *pfClipped = FALSE;
    ZeroMemory(prc, sizeof(RECT));

    if (EDIT_VIEW_COOKIE != vcView)
    {
        return E_INVALIDARG;
    }

    //does the caller have a lock
    if (!_IsLocked(TS_LF_READ))
    {
        //the caller doesn't have a lock
        return TS_E_NOLOCK;
    }

    //is this an empty request?
    if (acpStart == acpEnd)
    {
        return E_INVALIDARG;
    }
    prc->left = m_rectTextBox.left + m_caret_X;
    prc->right = m_rectTextBox.right;
    prc->top = m_rectTextBox.top;
    prc->bottom = m_rectTextBox.bottom;

    MapWindowPoints(m_hWnd, NULL, (LPPOINT)prc, 2);
	return S_OK;
}

STDMETHODIMP TextEdit::GetScreenExt(TsViewCookie vcView, RECT* prc)
{
    if (NULL == prc)
    {
        return E_INVALIDARG;
    }

    ZeroMemory(prc, sizeof(RECT));

    if (EDIT_VIEW_COOKIE != vcView)
    {
        return E_INVALIDARG;
    }

    GetWindowRect(m_hWnd, prc);
	return S_OK;
}

STDMETHODIMP TextEdit::GetWnd(TsViewCookie vcView, HWND* phwnd)
{
    if (EDIT_VIEW_COOKIE == vcView)
    {
        *phwnd = m_hWnd;

        return S_OK;
    }

    return E_INVALIDARG;
}

HRESULT TextEdit::_ClearAdviseSink(PADVISE_SINK pAdviseSink)
{
    if (pAdviseSink->punkID)
    {
        pAdviseSink->punkID->Release();
        pAdviseSink->punkID = NULL;
    }

    if (pAdviseSink->pTextStoreACPSink)
    {
        pAdviseSink->pTextStoreACPSink->Release();
        pAdviseSink->pTextStoreACPSink = NULL;
    }

    pAdviseSink->dwMask = 0;

    return S_OK;
}

BOOL TextEdit::_LockDocument(DWORD dwLockFlags)
{
    if (m_fLocked)
    {
        return FALSE;
    }

    m_fLocked = TRUE;
    m_dwLockType = dwLockFlags;

    return TRUE;
}

void TextEdit::_UnlockDocument()
{
    HRESULT hr;

    m_fLocked = FALSE;
    m_dwLockType = 0;

    //if there is a pending lock upgrade, grant it
    if (m_fPendingLockUpgrade)
    {
        m_fPendingLockUpgrade = FALSE;

        RequestLock(TS_LF_READWRITE, &hr);
    }

    //if any layout changes occurred during the lock, notify the manager
    if (m_fLayoutChanged)
    {
        m_fLayoutChanged = FALSE;
        m_AdviseSink.pTextStoreACPSink->OnLayoutChange(TS_LC_CHANGE, EDIT_VIEW_COOKIE);
    }
}

BOOL TextEdit::_InternalLockDocument(DWORD dwLockFlags)
{
    m_dwInternalLockType = dwLockFlags;

    return TRUE;
}

void TextEdit::_InternalUnlockDocument()
{
    m_dwInternalLockType = 0;
}

BOOL TextEdit::_IsLocked(DWORD dwLockType)
{
    if (m_dwInternalLockType)
    {
        return TRUE;
    }

    return m_fLocked && (m_dwLockType & dwLockType);
}
