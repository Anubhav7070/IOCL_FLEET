<%@ Page Title="Vehicle Management" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeFile="Vehicles.aspx.vb" Inherits="VehiclesPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <asp:Panel ID="pnlMainView" runat="server" CssClass="space-y-6">
        <!-- Filter Control Panel -->
        <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm flex flex-col md:flex-row md:items-center gap-4">
            <div class="relative flex-1">
                <span class="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none text-slate-400">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                </span>
                <asp:TextBox ID="txtSearch" runat="server" OnTextChanged="FilterVehicles" AutoPostBack="true" placeholder="Search vehicle number or type..." CssClass="w-full rounded border border-slate-200 bg-slate-50 pl-9 pr-4 py-2 text-xs font-semibold text-slate-600 placeholder-slate-400 outline-none focus:border-blue-500 focus:bg-white transition-all"></asp:TextBox>
            </div>

            <div class="flex flex-wrap items-center gap-3">
                <asp:DropDownList ID="ddlDeptFilter" runat="server" AutoPostBack="true" OnSelectedIndexChanged="FilterVehicles" CssClass="rounded border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-600 outline-none focus:border-blue-500 transition-all">
                </asp:DropDownList>

                <asp:DropDownList ID="ddlStatusFilter" runat="server" AutoPostBack="true" OnSelectedIndexChanged="FilterVehicles" CssClass="rounded border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-600 outline-none focus:border-blue-500 transition-all">
                    <asp:ListItem Text="All Statuses" Value=""></asp:ListItem>
                    <asp:ListItem Text="Compliant" Value="Compliant"></asp:ListItem>
                    <asp:ListItem Text="Non-Compliant" Value="Non-Compliant"></asp:ListItem>
                    <asp:ListItem Text="Expired" Value="Expired"></asp:ListItem>
                </asp:DropDownList>
                
                <asp:LinkButton ID="btnClearFilters" runat="server" OnClick="btnReset_Click" CssClass="text-xs font-semibold text-slate-500 hover:text-slate-800 px-2 mr-2">Reset</asp:LinkButton>


                
                <% Dim role As String = If(Session("Role") IsNot Nothing, Session("Role").ToString(), "")
                   If role <> "VIEWER" Then %>
                    <asp:LinkButton ID="btnOpenAdd" runat="server" OnClick="btnOpenAddModal_Click" CssClass="flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg bg-blue-50 hover:bg-blue-100 text-blue-700 transition-all border border-blue-200 text-xs font-bold focus:outline-none">
                        <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4" /></svg>
                        <span>Register Vehicle</span>
                    </asp:LinkButton>
                <% End If %>
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
                                            <div class="col-span-2">
                                                <p class="text-[9px] font-bold text-slate-400 uppercase tracking-widest">Allocated Department</p>
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

                                    <div class="flex items-center gap-2">
                                        <asp:LinkButton ID="btnView" runat="server" CommandName="ViewDetails" CommandArgument='<%# Eval("Id") %>' CssClass="rounded bg-blue-50 hover:bg-blue-100 text-[#0054A6] px-3.5 py-1.5 text-xs font-bold transition-all focus:outline-none">
                                            View Details
                                        </asp:LinkButton>
                                        <% If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then %>
                                            <asp:LinkButton ID="btnDelete" runat="server" CommandName="DeleteVehicle" CommandArgument='<%# Eval("Id") %>' OnClientClick="return confirm('Decommission this vehicle? It will be archived and hidden from active listings, but kept in the database.');" CssClass="rounded bg-red-50 hover:bg-red-100 text-red-600 p-1.5 transition-all focus:outline-none">
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
                        <div class="bg-gradient-to-br from-[#0054A6] to-blue-800 text-white w-full rounded-xl p-4 shadow shadow-blue-800/10 font-mono">
                            <p class="text-xs uppercase tracking-widest font-bold opacity-80">IOCL Windshield Clearance</p>
                            <p class="text-xl font-extrabold tracking-wider mt-1"><asp:Label ID="lblPlateNumber" runat="server"></asp:Label></p>
                            <p class="text-[10px] opacity-80 mt-0.5"><asp:Label ID="lblType" runat="server"></asp:Label></p>
                        </div>


                    </div>

                    <!-- Meta specs -->
                    <div class="border-t border-slate-100 pt-4 space-y-2.5 text-xs">
                        <div class="flex justify-between"><span class="text-slate-400 font-semibold">Registered By:</span> <span class="font-bold text-slate-700"><asp:Label ID="lblCreator" runat="server"></asp:Label></span></div>
                    </div>

                    <!-- Actions -->
                    <div class="flex flex-wrap gap-2 border-t border-slate-100 pt-4">
                        <asp:Button ID="btnVerifyVehicle" runat="server" CssClass="flex-1 rounded bg-emerald-50 hover:bg-emerald-100 text-emerald-700 py-2 text-xs font-bold transition-all cursor-pointer focus:outline-none" Text="Approve Verification" OnClick="btnVerifyVehicle_Click" />

                        <asp:Button ID="btnOpenEdit" runat="server" CssClass="w-full rounded bg-blue-50 hover:bg-blue-100 text-blue-700 py-2 text-xs font-bold transition-all cursor-pointer focus:outline-none" Text="Edit Vehicle Details" OnClick="btnOpenEditModal_Click" Visible="false" />
                        <asp:Button ID="btnDecommission" runat="server" CssClass="w-full rounded bg-red-50 hover:bg-red-100 text-red-600 py-2 text-xs font-bold transition-all cursor-pointer focus:outline-none" Text="Decommission Vehicle" OnClick="btnDecommission_Click" OnClientClick="return confirm('Decommission this vehicle? It will be archived and removed from active listings, but kept in the database.');" />
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
                                            <%# If(Not Convert.IsDBNull(Eval("ExpiryDate")) AndAlso Convert.ToDateTime(Eval("ExpiryDate")) <= DateTime.Today.AddDays(15), "<br /><span class='text-orange-500 font-bold'>Renewal needed soon</span>", "") %>
                                        </p>
                                    </div>
                                    <div class="flex items-center gap-2">
                                        <span class="rounded-full px-2 py-0.5 text-[9px] font-bold uppercase <%# GetStatusBadgeClass(Eval("Status")) %>">
                                            <%# Eval("Status") %>
                                        </span>
                                        <a href="Expiry.aspx?renewId=<%# Eval("Id") %>" class="text-[#0054A6] hover:underline font-bold text-[10px]">Renew</a>
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

        <%-- Decommissioned Vehicles Archive (SuperAdmin only) --%>
        <% If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then %>
        <asp:Panel ID="pnlDecommissioned" runat="server" CssClass="mt-6 rounded-xl border border-red-100 bg-white shadow-sm overflow-hidden">
            <!-- Section Header -->
            <div class="flex items-center justify-between bg-red-50 border-b border-red-100 px-5 py-3">
                <div class="flex items-center gap-2">
                    <svg class="h-4 w-4 text-red-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M5 8h14M5 8a2 2 0 110-4h14a2 2 0 110 4M5 8l1 12a2 2 0 002 2h8a2 2 0 002-2L19 8M10 12v4m4-4v4" />
                    </svg>
                    <h3 class="text-xs font-extrabold text-red-700 uppercase tracking-widest">Decommissioned Vehicles Archive</h3>
                    <span class="text-[9px] bg-red-100 text-red-600 font-bold px-2 py-0.5 rounded-full">Archived · Not counted in total</span>
                </div>
                <span class="text-[10px] text-red-400 font-semibold">SuperAdmin view only · Cannot be allocated</span>
            </div>
            <!-- Table -->
            <div class="overflow-x-auto">
                <asp:Repeater ID="rptDecommissioned" runat="server" OnItemCommand="rptDecommissioned_ItemCommand">
                    <HeaderTemplate>
                        <table class="w-full text-left border-collapse text-xs">
                            <thead>
                                <tr class="border-b border-red-50 text-slate-400 uppercase font-bold tracking-wider bg-red-50/40">
                                    <th class="py-3 px-4">Vehicle No</th>
                                    <th class="py-3 px-4">Type</th>
                                    <th class="py-3 px-4">Department</th>
                                    <th class="py-3 px-4">Last Status</th>
                                    <th class="py-3 px-4 text-right">Action</th>
                                </tr>
                            </thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr class="border-b border-red-50 hover:bg-red-50/30 transition-colors">
                            <td class="py-3 px-4 font-bold font-mono text-slate-500 line-through"><%# Eval("VehicleNumber") %></td>
                            <td class="py-3 px-4 font-semibold text-slate-400"><%# Eval("VehicleType") %></td>
                            <td class="py-3 px-4 font-semibold text-slate-400"><%# Eval("Department") %></td>
                            <td class="py-3 px-4">
                                <span class="rounded-full px-2 py-0.5 text-[10px] font-bold uppercase bg-slate-100 text-slate-500">
                                    <%# Eval("OverallStatus") %>
                                </span>
                            </td>
                            <td class="py-3 px-4 text-right">
                                <asp:LinkButton ID="btnReactivate" runat="server"
                                    CommandName="Reactivate"
                                    CommandArgument='<%# Eval("Id") %>'
                                    OnClientClick="return confirm('Reactivate this vehicle? It will return to the active fleet.');"
                                    CssClass="rounded bg-emerald-50 hover:bg-emerald-100 text-emerald-700 px-3 py-1.5 font-bold transition-all focus:outline-none text-xs">
                                    ↩ Reactivate
                                </asp:LinkButton>
                            </td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                            </tbody>
                        </table>
                        <%# If(rptDecommissioned.Items.Count = 0, "<div class='py-10 text-center text-xs text-slate-400'>No decommissioned vehicles in the archive.</div>", "") %>
                    </FooterTemplate>
                </asp:Repeater>
            </div>
        </asp:Panel>
        <% End If %>
    </asp:Panel>


    <!-- Registration Form (Shown when adding a vehicle) -->
    <asp:Panel ID="pnlAddModal" runat="server" CssClass="max-w-2xl mx-auto py-4" Visible="false">
        <div class="relative w-full max-w-2xl rounded-2xl bg-white border border-slate-250/80 shadow-sm overflow-hidden flex flex-col">

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

            <!-- Form Body -->
            <div class="px-6 py-4 space-y-5 text-xs">

                <!-- Vehicle Details -->
                <div class="rounded-lg border border-slate-100 bg-slate-50 p-4 space-y-3">
                    <p class="text-[10px] font-extrabold text-[#0054A6] uppercase tracking-widest">Vehicle Details</p>

                    <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 font-sans">
                        <div>
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Plate Number <span class="text-red-500">*</span></label>
                            <asp:TextBox ID="txtAddPlate" runat="server" placeholder="e.g. HR26AB1101" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all font-mono uppercase"></asp:TextBox>
                        </div>
                        <div>
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Vehicle Type <span class="text-red-500">*</span></label>
                            <asp:DropDownList ID="ddlAddType" runat="server" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all font-sans bg-white">
                                <asp:ListItem Text="Truck" Value="Truck"></asp:ListItem>
                                <asp:ListItem Text="Crane" Value="Crane"></asp:ListItem>
                                <asp:ListItem Text="Hydra" Value="Hydra"></asp:ListItem>
                                <asp:ListItem Text="Tractor" Value="Tractor"></asp:ListItem>
                                <asp:ListItem Text="JCB" Value="JCB"></asp:ListItem>
                                <asp:ListItem Text="Forklift" Value="Forklift"></asp:ListItem>
                                <asp:ListItem Text="Dumper" Value="Dumper"></asp:ListItem>
                                <asp:ListItem Text="Trailer" Value="Trailer"></asp:ListItem>
                                <asp:ListItem Text="Other" Value="Other"></asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div class="col-span-2">
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Department <span class="text-red-500">*</span></label>
                            <asp:DropDownList ID="ddlAddDept" runat="server" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all font-sans bg-white">
                                <asp:ListItem Text="PR - Human Resources" Value="PR - Human Resources" Selected="True"></asp:ListItem>
                                <asp:ListItem Text="PR - Fire & Safety" Value="PR - Fire & Safety"></asp:ListItem>
                                <asp:ListItem Text="PR - Refinery Operations" Value="PR - Refinery Operations"></asp:ListItem>
                                <asp:ListItem Text="PR - Chemical & Laboratory" Value="PR - Chemical & Laboratory"></asp:ListItem>
                                <asp:ListItem Text="PNC - Fire & Safety" Value="PNC - Fire & Safety"></asp:ListItem>
                                <asp:ListItem Text="PNC - Cracker Operations" Value="PNC - Cracker Operations"></asp:ListItem>
                                <asp:ListItem Text="PNC - Chemical & Testing" Value="PNC - Chemical & Testing"></asp:ListItem>
                            </asp:DropDownList>
                        </div>
                    </div>
                </div>

                <!-- Compulsory Compliance Documents -->
                <div class="rounded-lg border border-orange-100 bg-orange-50/40 p-4 space-y-4 font-sans">
                    <div class="flex items-center gap-2">
                        <svg class="h-4 w-4 text-orange-500 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
                        <p class="text-[10px] font-extrabold text-orange-600 uppercase tracking-widest">Compulsory Safety Documents (PDF required)</p>
                    </div>

                    <!-- RC (Registration Certificate) -->
                    <div class="rounded-lg border border-slate-200 bg-white p-3 space-y-2">
                        <p class="text-[10px] font-extrabold text-slate-600 uppercase tracking-widest flex items-center gap-1.5">
                            <span class="inline-flex items-center justify-center w-4 h-4 rounded-full bg-[#0054A6] text-white text-[8px] font-bold shrink-0">1</span>
                            Registration Certificate (RC) PDF <span class="text-red-500">*</span>
                        </p>
                        <div class="grid grid-cols-2 gap-2">
                            <div>
                                <label class="block text-[9px] font-bold text-slate-400 uppercase tracking-widest mb-1">Issue Date <span class="text-red-500">*</span></label>
                                <input type="date" id="issueDate_RC" name="issueDate_RC" required class="w-full rounded border border-slate-200 px-2 py-1.5 text-xs text-slate-700 outline-none focus:border-blue-500 transition-all" />
                            </div>
                            <div>
                                <label class="block text-[9px] font-bold text-slate-400 uppercase tracking-widest mb-1">Expiry Date <span class="text-red-500">*</span></label>
                                <input type="date" id="expiryDate_RC" name="expiryDate_RC" required class="w-full rounded border border-slate-200 px-2 py-1.5 text-xs text-slate-700 outline-none focus:border-blue-500 transition-all" />
                            </div>
                        </div>
                        <div class="mt-2">
                            <input type="file" id="docFile_RC" name="docFile_RC" accept=".pdf" required class="w-full text-[10px] text-slate-600 file:mr-2 file:rounded file:border-0 file:bg-blue-50 file:px-2 file:py-1 file:text-[10px] file:font-bold file:text-[#0054A6] hover:file:bg-blue-100" />
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
                            <input type="file" id="docFile_INSURANCE" name="docFile_INSURANCE" accept=".pdf" required class="w-full text-[10px] text-slate-600 file:mr-2 file:rounded file:border-0 file:bg-blue-50 file:px-2 file:py-1 file:text-[10px] file:font-bold file:text-[#0054A6] hover:file:bg-blue-100" />
                        </div>
                    </div>

                    <!-- PUCC -->
                    <div class="rounded-lg border border-slate-200 bg-white p-3 space-y-2">
                        <p class="text-[10px] font-extrabold text-slate-600 uppercase tracking-widest flex items-center gap-1.5">
                            <span class="inline-flex items-center justify-center w-4 h-4 rounded-full bg-[#0054A6] text-white text-[8px] font-bold shrink-0">3</span>
                            Pollution Under Control (PUCC) PDF <span class="text-red-500">*</span>
                        </p>
                        <div class="grid grid-cols-2 gap-2">
                            <div>
                                <label class="block text-[9px] font-bold text-slate-400 uppercase tracking-widest mb-1">Issue Date <span class="text-red-500">*</span></label>
                                <input type="date" id="issueDate_PUCC" name="issueDate_PUCC" required class="w-full rounded border border-slate-200 px-2 py-1.5 text-xs text-slate-700 outline-none focus:border-blue-500 transition-all" />
                            </div>
                            <div>
                                <label class="block text-[9px] font-bold text-slate-400 uppercase tracking-widest mb-1">Expiry Date <span class="text-red-500">*</span></label>
                                <input type="date" id="expiryDate_PUCC" name="expiryDate_PUCC" required class="w-full rounded border border-slate-200 px-2 py-1.5 text-xs text-slate-700 outline-none focus:border-blue-500 transition-all" />
                            </div>
                        </div>
                        <div class="mt-2">
                            <input type="file" id="docFile_PUCC" name="docFile_PUCC" accept=".pdf" required class="w-full text-[10px] text-slate-600 file:mr-2 file:rounded file:border-0 file:bg-blue-50 file:px-2 file:py-1 file:text-[10px] file:font-bold file:text-[#0054A6] hover:file:bg-blue-100" />
                        </div>
                    </div>
                </div>
            </div>

            <!-- Modal Footer -->
            <div class="flex gap-3 px-6 py-4 border-t border-slate-100 shrink-0 bg-white">
                <asp:Button ID="btnCancelAdd" runat="server" UseSubmitBehavior="false" CausesValidation="false" CssClass="flex-1 rounded border border-slate-200 bg-white hover:bg-slate-50 text-slate-600 py-2.5 text-xs font-bold cursor-pointer focus:outline-none" Text="Cancel" OnClick="btnCloseAddModal_Click" />
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

                    <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 font-sans">
                        <div>
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Plate Number <span class="text-red-500">*</span></label>
                            <asp:TextBox ID="txtEditPlate" runat="server" placeholder="e.g. HR26AB1101" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all font-mono uppercase"></asp:TextBox>
                        </div>
                        <div>
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Vehicle Type <span class="text-red-500">*</span></label>
                            <asp:DropDownList ID="ddlEditType" runat="server" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all font-sans bg-white">
                                <asp:ListItem Text="Truck" Value="Truck"></asp:ListItem>
                                <asp:ListItem Text="Crane" Value="Crane"></asp:ListItem>
                                <asp:ListItem Text="Hydra" Value="Hydra"></asp:ListItem>
                                <asp:ListItem Text="Tractor" Value="Tractor"></asp:ListItem>
                                <asp:ListItem Text="JCB" Value="JCB"></asp:ListItem>
                                <asp:ListItem Text="Forklift" Value="Forklift"></asp:ListItem>
                                <asp:ListItem Text="Dumper" Value="Dumper"></asp:ListItem>
                                <asp:ListItem Text="Trailer" Value="Trailer"></asp:ListItem>
                                <asp:ListItem Text="Other" Value="Other"></asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div class="col-span-2">
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Department <span class="text-red-500">*</span></label>
                            <asp:DropDownList ID="ddlEditDept" runat="server" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all font-sans bg-white"></asp:DropDownList>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Modal Footer -->
            <div class="flex gap-3 px-6 py-4 border-t border-slate-100 shrink-0 bg-white">
                <asp:Button ID="btnCancelEdit" runat="server" UseSubmitBehavior="false" CausesValidation="false" CssClass="flex-1 rounded border border-slate-200 bg-white hover:bg-slate-50 text-slate-600 py-2.5 text-xs font-bold cursor-pointer focus:outline-none" Text="Cancel" OnClick="btnCloseEditModal_Click" />
                <asp:Button ID="btnSaveEditVehicle" runat="server" CssClass="flex-1 rounded bg-[#0054A6] hover:bg-blue-700 text-white py-2.5 text-xs font-bold cursor-pointer focus:outline-none" Text="Save Changes" OnClick="btnSaveEditVehicle_Click" />
            </div>
        </div>
    </asp:Panel>



</asp:Content>
