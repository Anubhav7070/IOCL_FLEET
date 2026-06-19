<%@ Page Title="Document Vault" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeFile="Vault.aspx.vb" Inherits="VaultPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>


<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" runat="server">
    <div class="space-y-6">
        <!-- Stats Cards -->
        <div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
            <div class="rounded-xl border border-blue-200 bg-blue-50/50 p-4 flex items-center gap-3">
                <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-white border border-blue-200 text-[#0054A6]">
                    <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
                </div>
                <div>
                    <p class="text-[9px] font-bold text-slate-400 uppercase tracking-widest">Total Records</p>
                    <p class="text-xl font-extrabold text-[#0054A6] mt-0.5"><asp:Label ID="lblTotalDocs" runat="server" Text="0"></asp:Label></p>
                </div>
            </div>

            <div class="rounded-xl border border-purple-200 bg-purple-50/50 p-4 flex items-center gap-3">
                <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-white border border-purple-200 text-purple-600">
                    <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" /></svg>
                </div>
                <div>
                    <p class="text-[9px] font-bold text-slate-400 uppercase tracking-widest">RC Copies</p>
                    <p class="text-xl font-extrabold text-purple-600 mt-0.5"><asp:Label ID="lblRcCopies" runat="server" Text="0"></asp:Label></p>
                </div>
            </div>

            <div class="rounded-xl border border-orange-200 bg-orange-50/50 p-4 flex items-center gap-3">
                <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-white border border-orange-200 text-orange-600">
                    <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" /></svg>
                </div>
                <div>
                    <p class="text-[9px] font-bold text-slate-400 uppercase tracking-widest">Compliance Docs</p>
                    <p class="text-xl font-extrabold text-orange-600 mt-0.5"><asp:Label ID="lblComplianceDocs" runat="server" Text="0"></asp:Label></p>
                </div>
            </div>

            <div class="rounded-xl border border-emerald-200 bg-emerald-50/50 p-4 flex items-center gap-3">
                <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-white border border-emerald-200 text-emerald-600">
                    <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.952 11.952 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" /></svg>
                </div>
                <div>
                    <p class="text-[9px] font-bold text-slate-400 uppercase tracking-widest">Verified Docs</p>
                    <p class="text-xl font-extrabold text-emerald-600 mt-0.5"><asp:Label ID="lblVerifiedDocs" runat="server" Text="0"></asp:Label></p>
                </div>
            </div>
        </div>

        <!-- Filters Section -->
        <div class="flex flex-col sm:flex-row gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <div class="relative flex-1">
                <span class="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none text-slate-400">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                </span>
                <asp:TextBox ID="txtSearch" runat="server" OnTextChanged="FilterDocs" AutoPostBack="true" placeholder="Search by vehicle number, department, document type..." CssClass="w-full rounded border border-slate-200 bg-slate-50 pl-9 pr-4 py-2 text-xs font-semibold text-slate-600 outline-none focus:border-blue-500 focus:bg-white transition-all"></asp:TextBox>
            </div>
            <asp:DropDownList ID="ddlFilterType" runat="server" AutoPostBack="true" OnSelectedIndexChanged="FilterDocs" CssClass="rounded border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-600 outline-none focus:border-blue-500 transition-all font-sans">
                <asp:ListItem Text="All Document Types" Value=""></asp:ListItem>
                <asp:ListItem Text="RC Copies Only" Value="VEHICLE_RC"></asp:ListItem>
                <asp:ListItem Text="Compliance Certs Only" Value="COMPLIANCE"></asp:ListItem>
            </asp:DropDownList>
            <asp:DropDownList ID="ddlFilterVerified" runat="server" AutoPostBack="true" OnSelectedIndexChanged="FilterDocs" CssClass="rounded border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-600 outline-none focus:border-blue-500 transition-all font-sans">
                <asp:ListItem Text="All Verification Status" Value=""></asp:ListItem>
                <asp:ListItem Text="Verified Only" Value="1"></asp:ListItem>
                <asp:ListItem Text="Pending Only" Value="0"></asp:ListItem>
            </asp:DropDownList>
            <asp:LinkButton ID="btnRefresh" runat="server" OnClick="btnRefresh_Click" CssClass="flex items-center gap-1.5 rounded bg-blue-600 hover:bg-blue-700 px-4 py-2 text-xs font-bold text-white shadow transition-all duration-200 focus:outline-none shrink-0 justify-center">
                <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 1121.21 8.89M9 11l3 3m0 0l3-3m-3 3V2" /></svg>
                <span>Refresh</span>
            </asp:LinkButton>
        </div>

        <!-- Document Table -->
        <div class="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
            <div class="flex items-center justify-between px-5 py-3 bg-slate-50 border-b border-slate-200">
                <p class="text-xs font-bold text-slate-500 uppercase tracking-widest">
                    Compliance Record Registry
                </p>
            </div>

            <div class="overflow-x-auto">
                <asp:Repeater ID="rptVault" runat="server" OnItemCommand="rptVault_ItemCommand">
                    <HeaderTemplate>
                        <table class="w-full text-left text-xs border-collapse">
                            <thead>
                                <tr class="border-b border-slate-200 bg-slate-50 text-slate-500 font-bold uppercase tracking-wider">
                                    <th class="p-4 w-12">#</th>
                                    <th class="p-4">Document Type</th>
                                    <th class="p-4">Vehicle No</th>
                                    <th class="p-4">Department</th>
                                    <th class="p-4">Expiry Date</th>
                                    <th class="p-4">Status</th>
                                    <th class="p-4">Registered By</th>
                                    <th class="p-4">Verified</th>
                                    <th class="p-4 text-right">Action</th>
                                </tr>
                            </thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr class="border-b border-slate-100 hover:bg-slate-50/60 transition-colors">
                            <td class="p-4 font-mono text-slate-400"><%# Container.ItemIndex + 1 %></td>
                            <td class="p-4">
                                <div class="flex items-center gap-3">
                                    <div class="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg border <%# If(Eval("LicenseType").ToString() = "VEHICLE_RC", "bg-purple-50 border-purple-200 text-purple-600", "bg-orange-50 border-orange-200 text-orange-600") %>">
                                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
                                    </div>
                                    <div class="min-w-0">
                                        <p class="text-xs font-bold text-slate-700"><%# Eval("LicenseType").ToString().Replace("_", " ") %></p>
                                        <%# If(HasDocument(Eval("FilePath")), "<a href='" & ResolveUrl("~" & Eval("FilePath").ToString()) & "' target='_blank' class='text-[10px] text-blue-600 hover:underline font-semibold'>View PDF</a>", "<span class='text-[10px] text-slate-400 italic'>No PDF uploaded</span>") %>
                                    </div>
                                </div>
                            </td>
                            <td class="p-4 font-bold font-mono tracking-wider text-slate-700"><%# Eval("VehicleNumber") %></td>
                            <td class="p-4 font-semibold text-slate-600"><%# Eval("DeptCode") %></td>
                            <td class="p-4 font-mono text-slate-600 whitespace-nowrap">
                                <%# FmtDate(Eval("ExpiryDate")) %>
                                <%# If(Not Convert.IsDBNull(Eval("ExpiryDate")) AndAlso Not String.IsNullOrEmpty(Eval("ExpiryDate").ToString()) AndAlso Convert.ToDateTime(Eval("ExpiryDate")) <= DateTime.Today.AddMonths(2), "<br/><span class='text-[9px] font-bold text-orange-500'>⚠ Expires soon</span>", "") %>
                            </td>
                            <td class="p-4">
                                <span class="rounded-full px-2 py-0.5 text-[9px] font-bold uppercase <%# GetStatusBadgeClass(Eval("Status")) %>">
                                    <%# Eval("Status") %>
                                </span>
                            </td>
                            <td class="p-4 font-bold text-slate-600"><%# Eval("EmployeeName") %></td>
                            <td class="p-4">
                                <span class="rounded-full px-2 py-0.5 text-[9px] font-bold uppercase <%# If(Convert.ToInt32(Eval("IsVerified")) = 1, "bg-emerald-100 text-emerald-700", "bg-amber-100 text-amber-700") %>">
                                    <%# If(Convert.ToInt32(Eval("IsVerified")) = 1, "Verified", "Pending") %>
                                </span>
                            </td>
                            <td class="p-4 text-right">
                                <% If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then %>
                                    <asp:LinkButton ID="btnToggleVerify" runat="server" CommandName="ToggleVerify" CommandArgument='<%# Eval("Id") & "|" & Eval("IsVerified") & "|" & Eval("LicenseType") & "|" & Eval("VehicleId") %>' CssClass='<%# If(Convert.ToInt32(Eval("IsVerified")) = 1, "rounded bg-red-50 text-red-600 hover:bg-red-100 px-3 py-1.5 text-[10px] font-bold border border-red-200 transition-colors focus:outline-none", "rounded bg-emerald-50 text-emerald-700 hover:bg-emerald-100 px-3 py-1.5 text-[10px] font-bold border border-emerald-200 transition-colors focus:outline-none") %>'>
                                        <%# If(Convert.ToInt32(Eval("IsVerified")) = 1, "Revoke", "Verify") %>
                                    </asp:LinkButton>
                                <% End If %>
                            </td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                            </tbody>
                        </table>
                        <%# If(rptVault.Items.Count = 0, "<div class='py-12 text-center text-xs text-slate-400'>No compliance records found matching criteria.</div>", "") %>
                    </FooterTemplate>
                </asp:Repeater>
            </div>
        </div>
    </div>
</asp:Content>
