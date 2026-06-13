Imports System
Imports System.Data
Imports System.Data.SQLite
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports System.Collections.Generic

Public Class VaultPage
    Inherits Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Session("EmployeeId") Is Nothing Then
            Response.Redirect("~/Login.aspx", False)
            HttpContext.Current.ApplicationInstance.CompleteRequest()
            Return
        End If

        If Not IsPostBack Then
            LoadDocuments()
        End If
    End Sub

    Private Sub LoadDocuments()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        ' Primary query: All compliance records joined with optional documents.
        ' Shows ALL registered compliance slots, even if no PDF has been uploaded yet.
        Dim sql As String =
            "SELECT r.Id, " &
            "COALESCE(d.FileName, 'No document uploaded') AS FileName, " &
            "COALESCE(d.FilePath, '') AS FilePath, " &
            "COALESCE(d.FileType, '') AS FileType, " &
            "COALESCE(d.FileSize, 0) AS FileSize, " &
            "COALESCE(d.CreatedAt, r.CreatedAt) AS CreatedAt, " &
            "COALESCE(e.EmployeeName, reg.EmployeeName, 'Unknown') AS EmployeeName, " &
            "r.LicenseType, v.VehicleNumber, v.Department AS DeptCode, " &
            "r.IsVerified, r.VerifiedBy, v.Id AS VehicleId, r.Status, r.ExpiryDate " &
            "FROM ComplianceRecords r " &
            "INNER JOIN Vehicles v ON r.VehicleId = v.Id " &
            "LEFT JOIN Documents d ON r.DocumentId = d.Id " &
            "LEFT JOIN Employee e ON d.UploadedBy = e.EmployeeId " &
            "LEFT JOIN Employee reg ON v.EmployeeId = reg.EmployeeId"

        Dim whereClauses As New List(Of String)()
        Dim parameters As New List(Of SQLiteParameter)()

        ' Scoping: Employees see only their own vehicles
        If role = "Employee" Then
            whereClauses.Add("v.EmployeeId = @EmpId")
            parameters.Add(New SQLiteParameter("@EmpId", empId))
        End If

        ' Type filter
        Dim typeFilter As String = ddlFilterType.SelectedValue
        If typeFilter = "VEHICLE_RC" Then
            whereClauses.Add("r.LicenseType = 'VEHICLE_RC'")
        ElseIf typeFilter = "COMPLIANCE" Then
            whereClauses.Add("r.LicenseType <> 'VEHICLE_RC'")
        End If

        ' Search
        Dim search As String = txtSearch.Text.Trim()
        If Not String.IsNullOrEmpty(search) Then
            whereClauses.Add("(v.VehicleNumber LIKE @Search OR r.LicenseType LIKE @Search OR v.Department LIKE @Search OR COALESCE(d.FileName,'') LIKE @Search)")
            parameters.Add(New SQLiteParameter("@Search", "%" & search & "%"))
        End If

        ' Verified filter
        Dim verifiedFilter As String = ddlFilterVerified.SelectedValue
        If Not String.IsNullOrEmpty(verifiedFilter) Then
            Dim vVal As Integer = Convert.ToInt32(verifiedFilter)
            whereClauses.Add("r.IsVerified = " & vVal)
        End If

        If whereClauses.Count > 0 Then
            sql &= " WHERE " & String.Join(" AND ", whereClauses.ToArray())
        End If

        sql &= " ORDER BY v.VehicleNumber, r.LicenseType"

        Dim dt As DataTable = Database.ExecuteDataTable(sql, parameters.ToArray())

        ' Bind repeater
        rptVault.DataSource = dt
        rptVault.DataBind()

        ' Calculate stats
        Dim totalDocs As Integer = dt.Rows.Count
        Dim rcCopies As Integer = 0
        Dim compDocs As Integer = 0
        Dim verifiedDocs As Integer = 0

        For Each row As DataRow In dt.Rows
            If row("LicenseType").ToString() = "VEHICLE_RC" Then
                rcCopies += 1
            Else
                compDocs += 1
            End If
            If Convert.ToInt32(row("IsVerified")) = 1 Then
                verifiedDocs += 1
            End If
        Next

        lblTotalDocs.Text = totalDocs.ToString()
        lblRcCopies.Text = rcCopies.ToString()
        lblComplianceDocs.Text = compDocs.ToString()
        lblVerifiedDocs.Text = verifiedDocs.ToString()
    End Sub

    Protected Sub FilterDocs(ByVal sender As Object, ByVal e As EventArgs)
        LoadDocuments()
    End Sub

    Protected Sub btnRefresh_Click(ByVal sender As Object, ByVal e As EventArgs)
        txtSearch.Text = ""
        ddlFilterType.SelectedIndex = 0
        ddlFilterVerified.SelectedIndex = 0
        LoadDocuments()
    End Sub

    Protected Sub btnReset_Click(ByVal sender As Object, ByVal e As EventArgs)
        txtSearch.Text = ""
        ddlFilterType.SelectedIndex = 0
        ddlFilterVerified.SelectedIndex = 0
        LoadDocuments()
    End Sub

    Protected Sub rptVault_ItemCommand(ByVal source As Object, ByVal e As RepeaterCommandEventArgs)
        If e.CommandName = "ToggleVerify" Then
            Dim args As String() = e.CommandArgument.ToString().Split("|"c)
            Dim recordId As Integer = Convert.ToInt32(args(0))   ' ComplianceRecord Id
            Dim currentVerified As Integer = Convert.ToInt32(args(1))
            Dim licType As String = args(2)
            Dim vehicleId As Integer = Convert.ToInt32(args(3))

            Dim newVerified As Integer = If(currentVerified = 1, 0, 1)
            Dim verifier As String = Session("EmployeeName").ToString()
            Dim userId As Integer = Convert.ToInt32(Session("EmployeeId"))

            Try
                ' Update compliance record verification
                Database.ExecuteNonQuery(
                    "UPDATE ComplianceRecords SET IsVerified = " & newVerified & ", VerifiedBy = @Verifier, UpdatedAt = datetime('now') WHERE Id = " & recordId,
                    New SQLiteParameter("@Verifier", verifier))

                ' Also update vehicle-level verification flag
                If licType = "VEHICLE_RC" Then
                    Database.ExecuteNonQuery(
                        "UPDATE Vehicles SET IsVerified = " & newVerified & ", VerifiedBy = @Verifier, UpdatedAt = datetime('now') WHERE Id = " & vehicleId,
                        New SQLiteParameter("@Verifier", verifier))
                End If

                Compliance.UpdateVehicleStatus(vehicleId)

                Dim act As String = If(newVerified = 1, "DOCUMENT_VERIFY", "DOCUMENT_VERIFY_REVOKE")
                Dim desc As String = "Compliance document " & licType.Replace("_", " ") & " " & If(newVerified = 1, "verified", "revoked") & " by SuperAdmin."
                Dim dept As String = ""
                Try
                    dept = Database.ExecuteScalar("SELECT Department FROM Vehicles WHERE Id = " & vehicleId).ToString()
                Catch
                End Try

                Database.ExecuteNonQuery(
                    "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp, VehicleId, Department) " &
                    "VALUES (" & userId & ", @User, @Action, @Desc, @IP, datetime('now'), " & vehicleId & ", @Dept);",
                    New SQLiteParameter("@User", verifier),
                    New SQLiteParameter("@Action", act),
                    New SQLiteParameter("@Desc", desc),
                    New SQLiteParameter("@IP", Request.UserHostAddress),
                    New SQLiteParameter("@Dept", dept))

                ' Notify vehicle owner on approval
                If newVerified = 1 Then
                    Try
                        Dim creatorIdObj As Object = Database.ExecuteScalar("SELECT EmployeeId FROM Vehicles WHERE Id = " & vehicleId)
                        Dim plate As String = Database.ExecuteScalar("SELECT VehicleNumber FROM Vehicles WHERE Id = " & vehicleId).ToString()
                        If creatorIdObj IsNot Nothing Then
                            EmailService.NotifyEmployeeOfApproval(Convert.ToInt32(creatorIdObj), plate)
                        End If
                    Catch
                    End Try
                End If

                LoadDocuments()
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Verification status updated successfully!');", True)

            Catch ex As Exception
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Verification update failed: " & Server.HtmlEncode(ex.Message) & "');", True)
            End Try
        End If
    End Sub

    ' â”€â”€ Helpers â”€â”€

    Public Function FmtDate(ByVal dateObj As Object) As String
        If dateObj Is Nothing OrElse Convert.IsDBNull(dateObj) OrElse String.IsNullOrEmpty(dateObj.ToString()) Then
            Return "-"
        End If
        Dim dt As DateTime
        If DateTime.TryParse(dateObj.ToString(), dt) Then
            Return dt.ToString("dd-MMM-yyyy HH:mm")
        End If
        Return dateObj.ToString()
    End Function

    Public Function HasDocument(ByVal filePath As Object) As Boolean
        Return filePath IsNot Nothing AndAlso Not Convert.IsDBNull(filePath) AndAlso Not String.IsNullOrEmpty(filePath.ToString())
    End Function

    Public Function GetStatusBadgeClass(ByVal statusObj As Object) As String
        If statusObj Is Nothing OrElse Convert.IsDBNull(statusObj) Then Return "bg-slate-100 text-slate-500"
        Select Case statusObj.ToString()
            Case "ACTIVE", "FULLY_COMPLIANT"
                Return "bg-emerald-100 text-emerald-700"
            Case "WARNING"
                Return "bg-yellow-100 text-yellow-700"
            Case "CRITICAL", "HIGH_CRITICAL", "MEDIUM_CRITICAL"
                Return "bg-orange-100 text-orange-700"
            Case "EXPIRED"
                Return "bg-red-100 text-red-700"
            Case Else
                Return "bg-slate-100 text-slate-500"
        End Select
    End Function

End Class
