<%@ Page Title="Vehicle Allocation" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeFile="Allocation.aspx.vb" Inherits="AllocationPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="space-y-6">
        <!-- Error/Success Alerts -->
        <asp:Panel ID="pnlAlert" runat="server" Visible="false" CssClass="rounded-lg p-4 text-xs font-semibold">
            <asp:Label ID="lblAlertMsg" runat="server"></asp:Label>
        </asp:Panel>

        <!-- Main Form & Content Layout -->
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6 items-start">
            <!-- Left Side: Allocation Form (1/3 width) -->
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4">
                <div class="border-b border-slate-100 pb-3">
                    <span class="text-xs font-extrabold text-slate-400 uppercase tracking-widest">New Allocation</span>
                </div>

                <div class="space-y-4 text-xs">
                    <div>
                        <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Select Vehicle <span class="text-red-500">*</span></label>
                        <asp:DropDownList ID="ddlVehicles" runat="server" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 bg-slate-50 outline-none focus:border-blue-500 transition-all font-mono">
                        </asp:DropDownList>
                    </div>

                    <div>
                        <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Target Department <span class="text-red-500">*</span></label>
                        <asp:DropDownList ID="ddlDepartments" runat="server" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs font-semibold text-slate-700 bg-slate-50 outline-none focus:border-blue-500 transition-all font-sans">
                            <asp:ListItem Text="PR - Fire & Safety" Value="PR - Fire & Safety"></asp:ListItem>
                            <asp:ListItem Text="PR - Refinery Operations" Value="PR - Refinery Operations"></asp:ListItem>
                            <asp:ListItem Text="PR - Chemical & Laboratory" Value="PR - Chemical & Laboratory"></asp:ListItem>
                            <asp:ListItem Text="PNC - Fire & Safety" Value="PNC - Fire & Safety"></asp:ListItem>
                            <asp:ListItem Text="PNC - Cracker Operations" Value="PNC - Cracker Operations"></asp:ListItem>
                            <asp:ListItem Text="PNC - Chemical & Testing" Value="PNC - Chemical & Testing"></asp:ListItem>
                            <asp:ListItem Text="PR - Human Resources" Value="PR - Human Resources"></asp:ListItem>
                        </asp:DropDownList>
                    </div>

                    <div class="grid grid-cols-2 gap-2">
                        <div>
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">Start Date <span class="text-red-500">*</span></label>
                            <asp:TextBox ID="txtStartDate" runat="server" TextMode="Date" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs text-slate-700 bg-slate-50 outline-none focus:border-blue-500 transition-all"></asp:TextBox>
                        </div>
                        <div>
                            <label class="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1">End Date <span class="text-red-500">*</span></label>
                            <asp:TextBox ID="txtEndDate" runat="server" TextMode="Date" CssClass="w-full rounded border border-slate-200 px-3 py-2 text-xs text-slate-700 bg-slate-50 outline-none focus:border-blue-500 transition-all"></asp:TextBox>
                        </div>
                    </div>

                    <div class="pt-2">
                        <asp:Button ID="btnSubmitAllocation" runat="server" Text="Create Allocation" OnClick="btnSubmitAllocation_Click" CssClass="w-full rounded bg-blue-600 hover:bg-blue-700 text-white font-bold py-2.5 shadow cursor-pointer hover:scale-[1.01] active:scale-95 transition-all focus:outline-none" />
                    </div>
                </div>
            </div>

            <!-- Right Side: Allocations Lists (2/3 width) -->
            <div class="lg:col-span-2 space-y-6">
                <!-- Active Allocations Table -->
                <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4">
                    <div class="border-b border-slate-100 pb-3">
                        <span class="text-xs font-extrabold text-slate-400 uppercase tracking-widest">Active Allocations</span>
                    </div>

                    <div class="overflow-x-auto">
                        <asp:GridView ID="gvActiveAllocations" runat="server" AutoGenerateColumns="False" OnRowCommand="gvActiveAllocations_RowCommand" CssClass="w-full text-left border-collapse text-xs" GridLines="None">
                            <Columns>
                                <asp:TemplateField HeaderText="Vehicle Plate">
                                    <ItemTemplate>
                                        <span class="font-bold font-mono text-slate-800 bg-slate-100 border border-slate-200 px-2 py-1 rounded"><%# Eval("VehicleNumber") %></span>
                                    </ItemTemplate>
                                    <HeaderStyle CssClass="pb-3 text-slate-400 font-bold uppercase tracking-wider text-[10px]" />
                                    <ItemStyle CssClass="py-3.5 border-b border-slate-100" />
                                </asp:TemplateField>
                                <asp:BoundField DataField="VehicleType" HeaderText="Type">
                                    <HeaderStyle CssClass="pb-3 text-slate-400 font-bold uppercase tracking-wider text-[10px]" />
                                    <ItemStyle CssClass="py-3.5 border-b border-slate-100 font-semibold text-slate-600" />
                                </asp:BoundField>
                                <asp:BoundField DataField="Department" HeaderText="Department">
                                    <HeaderStyle CssClass="pb-3 text-slate-400 font-bold uppercase tracking-wider text-[10px]" />
                                    <ItemStyle CssClass="py-3.5 border-b border-slate-100 font-bold text-[#0054A6]" />
                                </asp:BoundField>
                                <asp:TemplateField HeaderText="Duration">
                                    <ItemTemplate>
                                        <div class="text-[11px] font-semibold text-slate-700">
                                            <%# Convert.ToDateTime(Eval("StartDate")).ToString("dd-MMM-yyyy") %> to <%# Convert.ToDateTime(Eval("EndDate")).ToString("dd-MMM-yyyy") %>
                                        </div>
                                        <div class="text-[9px] font-bold text-slate-400 uppercase tracking-wide mt-0.5">
                                            <%# GetDurationString(Eval("StartDate"), Eval("EndDate")) %>
                                        </div>
                                    </ItemTemplate>
                                    <HeaderStyle CssClass="pb-3 text-slate-400 font-bold uppercase tracking-wider text-[10px]" />
                                    <ItemStyle CssClass="py-3.5 border-b border-slate-100" />
                                </asp:TemplateField>
                                <asp:BoundField DataField="AllocatedByName" HeaderText="Allocated By">
                                    <HeaderStyle CssClass="pb-3 text-slate-400 font-bold uppercase tracking-wider text-[10px]" />
                                    <ItemStyle CssClass="py-3.5 border-b border-slate-100 text-slate-500 font-semibold" />
                                </asp:BoundField>
                                <asp:TemplateField HeaderText="Actions">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="btnRelease" runat="server" CommandName="ReleaseVehicle" CommandArgument='<%# Eval("Id") %>' OnClientClick="return confirm('Are you sure you want to release this allocation? The vehicle will default back to HR Department.');" CssClass="rounded bg-red-50 hover:bg-red-100 text-red-600 px-3 py-1.5 font-bold transition-all focus:outline-none">Release</asp:LinkButton>
                                    </ItemTemplate>
                                    <HeaderStyle CssClass="pb-3 text-slate-400 font-bold uppercase tracking-wider text-[10px] text-right" />
                                    <ItemStyle CssClass="py-3.5 border-b border-slate-100 text-right" />
                                </asp:TemplateField>
                            </Columns>
                            <EmptyDataTemplate>
                                <div class="py-6 text-center text-slate-400 font-semibold">No active allocations found. All vehicles are in default HR ownership.</div>
                            </EmptyDataTemplate>
                        </asp:GridView>
                    </div>
                </div>

                <!-- Allocation History Table -->
                <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4">
                    <div class="border-b border-slate-100 pb-3">
                        <span class="text-xs font-extrabold text-slate-400 uppercase tracking-widest">Allocation History Log</span>
                    </div>

                    <div class="overflow-x-auto">
                        <asp:GridView ID="gvAllocationHistory" runat="server" AutoGenerateColumns="False" CssClass="w-full text-left border-collapse text-xs" GridLines="None">
                            <Columns>
                                <asp:TemplateField HeaderText="Vehicle Plate">
                                    <ItemTemplate>
                                        <span class="font-bold font-mono text-slate-700 bg-slate-100 px-1.5 py-0.5 rounded border border-slate-200"><%# Eval("VehicleNumber") %></span>
                                    </ItemTemplate>
                                    <HeaderStyle CssClass="pb-3 text-slate-400 font-bold uppercase tracking-wider text-[10px]" />
                                    <ItemStyle CssClass="py-2.5 border-b border-slate-100" />
                                </asp:TemplateField>
                                <asp:BoundField DataField="Department" HeaderText="Allocated Department">
                                    <HeaderStyle CssClass="pb-3 text-slate-400 font-bold uppercase tracking-wider text-[10px]" />
                                    <ItemStyle CssClass="py-2.5 border-b border-slate-100 font-bold text-slate-700" />
                                </asp:BoundField>
                                <asp:TemplateField HeaderText="Allocated Period">
                                    <ItemTemplate>
                                        <%# Convert.ToDateTime(Eval("StartDate")).ToString("dd-MMM-yyyy") %> - <%# Convert.ToDateTime(Eval("EndDate")).ToString("dd-MMM-yyyy") %>
                                    </ItemTemplate>
                                    <HeaderStyle CssClass="pb-3 text-slate-400 font-bold uppercase tracking-wider text-[10px]" />
                                    <ItemStyle CssClass="py-2.5 border-b border-slate-100 text-slate-600 font-semibold" />
                                </asp:TemplateField>
                                <asp:BoundField DataField="AllocatedByName" HeaderText="Allocated By">
                                    <HeaderStyle CssClass="pb-3 text-slate-400 font-bold uppercase tracking-wider text-[10px]" />
                                    <ItemStyle CssClass="py-2.5 border-b border-slate-100 text-slate-500" />
                                </asp:BoundField>
                                <asp:TemplateField HeaderText="Date Logged">
                                    <ItemTemplate>
                                        <%# Convert.ToDateTime(Eval("CreatedAt")).ToString("dd-MMM-yyyy HH:mm") %>
                                    </ItemTemplate>
                                    <HeaderStyle CssClass="pb-3 text-slate-400 font-bold uppercase tracking-wider text-[10px]" />
                                    <ItemStyle CssClass="py-2.5 border-b border-slate-100 text-slate-400 font-mono" />
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Status">
                                    <ItemTemplate>
                                        <span class="rounded px-2 py-0.5 text-[10px] font-bold uppercase bg-slate-100 text-slate-500 border border-slate-200">
                                            <%# Eval("Status") %>
                                        </span>
                                    </ItemTemplate>
                                    <HeaderStyle CssClass="pb-3 text-slate-400 font-bold uppercase tracking-wider text-[10px] text-right" />
                                    <ItemStyle CssClass="py-2.5 border-b border-slate-100 text-right" />
                                </asp:TemplateField>
                            </Columns>
                            <EmptyDataTemplate>
                                <div class="py-6 text-center text-slate-400 font-semibold">No historical allocations logged.</div>
                            </EmptyDataTemplate>
                        </asp:GridView>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
