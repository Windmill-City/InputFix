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
    ACP* Acp = new ACP();
    Acp->acpEnd = acpTestEnd;
    Acp->acpStart = acpTestStart;

    m_fNotify = false;

    SendMessage(m_hWnd, TF_QUERYINSERT, (WPARAM)Acp, (LPARAM)cch);

    m_fNotify = true;

    if(Acp->acpStart == -1 || Acp->acpEnd == -1)
        return S_OK;

    *pacpResultStart = Acp->acpStart;
    *pacpResultEnd = Acp->acpEnd;
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

    _GetCurrentSelection();

    //if the caret position is the same as the start character, then the selection end is the start of the selection
    m_ActiveSelEnd = m_acpStart == m_acpEnd ? TS_AE_NONE : m_acpStart < m_acpEnd ? TS_AE_START : TS_AE_END;

    pSelection[0].acpStart = m_acpStart;
    pSelection[0].acpEnd = m_acpEnd;
    pSelection[0].style.fInterimChar = m_fInterimChar;
    if (m_fInterimChar)
    {
        /*
        fInterimChar will be set when an intermediate character has been
        set. One example of when this will happen is when an IME is being
        used to enter characters and a character has been set, but the IME
        is still active.
        */
        pSelection[0].style.ase = TS_AE_NONE;
    }
    else
    {
        pSelection[0].style.ase = m_ActiveSelEnd;
    }

    *pcFetched = 1;

    return S_OK;
}

STDMETHODIMP TextEdit::SetSelection(ULONG ulCount, const TS_SELECTION_ACP* pSelection)
{
    //verify pSelection
    if (NULL == pSelection)
    {
        return E_INVALIDARG;
    }

    if (ulCount > 1)
    {
        //this implementaiton only supports a single selection
        return E_INVALIDARG;
    }

    //does the caller have a lock
    if (!_IsLocked(TS_LF_READWRITE))
    {
        //the caller doesn't have a lock
        return TS_E_NOLOCK;
    }

    m_acpStart = pSelection[0].acpStart;
    m_acpEnd = pSelection[0].acpEnd;
    m_fInterimChar = pSelection[0].style.fInterimChar;
    if (m_fInterimChar)
    {
        /*
        fInterimChar will be set when an intermediate character has been
        set. One example of when this will happen is when an IME is being
        used to enter characters and a character has been set, but the IME
        is still active.
        */
        m_ActiveSelEnd = TS_AE_NONE;
    }
    else
    {
        m_ActiveSelEnd = pSelection[0].style.ase;
    }

    //if the selection end is at the start of the selection, reverse the parameters
    LONG    lStart = m_acpStart;
    LONG    lEnd = m_acpEnd;

    if (TS_AE_START == m_ActiveSelEnd)
    {
        lStart = m_acpEnd;
        lEnd = m_acpStart;
    }

    m_fNotify = FALSE;

    ::SendMessage(m_hWnd, EM_SETSEL, lStart, lEnd);

    m_fNotify = TRUE;

    return S_OK;
}

