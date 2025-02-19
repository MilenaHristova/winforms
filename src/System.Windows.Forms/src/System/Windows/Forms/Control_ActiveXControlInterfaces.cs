﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Drawing;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.Ole;
using System.Runtime.InteropServices;
using ComIDataObject = Windows.Win32.System.Com.IDataObject;

namespace System.Windows.Forms;

public unsafe partial class Control :
    IOleControl.Interface,
    IOleObject.Interface,
    IOleInPlaceObject.Interface,
    IOleInPlaceActiveObject.Interface,
    IOleWindow.Interface,
    IViewObject.Interface,
    IViewObject2.Interface,
    IPersist.Interface,
    IPersistStreamInit.Interface,
    IPersistPropertyBag.Interface,
    IPersistStorage.Interface,
    IQuickActivate.Interface
{
    unsafe HRESULT IOleControl.Interface.GetControlInfo(CONTROLINFO* pCI)
    {
        if (pCI is null)
        {
            return HRESULT.E_POINTER;
        }

        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:GetControlInfo");
        pCI->cb = (uint)sizeof(CONTROLINFO);
        pCI->hAccel = HACCEL.Null;
        pCI->cAccel = 0;
        pCI->dwFlags = 0;

        if (IsInputKey(Keys.Return))
        {
            pCI->dwFlags |= CTRLINFO.CTRLINFO_EATS_RETURN;
        }

        if (IsInputKey(Keys.Escape))
        {
            pCI->dwFlags |= CTRLINFO.CTRLINFO_EATS_ESCAPE;
        }

        return ActiveXInstance.GetControlInfo(pCI);
    }

    unsafe HRESULT IOleControl.Interface.OnMnemonic(MSG* pMsg)
    {
        if (pMsg is null)
        {
            return HRESULT.E_INVALIDARG;
        }

        // If we got a mnemonic here, then the appropriate control will focus itself which
        // will cause us to become UI active.
        bool processed = ProcessMnemonic((char)(nuint)pMsg->wParam);
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, $"AxSource:OnMnemonic processed: {processed}");
        return HRESULT.S_OK;
    }

    HRESULT IOleControl.Interface.OnAmbientPropertyChange(int dispID)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, $"AxSource:OnAmbientPropertyChange. Dispid: {dispID}");
        Debug.Indent();
        ActiveXInstance.OnAmbientPropertyChange(dispID);
        Debug.Unindent();
        return HRESULT.S_OK;
    }

    HRESULT IOleControl.Interface.FreezeEvents(BOOL bFreeze)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, $"AxSource:FreezeEvents. Freeze: {bFreeze}");
        ActiveXInstance.EventsFrozen = bFreeze;
        Debug.Assert(ActiveXInstance.EventsFrozen == bFreeze, "Failed to set EventsFrozen correctly");
        return HRESULT.S_OK;
    }

    unsafe HRESULT IOleInPlaceActiveObject.Interface.GetWindow(HWND* phwnd)
        => ((IOleInPlaceObject.Interface)this).GetWindow(phwnd);

    HRESULT IOleInPlaceActiveObject.Interface.ContextSensitiveHelp(BOOL fEnterMode)
        => ((IOleInPlaceObject.Interface)this).ContextSensitiveHelp(fEnterMode);

    unsafe HRESULT IOleInPlaceActiveObject.Interface.TranslateAccelerator(MSG* lpmsg)
        => ActiveXInstance.TranslateAccelerator(lpmsg);

    HRESULT IOleInPlaceActiveObject.Interface.OnFrameWindowActivate(BOOL fActivate)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:OnFrameWindowActivate");
        OnFrameWindowActivate(fActivate);
        return HRESULT.S_OK;
    }

    HRESULT IOleInPlaceActiveObject.Interface.OnDocWindowActivate(BOOL fActivate)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, $"AxSource:OnDocWindowActivate.  Activate: {(bool)fActivate}");
        Debug.Indent();
        ActiveXInstance.OnDocWindowActivate(fActivate);
        Debug.Unindent();
        return HRESULT.S_OK;
    }

    unsafe HRESULT IOleInPlaceActiveObject.Interface.ResizeBorder(RECT* prcBorder, IOleInPlaceUIWindow* pUIWindow, BOOL fFrameWindow)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:ResizesBorder");
        return HRESULT.S_OK;
    }

    HRESULT IOleInPlaceActiveObject.Interface.EnableModeless(BOOL fEnable)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:EnableModeless");
        return HRESULT.E_NOTIMPL;
    }

    unsafe HRESULT IOleInPlaceObject.Interface.GetWindow(HWND* phwnd)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:GetWindow");
        HRESULT hr = ActiveXInstance.GetWindow(phwnd);
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, $"\twin == {(phwnd is null ? HWND.Null : *phwnd)}");
        return hr;
    }

    HRESULT IOleInPlaceObject.Interface.ContextSensitiveHelp(BOOL fEnterMode)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, $"AxSource:ContextSensitiveHelp.  Mode: {fEnterMode}");
        if (fEnterMode)
        {
            OnHelpRequested(new HelpEventArgs(MousePosition));
        }

        return HRESULT.S_OK;
    }

    HRESULT IOleInPlaceObject.Interface.InPlaceDeactivate()
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:InPlaceDeactivate");
        Debug.Indent();
        HRESULT hr = ActiveXInstance.InPlaceDeactivate();
        Debug.Unindent();
        return hr;
    }

    HRESULT IOleInPlaceObject.Interface.UIDeactivate()
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:UIDeactivate");
        return ActiveXInstance.UIDeactivate();
    }

    unsafe HRESULT IOleInPlaceObject.Interface.SetObjectRects(RECT* lprcPosRect, RECT* lprcClipRect)
    {
        if (lprcClipRect is not null)
        {
            Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo,
                $"AxSource:SetObjectRects({lprcClipRect->left}, {lprcClipRect->top}, {lprcClipRect->right}, {lprcClipRect->bottom})");
        }

        Debug.Indent();
        HRESULT hr = ActiveXInstance.SetObjectRects(lprcPosRect, lprcClipRect);
        Debug.Unindent();
        return hr;
    }

    HRESULT IOleInPlaceObject.Interface.ReactivateAndUndo()
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:ReactivateAndUndo");
        return HRESULT.S_OK;
    }

    unsafe HRESULT IOleObject.Interface.SetClientSite(IOleClientSite* pClientSite)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:SetClientSite");
        ActiveXInstance.SetClientSite(pClientSite);
        return HRESULT.S_OK;
    }

    unsafe HRESULT IOleObject.Interface.GetClientSite(IOleClientSite** ppClientSite)
    {
        if (ppClientSite is null)
        {
            return HRESULT.E_POINTER;
        }

        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:GetClientSite");
        *ppClientSite = ActiveXInstance.GetClientSite().Value;
        return HRESULT.S_OK;
    }

    HRESULT IOleObject.Interface.SetHostNames(PCWSTR szContainerApp, PCWSTR szContainerObj)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:SetHostNames");

        // Since ActiveX controls never "open" for editing, we shouldn't need to store these.
        return HRESULT.S_OK;
    }

    HRESULT IOleObject.Interface.Close(uint dwSaveOption)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, $"AxSource:Close. Save option: {dwSaveOption}");
        ActiveXInstance.Close((OLECLOSE)dwSaveOption);
        return HRESULT.S_OK;
    }

    unsafe HRESULT IOleObject.Interface.SetMoniker(uint dwWhichMoniker, IMoniker* pmk)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:SetMoniker");
        return HRESULT.E_NOTIMPL;
    }

    HRESULT IOleObject.Interface.GetMoniker(uint dwAssign, uint dwWhichMoniker, IMoniker** ppmk)
    {
        if (ppmk is null)
        {
            return HRESULT.E_POINTER;
        }

        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:GetMoniker");
        *ppmk = null;
        return HRESULT.E_NOTIMPL;
    }

    HRESULT IOleObject.Interface.InitFromData(ComIDataObject* pDataObject, BOOL fCreation, uint dwReserved)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:InitFromData");
        return HRESULT.E_NOTIMPL;
    }

    HRESULT IOleObject.Interface.GetClipboardData(uint dwReserved, ComIDataObject** ppDataObject)
    {
        if (ppDataObject is null)
        {
            return HRESULT.E_POINTER;
        }

        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:GetClipboardData");
        ppDataObject = null;
        return HRESULT.E_NOTIMPL;
    }

    HRESULT IOleObject.Interface.DoVerb(
        int iVerb,
        MSG* lpmsg,
        IOleClientSite* pActiveSite,
        int lindex,
        HWND hwndParent,
        RECT* lprcPosRect)
    {
        // In Office they are internally casting an iVerb to a short and not doing the proper sign extension.
        short sVerb = unchecked((short)iVerb);
        iVerb = sVerb;

#if DEBUG
        if (CompModSwitches.ActiveX.TraceInfo)
        {
            Debug.WriteLine("AxSource:DoVerb {");
            Debug.WriteLine($"     verb: {iVerb}");
            Debug.WriteLine($"     msg: {*lpmsg}");
            Debug.WriteLine($"     activeSite: {*pActiveSite}");
            Debug.WriteLine($"     index: {lindex}");
            Debug.WriteLine($"     hwndParent: {hwndParent}");
            Debug.WriteLine($"     posRect: {(lprcPosRect is null ? "null" : lprcPosRect->ToString())}");
        }
#endif

        Debug.Indent();
        try
        {
            return ActiveXInstance.DoVerb((OLEIVERB)iVerb, lpmsg, pActiveSite, lindex, hwndParent, lprcPosRect);
        }
        finally
        {
            Debug.Unindent();
            Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "}");
        }
    }

    unsafe HRESULT IOleObject.Interface.EnumVerbs(IEnumOLEVERB** ppEnumOleVerb)
    {
        if (ppEnumOleVerb is null)
        {
            return HRESULT.E_POINTER;
        }

        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:EnumVerbs");
        *ppEnumOleVerb = ActiveXImpl.EnumVerbs();
        return HRESULT.S_OK;
    }

    HRESULT IOleObject.Interface.Update()
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:OleUpdate");
        return HRESULT.S_OK;
    }

    HRESULT IOleObject.Interface.IsUpToDate()
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:IsUpToDate");
        return HRESULT.S_OK;
    }

    unsafe HRESULT IOleObject.Interface.GetUserClassID(Guid* pClsid)
    {
        if (pClsid is null)
        {
            return HRESULT.E_POINTER;
        }

        *pClsid = GetType().GUID;
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, $"AxSource:GetUserClassID.  ClassID: {pClsid->ToString()}");
        return HRESULT.S_OK;
    }

    HRESULT IOleObject.Interface.GetUserType(uint dwFormOfType, PWSTR* pszUserType)
    {
        if (pszUserType is null)
        {
            return HRESULT.E_POINTER;
        }

        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:GetUserType");
        *pszUserType = (char*)Marshal.StringToCoTaskMemUni(
            (USERCLASSTYPE)dwFormOfType == USERCLASSTYPE.USERCLASSTYPE_FULL ? GetType().FullName : GetType().Name);

        return HRESULT.S_OK;
    }

    HRESULT IOleObject.Interface.SetExtent(DVASPECT dwDrawAspect, SIZE* psizel)
    {
        if (psizel is null)
        {
            return HRESULT.E_INVALIDARG;
        }

        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, $"AxSource:SetExtent({psizel->Width}, {psizel->Height}");
        Debug.Indent();
        ActiveXInstance.SetExtent(dwDrawAspect, (Size*)psizel);
        Debug.Unindent();
        return HRESULT.S_OK;
    }

    HRESULT IOleObject.Interface.GetExtent(DVASPECT dwDrawAspect, SIZE* psizel)
    {
        if (psizel is null)
        {
            return HRESULT.E_INVALIDARG;
        }

        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, $"AxSource:GetExtent.  Aspect: {dwDrawAspect}");
        Debug.Indent();
        ActiveXInstance.GetExtent(dwDrawAspect, (Size*)psizel);
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, $"value: {psizel->Width}, {psizel->Height}");
        Debug.Unindent();
        return HRESULT.S_OK;
    }

    HRESULT IOleObject.Interface.Advise(IAdviseSink* pAdvSink, uint* pdwConnection)
    {
        if (pdwConnection is null)
        {
            return HRESULT.E_POINTER;
        }

        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:Advise");
        *pdwConnection = ActiveXInstance.Advise(pAdvSink);
        return HRESULT.S_OK;
    }

    HRESULT IOleObject.Interface.Unadvise(uint dwConnection)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:Unadvise");
        Debug.Indent();
        HRESULT hr = ActiveXInstance.Unadvise(dwConnection);
        Debug.Unindent();
        return hr;
    }

    HRESULT IOleObject.Interface.EnumAdvise(IEnumSTATDATA** ppenumAdvise)
    {
        if (ppenumAdvise is null)
        {
            return HRESULT.E_POINTER;
        }

        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:EnumAdvise");
        *ppenumAdvise = null;
        return HRESULT.E_NOTIMPL;
    }

    HRESULT IOleObject.Interface.GetMiscStatus(DVASPECT dwAspect, OLEMISC* pdwStatus)
    {
        if (pdwStatus is null)
        {
            return HRESULT.E_POINTER;
        }

        if (!dwAspect.HasFlag(DVASPECT.DVASPECT_CONTENT))
        {
            Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:GetMiscStatus.  Status: ERROR, wrong aspect.");
            *pdwStatus = 0;
            return HRESULT.DV_E_DVASPECT;
        }

        OLEMISC status = OLEMISC.OLEMISC_ACTIVATEWHENVISIBLE | OLEMISC.OLEMISC_INSIDEOUT | OLEMISC.OLEMISC_SETCLIENTSITEFIRST;
        if (GetStyle(ControlStyles.ResizeRedraw))
        {
            status |= OLEMISC.OLEMISC_RECOMPOSEONRESIZE;
        }

        if (this is IButtonControl)
        {
            status |= OLEMISC.OLEMISC_ACTSLIKEBUTTON;
        }

        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, $"AxSource:GetMiscStatus. Status: {status}");
        *pdwStatus = status;
        return HRESULT.S_OK;
    }

    HRESULT IOleObject.Interface.SetColorScheme(LOGPALETTE* pLogpal)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:SetColorScheme");
        return HRESULT.S_OK;
    }

    HRESULT IOleWindow.Interface.GetWindow(HWND* phwnd) => ((IOleInPlaceObject.Interface)this).GetWindow(phwnd);

    HRESULT IOleWindow.Interface.ContextSensitiveHelp(BOOL fEnterMode) => ((IOleInPlaceObject.Interface)this).ContextSensitiveHelp(fEnterMode);

    unsafe HRESULT IPersist.Interface.GetClassID(Guid* pClassID)
    {
        if (pClassID is null)
        {
            return HRESULT.E_POINTER;
        }

        *pClassID = GetType().GUID;
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, $"AxSource:IPersist.GetClassID.  ClassID: {*pClassID}");
        return HRESULT.S_OK;
    }

    HRESULT IPersistPropertyBag.Interface.InitNew()
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:IPersistPropertyBag.InitNew");
        return HRESULT.S_OK;
    }

    HRESULT IPersistPropertyBag.Interface.GetClassID(Guid* pClassID)
    {
        if (pClassID is null)
        {
            return HRESULT.E_POINTER;
        }

        *pClassID = GetType().GUID;
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, $"AxSource:IPersistPropertyBag.GetClassID.  ClassID: {*pClassID}");
        return HRESULT.S_OK;
    }

    HRESULT IPersistPropertyBag.Interface.Load(IPropertyBag* pPropBag, IErrorLog* pErrorLog)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:Load (IPersistPropertyBag)");
        Debug.Indent();
        ActiveXInstance.Load(pPropBag, pErrorLog);
        Debug.Unindent();
        return HRESULT.S_OK;
    }

    HRESULT IPersistPropertyBag.Interface.Save(IPropertyBag* pPropBag, BOOL fClearDirty, BOOL fSaveAllProperties)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:Save (IPersistPropertyBag)");
        Debug.Indent();
        ActiveXInstance.Save(pPropBag, fClearDirty, fSaveAllProperties);
        Debug.Unindent();
        return HRESULT.S_OK;
    }

    HRESULT IPersistStorage.Interface.GetClassID(Guid* pClassID)
    {
        if (pClassID is null)
        {
            return HRESULT.E_POINTER;
        }

        *pClassID = GetType().GUID;
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, $"AxSource:IPersistStorage.GetClassID.  ClassID: {*pClassID}");
        return HRESULT.S_OK;
    }

    HRESULT IPersistStorage.Interface.IsDirty()
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:IPersistStorage.IsDirty");
        return ActiveXInstance.IsDirty();
    }

    HRESULT IPersistStorage.Interface.InitNew(IStorage* pStg)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:IPersistStorage.InitNew");
        return HRESULT.S_OK;
    }

    HRESULT IPersistStorage.Interface.Load(IStorage* pStg)
    {
        if (pStg is null)
        {
            return HRESULT.E_POINTER;
        }

        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:IPersistStorage.Load");
        Debug.Indent();
        HRESULT result = ActiveXInstance.Load(pStg);
        Debug.Unindent();
        return result;
    }

    HRESULT IPersistStorage.Interface.Save(IStorage* pStgSave, BOOL fSameAsLoad)
    {
        if (pStgSave is null)
        {
            return HRESULT.E_POINTER;
        }

        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:IPersistStorage.Save");
        Debug.Indent();
        HRESULT result = ActiveXInstance.Save(pStgSave, fSameAsLoad);
        Debug.Unindent();
        return result;
    }

    HRESULT IPersistStorage.Interface.SaveCompleted(IStorage* pStgNew)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:IPersistStorage.SaveCompleted");
        return HRESULT.S_OK;
    }

    HRESULT IPersistStorage.Interface.HandsOffStorage()
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:IPersistStorage.HandsOffStorage");
        return HRESULT.S_OK;
    }

    HRESULT IPersistStreamInit.Interface.GetClassID(Guid* pClassID)
    {
        if (pClassID is null)
        {
            return HRESULT.E_POINTER;
        }

        *pClassID = GetType().GUID;
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, $"AxSource:IPersistStreamInit.GetClassID.  ClassID: {*pClassID}");
        return HRESULT.S_OK;
    }

    HRESULT IPersistStreamInit.Interface.IsDirty()
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:IPersistStreamInit.IsDirty");
        return ActiveXInstance.IsDirty();
    }

    HRESULT IPersistStreamInit.Interface.Load(IStream* pStm)
    {
        if (pStm is null)
        {
            return HRESULT.E_POINTER;
        }

        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:IPersistStreamInit.Load");
        Debug.Indent();
        ActiveXInstance.Load(pStm);
        Debug.Unindent();
        return HRESULT.S_OK;
    }

    HRESULT IPersistStreamInit.Interface.Save(IStream* pStm, BOOL fClearDirty)
    {
        if (pStm is null)
        {
            return HRESULT.E_POINTER;
        }

        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:IPersistStreamInit.Save");
        Debug.Indent();
        ActiveXInstance.Save(pStm, fClearDirty);
        Debug.Unindent();
        return HRESULT.S_OK;
    }

    HRESULT IPersistStreamInit.Interface.GetSizeMax(ulong* pCbSize)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:GetSizeMax");
        return HRESULT.S_OK;
    }

    HRESULT IPersistStreamInit.Interface.InitNew()
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:IPersistStreamInit.InitNew");
        return HRESULT.S_OK;
    }

    HRESULT IQuickActivate.Interface.QuickActivate(QACONTAINER* pQaContainer, QACONTROL* pQaControl)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:QuickActivate");
        Debug.Indent();
        HRESULT hr = ActiveXInstance.QuickActivate(pQaContainer, pQaControl);
        Debug.Unindent();
        return hr;
    }

    HRESULT IQuickActivate.Interface.SetContentExtent(SIZE* pSizel)
    {
        if (pSizel is null)
        {
            return HRESULT.E_INVALIDARG;
        }

        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:SetContentExtent");
        Debug.Indent();
        ActiveXInstance.SetExtent(DVASPECT.DVASPECT_CONTENT, (Size*)pSizel);
        Debug.Unindent();
        return HRESULT.S_OK;
    }

    HRESULT IQuickActivate.Interface.GetContentExtent(SIZE* pSizel)
    {
        if (pSizel is null)
        {
            return HRESULT.E_INVALIDARG;
        }

        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:GetContentExtent");
        Debug.Indent();
        ActiveXInstance.GetExtent(DVASPECT.DVASPECT_CONTENT, (Size*)pSizel);
        Debug.Unindent();
        return HRESULT.S_OK;
    }

    HRESULT IViewObject.Interface.Draw(
        DVASPECT dwDrawAspect,
        int lindex,
        void* pvAspect,
        DVTARGETDEVICE* ptd,
        HDC hdcTargetDev,
        HDC hdcDraw,
        RECTL* lprcBounds,
        RECTL* lprcWBounds,
        nint pfnContinue,
        nuint dwContinue)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:Draw");

        Debug.Indent();
        HRESULT hr = ActiveXInstance.Draw(
            dwDrawAspect,
            lindex,
            pvAspect,
            ptd,
            hdcTargetDev,
            hdcDraw,
            (RECT*)lprcBounds,
            (RECT*)lprcWBounds,
            pfnContinue,
            dwContinue);
        Debug.Unindent();
        return HRESULT.S_OK;
    }

    HRESULT IViewObject.Interface.GetColorSet(
        DVASPECT dwDrawAspect,
        int lindex,
        void* pvAspect,
        DVTARGETDEVICE* ptd,
        HDC hicTargetDev,
        LOGPALETTE** ppColorSet)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:GetColorSet");

        // GDI+ doesn't do palettes.
        return HRESULT.E_NOTIMPL;
    }

    unsafe HRESULT IViewObject.Interface.Freeze(DVASPECT dwDrawAspect, int lindex, void* pvAspect, uint* pdwFreeze)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:Freezes");
        return HRESULT.E_NOTIMPL;
    }

    HRESULT IViewObject.Interface.Unfreeze(uint dwFreeze)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:Unfreeze");
        return HRESULT.E_NOTIMPL;
    }

    HRESULT IViewObject.Interface.SetAdvise(DVASPECT aspects, uint advf, IAdviseSink* pAdvSink)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:SetAdvise");
        return ActiveXInstance.SetAdvise(aspects, (ADVF)advf, pAdvSink);
    }

    HRESULT IViewObject.Interface.GetAdvise(uint* pAspects, uint* pAdvf, IAdviseSink** ppAdvSink)
    {
        Debug.WriteLineIf(CompModSwitches.ActiveX.TraceInfo, "AxSource:GetAdvise");
        return ActiveXInstance.GetAdvise((DVASPECT*)pAspects, (ADVF*)pAdvf, ppAdvSink);
    }

    HRESULT IViewObject2.Interface.Draw(
        DVASPECT dwDrawAspect,
        int lindex,
        void* pvAspect,
        DVTARGETDEVICE* ptd,
        HDC hdcTargetDev,
        HDC hdcDraw,
        RECTL* lprcBounds,
        RECTL* lprcWBounds,
        nint pfnContinue,
        nuint dwContinue)
        => ((IViewObject.Interface)this).Draw(
            dwDrawAspect,
            lindex,
            pvAspect,
            ptd,
            hdcTargetDev,
            hdcDraw,
            lprcBounds,
            lprcWBounds,
            pfnContinue,
            dwContinue);

    HRESULT IViewObject2.Interface.GetColorSet(
        DVASPECT dwDrawAspect,
        int lindex,
        void* pvAspect,
        DVTARGETDEVICE* ptd,
        HDC hdcTargetDev,
        LOGPALETTE** ppColorSet)
        => ((IViewObject.Interface)this).GetColorSet(dwDrawAspect, lindex, pvAspect, ptd, hdcTargetDev, ppColorSet);

    HRESULT IViewObject2.Interface.Freeze(DVASPECT dwDrawAspect, int lindex, void* pvAspect, uint* pdwFreeze)
        => ((IViewObject.Interface)this).Freeze(dwDrawAspect, lindex, pvAspect, pdwFreeze);

    HRESULT IViewObject2.Interface.Unfreeze(uint dwFreeze)
        => ((IViewObject.Interface)this).Unfreeze(dwFreeze);

    HRESULT IViewObject2.Interface.SetAdvise(DVASPECT aspects, uint advf, IAdviseSink* pAdvSink)
        => ((IViewObject.Interface)this).SetAdvise(aspects, advf, pAdvSink);

    HRESULT IViewObject2.Interface.GetAdvise(uint* pAspects, uint* pAdvf, IAdviseSink** ppAdvSink)
        => ((IViewObject.Interface)this).GetAdvise(pAspects, pAdvf, ppAdvSink);

    HRESULT IViewObject2.Interface.GetExtent(DVASPECT dwDrawAspect, int lindex, DVTARGETDEVICE* ptd, SIZE* lpsizel)
        => ((IOleObject.Interface)this).GetExtent((DVASPECT)dwDrawAspect, lpsizel);
}
