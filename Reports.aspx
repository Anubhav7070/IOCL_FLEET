<%@ Page Title="Compliance Reports" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeFile="Reports.aspx.vb" Inherits="ReportsPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="space-y-6">
        <!-- Title Banner -->
        <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 rounded-xl bg-slate-900 border border-slate-800 text-white p-6 shadow-md">
            <div>
                <h2 class="text-xl font-bold tracking-wide uppercase">Compliance Reports</h2>
                <p class="text-xs text-slate-400 mt-1">Generate and download fleet compliance reports in PDF or Excel format.</p>
            </div>
            <div class="flex flex-wrap items-center gap-3">
                <a href="Default.aspx" class="flex items-center gap-1.5 rounded bg-slate-700 hover:bg-slate-600 px-3.5 py-2 text-xs font-bold text-white shadow transition-all duration-200">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" /></svg>
                    <span>Dashboard</span>
                </a>
            </div>
        </div>

        <!-- Department Filter + Download Buttons -->
        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
            <h3 class="text-sm font-bold text-slate-700 mb-4 uppercase tracking-wide">Generate Report</h3>
            <div class="flex flex-col sm:flex-row items-end gap-4">
                <div class="flex-1">
                    <label class="block text-xs font-bold text-slate-500 uppercase tracking-widest mb-1.5">Filter by Department</label>
                    <asp:DropDownList ID="ddlDept" runat="server" CssClass="w-full rounded border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-600 outline-none focus:border-blue-500 transition-all">
                    </asp:DropDownList>
                </div>
                <div class="flex items-center gap-3">
                    <asp:LinkButton ID="btnDownloadPDF" runat="server" OnClick="btnDownloadPDF_Click"
                        CssClass="flex items-center gap-1.5 rounded bg-blue-600 hover:bg-blue-700 px-4 py-2 text-xs font-bold text-white shadow transition-all duration-200 focus:outline-none">
                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" /></svg>
                        <span>Download PDF</span>
                    </asp:LinkButton>
                    <asp:LinkButton ID="btnDownloadExcel" runat="server" OnClick="btnDownloadExcel_Click"
                        CssClass="flex items-center gap-1.5 rounded bg-emerald-600 hover:bg-emerald-700 px-4 py-2 text-xs font-bold text-white shadow transition-all duration-200 focus:outline-none">
                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" /></svg>
                        <span>Download Excel</span>
                    </asp:LinkButton>
                </div>
            </div>
        </div>

        <!-- Report Summary Stats Cards -->
        <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
                <div class="flex items-center justify-between">
                    <span class="text-xs font-bold text-slate-400 uppercase tracking-widest">Total Vehicles</span>
                    <span class="rounded bg-blue-50 p-2 text-blue-600">
                        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0z" /><path stroke-linecap="round" stroke-linejoin="round" d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10M21 16V10a1 1 0 00-1-1h-7m8 7H3" /></svg>
                    </span>
                </div>
                <p class="text-2xl font-extrabold text-slate-800 mt-2"><asp:Label ID="lblTotal" runat="server" Text="0"></asp:Label></p>
                <p class="text-[10px] text-slate-400 mt-1 font-semibold">Registered</p>
            </div>
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
                <div class="flex items-center justify-between">
                    <span class="text-xs font-bold text-slate-400 uppercase tracking-widest">Total Licenses</span>
                    <span class="rounded bg-purple-50 p-2 text-purple-600">
                        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
                    </span>
                </div>
                <p class="text-2xl font-extrabold text-slate-800 mt-2"><asp:Label ID="lblTotalLicenses" runat="server" Text="0"></asp:Label></p>
                <p class="text-[10px] text-slate-400 mt-1 font-semibold">Compliance Records</p>
            </div>
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
                <div class="flex items-center justify-between">
                    <span class="text-xs font-bold text-slate-400 uppercase tracking-widest">Expiring / Expired</span>
                    <span class="rounded bg-red-50 p-2 text-red-600">
                        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                    </span>
                </div>
                <p class="text-2xl font-extrabold text-red-600 mt-2"><asp:Label ID="lblExpiring" runat="server" Text="0"></asp:Label></p>
                <p class="text-[10px] text-red-500 mt-1 font-semibold">Need Renewal</p>
            </div>
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
                <div class="flex items-center justify-between">
                    <span class="text-xs font-bold text-slate-400 uppercase tracking-widest">Avg Score</span>
                    <span class="rounded bg-emerald-50 p-2 text-emerald-600">
                        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                    </span>
                </div>
                <p class="text-2xl font-extrabold text-emerald-600 mt-2"><asp:Label ID="lblAvgScore" runat="server" Text="0"></asp:Label>%</p>
                <p class="text-[10px] text-emerald-500 mt-1 font-semibold">Fleet Compliance Score</p>
            </div>
        </div>

        <!-- Department Breakdown Table -->
        <div class="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
            <div class="px-5 py-4 border-b border-slate-100">
                <h3 class="text-sm font-bold text-slate-700 uppercase tracking-wide">Department Compliance Breakdown</h3>
            </div>
            <div class="overflow-x-auto">
                <table class="w-full text-xs">
                    <thead>
                        <tr class="bg-slate-50 border-b border-slate-100">
                            <th class="text-left px-4 py-3 font-bold text-slate-500 uppercase tracking-wide">Department</th>
                            <th class="text-left px-4 py-3 font-bold text-slate-500 uppercase tracking-wide">Division</th>
                            <th class="text-center px-4 py-3 font-bold text-slate-500 uppercase tracking-wide">Vehicles</th>
                            <th class="text-center px-4 py-3 font-bold text-slate-500 uppercase tracking-wide">Compliance Score</th>
                            <th class="text-center px-4 py-3 font-bold text-slate-500 uppercase tracking-wide">Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        <asp:Repeater ID="rptDepts" runat="server">
                            <ItemTemplate>
                                <tr class="border-b border-slate-50 hover:bg-slate-25 transition-colors">
                                    <td class="px-4 py-3 font-semibold text-slate-700"><%# Eval("Name") %></td>
                                    <td class="px-4 py-3 text-slate-500"><%# Eval("Division") %></td>
                                    <td class="px-4 py-3 text-center font-bold text-slate-700"><%# Eval("VehicleCount") %></td>
                                    <td class="px-4 py-3 text-center">
                                        <span class="font-extrabold <%# GetScoreClass(Eval("ComplianceScore")) %>"><%# String.Format("{0:0.0}", Eval("ComplianceScore")) %>%</span>
                                    </td>
                                    <td class="px-4 py-3 text-center">
                                        <asp:LinkButton runat="server" CommandName="DeptPDF" CommandArgument='<%# Eval("Id") %>' OnCommand="rptDepts_Command"
                                            CssClass="inline-flex items-center gap-1 rounded bg-blue-600 hover:bg-blue-700 px-2.5 py-1 text-[10px] font-bold text-white transition-all">PDF</asp:LinkButton>
                                        <asp:LinkButton runat="server" CommandName="DeptExcel" CommandArgument='<%# Eval("Id") %>' OnCommand="rptDepts_Command"
                                            CssClass="inline-flex items-center gap-1 rounded bg-emerald-600 hover:bg-emerald-700 px-2.5 py-1 text-[10px] font-bold text-white transition-all ml-1">Excel</asp:LinkButton>
                                    </td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</asp:Content>
