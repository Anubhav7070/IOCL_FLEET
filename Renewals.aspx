<%@ Page Title="Renewal History" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeFile="Renewals.aspx.vb" Inherits="RenewalsHistoryPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>


<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" runat="server">
    <div class="space-y-6">
        <!-- Title bar -->
        <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4 rounded-xl bg-slate-900 border border-slate-800 text-white p-6 shadow-md shadow-slate-900/10">
            <div>
                <h2 class="text-xl font-bold tracking-wide uppercase">Historical Renewal Logs</h2>
                <p class="text-xs text-slate-400 mt-1">
                    Review history of vehicle document updates, old and new certifications, and operator comments.
                </p>
            </div>
        </div>

        <!-- Filter Control Panel -->
        <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm flex flex-col md:flex-row md:items-center gap-4">
            <div class="relative flex-1">
                <span class="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none text-slate-400">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                </span>
                <asp:TextBox ID="txtSearchPlate" runat="server" OnTextChanged="FilterLogs" AutoPostBack="true" placeholder="Search vehicle plate number..." CssClass="w-full rounded border border-slate-200 bg-slate-50 pl-9 pr-4 py-2 text-xs font-semibold text-slate-600 placeholder-slate-400 outline-none focus:border-blue-500 focus:bg-white transition-all"></asp:TextBox>
            </div>

            <div class="flex items-center gap-3">
                <asp:DropDownList ID="ddlLicenseFilter" runat="server" AutoPostBack="true" OnSelectedIndexChanged="FilterLogs" CssClass="rounded border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-600 outline-none focus:border-blue-500 transition-all">
                    <asp:ListItem Text="All Document Types" Value=""></asp:ListItem>
                    <asp:ListItem Text="Road Permit" Value="ROAD_PERMIT"></asp:ListItem>
                    <asp:ListItem Text="Age Determination" Value="AGE_DETERMINATION"></asp:ListItem>
                    <asp:ListItem Text="PUC" Value="PUC"></asp:ListItem>
                    <asp:ListItem Text="Fitness" Value="FITNESS"></asp:ListItem>
                    <asp:ListItem Text="Explosive" Value="EXPLOSIVE"></asp:ListItem>
                    <asp:ListItem Text="Green Card" Value="GREEN_CARD"></asp:ListItem>
                    <asp:ListItem Text="Insurance" Value="INSURANCE"></asp:ListItem>
                    <asp:ListItem Text="Calibration" Value="CALIBRATION"></asp:ListItem>
                </asp:DropDownList>
                
                <asp:LinkButton ID="btnClearFilters" runat="server" OnClick="btnReset_Click" CssClass="text-xs font-semibold text-slate-500 hover:text-slate-800 px-2">Reset</asp:LinkButton>
            </div>
        </div>

        <!-- History Grid -->
        <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4">
            <div class="border-b border-slate-100 pb-3">
                <span class="text-xs font-extrabold text-slate-400 uppercase tracking-widest">Logs History</span>
            </div>

            <div class="overflow-x-auto">
                <asp:Repeater ID="rptHistory" runat="server">
                    <HeaderTemplate>
                        <table class="w-full text-left border-collapse text-xs">
                            <thead>
                                <tr class="border-b border-slate-100 text-slate-400 uppercase font-bold tracking-wider">
                                    <th class="py-3 px-2">Vehicle No</th>
                                    <th class="py-3 px-2">Document Type</th>
                                    <th class="py-3 px-2">Old Expiry</th>
                                    <th class="py-3 px-2">New Expiry</th>
                                    <th class="py-3 px-2">Renewed By</th>
                                    <th class="py-3 px-2">Renewed At</th>
                                    <th class="py-3 px-2">Remarks</th>
                                    <th class="py-3 px-2 text-right">Documents</th>
                                </tr>
                            </thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr class="border-b border-slate-50 hover:bg-slate-50 transition-colors">
                            <td class="py-3 px-2 font-bold font-mono text-slate-800"><%# Eval("VehicleNumber") %></td>
                            <td class="py-3 px-2 font-semibold text-slate-600"><%# Eval("LicenseType").ToString().Replace("_", " ") %></td>
                            <td class="py-3 px-2 font-semibold text-slate-500 font-mono"><%# FmtDate(Eval("OldExpiryDate")) %></td>
                            <td class="py-3 px-2 font-semibold text-slate-850 font-mono"><%# FmtDate(Eval("NewExpiryDate")) %></td>
                            <td class="py-3 px-2 font-bold text-slate-700"><%# Eval("EmployeeName") %></td>
                            <td class="py-3 px-2 text-slate-500"><%# FmtDateTime(Eval("RenewedAt")) %></td>
                            <td class="py-3 px-2 text-slate-600 max-w-xs truncate" title="<%# Eval("Remarks") %>"><%# If(Convert.IsDBNull(Eval("Remarks")) OrElse String.IsNullOrEmpty(Eval("Remarks").ToString()), "-", Eval("Remarks")) %></td>
                            <td class="py-3 px-2 text-right">
                                <div class="flex justify-end gap-2 text-[10px] font-bold">
                                    <%# GetDocLink(Eval("OldDocPath"), "Old") %>
                                    <%# GetDocLink(Eval("NewDocPath"), "New") %>
                                </div>
                            </td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                            </tbody>
                        </table>
                        <%# If(rptHistory.Items.Count = 0, "<div class='py-12 text-center text-xs text-slate-400'>No historical renewals logged.</div>", "") %>
                    </FooterTemplate>
                </asp:Repeater>
            </div>
        </div>
    </div>
</asp:Content>
