<%@ Page Title="Dashboard" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeFile="Default.aspx.vb" Inherits="DefaultPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <!-- ChartJS library -->
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="space-y-6">
        <!-- Hidden buttons for postback events -->
        <asp:LinkButton ID="btnTotalVehiclesClick" runat="server" OnClick="lnkTotalVehicles_Click" style="display:none;" />
        <asp:LinkButton ID="btnCompliantClick" runat="server" OnClick="lnkCompliantVehicles_Click" style="display:none;" />
        <asp:LinkButton ID="btnNonCompliantClick" runat="server" OnClick="lnkNonCompliantVehicles_Click" style="display:none;" />
        <asp:LinkButton ID="btnExpiredClick" runat="server" OnClick="lnkExpiredVehicles_Click" style="display:none;" />

        <!-- KPI Cards -->
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <!-- Card 1: Total Vehicles -->
            <asp:Panel ID="pnlTotalVehiclesCard" runat="server" CssClass="rounded-xl border border-slate-200 bg-white p-5 shadow-sm hover:shadow-md hover:border-blue-400 transition-all w-full flex items-center justify-between cursor-pointer" onclick="document.getElementById('MainContent_btnTotalVehiclesClick').click();">
                <div class="flex-1 text-left pr-2">
                    <span class="text-xs font-bold text-slate-400 uppercase tracking-widest font-sans">Total Vehicles</span>
                    <p class="text-2xl font-extrabold text-slate-800 mt-2"><asp:Label ID="lblTotalVehicles" runat="server" Text="0"></asp:Label></p>
                </div>
                
                <div class="flex flex-col gap-1.5 shrink-0" onclick="event.stopPropagation();">
                    <!-- Add Vehicle (+) Link -->
                    <a href="Vehicles.aspx?add=1" title="Register New Vehicle" class="flex items-center justify-center rounded-lg bg-blue-50 hover:bg-blue-100 p-1.5 text-[#0054A6] transition-all border border-blue-200">
                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4" />
                        </svg>
                    </a>
                    <!-- PDF Export Logo -->
                    <asp:LinkButton ID="btnExportPDF" runat="server" OnClick="btnExportPDF_Click" title="Export PDF Compliance Report" CssClass="flex items-center justify-center rounded-lg bg-red-50 hover:bg-red-100 p-1.5 text-red-600 transition-all border border-red-200 focus:outline-none">
                        <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                            <polyline points="14 2 14 8 20 8"/>
                            <line x1="16" y1="13" x2="8" y2="13"/>
                            <line x1="16" y1="17" x2="8" y2="17"/>
                            <polyline points="10 9 9 9 8 9"/>
                        </svg>
                    </asp:LinkButton>
                    <!-- Excel Export Logo -->
                    <asp:LinkButton ID="btnExportExcel" runat="server" OnClick="btnExportExcel_Click" title="Export Excel Compliance Sheet" CssClass="flex items-center justify-center rounded-lg bg-emerald-50 hover:bg-emerald-100 p-1.5 text-emerald-600 transition-all border border-emerald-200 focus:outline-none">
                        <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                            <polyline points="14 2 14 8 20 8"/>
                            <line x1="16" y1="13" x2="8" y2="13"/>
                            <line x1="16" y1="17" x2="8" y2="17"/>
                            <polyline points="10 9 9 9 8 9"/>
                        </svg>
                    </asp:LinkButton>
                </div>
            </asp:Panel>

            <!-- Card 2: Compliant -->
            <asp:Panel ID="pnlCompliantCard" runat="server" CssClass="rounded-xl border border-slate-200 bg-white p-5 shadow-sm hover:shadow-md hover:border-emerald-400 transition-all w-full flex items-center justify-between cursor-pointer" onclick="document.getElementById('MainContent_btnCompliantClick').click();">
                <div class="flex-1 text-left">
                    <div class="flex items-center justify-between">
                        <span class="text-xs font-bold text-slate-400 uppercase tracking-widest font-sans">Valid</span>
                        <span class="rounded bg-emerald-50 p-2 text-emerald-600 shrink-0">
                            <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                        </span>
                    </div>
                    <p class="text-2xl font-extrabold text-emerald-600 mt-2"><asp:Label ID="lblCompliantVehicles" runat="server" Text="0"></asp:Label></p>
                    <p class="text-[10px] text-emerald-500 mt-1 font-bold"><asp:Label ID="lblCompliantPercent" runat="server" Text="0"></asp:Label>% of Vehicles</p>
                </div>
            </asp:Panel>
 
            <!-- Card 3: Non-Compliant -->
            <asp:Panel ID="pnlNonCompliantCard" runat="server" CssClass="rounded-xl border border-slate-200 bg-white p-5 shadow-sm hover:shadow-md hover:border-orange-400 transition-all w-full flex items-center justify-between cursor-pointer" onclick="document.getElementById('MainContent_btnNonCompliantClick').click();">
                <div class="flex-1 text-left">
                    <div class="flex items-center justify-between">
                        <span class="text-xs font-bold text-slate-400 uppercase tracking-widest font-sans">Expiring</span>
                        <span class="rounded bg-orange-50 p-2 text-orange-600 shrink-0">
                            <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
                        </span>
                    </div>
                    <p class="text-2xl font-extrabold text-orange-600 mt-2"><asp:Label ID="lblNonCompliantVehicles" runat="server" Text="0"></asp:Label></p>
                    <p class="text-[10px] text-orange-500 mt-1 font-bold">Action Required</p>
                </div>
            </asp:Panel>

            <!-- Card 4: Expired -->
            <asp:Panel ID="pnlExpiredCard" runat="server" CssClass="rounded-xl border border-slate-200 bg-white p-5 shadow-sm hover:shadow-md hover:border-red-400 transition-all w-full flex items-center justify-between cursor-pointer" onclick="document.getElementById('MainContent_btnExpiredClick').click();">
                <div class="flex-1 text-left">
                    <div class="flex items-center justify-between">
                        <span class="text-xs font-bold text-slate-400 uppercase tracking-widest font-sans">Expired</span>
                        <span class="rounded bg-red-50 p-2 text-red-600 shrink-0">
                            <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
                        </span>
                    </div>
                    <p class="text-2xl font-extrabold text-red-600 mt-2"><asp:Label ID="lblExpiredVehicles" runat="server" Text="0"></asp:Label></p>
                    <p class="text-[10px] text-red-500 mt-1 font-bold">Gate Blocked</p>
                </div>
            </asp:Panel>
        </div>

        <!-- Vehicle Metrics Summary Section -->
        <asp:Panel ID="pnlMetricsSummary" runat="server" CssClass="grid grid-cols-1 gap-6 max-w-md">
            <!-- Column 1: Summary by Vehicle Type -->
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4">
                <div class="border-b border-slate-100 pb-3">
                    <h3 class="text-xs font-bold text-slate-400 uppercase tracking-widest">Summary by Vehicle Type</h3>
                    <p class="text-[10px] text-slate-500 mt-0.5">Total counts of registered vehicle types</p>
                </div>
                <div class="space-y-2 max-h-[300px] overflow-y-auto">
                    <asp:Repeater ID="rptVehicleTypes" runat="server">
                        <ItemTemplate>
                            <div class="flex justify-between items-center bg-slate-50 hover:bg-slate-100 rounded-lg p-2.5 transition-colors border border-slate-100">
                                <span class="font-bold text-slate-700 text-xs"><%# Eval("VehicleType") %></span>
                                <span class="bg-blue-100 text-blue-800 text-[10px] font-extrabold px-2.5 py-1 rounded-full"><%# Eval("Cnt") %></span>
                            </div>
                        </ItemTemplate>
                        <FooterTemplate>
                            <%# If(rptVehicleTypes.Items.Count = 0, "<div class='py-8 text-center text-xs text-slate-400'>No vehicle types recorded.</div>", "") %>
                        </FooterTemplate>
                    </asp:Repeater>
                </div>
            </div>
        </asp:Panel>

        <!-- Dynamic Detail Panels -->
        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4">
            <!-- Header section of active tab -->
            <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 border-b border-slate-100 pb-4">
                <div>
                    <h3 class="text-sm font-extrabold text-[#001F5B] uppercase tracking-wider">
                        <asp:Label ID="lblActiveTabTitle" runat="server" Text="Registered Vehicles"></asp:Label>
                    </h3>
                    <p class="text-xs text-slate-500 mt-0.5">
                        <asp:Label ID="lblActiveTabDesc" runat="server" Text="Directory of all registered refinery vehicles"></asp:Label>
                    </p>
                </div>

                <div class="flex flex-wrap items-center gap-2">
                    <!-- SuperAdmin Division Filter -->
                    <% If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then %>
                        <asp:DropDownList ID="ddlAlertDept" runat="server" AutoPostBack="true" OnSelectedIndexChanged="FilterAlerts" CssClass="rounded border border-slate-200 bg-slate-50 px-2.5 py-1.5 text-xs font-semibold text-slate-600 outline-none">
                        </asp:DropDownList>
                    <% End If %>


                </div>
            </div>

            <!-- PANEL 1: Total Vehicles View -->
            <asp:Panel ID="pnlTotalVehiclesView" runat="server" CssClass="overflow-x-auto">
                <asp:Repeater ID="rptTotalVehicles" runat="server">
                    <HeaderTemplate>
                        <table class="w-full text-left border-collapse text-xs">
                            <thead>
                                <tr class="border-b border-slate-100 text-slate-400 uppercase font-bold tracking-wider">
                                    <th class="py-3 px-2">Vehicle No</th>
                                    <th class="py-3 px-2">Vehicle Type</th>
                                    <th class="py-3 px-2">Allocated Department</th>
                                    <th class="py-3 px-2">Status</th>
                                    <th class="py-3 px-2 text-right">Action</th>
                                </tr>
                            </thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr class="border-b border-slate-50 hover:bg-slate-50 transition-colors">
                            <td class="py-3 px-2 font-bold font-mono text-slate-800"><%# Eval("VehicleNumber") %></td>
                            <td class="py-3 px-2 font-semibold text-slate-500"><%# Eval("VehicleType") %></td>
                            <td class="py-3 px-2 font-semibold text-slate-500"><%# Eval("DepartmentName") %></td>
                            <td class="py-3 px-2">
                                <span class="rounded-full px-2 py-0.5 text-[10px] font-bold uppercase <%# GetBadgeCSS(Eval("OverallStatus")) %>">
                                    <%# Eval("OverallStatus") %>
                                </span>
                            </td>
                            <td class="py-3 px-2 text-right">
                                <a href="Vehicles.aspx?id=<%# Eval("Id") %>" class="rounded bg-slate-100 text-slate-700 hover:bg-slate-200 px-3 py-1.5 font-bold transition-colors">
                                    View Details
                                </a>
                            </td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                            </tbody>
                        </table>
                        <%# If(rptTotalVehicles.Items.Count = 0, "<div class='py-12 text-center text-xs text-slate-400'>No vehicles registered.</div>", "") %>
                    </FooterTemplate>
                </asp:Repeater>
            </asp:Panel>

            <!-- PANEL 2: Compliant Vehicles View -->
            <asp:Panel ID="pnlCompliantVehiclesView" runat="server" CssClass="overflow-x-auto" Visible="false">
                <asp:Repeater ID="rptCompliantVehicles" runat="server">
                    <HeaderTemplate>
                        <table class="w-full text-left border-collapse text-xs">
                            <thead>
                                <tr class="border-b border-slate-100 text-slate-400 uppercase font-bold tracking-wider">
                                    <th class="py-3 px-2">Vehicle No</th>
                                    <th class="py-3 px-2">Vehicle Type</th>
                                    <th class="py-3 px-2">Allocated Department</th>
                                    <th class="py-3 px-2">Status</th>
                                    <th class="py-3 px-2 text-right">Action</th>
                                </tr>
                            </thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr class="border-b border-slate-50 hover:bg-slate-50 transition-colors">
                            <td class="py-3 px-2 font-bold font-mono text-slate-800"><%# Eval("VehicleNumber") %></td>
                            <td class="py-3 px-2 font-semibold text-slate-500"><%# Eval("VehicleType") %></td>
                            <td class="py-3 px-2 font-semibold text-slate-500"><%# Eval("DepartmentName") %></td>
                            <td class="py-3 px-2">
                                <span class="rounded-full px-2 py-0.5 text-[10px] font-bold uppercase <%# GetBadgeCSS(Eval("OverallStatus")) %>">
                                    <%# Eval("OverallStatus") %>
                                </span>
                            </td>
                            <td class="py-3 px-2 text-right">
                                <a href="Vehicles.aspx?id=<%# Eval("Id") %>" class="rounded bg-slate-100 text-slate-700 hover:bg-slate-200 px-3 py-1.5 font-bold transition-colors">
                                    View Details
                                </a>
                            </td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                            </tbody>
                        </table>
                        <%# If(rptCompliantVehicles.Items.Count = 0, "<div class='py-12 text-center text-xs text-slate-400'>No valid vehicles.</div>", "") %>
                    </FooterTemplate>
                </asp:Repeater>
            </asp:Panel>

            <!-- PANEL 3: Non-Compliant 4-column View -->
            <asp:Panel ID="pnlNonCompliantView" runat="server" Visible="false">
                <div class="grid grid-cols-1 md:grid-cols-4 gap-6">
                    <!-- RC Column -->
                    <div class="rounded-xl border border-slate-100 bg-slate-50/50 p-4 space-y-3">
                        <h4 class="text-xs font-bold text-orange-600 border-b border-orange-100 pb-2 flex items-center justify-between">
                            <span>Registration Certificate (RC)</span>
                            <span class="text-[9px] bg-orange-100 text-orange-700 px-2 py-0.5 rounded-full font-bold">Going to Expire</span>
                        </h4>
                        <div class="space-y-3">
                            <asp:Repeater ID="rptNonCompliantRC" runat="server">
                                <ItemTemplate>
                                    <div class="bg-white border border-slate-100 rounded-lg p-3 space-y-2 shadow-sm">
                                        <div class="flex justify-between items-start">
                                            <span class="font-bold text-slate-800 font-mono text-[11px]"><%# Eval("VehicleNumber") %></span>
                                            <span class="text-[9px] text-slate-400 font-semibold"><%# Eval("DepartmentName") %></span>
                                        </div>
                                        <div class="flex justify-between items-center text-[10px] text-slate-500 pt-1">
                                            <span>Expires: <b class="font-mono text-slate-600"><%# FmtDate(Eval("ExpiryDate")) %></b></span>
                                            <a href="Expiry.aspx?renewId=<%# Eval("Id") %>" class="text-blue-600 hover:underline font-bold">Renew</a>
                                        </div>
                                    </div>
                                </ItemTemplate>
                                <FooterTemplate>
                                    <%# If(rptNonCompliantRC.Items.Count = 0, "<div class='py-8 text-center text-xs text-slate-400'>No pending alerts.</div>", "") %>
                                </FooterTemplate>
                            </asp:Repeater>
                        </div>
                    </div>

                    <!-- Insurance Column -->
                    <div class="rounded-xl border border-slate-100 bg-slate-50/50 p-4 space-y-3">
                        <h4 class="text-xs font-bold text-orange-600 border-b border-orange-100 pb-2 flex items-center justify-between">
                            <span>Vehicle Insurance</span>
                            <span class="text-[9px] bg-orange-100 text-orange-700 px-2 py-0.5 rounded-full font-bold">Going to Expire</span>
                        </h4>
                        <div class="space-y-3">
                            <asp:Repeater ID="rptNonCompliantInsurance" runat="server">
                                <ItemTemplate>
                                    <div class="bg-white border border-slate-100 rounded-lg p-3 space-y-2 shadow-sm">
                                        <div class="flex justify-between items-start">
                                            <span class="font-bold text-slate-800 font-mono text-[11px]"><%# Eval("VehicleNumber") %></span>
                                            <span class="text-[9px] text-slate-400 font-semibold"><%# Eval("DepartmentName") %></span>
                                        </div>
                                        <div class="flex justify-between items-center text-[10px] text-slate-500 pt-1">
                                            <span>Expires: <b class="font-mono text-slate-600"><%# FmtDate(Eval("ExpiryDate")) %></b></span>
                                            <a href="Expiry.aspx?renewId=<%# Eval("Id") %>" class="text-blue-600 hover:underline font-bold">Renew</a>
                                        </div>
                                    </div>
                                </ItemTemplate>
                                <FooterTemplate>
                                    <%# If(rptNonCompliantInsurance.Items.Count = 0, "<div class='py-8 text-center text-xs text-slate-400'>No pending alerts.</div>", "") %>
                                </FooterTemplate>
                            </asp:Repeater>
                        </div>
                    </div>

                    <!-- PUCC Column -->
                    <div class="rounded-xl border border-slate-100 bg-slate-50/50 p-4 space-y-3">
                        <h4 class="text-xs font-bold text-orange-600 border-b border-orange-100 pb-2 flex items-center justify-between">
                            <span>PUC Certificate</span>
                            <span class="text-[9px] bg-orange-100 text-orange-700 px-2 py-0.5 rounded-full font-bold">Going to Expire</span>
                        </h4>
                        <div class="space-y-3">
                            <asp:Repeater ID="rptNonCompliantPUCC" runat="server">
                                <ItemTemplate>
                                    <div class="bg-white border border-slate-100 rounded-lg p-3 space-y-2 shadow-sm">
                                        <div class="flex justify-between items-start">
                                            <span class="font-bold text-slate-800 font-mono text-[11px]"><%# Eval("VehicleNumber") %></span>
                                            <span class="text-[9px] text-slate-400 font-semibold"><%# Eval("DepartmentName") %></span>
                                        </div>
                                        <div class="flex justify-between items-center text-[10px] text-slate-500 pt-1">
                                            <span>Expires: <b class="font-mono text-slate-600"><%# FmtDate(Eval("ExpiryDate")) %></b></span>
                                            <a href="Expiry.aspx?renewId=<%# Eval("Id") %>" class="text-blue-600 hover:underline font-bold">Renew</a>
                                        </div>
                                    </div>
                                </ItemTemplate>
                                <FooterTemplate>
                                    <%# If(rptNonCompliantPUCC.Items.Count = 0, "<div class='py-8 text-center text-xs text-slate-400'>No pending alerts.</div>", "") %>
                                </FooterTemplate>
                            </asp:Repeater>
                        </div>
                    </div>

                    <!-- Fitness Certificate Column -->
                    <div class="rounded-xl border border-slate-100 bg-slate-50/50 p-4 space-y-3">
                        <h4 class="text-xs font-bold text-orange-600 border-b border-orange-100 pb-2 flex items-center justify-between">
                            <span>Fitness Certificate</span>
                            <span class="text-[9px] bg-orange-100 text-orange-700 px-2 py-0.5 rounded-full font-bold">Going to Expire</span>
                        </h4>
                        <div class="space-y-3">
                            <asp:Repeater ID="rptNonCompliantFitness" runat="server">
                                <ItemTemplate>
                                    <div class="bg-white border border-slate-100 rounded-lg p-3 space-y-2 shadow-sm">
                                        <div class="flex justify-between items-start">
                                            <span class="font-bold text-slate-800 font-mono text-[11px]"><%# Eval("VehicleNumber") %></span>
                                            <span class="text-[9px] text-slate-400 font-semibold"><%# Eval("DepartmentName") %></span>
                                        </div>
                                        <div class="flex justify-between items-center text-[10px] text-slate-500 pt-1">
                                            <span>Expires: <b class="font-mono text-slate-600"><%# FmtDate(Eval("ExpiryDate")) %></b></span>
                                            <a href="Expiry.aspx?renewId=<%# Eval("Id") %>" class="text-blue-600 hover:underline font-bold">Renew</a>
                                        </div>
                                    </div>
                                </ItemTemplate>
                                <FooterTemplate>
                                    <%# If(rptNonCompliantFitness.Items.Count = 0, "<div class='py-8 text-center text-xs text-slate-400'>No pending alerts.</div>", "") %>
                                </FooterTemplate>
                            </asp:Repeater>
                        </div>
                    </div>
                </div>
            </asp:Panel>

            <!-- PANEL 4: Expired 4-column View -->
            <asp:Panel ID="pnlExpiredView" runat="server" Visible="false">
                <div class="grid grid-cols-1 md:grid-cols-4 gap-6">
                    <!-- RC Column -->
                    <div class="rounded-xl border border-slate-100 bg-slate-50/50 p-4 space-y-3">
                        <h4 class="text-xs font-bold text-red-600 border-b border-red-100 pb-2 flex items-center justify-between">
                            <span>Registration Certificate (RC)</span>
                            <span class="text-[9px] bg-red-100 text-red-700 px-2 py-0.5 rounded-full font-bold">Expired</span>
                        </h4>
                        <div class="space-y-3">
                            <asp:Repeater ID="rptExpiredRC" runat="server">
                                <ItemTemplate>
                                    <div class="bg-white border border-slate-100 rounded-lg p-3 space-y-2 shadow-sm">
                                        <div class="flex justify-between items-start">
                                            <span class="font-bold text-slate-800 font-mono text-[11px]"><%# Eval("VehicleNumber") %></span>
                                            <span class="text-[9px] text-slate-400 font-semibold"><%# Eval("DepartmentName") %></span>
                                        </div>
                                        <div class="flex justify-between items-center text-[10px] text-slate-500 pt-1">
                                            <span>Expired: <b class="font-mono text-red-500"><%# FmtDate(Eval("ExpiryDate")) %></b></span>
                                            <a href="Expiry.aspx?renewId=<%# Eval("Id") %>" class="text-blue-600 hover:underline font-bold">Renew</a>
                                        </div>
                                    </div>
                                </ItemTemplate>
                                <FooterTemplate>
                                    <%# If(rptExpiredRC.Items.Count = 0, "<div class='py-8 text-center text-xs text-slate-400'>No expired certificates.</div>", "") %>
                                </FooterTemplate>
                            </asp:Repeater>
                        </div>
                    </div>

                    <!-- Insurance Column -->
                    <div class="rounded-xl border border-slate-100 bg-slate-50/50 p-4 space-y-3">
                        <h4 class="text-xs font-bold text-red-600 border-b border-red-100 pb-2 flex items-center justify-between">
                            <span>Vehicle Insurance</span>
                            <span class="text-[9px] bg-red-100 text-red-700 px-2 py-0.5 rounded-full font-bold">Expired</span>
                        </h4>
                        <div class="space-y-3">
                            <asp:Repeater ID="rptExpiredInsurance" runat="server">
                                <ItemTemplate>
                                    <div class="bg-white border border-slate-100 rounded-lg p-3 space-y-2 shadow-sm">
                                        <div class="flex justify-between items-start">
                                            <span class="font-bold text-slate-800 font-mono text-[11px]"><%# Eval("VehicleNumber") %></span>
                                            <span class="text-[9px] text-slate-400 font-semibold"><%# Eval("DepartmentName") %></span>
                                        </div>
                                        <div class="flex justify-between items-center text-[10px] text-slate-500 pt-1">
                                            <span>Expired: <b class="font-mono text-red-500"><%# FmtDate(Eval("ExpiryDate")) %></b></span>
                                            <a href="Expiry.aspx?renewId=<%# Eval("Id") %>" class="text-blue-600 hover:underline font-bold">Renew</a>
                                        </div>
                                    </div>
                                </ItemTemplate>
                                <FooterTemplate>
                                    <%# If(rptExpiredInsurance.Items.Count = 0, "<div class='py-8 text-center text-xs text-slate-400'>No expired certificates.</div>", "") %>
                                </FooterTemplate>
                            </asp:Repeater>
                        </div>
                    </div>

                    <!-- PUCC Column -->
                    <div class="rounded-xl border border-slate-100 bg-slate-50/50 p-4 space-y-3">
                        <h4 class="text-xs font-bold text-red-600 border-b border-red-100 pb-2 flex items-center justify-between">
                            <span>PUC Certificate</span>
                            <span class="text-[9px] bg-red-100 text-red-700 px-2 py-0.5 rounded-full font-bold">Expired</span>
                        </h4>
                        <div class="space-y-3">
                            <asp:Repeater ID="rptExpiredPUCC" runat="server">
                                <ItemTemplate>
                                    <div class="bg-white border border-slate-100 rounded-lg p-3 space-y-2 shadow-sm">
                                        <div class="flex justify-between items-start">
                                            <span class="font-bold text-slate-800 font-mono text-[11px]"><%# Eval("VehicleNumber") %></span>
                                            <span class="text-[9px] text-slate-400 font-semibold"><%# Eval("DepartmentName") %></span>
                                        </div>
                                        <div class="flex justify-between items-center text-[10px] text-slate-500 pt-1">
                                            <span>Expired: <b class="font-mono text-red-500"><%# FmtDate(Eval("ExpiryDate")) %></b></span>
                                            <a href="Expiry.aspx?renewId=<%# Eval("Id") %>" class="text-blue-600 hover:underline font-bold">Renew</a>
                                        </div>
                                    </div>
                                </ItemTemplate>
                                <FooterTemplate>
                                    <%# If(rptExpiredPUCC.Items.Count = 0, "<div class='py-8 text-center text-xs text-slate-400'>No expired certificates.</div>", "") %>
                                </FooterTemplate>
                            </asp:Repeater>
                        </div>
                    </div>

                    <!-- Fitness Certificate Column -->
                    <div class="rounded-xl border border-slate-100 bg-slate-50/50 p-4 space-y-3">
                        <h4 class="text-xs font-bold text-red-600 border-b border-red-100 pb-2 flex items-center justify-between">
                            <span>Fitness Certificate</span>
                            <span class="text-[9px] bg-red-100 text-red-700 px-2 py-0.5 rounded-full font-bold">Expired</span>
                        </h4>
                        <div class="space-y-3">
                            <asp:Repeater ID="rptExpiredFitness" runat="server">
                                <ItemTemplate>
                                    <div class="bg-white border border-slate-100 rounded-lg p-3 space-y-2 shadow-sm">
                                        <div class="flex justify-between items-start">
                                            <span class="font-bold text-slate-800 font-mono text-[11px]"><%# Eval("VehicleNumber") %></span>
                                            <span class="text-[9px] text-slate-400 font-semibold"><%# Eval("DepartmentName") %></span>
                                        </div>
                                        <div class="flex justify-between items-center text-[10px] text-slate-500 pt-1">
                                            <span>Expired: <b class="font-mono text-red-500"><%# FmtDate(Eval("ExpiryDate")) %></b></span>
                                            <a href="Expiry.aspx?renewId=<%# Eval("Id") %>" class="text-blue-600 hover:underline font-bold">Renew</a>
                                        </div>
                                    </div>
                                </ItemTemplate>
                                <FooterTemplate>
                                    <%# If(rptExpiredFitness.Items.Count = 0, "<div class='py-8 text-center text-xs text-slate-400'>No expired certificates.</div>", "") %>
                                </FooterTemplate>
                            </asp:Repeater>
                        </div>
                    </div>
                </div>
            </asp:Panel>
        </div>

        <!-- Super Admin Document Verification Hub (Full Width) -->
        <asp:Panel ID="pnlVerificationHub" runat="server" CssClass="rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4" Visible="false">
            <div>
                <h3 class="text-xs font-bold text-slate-400 uppercase tracking-widest">Refinery Document Verification Hub</h3>
                <p class="text-xs text-slate-500 mt-0.5">Super Admin clearance terminal for newly uploaded RC and compliance certificates</p>
            </div>

            <div class="overflow-x-auto">
                <asp:Repeater ID="rptVerificationDocs" runat="server" OnItemCommand="rptVerificationDocs_ItemCommand">
                    <HeaderTemplate>
                        <table class="w-full text-left border-collapse text-xs">
                            <thead>
                                <tr class="border-b border-slate-100 text-slate-400 uppercase font-bold tracking-wider">
                                    <th class="py-3 px-2">Vehicle No</th>
                                    <th class="py-3 px-2">Department</th>
                                    <th class="py-3 px-2">Document Category</th>
                                    <th class="py-3 px-2">Uploaded File</th>
                                    <th class="py-3 px-2">Cleared Status</th>
                                    <th class="py-3 px-2 text-right">Verification Action</th>
                                </tr>
                            </thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr class="border-b border-slate-50 hover:bg-slate-50 transition-colors">
                            <td class="py-3 px-2 font-bold font-mono text-slate-800"><%# Eval("VehicleNumber") %></td>
                            <td class="py-3 px-2 font-semibold text-slate-500"><%# Eval("DepartmentCode") %></td>
                            <td class="py-3 px-2 font-semibold text-slate-600"><%# Eval("LicenseType").ToString().Replace("_", " ") %></td>
                            <td class="py-3 px-2 font-medium">
                                <a href="<%# Eval("FilePath") %>" target="_blank" rel="noopener noreferrer" class="flex items-center gap-1 text-blue-600 hover:underline font-bold">
                                    <svg class="h-3.5 w-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /><path stroke-linecap="round" stroke-linejoin="round" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" /></svg>
                                    <span class="max-w-[120px] truncate"><%# Eval("FileName") %></span>
                                </a>
                            </td>
                            <td class="py-3 px-2">
                                <span class="rounded px-2 py-0.5 font-bold uppercase text-[9px] <%# If(Convert.ToInt32(Eval("IsVerified")) = 1, "bg-emerald-100 text-emerald-700", "bg-amber-100 text-amber-700") %>">
                                    <%# If(Convert.ToInt32(Eval("IsVerified")) = 1, "Verified", "Pending") %>
                                </span>
                            </td>
                            <td class="py-3 px-2 text-right">
                                <asp:LinkButton ID="btnToggleVerify" runat="server" CommandName="ToggleVerify" CommandArgument='<%# Eval("Id") & "|" & Eval("IsVerified") %>' CssClass='<%# If(Convert.ToInt32(Eval("IsVerified")) = 1, "rounded bg-red-50 text-red-600 hover:bg-red-100 px-3 py-1.5 font-bold transition-colors focus:outline-none", "rounded bg-emerald-50 text-emerald-600 hover:bg-emerald-100 px-3 py-1.5 font-bold transition-colors focus:outline-none") %>'>
                                    <%# If(Convert.ToInt32(Eval("IsVerified")) = 1, "Revoke", "Verify") %>
                                </asp:LinkButton>
                            </td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                            </tbody>
                        </table>
                        <%# If(rptVerificationDocs.Items.Count = 0, "<div class='py-12 text-center text-xs text-slate-400'>No documents pending verification.</div>", "") %>
                    </FooterTemplate>
                </asp:Repeater>
            </div>
        </asp:Panel>
    </div>

</asp:Content>
