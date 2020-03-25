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

STDMETHODIMP_(HRESULT __stdcall)  TextEdit::QueryInterface(REFIID riid, void** ppvObject)
{
    *ppvObject = NULL;

    //IUnknown
    if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_ITextStoreACP))
    {
        *ppvObject = (ITextStoreACP*)this;
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
