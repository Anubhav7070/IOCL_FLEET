Imports System
Imports System.Data
Imports System.Data.SQLite
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports System.Collections.Generic

Public Class AuditPage
    Inherits Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        ' Restrict access strictly to SuperAdmin and DEPT_ADMIN
        If Session("Role") Is Nothing Then
            Response.Redirect("~/Login.aspx")
        End If

        Dim role As String = Session("Role").ToString()
        If role <> "SuperAdmin" AndAlso role <> "DEPT_ADMIN" Then
            Response.Redirect("~/Default.aspx")
        End If

        If Not IsPostBack Then
            PopulateDepartmentsFilter()
            LoadAuditLogs()
        End If
    End Sub

    Private Sub PopulateDepartmentsFilter()
        ' Only SuperAdmin needs department filter dropdown
        If Session("Role").ToString() = "SuperAdmin" Then
            ddlDeptFilter.Items.Clear()
            ddlDeptFilter.Items.Add(New ListItem("All Divisions", ""))
            
            Dim dt As DataTable = Database.ExecuteDataTable("SELECT Id, Name FROM Departments ORDER BY Name")
            For Each row As DataRow In dt.Rows
                ddlDeptFilter.Items.Add(New ListItem(row("Name").ToString(), row("Id").ToString()))
            Next
        End If
    End Sub

    Private Sub LoadAuditLogs()
        Dim role As String = Session("Role").ToString()
        Dim sql As String = "SELECT Id, Timestamp, Username, Action, Description, IpAddress, OldValue, NewValue, DepartmentId FROM AuditLogs"
        
        Dim whereClauses As New List(Of String)()
        Dim parameters As New List(Of SQLiteParameter)()

        ' 1. Role-based Scoping
        If role = "DEPT_ADMIN" Then
            Dim userDeptName As String = If(Session("Department") IsNot Nothing, Session("Department").ToString(), "")
            Dim deptId As Integer = 0
            
            If Not String.IsNullOrEmpty(userDeptName) Then
                Dim objDeptId As Object = Database.ExecuteScalar("SELECT Id FROM Departments WHERE Name = @Name LIMIT 1", New SQLiteParameter("@Name", userDeptName))
                If objDeptId IsNot Nothing AndAlso Not Convert.IsDBNull(objDeptId) Then
                    deptId = Convert.ToInt32(objDeptId)
                End If
            End If
            
            whereClauses.Add("DepartmentId = @UserDeptId")
            parameters.Add(New SQLiteParameter("@UserDeptId", deptId))
        Else
            ' SuperAdmin filters
            If ddlDeptFilter.SelectedIndex > 0 Then
                Dim selectedDeptId As Integer = Convert.ToInt32(ddlDeptFilter.SelectedValue)
                whereClauses.Add("DepartmentId = @SelectedDeptId")
                parameters.Add(New SQLiteParameter("@SelectedDeptId", selectedDeptId))
            End If
        End If

        ' 2. Search filter
        If Not String.IsNullOrEmpty(txtSearch.Text.Trim()) Then
            whereClauses.Add("(Username LIKE @Search OR Action LIKE @Search OR Description LIKE @Search)")
            parameters.Add(New SQLiteParameter("@Search", "%" & txtSearch.Text.Trim() & "%"))
        End If

        If whereClauses.Count > 0 Then
            sql &= " WHERE " & String.Join(" AND ", whereClauses.ToArray())
        End If

        sql &= " ORDER BY Id DESC LIMIT 150"

        Dim dt As DataTable = Database.ExecuteDataTable(sql, parameters.ToArray())
        If dt.Rows.Count = 0 Then
            pnlNoLogs.Visible = True
            rptAudit.Visible = False
        Else
            pnlNoLogs.Visible = False
            rptAudit.Visible = True
            rptAudit.DataSource = dt
            rptAudit.DataBind()
        End If
    End Sub

    Protected Sub btnFilter_Click(ByVal sender As Object, ByVal e As EventArgs)
        LoadAuditLogs()
    End Sub

    Protected Sub btnReset_Click(ByVal sender As Object, ByVal e As EventArgs)
        txtSearch.Text = ""
        If ddlDeptFilter.Items.Count > 0 Then
            ddlDeptFilter.SelectedIndex = 0
        End If
        LoadAuditLogs()
    End Sub

    Protected Sub ddlDeptFilter_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
        LoadAuditLogs()
    End Sub

    ' Helpers
    Public Function FmtDateTime(ByVal dateObj As Object) As String
        If dateObj Is Nothing OrElse Convert.IsDBNull(dateObj) OrElse String.IsNullOrEmpty(dateObj.ToString()) Then
            Return "-"
        End If
        Dim dt As DateTime
        If DateTime.TryParse(dateObj.ToString(), dt) Then
            Return dt.ToString("dd-MMM-yyyy hh:mm:ss tt")
        End If
        Return dateObj.ToString()
    End Function

    Public Function HasPayload(ByVal oldVal As Object, ByVal newVal As Object) As Boolean
        Dim hasOld As Boolean = oldVal IsNot Nothing AndAlso Not Convert.IsDBNull(oldVal) AndAlso Not String.IsNullOrEmpty(oldVal.ToString()) AndAlso oldVal.ToString() <> "-"
        Dim hasNew As Boolean = newVal IsNot Nothing AndAlso Not Convert.IsDBNull(newVal) AndAlso Not String.IsNullOrEmpty(newVal.ToString()) AndAlso newVal.ToString() <> "-"
        Return hasOld OrElse hasNew
    End Function
End Class
