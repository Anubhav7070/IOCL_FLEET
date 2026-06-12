<%@ Page Title="Employee Registry" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeFile="Users.aspx.vb" Inherits="UsersPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="space-y-6 page-enter font-sans max-w-7xl mx-auto">
        <!-- Header Block -->
        <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 rounded-xl bg-slate-900 border border-slate-800 text-white p-6 shadow-md shadow-slate-900/10">
            <div>
                <h2 class="text-xl font-bold tracking-wide uppercase">Refinery User Directory</h2>
                <p class="text-xs text-slate-400 mt-1">
                    Manage system operators, access key permissions, and department scopes.
                </p>
            </div>

            <asp:Button ID="btnNewEmp" runat="server" Text="Create Account" OnClick="btnNewEmp_Click" CssClass="flex items-center gap-1.5 rounded bg-blue-600 hover:bg-blue-700 px-4 py-2.5 text-xs font-bold text-white shadow transition-all duration-200 cursor-pointer" />
        </div>

        <!-- Filter Panel -->
        <div class="bg-white border border-slate-200 rounded-xl p-5 shadow-sm">
            <div class="flex flex-wrap items-end gap-4">
                <div class="flex flex-col gap-1.5">
                    <label class="text-[10px] font-bold text-slate-500 uppercase tracking-widest block">Search Operator</label>
                    <asp:TextBox ID="txtEmpSearch" runat="server" placeholder="Name or Employee Number" CssClass="w-64 rounded border border-slate-200 py-1.5 px-3 text-xs text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-blue-500 font-medium"></asp:TextBox>
                </div>
                <div class="flex items-center gap-2">
                    <asp:Button ID="btnFilter" runat="server" Text="Filter Directory" OnClick="btnFilter_Click" CssClass="rounded bg-blue-600 hover:bg-blue-700 px-4 py-1.5 text-xs font-bold text-white shadow cursor-pointer transition-all" />
                    <asp:Button ID="btnReset" runat="server" Text="Reset" OnClick="btnReset_Click" CssClass="rounded border border-slate-250 hover:bg-slate-50 px-4 py-1.5 text-xs font-bold text-slate-500 cursor-pointer transition-all" />
                </div>
            </div>
        </div>

        <!-- Users Repeater Table -->
        <asp:Repeater ID="rptUsers" runat="server">
            <HeaderTemplate>
                <div class="rounded-xl border border-slate-200 bg-white overflow-hidden shadow-sm">
                    <div class="overflow-x-auto">
                        <table class="w-full text-left text-xs border-collapse">
                            <thead>
                                <tr class="border-b border-slate-200 bg-slate-50 text-slate-500 font-bold uppercase tracking-wider">
                                    <th class="p-4">Employee / Username</th>
                                    <th class="p-4">Email</th>
                                    <th class="p-4">Access Role</th>
                                    <th class="p-4">Department Scope</th>
                                    <th class="p-4 text-right">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
            </HeaderTemplate>
            <ItemTemplate>
                <tr class="border-b border-slate-100 hover:bg-slate-50 transition-colors">
                    <td class="p-4">
                        <div class="font-bold text-slate-800"><%# Eval("EmployeeName") %></div>
                        <div class="text-[10px] text-slate-400 font-semibold font-mono mt-0.5">Emp No: <%# Eval("EmpNumber") %></div>
                    </td>
                    <td class="p-4 text-slate-600 font-medium font-mono"><%# Eval("EmailId") %></td>
                    <td class="p-4">
                        <span class="inline-flex items-center gap-1 text-[9px] font-extrabold uppercase tracking-wider text-blue-700 bg-blue-50 border border-blue-100 rounded px-2 py-0.5">
                            <svg class="h-3 w-3 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.952 11.952 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" /></svg>
                            <span><%# Eval("Role").ToString().Replace("_", " ") %></span>
                        </span>
                    </td>
                    <td class="p-4 text-slate-600 font-bold uppercase tracking-wide">
                        <%# If(String.IsNullOrEmpty(Eval("Department").ToString()), "GLOBAL (REFINERY-WIDE)", Eval("Department").ToString().ToUpper()) %>
                    </td>
                    <td class="p-4 text-right">
                        <div class="flex items-center justify-end gap-2">
                            <asp:LinkButton ID="lnkEdit" runat="server" CommandArgument='<%# Eval("EmployeeId") %>' OnClick="lnkEdit_Click" class="rounded border border-slate-200 hover:bg-slate-50 p-1.5 text-slate-500 hover:text-slate-700 focus:outline-none" title="Edit User">
                                <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" /></svg>
                            </asp:LinkButton>
                            <asp:LinkButton ID="lnkDelete" runat="server" CommandArgument='<%# Eval("EmployeeId") %>' OnClick="lnkDelete_Click" OnClientClick="return confirm('Are you sure you want to delete this user account?');" class="rounded border border-red-100 hover:bg-red-50 p-1.5 text-red-500 hover:text-red-700 focus:outline-none" title="Delete User">
                                <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-4v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                            </asp:LinkButton>
                        </div>
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

        <!-- No users template -->
        <asp:Panel ID="pnlNoUsers" runat="server" CssClass="rounded-xl border border-dashed border-slate-355 bg-white py-16 text-center text-slate-400" Visible="false">
            <svg class="h-12 w-12 mx-auto mb-3 opacity-30" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a3 3 0 11-6 0 3 3 0 016 0z" /></svg>
            <p class="text-sm font-bold text-slate-500">No users found.</p>
        </asp:Panel>

        <!-- Add/Edit Modal -->
        <asp:Panel ID="pnlEdit" runat="server" CssClass="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4" Visible="false">
            <div class="relative w-full max-w-md rounded-xl border border-slate-200 bg-white p-6 shadow-2xl">
                <h3 class="text-sm font-bold text-slate-800 uppercase tracking-wider border-b border-slate-100 pb-3">
                    <asp:Label ID="lblFormTitle" runat="server" Text="Register Employee"></asp:Label>
                </h3>
                
                <div class="mt-4 space-y-4">
                    <asp:HiddenField ID="hdnEmpId" runat="server" />

                    <div class="grid grid-cols-2 gap-4">
                        <div>
                            <label class="text-[10px] font-bold text-slate-500 uppercase tracking-widest block mb-1">Employee ID *</label>
                            <asp:TextBox ID="txtEmpNo" runat="server" placeholder="e.g. 10000001" MaxLength="8" CssClass="w-full rounded-md border border-slate-200 py-2 px-3 text-xs text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-blue-500 font-semibold"></asp:TextBox>
                        </div>
                        <div>
                            <label class="text-[10px] font-bold text-slate-500 uppercase tracking-widest block mb-1">Email Address *</label>
                            <asp:TextBox ID="txtEmpEmail" runat="server" placeholder="operator@iocl.co.in" CssClass="w-full rounded-md border border-slate-200 py-2 px-3 text-xs text-slate-850 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-blue-500 font-semibold"></asp:TextBox>
                        </div>
                    </div>

                    <div class="grid grid-cols-2 gap-4">
                        <div>
                            <label class="text-[10px] font-bold text-slate-500 uppercase tracking-widest block mb-1">Full Name *</label>
                            <asp:TextBox ID="txtEmpName" runat="server" placeholder="Employee Name" CssClass="w-full rounded-md border border-slate-200 py-2 px-3 text-xs text-slate-850 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-blue-500 font-semibold"></asp:TextBox>
                        </div>
                        <div>
                            <label class="text-[10px] font-bold text-slate-500 uppercase tracking-widest block mb-1">Designation *</label>
                            <asp:TextBox ID="txtEmpDesg" runat="server" placeholder="e.g. Inspector / Manager" CssClass="w-full rounded-md border border-slate-200 py-2 px-3 text-xs text-slate-850 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-blue-500 font-semibold"></asp:TextBox>
                        </div>
                    </div>

                    <div class="grid grid-cols-2 gap-4">
                        <div>
                            <label class="text-[10px] font-bold text-slate-500 uppercase tracking-widest block mb-1">Access Role *</label>
                            <asp:DropDownList ID="ddlEmpRole" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlEmpRole_SelectedIndexChanged" CssClass="w-full rounded-md border border-slate-200 bg-white py-2 px-3 text-xs text-slate-700 focus:outline-none focus:ring-1 focus:ring-blue-500 font-semibold">
                                <asp:ListItem Text="Department Admin" Value="DEPT_ADMIN"></asp:ListItem>
                                <asp:ListItem Text="Super Admin" Value="SuperAdmin"></asp:ListItem>
                                <asp:ListItem Text="Viewer" Value="VIEWER"></asp:ListItem>
                                <asp:ListItem Text="Gateman" Value="GATEMAN"></asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div>
                            <label class="text-[10px] font-bold text-slate-500 uppercase tracking-widest block mb-1">Department Scope *</label>
                            <asp:DropDownList ID="ddlEmpDept" runat="server" CssClass="w-full rounded-md border border-slate-200 bg-white py-2 px-3 text-xs text-slate-700 focus:outline-none focus:ring-1 focus:ring-blue-500 font-semibold">
                            </asp:DropDownList>
                            <asp:Label ID="lblDeptGlobal" runat="server" Text="GLOBAL (REFINERY-WIDE)" CssClass="w-full bg-slate-50 rounded-md border border-slate-200 py-2 px-3 text-[10px] text-slate-500 font-bold uppercase tracking-wider block" Visible="false"></asp:Label>
                        </div>
                    </div>

                    <div>
                        <label class="text-[10px] font-bold text-slate-500 uppercase tracking-widest block mb-1">
                            Access Key Password
                        </label>
                        <asp:TextBox ID="txtEmpPassword" runat="server" TextMode="Password" placeholder="Enter password (leave blank to keep current if editing)" CssClass="w-full rounded-md border border-slate-200 py-2 px-3 text-xs text-slate-800 focus:outline-none focus:ring-1 focus:ring-blue-500"></asp:TextBox>
                    </div>

                    <div class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100">
                        <asp:Button ID="btnCancel" runat="server" Text="Cancel" OnClick="btnCancel_Click" CssClass="rounded border border-slate-200 hover:bg-slate-50 px-4 py-2 text-xs font-bold text-slate-500 cursor-pointer transition-colors" />
                        <asp:Button ID="btnSave" runat="server" Text="Save Account" OnClick="btnSave_Click" CssClass="rounded bg-blue-600 hover:bg-blue-700 px-4 py-2 text-xs font-bold text-white shadow shadow-blue-600/10 cursor-pointer transition-colors" />
                    </div>
                </div>
            </div>
        </asp:Panel>
    </div>
</asp:Content>
