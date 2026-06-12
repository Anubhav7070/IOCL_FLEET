<%@ Page Title="Change Password" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeFile="ChangePassword.aspx.vb" Inherits="ChangePasswordPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="flex items-center justify-center h-full min-h-[70vh]">
        <div class="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-8 shadow-xl shadow-slate-200/50">
            
            <div class="text-center mb-6">
                <div class="inline-flex items-center justify-center w-12 h-12 rounded-full bg-orange-100 text-orange-600 mb-3">
                    <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                    </svg>
                </div>
                <h2 class="text-xl font-black text-slate-800 tracking-wide uppercase">Security Notice</h2>
                <p class="text-xs text-slate-500 mt-2 font-medium">You are using the default password. For security reasons, please set a new personal password before continuing.</p>
            </div>

            <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="mb-5 rounded-lg bg-red-50 p-3 border border-red-100 flex items-start gap-2">
                <svg class="h-4 w-4 text-red-500 mt-0.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <asp:Label ID="lblError" runat="server" CssClass="text-xs font-bold text-red-700"></asp:Label>
            </asp:Panel>

            <div class="space-y-5">
                <div>
                    <label class="block text-[10px] font-extrabold text-slate-500 uppercase tracking-widest mb-1.5">New Password</label>
                    <asp:TextBox ID="txtNewPassword" runat="server" TextMode="Password" placeholder="Enter a secure password" 
                        CssClass="w-full rounded-lg border border-slate-200 bg-slate-50 px-4 py-2.5 text-sm font-semibold text-slate-800 outline-none focus:border-blue-500 focus:bg-white transition-all"></asp:TextBox>
                </div>

                <div>
                    <label class="block text-[10px] font-extrabold text-slate-500 uppercase tracking-widest mb-1.5">Confirm Password</label>
                    <asp:TextBox ID="txtConfirmPassword" runat="server" TextMode="Password" placeholder="Type password again" 
                        CssClass="w-full rounded-lg border border-slate-200 bg-slate-50 px-4 py-2.5 text-sm font-semibold text-slate-800 outline-none focus:border-blue-500 focus:bg-white transition-all"></asp:TextBox>
                </div>

                <asp:Button ID="btnUpdate" runat="server" Text="Update & Continue" OnClick="btnUpdate_Click" 
                    CssClass="w-full rounded-lg bg-blue-600 hover:bg-blue-700 hover:scale-[1.02] active:scale-95 py-3 text-xs font-black uppercase tracking-wider text-white shadow-lg shadow-blue-600/30 transition-all duration-200 cursor-pointer focus:outline-none" />
            </div>
        </div>
    </div>
</asp:Content>
