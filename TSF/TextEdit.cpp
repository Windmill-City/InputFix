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

    m_string.clear();

    //update current selection
    m_acpStart = m_acpEnd = 0;

    //reset result start
    m_resultstart = 0;

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
    m_isStartComp = true;
    return S_OK;
}

STDMETHODIMP TextEdit::OnUpdateComposition(ITfCompositionView* pComposition, ITfRange* pRangeNew)
{
    OutputDebugString(TEXT("OnUpdateComposition\n"));
    HandleComposition();
    m_isStartComp = false;
    return S_OK;
}

STDMETHODIMP TextEdit::OnEndComposition(ITfCompositionView* pComposition)
{
    OutputDebugString(TEXT("OnEndComposition\n"));
    SendMessage(m_hWnd, WM_IME_ENDCOMPOSITION, 0, 0);
    return S_OK;
}

void TextEdit::HandleComposition()
{
    DEBUG("m_string:%ws\n", m_string.c_str());
    //the text with underline
    ULONG comp_start = m_isStartComp && static_cast<LONG>(m_string.length()) > m_resultstart&& iswalpha(m_string[m_resultstart]) ? m_resultstart : -1;
    //comp text
    WCHAR* text = new WCHAR[32];

    ITfRange* range;
    //Get attr
    VARIANT var;
    VariantInit(&var);
    HRESULT hr;

    IEnumTfRanges* enumRanges;
    attr_prop->EnumRanges(editcookie, &enumRanges, NULL);
    while (enumRanges->Next(1, &range, NULL) == S_OK) {
        //GetText
        ULONG pcch;
        range->GetText(editcookie, 0, text, 32, &pcch);
        text[pcch] = '\0';
        hr = attr_prop->GetValue(editcookie, range, &var);
        DEBUG("Text:%S£¬cch:%lu\n", text, pcch);
        if (pcch == 0)
            continue;
        if (SUCCEEDED(hr))
        {
            if (VT_I4 == var.vt)
            {
                //The property is a guidatom. 
                GUID    guid;

                //Convert the guidatom into a GUID. 
                hr = CategoryMgr->GetGUID((TfGuidAtom)var.lVal, &guid);
                if (SUCCEEDED(hr))
                {
                    ITfDisplayAttributeInfo* pDispInfo;

                    //Get the display attribute info object for this attribute. 
                    hr = DispMgr->GetDisplayAttributeInfo(guid, &pDispInfo, NULL);
                    if (SUCCEEDED(hr))
                    {
                        //Get the display attribute info. 
                        TF_DISPLAYATTRIBUTE* pDispAttr = new TF_DISPLAYATTRIBUTE();
                        hr = pDispInfo->GetAttributeInfo(pDispAttr);
                        if (SUCCEEDED(hr)) {
                            if (pDispAttr->lsStyle != TF_LS_NONE)
                            {
                                ITfRangeACP* rangeacp;
                                LONG start;
                                LONG len;
                                range->QueryInterface(IID_ITfRangeACP, (void**)&rangeacp);
                                rangeacp->GetExtent(&start, &len);
                                comp_start = start;
                                pDispInfo->Release();
                                break;
                            }
                        }
                        pDispInfo->Release();
                    }
                }
            }
        }
    }
    VariantClear(&var);
    if (static_cast<LONG>(m_string.length()) == m_resultstart + 1 && m_string[m_resultstart] == m_lastchar) {
        comp_start = m_resultstart;
    }
    m_lastchar = text[0];
    if (comp_start != -1 || static_cast<LONG>(m_string.length()) > 0) {
        ULONG len = static_cast<LONG>(m_string.length());

        if (comp_start != -1) {
            //have comp and result
            if (comp_start != 0) {
                int cch = comp_start - m_resultstart;
                if (cch > 0) {
                    LPWSTR result = (LPWSTR)GlobalAlloc(GPTR, (cch + 1) * sizeof(WCHAR));
                    m_string.copy(result, cch, m_resultstart);//get result
                    result[cch] = '\0';

                    m_resultstart += cch;

                    SendMessage(m_hWnd, WM_IME_COMPOSITION, (WPARAM)result, 1);

                    GlobalFree(result);
                }
            }
            //only comp
            if (len > comp_start) {
                LPWSTR comp = (LPWSTR)GlobalAlloc(GPTR, (len - comp_start + 1) * sizeof(WCHAR));
                m_string.copy(comp, len - comp_start, comp_start);
                comp[len - comp_start] = '\0';
                SendMessage(m_hWnd, WM_IME_COMPOSITION, (WPARAM)comp, 0);
                GlobalFree(comp);
            }
            else
                SendMessage(m_hWnd, WM_IME_COMPOSITION, 0, -1);
        }
        else
        {
            //no comp,only result
            int cch = len - m_resultstart;
            if (cch > 0) {
                LPWSTR result = (LPWSTR)GlobalAlloc(GPTR, (cch + 1) * sizeof(WCHAR));
                m_string.copy(result, cch, m_resultstart);
                result[cch] = '\0';

                m_resultstart += cch;

                SendMessage(m_hWnd, WM_IME_COMPOSITION, (WPARAM)result, 1);
                GlobalFree(result);
            }
            else
                SendMessage(m_hWnd, WM_IME_COMPOSITION, hr, -1);//clear comp
        }
    }
    else
        //lparam->
        //        0->composition text
        //        -1->hr result
        //        1->result text
        SendMessage(m_hWnd, WM_IME_COMPOSITION, hr, -1);
}

HRESULT __stdcall TextEdit::BeginUIElement(DWORD dwUIElementId, BOOL* pbShow)
{
    *pbShow = TRUE;
    return S_OK;
}

HRESULT __stdcall TextEdit::UpdateUIElement(DWORD dwUIElementId)
{
    HandleComposition();
    return S_OK;
}

HRESULT __stdcall TextEdit::EndUIElement(DWORD dwUIElementId)
{
    return S_OK;
}

STDMETHODIMP TextEdit::OnUpdateInfo(void)
{
    HandleComposition();
    return S_OK;
}

STDMETHODIMP TextEdit::OnTransitoryExtensionUpdated(ITfContext* pic, TfEditCookie ecReadOnly, ITfRange* pResultRange, ITfRange* pCompositionRange, BOOL* pfDeleteResultRange)
{
    return S_OK;
}
