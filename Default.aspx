<%@ Page Title="Dashboard" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeFile="Default.aspx.vb" Inherits="DefaultPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <!-- ChartJS library -->
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="space-y-6">
        <!-- Dashboard Title Banner -->
        <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 rounded-lg bg-slate-900 border border-slate-800 text-white p-6 shadow-md shadow-slate-900/10">
            <div>
                <h2 class="text-xl font-bold tracking-wide uppercase">Refinery Operations Terminal</h2>
                <p class="text-xs text-slate-400 mt-1">
                    Monitoring gate entry clearance and document compliance logs for <%= GetBannerScopeText() %>.
                </p>
            </div>

            <!-- Export Report Actions -->
            <div class="flex flex-wrap items-center gap-3">
                <asp:LinkButton ID="btnExportPDF" runat="server" OnClick="btnExportPDF_Click" CssClass="flex items-center gap-1.5 rounded bg-blue-600 hover:bg-blue-700 px-3.5 py-2 text-xs font-bold text-white shadow transition-all duration-200 focus:outline-none">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" /></svg>
                    <span>Export PDF Log</span>
                </asp:LinkButton>
                <asp:LinkButton ID="btnExportExcel" runat="server" OnClick="btnExportExcel_Click" CssClass="flex items-center gap-1.5 rounded bg-emerald-600 hover:bg-emerald-700 px-3.5 py-2 text-xs font-bold text-white shadow transition-all duration-200 focus:outline-none">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" /></svg>
                    <span>Export Excel Sheet</span>
                </asp:LinkButton>
                <% If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then %>
                <asp:LinkButton ID="btnTriggerEmails" runat="server" OnClick="btnTriggerEmails_Click" CssClass="flex items-center gap-1.5 rounded bg-purple-600 hover:bg-purple-700 px-3.5 py-2 text-xs font-bold text-white shadow transition-all duration-200 focus:outline-none">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" /></svg>
                    <span>Trigger Compliance Emails</span>
                </asp:LinkButton>
                <% End If %>
                <a href="Reports.aspx" class="flex items-center gap-1.5 rounded bg-slate-700 hover:bg-slate-600 px-3.5 py-2 text-xs font-bold text-white shadow transition-all duration-200">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
                    <span>Reports</span>
                </a>
            </div>
        </div>

        <!-- KPI Cards -->
        <div class="grid grid-cols-2 lg:grid-cols-5 gap-4">
            <!-- Card 1: Total Fleet -->
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm hover:shadow-md transition-shadow">
                <div class="flex items-center justify-between">
                    <span class="text-xs font-bold text-slate-400 uppercase tracking-widest">Total Fleet</span>
                    <span class="rounded bg-blue-50 p-2 text-blue-600">
                        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0z" /><path stroke-linecap="round" stroke-linejoin="round" d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10M21 16V10a1 1 0 00-1-1h-7m8 7H3" /></svg>
                    </span>
                </div>
                <p class="text-2xl font-extrabold text-slate-800 mt-2"><asp:Label ID="lblTotalVehicles" runat="server" Text="0"></asp:Label></p>
                <p class="text-[10px] text-slate-400 mt-1 font-semibold">Registered Vehicles</p>
            </div>

            <!-- Card 2: Compliant -->
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm hover:shadow-md transition-shadow">
                <div class="flex items-center justify-between">
                    <span class="text-xs font-bold text-slate-400 uppercase tracking-widest">Compliant</span>
                    <span class="rounded bg-emerald-50 p-2 text-emerald-600">
                        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                    </span>
                </div>
                <p class="text-2xl font-extrabold text-emerald-600 mt-2"><asp:Label ID="lblCompliantVehicles" runat="server" Text="0"></asp:Label></p>
                <p class="text-[10px] text-emerald-500 mt-1 font-bold"><asp:Label ID="lblCompliantPercent" runat="server" Text="0"></asp:Label>% of Fleet</p>
            </div>

            <!-- Card 3: Warning -->
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm hover:shadow-md transition-shadow">
                <div class="flex items-center justify-between">
                    <span class="text-xs font-bold text-slate-400 uppercase tracking-widest">Warning</span>
                    <span class="rounded bg-yellow-50 p-2 text-yellow-600">
                        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                    </span>
                </div>
                <p class="text-2xl font-extrabold text-yellow-600 mt-2"><asp:Label ID="lblWarningVehicles" runat="server" Text="0"></asp:Label></p>
                <p class="text-[10px] text-yellow-500 mt-1 font-bold">Pending Expiry</p>
            </div>

            <!-- Card 4: Critical -->
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm hover:shadow-md transition-shadow">
                <div class="flex items-center justify-between">
                    <span class="text-xs font-bold text-slate-400 uppercase tracking-widest">Critical</span>
                    <span class="rounded bg-orange-50 p-2 text-orange-600">
                        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
                    </span>
                </div>
                <p class="text-2xl font-extrabold text-orange-600 mt-2"><asp:Label ID="lblCriticalVehicles" runat="server" Text="0"></asp:Label></p>
                <p class="text-[10px] text-orange-500 mt-1 font-bold">Action Required</p>
            </div>

            <!-- Card 5: Expired -->
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm hover:shadow-md transition-shadow">
                <div class="flex items-center justify-between">
                    <span class="text-xs font-bold text-slate-400 uppercase tracking-widest">Expired</span>
                    <span class="rounded bg-red-50 p-2 text-red-600">
                        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
                    </span>
                </div>
                <p class="text-2xl font-extrabold text-red-600 mt-2"><asp:Label ID="lblExpiredVehicles" runat="server" Text="0"></asp:Label></p>
                <p class="text-[10px] text-red-500 mt-1 font-bold">Gate Blocked</p>
            </div>
        </div>

        <!-- Charts Row -->
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <div class="rounded-xl border border-slate-200 bg-white p-5 flex flex-col h-80 shadow-sm">
                <h3 class="text-xs font-bold text-slate-400 uppercase tracking-widest mb-4">Fleet Status Breakdown</h3>
                <div class="relative flex-1 w-full h-full">
                    <canvas id="statusChart"></canvas>
                </div>
            </div>
            
            <div class="lg:col-span-2 rounded-xl border border-slate-200 bg-white p-5 flex flex-col h-80 shadow-sm">
                <h3 class="text-xs font-bold text-slate-400 uppercase tracking-widest mb-4">Division Compliance Comparison</h3>
                <div class="relative flex-1 w-full h-full">
                    <canvas id="deptChart"></canvas>
                </div>
            </div>
        </div>

        <!-- Alerts & Verification Section -->
        <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
            <!-- Alerts & Expiries List -->
            <div class="xl:col-span-2 rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4">
                <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 border-b border-slate-100 pb-4">
                    <div>
                        <h3 class="text-xs font-bold text-slate-400 uppercase tracking-widest">Compliance Alerts & Expiring Certificates</h3>
                        <p class="text-xs text-slate-500 mt-0.5">Vehicles with warning or expired status logs</p>
                    </div>
                    <!-- Filters -->
                    <div class="flex items-center gap-2">
                        <% If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then %>
                            <asp:DropDownList ID="ddlAlertDept" runat="server" AutoPostBack="true" OnSelectedIndexChanged="FilterAlerts" CssClass="rounded border border-slate-200 bg-slate-50 px-2 py-1.5 text-xs font-semibold text-slate-600 outline-none">
                            </asp:DropDownList>
                        <% End If %>
                        <asp:DropDownList ID="ddlAlertPriority" runat="server" AutoPostBack="true" OnSelectedIndexChanged="FilterAlerts" CssClass="rounded border border-slate-200 bg-slate-50 px-2 py-1.5 text-xs font-semibold text-slate-600 outline-none">
                            <asp:ListItem Value="" Text="All Priorities"></asp:ListItem>
                            <asp:ListItem Value="HIGH" Text="Expired / High"></asp:ListItem>
                            <asp:ListItem Value="MEDIUM" Text="Medium"></asp:ListItem>
                            <asp:ListItem Value="LOW" Text="Warning / Low"></asp:ListItem>
                        </asp:DropDownList>
                    </div>
                </div>

                <div class="overflow-x-auto">
                    <asp:Repeater ID="rptAlerts" runat="server">
                        <HeaderTemplate>
                            <table class="w-full text-left border-collapse text-xs">
                                <thead>
                                    <tr class="border-b border-slate-100 text-slate-400 uppercase font-bold tracking-wider">
                                        <th class="py-3 px-2">Vehicle No</th>
                                        <th class="py-3 px-2">Division</th>
                                        <th class="py-3 px-2">Document Slot</th>
                                        <th class="py-3 px-2">Status</th>
                                        <th class="py-3 px-2">Expiry Date</th>
                                        <th class="py-3 px-2 text-right">Action</th>
                                    </tr>
                                </thead>
                                <tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr class="border-b border-slate-50 hover:bg-slate-50 transition-colors">
                                <td class="py-3 px-2 font-bold font-mono text-slate-800"><%# Eval("VehicleNumber") %></td>
                                <td class="py-3 px-2 font-semibold text-slate-500"><%# Eval("DepartmentCode") %></td>
                                <td class="py-3 px-2 font-semibold text-slate-600"><%# Eval("LicenseType").ToString().Replace("_", " ") %></td>
                                <td class="py-3 px-2">
                                    <span class="rounded-full px-2 py-0.5 text-[10px] font-bold uppercase <%# GetBadgeCSS(Eval("Status")) %>">
                                        <%# Eval("Status") %>
                                    </span>
                                </td>
                                <td class="py-3 px-2 font-semibold text-slate-600 font-mono"><%# FmtDate(Eval("ExpiryDate")) %></td>
                                <td class="py-3 px-2 text-right">
                                    <a href="Expiry.aspx?renewId=<%# Eval("Id") %>" class="rounded bg-blue-50 text-blue-600 hover:bg-blue-100 px-2.5 py-1.5 font-bold transition-colors">
                                        View
                                    </a>
                                </td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate>
                                </tbody>
                            </table>
                            <%# If(rptAlerts.Items.Count = 0, "<div class='py-12 text-center text-xs text-slate-400'>No active compliance alerts.</div>", "") %>
                        </FooterTemplate>
                    </asp:Repeater>
                </div>
            </div>

            <!-- Recent Audits & Notifications -->
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm flex flex-col h-full space-y-4">
                <h3 class="text-xs font-bold text-slate-400 uppercase tracking-widest border-b border-slate-100 pb-4">Terminal Operations Log</h3>
                
                <div class="flex-1 overflow-y-auto space-y-4 max-h-[400px] pr-1">
                    <asp:Repeater ID="rptAuditFeed" runat="server">
                        <ItemTemplate>
                            <div class="rounded-lg border border-slate-100 bg-slate-50/50 p-3 text-[11px] space-y-1.5">
                                <div class="flex items-center justify-between">
                                    <span class="font-bold text-blue-600 uppercase tracking-wide"><%# Eval("Action").ToString().Replace("_", " ") %></span>
                                    <span class="text-[9px] text-slate-400 font-mono"><%# Eval("FormattedTime") %></span>
                                </div>
                                <p class="text-slate-600 font-semibold leading-relaxed"><%# Eval("Description") %></p>
                                <p class="text-[9px] text-slate-400 font-mono">Operator: <span class="text-slate-500 font-bold"><%# Eval("Username") %></span></p>
                            </div>
                        </ItemTemplate>
                        <FooterTemplate>
                            <%# If(rptAuditFeed.Items.Count = 0, "<div class='py-8 text-center text-xs text-slate-400'>No recent operations logged.</div>", "") %>
                        </FooterTemplate>
                    </asp:Repeater>
                </div>
            </div>
        </div>

        <!-- Super Admin Document Verification & Email Dispatch Hub -->
        <% If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then %>
            <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
                <!-- Document Verification Hub -->
                <div class="lg:col-span-2 rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4">
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
                                            <th class="py-3 px-2">Division</th>
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
                                        <a href="<%# Eval("FilePath") %>" target="_blank" rel="noopener noreferrer" class="flex items-center gap-1 text-blue-600 hover:underline">
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
                                        <asp:LinkButton ID="btnToggleVerify" runat="server" CommandName="ToggleVerify" CommandArgument='<%# Eval("Id") & "|" & Eval("IsVerified") %>' CssClass='<%# If(Convert.ToInt32(Eval("IsVerified")) = 1, "rounded bg-red-50 text-red-600 hover:bg-red-100 px-3 py-1.5 font-bold transition-colors", "rounded bg-emerald-50 text-emerald-600 hover:bg-emerald-100 px-3 py-1.5 font-bold transition-colors") %>'>
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
                </div>

                <!-- Email & Alert Dispatcher Control Center -->
                <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4 flex flex-col justify-between">
                    <div>
                        <h3 class="text-xs font-bold text-slate-400 uppercase tracking-widest">Email & Alert Dispatcher</h3>
                        <p class="text-xs text-slate-500 mt-0.5">Clearance notifications and daily digests triggered manually for all departments</p>
                    </div>

                    <div class="flex-1 space-y-4 flex flex-col justify-center my-4">
                        <div class="space-y-3">
                            <asp:LinkButton ID="btnDailyDigest" runat="server" OnClick="btnDailyDigest_Click" CssClass="w-full flex items-center justify-center gap-2 rounded bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 py-3 text-xs font-bold text-white shadow transition-all duration-200 focus:outline-none">
                                <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                                    <path stroke-linecap="round" stroke-linejoin="round" d="M3 19v-8.93a2 2 0 01.89-1.664l8-5.333a2 2 0 012.22 0l8 5.333A2 2 0 0121 10.07V19M3 19a2 2 0 002 2h14a2 2 0 002-2M3 19l6.75-4.5M21 19l-6.75-4.5M3 10l6.75 4.5M21 10l-6.75-4.5m0 0l-2.25-1.5a2 2 0 00-2.22 0l-2.25 1.5M12 14.25a8.25 8.25 0 00-8.25-8.25h16.5A8.25 8.25 0 0012 14.25z" />
                                </svg>
                                <span>Send Daily summary to all departments</span>
                            </asp:LinkButton>

                            <asp:LinkButton ID="btnComplianceScan" runat="server" OnClick="btnComplianceScan_Click" CssClass="w-full flex items-center justify-center gap-2 rounded bg-gradient-to-r from-orange-500 to-amber-600 hover:from-orange-600 hover:to-amber-700 py-3 text-xs font-bold text-white shadow transition-all duration-200 focus:outline-none">
                                <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                                    <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                                </svg>
                                <span>Run Alert Scan & Send Emails</span>
                            </asp:LinkButton>
                        </div>

                        <asp:Panel ID="pnlDispatcherStatus" runat="server" Visible="false" CssClass="rounded-lg border p-3 text-xs font-semibold">
                            <asp:Label ID="lblDispatcherStatus" runat="server"></asp:Label>
                        </asp:Panel>
                    </div>
                </div>
            </div>
        <% End If %>
    </div>

    <!-- Chart Configuration Script -->
    <script>
        window.charts = {
            doughnutChart: null,
            barChart: null,

            createDoughnut: function (canvasId, data, labels) {
                var canvas = document.getElementById(canvasId);
                if (!canvas) return;

                if (this.doughnutChart) {
                    this.doughnutChart.destroy();
                }

                var ctx = canvas.getContext('2d');
                this.doughnutChart = new Chart(ctx, {
                    type: 'doughnut',
                    data: {
                        labels: labels,
                        datasets: [{
                            data: data,
                            backgroundColor: ['#10B981', '#F59E0B', '#F97316', '#EF4444'], // green, yellow, orange, red
                            borderWidth: 1,
                            borderColor: '#ffffff'
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: {
                                position: 'bottom',
                                labels: {
                                    font: { size: 10, weight: 'bold' },
                                    boxWidth: 12
                                }
                            }
                        }
                    }
                });
            },

            createBar: function (canvasId, data, labels) {
                var canvas = document.getElementById(canvasId);
                if (!canvas) return;

                if (this.barChart) {
                    this.barChart.destroy();
                }

                var ctx = canvas.getContext('2d');
                this.barChart = new Chart(ctx, {
                    type: 'bar',
                    data: {
                        labels: labels,
                        datasets: [{
                            label: 'Compliance Index %',
                            data: data,
                            backgroundColor: '#0054A6',
                            hoverBackgroundColor: '#F47920',
                            borderRadius: 4
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: { display: false }
                        },
                        scales: {
                            y: {
                                min: 0,
                                max: 100,
                                ticks: { font: { size: 9, weight: 'bold' } }
                            },
                            x: {
                                ticks: { font: { size: 9, weight: 'bold' } }
                            }
                        }
                    }
                });
            }
        };

        document.addEventListener("DOMContentLoaded", function () {
            var chartData = <%= ChartDataJson %>;
            if (chartData && chartData.StatusData) {
                window.charts.createDoughnut("statusChart", chartData.StatusData, ["Compliant", "Warning", "Critical", "Expired"]);
            }
            if (chartData && chartData.DeptNames && chartData.DeptScores) {
                window.charts.createBar("deptChart", chartData.DeptScores, chartData.DeptNames);
            }
        });
    </script>
</asp:Content>
