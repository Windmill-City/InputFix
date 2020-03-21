#include "pch.h"
#include "Msctf.h"
#include "TSF.h"
#include "Context.h"

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
