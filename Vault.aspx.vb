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
            Response.Redirect("~/Login.aspx")
            Return
        End If

        If Not IsPostBack Then
            LoadDocuments()
        End If
    End Sub

    Private Sub LoadDocuments()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Dim q1 As String = "SELECT d.Id, d.FileName, d.FilePath, d.FileType, d.FileSize, d.CreatedAt, e.EmployeeName, " &
                          "'VEHICLE_RC' As LicenseType, v.VehicleNumber, dept.Code As DeptCode, v.IsVerified, v.VerifiedBy, v.Id As VehicleId " &
                          "FROM Documents d " &
                          "INNER JOIN Employee e ON d.UploadedBy = e.EmployeeId " &
                          "INNER JOIN Vehicles v ON v.DocumentId = d.Id " &
                          "INNER JOIN Departments dept ON v.DepartmentId = dept.Id"

        Dim q2 As String = "SELECT d.Id, d.FileName, d.FilePath, d.FileType, d.FileSize, d.CreatedAt, e.EmployeeName, " &
                          "r.LicenseType, v.VehicleNumber, dept.Code As DeptCode, r.IsVerified, r.VerifiedBy, v.Id As VehicleId " &
                          "FROM Documents d " &
                          "INNER JOIN Employee e ON d.UploadedBy = e.EmployeeId " &
                          "INNER JOIN ComplianceRecords r ON r.DocumentId = d.Id " &
                          "INNER JOIN Vehicles v ON r.VehicleId = v.Id " &
                          "INNER JOIN Departments dept ON v.DepartmentId = dept.Id"

        Dim where1 As New List(Of String)()
        Dim where2 As New List(Of String)()
        Dim parameters As New List(Of SQLiteParameter)()

        ' Scoping
        If role = "Employee" Then
            where1.Add("d.UploadedBy = @EmpId")
            where2.Add("d.UploadedBy = @EmpId")
            parameters.Add(New SQLiteParameter("@EmpId", empId))
        End If

        ' Search
        Dim search As String = txtSearch.Text.Trim()
        If Not String.IsNullOrEmpty(search) Then
            Dim searchClause As String = "(v.VehicleNumber LIKE @Search OR d.FileName LIKE @Search OR dept.Code LIKE @Search)"
            where1.Add(searchClause)
            where2.Add(searchClause)
            parameters.Add(New SQLiteParameter("@Search", "%" & search & "%"))
        End If

        ' Verified
        Dim verifiedFilter As String = ddlFilterVerified.SelectedValue
        If Not String.IsNullOrEmpty(verifiedFilter) Then
            Dim vVal As Integer = Convert.ToInt32(verifiedFilter)
            where1.Add("v.IsVerified = " & vVal)
            where2.Add("r.IsVerified = " & vVal)
        End If

        ' Apply wheres to queries
        If where1.Count > 0 Then
            q1 &= " WHERE " & String.Join(" AND ", where1.ToArray())
        End If
        If where2.Count > 0 Then
            q2 &= " WHERE " & String.Join(" AND ", where2.ToArray())
        End If

        ' Combine
        Dim finalSql As String = ""
        Dim typeFilter As String = ddlFilterType.SelectedValue
        If typeFilter = "VEHICLE_RC" Then
            finalSql = q1 & " ORDER BY d.Id DESC"
        ElseIf typeFilter = "COMPLIANCE" Then
            finalSql = q2 & " ORDER BY d.Id DESC"
        Else
            finalSql = q1 & " UNION ALL " & q2 & " ORDER BY Id DESC"
        End If

        Dim dt As DataTable = Database.ExecuteDataTable(finalSql, parameters.ToArray())
        
        ' Bind repeater
        rptVault.DataSource = dt
        rptVault.DataBind()

        ' Calculate stats dynamically
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
            Dim id As Integer = Convert.ToInt32(args(0))
            Dim currentVerified As Integer = Convert.ToInt32(args(1))
            Dim type As String = args(2)
            Dim vehicleId As Integer = Convert.ToInt32(args(3))
            
            Dim newVerified As Integer = If(currentVerified = 1, 0, 1)
            Dim verifier As String = Session("EmployeeName").ToString()
            Dim userId As Integer = Convert.ToInt32(Session("EmployeeId"))

            Try
                If type = "VEHICLE_RC" Then
                    Database.ExecuteNonQuery("UPDATE Vehicles SET IsVerified = " & newVerified & ", VerifiedBy = @Verifier, UpdatedAt = datetime('now') WHERE Id = " & vehicleId, New SQLiteParameter("@Verifier", verifier))
                    Compliance.UpdateVehicleStatus(vehicleId)
                    
                    Dim act As String = If(newVerified = 1, "VEHICLE_VERIFY", "VEHICLE_VERIFY_REVOKE")
                    Dim desc As String = "Vehicle RC copy verification " & If(newVerified = 1, "approved", "revoked") & " by SuperAdmin."
                    Database.ExecuteNonQuery("INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp, VehicleId) VALUES (" & userId & ", @User, @Action, @Desc, @IP, datetime('now'), " & vehicleId & ");", New SQLiteParameter("@User", verifier), New SQLiteParameter("@Action", act), New SQLiteParameter("@Desc", desc), New SQLiteParameter("@IP", Request.UserHostAddress))
                    
                    ' Email creator
                    Dim creatorIdObj As Object = Database.ExecuteScalar("SELECT EmployeeId FROM Vehicles WHERE Id = " & vehicleId)
                    If creatorIdObj IsNot Nothing Then
                        Dim plate As String = Database.ExecuteScalar("SELECT VehicleNumber FROM Vehicles WHERE Id = " & vehicleId).ToString()
                        EmailService.NotifyEmployeeOfApproval(Convert.ToInt32(creatorIdObj), plate)
                    End If
                Else
                    Database.ExecuteNonQuery("UPDATE ComplianceRecords SET IsVerified = " & newVerified & ", VerifiedBy = @Verifier, UpdatedAt = datetime('now') WHERE VehicleId = " & vehicleId & " AND LicenseType = @Type", New SQLiteParameter("@Verifier", verifier), New SQLiteParameter("@Type", type))
                    Compliance.UpdateVehicleStatus(vehicleId)
                    
                    Dim act As String = If(newVerified = 1, "DOCUMENT_VERIFY", "DOCUMENT_VERIFY_REVOKE")
                    Dim desc As String = "Compliance document " & type.Replace("_", " ") & " " & If(newVerified = 1, "approved", "revoked") & " by SuperAdmin."
                    Database.ExecuteNonQuery("INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp, VehicleId) VALUES (" & userId & ", @User, @Action, @Desc, @IP, datetime('now'), " & vehicleId & ");", New SQLiteParameter("@User", verifier), New SQLiteParameter("@Action", act), New SQLiteParameter("@Desc", desc), New SQLiteParameter("@IP", Request.UserHostAddress))
                End If

                LoadDocuments()
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Verification pass updated successfully!');", True)

            Catch ex As Exception
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Verification update failed: " & Server.HtmlEncode(ex.Message) & "');", True)
            End Try
        End If
    End Sub

    ' Helpers
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
End Class
