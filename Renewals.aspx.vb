Imports System
Imports System.Data
Imports System.Data.SQLite
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports System.Collections.Generic

Public Class RenewalsHistoryPage
    Inherits Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Session("EmployeeId") Is Nothing Then
            Response.Redirect("~/Login.aspx")
            Return
        End If

        If Not IsPostBack Then
            LoadHistory()
        End If
    End Sub

    Private Sub LoadHistory()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Dim sql As String = "SELECT h.Id, h.OldExpiryDate, h.NewExpiryDate, h.RenewedAt, h.Remarks, h.LicenseType, v.VehicleNumber, e.EmployeeName, dOld.FilePath As OldDocPath, dNew.FilePath As NewDocPath FROM RenewalHistories h INNER JOIN Vehicles v ON h.VehicleId = v.Id INNER JOIN Employee e ON h.RenewedBy = e.EmployeeId LEFT JOIN Documents dOld ON h.OldDocumentId = dOld.Id LEFT JOIN Documents dNew ON h.NewDocumentId = dNew.Id"

        Dim whereClauses As New List(Of String)()
        Dim parameters As New List(Of SQLiteParameter)()

        ' Scoping: Employees see only renewals of their vehicles
        If role = "Employee" Then
            whereClauses.Add("(v.EmployeeId = @EmpId OR h.RenewedBy = @EmpId)")
            parameters.Add(New SQLiteParameter("@EmpId", empId))
        End If

        ' Apply search
        If Not String.IsNullOrEmpty(txtSearchPlate.Text.Trim()) Then
            whereClauses.Add("v.VehicleNumber LIKE @Search")
            parameters.Add(New SQLiteParameter("@Search", "%" & txtSearchPlate.Text.Trim() & "%"))
        End If

        ' Apply type
        If Not String.IsNullOrEmpty(ddlLicenseFilter.SelectedValue) Then
            whereClauses.Add("h.LicenseType = @Type")
            parameters.Add(New SQLiteParameter("@Type", ddlLicenseFilter.SelectedValue))
        End If

        If whereClauses.Count > 0 Then
            sql &= " WHERE " & String.Join(" AND ", whereClauses.ToArray())
        End If

        sql &= " ORDER BY h.Id DESC"

        Dim dt As DataTable = Database.ExecuteDataTable(sql, parameters.ToArray())
        rptHistory.DataSource = dt
        rptHistory.DataBind()
    End Sub

    Protected Sub FilterLogs(ByVal sender As Object, ByVal e As EventArgs)
        LoadHistory()
    End Sub

    Protected Sub btnReset_Click(ByVal sender As Object, ByVal e As EventArgs)
        txtSearchPlate.Text = ""
        ddlLicenseFilter.SelectedIndex = 0
        LoadHistory()
    End Sub

    ' Helpers
    Public Function FmtDate(ByVal dateObj As Object) As String
        If dateObj Is Nothing OrElse Convert.IsDBNull(dateObj) OrElse String.IsNullOrEmpty(dateObj.ToString()) Then
            Return "N/A"
        End If
        Dim dt As DateTime
        If DateTime.TryParse(dateObj.ToString(), dt) Then
            Return dt.ToString("dd-MMM-yyyy")
        End If
        Return dateObj.ToString()
    End Function

    Public Function FmtDateTime(ByVal dateObj As Object) As String
        If dateObj Is Nothing OrElse Convert.IsDBNull(dateObj) OrElse String.IsNullOrEmpty(dateObj.ToString()) Then
            Return "-"
        End If
        Dim dt As DateTime
        If DateTime.TryParse(dateObj.ToString(), dt) Then
            Return dt.ToString("dd-MMM-yyyy HH:mm")
        End If
        Return dateObj.ToString()
    End Function

    Public Function GetDocLink(ByVal pathObj As Object, ByVal label As String) As String
        If pathObj Is Nothing OrElse Convert.IsDBNull(pathObj) OrElse String.IsNullOrEmpty(pathObj.ToString()) Then
            Return "<span class='text-slate-400 font-semibold'>" & label & " (N/A)</span>"
        End If
        Dim url As String = ResolveUrl("~" & pathObj.ToString())
        Return "<a href='" & url & "' target='_blank' class='text-[#0054A6] hover:underline font-bold'>" & label & " Copy</a>"
    End Function
End Class
