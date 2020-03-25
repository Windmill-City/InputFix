#include "TSF.h"
#include "TextEdit.h"

TSF::TSF() {
	HRESULT hr = CoInitialize(NULL);//A STA Thread is required or TSF wont work
	CheckHr(hr,"Failed to CoInitialize, need to run at a STA Thread");

	Pin(mgr, ITfThreadMgr*)
	hr = CoCreateInstance(CLSID_TF_ThreadMgr, NULL, CLSCTX_INPROC_SERVER, IID_ITfThreadMgr, (void**)p_mgr);
	CheckHr(hr,"Failed to create ThreadMgr");

	Pin(id, TfClientId);
	hr = mgr->Activate(p_id);
	CheckHr(hr,"Failed to Activate");

	ITfSource* source;
	hr = mgr->QueryInterface(IID_ITfSource, (void**)&source);
	CheckHr(hr, "Failed to query ITfSource");

	Pin(DocMgr, ITfDocumentMgr*);
	hr = mgr->CreateDocumentMgr(p_DocMgr);
	CheckHr(hr, "Failed to create DocMgr");
}

TSF::~TSF()
{
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

void TSF::CreateContext(_Handle ptr) {
	edit = new TextEdit(ToHWND(ptr));
	Pin(context, ITfContext*);
	Pin(EditCookie, TfEditCookie)
	HRESULT hr = DocMgr->CreateContext(id, 0, edit, p_context, p_EditCookie);
	CheckHr(hr, "Failed to create Context");
}

void TSF::PushContext() {
	//push the context onto the document stack
	HRESULT hr = DocMgr->Push(context);
	CheckHr(hr, "Failed to Push");
}

void TSF::PopContext() {
	HRESULT hr = DocMgr->Pop(TF_POPF_ALL);
	CheckHr(hr, "Failed to Pop");
}

void TSF::ReleaseContext() {
	if (context) {
		context->Release();
	}
	if (edit)
		edit->Release();
}

void TSF::SetTextExt(int left, int right, int top, int bottom) {
	edit->SetTextBoxRect(left, right, top, bottom);
}

void TSF::SetEnable(bool enable) {
	edit->SetEnable(enable);
}

void TSF::SetFocus() {
	mgr->SetFocus(DocMgr);
}

void TSF::AssociateFocus(_Handle hwnd) {
	ITfDocumentMgr* prev_DocMgr;
	mgr->AssociateFocus(ToHWND(hwnd),DocMgr, &prev_DocMgr);
	if (prev_DocMgr && prev_DocMgr != DocMgr) {
		prev_DocMgr->Release();
	}
}
