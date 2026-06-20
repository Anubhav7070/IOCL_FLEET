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
            Response.Redirect("~/Login.aspx", False)
            HttpContext.Current.ApplicationInstance.CompleteRequest()
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
        Dim dt As DataTable = Database.ExecuteDataTable("SELECT DISTINCT Department As Code FROM Employee WHERE Department IS NOT NULL AND Department <> '' ORDER BY Department")
        ddlDeptFilter.Items.Clear()
        ddlDeptFilter.Items.Add(New ListItem("All Divisions", ""))
        For Each row As DataRow In dt.Rows
            ddlDeptFilter.Items.Add(New ListItem(row("Code").ToString(), row("Code").ToString()))
        Next
    End Sub

    Private Sub LoadAlerts()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Dim sql As String = "SELECT r.Id, r.VehicleId, r.LicenseType, r.ExpiryDate, r.Status, v.VehicleNumber, v.Department As DeptName FROM ComplianceRecords r INNER JOIN Vehicles v ON r.VehicleId = v.Id WHERE r.Status IN ('Expired', 'Expiring') AND (v.IsDecommissioned = 0 OR v.IsDecommissioned IS NULL)"

        Dim whereClauses As New List(Of String)()
        Dim parameters As New List(Of SQLiteParameter)()

        ' Scope: Employees see only their own vehicle expiries
        If role = "Employee" Then
            whereClauses.Add("v.EmployeeId = @EmpId")
            parameters.Add(New SQLiteParameter("@EmpId", empId))
        ElseIf role = "DEPT_ADMIN" Then
            ' DEPT_ADMIN sees only their department
            Dim deptScope As String = If(Session("Department") IsNot Nothing, Session("Department").ToString(), "")
            If Not String.IsNullOrEmpty(deptScope) Then
                whereClauses.Add("v.Department = @DeptScope")
                parameters.Add(New SQLiteParameter("@DeptScope", deptScope))
            End If
        End If

        ' Apply search
        If Not String.IsNullOrEmpty(txtVehSearch.Text.Trim()) Then
            whereClauses.Add("v.VehicleNumber LIKE @Search")
            parameters.Add(New SQLiteParameter("@Search", "%" & txtVehSearch.Text.Trim() & "%"))
        End If

        ' Apply department
        If Not String.IsNullOrEmpty(ddlDeptFilter.SelectedValue) Then
            whereClauses.Add("v.Department = @Dept")
            parameters.Add(New SQLiteParameter("@Dept", ddlDeptFilter.SelectedValue))
        End If

        ' Apply severity filter
        If Not String.IsNullOrEmpty(ddlSeverityFilter.SelectedValue) Then
            whereClauses.Add("r.Status = @Status")
            parameters.Add(New SQLiteParameter("@Status", ddlSeverityFilter.SelectedValue))
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
        Dim role As String = Session("Role").ToString()

        If e.CommandName = "SelectAlert" Then
            Dim recordId As Integer = Convert.ToInt32(e.CommandArgument)
            LoadRenewalForm(recordId)
        ElseIf e.CommandName = "SendNotification" Then
            ' SuperAdmin sends a renewal reminder notification to the vehicle owner
            Dim recordId As Integer = Convert.ToInt32(e.CommandArgument)
            Dim empName As String = Session("EmployeeName").ToString()
            Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

            Try
                ' Get vehicle owner's info
                Dim dtRec As DataTable = Database.ExecuteDataTable(
                    "SELECT r.LicenseType, r.ExpiryDate, v.VehicleNumber, v.Id As VehId, v.Department, e.EmployeeId As OwnerId, e.EmployeeName As OwnerName, e.EmailId AS OwnerEmail " &
                    "FROM ComplianceRecords r " &
                    "INNER JOIN Vehicles v ON r.VehicleId = v.Id " &
                    "INNER JOIN Employee e ON v.EmployeeId = e.EmployeeId " &
                    "WHERE r.Id = @Id LIMIT 1",
                    New SQLiteParameter("@Id", recordId))

                If dtRec.Rows.Count > 0 Then
                    Dim row As DataRow = dtRec.Rows(0)
                    Dim docType As String = row("LicenseType").ToString()
                    Dim plate As String = row("VehicleNumber").ToString()
                    Dim dept As String = row("Department").ToString()
                    Dim vehId As Integer = Convert.ToInt32(row("VehId"))
                    Dim ownerId As Integer = Convert.ToInt32(row("OwnerId"))
                    Dim ownerName As String = row("OwnerName").ToString()
                    Dim expiry As String = row("ExpiryDate").ToString()

                    Dim notifTitle As String = "Renewal Required: " & docType.Replace("_", " ") & " for " & plate
                    Dim notifMsg As String = "SuperAdmin " & empName & " has notified you that the " & docType.Replace("_", " ") & " for vehicle " & plate & " (Expiry: " & expiry & ") needs urgent renewal. Please submit the renewal at the earliest."

                    ' Insert notification for the vehicle owner
                    Database.ExecuteNonQuery(
                        "INSERT INTO Notifications (VehicleId, Department, Title, Message, Status, Type, CreatedAt) VALUES (@VehId, @Dept, @Title, @Msg, 'UNREAD', 'EXPIRY_REMINDER', datetime('now'));",
                        New SQLiteParameter("@VehId", vehId),
                        New SQLiteParameter("@Dept", dept),
                        New SQLiteParameter("@Title", notifTitle),
                        New SQLiteParameter("@Msg", notifMsg))

                    ' Try email notification
                    Try
                        Dim parsedExpiry As DateTime
                        Dim daysRemaining As Integer = 0
                        If DateTime.TryParse(expiry, parsedExpiry) Then
                            daysRemaining = (parsedExpiry.Date - DateTime.Today).Days
                        End If
                        EmailService.NotifyEmployeeOfDocumentExpiry(ownerId, plate, docType, expiry, daysRemaining)
                    Catch
                    End Try

                    ' Log audit
                    Database.ExecuteNonQuery(
                        "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp, VehicleId, Department) VALUES (" & empId & ", @User, 'EXPIRY_NOTIFICATION_SENT', 'SuperAdmin sent renewal notification for " & docType & " of vehicle " & plate & " to " & ownerName & ".', @IP, datetime('now'), " & vehId & ", @Dept);",
                        New SQLiteParameter("@User", empName),
                        New SQLiteParameter("@IP", Request.UserHostAddress),
                        New SQLiteParameter("@Dept", dept))

                    ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Renewal notification sent to " & Server.HtmlEncode(ownerName) & " for vehicle " & Server.HtmlEncode(plate) & ".');", True)
                End If
            Catch ex As Exception
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Failed to send notification: " & Server.HtmlEncode(ex.Message) & "');", True)
            End Try

            LoadAlerts()
        ElseIf e.CommandName = "VerifyDoc" Then
            If role <> "SuperAdmin" Then Return
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
            If role <> "SuperAdmin" Then Return
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
        Dim sql As String = "SELECT r.*, v.VehicleNumber, v.Id As VehId, v.EmployeeId As VehicleOwnerId FROM ComplianceRecords r INNER JOIN Vehicles v ON r.VehicleId = v.Id WHERE r.Id = @Id LIMIT 1"

        Dim dt As DataTable = Database.ExecuteDataTable(sql, New SQLiteParameter("@Id", recordId))
        If dt.Rows.Count = 0 Then Return

        Dim row As DataRow = dt.Rows(0)
        Dim ownerId As Integer = Convert.ToInt32(row("VehicleOwnerId"))
        Dim loggedInEmpId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim role As String = Session("Role").ToString()

        Dim isOwner As Boolean = (ownerId = loggedInEmpId)

        If Not isOwner Then
            If role = "SuperAdmin" Then
                ' Super Admin is restricted to view-only and sending notifications!
                LoadNotifyPanel(recordId)
                Return
            Else
                ' Other non-owners are redirected back to default with a warning
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Access Denied: Only the vehicle owner or creator can renew this document.'); window.location.href='Default.aspx';", True)
                Return
            End If
        End If

        hdnRecordId.Value = recordId.ToString()
        hdnVehicleId.Value = row("VehId").ToString()
        txtVehPlate.Text = row("VehicleNumber").ToString()
        txtDocType.Text = row("LicenseType").ToString()

        txtDocNumber.Text = If(row("LicenseNumber") Is DBNull.Value, "", row("LicenseNumber").ToString())
        txtAuthority.Text = If(row("IssuingAuthority") Is DBNull.Value, "", row("IssuingAuthority").ToString())
        txtIssueDate.Text = If(row("IssueDate") Is DBNull.Value, "", row("IssueDate").ToString())
        txtExpiryDate.Text = If(row("ExpiryDate") Is DBNull.Value, "", row("ExpiryDate").ToString())
        txtRemarks.Text = ""

        txtDocNumber.Enabled = True
        txtAuthority.Enabled = True
        txtIssueDate.Enabled = True
        txtExpiryDate.Enabled = True
        txtRemarks.Enabled = True
        fileScan.Visible = True
        btnSubmitRenew.Visible = True
        btnSendNotify.Visible = False

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

    ' SuperAdmin-only: Load a read-only view with "Send Notification" button
    Private Sub LoadNotifyPanel(ByVal recordId As Integer)
        Dim sql As String = "SELECT r.*, v.VehicleNumber, v.Id As VehId FROM ComplianceRecords r INNER JOIN Vehicles v ON r.VehicleId = v.Id WHERE r.Id = @Id LIMIT 1"
        Dim dt As DataTable = Database.ExecuteDataTable(sql, New SQLiteParameter("@Id", recordId))
        If dt.Rows.Count = 0 Then Return

        Dim row As DataRow = dt.Rows(0)
        hdnRecordId.Value = recordId.ToString()
        hdnVehicleId.Value = row("VehId").ToString()
        txtVehPlate.Text = row("VehicleNumber").ToString()
        txtDocType.Text = row("LicenseType").ToString()
        txtDocNumber.Text = If(row("LicenseNumber") Is DBNull.Value, "N/A", row("LicenseNumber").ToString())
        txtAuthority.Text = If(row("IssuingAuthority") Is DBNull.Value, "N/A", row("IssuingAuthority").ToString())
        txtIssueDate.Text = If(row("IssueDate") Is DBNull.Value, "", row("IssueDate").ToString())
        txtExpiryDate.Text = If(row("ExpiryDate") Is DBNull.Value, "", row("ExpiryDate").ToString())
        txtRemarks.Text = ""

        ' In SuperAdmin mode, disable the renewal form fields and show notify button
        txtDocNumber.Enabled = False
        txtAuthority.Enabled = False
        txtIssueDate.Enabled = False
        txtExpiryDate.Enabled = False
        txtRemarks.Enabled = False
        fileScan.Visible = False
        btnSubmitRenew.Visible = False
        btnSendNotify.Visible = True

        pnlRenewForm.Visible = True
        pnlNoForm.Visible = False
    End Sub



    Protected Sub btnCancelRenew_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlRenewForm.Visible = False
        pnlNoForm.Visible = True
    End Sub

    Protected Sub btnSendNotify_Click(ByVal sender As Object, ByVal e As EventArgs)
        ' Trigger the SendNotification command logic using the hidden record ID
        Dim recordId As Integer = Convert.ToInt32(hdnRecordId.Value)
        Dim empName As String = Session("EmployeeName").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Try
            Dim dtRec As DataTable = Database.ExecuteDataTable(
                "SELECT r.LicenseType, r.ExpiryDate, v.VehicleNumber, v.Id As VehId, v.Department, e.EmployeeId As OwnerId, e.EmployeeName As OwnerName " &
                "FROM ComplianceRecords r " &
                "INNER JOIN Vehicles v ON r.VehicleId = v.Id " &
                "INNER JOIN Employee e ON v.EmployeeId = e.EmployeeId " &
                "WHERE r.Id = @Id LIMIT 1",
                New SQLiteParameter("@Id", recordId))

            If dtRec.Rows.Count > 0 Then
                Dim row As DataRow = dtRec.Rows(0)
                Dim docType As String = row("LicenseType").ToString()
                Dim plate As String = row("VehicleNumber").ToString()
                Dim dept As String = row("Department").ToString()
                Dim vehId As Integer = Convert.ToInt32(row("VehId"))
                Dim ownerId As Integer = Convert.ToInt32(row("OwnerId"))
                Dim ownerName As String = row("OwnerName").ToString()
                Dim expiry As String = row("ExpiryDate").ToString()

                Dim notifTitle As String = "Renewal Required: " & docType.Replace("_", " ") & " for " & plate
                Dim notifMsg As String = "SuperAdmin " & empName & " has notified you that the " & docType.Replace("_", " ") & " for vehicle " & plate & " (Expiry: " & expiry & ") needs urgent renewal."

                Database.ExecuteNonQuery(
                    "INSERT INTO Notifications (VehicleId, Department, Title, Message, Status, Type, CreatedAt) VALUES (@VehId, @Dept, @Title, @Msg, 'UNREAD', 'EXPIRY_REMINDER', datetime('now'));",
                    New SQLiteParameter("@VehId", vehId),
                    New SQLiteParameter("@Dept", dept),
                    New SQLiteParameter("@Title", notifTitle),
                    New SQLiteParameter("@Msg", notifMsg))

                Try
                    Dim parsedExpiry As DateTime
                    Dim daysRemaining As Integer = 0
                    If DateTime.TryParse(expiry, parsedExpiry) Then
                        daysRemaining = (parsedExpiry.Date - DateTime.Today).Days
                    End If
                    EmailService.NotifyEmployeeOfDocumentExpiry(ownerId, plate, docType, expiry, daysRemaining)
                Catch
                End Try

                Database.ExecuteNonQuery(
                    "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp, VehicleId, Department) VALUES (" & empId & ", @User, 'EXPIRY_NOTIFICATION_SENT', 'SuperAdmin sent renewal notification for " & docType & " of vehicle " & plate & " to " & ownerName & ".', @IP, datetime('now'), " & vehId & ", @Dept);",
                    New SQLiteParameter("@User", empName),
                    New SQLiteParameter("@IP", Request.UserHostAddress),
                    New SQLiteParameter("@Dept", dept))

                pnlRenewForm.Visible = False
                pnlNoForm.Visible = True
                LoadAlerts()
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Renewal notification sent to " & Server.HtmlEncode(ownerName) & " successfully.');", True)
            End If
        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Failed to send notification: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    Protected Sub btnSubmitRenew_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim recordId As Integer = Convert.ToInt32(hdnRecordId.Value)
        Dim vehicleId As Integer = Convert.ToInt32(hdnVehicleId.Value)
        
        Dim loggedInEmpId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim ownerIdObj As Object = Database.ExecuteScalar("SELECT EmployeeId FROM Vehicles WHERE Id = @VehId", New SQLiteParameter("@VehId", vehicleId))
        If ownerIdObj Is Nothing OrElse Convert.ToInt32(ownerIdObj) <> loggedInEmpId Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Access Denied: Only the vehicle owner can renew this document.');", True)
            Return
        End If

        Dim docNumber As String = txtDocNumber.Text.Trim()
        Dim authority As String = txtAuthority.Text.Trim()
        Dim issueDate As String = txtIssueDate.Text
        Dim expiryDate As String = txtExpiryDate.Text
        Dim remarks As String = txtRemarks.Text.Trim()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim empName As String = Session("EmployeeName").ToString()

        ' File Upload is mandatory
        If Not fileScan.HasFile Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Please upload a scanned certificate copy (PDF required).');", True)
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

            ' Fetch previous dates for DocumentHistory audit log before updating ComplianceRecords
            Dim dtOld As DataTable = Database.ExecuteDataTable("SELECT IssueDate, ExpiryDate, LastUpdatedBy, DocumentId FROM ComplianceRecords WHERE Id = @Id LIMIT 1", New SQLiteParameter("@Id", recordId))
            Dim oldStart As String = ""
            Dim oldExpiry As String = ""
            Dim oldDocId As Object = DBNull.Value

            If dtOld.Rows.Count > 0 Then
                oldStart = If(dtOld.Rows(0)("IssueDate") Is DBNull.Value, "", dtOld.Rows(0)("IssueDate").ToString())
                oldExpiry = If(dtOld.Rows(0)("ExpiryDate") Is DBNull.Value, "", dtOld.Rows(0)("ExpiryDate").ToString())
                If dtOld.Rows(0)("DocumentId") IsNot DBNull.Value Then
                    oldDocId = dtOld.Rows(0)("DocumentId")
                End If
            End If

            ' Insert Document History log
            Dim historySql As String = "INSERT INTO DocumentHistory (VehicleId, DocumentType, OldStartDate, OldExpiryDate, NewStartDate, NewExpiryDate, ChangedBy, ChangedOn, Remarks) " &
                                       "VALUES (@VehId, @DocType, @OldStart, @OldExpiry, @NewStart, @NewExpiry, @ChangedBy, datetime('now'), @Remarks)"
            Database.ExecuteNonQuery(historySql,
                New SQLiteParameter("@VehId", vehicleId),
                New SQLiteParameter("@DocType", txtDocType.Text),
                New SQLiteParameter("@OldStart", If(String.IsNullOrEmpty(oldStart), DBNull.Value, oldStart)),
                New SQLiteParameter("@OldExpiry", If(String.IsNullOrEmpty(oldExpiry), DBNull.Value, oldExpiry)),
                New SQLiteParameter("@NewStart", issueDate),
                New SQLiteParameter("@NewExpiry", expiryDate),
                New SQLiteParameter("@ChangedBy", empName),
                New SQLiteParameter("@Remarks", remarks)
            )

            ' Update compliance record details
            Dim calculatedStatus As String = Compliance.CalculateStatus(txtDocType.Text, expiryDate)
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
            
            Dim dept As String = Database.ExecuteScalar("SELECT Department FROM Vehicles WHERE Id = " & vehicleId).ToString()

            Dim sqlNotif As String = "INSERT INTO Notifications (VehicleId, Department, Title, Message, Status, Type, CreatedAt) VALUES (" & vehicleId & ", @Dept, @Title, @Msg, 'UNREAD', 'RENEWAL', datetime('now'));"
            Database.ExecuteNonQuery(sqlNotif, New SQLiteParameter("@Dept", dept), New SQLiteParameter("@Title", notifTitle), New SQLiteParameter("@Msg", notifMsg))

            EmailService.NotifySuperAdminsOfRenewal(empName, plateNo, docType)

            ' Refresh status
            Compliance.UpdateVehicleStatus(vehicleId)

            ' Log Audit
            Dim sqlAudit As String = "INSERT INTO AuditLogs (UserId, Username, Action, Description, OldValue, NewValue, IpAddress, Timestamp, VehicleId, Department) VALUES (" & empId & ", @User, 'DOCUMENT_RENEWAL', 'Uploaded and renewed certificate for " & docType & " of vehicle " & plateNo & ". Pending verification.', @OldExp, @NewExp, @IP, datetime('now'), " & vehicleId & ", @Dept);"
            Database.ExecuteNonQuery(sqlAudit, New SQLiteParameter("@User", empName), New SQLiteParameter("@OldExp", oldExpiry), New SQLiteParameter("@NewExp", expiryDate), New SQLiteParameter("@IP", Request.UserHostAddress), New SQLiteParameter("@Dept", dept))

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
            Case "Expired"
                Return "bg-red-50 text-red-700 border border-red-200"
            Case "Expiring"
                Return "bg-orange-50 text-orange-700 border border-orange-200"
            Case Else
                Return "bg-emerald-50 text-emerald-700 border border-emerald-200"
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

    ' ─── Bulk Renewal Handler ──────────────────────────────────────────────────
    Protected Sub btnBulkRenew_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim bulkIdsRaw As String = Request.Form("hdnBulkSelectedIds")
        If String.IsNullOrEmpty(bulkIdsRaw) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('No documents selected for bulk renewal.');", True)
            Return
        End If

        Dim remarks As String = If(Request.Form("txtBulkRemarks"), "Bulk renewal batch")
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim empName As String = Session("EmployeeName").ToString()

        ' Parse selected record IDs
        Dim selectedIds As New List(Of Integer)()
        For Each part As String In bulkIdsRaw.Split(","c)
            Dim id As Integer = 0
            If Integer.TryParse(part.Trim(), id) AndAlso id > 0 Then
                selectedIds.Add(id)
            End If
        Next

        If selectedIds.Count = 0 Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('No valid document IDs found.');", True)
            Return
        End If

        ' Validate ownership for all records
        Dim loggedInEmpId As Integer = Convert.ToInt32(Session("EmployeeId"))
        For Each recordId As Integer In selectedIds
            Dim ownerIdObj As Object = Database.ExecuteScalar(
                "SELECT v.EmployeeId FROM ComplianceRecords r INNER JOIN Vehicles v ON r.VehicleId = v.Id WHERE r.Id = @Id",
                New SQLiteParameter("@Id", recordId))
            If ownerIdObj Is Nothing OrElse Convert.ToInt32(ownerIdObj) <> loggedInEmpId Then
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Access Denied: You can only renew vehicles you registered.');", True)
                Return
            End If
        Next

        ' Validate that every selected record has required per-document fields filled
        For Each recordId As Integer In selectedIds
            Dim docNo As String = Request.Form("docNumber_" & recordId)
            Dim auth As String = Request.Form("authority_" & recordId)
            Dim issD As String = Request.Form("issueDate_" & recordId)
            Dim expD As String = Request.Form("expiryDate_" & recordId)
            If String.IsNullOrEmpty(docNo) OrElse String.IsNullOrEmpty(auth) OrElse String.IsNullOrEmpty(issD) OrElse String.IsNullOrEmpty(expD) Then
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert",
                    "alert('Please fill Document Number, Issuing Authority, Issue Date and Expiry Date for every document (record #" & recordId & ").');", True)
                Return
            End If
            Dim issDt, expDt As DateTime
            If Not DateTime.TryParse(issD, issDt) OrElse Not DateTime.TryParse(expD, expDt) Then
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert",
                    "alert('Invalid date values for document record #" & recordId & ".');", True)
                Return
            End If
            If expDt <= issDt Then
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert",
                    "alert('Expiry date must be after issue date for document record #" & recordId & ".');", True)
                Return
            End If

            Dim uploadedFile As System.Web.HttpPostedFile = Request.Files("docFile_" & recordId)
            If uploadedFile Is Nothing OrElse uploadedFile.ContentLength = 0 Then
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert",
                    "alert('Please upload a PDF document copy for document record #" & recordId & ".');", True)
                Return
            End If
            Dim fileExt As String = System.IO.Path.GetExtension(uploadedFile.FileName).ToLower()
            If fileExt <> ".pdf" Then
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert",
                    "alert('Only PDF files are accepted. Please check the file for document record #" & recordId & ".');", True)
                Return
            End If
        Next

        Dim renewedCount As Integer = 0

        For Each recordId As Integer In selectedIds
            Try
                Dim docNumber As String     = Request.Form("docNumber_" & recordId).Trim()
                Dim authority As String     = Request.Form("authority_" & recordId)
                Dim issueDateStr As String  = Request.Form("issueDate_" & recordId)
                Dim expiryDateStr As String = Request.Form("expiryDate_" & recordId)

                ' Handle individual PDF upload for this record
                Dim newDocId As Object = DBNull.Value
                Dim uploadedFile As System.Web.HttpPostedFile = Request.Files("docFile_" & recordId)
                If uploadedFile IsNot Nothing AndAlso uploadedFile.ContentLength > 0 Then
                    Dim uploadFolder As String = Server.MapPath("~/uploads")
                    If Not System.IO.Directory.Exists(uploadFolder) Then System.IO.Directory.CreateDirectory(uploadFolder)
                    Dim ts As String = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
                    Dim savedName As String = ts & "_REC" & recordId & "_" & System.IO.Path.GetFileName(uploadedFile.FileName)
                    Dim physPath As String = System.IO.Path.Combine(uploadFolder, savedName)
                    uploadedFile.SaveAs(physPath)
                    Dim relPath As String = "/uploads/" & savedName
                    Database.ExecuteNonQuery(
                        "INSERT INTO Documents (FileName, FilePath, FileType, FileSize, UploadedBy, CreatedAt) VALUES (@FName, @FPath, 'application/pdf', @FSize, @EmpId, datetime('now'));",
                        New SQLiteParameter("@FName", savedName),
                        New SQLiteParameter("@FPath", relPath),
                        New SQLiteParameter("@FSize", uploadedFile.ContentLength),
                        New SQLiteParameter("@EmpId", empId))
                    newDocId = Convert.ToInt32(Database.ExecuteScalar(
                        "SELECT Id FROM Documents WHERE FilePath=@FPath ORDER BY Id DESC LIMIT 1",
                        New SQLiteParameter("@FPath", relPath)))
                End If

                Dim dtOld As DataTable = Database.ExecuteDataTable(
                    "SELECT VehicleId, LicenseType, IssueDate, ExpiryDate, DocumentId FROM ComplianceRecords WHERE Id = @Id LIMIT 1",
                    New SQLiteParameter("@Id", recordId))
                If dtOld.Rows.Count = 0 Then Continue For

                Dim row As DataRow = dtOld.Rows(0)
                Dim vehicleId As Integer = Convert.ToInt32(row("VehicleId"))
                Dim licType As String = row("LicenseType").ToString()
                Dim oldStart As String = If(row("IssueDate") Is DBNull.Value, "", row("IssueDate").ToString())
                Dim oldExpiry As String = row("ExpiryDate").ToString()
                Dim oldDocId As Object = If(row("DocumentId") Is DBNull.Value, CType(DBNull.Value, Object), row("DocumentId"))

                ' Preserve existing document if no new file uploaded for this record (though validator guarantees it is)
                If newDocId Is DBNull.Value Then newDocId = oldDocId

                ' Insert Document History log
                Dim historySql As String = "INSERT INTO DocumentHistory (VehicleId, DocumentType, OldStartDate, OldExpiryDate, NewStartDate, NewExpiryDate, ChangedBy, ChangedOn, Remarks) " &
                                           "VALUES (@VehId, @DocType, @OldStart, @OldExpiry, @NewStart, @NewExpiry, @ChangedBy, datetime('now'), @Remarks)"
                Database.ExecuteNonQuery(historySql,
                    New SQLiteParameter("@VehId", vehicleId),
                    New SQLiteParameter("@DocType", licType),
                    New SQLiteParameter("@OldStart", If(String.IsNullOrEmpty(oldStart), DBNull.Value, oldStart)),
                    New SQLiteParameter("@OldExpiry", If(String.IsNullOrEmpty(oldExpiry), DBNull.Value, oldExpiry)),
                    New SQLiteParameter("@NewStart", issueDateStr),
                    New SQLiteParameter("@NewExpiry", expiryDateStr),
                    New SQLiteParameter("@ChangedBy", empName),
                    New SQLiteParameter("@Remarks", remarks)
                )

                Dim calculatedStatus As String = Compliance.CalculateStatus(licType, expiryDateStr)

                Database.ExecuteNonQuery(
                    "UPDATE ComplianceRecords SET LicenseNumber = @Number, IssuingAuthority = @Auth, IssueDate = @IssueDate, ExpiryDate = @ExpiryDate, Status = @Status, DocumentId = @DocId, IsVerified = 0, VerifiedBy = NULL, LastUpdatedBy = @User, LastUpdatedTimestamp = datetime('now'), UpdatedAt = datetime('now') WHERE Id = @Id",
                    New SQLiteParameter("@Number", docNumber),
                    New SQLiteParameter("@Auth", authority),
                    New SQLiteParameter("@IssueDate", issueDateStr),
                    New SQLiteParameter("@ExpiryDate", expiryDateStr),
                    New SQLiteParameter("@Status", calculatedStatus),
                    New SQLiteParameter("@DocId", newDocId),
                    New SQLiteParameter("@User", empName),
                    New SQLiteParameter("@Id", recordId))

                Database.ExecuteNonQuery(
                    "INSERT INTO RenewalHistories (VehicleId, ComplianceRecordId, LicenseType, OldExpiryDate, NewExpiryDate, OldDocumentId, NewDocumentId, RenewedBy, RenewedAt, Remarks) VALUES (@VehId, @RecId, @Type, @OldExp, @NewExp, @OldDoc, @NewDoc, @EmpId, datetime('now'), @Remarks)",
                    New SQLiteParameter("@VehId", vehicleId),
                    New SQLiteParameter("@RecId", recordId),
                    New SQLiteParameter("@Type", licType),
                    New SQLiteParameter("@OldExp", If(String.IsNullOrEmpty(oldExpiry), CType(DBNull.Value, Object), oldExpiry)),
                    New SQLiteParameter("@NewExp", expiryDateStr),
                    New SQLiteParameter("@OldDoc", oldDocId),
                    New SQLiteParameter("@NewDoc", newDocId),
                    New SQLiteParameter("@EmpId", empId),
                    New SQLiteParameter("@Remarks", If(String.IsNullOrEmpty(remarks), "Bulk renewal batch", remarks)))

                Compliance.UpdateVehicleStatus(vehicleId)
                renewedCount += 1

                Dim dept As String = Database.ExecuteScalar("SELECT Department FROM Vehicles WHERE Id = " & vehicleId).ToString()
                Dim plateNo As String = Database.ExecuteScalar("SELECT VehicleNumber FROM Vehicles WHERE Id = " & vehicleId).ToString()
                Database.ExecuteNonQuery(
                    "INSERT INTO AuditLogs (UserId, Username, Action, Description, OldValue, NewValue, IpAddress, Timestamp, VehicleId, Department) VALUES (" & empId & ", @User, 'BULK_DOCUMENT_RENEWAL', 'Bulk renewed " & licType & " for vehicle " & plateNo & ".', @OldExp, @NewExp, @IP, datetime('now'), " & vehicleId & ", @Dept);",
                    New SQLiteParameter("@User", empName),
                    New SQLiteParameter("@OldExp", oldExpiry),
                    New SQLiteParameter("@NewExp", expiryDateStr),
                    New SQLiteParameter("@IP", Request.UserHostAddress),
                    New SQLiteParameter("@Dept", dept))
            Catch ex As Exception
                Console.WriteLine("[BulkRenew] Error renewing record " & recordId & ": " & ex.Message)
            End Try
        Next

        Try
            EmailService.NotifySuperAdminsOfRenewal(empName, "[BULK - " & renewedCount & " docs]", "Multiple certificate types")
        Catch
        End Try

        LoadAlerts()
        ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Bulk renewal submitted for " & renewedCount & " document(s). Pending verification.');", True)
    End Sub
End Class
