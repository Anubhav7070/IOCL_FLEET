<%@ Page Title="Refinery Departments" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeFile="Departments.aspx.vb" Inherits="DepartmentsPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="space-y-6 page-enter font-sans max-w-7xl mx-auto">
        <!-- Header Block -->
        <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 rounded-xl bg-slate-900 border border-slate-800 text-white p-6 shadow-md shadow-slate-900/10">
            <div>
                <h2 class="text-xl font-bold tracking-wide uppercase">Refinery Divisions & Departments</h2>
                <p class="text-xs text-slate-400 mt-1">
                    Manage divisions ownership profiles and monitor compliance index scores.
                </p>
            </div>

            <% If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then %>
                <asp:Button ID="btnAddDept" runat="server" Text="Add Department" OnClick="btnAddDept_Click" CssClass="flex items-center gap-1.5 rounded bg-blue-600 hover:bg-blue-700 px-4 py-2.5 text-xs font-bold text-white shadow transition-all duration-200 cursor-pointer" />
            <% End If %>
        </div>

        <!-- Departments Grid -->
        <asp:Repeater ID="rptDepts" runat="server">
            <HeaderTemplate>
                <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 font-sans">
            </HeaderTemplate>
            <ItemTemplate>
                <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm flex flex-col justify-between">
                    <div>
                        <div class="flex items-start justify-between gap-3">
                            <div class="flex items-center gap-3">
                                <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded bg-slate-50 border border-slate-100 text-slate-600">
                                    <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" /></svg>
                                </div>
                                <div class="min-w-0">
                                    <h3 class="text-xs font-bold text-slate-800 uppercase tracking-wider leading-tight truncate"><%# Eval("Name") %></h3>
                                    <span class="mt-1.5 inline-block rounded bg-slate-100 px-1.5 py-0.5 text-[8.5px] font-bold text-slate-500 tracking-wider">
                                        <%# Eval("Code") %>
                                    </span>
                                </div>
                            </div>

                            <!-- Compliance Score Gauge -->
                            <div class='rounded border px-2 py-1 text-center shrink-0 <%# GetScoreStyle(Convert.ToDouble(Eval("ComplianceScore"))) %>'>
                                <p class="text-[7px] font-bold uppercase tracking-wider leading-none">Score</p>
                                <p class="mt-0.5 text-xs font-black leading-none"><%# Eval("ComplianceScore", "{0:F0}") %>%</p>
                            </div>
                        </div>

                        <p class="mt-4 text-[11px] text-slate-500 leading-relaxed font-medium line-clamp-2">
                            <%# If(String.IsNullOrEmpty(Eval("Description").ToString()), "No division description provided.", Eval("Description")) %>
                        </p>
                    </div>

                    <!-- Actions -->
                    <div class="mt-5 pt-3 border-t border-slate-100 flex items-center justify-between gap-3">
                        <asp:LinkButton ID="lnkScan" runat="server" CommandArgument='<%# Eval("Id") %>' OnClick="lnkScan_Click" class="flex items-center gap-1.5 rounded-lg border border-orange-200 bg-orange-50 hover:bg-orange-100 px-3.5 py-1.5 text-[9.5px] font-bold text-orange-600 uppercase tracking-wider transition-all focus:outline-none">
                            <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h.01M16 16h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                            <span>Scan Gate QR</span>
                        </asp:LinkButton>

                        <div class="flex items-center gap-2">
                            <% If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then %>
                                <asp:LinkButton ID="lnkEdit" runat="server" CommandArgument='<%# Eval("Id") %>' OnClick="lnkEdit_Click" class="rounded border border-slate-200 hover:bg-slate-50 p-1.5 text-slate-500 hover:text-slate-700 focus:outline-none" title="Edit Department">
                                    <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" /></svg>
                                </asp:LinkButton>
                                <asp:LinkButton ID="lnkDelete" runat="server" CommandArgument='<%# Eval("Id") %>' OnClick="lnkDelete_Click" OnClientClick="return confirm('Are you sure you want to delete this department? All associations must be empty.');" class="rounded border border-red-100 hover:bg-red-50 p-1.5 text-red-500 hover:text-red-700 focus:outline-none" title="Delete Department">
                                    <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-4v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                                </asp:LinkButton>
                            <% End If %>
                        </div>
                    </div>
                </div>
            </ItemTemplate>
            <FooterTemplate>
                </div>
            </FooterTemplate>
        </asp:Repeater>

        <!-- No departments template -->
        <asp:Panel ID="pnlNoDepts" runat="server" CssClass="rounded-xl border border-dashed border-slate-350 bg-white py-16 text-center text-slate-400" Visible="false">
            <svg class="h-12 w-12 mx-auto mb-3 opacity-30" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" /></svg>
            <p class="text-sm font-bold text-slate-500">No departments found.</p>
        </asp:Panel>

        <!-- Create / Edit Modal -->
        <asp:Panel ID="pnlModal" runat="server" CssClass="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4" Visible="false">
            <div class="relative w-full max-w-md rounded-xl border border-slate-200 bg-white p-6 shadow-2xl">
                <h3 class="text-sm font-bold text-slate-800 uppercase tracking-wider border-b border-slate-100 pb-3">
                    <asp:Label ID="lblFormTitle" runat="server" Text="Create Department"></asp:Label>
                </h3>
                
                <div class="mt-4 space-y-4">
                    <asp:HiddenField ID="hdnDeptId" runat="server" />

                    <div>
                        <label class="text-[10px] font-bold text-slate-500 uppercase tracking-widest block mb-1">Department Name *</label>
                        <asp:TextBox ID="txtDeptName" runat="server" placeholder="e.g. Safety & Emergency Services" CssClass="w-full rounded-md border border-slate-200 py-2 px-3 text-xs text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-blue-500"></asp:TextBox>
                    </div>

                    <div>
                        <label class="text-[10px] font-bold text-slate-500 uppercase tracking-widest block mb-1">Department Code *</label>
                        <asp:TextBox ID="txtDeptCode" runat="server" placeholder="e.g. PN-SF-SAFE" CssClass="w-full rounded-md border border-slate-200 py-2 px-3 text-xs text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-blue-500 uppercase"></asp:TextBox>
                    </div>

                    <div>
                        <label class="text-[10px] font-bold text-slate-500 uppercase tracking-widest block mb-1">Division *</label>
                        <asp:TextBox ID="txtDivision" runat="server" placeholder="e.g. Panipat Refinery" CssClass="w-full rounded-md border border-slate-200 py-2 px-3 text-xs text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-blue-500"></asp:TextBox>
                    </div>

                    <div>
                        <label class="text-[10px] font-bold text-slate-500 uppercase tracking-widest block mb-1">Description</label>
                        <asp:TextBox ID="txtDescription" runat="server" TextMode="MultiLine" Rows="3" placeholder="Describe division responsibilities..." CssClass="w-full rounded-md border border-slate-200 py-2 px-3 text-xs text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-blue-500"></asp:TextBox>
                    </div>

                    <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100">
                        <asp:Button ID="btnCancel" runat="server" Text="Cancel" OnClick="btnCancel_Click" CssClass="rounded border border-slate-200 hover:bg-slate-50 px-4 py-2 text-xs font-bold text-slate-500 cursor-pointer transition-colors" />
                        <asp:Button ID="btnSave" runat="server" Text="Save" OnClick="btnSave_Click" CssClass="rounded bg-blue-600 hover:bg-blue-700 px-4 py-2 text-xs font-bold text-white shadow shadow-blue-600/10 cursor-pointer transition-colors" />
                    </div>
                </div>
            </div>
        </asp:Panel>

        <!-- QR Scanner Modal Overlay -->
        <asp:Panel ID="pnlScannerModal" runat="server" CssClass="fixed inset-0 z-50 flex items-center justify-center bg-black/75 backdrop-blur-sm p-4" Visible="false">
            <div class="relative w-full max-w-sm rounded-2xl bg-slate-900 border border-slate-800 shadow-2xl overflow-hidden p-6">
                <!-- Scanner Header -->
                <div class="flex items-center justify-between border-b border-slate-800 pb-3.5 mb-4">
                    <div class="flex items-center gap-2.5">
                        <svg class="h-5 w-5 text-orange-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h.01M16 16h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                        <div>
                            <p class="text-[10px] font-extrabold text-slate-300 uppercase tracking-widest">QR Gate Scanner</p>
                            <p class="text-[8.5px] text-orange-400 font-bold uppercase tracking-wide"><asp:Label ID="lblScannerDeptName" runat="server"></asp:Label> (<asp:Label ID="lblScannerDeptCode" runat="server"></asp:Label>)</p>
                        </div>
                    </div>
                    <asp:LinkButton ID="lnkCloseScanner" runat="server" OnClick="lnkCloseScanner_Click" class="rounded-full p-1.5 hover:bg-slate-800 text-slate-400 hover:text-white transition-colors focus:outline-none">
                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                    </asp:LinkButton>
                </div>

                <div id="scanner-error-msg" class="hidden rounded-xl bg-red-950/40 border border-red-900/50 text-red-400 text-xs p-4 mb-4 text-center leading-relaxed"></div>

                <!-- Scanner Camera Viewfinder -->
                <asp:Panel ID="pnlScannerCam" runat="server" CssClass="relative rounded-xl overflow-hidden bg-black border border-slate-800 aspect-video flex items-center justify-center">
                    <video id="dept-scan-video" class="absolute w-full h-full object-cover" autoplay playsinline></video>
                    <canvas id="dept-scan-canvas" class="hidden"></canvas>

                    <div class="absolute inset-0 border-[24px] border-black/50 flex items-center justify-center pointer-events-none">
                        <div class="w-32 h-32 border border-dashed border-orange-500 animate-pulse relative">
                            <div class="absolute -top-1 -left-1 w-4.5 h-4.5 border-t-4 border-l-4 border-orange-500"></div>
                            <div class="absolute -top-1 -right-1 w-4.5 h-4.5 border-t-4 border-r-4 border-orange-500"></div>
                            <div class="absolute -bottom-1 -left-1 w-4.5 h-4.5 border-b-4 border-l-4 border-orange-500"></div>
                            <div class="absolute -bottom-1 -right-1 w-4.5 h-4.5 border-b-4 border-r-4 border-orange-500"></div>
                        </div>
                    </div>

                    <span class="absolute bottom-2 rounded bg-slate-950/80 px-2 py-1 text-[8px] text-white font-bold tracking-wider uppercase">
                        Align QR Code In Frame
                    </span>
                </asp:Panel>

                <!-- Scan Verification Results -->
                <asp:Panel ID="pnlScannerResult" runat="server" CssClass="space-y-4" Visible="false">
                    <!-- Clearance Badges -->
                    <asp:Panel ID="pnlScanCleared" runat="server" CssClass="rounded-xl border border-emerald-900/40 bg-emerald-950/30 p-4 text-center">
                        <svg class="h-10 w-10 text-emerald-400 mx-auto mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.952 11.952 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" /></svg>
                        <p class="text-xs font-extrabold text-emerald-300 uppercase tracking-widest leading-none">Clearance Granted</p>
                        <p class="text-[9.5px] text-emerald-600 font-semibold mt-1">Vehicle OK to enter department gate premises.</p>
                    </asp:Panel>

                    <asp:Panel ID="pnlScanDenied" runat="server" CssClass="rounded-xl border border-red-950/40 bg-red-950/30 p-4 text-center">
                        <svg class="h-10 w-10 text-red-500 mx-auto mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
                        <p class="text-xs font-extrabold text-red-400 uppercase tracking-widest leading-none">Entry Denied</p>
                        <p class="text-[9.5px] text-red-500 font-semibold mt-1">Expired or missing compliance certificates</p>
                    </asp:Panel>

                    <!-- Details Card -->
                    <div class="rounded-lg bg-slate-850 border border-slate-800 divide-y divide-slate-800 text-[10px]">
                        <div class="flex justify-between px-3 py-2">
                            <span class="text-slate-500 font-bold uppercase tracking-wider">Reg. No</span>
                            <span class="font-bold text-slate-200 font-mono"><asp:Label ID="lblScanPlate" runat="server"></asp:Label></span>
                        </div>
                        <div class="flex justify-between px-3 py-2">
                            <span class="text-slate-500 font-bold uppercase tracking-wider">Type</span>
                            <span class="font-bold text-slate-200"><asp:Label ID="lblScanType" runat="server"></asp:Label></span>
                        </div>
                        <div class="flex justify-between px-3 py-2">
                            <span class="text-slate-500 font-bold uppercase tracking-wider">Driver</span>
                            <span class="font-bold text-slate-200"><asp:Label ID="lblScanDriver" runat="server"></asp:Label></span>
                        </div>
                        <div class="flex justify-between px-3 py-2">
                            <span class="text-slate-500 font-bold uppercase tracking-wider">Vendor</span>
                            <span class="font-bold text-slate-200"><asp:Label ID="lblScanVendor" runat="server"></asp:Label></span>
                        </div>
                    </div>

                    <!-- Checklist -->
                    <div class="space-y-1.5">
                        <p class="text-[8.5px] font-bold text-slate-500 uppercase tracking-widest">Safety Checklist</p>
                        
                        <asp:Repeater ID="rptScanChecklist" runat="server">
                            <ItemTemplate>
                                <div class="flex items-center justify-between rounded bg-slate-850/60 border border-slate-800/80 px-3 py-1.5 text-[10px]">
                                    <div class="flex items-center gap-2">
                                        <span class='h-2 w-2 rounded-full <%# If(Eval("Status").ToString() = "ACTIVE", "bg-emerald-500", If(Eval("Status").ToString() = "WARNING", "bg-amber-500", "bg-red-500")) %>'></span>
                                        <span class="font-bold text-slate-300 uppercase"><%# Eval("LicenseType").ToString().Replace("_", " ") %></span>
                                    </div>
                                    <span class="text-[8.5px] text-slate-500 font-mono">EXP: <%# FmtDate(Eval("ExpiryDate")) %></span>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                    <asp:Button ID="btnResetScan" runat="server" Text="Scan Another Vehicle" OnClick="btnResetScan_Click" CssClass="w-full rounded-lg bg-orange-500 hover:bg-orange-600 px-4 py-2 text-[10px] font-bold text-white uppercase tracking-wider cursor-pointer transition-colors" />
                </asp:Panel>

                <asp:Button ID="btnTurnOffCam" runat="server" Text="Turn Off Camera" OnClick="lnkCloseScanner_Click" CssClass="w-full mt-5 rounded-lg border border-slate-800 bg-slate-850 hover:bg-slate-850 text-slate-400 py-2.5 text-xs font-bold cursor-pointer transition-colors" />
            </div>
        </asp:Panel>
    </div>

    <!-- Hidden Fields & Postbacks -->
    <asp:HiddenField ID="hdnScannerDeptId" runat="server" />
    <asp:HiddenField ID="hdnScannedVehicleId" runat="server" />
    <asp:Button ID="btnProcessScan" runat="server" Style="display:none;" OnClick="btnProcessScan_Click" />

    <!-- Scanner script execution -->
    <script>
        window.qrScanner = {
            stream: null,
            video: null,
            canvas: null,
            animationFrameId: null,
            dotNetHelper: null,

            start: async function (videoElementId, canvasElementId, dotNetHelper) {
                this.dotNetHelper = dotNetHelper;
                this.video = document.getElementById(videoElementId);
                this.canvas = document.getElementById(canvasElementId);

                if (!this.video || !this.canvas) return;

                if (!window.jsQR) {
                    await new Promise((resolve) => {
                        const script = document.createElement('script');
                        script.src = 'https://cdn.jsdelivr.net/npm/jsqr@1.4.0/dist/jsQR.js';
                        script.onload = resolve;
                        document.head.appendChild(script);
                    });
                }

                try {
                    this.stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } });
                    this.video.srcObject = this.stream;
                    this.video.setAttribute('playsinline', 'true');
                    await this.video.play();
                    this.tick();
                } catch (err) {
                    console.error('[QRScanner] Camera access error:', err);
                    this.dotNetHelper.invokeMethodAsync('OnCameraError', 'Camera access denied. Please allow camera permissions and try again.');
                }
            },

            tick: function () {
                if (!this.video || !this.canvas || !window.jsQR) return;

                if (this.video.readyState === this.video.HAVE_ENOUGH_DATA) {
                    this.canvas.height = this.video.videoHeight;
                    this.canvas.width = this.video.videoWidth;
                    const ctx = this.canvas.getContext('2d');
                    ctx.drawImage(this.video, 0, 0, this.canvas.width, this.canvas.height);
                    const imageData = ctx.getImageData(0, 0, this.canvas.width, this.canvas.height);
                    const code = window.jsQR(imageData.data, imageData.width, imageData.height, { inversionAttempts: 'dontInvert' });

                    if (code) {
                        const match = code.data.match(/\/verify\/(?:vehicle\/)?(\d+)/);
                        if (match) {
                            const vehicleId = parseInt(match[1]);
                            this.stop();
                            this.dotNetHelper.invokeMethodAsync('OnQrCodeScanned', vehicleId);
                            return;
                        }
                    }
                }
                this.animationFrameId = requestAnimationFrame(this.tick.bind(this));
            },

            stop: function () {
                if (this.animationFrameId) {
                    cancelAnimationFrame(this.animationFrameId);
                    this.animationFrameId = null;
                }
                if (this.stream) {
                    this.stream.getTracks().forEach(t => t.stop());
                    this.stream = null;
                }
                if (this.video) {
                    this.video.srcObject = null;
                }
            }
        };

        function startDeptCameraScanner() {
            const errDiv = document.getElementById('scanner-error-msg');
            if (errDiv) errDiv.classList.add('hidden');
            
            window.qrScanner.start('dept-scan-video', 'dept-scan-canvas', {
                invokeMethodAsync: function(methodName, arg1) {
                    if (methodName === 'OnQrCodeScanned') {
                        document.getElementById('<%= hdnScannedVehicleId.ClientID %>').value = arg1;
                        document.getElementById('<%= btnProcessScan.ClientID %>').click();
                    } else if (methodName === 'OnCameraError') {
                        if (errDiv) {
                            errDiv.innerText = arg1;
                            errDiv.classList.remove('hidden');
                        }
                    }
                }
            });
        }

        function stopDeptCameraScanner() {
            window.qrScanner.stop();
        }
    </script>
</asp:Content>
