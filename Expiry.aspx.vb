Imports System
Imports System.IO
Imports System.Data
Imports System.Data.SQLite
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports System.Collections.Generic

Public Class ExpiryPage
    Inherits Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Session("EmployeeId") Is Nothing Then
            Response.Redirect("~/Login.aspx")
            Return
        End If

        If Not IsPostBack Then
            LoadFilterDepartments()
            LoadAlerts()

            ' Handle direct redirect parameters (from Vehicles list page)
            If Request.QueryString("renewId") IsNot Nothing Then
                Dim renewId As Integer = Convert.ToInt32(Request.QueryString("renewId"))
                LoadRenewalForm(renewId)
            ElseIf Request.QueryString("vehId") IsNot Nothing AndAlso Request.QueryString("type") IsNot Nothing Then
                Dim vehId As Integer = Convert.ToInt32(Request.QueryString("vehId"))
                Dim type As String = Request.QueryString("type")
                LoadRenewalFormForDirectSelect(vehId, type)
            End If
        End If
    End Sub

    Private Sub LoadFilterDepartments()
        Dim dt As DataTable = Database.ExecuteDataTable("SELECT Id, Code, Name FROM Departments ORDER BY Code")
        ddlDeptFilter.Items.Clear()
        ddlDeptFilter.Items.Add(New ListItem("All Divisions", ""))
        For Each row As DataRow In dt.Rows
            ddlDeptFilter.Items.Add(New ListItem(row("Code").ToString() & " - " & row("Name").ToString(), row("Id").ToString()))
        Next
    End Sub

    Private Sub LoadAlerts()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Dim sql As String = "SELECT r.Id, r.VehicleId, r.LicenseType, r.ExpiryDate, r.Status, v.VehicleNumber, d.Code As DeptName FROM ComplianceRecords r INNER JOIN Vehicles v ON r.VehicleId = v.Id INNER JOIN Departments d ON v.DepartmentId = d.Id WHERE r.Status IN ('EXPIRED', 'WARNING', 'MEDIUM_CRITICAL', 'HIGH_CRITICAL')"

        Dim whereClauses As New List(Of String)()
        Dim parameters As New List(Of SQLiteParameter)()

        ' Scope: Employees see only their own vehicle expiries
        If role = "Employee" Then
            whereClauses.Add("v.EmployeeId = @EmpId")
            parameters.Add(New SQLiteParameter("@EmpId", empId))
        End If

        ' Apply search
        If Not String.IsNullOrEmpty(txtVehSearch.Text.Trim()) Then
            whereClauses.Add("v.VehicleNumber LIKE @Search")
            parameters.Add(New SQLiteParameter("@Search", "%" & txtVehSearch.Text.Trim() & "%"))
        End If

        ' Apply department
        If Not String.IsNullOrEmpty(ddlDeptFilter.SelectedValue) Then
            whereClauses.Add("v.DepartmentId = @DeptId")
            parameters.Add(New SQLiteParameter("@DeptId", Convert.ToInt32(ddlDeptFilter.SelectedValue)))
        End If

        ' Apply severity filter
        If Not String.IsNullOrEmpty(ddlSeverityFilter.SelectedValue) Then
            If ddlSeverityFilter.SelectedValue = "CRITICAL" Then
                whereClauses.Add("r.Status IN ('HIGH_CRITICAL', 'MEDIUM_CRITICAL')")
            Else
                whereClauses.Add("r.Status = @Status")
                parameters.Add(New SQLiteParameter("@Status", ddlSeverityFilter.SelectedValue))
            End If
        End If

        If whereClauses.Count > 0 Then
            sql &= " AND " & String.Join(" AND ", whereClauses.ToArray())
        End If

        sql &= " ORDER BY r.ExpiryDate"

        Dim dt As DataTable = Database.ExecuteDataTable(sql, parameters.ToArray())
        rptAlerts.DataSource = dt
        rptAlerts.DataBind()
    End Sub

    Protected Sub FilterAlerts(ByVal sender As Object, ByVal e As EventArgs)
        LoadAlerts()
        pnlRenewForm.Visible = False
        pnlNoForm.Visible = True
    End Sub

    Protected Sub btnResetFilter_Click(ByVal sender As Object, ByVal e As EventArgs)
        txtVehSearch.Text = ""
        ddlDeptFilter.SelectedIndex = 0
        ddlSeverityFilter.SelectedIndex = 0
        LoadAlerts()
        pnlRenewForm.Visible = False
        pnlNoForm.Visible = True
    End Sub

    Protected Sub rptAlerts_ItemCommand(ByVal source As Object, ByVal e As RepeaterCommandEventArgs)
        If e.CommandName = "SelectAlert" Then
            Dim recordId As Integer = Convert.ToInt32(e.CommandArgument)
            LoadRenewalForm(recordId)
        ElseIf e.CommandName = "VerifyDoc" Then
            If Session("Role").ToString() <> "SuperAdmin" Then Return
            Dim recordId As Integer = Convert.ToInt32(e.CommandArgument)
            Dim empName As String = Session("EmployeeName").ToString()
            Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))
            Database.ExecuteNonQuery(
                "UPDATE ComplianceRecords SET IsVerified = 1, VerifiedBy = @Verifier, UpdatedAt = datetime('now') WHERE Id = @Id",
                New SQLiteParameter("@Verifier", empName), New SQLiteParameter("@Id", recordId))
            Database.ExecuteNonQuery(
                "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & empId & ", @User, 'VERIFY_COMPLIANCE_DOC', 'Verified compliance document #" & recordId & "', @IP, datetime('now'));",
                New SQLiteParameter("@User", empName), New SQLiteParameter("@IP", Request.UserHostAddress))
            LoadAlerts()
        ElseIf e.CommandName = "RevokeDoc" Then
            If Session("Role").ToString() <> "SuperAdmin" Then Return
            Dim recordId As Integer = Convert.ToInt32(e.CommandArgument)
            Dim empName As String = Session("EmployeeName").ToString()
            Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))
            Database.ExecuteNonQuery(
                "UPDATE ComplianceRecords SET IsVerified = 0, VerifiedBy = NULL, UpdatedAt = datetime('now') WHERE Id = @Id",
                New SQLiteParameter("@Id", recordId))
            Database.ExecuteNonQuery(
                "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & empId & ", @User, 'REVOKE_COMPLIANCE_DOC', 'Revoked compliance document verification #" & recordId & "', @IP, datetime('now'));",
                New SQLiteParameter("@User", empName), New SQLiteParameter("@IP", Request.UserHostAddress))
            LoadAlerts()
        End If
    End Sub

    Private Sub LoadRenewalForm(ByVal recordId As Integer)
        Dim sql As String = "SELECT r.*, v.VehicleNumber, v.Id As VehId FROM ComplianceRecords r INNER JOIN Vehicles v ON r.VehicleId = v.Id WHERE r.Id = @Id LIMIT 1"

        Dim dt As DataTable = Database.ExecuteDataTable(sql, New SQLiteParameter("@Id", recordId))
        If dt.Rows.Count = 0 Then Return

        Dim row As DataRow = dt.Rows(0)
        hdnRecordId.Value = recordId.ToString()
        hdnVehicleId.Value = row("VehId").ToString()
        txtVehPlate.Text = row("VehicleNumber").ToString()
        txtDocType.Text = row("LicenseType").ToString()

        txtDocNumber.Text = If(row("LicenseNumber") Is DBNull.Value, "", row("LicenseNumber").ToString())
        txtAuthority.Text = If(row("IssuingAuthority") Is DBNull.Value, "", row("IssuingAuthority").ToString())
        txtIssueDate.Text = If(row("IssueDate") Is DBNull.Value, "", row("IssueDate").ToString())
        txtExpiryDate.Text = If(row("ExpiryDate") Is DBNull.Value, "", row("ExpiryDate").ToString())
        txtRemarks.Text = ""

        pnlRenewForm.Visible = True
        pnlNoForm.Visible = False
    End Sub

    Private Sub LoadRenewalFormForDirectSelect(ByVal vehicleId As Integer, ByVal licenseType As String)
        Dim sql As String = "SELECT r.*, v.VehicleNumber FROM ComplianceRecords r INNER JOIN Vehicles v ON r.VehicleId = v.Id WHERE r.VehicleId = @VehId AND r.LicenseType = @Type LIMIT 1"
        
        Dim dt As DataTable = Database.ExecuteDataTable(sql, 
            New SQLiteParameter("@VehId", vehicleId),
            New SQLiteParameter("@Type", licenseType))

        If dt.Rows.Count > 0 Then
            Dim recordId As Integer = Convert.ToInt32(dt.Rows(0)("Id"))
            LoadRenewalForm(recordId)
        End If
    End Sub

    Protected Sub btnCancelRenew_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlRenewForm.Visible = False
        pnlNoForm.Visible = True
    End Sub

    Protected Sub btnSubmitRenew_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim recordId As Integer = Convert.ToInt32(hdnRecordId.Value)
        Dim vehicleId As Integer = Convert.ToInt32(hdnVehicleId.Value)
        Dim docNumber As String = txtDocNumber.Text.Trim()
        Dim authority As String = txtAuthority.Text.Trim()
        Dim issueDate As String = txtIssueDate.Text
        Dim expiryDate As String = txtExpiryDate.Text
        Dim remarks As String = txtRemarks.Text.Trim()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim empName As String = Session("EmployeeName").ToString()

        ' File Upload is mandatory
        If Not fileScan.HasFile Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Please upload a scanned certificate copy (PDF required.');", True)
            Return
        End If

        ' PDF only
        Dim fileExt As String = System.IO.Path.GetExtension(fileScan.PostedFile.FileName).ToLower()
        If fileExt <> ".pdf" Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Only PDF files are accepted. Scanned images are not permitted.');", True)
            Return
        End If

        ' Auto-extract expiry date from PDF if not manually entered
        Dim autoExtracted As Boolean = False
        If String.IsNullOrEmpty(expiryDate) Then
            Try
                Dim pdfText As String = PdfExtractor.ExtractText(fileScan.PostedFile.InputStream)
                Dim extracted As Nullable(Of DateTime) = PdfExtractor.ExtractExpiryDate(pdfText)
                If extracted.HasValue Then
                    expiryDate = extracted.Value.ToString("yyyy-MM-dd")
                    autoExtracted = True
                End If
            Catch ex As Exception
                Console.WriteLine("[PdfExtractor] " & ex.Message)
            End Try
        End If

        If String.IsNullOrEmpty(docNumber) OrElse String.IsNullOrEmpty(authority) OrElse String.IsNullOrEmpty(issueDate) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Document number, issuing authority, and issue date are required.');", True)
            Return
        End If

        If String.IsNullOrEmpty(expiryDate) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Could not auto-extract expiry date from the PDF. Please enter it manually.');", True)
            Return
        End If

        Dim expDt, issDt As DateTime
        If Not DateTime.TryParse(expiryDate, expDt) OrElse Not DateTime.TryParse(issueDate, issDt) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Invalid dates.');", True)
            Return
        End If

        If expDt <= issDt Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Expiry date must be after issue date.');", True)
            Return
        End If

        Try
            Dim fileName As String = System.IO.Path.GetFileName(fileScan.PostedFile.FileName)
            
            Dim uploadFolder As String = Server.MapPath("~/uploads")
            If Not Directory.Exists(uploadFolder) Then
                Directory.CreateDirectory(uploadFolder)
            End If

            Dim timestamp As String = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
            Dim savedFileName As String = timestamp & "_" & fileName
            Dim physicalPath As String = Path.Combine(uploadFolder, savedFileName)
            fileScan.SaveAs(physicalPath)

            Dim relativePath As String = "/uploads/" & savedFileName

            Dim insertDocSql As String = "INSERT INTO Documents (FileName, FilePath, FileType, FileSize, UploadedBy, CreatedAt) VALUES (@FileName, @FilePath, @FileType, @FileSize, @EmpId, datetime('now'));"
            Database.ExecuteNonQuery(insertDocSql,
                New SQLiteParameter("@FileName", fileName),
                New SQLiteParameter("@FilePath", relativePath),
                New SQLiteParameter("@FileType", fileScan.PostedFile.ContentType),
                New SQLiteParameter("@FileSize", fileScan.PostedFile.ContentLength),
                New SQLiteParameter("@EmpId", empId)
            )

            Dim newDocId As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT Id FROM Documents WHERE FilePath=@FilePath", New SQLiteParameter("@FilePath", relativePath)))

            ' Old document history
            Dim dtOld As DataTable = Database.ExecuteDataTable("SELECT ExpiryDate, DocumentId FROM ComplianceRecords WHERE Id = @Id LIMIT 1", New SQLiteParameter("@Id", recordId))
            Dim oldExpiry As String = ""
            Dim oldDocId As Object = DBNull.Value

            If dtOld.Rows.Count > 0 Then
                oldExpiry = dtOld.Rows(0)("ExpiryDate").ToString()
                If dtOld.Rows(0)("DocumentId") IsNot DBNull.Value Then
                    oldDocId = dtOld.Rows(0)("DocumentId")
                End If
            End If

            ' Update compliance record details
            Dim calculatedStatus As String = Compliance.CalculateStatus(expiryDate)
            Dim updateRecordSql As String = "UPDATE ComplianceRecords SET LicenseNumber = @Number, IssuingAuthority = @Auth, IssueDate = @IssueDate, ExpiryDate = @ExpiryDate, Status = @Status, DocumentId = @DocId, IsVerified = 0, VerifiedBy = NULL, LastUpdatedBy = @User, LastUpdatedTimestamp = datetime('now'), UpdatedAt = datetime('now') WHERE Id = @Id"

            Database.ExecuteNonQuery(updateRecordSql,
                New SQLiteParameter("@Number", docNumber),
                New SQLiteParameter("@Auth", authority),
                New SQLiteParameter("@IssueDate", issueDate),
                New SQLiteParameter("@ExpiryDate", expiryDate),
                New SQLiteParameter("@Status", calculatedStatus),
                New SQLiteParameter("@DocId", newDocId),
                New SQLiteParameter("@User", empName),
                New SQLiteParameter("@Id", recordId)
            )

            ' Create renewal log history
            Dim insertHistorySql As String = "INSERT INTO RenewalHistories (VehicleId, ComplianceRecordId, LicenseType, OldExpiryDate, NewExpiryDate, OldDocumentId, NewDocumentId, RenewedBy, RenewedAt, Remarks) VALUES (@VehId, @RecId, @Type, @OldExp, @NewExp, @OldDoc, @NewDoc, @EmpId, datetime('now'), @Remarks)"
            Database.ExecuteNonQuery(insertHistorySql,
                New SQLiteParameter("@VehId", vehicleId),
                New SQLiteParameter("@RecId", recordId),
                New SQLiteParameter("@Type", txtDocType.Text),
                New SQLiteParameter("@OldExp", If(String.IsNullOrEmpty(oldExpiry), DBNull.Value, oldExpiry)),
                New SQLiteParameter("@NewExp", expiryDate),
                New SQLiteParameter("@OldDoc", oldDocId),
                New SQLiteParameter("@NewDoc", newDocId),
                New SQLiteParameter("@EmpId", empId),
                New SQLiteParameter("@Remarks", remarks)
            )

            ' Notify SuperAdmins
            Dim plateNo As String = txtVehPlate.Text
            Dim docType As String = txtDocType.Text
            Dim notifTitle As String = "Document Renewed: " & plateNo
            Dim notifMsg As String = "Employee " & empName & " renewed the " & docType.Replace("_", " ") & " for vehicle " & plateNo & " (Doc Number: " & docNumber & ")."
            
            Dim deptId As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT DepartmentId FROM Vehicles WHERE Id = " & vehicleId))

            Dim sqlNotif As String = "INSERT INTO Notifications (VehicleId, DepartmentId, Title, Message, Status, Type, CreatedAt) VALUES (" & vehicleId & ", " & deptId & ", @Title, @Msg, 'UNREAD', 'RENEWAL', datetime('now'));"
            Database.ExecuteNonQuery(sqlNotif, New SQLiteParameter("@Title", notifTitle), New SQLiteParameter("@Msg", notifMsg))

            EmailService.NotifySuperAdminsOfRenewal(empName, plateNo, docType)

            ' Refresh status
            Compliance.UpdateVehicleStatus(vehicleId)

            ' Log Audit
            Dim sqlAudit As String = "INSERT INTO AuditLogs (UserId, Username, Action, Description, OldValue, NewValue, IpAddress, Timestamp, VehicleId, DepartmentId) VALUES (" & empId & ", @User, 'DOCUMENT_RENEWAL', 'Uploaded and renewed certificate for " & docType & " of vehicle " & plateNo & ". Pending verification.', @OldExp, @NewExp, @IP, datetime('now'), " & vehicleId & ", " & deptId & ");"
            Database.ExecuteNonQuery(sqlAudit, New SQLiteParameter("@User", empName), New SQLiteParameter("@OldExp", oldExpiry), New SQLiteParameter("@NewExp", expiryDate), New SQLiteParameter("@IP", Request.UserHostAddress))

            pnlRenewForm.Visible = False
            pnlNoForm.Visible = True
            LoadAlerts()
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Document renewal submitted successfully. Pending verification.');", True)

        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Renewal failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    ' Helpers
    Public Function GetAlertBadgeClass(ByVal statusObj As Object) As String
        Dim status As String = statusObj.ToString()
        Select Case status
            Case "WARNING"
                Return "bg-yellow-50 text-yellow-750 border border-yellow-250"
            Case "EXPIRED"
                Return "bg-red-50 text-red-700 border border-red-200"
            Case Else
                Return "bg-orange-50 text-orange-700 border border-orange-200"
        End Select
    End Function

    Public Function FmtDate(ByVal dateObj As Object) As String
        If dateObj Is Nothing OrElse Convert.IsDBNull(dateObj) OrElse String.IsNullOrEmpty(dateObj.ToString()) Then
            Return "PENDING"
        End If
        Dim dt As DateTime
        If DateTime.TryParse(dateObj.ToString(), dt) Then
            Return dt.ToString("dd-MMM-yyyy")
        End If
        Return dateObj.ToString()
    End Function

    Public Function GetDaysRemainingText(ByVal expiryDateObj As Object) As String
        If expiryDateObj Is Nothing OrElse Convert.IsDBNull(expiryDateObj) OrElse String.IsNullOrEmpty(expiryDateObj.ToString()) Then
            Return "N/A"
        End If
        
        Dim expiry As DateTime
        If Not DateTime.TryParse(expiryDateObj.ToString(), expiry) Then Return "N/A"

        Dim diff As Integer = Convert.ToInt32(Math.Ceiling((expiry.Date - DateTime.Today).TotalDays))
        If diff < 0 Then
            Return Math.Abs(diff).ToString() & " days overdue"
        Else
            Return diff.ToString() & " days remaining"
        End If
    End Function
End Class
