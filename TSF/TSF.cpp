#include "TSF.h"
#include "Context.h"


TSF::TSF() {
	//CoUninitialize();//Unsafe, but it actually work
	HRESULT hr = CoInitialize(NULL);//A STA Thread is required or TSF wont work
	if (FAILED(hr))
		throw gcnew System::Exception("Failed to CoInitialize, need to run at a STA Thread");
	pin_ptr<ITfThreadMgr*> p_mgr = &mgr;
	hr = CoCreateInstance(CLSID_TF_ThreadMgr, NULL, CLSCTX_INPROC_SERVER, IID_ITfThreadMgr, (void**)p_mgr);

	if (FAILED(hr))
		throw gcnew System::Exception("Failed to create ThreadMgr");

	pin_ptr<TfClientId> p_id = &id;
	hr = mgr->Activate(p_id);

	if (FAILED(hr))
		throw gcnew System::Exception("Failed to Activate");

	ITfSource* source;
	pin_ptr<ITfSource*> p_source = &source;
	hr = mgr->QueryInterface(IID_ITfSource, (void**)&source);

	if (FAILED(hr))
		throw gcnew System::Exception("Failed to query ITfSource");

	pin_ptr<ITfDocumentMgr*> p_DocMgr = &DocMgr;
	hr = mgr->CreateDocumentMgr(p_DocMgr);

	if (FAILED(hr))
		throw gcnew System::Exception("Failed to create DocMgr");
	
}

TSF::~TSF() {
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

void TSF::CreateContext(System::IntPtr ptr) {
	pin_ptr<ITfContext*> p_context = &context;
	pin_ptr<TfEditCookie> p_EditCookie = &EditCookie;
	sink = new TfContextOwnerCompositionSink((HWND)(int)ptr);
	HRESULT hr = DocMgr->CreateContext(id, 0, sink, p_context, p_EditCookie);
	sink->SetCookie(EditCookie);

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

	/*ITfSource* _source;
	pin_ptr<ITfSource*> p__source = &_source;
	hr = mgr->QueryInterface(IID_ITfSource, (void**)p__source);

	if (FAILED(hr))
		throw gcnew System::Exception("Failed to query ITfSource");

	tesink = new TfTransitoryExtensionSink((HWND)(int)ptr);

	p_cookie = &teCookie;
	_source->AdviseSink(IID_ITfTransitoryExtensionSink, tesink, p_cookie);

	if (teCookie == -1)
		throw gcnew System::Exception("Failed to advise ITfTransitoryExtensionSink");*/
}

void TSF::PushContext() {
	//push the context onto the document stack
	HRESULT hr = DocMgr->Push(context);
	if (FAILED(hr))
		throw gcnew System::Exception("Failed to Push");
}

void TSF::PopContext() {
	HRESULT hr = DocMgr->Pop(TF_POPF_ALL);
	if (FAILED(hr))
		throw gcnew System::Exception("Failed to Pop");
}

void TSF::ReleaseContext() {
	ITfSource* source;
	pin_ptr<ITfSource*> p_source = &source;
	HRESULT hr = context->QueryInterface(IID_ITfSource, (void**)p_source);

	if (FAILED(hr))
		throw gcnew System::Exception("Failed to query ITfSource");
	source->UnadviseSink(ownerCookie);
	ownerCookie = NULL;
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

void TSF::SetScreenExt() {
	owner->SetScreenExt();
}

void TSF::SetScreenExt(int left, int right, int top, int bottom) {
	owner->SetScreenExt(left, right, top, bottom);
}

void TSF::SetTextExt(int left, int right, int top, int bottom) {
	owner->SetTextExt(left, right, top, bottom);
}

void TSF::SetEnable(bool enable) {
	sink->SetEnable(enable);
}

void TSF::SetFocus() {
	mgr->SetFocus(DocMgr);
}

void TSF::AssociateFocus(System::IntPtr hwnd) {
	ITfDocumentMgr* prev_DocMgr;
	pin_ptr<ITfDocumentMgr*> p_prev_DocMgr = &prev_DocMgr;
	mgr->AssociateFocus((HWND)(int)hwnd,DocMgr, p_prev_DocMgr);
	if (prev_DocMgr && prev_DocMgr != DocMgr) {
		prev_DocMgr->Release();
	}
}
