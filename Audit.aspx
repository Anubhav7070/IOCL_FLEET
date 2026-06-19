<%@ Page Title="Audit Trails" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeFile="Audit.aspx.vb" Inherits="AuditPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="space-y-6 page-enter font-sans max-w-7xl mx-auto">
        <!-- Filters Section -->
        <div class="flex flex-col sm:flex-row gap-4 rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <div class="relative flex-1">
                <span class="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none text-slate-400">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                </span>
                <asp:TextBox ID="txtSearch" runat="server" placeholder="Search by action, operator name, or description..." CssClass="w-full rounded border border-slate-200 bg-slate-50 pl-9 pr-4 py-2 text-xs font-semibold text-slate-650 outline-none focus:border-blue-500 focus:bg-white focus:ring-1 focus:ring-blue-500 transition-all"></asp:TextBox>
            </div>

            <% If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then %>
                <asp:DropDownList ID="ddlDeptFilter" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlDeptFilter_SelectedIndexChanged" class="rounded border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-650 outline-none focus:border-blue-500 transition-all font-sans cursor-pointer">
                </asp:DropDownList>
            <% End If %>
            
            <div class="flex items-center gap-2">
                <asp:Button ID="btnFilter" runat="server" Text="Filter Audit" OnClick="btnFilter_Click" CssClass="rounded bg-blue-600 hover:bg-blue-700 px-4 py-2 text-xs font-bold text-white shadow cursor-pointer transition-all" />
                <asp:Button ID="btnReset" runat="server" Text="Reset" OnClick="btnReset_Click" CssClass="rounded border border-slate-250 hover:bg-slate-50 px-4 py-2 text-xs font-bold text-slate-500 cursor-pointer transition-all" />
                <asp:LinkButton ID="lnkRefresh" runat="server" OnClick="btnFilter_Click" class="flex items-center gap-1.5 rounded bg-slate-100 hover:bg-slate-200 border border-slate-250 px-4 py-2 text-xs font-bold text-slate-600 shadow-sm transition-all duration-200 focus:outline-none shrink-0 justify-center">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 1121.21 8.89M9 11l3 3m0 0l3-3m-3 3V2" /></svg>
                    <span>Refresh</span>
                </asp:LinkButton>
            </div>
        </div>

        <!-- Audit Log Repeater Table -->
        <asp:Repeater ID="rptAudit" runat="server">
            <HeaderTemplate>
                <div class="rounded-xl border border-slate-200 bg-white overflow-hidden shadow-sm">
                    <div class="overflow-x-auto">
                        <table class="w-full text-left text-xs border-collapse">
                            <thead>
                                <tr class="border-b border-slate-200 bg-slate-50 text-slate-505 text-slate-500 font-bold uppercase tracking-wider">
                                    <th class="p-4">Action</th>
                                    <th class="p-4">Description</th>
                                    <th class="p-4">Authorized Operator</th>
                                    <th class="p-4">Network Context</th>
                                    <th class="p-4">Data Payload</th>
                                    <th class="p-4">Timestamp</th>
                                </tr>
                            </thead>
                            <tbody>
            </HeaderTemplate>
            <ItemTemplate>
                <tr class="border-b border-slate-100 hover:bg-slate-50 transition-colors">
                    <td class="p-4">
                        <span class="inline-flex rounded bg-slate-900 border border-slate-800 text-white font-mono font-bold text-[8.5px] px-2 py-0.5 uppercase tracking-wider">
                            <%# Eval("Action").ToString().Replace("_", " ") %>
                        </span>
                    </td>
                    <td class="p-4 font-semibold text-slate-700 leading-relaxed max-w-sm">
                        <%# Eval("Description") %>
                    </td>
                    <td class="p-4">
                        <div class="flex items-center gap-1.5 font-bold text-slate-700">
                            <svg class="h-3.5 w-3.5 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></svg>
                            <span><%# Eval("Username") %></span>
                        </div>
                    </td>
                    <td class="p-4 font-mono font-bold text-slate-500">
                        <div class="flex items-center gap-1.5">
                            <svg class="h-3.5 w-3.5 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M21 12a9 9 0 01-9 9m9-9a9 9 0 00-9-9m9 9H3m9 9a9 9 0 01-9-9m9 9c1.657 0 3-4.03 3-9s-1.343-9-3-9m0 18c-1.657 0-3-4.03-3-9s1.343-9 3-9m-9 9a9 9 0 019-9" /></svg>
                            <span><%# If(String.IsNullOrEmpty(Eval("IpAddress").ToString()), "127.0.0.1", Eval("IpAddress")) %></span>
                        </div>
                    </td>
                    <td class="p-4">
                        <%# If(HasPayload(Eval("OldValue"), Eval("NewValue")), _
                            "<button type='button' onclick='inspectPayload(""" & Eval("Action").ToString() & """, """ & Server.HtmlEncode(If(Convert.IsDBNull(Eval("OldValue")), "", Eval("OldValue").ToString()).Replace("""", "\""")) & """, """ & Server.HtmlEncode(If(Convert.IsDBNull(Eval("NewValue")), "", Eval("NewValue").ToString()).Replace("""", "\""")) & """)' class='flex items-center gap-1 text-[9px] font-bold text-blue-600 hover:text-blue-700 uppercase tracking-widest transition-all focus:outline-none'><svg class='h-3.5 w-3.5' fill='none' viewBox='0 0 24 24' stroke='currentColor' stroke-width='2'><path stroke-linecap='round' stroke-linejoin='round' d='M15 12a3 3 0 11-6 0 3 3 0 016 0z' /><path stroke-linecap='round' stroke-linejoin='round' d='M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z' /></svg><span>Inspect</span></button>", _
                            "<span class='text-[9px] font-bold text-slate-400 uppercase tracking-widest'>No Payload</span>") %>
                    </td>
                    <td class="p-4 font-mono text-slate-500 font-semibold whitespace-nowrap">
                        <%# FmtDateTime(Eval("Timestamp")) %>
                    </td>
                </tr>
            </ItemTemplate>
            <FooterTemplate>
                            </tbody>
                        </table>
                    </div>
                </div>
            </FooterTemplate>
        </asp:Repeater>

        <!-- Empty trail template -->
        <asp:Panel ID="pnlNoLogs" runat="server" CssClass="rounded-xl border border-dashed border-slate-300 bg-white py-16 text-center text-slate-400" Visible="false">
            <svg class="h-12 w-12 mx-auto mb-3 opacity-30" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.952 11.952 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" /></svg>
            <p class="text-sm font-bold text-slate-500">No audit logs matched search criteria.</p>
        </asp:Panel>
    </div>

    <!-- Payload Inspection Modal (Pure Client-side modal) -->
    <div id="inspectModal" class="hidden fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
        <div class="relative w-full max-w-2xl rounded-xl border border-slate-200 bg-white p-6 shadow-2xl flex flex-col max-h-[85vh]">
            <h3 class="text-sm font-bold text-slate-800 uppercase tracking-wider border-b border-slate-100 pb-3">
                Inspect Data Payload: <span id="inspectAction"></span>
            </h3>
            
            <div class="flex-1 overflow-y-auto space-y-4 my-4 pr-1">
                <div id="inspectOldSection" class="hidden">
                    <h4 class="text-[10px] font-bold text-red-500 uppercase tracking-widest mb-1.5">Previous State Data</h4>
                    <pre id="inspectOldPre" class="rounded bg-red-50/50 border border-red-100 p-4 text-[10px] font-mono text-red-800 overflow-x-auto"></pre>
                </div>
                <div id="inspectNewSection" class="hidden">
                    <h4 class="text-[10px] font-bold text-emerald-500 uppercase tracking-widest mb-1.5">Mutated State Data</h4>
                    <pre id="inspectNewPre" class="rounded bg-emerald-50/50 border border-emerald-100 p-4 text-[10px] font-mono text-emerald-800 overflow-x-auto font-semibold"></pre>
                </div>
            </div>

            <div class="flex items-center justify-end pt-3 border-t border-slate-100">
                <button type="button" onclick="closeInspector()" class="rounded border border-slate-200 bg-slate-50 hover:bg-slate-100 px-5 py-2 text-xs font-bold text-slate-500 transition-all cursor-pointer focus:outline-none">
                    Close Inspector
                </button>
            </div>
        </div>
    </div>

    <!-- Script to handle inspect modal toggling -->
    <script>
        function inspectPayload(action, oldVal, newVal) {
            document.getElementById('inspectAction').innerText = action;
            
            const oldPre = document.getElementById('inspectOldPre');
            const newPre = document.getElementById('inspectNewPre');
            const oldSection = document.getElementById('inspectOldSection');
            const newSection = document.getElementById('inspectNewSection');
            
            // Decode HTML-encoded values if needed
            const oldStr = htmlDecode(oldVal);
            const newStr = htmlDecode(newVal);

            if (oldStr && oldStr !== '-' && oldStr !== 'None' && oldStr.trim().length > 0) {
                try {
                    oldPre.innerText = JSON.stringify(JSON.parse(oldStr), null, 2);
                } catch(e) {
                    oldPre.innerText = oldStr;
                }
                oldSection.classList.remove('hidden');
            } else {
                oldSection.classList.add('hidden');
            }

            if (newStr && newStr !== '-' && newStr !== 'None' && newStr.trim().length > 0) {
                try {
                    newPre.innerText = JSON.stringify(JSON.parse(newStr), null, 2);
                } catch(e) {
                    newPre.innerText = newStr;
                }
                newSection.classList.remove('hidden');
            } else {
                newSection.classList.add('hidden');
            }
            
            document.getElementById('inspectModal').classList.remove('hidden');
        }

        function closeInspector() {
            document.getElementById('inspectModal').classList.add('hidden');
        }

        function htmlDecode(input) {
            var doc = new DOMParser().parseFromString(input, "text/html");
            return doc.documentElement.textContent;
        }
    </script>
</asp:Content>