STDMETHODIMP TextEdit::GetText(LONG acpStart, LONG acpEnd, WCHAR* pchPlain, ULONG cchPlainReq, ULONG* pcchPlainRet, TS_RUNINFO* prgRunInfo, ULONG cRunInfoReq, ULONG* pcRunInfoRet, LONG* pacpNext)
{
    //does the caller have a lock
    if (!_IsLocked(TS_LF_READ))
    {
        //the caller doesn't have a lock
        return TS_E_NOLOCK;
    }

    BOOL    fDoText = cchPlainReq > 0;
    BOOL    fDoRunInfo = cRunInfoReq > 0;
    LONG    cchTotal;
    HRESULT hr = E_FAIL;

    if (pcchPlainRet)
    {
        *pcchPlainRet = 0;
    }

    if (fDoRunInfo)
    {
        *pcchPlainRet = 0;
    }

    if (pacpNext)
    {
        *pacpNext = acpStart;
    }

    //get all of the text
    LPWSTR  pwszText;
    hr = _GetText(&pwszText, &cchTotal);
    if (FAILED(hr))
    {
        return hr;
    }

    //validate the start pos
    if ((acpStart < 0) || (acpStart > cchTotal))
    {
        hr = TS_E_INVALIDPOS;
    }
    else
    {
        //are we at the end of the document
        if (acpStart == cchTotal)
        {
            hr = S_OK;
        }
        else
        {
            ULONG    cchReq;

            /*
            acpEnd will be -1 if all of the text up to the end is being requested.
            */

            if (acpEnd >= acpStart)
            {
                cchReq = acpEnd - acpStart;
            }
            else
            {
                cchReq = cchTotal - acpStart;
            }

            if (fDoText)
            {
                if (cchReq > cchPlainReq)
                {
                    cchReq = cchPlainReq;
                }

                //extract the specified text range
                LPWSTR  pwszStart = pwszText + acpStart;

                if (pchPlain && cchPlainReq)
                {
                    //the text output is not NULL terminated
                    CopyMemory(pchPlain, pwszStart, cchReq * sizeof(WCHAR));
                }
            }

            //it is possible that only the length of the text is being requested
            if (pcchPlainRet)
            {
                *pcchPlainRet = cchReq;
            }

            if (fDoRunInfo)
            {
                /*
                Runs are used to separate text characters from formatting characters.

                In this example, sequences inside and including the <> are treated as
                control sequences and are not displayed.

                Plain text = "Text formatting."
                Actual text = "Text <B><I>formatting</I></B>."

                If all of this text were requested, the run sequence would look like this:

                prgRunInfo[0].type = TS_RT_PLAIN;   //"Text "
                prgRunInfo[0].uCount = 5;

                prgRunInfo[1].type = TS_RT_HIDDEN;  //<B><I>
                prgRunInfo[1].uCount = 6;

                prgRunInfo[2].type = TS_RT_PLAIN;   //"formatting"
                prgRunInfo[2].uCount = 10;

                prgRunInfo[3].type = TS_RT_HIDDEN;  //</B></I>
                prgRunInfo[3].uCount = 8;

                prgRunInfo[4].type = TS_RT_PLAIN;   //"."
                prgRunInfo[4].uCount = 1;

                TS_RT_OPAQUE is used to indicate characters or character sequences
                that are in the document, but are used privately by the application
                and do not map to text.  Runs of text tagged with TS_RT_OPAQUE should
                NOT be included in the pchPlain or cchPlainOut [out] parameters.
                */

                /*
                This implementation is plain text, so the text only consists of one run.
                If there were multiple runs, it would be an error to have consecuative runs
                of the same type.
                */
                * pcchPlainRet = 1;
                prgRunInfo[0].type = TS_RT_PLAIN;
                prgRunInfo[0].uCount = cchReq;
            }

            if (pacpNext)
            {
                *pacpNext = acpStart + cchReq;
            }

            hr = S_OK;
        }
    }

    GlobalFree(pwszText);

    return hr;
}

STDMETHODIMP TextEdit::SetText(DWORD dwFlags, LONG acpStart, LONG acpEnd, const WCHAR* pchText, ULONG cch, TS_TEXTCHANGE* pChange)
{
    HRESULT hr;

    /*
    dwFlags can be:
    TS_ST_CORRECTION
    */

    if (dwFlags & TS_ST_CORRECTION)
    {
        OutputDebugString(TEXT("\tTS_ST_CORRECTION\n"));
    }

    //set the selection to the specified range
    TS_SELECTION_ACP    tsa;
    tsa.acpStart = acpStart;
    tsa.acpEnd = acpEnd;
    tsa.style.ase = TS_AE_START;
    tsa.style.fInterimChar = FALSE;

    hr = SetSelection(1, &tsa);

    if (SUCCEEDED(hr))
    {
        //call InsertTextAtSelection
        hr = InsertTextAtSelection(TS_IAS_NOQUERY, pchText, cch, NULL, NULL, pChange);
    }
    return hr;
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
    LONG    lTemp;

    //does the caller have a lock
    if (!_IsLocked(TS_LF_READWRITE))
    {
        //the caller doesn't have a lock
        return TS_E_NOLOCK;
    }

    //verify pwszText
    if (NULL == pchText)
    {
        return E_INVALIDARG;
    }

    //verify pacpStart
    if (NULL == pacpStart)
    {
        pacpStart = &lTemp;
    }

    //verify pacpEnd
    if (NULL == pacpEnd)
    {
        pacpEnd = &lTemp;
    }

    LONG    acpStart;
    LONG    acpOldEnd;
    LONG    acpNewEnd;

    acpOldEnd = m_acpEnd;

    //set the start point after the insertion
    acpStart = m_acpStart;

    //set the end point after the insertion
    acpNewEnd = m_acpStart + cch;

    if (dwFlags & TS_IAS_QUERYONLY)
    {
        *pacpStart = acpStart;
        *pacpEnd = acpOldEnd;
        return S_OK;
    }

    LPWSTR  pwszCopy;

    pwszCopy = (LPWSTR)GlobalAlloc(GMEM_FIXED, (cch + 1) * sizeof(WCHAR));
    if (NULL == pwszCopy)
    {
        return E_OUTOFMEMORY;
    }

    //pwszText will most likely not be NULL terminated
    CopyMemory(pwszCopy, pchText, cch * sizeof(WCHAR));
    pwszCopy[cch] = 0;

    //don't notify TSF of text and selection changes when in response to a TSF action
    m_fNotify = FALSE;

    //insert the text
    ::SendMessage(m_hWnd, EM_REPLACESEL, TRUE, (LPARAM)pwszCopy);

    //set the selection
    ::SendMessage(m_hWnd, EM_SETSEL, acpStart, acpNewEnd);

    m_fNotify = TRUE;

    _GetCurrentSelection();

    if (!(dwFlags & TS_IAS_NOQUERY))
    {
        *pacpStart = acpStart;
        *pacpEnd = acpNewEnd;
    }

    //set the TS_TEXTCHANGE members
    pChange->acpStart = acpStart;
    pChange->acpOldEnd = acpOldEnd;
    pChange->acpNewEnd = acpNewEnd;

    //defer the layout change notification until the document is unlocked
    m_fLayoutChanged = TRUE;

    return S_OK;
}

