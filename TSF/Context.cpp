#include "Context.h"

HRESULT __stdcall TfActiveLanguageProfileNotifySink::QueryInterface(REFIID riid, void** ppvObject)
{
	*ppvObject = NULL;

	//IUnknown
	if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_ITfActiveLanguageProfileNotifySink))
	{
		*ppvObject = (ITfActiveLanguageProfileNotifySink*)this;
	}
	if (*ppvObject)
	{
		(*(LPUNKNOWN*)ppvObject)->AddRef();
		return S_OK;
	}
	return E_NOINTERFACE;
}

ULONG __stdcall TfActiveLanguageProfileNotifySink::AddRef(void)
{
	return ++m_ObjRefCount;
}

ULONG __stdcall TfActiveLanguageProfileNotifySink::Release(void)
{
	if (--m_ObjRefCount == 0)
	{
		delete this;
		return 0;
	}

	return m_ObjRefCount;
}

HRESULT __stdcall TfActiveLanguageProfileNotifySink::OnActivated(REFCLSID clsid, REFGUID guidProfile, BOOL fActivated)
{
	return S_OK;
}

/**************************************************************************

   TfContextOwner struct impl

**************************************************************************/
TfContextOwner::TfContextOwner(HWND _hwnd)
{
	hwnd = _hwnd;

	TextExt = { 0,0,0,0 };
	ScreenExt = { 0,0,0,0 };

	GetWindowRect(hwnd, &ScreenExt);
}

void TfContextOwner::SetTextExt(int left, int right, int top, int bottom)
{
	TextExt.left = left;
	TextExt.right = right;
	TextExt.top = top;
	TextExt.bottom = bottom;

	MapWindowPoints(hwnd, NULL, (LPPOINT)&TextExt, 2);
}

void TfContextOwner::SetScreenExt()
{
	GetWindowRect(hwnd, &ScreenExt);
}

void TfContextOwner::SetScreenExt(int left, int right, int top, int bottom)
{
	ScreenExt.left = left;
	ScreenExt.right = right;
	ScreenExt.top = top;
	ScreenExt.bottom = bottom;

	MapWindowPoints(hwnd, NULL, (LPPOINT)&ScreenExt, 2);
}

HRESULT __stdcall TfContextOwner::QueryInterface(REFIID riid, void** ppvObject)
{
	*ppvObject = NULL;

	if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_ITfContextOwner))
	{
		*ppvObject = (ITfContextOwner*)this;
	}

	if (*ppvObject)
	{
		(*(LPUNKNOWN*)ppvObject)->AddRef();
		return S_OK;
	}
	return E_NOINTERFACE;
}

ULONG __stdcall TfContextOwner::AddRef(void)
{
	return ++m_ObjRefCount;
}

ULONG __stdcall TfContextOwner::Release(void)
{
	if (--m_ObjRefCount == 0)
	{
		delete this;
		return 0;
	}

	return m_ObjRefCount;
}

HRESULT __stdcall TfContextOwner::GetACPFromPoint(const POINT* ptScreen, DWORD dwFlags, LONG* pacp)
{
	return S_OK;
}

HRESULT __stdcall TfContextOwner::GetTextExt(LONG acpStart, LONG acpEnd, RECT* prc, BOOL* pfClipped)
{
	*pfClipped = FALSE;

	//it is required, if you set pfOK=FALSE at onStartComposition
	if (acpStart == acpEnd)
	{
		return E_INVALIDARG;
	}

	prc->left = TextExt.left;
	prc->right = TextExt.right;
	prc->top = TextExt.top;
	prc->bottom = TextExt.bottom;
	return S_OK;
}

HRESULT __stdcall TfContextOwner::GetScreenExt(RECT* prc)
{
	prc->left = ScreenExt.left;
	prc->right = ScreenExt.right;
	prc->top = ScreenExt.top;
	prc->bottom = ScreenExt.bottom;
	return S_OK;
}

HRESULT __stdcall TfContextOwner::GetStatus(TF_STATUS* pdcs)
{
	pdcs->dwDynamicFlags = 0;
	pdcs->dwStaticFlags = 0;
	return S_OK;
}

HRESULT __stdcall TfContextOwner::GetWnd(HWND* phwnd)
{
	//phwnd = &hwnd;
	phwnd = NULL;
	return S_OK;
}

HRESULT __stdcall TfContextOwner::GetAttribute(REFGUID rguidAttribute, VARIANT* pvarValue)
{
	pvarValue->vt = VT_EMPTY;
	return S_OK;
}
/**************************************************************************

   ITfContextOwnerCompositionSink struct impl

**************************************************************************/

TfContextOwnerCompositionSink::TfContextOwnerCompositionSink(HWND _hwnd)
{
	hwnd = _hwnd;
	enable = false;
}
TfContextOwnerCompositionSink::~TfContextOwnerCompositionSink()
{
}
void TfContextOwnerCompositionSink::SetEnable(BOOL _enable)
{
	enable = _enable;
}
HRESULT __stdcall TfContextOwnerCompositionSink::QueryInterface(REFIID riid, void** ppvObject)
{
	*ppvObject = NULL;

	if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_ITfContextOwnerCompositionSink))
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

ULONG __stdcall TfContextOwnerCompositionSink::AddRef(void)
{
	return ++m_ObjRefCount;
}

ULONG __stdcall TfContextOwnerCompositionSink::Release(void)
{
	if (--m_ObjRefCount == 0)
	{
		delete this;
		return 0;
	}

	return m_ObjRefCount;
}

HRESULT __stdcall TfContextOwnerCompositionSink::OnStartComposition(ITfCompositionView* pComposition, BOOL* pfOk)
{
	*pfOk = enable;
	SendMessage(hwnd, WM_IME_STARTCOMPOSITION, 0, 0);
	return S_OK;
}

HRESULT __stdcall TfContextOwnerCompositionSink::OnUpdateComposition(ITfCompositionView* pComposition, ITfRange* pRangeNew)
{
	
	SendMessage(hwnd, WM_IME_COMPOSITION, 0, 0);
	return S_OK;
}


HRESULT __stdcall TfContextOwnerCompositionSink::OnEndComposition(ITfCompositionView* pComposition)
{
	SendMessage(hwnd, WM_IME_ENDCOMPOSITION, 0, 0);
	return S_OK;
}