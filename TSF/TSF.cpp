#include "pch.h"
#include "Msctf.h"
#include "TSF.h"

public ref class TSF {
	ITfThreadMgr* mgr;
	ITfDocumentMgr* DocMgr;

	TfClientId id;
	ITfContext* context;
	TfContextOwner* owner;
	TfContextOwnerCompositionSink* sink;
	DWORD ownerCookie;
	
	TfEditCookie EditCookie;
	
public:
	TSF() {
		//CoUninitialize();//Unsafe, but it actually work
		HRESULT hr = CoInitialize(NULL);//A STA Thread is required or TSF wont work
		if(FAILED(hr))
			throw gcnew System::Exception("Failed to CoInitialize, need to run at a STA Thread");
		pin_ptr<ITfThreadMgr*> p_mgr = &mgr;
		hr = CoCreateInstance(CLSID_TF_ThreadMgr, NULL, CLSCTX_INPROC_SERVER, IID_ITfThreadMgr, (void**)p_mgr);

		if (FAILED(hr))
			throw gcnew System::Exception("Failed to create ThreadMgr");

		ITfSource* source;
		pin_ptr<ITfSource*> p_source = &source;
		hr = mgr->QueryInterface(IID_ITfSource, (void**)p_source);

		if (FAILED(hr))
			throw gcnew System::Exception("Failed to query ITfSource");

		DWORD cookie;
		pin_ptr<DWORD> p_cookie = &cookie;
		//hr = source->AdviseSink(IID_ITfActiveLanguageProfileNotifySink, new TfActiveLanguageProfileNotifySink(), p_cookie);

		if (FAILED(hr))
			throw gcnew System::Exception("Failed to advise ITfActiveLanguageProfileNotifySink");

		pin_ptr<TfClientId> p_id = &id;
		hr = mgr->Activate(p_id);

		if (FAILED(hr))
			throw gcnew System::Exception("Failed to Activate");

		pin_ptr<ITfDocumentMgr*> p_DocMgr = &DocMgr;
		hr = mgr->CreateDocumentMgr(p_DocMgr);

		if (FAILED(hr))
			throw gcnew System::Exception("Failed to create DocMgr");
	}

	void CreateContext(System::IntPtr ptr) {
		pin_ptr<ITfContext*> p_context = &context;
		pin_ptr<TfEditCookie> p_EditCookie = &EditCookie;
		sink = new TfContextOwnerCompositionSink();
		HRESULT hr = DocMgr->CreateContext(id, 0, sink, p_context, p_EditCookie);

		if (FAILED(hr))
			throw gcnew System::Exception("Failed to create Context");

		owner = new TfContextOwner((HWND)(int)ptr);

		ITfSource* source;
		pin_ptr<ITfSource*> p_source = &source;
		hr = context->QueryInterface(IID_ITfSource, (void**)p_source);

		if (FAILED(hr))
			throw gcnew System::Exception("Failed to query ITfSource");

		pin_ptr<DWORD> p_cookie = &ownerCookie;
		source->AdviseSink(IID_ITfContextOwner, owner, p_cookie);

		if(ownerCookie == -1)
			throw gcnew System::Exception("Failed to advise ITfContextOwner");
	}

	void PushContext() {
		//push the context onto the document stack
		HRESULT hr = DocMgr->Push(context);
		if (FAILED(hr))
			throw gcnew System::Exception("Failed to Push");
	}

	void PopContext() {
		HRESULT hr = DocMgr->Pop(TF_POPF_ALL);
		if (FAILED(hr))
			throw gcnew System::Exception("Failed to Pop");
	}

	void ReleaseContext() {
		if (context) {
			context->Release();
		}
		if (owner) {
			owner->Release();
		}
		if (sink) {
			sink->Release();
		}
	}

	void SetScreenExt() {
		owner->SetScreenExt();
	}

	void SetScreenExt(int left, int right, int top, int bottom) {
		owner->SetScreenExt(left, right, top, bottom);
	}

	void SetTextExt(int left, int right, int top, int bottom) {
		owner->SetTextExt(left, right, top, bottom);
	}

	void SetEnable(bool enable) {
		sink->SetEnable(enable);
	}

	void SetFocus() {
		mgr->SetFocus(DocMgr);
	}
	void AssociateFocus(System::IntPtr hwnd) {
		ITfDocumentMgr* prev_DocMgr;
		pin_ptr<ITfDocumentMgr*> p_prev_DocMgr = &prev_DocMgr;
		mgr->AssociateFocus((HWND)(int)hwnd,DocMgr, p_prev_DocMgr);
		if (prev_DocMgr && prev_DocMgr != DocMgr) {
			prev_DocMgr->Release();
		}
	}
	~TSF() {
		if (DocMgr)
		{
			//pop all of the contexts off of the stack
			DocMgr->Pop(TF_POPF_ALL);

			DocMgr->Release();
			DocMgr = NULL;
		}
		if (context)
		{
			context->Release();
			context = NULL;
		}

		mgr->Release();
		mgr = NULL;

		CoUninitialize();
	}
};

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
	return E_NOTIMPL;
}

HRESULT __stdcall TfContextOwner::GetTextExt(LONG acpStart, LONG acpEnd, RECT* prc, BOOL* pfClipped)
{
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
	phwnd = &hwnd;
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
void TfContextOwnerCompositionSink::SetEnable(BOOL enable)
{
	Enable = enable;
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
	*pfOk = FALSE;
	return S_OK;
}

HRESULT __stdcall TfContextOwnerCompositionSink::OnUpdateComposition(ITfCompositionView* pComposition, ITfRange* pRangeNew)
{
	return S_OK;
}


HRESULT __stdcall TfContextOwnerCompositionSink::OnEndComposition(ITfCompositionView* pComposition)
{
	return S_OK;
}
