<%@ Page Title="Gate Security Check" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeFile="Gate.aspx.vb" Inherits="GatePage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        /* Print Media styling */
        @media print {
            body * {
                visibility: hidden;
            }
            #printArea, #printArea * {
                visibility: visible;
            }
            #printArea {
                position: absolute;
                left: 0;
                top: 0;
                width: 100%;
                color: black !important;
                background-color: white !important;
            }
            .no-print {
                display: none !important;
            }
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="space-y-6 page-enter font-sans max-w-7xl mx-auto no-print">
        <!-- Header banner -->
        <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4 rounded-xl bg-slate-900 border border-slate-800 text-white p-6 shadow-md shadow-slate-900/10">
            <div class="flex items-center gap-4">
                <div class="bg-white rounded-lg p-1">
                    <img src="/iocl-logo.gif" alt="IOCL" class="h-10 w-auto" style="object-fit: contain; mix-blend-mode: multiply;" />
                </div>
                <div>
                    <h1 class="text-lg font-bold tracking-wide uppercase">Gate Access Terminal</h1>
                    <p class="text-xs text-slate-400 mt-1">
                        Panipat Refinery computerized gateway validity checker. Scan QR or look up plate number.
                    </p>
                </div>
            </div>
            <div class="text-right">
                <span class="text-[10px] font-bold text-orange-500 uppercase tracking-widest bg-orange-950/20 border border-orange-900/40 rounded px-2.5 py-1">
                    Security Ops Gate-3
                </span>
            </div>
        </div>

        <!-- Main Grid Workspace -->
        <div class="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
            
            <!-- Left: Search & Scanner Panel -->
            <div class="lg:col-span-5 bg-white border border-slate-200 rounded-xl p-5 shadow-sm space-y-6">
                <div>
                    <h2 class="text-xs font-bold uppercase tracking-widest text-slate-400 mb-3">Lookup Vehicle Details</h2>
                    
                    <!-- Manual Search bar -->
                    <div class="space-y-2">
                        <label class="text-[10px] font-bold text-slate-500 uppercase tracking-widest block font-sans">Plate Registration Number</label>
                        <div class="flex gap-2">
                            <div class="relative flex-1">
                                <span class="absolute left-3 top-2.5 text-slate-400">
                                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                                </span>
                                <asp:TextBox ID="txtPlateCheck" runat="server" placeholder="e.g. HR06FS1000" CssClass="w-full rounded border border-slate-200 py-1.5 pl-9 pr-3 text-xs text-slate-800 focus:outline-none focus:ring-1 focus:ring-blue-500 font-mono font-bold uppercase"></asp:TextBox>
                            </div>
                            <asp:Button ID="btnCheck" runat="server" Text="Search" OnClick="btnCheck_Click" CssClass="rounded bg-blue-600 hover:bg-blue-700 disabled:opacity-50 px-4 py-1.5 text-xs font-bold text-white shadow cursor-pointer transition-colors" />
                        </div>
                    </div>
                </div>

                <div class="border-t border-slate-100 pt-5">
                    <div class="flex items-center justify-between mb-3">
                        <h2 class="text-xs font-bold uppercase tracking-widest text-slate-400">Camera QR Scanner</h2>
                        <button type="button" id="btn-toggle-scanner" onclick="toggleScanner()" class="flex items-center gap-1 text-[10px] font-bold uppercase tracking-widest text-blue-600 hover:text-blue-700 focus:outline-none">
                            <span>Start Camera</span>
                        </button>
                    </div>

                    <!-- Webcam viewfinder container -->
                    <div class="relative overflow-hidden rounded-lg bg-slate-950 border border-slate-800 flex flex-col items-center justify-center aspect-video shadow-inner">
                        <!-- Active scanner element -->
                        <div id="scanner-active-container" class="hidden w-full h-full relative">
                            <video id="gate-viewfinder" class="w-full h-full object-cover" autoplay playsinline></video>
                            <canvas id="gate-canvas" style="display: none;"></canvas>
                            <div class="absolute inset-4 pointer-events-none border border-dashed border-orange-500/50 rounded flex items-center justify-center">
                                <div class="h-32 w-32 border-2 border-orange-500 rounded relative">
                                    <div class="absolute inset-x-0 top-1/2 h-0.5 bg-red-500 animate-bounce"></div>
                                </div>
                            </div>
                        </div>

                        <!-- Offline scanner element -->
                        <div id="scanner-offline-container" class="p-6 text-center text-slate-500">
                            <svg class="h-10 w-10 mx-auto text-slate-700 mb-2" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h.01M16 20h4M4 12h4m0 0l-2-2m2 2l-2 2" />
                            </svg>
                            <p class="text-[10px] font-bold uppercase tracking-widest text-slate-500">Camera Scanner Offline</p>
                            <p class="text-[9px] text-slate-600 mt-1 max-w-[200px] mx-auto">Click "Start Camera" to scan QR code on vehicle passes.</p>
                        </div>
                    </div>

                    <div id="scanner-error-message" class="hidden mt-3 rounded bg-red-50 border border-red-200 text-red-600 text-[10px] p-2 font-semibold font-mono"></div>
                </div>
            </div>

            <!-- Right: Verification Details & Action Log Panel -->
            <div class="lg:col-span-7">
                <asp:Panel ID="pnlAwaiting" runat="server" CssClass="bg-white border border-slate-200 rounded-xl p-16 text-center text-slate-400 shadow-sm border-dashed">
                    <svg class="h-16 w-16 mx-auto mb-4 text-slate-200" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.952 11.952 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                    </svg>
                    <h3 class="text-sm font-bold text-slate-500 uppercase tracking-widest">Awaiting Verification Scan</h3>
                    <p class="text-xs text-slate-400 mt-1 max-w-sm mx-auto">Please input a vehicle registration plate number or trigger the QR scanner camera.</p>
                </asp:Panel>

                <!-- Vehicle Details Panel -->
                <asp:Panel ID="pnlClearance" runat="server" CssClass="bg-white border border-slate-200 rounded-xl shadow-sm overflow-hidden flex flex-col" Visible="false">
                    <!-- Dynamic Gate Clearance Badge -->
                    <asp:Panel ID="pnlClearedBadge" runat="server" CssClass="bg-emerald-500 text-white px-6 py-5 flex items-center gap-4">
                        <div class="h-12 w-12 rounded-full bg-white/20 flex items-center justify-center shrink-0">
                            <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.952 11.952 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" /></svg>
                        </div>
                        <div>
                            <h2 class="text-base font-extrabold tracking-wider uppercase leading-none">CLEARANCE GRANTED</h2>
                            <p class="text-[10px] font-bold uppercase tracking-wider text-emerald-100 mt-1">Vehicle fully compliant. Clear to enter refinery premises.</p>
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlDeniedBadge" runat="server" CssClass="bg-red-500 text-white px-6 py-5 flex items-center gap-4">
                        <div class="h-12 w-12 rounded-full bg-white/20 flex items-center justify-center shrink-0">
                            <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
                        </div>
                        <div>
                            <h2 class="text-base font-extrabold tracking-wider uppercase leading-none">ENTRY DENIED</h2>
                            <p class="text-[10px] font-bold uppercase tracking-wider text-red-100 mt-1">Critical warning or expired compliance licenses. Do NOT allow access.</p>
                        </div>
                    </asp:Panel>

                    <!-- Vehicle details -->
                    <div class="p-6 border-b border-slate-100 grid grid-cols-2 gap-x-6 gap-y-4">
                        <div>
                            <span class="text-[10px] font-bold uppercase text-slate-400 tracking-wider">Registration Number</span>
                            <div class="text-sm font-black text-slate-800 font-mono tracking-tight mt-0.5"><asp:Label ID="lblVehPlate" runat="server"></asp:Label></div>
                        </div>
                        <div>
                            <span class="text-[10px] font-bold uppercase text-slate-400 tracking-wider">Category</span>
                            <div class="text-sm font-semibold text-slate-700 mt-0.5"><asp:Label ID="lblVehType" runat="server"></asp:Label></div>
                        </div>
                        <div>
                            <span class="text-[10px] font-bold uppercase text-slate-400 tracking-wider">Driver Name</span>
                            <div class="text-sm font-semibold text-slate-700 mt-0.5"><asp:Label ID="lblDriverName" runat="server"></asp:Label></div>
                        </div>
                        <div>
                            <span class="text-[10px] font-bold uppercase text-slate-400 tracking-wider">Vendor / Contractor</span>
                            <div class="text-sm font-semibold text-slate-700 mt-0.5"><asp:Label ID="lblVendorName" runat="server"></asp:Label></div>
                        </div>
                        <div class="col-span-2">
                            <span class="text-[10px] font-bold uppercase text-slate-400 tracking-wider">Department Scope</span>
                            <div class="text-sm font-bold text-slate-800 mt-0.5"><asp:Label ID="lblDeptName" runat="server"></asp:Label></div>
                        </div>
                    </div>

                    <!-- Checklist -->
                    <div class="p-6 border-b border-slate-100 space-y-3">
                        <h3 class="text-[10px] font-extrabold text-slate-400 uppercase tracking-widest">Compliance licenses checklist</h3>
                        
                        <asp:Repeater ID="rptCompliance" runat="server">
                            <HeaderTemplate>
                                <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
                            </HeaderTemplate>
                            <ItemTemplate>
                                <div class="flex items-center justify-between border border-slate-200 rounded p-2.5 bg-slate-50/50">
                                    <div class="flex items-center gap-2">
                                        <span class='h-2 w-2 rounded-full shrink-0 <%# GetDotColor(Eval("Status").ToString()) %>'></span>
                                        <div>
                                            <h4 class="text-[10px] font-bold text-slate-800 uppercase tracking-wide">
                                                <%# Eval("LicenseType").ToString().Replace("_", " ") %>
                                            </h4>
                                            <p class="text-[8.5px] text-slate-500 font-mono mt-0.5"># <%# If(String.IsNullOrEmpty(Eval("LicenseNumber").ToString()), "PENDING", Eval("LicenseNumber").ToString()) %></p>
                                        </div>
                                    </div>
                                    <div class="text-right text-[8.5px] font-bold font-mono">
                                        <span class='<%# GetStatusTextColor(Eval("Status").ToString()) %>'><%# Eval("Status").ToString().Replace("_", " ") %></span>
                                        <p class="text-slate-450 text-[7.5px] mt-0.5">EXP: <%# FmtDate(Eval("ExpiryDate")) %></p>
                                    </div>
                                </div>
                            </ItemTemplate>
                            <FooterTemplate>
                                </div>
                            </FooterTemplate>
                        </asp:Repeater>
                    </div>

                    <!-- Entry Log Decision Block -->
                    <div class="p-6 bg-slate-50/50 space-y-4">
                        <div>
                            <label class="text-[10px] font-bold text-slate-500 uppercase tracking-widest block mb-1">Gate Entry Remarks / Notes</label>
                            <asp:TextBox ID="txtRemarks" runat="server" TextMode="MultiLine" Rows="2" placeholder="Input any gate notes, e.g. driver seal checked, cargo inspection remarks..." CssClass="w-full rounded border border-slate-200 py-1.5 px-3 text-xs text-slate-800 focus:outline-none focus:ring-1 focus:ring-blue-500 font-medium"></asp:TextBox>
                        </div>

                        <div class="flex items-center gap-3 justify-end flex-wrap">
                            <asp:Button ID="btnPrint" runat="server" Text="Print Gate Pass" OnClick="btnPrint_Click" CssClass="rounded border border-slate-250 hover:bg-slate-50 px-4 py-2 text-xs font-bold text-slate-500 cursor-pointer transition-colors focus:outline-none" />
                            <asp:Button ID="btnClear" runat="server" Text="Reset Terminal" OnClick="btnClear_Click" CssClass="rounded border border-slate-250 hover:bg-slate-50 px-4 py-2 text-xs font-bold text-slate-500 cursor-pointer transition-colors focus:outline-none" />
                            <asp:Button ID="btnDeny" runat="server" Text="Deny Premises Access" OnClick="btnDeny_Click" CssClass="rounded bg-red-600 hover:bg-red-700 px-5 py-2 text-xs font-bold text-white shadow shadow-red-600/10 cursor-pointer transition-colors focus:outline-none" />
                            <asp:Button ID="btnAllow" runat="server" Text="Allow Entry Clearance" OnClick="btnAllow_Click" CssClass="rounded bg-emerald-600 hover:bg-emerald-700 px-5 py-2 text-xs font-bold text-white shadow shadow-emerald-600/10 cursor-pointer transition-colors focus:outline-none" />
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </div>

    <!-- Hidden Fields & Scanner Postback Target -->
    <asp:HiddenField ID="hdnScannedVehicleId" runat="server" />
    <asp:HiddenField ID="hdnVehicleId" runat="server" />
    <asp:HiddenField ID="hdnDepartmentId" runat="server" />
    <asp:Button ID="btnSearchScanned" runat="server" Style="display:none;" OnClick="btnSearchScanned_Click" />

    <!-- Printable Pass Template (hidden normally, visible under @media print) -->
    <div id="printArea" style="display:none; color:black; background-color:white; padding: 2rem; border: 2px solid black; font-family: sans-serif; max-width: 600px; margin: 0 auto;">
        <div style="text-align:center; border-bottom: 3px double black; padding-bottom: 1rem; margin-bottom: 1.5rem;">
            <h2>INDIAN OIL CORPORATION LIMITED</h2>
            <h3>Panipat Refinery - Gate Entry Pass</h3>
            <span style="font-size:0.85rem; font-style:italic;">Safety Clearance Certificate</span>
        </div>

        <div style="margin-bottom: 1.5rem; font-size: 1.1rem; line-height: 1.6;">
            <p><strong>Gate Pass Status:</strong> <span style="font-weight:bold; text-transform:uppercase;" id="printStatus">APPROVED</span></p>
            <p><strong>Vehicle Number:</strong> <span id="printPlate">HR26AB1101</span></p>
            <p><strong>Vehicle Type:</strong> <span id="printType">Petroleum Tanker</span></p>
            <p><strong>Driver Name:</strong> <span id="printDriver">John Doe</span></p>
            <p><strong>Contractor / Vendor:</strong> <span id="printVendor">Refinery Transport Corp</span></p>
            <p><strong>Refinery Department:</strong> <span id="printDept">PR - Fire & Safety</span></p>
            <p><strong>Validation Timestamp:</strong> <span id="printTimestamp"><%= DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss") %></span></p>
        </div>

        <h4 style="border-bottom: 1px solid black; padding-bottom: 0.3rem; margin-bottom: 0.8rem;">Compliance Licenses Status</h4>
        <table style="width:100%; border-collapse:collapse; font-size: 0.9rem;" id="printTable">
            <thead>
                <tr style="border-bottom: 1px solid black;">
                    <th style="text-align:left; padding: 0.4rem;">Document Type</th>
                    <th style="text-align:left; padding: 0.4rem;">License Number</th>
                    <th style="text-align:left; padding: 0.4rem;">Expiry Date</th>
                    <th style="text-align:left; padding: 0.4rem;">Status</th>
                </tr>
            </thead>
            <tbody>
                <!-- Populated via Javascript -->
            </tbody>
        </table>

        <div style="margin-top: 4rem; display: flex; justify-content: space-between; font-size: 0.9rem;">
            <div style="text-align:center; width: 200px; border-top: 1px solid black; padding-top: 0.4rem;">
                Security Inspector Sign
            </div>
            <div style="text-align:center; width: 200px; border-top: 1px solid black; padding-top: 0.4rem;">
                Refinery Gate Officer Sign
            </div>
        </div>
    </div>

    <!-- Script to trigger print, fill printable layout, and handle scanner -->
    <script>
        function triggerPrintPass(plate, type, driver, vendor, dept, status, docsJson) {
            document.getElementById('printPlate').innerText = plate;
            document.getElementById('printType').innerText = type;
            document.getElementById('printDriver').innerText = driver;
            document.getElementById('printVendor').innerText = vendor;
            document.getElementById('printDept').innerText = dept;
            document.getElementById('printStatus').innerText = status;
            
            const tbody = document.querySelector('#printTable tbody');
            tbody.innerHTML = "";
            
            const docs = JSON.parse(docsJson);
            docs.forEach(d => {
                const tr = document.createElement('tr');
                tr.style.borderBottom = "1px solid #ddd";
                tr.innerHTML = `
                    <td style="padding:0.4rem;">${d.Type}</td>
                    <td style="padding:0.4rem;">${d.Number || 'PENDING'}</td>
                    <td style="padding:0.4rem;">${d.Expiry || 'PENDING'}</td>
                    <td style="padding:0.4rem; font-weight:bold;">${d.Status}</td>
                `;
                tbody.appendChild(tr);
            });

            // Show printing layout block temporarily, trigger print, hide it
            const area = document.getElementById('printArea');
            area.style.display = "block";
            window.print();
            area.style.display = "none";
        }

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
                        // Expected URL pattern matches /verify/vehicle/{id} or /verify/{id}
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

        let scanning = false;
        function toggleScanner() {
            const btn = document.getElementById('btn-toggle-scanner');
            const activeContainer = document.getElementById('scanner-active-container');
            const offlineContainer = document.getElementById('scanner-offline-container');
            const errDiv = document.getElementById('scanner-error-message');
            
            if (scanning) {
                // Stop scanning
                scanning = false;
                btn.innerHTML = `<span>Start Camera</span>`;
                btn.className = "flex items-center gap-1 text-[10px] font-bold uppercase tracking-widest text-blue-600 hover:text-blue-700 focus:outline-none";
                activeContainer.classList.add('hidden');
                offlineContainer.classList.remove('hidden');
                window.qrScanner.stop();
            } else {
                // Start scanning
                scanning = true;
                btn.innerHTML = `<span class="flex h-2 w-2 rounded-full bg-red-500 animate-pulse mr-1"></span><span>Stop Camera</span>`;
                btn.className = "flex items-center gap-1 text-[10px] font-bold uppercase tracking-widest text-red-500 hover:text-red-600 focus:outline-none";
                activeContainer.classList.remove('hidden');
                offlineContainer.classList.add('hidden');
                errDiv.classList.add('hidden');
                
                window.qrScanner.start('gate-viewfinder', 'gate-canvas', {
                    invokeMethodAsync: function(methodName, arg1) {
                        if (methodName === 'OnQrCodeScanned') {
                            // Set scanned vehicle ID and trigger postback
                            document.getElementById('<%= hdnScannedVehicleId.ClientID %>').value = arg1;
                            document.getElementById('<%= btnSearchScanned.ClientID %>').click();
                        } else if (methodName === 'OnCameraError') {
                            errDiv.innerText = arg1;
                            errDiv.classList.remove('hidden');
                            // Reset scanner UI
                            scanning = false;
                            btn.innerHTML = `<span>Start Camera</span>`;
                            btn.className = "flex items-center gap-1 text-[10px] font-bold uppercase tracking-widest text-blue-600 hover:text-blue-700 focus:outline-none";
                            activeContainer.classList.add('hidden');
                            offlineContainer.classList.remove('hidden');
                        }
                    }
                });
            }
        }
    </script>
</asp:Content>