STDMETHODIMP TextEdit::InsertEmbeddedAtSelection(DWORD dwFlags, IDataObject* pDataObject, LONG* pacpStart, LONG* pacpEnd, TS_TEXTCHANGE* pChange)
{
	return E_NOTIMPL;
}

STDMETHODIMP TextEdit::RequestSupportedAttrs(DWORD dwFlags, ULONG cFilterAttrs, const TS_ATTRID* paFilterAttrs)
{
    return E_NOTIMPL;
}

STDMETHODIMP TextEdit::RequestAttrsAtPosition(LONG acpPos, ULONG cFilterAttrs, const TS_ATTRID* paFilterAttrs, DWORD dwFlags)
{
    return E_NOTIMPL;
}

STDMETHODIMP TextEdit::RequestAttrsTransitioningAtPosition(LONG acpPos, ULONG cFilterAttrs, const TS_ATTRID* paFilterAttrs, DWORD dwFlags)
{
    return E_NOTIMPL;
}

STDMETHODIMP TextEdit::FindNextAttrTransition(LONG acpStart, LONG acpHalt, ULONG cFilterAttrs, const TS_ATTRID* paFilterAttrs, DWORD dwFlags, LONG* pacpNext, BOOL* pfFound, LONG* plFoundOffset)
{
    return E_NOTIMPL;
}

STDMETHODIMP TextEdit::RetrieveRequestedAttrs(ULONG ulCount, TS_ATTRVAL* paAttrVals, ULONG* pcFetched)
{
    return E_NOTIMPL;
}

STDMETHODIMP TextEdit::GetEndACP(LONG* pacp)
{
    //OutputDebugString(TEXT("GetEndACP\n"));
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

    _GetCurrentSelection();

    *pacp = m_acpEnd;

    return S_OK;
}

STDMETHODIMP TextEdit::GetActiveView(TsViewCookie* pvcView)
{
    //OutputDebugString(TEXT("GetActiveView\n"));
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
    //OutputDebugString(TEXT("GetTextExt\n"));
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
    ACP* acp = new ACP();
    acp->acpEnd = acpEnd;
    acp->acpStart = acpStart;
    *pfClipped = SendMessage(m_hWnd, TF_GETTEXTEXT, (WPARAM)prc, (LPARAM)acp);

	return S_OK;
}

STDMETHODIMP TextEdit::GetScreenExt(TsViewCookie vcView, RECT* prc)
{
    //OutputDebugString(TEXT("GetScreenExt\n"));
    if (NULL == prc)
    {
        return E_INVALIDARG;
    }

    if (EDIT_VIEW_COOKIE != vcView)
    {
        return E_INVALIDARG;
    }

    GetWindowRect(m_hWnd, prc);
	return S_OK;
}

STDMETHODIMP TextEdit::GetWnd(TsViewCookie vcView, HWND* phwnd)
{
    //OutputDebugString(TEXT("GetWnd\n"));
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

    SendMessage(m_hWnd, TF_LOCKED, 0, dwLockFlags);
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
    SendMessage(m_hWnd, TF_UNLOCKED, 0, 0);
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

BOOL TextEdit::_GetCurrentSelection(void)
{
    //get the selection from the edit control
    ::SendMessage(m_hWnd, EM_GETSEL, (WPARAM)&m_acpStart, (LPARAM)&m_acpEnd);

    return TRUE;
}

HRESULT TextEdit::_GetText(LPWSTR* ppwsz, LPLONG pcch)
{
    DWORD   cch;
    LPWSTR  pwszText;

    *ppwsz = NULL;

    cch = _GetTextLength();
    pwszText = (LPWSTR)GlobalAlloc(GMEM_FIXED, (cch + 1) * sizeof(WCHAR));
    if (NULL == pwszText)
    {
        return E_OUTOFMEMORY;
    }

    SendMessage(m_hWnd, TF_GETTEXT, (WPARAM)pwszText, cch + 1);

    *ppwsz = pwszText;

    if (pcch)
    {
        *pcch = cch;
    }

    return S_OK;
}

ULONG TextEdit::_GetTextLength()
{
    return SendMessage(m_hWnd, TF_GETTEXTLENGTH, 0, 0);
}
