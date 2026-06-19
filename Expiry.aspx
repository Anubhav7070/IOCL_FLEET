<%@ Page Title="Expiry Management" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeFile="Expiry.aspx.vb" Inherits="ExpiryPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>


<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" runat="server">
    <div class="space-y-6">
        <!-- Filters Control Panel -->
        <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm flex flex-col md:flex-row md:items-center gap-4">
            <div class="relative flex-1">
                <span class="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none text-slate-400">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                </span>
                <asp:TextBox ID="txtVehSearch" runat="server" OnTextChanged="FilterAlerts" AutoPostBack="true" placeholder="Search vehicle number, plate ID..." CssClass="w-full rounded border border-slate-200 bg-slate-50 pl-9 pr-4 py-2 text-xs font-semibold text-slate-600 placeholder-slate-400 outline-none focus:border-blue-500 focus:bg-white transition-all"></asp:TextBox>
            </div>

            <div class="flex items-center gap-3">
                <asp:DropDownList ID="ddlDeptFilter" runat="server" AutoPostBack="true" OnSelectedIndexChanged="FilterAlerts" CssClass="rounded border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-600 outline-none focus:border-blue-500 transition-all">
                </asp:DropDownList>

                <asp:DropDownList ID="ddlSeverityFilter" runat="server" AutoPostBack="true" OnSelectedIndexChanged="FilterAlerts" CssClass="rounded border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-600 outline-none focus:border-blue-500 transition-all">
                    <asp:ListItem Text="All Flagged Alerts" Value=""></asp:ListItem>
                    <asp:ListItem Text="Expired Only" Value="Expired"></asp:ListItem>
                    <asp:ListItem Text="Non-Compliant Only" Value="Non-Compliant"></asp:ListItem>
                </asp:DropDownList>
                
                <asp:LinkButton ID="btnClearFilters" runat="server" OnClick="btnResetFilter_Click" CssClass="text-xs font-semibold text-slate-500 hover:text-slate-800 px-2">Reset</asp:LinkButton>
            </div>
        </div>

        <!-- Split Grid & Process Form Layout -->
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6 items-start">
            <!-- Left Alerts Table (2/3 width) -->
            <div class="lg:col-span-2 rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4">
                <div class="border-b border-slate-100 pb-3">
                    <span class="text-xs font-extrabold text-slate-400 uppercase tracking-widest">Active Expiry Alerts</span>
                </div>

                <div class="overflow-x-auto">
                    <asp:Repeater ID="rptAlerts" runat="server" OnItemCommand="rptAlerts_ItemCommand">
                        <HeaderTemplate>
                            <table class="w-full text-left border-collapse text-xs">
                                <thead>
                                    <tr class="border-b border-slate-100 text-slate-400 uppercase font-bold tracking-wider">
                                         <% If Session("Role") IsNot Nothing Then %>
                                         <th class="py-3 px-2 w-8"><input type="checkbox" id="chkSelectAll" onclick="toggleAllAlerts(this)" class="rounded border-slate-300" title="Select All" /></th>
                                         <% End If %>
                                        <th class="py-3 px-2">Vehicle No</th>
                                        <th class="py-3 px-2">Division</th>
                                        <th class="py-3 px-2">Document Type</th>
                                        <th class="py-3 px-2">Expiry Date</th>
                                        <th class="py-3 px-2">Alert Level</th>
                                        <th class="py-3 px-2">Validity</th>
                                        <th class="py-3 px-2 text-right">Action</th>
                                    </tr>
                                </thead>
                                <tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr class="border-b border-slate-50 hover:bg-slate-50 transition-colors">
                                 <% If Session("Role") IsNot Nothing Then %>
                                 <td class="py-3 px-2">
                                     <input type="checkbox" class="alertCheckbox rounded border-slate-300"
                                            value='<%# Eval("Id") %>'
                                            data-vehicle='<%# Eval("VehicleNumber") %>'
                                            data-type='<%# Eval("LicenseType").ToString().Replace("_", " ") %>'
                                            onclick="updateBulkBar()" />
                                 </td>
                                 <% End If %>
                                <td class="py-3 px-2 font-bold font-mono text-slate-800"><%# Eval("VehicleNumber") %></td>
                                <td class="py-3 px-2 font-semibold text-slate-500"><%# Eval("DeptName") %></td>
                                <td class="py-3 px-2 font-semibold text-slate-600"><%# Eval("LicenseType").ToString().Replace("_", " ") %></td>
                                <td class="py-3 px-2 font-semibold text-slate-600 font-mono"><%# FmtDate(Eval("ExpiryDate")) %></td>
                                <td class="py-3 px-2">
                                    <span class="rounded-full px-2 py-0.5 text-[9px] font-bold uppercase <%# GetAlertBadgeClass(Eval("Status")) %>">
                                        <%# Eval("Status").ToString().Replace("_", " ") %>
                                    </span>
                                </td>
                                <td class="py-3 px-2 font-medium text-slate-600"><%# GetDaysRemainingText(Eval("ExpiryDate")) %></td>
                                <td class="py-3 px-2 text-right">
                                     <% If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then %>
                                         <div class="flex gap-2 justify-end">
                                             <asp:LinkButton ID="btnProcessSuper" runat="server" CommandName="SelectAlert" CommandArgument='<%# Eval("Id") %>' CssClass="rounded bg-blue-50 hover:bg-blue-100 text-[#0054A6] px-2.5 py-1.5 font-bold transition-all focus:outline-none">
                                                 Renew
                                             </asp:LinkButton>
                                             <asp:LinkButton ID="btnNotify" runat="server" CommandName="SendNotification" CommandArgument='<%# Eval("Id") %>' CssClass="rounded bg-orange-50 hover:bg-orange-100 text-orange-600 px-2.5 py-1.5 font-bold transition-all focus:outline-none border border-orange-200">
                                                 Notify
                                             </asp:LinkButton>
                                         </div>
                                     <% Else %>
                                         <asp:LinkButton ID="btnProcess2" runat="server" CommandName="SelectAlert" CommandArgument='<%# Eval("Id") %>' CssClass="rounded bg-blue-50 hover:bg-blue-100 text-[#0054A6] px-2.5 py-1.5 font-bold transition-all focus:outline-none">
                                             Renew
                                         </asp:LinkButton>
                                     <% End If %>
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

                <%-- Bulk Action Bar (shown when checkboxes are selected) --%>
                 <% If Session("Role") IsNot Nothing Then %>
                 <div id="bulkActionBar" class="hidden mt-4 flex items-center justify-between rounded-lg bg-blue-50 border border-blue-200 px-4 py-3">
                     <span id="bulkCountLabel" class="text-xs font-bold text-[#0054A6]">0 documents selected</span>
                     <button type="button" onclick="openBulkRenewPanel()" class="rounded bg-[#0054A6] hover:bg-blue-700 text-white px-4 py-2 text-xs font-bold transition-all focus:outline-none">
                         Bulk Renew Selected
                     </button>
                 </div>
                 <% End If %>
            </div>

            <!-- Right Renewal Process Form (1/3 width) -->
            <div class="lg:col-span-1">
                <asp:Panel ID="pnlRenewForm" runat="server" CssClass="rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4" Visible="false">
                    <div class="border-b border-slate-100 pb-3 flex justify-between items-center">
                        <span class="text-xs font-extrabold text-slate-400 uppercase tracking-widest">Execute Certificate Renewal</span>
                        <asp:LinkButton ID="btnCloseForm" runat="server" OnClick="btnCancelRenew_Click" CssClass="text-slate-400 hover:text-slate-600 focus:outline-none">
                            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                        </asp:LinkButton>
                    </div>

                    <asp:HiddenField ID="hdnRecordId" runat="server" />
                    <asp:HiddenField ID="hdnVehicleId" runat="server" />

                    <div class="space-y-4 text-xs">
                        <div>
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Target Vehicle</label>
                            <asp:TextBox ID="txtVehPlate" runat="server" ReadOnly="true" Enabled="false" CssClass="w-full rounded border border-slate-150 bg-slate-50 px-3 py-2 text-xs font-bold text-slate-500 font-mono uppercase cursor-not-allowed"></asp:TextBox>
                        </div>

                        <div>
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Document Class / Type</label>
                            <asp:TextBox ID="txtDocType" runat="server" ReadOnly="true" Enabled="false" CssClass="w-full rounded border border-slate-150 bg-slate-50 px-3 py-2 text-xs font-bold text-slate-500 uppercase cursor-not-allowed"></asp:TextBox>
                        </div>

                        <div>
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">New License / Document Number</label>
                            <asp:TextBox ID="txtDocNumber" runat="server" placeholder="Enter Registration/License No" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all font-mono uppercase"></asp:TextBox>
                        </div>

                        <div>
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Issuing Authority</label>
                            <asp:TextBox ID="txtAuthority" runat="server" placeholder="e.g. Regional Transport Office (RTO)" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all"></asp:TextBox>
                        </div>

                        <div class="grid grid-cols-2 gap-4">
                            <div>
                                <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Issue Date</label>
                                <asp:TextBox ID="txtIssueDate" runat="server" TextMode="Date" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-600 outline-none focus:border-blue-500 transition-all"></asp:TextBox>
                            </div>
                            <div>
                                <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Expiry Date</label>
                                <asp:TextBox ID="txtExpiryDate" runat="server" TextMode="Date" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-600 outline-none focus:border-blue-500 transition-all"></asp:TextBox>
                            </div>
                        </div>

                        <div>
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Upload Scanned Copy (PDF/Image)</label>
                            <asp:FileUpload ID="fileScan" runat="server" CssClass="w-full rounded border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-600 outline-none focus:border-blue-500 cursor-pointer" />
                            <span class="text-[9px] text-slate-400 mt-1 block">File size limit: 10MB. Formats: .pdf, .jpg, .png</span>
                        </div>

                        <div>
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Audit Remarks / Notes</label>
                            <asp:TextBox ID="txtRemarks" runat="server" TextMode="MultiLine" Rows="2" placeholder="Describe the renewal updates..." CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all"></asp:TextBox>
                        </div>
                    </div>

                    <div class="flex gap-3 pt-3 border-t border-slate-100">
                        <asp:Button ID="btnCancelRenew" runat="server" CssClass="flex-1 rounded border border-slate-200 bg-white hover:bg-slate-50 text-slate-600 py-2.5 text-xs font-bold cursor-pointer focus:outline-none" Text="Cancel" OnClick="btnCancelRenew_Click" />
                        <asp:Button ID="btnSubmitRenew" runat="server" CssClass="flex-1 rounded bg-[#0054A6] hover:bg-blue-700 text-white py-2.5 text-xs font-bold cursor-pointer focus:outline-none" Text="Submit Renewal" OnClick="btnSubmitRenew_Click" />
                        <asp:Button ID="btnSendNotify" runat="server" Visible="false" CssClass="flex-1 rounded bg-orange-500 hover:bg-orange-600 text-white py-2.5 text-xs font-bold cursor-pointer focus:outline-none" Text="Send Notification" OnClick="btnSendNotify_Click" />
                    </div>
                </asp:Panel>

                <%-- Bulk Renewal Panel (shown by JS with per-document upload cards) --%>
                <% If Session("Role") IsNot Nothing Then %>
                <div id="pnlBulkRenew" class="hidden">
                    <div class="rounded-xl border border-blue-200 bg-white p-5 shadow-sm space-y-4">
                        <div class="border-b border-blue-100 pb-3 flex justify-between items-center">
                            <span class="text-xs font-extrabold text-[#0054A6] uppercase tracking-widest">Bulk Certificate Renewal</span>
                            <button type="button" onclick="closeBulkRenewPanel()" class="text-slate-400 hover:text-slate-600 focus:outline-none">
                                <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                            </button>
                        </div>
                        <p class="text-[10px] text-blue-600 font-semibold">Fill in renewal details for each selected document below.</p>

                        <%-- Hidden field carries comma-separated record IDs to the server --%>
                        <input type="hidden" name="hdnBulkSelectedIds" id="hdnBulkSelectedIds" />

                        <%-- Per-document cards injected here by JS --%>
                        <div id="bulkDocCards" class="space-y-4"></div>

                        <%-- Shared remarks --%>
                        <div class="text-xs">
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Batch Remarks (applies to all)</label>
                            <textarea name="txtBulkRemarks" id="txtBulkRemarks" rows="2"
                                      placeholder="e.g. Annual renewal batch"
                                      class="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all"></textarea>
                        </div>

                        <div class="flex gap-3 pt-2 border-t border-blue-100">
                            <button type="button" onclick="closeBulkRenewPanel()" class="flex-1 rounded border border-slate-200 bg-white hover:bg-slate-50 text-slate-600 py-2.5 text-xs font-bold cursor-pointer focus:outline-none">Cancel</button>
                            <asp:Button ID="btnBulkRenew" runat="server" CssClass="flex-1 rounded bg-[#0054A6] hover:bg-blue-700 text-white py-2.5 text-xs font-bold cursor-pointer focus:outline-none" Text="Submit Bulk Renewal" OnClick="btnBulkRenew_Click" OnClientClick="return validateBulkRenewForm();" />
                        </div>
                    </div>
                </div>
                <% End If %>

                <!-- If nothing selected placeholder -->
                <asp:Panel ID="pnlNoForm" runat="server" CssClass="rounded-xl border-2 border-dashed border-slate-200 bg-white p-12 text-center text-slate-400">
                    <svg class="h-10 w-10 mx-auto mb-2 opacity-30" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
                    <p class="text-xs font-bold text-slate-500">Select a flagged certificate item from the list to process safety logs and file uploads.</p>
                </asp:Panel>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        function toggleAllAlerts(chk) {
            document.querySelectorAll('.alertCheckbox').forEach(function(cb) { cb.checked = chk.checked; });
            updateBulkBar();
        }

        function updateBulkBar() {
            var checked = document.querySelectorAll('.alertCheckbox:checked');
            var bar = document.getElementById('bulkActionBar');
            var label = document.getElementById('bulkCountLabel');
            if (!bar) return;
            if (checked.length > 0) {
                bar.classList.remove('hidden');
                label.textContent = checked.length + ' document(s) selected';
            } else {
                bar.classList.add('hidden');
                closeBulkRenewPanel();
            }
        }

        function openBulkRenewPanel() {
            var checked = document.querySelectorAll('.alertCheckbox:checked');
            if (checked.length === 0) return;

            // Collect IDs and labels
            var ids = [];
            checked.forEach(function(cb) { ids.push(cb.value); });
            document.getElementById('hdnBulkSelectedIds').value = ids.join(',');

            // Build per-document upload cards
            var container = document.getElementById('bulkDocCards');
            container.innerHTML = '';
            checked.forEach(function(cb, idx) {
                var recId   = cb.value;
                var vehicle = cb.getAttribute('data-vehicle') || '';
                var docType = cb.getAttribute('data-type') || 'Document';
                var num = idx + 1;

                container.innerHTML += [
                    '<div class="rounded-lg border border-slate-200 bg-slate-50 p-4 space-y-3">',
                        '<div class="flex items-center gap-2">',
                            '<span class="flex-none w-6 h-6 rounded-full bg-[#0054A6] text-white text-[10px] font-bold flex items-center justify-center">' + num + '</span>',
                            '<div>',
                                '<p class="text-[11px] font-bold text-slate-700">' + docType + '</p>',
                                '<p class="text-[9px] text-slate-400 font-mono">' + vehicle + '</p>',
                            '</div>',
                        '</div>',

                        '<div>',
                            '<label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">New Document/License Number <span class="text-red-500">*</span></label>',
                            '<input type="text" name="docNumber_' + recId + '" placeholder="Enter new number" class="w-full rounded border border-slate-200 px-3 py-1.5 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all font-mono uppercase" />',
                        '</div>',

                        '<div>',
                            '<label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Issuing Authority <span class="text-red-500">*</span></label>',
                            '<input type="text" name="authority_' + recId + '" placeholder="e.g. RTO" class="w-full rounded border border-slate-200 px-3 py-1.5 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 transition-all" />',
                        '</div>',

                        '<div class="grid grid-cols-2 gap-3">',
                            '<div>',
                                '<label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Issue Date <span class="text-red-500">*</span></label>',
                                '<input type="date" name="issueDate_' + recId + '" class="w-full rounded border border-slate-200 px-3 py-1.5 text-xs outline-none focus:border-blue-500 transition-all" />',
                            '</div>',
                            '<div>',
                                '<label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">New Expiry Date <span class="text-red-500">*</span></label>',
                                '<input type="date" name="expiryDate_' + recId + '" class="w-full rounded border border-slate-200 px-3 py-1.5 text-xs outline-none focus:border-blue-500 transition-all" />',
                            '</div>',
                        '</div>',

                        '<div>',
                            '<label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Upload PDF <span class="text-red-500">*</span></label>',
                            '<input type="file" name="docFile_' + recId + '" accept=".pdf" class="w-full text-[10px] text-slate-600 file:mr-2 file:rounded file:border-0 file:bg-blue-50 file:px-2 file:py-1 file:text-[10px] file:font-bold file:text-[#0054A6] hover:file:bg-blue-100" />',
                        '</div>',
                    '</div>'
                ].join('');
            });

            document.getElementById('pnlBulkRenew').classList.remove('hidden');
        }

        function validateBulkRenewForm() {
            var checked = document.querySelectorAll('.alertCheckbox:checked');
            if (checked.length === 0) {
                alert('No documents selected.');
                return false;
            }

            var isValid = true;
            checked.forEach(function(cb) {
                if (!isValid) return;
                var recId = cb.value;
                var docType = cb.getAttribute('data-type') || 'Document';
                var veh = cb.getAttribute('data-vehicle') || '';

                var docNumber = document.getElementsByName('docNumber_' + recId)[0];
                var authority = document.getElementsByName('authority_' + recId)[0];
                var issueDate = document.getElementsByName('issueDate_' + recId)[0];
                var expiryDate = document.getElementsByName('expiryDate_' + recId)[0];
                var docFile = document.getElementsByName('docFile_' + recId)[0];

                if (!docNumber || !docNumber.value.trim()) {
                    alert('Please enter Document Number for ' + docType + ' of vehicle ' + veh);
                    if (docNumber) docNumber.focus();
                    isValid = false;
                    return;
                }

                if (!authority || !authority.value.trim()) {
                    alert('Please enter Issuing Authority for ' + docType + ' of vehicle ' + veh);
                    if (authority) authority.focus();
                    isValid = false;
                    return;
                }

                if (!issueDate || !issueDate.value) {
                    alert('Please select Issue Date for ' + docType + ' of vehicle ' + veh);
                    if (issueDate) issueDate.focus();
                    isValid = false;
                    return;
                }

                if (!expiryDate || !expiryDate.value) {
                    alert('Please select New Expiry Date for ' + docType + ' of vehicle ' + veh);
                    if (expiryDate) expiryDate.focus();
                    isValid = false;
                    return;
                }

                var iss = new Date(issueDate.value);
                var exp = new Date(expiryDate.value);
                if (exp <= iss) {
                    alert('Expiry date must be after issue date for ' + docType + ' of vehicle ' + veh);
                    if (expiryDate) expiryDate.focus();
                    isValid = false;
                    return;
                }

                if (!docFile || docFile.files.length === 0) {
                    alert('Please upload PDF document copy for ' + docType + ' of vehicle ' + veh);
                    if (docFile) docFile.focus();
                    isValid = false;
                    return;
                }

                var fileName = docFile.value;
                var ext = fileName.substring(fileName.lastIndexOf('.') + 1).toLowerCase();
                if (ext !== 'pdf') {
                    alert('Only PDF files are accepted. Please check the file for ' + docType + ' of vehicle ' + veh);
                    if (docFile) docFile.focus();
                    isValid = false;
                    return;
                }
            });

            return isValid;
        }

        function closeBulkRenewPanel() {
            var panel = document.getElementById('pnlBulkRenew');
            if (panel) panel.classList.add('hidden');
        }
    </script>
</asp:Content>
