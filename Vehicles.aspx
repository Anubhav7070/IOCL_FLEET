<%@ Page Title="Fleet Management" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeFile="Vehicles.aspx.vb" Inherits="VehiclesPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="space-y-6">
        <!-- Title and Action Bar -->
        <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4 rounded-xl bg-slate-900 border border-slate-800 text-white p-6 shadow-md shadow-slate-900/10">
            <div>
                <h2 class="text-xl font-bold tracking-wide uppercase">Fleet Registry Terminal</h2>
                <p class="text-xs text-slate-400 mt-1">
                    Manage refinery-registered vehicles, compliance clearance, and gate scanning.
                </p>
            </div>

            <div class="flex flex-wrap items-center gap-3">
                <a href="Gate.aspx" class="flex items-center gap-1.5 rounded bg-orange-600 hover:bg-orange-700 hover:scale-105 active:scale-95 px-4 py-2 text-xs font-bold text-white shadow transition-all duration-200">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h.01M16 16h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                    <span>Gate Scan (Camera)</span>
                </a>
                <% Dim role As String = If(Session("Role") IsNot Nothing, Session("Role").ToString(), "")
                   If role <> "VIEWER" Then %>
                    <asp:LinkButton ID="btnOpenAdd" runat="server" OnClick="btnOpenAddModal_Click" CssClass="flex items-center gap-1.5 rounded bg-blue-600 hover:bg-blue-700 hover:scale-105 active:scale-95 px-4 py-2 text-xs font-bold text-white shadow transition-all duration-200 focus:outline-none">
                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4" /></svg>
                        <span>Register Vehicle</span>
                    </asp:LinkButton>
                <% End If %>

            </div>
        </div>

        <!-- Filter Control Panel -->
        <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm flex flex-col md:flex-row md:items-center gap-4">
            <div class="relative flex-1">
                <span class="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none text-slate-400">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                </span>
                <asp:TextBox ID="txtSearch" runat="server" OnTextChanged="FilterVehicles" AutoPostBack="true" placeholder="Search vehicle number, driver, vendor..." CssClass="w-full rounded border border-slate-200 bg-slate-50 pl-9 pr-4 py-2 text-xs font-semibold text-slate-600 placeholder-slate-400 outline-none focus:border-blue-500 focus:bg-white transition-all"></asp:TextBox>
            </div>

            <div class="flex items-center gap-3">
                <asp:DropDownList ID="ddlDeptFilter" runat="server" AutoPostBack="true" OnSelectedIndexChanged="FilterVehicles" CssClass="rounded border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-600 outline-none focus:border-blue-500 transition-all">
                </asp:DropDownList>

                <asp:DropDownList ID="ddlStatusFilter" runat="server" AutoPostBack="true" OnSelectedIndexChanged="FilterVehicles" CssClass="rounded border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-600 outline-none focus:border-blue-500 transition-all">
                    <asp:ListItem Text="All Statuses" Value=""></asp:ListItem>
                    <asp:ListItem Text="FULLY COMPLIANT" Value="FULLY_COMPLIANT"></asp:ListItem>
                    <asp:ListItem Text="WARNING" Value="WARNING"></asp:ListItem>
                    <asp:ListItem Text="CRITICAL" Value="CRITICAL"></asp:ListItem>
                    <asp:ListItem Text="EXPIRED" Value="EXPIRED"></asp:ListItem>
                </asp:DropDownList>
                
                <asp:LinkButton ID="btnClearFilters" runat="server" OnClick="btnReset_Click" CssClass="text-xs font-semibold text-slate-500 hover:text-slate-800 px-2">Reset</asp:LinkButton>
            </div>
        </div>

        <!-- Split Grid Layout -->
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6 items-start">
            <!-- Left Cards Grid (2/3 width) -->
            <div class="lg:col-span-2 space-y-6">
                <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                    <asp:Repeater ID="rptVehicles" runat="server" OnItemCommand="rptVehicles_ItemCommand">
                        <ItemTemplate>
                            <div class="rounded-xl border border-slate-200 bg-white overflow-hidden shadow-sm hover:shadow-md transition-shadow flex flex-col justify-between">
                                <div>
                                    <!-- Card Header -->
                                    <div class="flex items-center justify-between text-white p-4 font-mono <%# GetHeaderBg(Eval("OverallStatus")) %>">
                                        <div>
                                            <h3 class="text-base font-extrabold tracking-wider"><%# Eval("VehicleNumber") %></h3>
                                            <p class="text-[9px] font-bold text-white/80 uppercase tracking-widest mt-0.5"><%# Eval("VehicleType") %></p>
                                        </div>
                                        <span class="rounded-full bg-white/20 border border-white/30 px-2.5 py-0.5 text-[9px] font-extrabold uppercase tracking-widest">
                                            <%# Eval("OverallStatus").ToString().Replace("_", " ") %>
                                        </span>
                                    </div>

                                    <!-- Card Body -->
                                    <div class="p-4 space-y-4">
                                        <div class="grid grid-cols-2 gap-3 text-xs">
                                            <%# GetDriverVendorHtml(Eval("OwnershipType"), Eval("DriverName"), Eval("VendorName")) %>
                                            <div class="col-span-2 border-t border-slate-50 pt-2">
                                                <p class="text-[9px] font-bold text-slate-400 uppercase tracking-widest">Department</p>
                                                <p class="font-bold text-[#0054A6] truncate mt-0.5"><%# Eval("DeptCode") %></p>
                                            </div>
                                        </div>

                                        <!-- Compliance Indicators -->
                                        <div class="border-t border-slate-100 pt-3">
                                            <p class="text-[9px] font-bold text-slate-400 uppercase tracking-widest mb-2">Compliance Slots</p>
                                            <div class="flex flex-wrap gap-2">
                                                <%# GetComplianceSlotsHtml(Convert.ToInt32(Eval("Id"))) %>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- Card Actions -->
                                <div class="border-t border-slate-100 bg-slate-50 p-4 flex items-center justify-between gap-3 mt-auto">
                                    <asp:LinkButton ID="btnQr" runat="server" CommandName="ShowQr" CommandArgument='<%# Eval("Id") %>' CssClass="flex items-center gap-1.5 rounded border border-slate-200 bg-white hover:bg-slate-50 text-slate-600 px-3 py-1.5 text-xs font-bold transition-all focus:outline-none">
                                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h.01M16 16h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                        <span>QR Code</span>
                                    </asp:LinkButton>
                                    <div class="flex items-center gap-2">
                                        <asp:LinkButton ID="btnView" runat="server" CommandName="ViewDetails" CommandArgument='<%# Eval("Id") %>' CssClass="rounded bg-blue-50 hover:bg-blue-100 text-[#0054A6] px-3.5 py-1.5 text-xs font-bold transition-all focus:outline-none">
                                            View Details
                                        </asp:LinkButton>
                                        <% If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then %>
                                            <asp:LinkButton ID="btnDelete" runat="server" CommandName="DeleteVehicle" CommandArgument='<%# Eval("Id") %>' OnClientClick="return confirm('Are you sure you want to decommission this vehicle? This deletes all compliance history.');" CssClass="rounded bg-red-50 hover:bg-red-100 text-red-600 p-1.5 transition-all focus:outline-none">
                                                <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-4v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                                            </asp:LinkButton>
                                        <% End If %>
                                    </div>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </div>

            <!-- Right Selected Vehicle Details (1/3 width) -->
            <div class="lg:col-span-1">
                <asp:Panel ID="pnlDetails" runat="server" CssClass="rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-6" Visible="false">
                    <div class="border-b border-slate-100 pb-3 flex justify-between items-center">
                        <span class="text-xs font-extrabold text-slate-400 uppercase tracking-widest">Selected Vehicle Details</span>
                        <asp:Label ID="lblVerifiedBadge" runat="server" CssClass="rounded-full px-2 py-0.5 text-[9px] font-bold uppercase"></asp:Label>
                    </div>

                    <div class="flex flex-col items-center text-center space-y-4">
                        <div class="bg-gradient-to-br from-[#0054A6] to-blue-800 text-white w-full rounded-xl p-4 shadow shadow-blue-800/10">
                            <p class="text-xs uppercase tracking-widest font-bold opacity-80">IOCL Windshield Clearance</p>
                            <p class="text-xl font-extrabold font-mono tracking-wider mt-1"><asp:Label ID="lblPlateNumber" runat="server"></asp:Label></p>
                            <p class="text-[10px] opacity-80 mt-0.5"><asp:Label ID="lblType" runat="server"></asp:Label></p>
                        </div>

                        <!-- Mini QR windmill display -->
                        <div class="border border-slate-200 p-2.5 bg-white rounded-2xl shadow-sm">
                            <asp:Image ID="imgQrCode" runat="server" CssClass="h-32 w-32 object-contain" />
                        </div>
                        <p class="text-[9px] text-slate-400 font-mono tracking-tighter uppercase">RFID / QRWindshield pass</p>
                    </div>

                    <!-- Meta specs -->
                    <div class="border-t border-slate-100 pt-4 space-y-2.5 text-xs">
                        <div id="rowOwnership" runat="server" class="flex justify-between"><span class="text-slate-400 font-semibold">Ownership Type:</span> <span class="font-bold text-[#0054A6]"><asp:Label ID="lblOwnership" runat="server"></asp:Label></span></div>
                        <div id="rowDriver" runat="server" class="flex justify-between"><span class="text-slate-400 font-semibold">Driver:</span> <span class="font-bold text-slate-700"><asp:Label ID="lblDriver" runat="server"></asp:Label></span></div>
                        <div id="rowVendor" runat="server" class="flex justify-between"><span class="text-slate-400 font-semibold">Contractor/Vendor:</span> <span class="font-bold text-slate-700"><asp:Label ID="lblVendor" runat="server"></asp:Label></span></div>
                        <div id="rowEmpNumber" runat="server" class="flex justify-between"><span class="text-slate-400 font-semibold">Employee Number:</span> <span class="font-bold text-slate-700"><asp:Label ID="lblEmpNumber" runat="server"></asp:Label></span></div>
                        <div id="rowEmpName" runat="server" class="flex justify-between"><span class="text-slate-400 font-semibold">Employee Name:</span> <span class="font-bold text-slate-700"><asp:Label ID="lblEmpName" runat="server"></asp:Label></span></div>
                        <div class="flex justify-between"><span class="text-slate-400 font-semibold">Registered By:</span> <span class="font-bold text-slate-700"><asp:Label ID="lblCreator" runat="server"></asp:Label></span></div>
                    </div>

                    <!-- Actions -->
                    <div class="flex flex-wrap gap-2 border-t border-slate-100 pt-4">
                        <asp:Button ID="btnVerifyVehicle" runat="server" CssClass="flex-1 rounded bg-emerald-50 hover:bg-emerald-100 text-emerald-700 py-2 text-xs font-bold transition-all cursor-pointer focus:outline-none" Text="Approve Verification" OnClick="btnVerifyVehicle_Click" />
                        <asp:HyperLink ID="lnkPrintGatePass" runat="server" CssClass="flex-1 rounded border border-slate-200 bg-white hover:bg-slate-50 text-slate-600 py-2 text-xs font-bold text-center transition-all" Target="_blank" Text="Gate Pass"></asp:HyperLink>
                        <asp:Button ID="btnOpenEdit" runat="server" CssClass="w-full rounded bg-blue-50 hover:bg-blue-100 text-blue-700 py-2 text-xs font-bold transition-all cursor-pointer focus:outline-none" Text="Edit Vehicle Details" OnClick="btnOpenEditModal_Click" Visible="false" />
                        <asp:Button ID="btnDecommission" runat="server" CssClass="w-full rounded bg-red-50 hover:bg-red-100 text-red-600 py-2 text-xs font-bold transition-all cursor-pointer focus:outline-none" Text="Decommission Vehicle" OnClick="btnDecommission_Click" OnClientClick="return confirm('Are you sure you want to permanently delete this vehicle?');" />
                    </div>

                    <!-- Document checklist grid -->
                    <div class="border-t border-slate-100 pt-4 space-y-3">
                        <h4 class="text-[10px] font-extrabold text-slate-400 uppercase tracking-widest">Compliance Checklist</h4>
                        
                        <asp:Repeater ID="rptComplianceSlots" runat="server">
                            <ItemTemplate>
                                <div class="flex justify-between items-center border-b border-slate-50 py-2 text-xs">
                                    <div>
                                        <p class="font-semibold text-slate-700"><%# Eval("LicenseType").ToString().Replace("_", " ") %></p>
                                        <p class="text-[9px] text-slate-400 font-mono mt-0.5">
                                            <%# If(Convert.IsDBNull(Eval("LicenseNumber")), "Pending Upload", Eval("LicenseNumber")) %>
                                            <br />
                                            Expires: <%# FmtDate(Eval("ExpiryDate")) %>
                                            <%# If(Not Convert.IsDBNull(Eval("ExpiryDate")) AndAlso Convert.ToDateTime(Eval("ExpiryDate")) <= DateTime.Today.AddMonths(2), "<br /><span class='text-orange-500 font-bold'>Please renew this also</span>", "") %>
                                        </p>
                                    </div>
                                    <div class="flex items-center gap-2">
                                        <span class="rounded-full px-2 py-0.5 text-[9px] font-bold uppercase <%# GetStatusBadgeClass(Eval("Status")) %>">
                                            <%# Eval("Status") %>
                                        </span>
                                        <a href="Expiry.aspx?vehId=<%# Eval("VehicleId") %>&type=<%# Eval("LicenseType") %>" class="text-[#0054A6] hover:underline font-bold text-[10px]">Renew</a>
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </asp:Panel>

                <!-- If nothing selected placeholder -->
                <asp:Panel ID="pnlNoDetails" runat="server" CssClass="rounded-xl border-2 border-dashed border-slate-200 bg-white p-12 text-center text-slate-400">
                    <svg class="h-10 w-10 mx-auto mb-2 opacity-30" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /><path stroke-linecap="round" stroke-linejoin="round" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" /></svg>
                    <p class="text-xs font-bold text-slate-500">Select a vehicle from the directory to review windshield parameters & safety clearances.</p>
                </asp:Panel>
            </div>
        </div>
    </div>

    <!-- Registration Popup Modal — only for non-SuperAdmin users -->
    <asp:Panel ID="pnlAddModal" runat="server" CssClass="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4" Visible="false">
        <div class="relative w-full max-w-2xl rounded-2xl bg-white shadow-2xl overflow-hidden flex flex-col" style="max-height:92vh;">

            <!-- Modal Header -->
            <div class="flex items-center justify-between border-b border-slate-100 px-6 py-4 shrink-0">
                <div>
                    <h3 class="text-sm font-extrabold uppercase text-[#001F5B] tracking-wide">Register New Vehicle</h3>
                    <p class="text-[10px] text-slate-400 mt-0.5">Fill vehicle details and upload all compliance documents (PDF only)</p>
                </div>
                <asp:LinkButton ID="btnCloseAdd" runat="server" OnClick="btnCloseAddModal_Click" CssClass="text-slate-400 hover:text-slate-600 focus:outline-none">
                    <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                </asp:LinkButton>
            </div>

            <!-- Scrollable Body -->
            <div class="overflow-y-auto px-6 py-4 space-y-5 text-xs flex-1">

                <!-- ── Vehicle Details ── -->
                <div class="rounded-lg border border-slate-100 bg-slate-50 p-4 space-y-3">
                    <p class="text-[10px] font-extrabold text-[#0054A6] uppercase tracking-widest">Vehicle Details</p>

                    <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                        <div class="col-span-2">
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Ownership Type <span class="text-red-500">*</span></label>
                            <select id="ddlAddOwnershipType" name="ddlAddOwnershipType" onchange="toggleOwnershipType()" class="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all font-sans">
                                <option value="Contractual">Contractual Vehicle</option>
                                <option value="Personal">Personal Vehicle</option>
                            </select>
                        </div>

                        <div>
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Plate Number <span class="text-red-500">*</span></label>
                            <asp:TextBox ID="txtAddPlate" runat="server" placeholder="e.g. HR26AB1101" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all font-mono uppercase"></asp:TextBox>
                        </div>
                        <div id="divTypeContainer">
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Vehicle Type <span class="text-red-500">*</span></label>
                            <asp:TextBox ID="txtAddType" runat="server" placeholder="e.g. Petroleum Tanker" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all"></asp:TextBox>
                        </div>
                        <div id="divDriverContainer">
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Driver Name</label>
                            <asp:TextBox ID="txtAddDriver" runat="server" placeholder="Full Name" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all"></asp:TextBox>
                        </div>
                        <div id="divVendorContainer">
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Contractor / Vendor</label>
                            <asp:TextBox ID="txtAddVendor" runat="server" placeholder="Vendor / Company Name" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all"></asp:TextBox>
                        </div>
                        <div id="divEmployeeDetails" class="hidden grid grid-cols-1 sm:grid-cols-2 gap-3 col-span-2">
                            <div>
                                <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Employee Number</label>
                                <input type="text" class="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-500 bg-slate-100 outline-none font-mono" readonly="readonly" value="<%= If(Session("EmpNumber") IsNot Nothing, Session("EmpNumber").ToString(), "") %>" />
                            </div>
                            <div>
                                <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Employee Name</label>
                                <input type="text" class="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-500 bg-slate-100 outline-none" readonly="readonly" value="<%= If(Session("EmployeeName") IsNot Nothing, Session("EmployeeName").ToString(), "") %>" />
                            </div>
                        </div>
                    </div>
                </div>

                <!-- ── Compulsory Compliance Documents ── -->
                <div class="rounded-lg border border-orange-100 bg-orange-50/40 p-4 space-y-4">
                    <div class="flex items-center gap-2">
                        <svg class="h-4 w-4 text-orange-500 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
                        <p class="text-[10px] font-extrabold text-orange-600 uppercase tracking-widest">Compulsory Safety Documents (PDF required)</p>
                    </div>

                    <!-- RC (Registration Card) -->
                    <div class="rounded-lg border border-slate-200 bg-white p-3 space-y-2">
                        <p class="text-[10px] font-extrabold text-slate-600 uppercase tracking-widest flex items-center gap-1.5">
                            <span class="inline-flex items-center justify-center w-4 h-4 rounded-full bg-[#0054A6] text-white text-[8px] font-bold shrink-0">1</span>
                            Registration Card (RC) PDF <span class="text-red-500">*</span>
                        </p>
                        <div>
                            <input type="file" id="docFile_VEHICLE_RC" name="docFile_VEHICLE_RC" accept=".pdf" required class="w-full text-[10px] text-slate-600 file:mr-2 file:rounded file:border-0 file:bg-blue-50 file:px-2 file:py-1 file:text-[10px] file:font-bold file:text-[#0054A6] hover:file:bg-blue-100" />
                        </div>
                    </div>

                    <!-- Vehicle Insurance -->
                    <div class="rounded-lg border border-slate-200 bg-white p-3 space-y-2">
                        <p class="text-[10px] font-extrabold text-slate-600 uppercase tracking-widest flex items-center gap-1.5">
                            <span class="inline-flex items-center justify-center w-4 h-4 rounded-full bg-[#0054A6] text-white text-[8px] font-bold shrink-0">2</span>
                            Vehicle Insurance PDF <span class="text-red-500">*</span>
                        </p>
                        <div class="grid grid-cols-2 gap-2">
                            <div>
                                <label class="block text-[9px] font-bold text-slate-400 uppercase tracking-widest mb-1">Issue Date <span class="text-red-500">*</span></label>
                                <input type="date" id="issueDate_INSURANCE" name="issueDate_INSURANCE" required class="w-full rounded border border-slate-200 px-2 py-1.5 text-xs text-slate-700 outline-none focus:border-blue-500 transition-all" />
                            </div>
                            <div>
                                <label class="block text-[9px] font-bold text-slate-400 uppercase tracking-widest mb-1">Expiry Date <span class="text-red-500">*</span></label>
                                <input type="date" id="expiryDate_INSURANCE" name="expiryDate_INSURANCE" required class="w-full rounded border border-slate-200 px-2 py-1.5 text-xs text-slate-700 outline-none focus:border-blue-500 transition-all" />
                            </div>
                        </div>
                        <div class="mt-2">
                            <label class="block text-[9px] font-bold text-slate-400 uppercase tracking-widest mb-1">Insurance PDF File <span class="text-red-500">*</span></label>
                            <input type="file" id="docFile_INSURANCE" name="docFile_INSURANCE" accept=".pdf" required class="w-full text-[10px] text-slate-600 file:mr-2 file:rounded file:border-0 file:bg-blue-50 file:px-2 file:py-1 file:text-[10px] file:font-bold file:text-[#0054A6] hover:file:bg-blue-100" />
                        </div>
                    </div>

                    <!-- Age Determination -->
                    <div class="rounded-lg border border-slate-200 bg-white p-3 space-y-2">
                        <p class="text-[10px] font-extrabold text-slate-600 uppercase tracking-widest flex items-center gap-1.5">
                            <span class="inline-flex items-center justify-center w-4 h-4 rounded-full bg-[#0054A6] text-white text-[8px] font-bold shrink-0">3</span>
                            Age Determination / DOM PDF <span class="text-red-500">*</span>
                        </p>
                        <div class="grid grid-cols-2 gap-2">
                            <div>
                                <label class="block text-[9px] font-bold text-slate-400 uppercase tracking-widest mb-1">Issue Date <span class="text-red-500">*</span></label>
                                <input type="date" id="issueDate_AGE_DETERMINATION" name="issueDate_AGE_DETERMINATION" required class="w-full rounded border border-slate-200 px-2 py-1.5 text-xs text-slate-700 outline-none focus:border-blue-500 transition-all" />
                            </div>
                            <div>
                                <label class="block text-[9px] font-bold text-slate-400 uppercase tracking-widest mb-1">Expiry Date <span class="text-red-500">*</span></label>
                                <input type="date" id="expiryDate_AGE_DETERMINATION" name="expiryDate_AGE_DETERMINATION" required class="w-full rounded border border-slate-200 px-2 py-1.5 text-xs text-slate-700 outline-none focus:border-blue-500 transition-all" />
                            </div>
                        </div>
                        <div class="mt-2">
                            <label class="block text-[9px] font-bold text-slate-400 uppercase tracking-widest mb-1">Age Det. PDF File <span class="text-red-500">*</span></label>
                            <input type="file" id="docFile_AGE_DETERMINATION" name="docFile_AGE_DETERMINATION" accept=".pdf" required class="w-full text-[10px] text-slate-600 file:mr-2 file:rounded file:border-0 file:bg-blue-50 file:px-2 file:py-1 file:text-[10px] file:font-bold file:text-[#0054A6] hover:file:bg-blue-100" />
                        </div>
                    </div>
                </div>

                <!-- ── Optional Compliance Documents ── -->
                <div class="rounded-lg border border-slate-100 bg-slate-50/50 p-4 space-y-4">
                    <div class="flex items-center justify-between">
                        <p class="text-[10px] font-extrabold text-slate-600 uppercase tracking-widest">Additional Documents (Optional)</p>
                        <button type="button" onclick="addOptionalDocRow()" class="rounded bg-blue-50 hover:bg-blue-100 text-[#0054A6] px-2.5 py-1 text-[10px] font-bold transition-all focus:outline-none">
                            + Add Document
                        </button>
                    </div>
                    
                    <input type="hidden" id="optDocCount" name="optDocCount" value="0" />
                    <div id="optionalDocsContainer" class="space-y-3">
                        <!-- Dynamic rows appended here -->
                    </div>
                </div>
            </div>

            <!-- Modal Footer -->
            <div class="flex gap-3 px-6 py-4 border-t border-slate-100 shrink-0 bg-white">
                <asp:Button ID="btnCancelAdd" runat="server" CssClass="flex-1 rounded border border-slate-200 bg-white hover:bg-slate-50 text-slate-600 py-2.5 text-xs font-bold cursor-pointer focus:outline-none" Text="Cancel" OnClick="btnCloseAddModal_Click" />
                <asp:Button ID="btnSaveVehicle" runat="server" CssClass="flex-1 rounded bg-[#0054A6] hover:bg-blue-700 text-white py-2.5 text-xs font-bold cursor-pointer focus:outline-none" Text="Register Vehicle" OnClick="btnSaveVehicle_Click" />
            </div>
        </div>
    </asp:Panel>


    <!-- Edit Vehicle Modal — only for SuperAdmin -->
    <asp:Panel ID="pnlEditModal" runat="server" CssClass="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4" Visible="false">
        <div class="relative w-full max-w-2xl rounded-2xl bg-white shadow-2xl overflow-hidden flex flex-col" style="max-height:92vh;">

            <!-- Modal Header -->
            <div class="flex items-center justify-between border-b border-slate-100 px-6 py-4 shrink-0">
                <div>
                    <h3 class="text-sm font-extrabold uppercase text-[#001F5B] tracking-wide">Edit Vehicle Details</h3>
                    <p class="text-[10px] text-slate-400 mt-0.5">Modify vehicle registration parameters</p>
                </div>
                <asp:LinkButton ID="btnCloseEdit" runat="server" OnClick="btnCloseEditModal_Click" CssClass="text-slate-400 hover:text-slate-600 focus:outline-none">
                    <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                </asp:LinkButton>
            </div>

            <!-- Scrollable Body -->
            <div class="overflow-y-auto px-6 py-4 space-y-5 text-xs flex-1">
                <div class="rounded-lg border border-slate-100 bg-slate-50 p-4 space-y-3">
                    <p class="text-[10px] font-extrabold text-[#0054A6] uppercase tracking-widest">Vehicle Details</p>

                    <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                        <div class="col-span-2">
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Ownership Type <span class="text-red-500">*</span></label>
                            <asp:DropDownList ID="ddlEditOwnershipType" runat="server" OnSelectedIndexChanged="ddlEditOwnershipType_SelectedIndexChanged" AutoPostBack="true" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all font-sans">
                                <asp:ListItem Text="Contractual Vehicle" Value="Contractual"></asp:ListItem>
                                <asp:ListItem Text="Personal Vehicle" Value="Personal"></asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div>
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Plate Number <span class="text-red-500">*</span></label>
                            <asp:TextBox ID="txtEditPlate" runat="server" placeholder="e.g. HR26AB1101" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all font-mono uppercase"></asp:TextBox>
                        </div>
                        <div id="divEditTypeContainer" runat="server">
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Vehicle Type <span class="text-red-500">*</span></label>
                            <asp:TextBox ID="txtEditType" runat="server" placeholder="e.g. Petroleum Tanker" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all"></asp:TextBox>
                        </div>
                        <div id="divEditDriverContainer" runat="server">
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Driver Name</label>
                            <asp:TextBox ID="txtEditDriver" runat="server" placeholder="Full Name" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all"></asp:TextBox>
                        </div>
                        <div id="divEditVendorContainer" runat="server">
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Contractor / Vendor</label>
                            <asp:TextBox ID="txtEditVendor" runat="server" placeholder="Vendor / Company Name" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all"></asp:TextBox>
                        </div>
                        <div class="col-span-2">
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Department <span class="text-red-500">*</span></label>
                            <asp:DropDownList ID="ddlEditDept" runat="server" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all font-sans"></asp:DropDownList>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Modal Footer -->
            <div class="flex gap-3 px-6 py-4 border-t border-slate-100 shrink-0 bg-white">
                <asp:Button ID="btnCancelEdit" runat="server" CssClass="flex-1 rounded border border-slate-200 bg-white hover:bg-slate-50 text-slate-600 py-2.5 text-xs font-bold cursor-pointer focus:outline-none" Text="Cancel" OnClick="btnCloseEditModal_Click" />
                <asp:Button ID="btnSaveEditVehicle" runat="server" CssClass="flex-1 rounded bg-[#0054A6] hover:bg-blue-700 text-white py-2.5 text-xs font-bold cursor-pointer focus:outline-none" Text="Save Changes" OnClick="btnSaveEditVehicle_Click" />
            </div>
        </div>
    </asp:Panel>


    <!-- QR Windshield Modal -->
    <asp:Panel ID="pnlQrModal" runat="server" CssClass="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4" Visible="false">
        <div class="relative w-full max-w-sm rounded-2xl bg-white shadow-2xl overflow-hidden p-6 space-y-4">
            <div class="flex items-center justify-between border-b border-slate-100 pb-3">
                <h3 class="text-sm font-bold text-slate-800">Vehicle Windshield Pass</h3>
                <asp:LinkButton ID="btnCloseQr" runat="server" OnClick="btnCloseQrModal_Click" CssClass="text-slate-400 hover:text-slate-600 focus:outline-none">
                    <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                </asp:LinkButton>
            </div>
            
            <div id="printable-area" class="flex flex-col items-center py-4 text-center">
                <div class="bg-gradient-to-br from-[#0054A6] to-blue-800 text-white w-full rounded-xl p-4 mb-5 shadow-inner">
                    <p class="text-[10px] uppercase tracking-widest font-extrabold opacity-80">IOCL Gate Entry Pass</p>
                    <p class="text-lg font-extrabold font-mono tracking-wider mt-1"><asp:Label ID="lblModalPlateNumber" runat="server"></asp:Label></p>
                    <p class="text-[9px] opacity-75 mt-0.5"><asp:Label ID="lblModalType" runat="server"></asp:Label></p>
                </div>
                
                <asp:Image ID="imgModalQrCode" runat="server" CssClass="h-44 w-44 rounded-xl border border-slate-200 p-2 bg-white shadow-md" />
                
                <p class="text-[9px] text-slate-400 font-mono mt-4 break-all px-2 select-all">
                    <asp:Label ID="lblModalUrl" runat="server"></asp:Label>
                </p>
            </div>

            <div class="flex items-center gap-3 border-t border-slate-100 pt-4">
                <button type="button" onclick="window.print()" class="flex-1 flex items-center justify-center gap-1.5 rounded-lg bg-[#0054A6] hover:bg-blue-700 text-white py-2.5 text-xs font-bold transition-all shadow focus:outline-none">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" /></svg>
                    Print Pass
                </button>
                <asp:Button ID="btnCloseQrBtn" runat="server" CssClass="flex-1 rounded-lg border border-slate-200 hover:bg-slate-50 text-slate-600 py-2.5 text-xs font-bold cursor-pointer focus:outline-none" Text="Close" OnClick="btnCloseQrModal_Click" />
            </div>
        </div>
    </asp:Panel>

    <script type="text/javascript">
        function toggleOwnershipType() {
            var ddl = document.getElementById("ddlAddOwnershipType");
            if (!ddl) return;
            
            var typeContainer = document.getElementById("divTypeContainer");
            var driverContainer = document.getElementById("divDriverContainer");
            var vendorContainer = document.getElementById("divVendorContainer");
            var empContainer = document.getElementById("divEmployeeDetails");
            
            var txtType = document.getElementById("<%= txtAddType.ClientID %>");
            
            if (ddl.value === "Personal") {
                if (typeContainer) typeContainer.style.display = "none";
                if (driverContainer) driverContainer.style.display = "none";
                if (vendorContainer) vendorContainer.style.display = "none";
                if (empContainer) empContainer.classList.remove("hidden");
                if (txtType) txtType.value = "Car";
            } else {
                if (typeContainer) typeContainer.style.display = "block";
                if (driverContainer) driverContainer.style.display = "block";
                if (vendorContainer) vendorContainer.style.display = "block";
                if (empContainer) empContainer.classList.add("hidden");
                if (txtType && txtType.value === "Car") txtType.value = "";
            }
        }

        window.addEventListener('DOMContentLoaded', (event) => {
            toggleOwnershipType();
        });

        let optionalDocIdx = 0;

        function addOptionalDocRow() {
            optionalDocIdx++;
            const container = document.getElementById('optionalDocsContainer');
            const row = document.createElement('div');
            row.id = `optDocRow_${optionalDocIdx}`;
            row.className = 'rounded-lg border border-slate-200 bg-white p-3 space-y-2 relative';
            
            row.innerHTML = `
                <button type="button" onclick="removeOptionalDocRow(${optionalDocIdx})" class="absolute top-2 right-2 text-red-500 hover:text-red-700 text-[10px] font-bold transition-all focus:outline-none">
                    Remove
                </button>
                <div class="grid grid-cols-2 gap-2">
                    <div class="col-span-2 sm:col-span-1">
                        <label class="block text-[9px] font-bold text-slate-400 uppercase tracking-widest mb-1">Doc Type</label>
                        <select name="optDocType_${optionalDocIdx}" id="optDocType_${optionalDocIdx}" onchange="toggleCustomNameInput(${optionalDocIdx})" class="w-full rounded border border-slate-200 px-2 py-1.5 text-xs text-slate-700 outline-none focus:border-blue-500 transition-all">
                            <option value="ROAD_PERMIT">Road Permit</option>
                            <option value="PUC">Pollution Under Control (PUC)</option>
                            <option value="FITNESS">Fitness Certificate</option>
                            <option value="EXPLOSIVE">Explosive License</option>
                            <option value="GREEN_CARD">Green Card</option>
                            <option value="CALIBRATION">Calibration Card</option>
                            <option value="CUSTOM">Custom Document Type (Specify...)</option>
                        </select>
                    </div>
                    <div class="col-span-2 sm:col-span-1 hidden" id="customNameContainer_${optionalDocIdx}">
                        <label class="block text-[9px] font-bold text-slate-400 uppercase tracking-widest mb-1">Custom Doc Name</label>
                        <input type="text" name="optDocCustomName_${optionalDocIdx}" id="optDocCustomName_${optionalDocIdx}" placeholder="e.g. Health Certificate" class="w-full rounded border border-slate-200 px-2 py-1.5 text-xs text-slate-700 outline-none focus:border-blue-500 transition-all" />
                    </div>
                </div>
                
                <div class="grid grid-cols-2 gap-2">
                    <div>
                        <label class="block text-[9px] font-bold text-slate-400 uppercase tracking-widest mb-1">Issue Date</label>
                        <input type="date" name="optDocIssueDate_${optionalDocIdx}" class="w-full rounded border border-slate-200 px-2 py-1.5 text-xs text-slate-700 outline-none focus:border-blue-500 transition-all" />
                    </div>
                    <div>
                        <label class="block text-[9px] font-bold text-slate-400 uppercase tracking-widest mb-1">Expiry Date</label>
                        <input type="date" name="optDocExpiryDate_${optionalDocIdx}" class="w-full rounded border border-slate-200 px-2 py-1.5 text-xs text-slate-700 outline-none focus:border-blue-500 transition-all" />
                    </div>
                </div>
                
                <div>
                    <label class="block text-[9px] font-bold text-slate-400 uppercase tracking-widest mb-1">Document File (PDF)</label>
                    <input type="file" name="optDocFile_${optionalDocIdx}" accept=".pdf" class="w-full text-[10px] text-slate-600 file:mr-2 file:rounded file:border-0 file:bg-blue-50 file:px-2 file:py-1 file:text-[10px] file:font-bold file:text-[#0054A6] hover:file:bg-blue-100" />
                </div>
            `;
            
            container.appendChild(row);
            document.getElementById('optDocCount').value = optionalDocIdx;
        }

        function removeOptionalDocRow(idx) {
            const row = document.getElementById(`optDocRow_${idx}`);
            if (row) {
                row.remove();
            }
        }

        function toggleCustomNameInput(idx) {
            const select = document.getElementById(`optDocType_${idx}`);
            const container = document.getElementById(`customNameContainer_${idx}`);
            if (select && select.value === 'CUSTOM') {
                container.classList.remove('hidden');
                document.getElementById(`optDocCustomName_${idx}`).setAttribute('required', 'required');
            } else {
                container.classList.add('hidden');
                document.getElementById(`optDocCustomName_${idx}`).removeAttribute('required');
            }
        }
    </script>
</asp:Content>
