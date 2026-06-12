<%@ Page Language="VB" AutoEventWireup="false" CodeFile="Verify.aspx.vb" Inherits="VerifyPage" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>IOCL Refinery - Gate Entry Verification Card</title>
    <!-- Google Fonts: Inter/Outfit -->
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;500;600;700;800&display=swap" rel="stylesheet">
    
    <!-- Tailwind CSS CDN -->
    <script src="https://cdn.tailwindcss.com"></script>
    <script>
        tailwind.config = {
            theme: {
                extend: {
                    colors: {
                        iocl: {
                            blue: '#001F5B',
                            orange: '#F47920',
                        }
                    }
                }
            }
        }
    </script>
    <style>
        body {
            font-family: 'Outfit', sans-serif;
        }
    </style>
</head>
<body class="bg-slate-950 min-h-screen flex flex-col items-center justify-center p-4 text-slate-200">
    <form id="form1" runat="server" class="w-full max-w-md flex flex-col">
        <!-- Error Panel -->
        <asp:Panel ID="pnlError" runat="server" CssClass="flex flex-col items-center justify-center p-6 text-center" Visible="false">
            <svg class="h-14 w-14 text-red-500 mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <h3 class="text-lg font-bold uppercase tracking-wider text-red-500">Security Clearance Error</h3>
            <p class="mt-2 text-xs text-slate-400 max-w-sm"><asp:Label ID="lblErrorMsg" runat="server"></asp:Label></p>
            <span class="mt-8 text-[9px] text-slate-650 font-bold uppercase tracking-widest">IOCL Panipat Gate Safety Operations</span>
        </asp:Panel>

        <!-- Verification Panel -->
        <asp:Panel ID="pnlVerify" runat="server" CssClass="w-full">
            <!-- Saffron/Blue Header -->
            <div class="w-full flex items-center justify-between border-b border-slate-800 bg-slate-900 p-4 rounded-t-xl">
                <div class="flex items-center gap-3">
                    <div class="bg-white rounded-lg p-1">
                        <img src="/iocl-logo.gif" alt="IOCL Logo" class="h-10 w-auto" style="object-fit: contain; mix-blend-mode: multiply;" />
                    </div>
                    <div>
                        <h1 class="text-xs font-bold text-white uppercase tracking-wider">IndianOil Corporation</h1>
                        <p class="text-[9px] text-orange-500 font-bold uppercase tracking-widest">Panipat Refinery Gate-3</p>
                    </div>
                </div>
                <span class="flex items-center gap-1 text-[8.5px] font-bold text-emerald-500 bg-emerald-950/20 px-2.5 py-0.5 rounded border border-emerald-900/30 uppercase tracking-widest">
                    <svg class="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.952 11.952 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" /></svg>
                    <span>Verified ID</span>
                </span>
            </div>

            <!-- Main Verification Body -->
            <div class="w-full bg-slate-900 border-x border-b border-slate-800 p-5 rounded-b-xl space-y-6">
                <!-- Clearance Badges -->
                <asp:Panel ID="pnlClearedBadge" runat="server" CssClass="rounded-lg border border-emerald-800/40 bg-emerald-950/20 p-5 text-center shadow-lg shadow-emerald-900/5">
                    <svg class="h-12 w-12 text-emerald-500 mx-auto mb-2" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.952 11.952 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" /></svg>
                    <h2 class="text-sm font-extrabold text-emerald-400 tracking-widest uppercase">CLEARANCE GRANTED</h2>
                    <p class="text-[10px] text-emerald-600 font-semibold mt-1">VEHICLE OK TO ENTER REFINERY TERMINAL</p>
                </asp:Panel>

                <asp:Panel ID="pnlDeniedBadge" runat="server" CssClass="rounded-lg border border-red-800/40 bg-red-950/20 p-5 text-center shadow-lg shadow-red-900/5">
                    <svg class="h-12 w-12 text-red-500 mx-auto mb-2" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
                    <h2 class="text-sm font-extrabold text-red-400 tracking-widest uppercase">ENTRY DENIED</h2>
                    <p class="text-[10px] text-red-500 font-semibold mt-1">EXPIRED OR MISSING COMPLIANCE CERTIFICATES</p>
                </asp:Panel>

                <!-- Vehicle metadata -->
                <div class="rounded-lg bg-slate-950/50 border border-slate-800/60 p-4 space-y-2 text-xs">
                    <div class="flex justify-between border-b border-slate-800 pb-1.5">
                        <span class="text-slate-500 font-bold uppercase tracking-wider text-[9px]">Registration No</span>
                        <span class="font-bold text-white font-mono text-sm"><asp:Label ID="lblPlate" runat="server"></asp:Label></span>
                    </div>
                    <div class="flex justify-between border-b border-slate-800 py-1.5">
                        <span class="text-slate-500 font-bold uppercase tracking-wider text-[9px]">Vehicle Category</span>
                        <span class="font-semibold text-slate-300"><asp:Label ID="lblCategory" runat="server"></asp:Label></span>
                    </div>
                    <div class="flex justify-between border-b border-slate-800 py-1.5">
                        <span class="text-slate-500 font-bold uppercase tracking-wider text-[9px]">Department</span>
                        <span class="font-semibold text-slate-300"><asp:Label ID="lblDept" runat="server"></asp:Label></span>
                    </div>
                    <div class="flex justify-between border-b border-slate-800 py-1.5">
                        <span class="text-slate-500 font-bold uppercase tracking-wider text-[9px]">Driver Name</span>
                        <span class="font-semibold text-slate-300"><asp:Label ID="lblDriver" runat="server"></asp:Label></span>
                    </div>
                    <div class="flex justify-between pt-1.5">
                        <span class="text-slate-500 font-bold uppercase tracking-wider text-[9px]">Vendor / Contractor</span>
                        <span class="font-semibold text-slate-300"><asp:Label ID="lblVendor" runat="server"></asp:Label></span>
                    </div>
                </div>

                <!-- Compliance Checklist -->
                <div class="space-y-3">
                    <h3 class="text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-2">Refinery Safety Checklist</h3>
                    
                    <asp:Repeater ID="rptCompliance" runat="server">
                        <HeaderTemplate>
                            <div class="space-y-2">
                        </HeaderTemplate>
                        <ItemTemplate>
                            <div class="flex items-center justify-between rounded bg-slate-950/30 border border-slate-800/50 p-3">
                                <div class="flex items-center gap-2.5">
                                    <span class='h-2.5 w-2.5 rounded-full shrink-0 <%# GetDotColor(Eval("Status").ToString()) %>'></span>
                                    <div>
                                        <h4 class="text-[10px] font-bold text-slate-300 uppercase tracking-wide leading-none">
                                            <%# Eval("LicenseType").ToString().Replace("_", " ") %>
                                        </h4>
                                        <p class="text-[8.5px] text-slate-500 font-mono mt-1">NO: <%# If(String.IsNullOrEmpty(Eval("LicenseNumber").ToString()), "PENDING", Eval("LicenseNumber").ToString()) %></p>
                                    </div>
                                </div>
                                <div class="text-right text-[9px] font-semibold font-mono">
                                    <span class='<%# GetStatusTextColor(Eval("Status").ToString()) %>'><%# Eval("Status").ToString().Replace("_", " ") %></span>
                                    <p class="text-slate-500 text-[8px] mt-0.5">EXP: <%# FmtDate(Eval("ExpiryDate")) %></p>
                                </div>
                            </div>
                        </ItemTemplate>
                        <FooterTemplate>
                            </div>
                        </FooterTemplate>
                    </asp:Repeater>
                </div>

                <!-- Scan timestamp -->
                <div class="text-center text-[8px] text-slate-600 font-bold uppercase tracking-widest border-t border-slate-800/60 pt-4">
                    <span>Checked: <%= DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss") %></span><br />
                    <span class="mt-1 block">PANIPAT REFINERY COMPUTERIZED SECURITY GATEWAY</span>
                </div>
            </div>
        </asp:Panel>
    </form>
</body>
</html>
