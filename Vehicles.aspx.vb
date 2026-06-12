Imports System
Imports System.Data
Imports System.Data.SQLite
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports System.Collections.Generic
Imports System.IO
Imports QRCoder

Public Class VehiclesPage
    Inherits Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Session("EmployeeId") Is Nothing Then
            Response.Redirect("~/Login.aspx")
            Return
        End If

        If Not IsPostBack Then
            LoadFilterDepartments()
            LoadVehicles()
        End If
    End Sub

    Private Sub LoadFilterDepartments()
        Dim dt As DataTable = Database.ExecuteDataTable("SELECT Id, Code, Name FROM Departments ORDER BY Code")
        
        ' Filter Ddl
        ddlDeptFilter.Items.Clear()
        ddlDeptFilter.Items.Add(New ListItem("All Divisions", ""))
        For Each row As DataRow In dt.Rows
            ddlDeptFilter.Items.Add(New ListItem(row("Code").ToString() & " - " & row("Name").ToString(), row("Id").ToString()))
        Next

        ' Modal Ddl - departments removed, nothing to populate
    End Sub

    Private Sub LoadVehicles()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Dim sql As String = "SELECT v.Id, v.VehicleNumber, v.VehicleType, v.DriverName, v.VendorName, v.OverallStatus, " &
                           "d.Code As DeptCode, d.Name As DeptName " &
                           "FROM Vehicles v " &
                           "INNER JOIN Departments d ON v.DepartmentId = d.Id"
        
        Dim whereClauses As New List(Of String)()
        Dim parameters As New List(Of SQLiteParameter)()

        ' Employees see only self uploads
        If role = "Employee" Then
            whereClauses.Add("v.EmployeeId = @EmpId")
            parameters.Add(New SQLiteParameter("@EmpId", empId))
        End If

        ' Search query
        If Not String.IsNullOrEmpty(txtSearch.Text.Trim()) Then
            whereClauses.Add("(v.VehicleNumber LIKE @Search OR v.DriverName LIKE @Search OR v.VendorName LIKE @Search)")
            parameters.Add(New SQLiteParameter("@Search", "%" & txtSearch.Text.Trim() & "%"))
        End If

        ' Department filter
        If Not String.IsNullOrEmpty(ddlDeptFilter.SelectedValue) Then
            whereClauses.Add("v.DepartmentId = @DeptId")
            parameters.Add(New SQLiteParameter("@DeptId", Convert.ToInt32(ddlDeptFilter.SelectedValue)))
        End If

        ' Status filter
        If Not String.IsNullOrEmpty(ddlStatusFilter.SelectedValue) Then
            whereClauses.Add("v.OverallStatus = @Status")
            parameters.Add(New SQLiteParameter("@Status", ddlStatusFilter.SelectedValue))
        End If

        If whereClauses.Count > 0 Then
            sql &= " WHERE " & String.Join(" AND ", whereClauses.ToArray())
        End If

        sql &= " ORDER BY v.VehicleNumber"

        Dim dt As DataTable = Database.ExecuteDataTable(sql, parameters.ToArray())
        rptVehicles.DataSource = dt
        rptVehicles.DataBind()
    End Sub

    Protected Sub FilterVehicles(ByVal sender As Object, ByVal e As EventArgs)
        LoadVehicles()
        pnlDetails.Visible = False
        pnlNoDetails.Visible = True
    End Sub

    Protected Sub btnReset_Click(ByVal sender As Object, ByVal e As EventArgs)
        txtSearch.Text = ""
        ddlDeptFilter.SelectedIndex = 0
        ddlStatusFilter.SelectedIndex = 0
        LoadVehicles()
        pnlDetails.Visible = False
        pnlNoDetails.Visible = True
    End Sub

    Protected Sub rptVehicles_ItemCommand(ByVal source As Object, ByVal e As RepeaterCommandEventArgs)
        Dim vehicleId As Integer = Convert.ToInt32(e.CommandArgument)

        If e.CommandName = "ViewDetails" Then
            ViewState("SelectedVehicleId") = vehicleId
            LoadVehicleDetails(vehicleId)
        ElseIf e.CommandName = "ShowQr" Then
            LoadQrModal(vehicleId)
        ElseIf e.CommandName = "DeleteVehicle" Then
            DecommissionVehicle(vehicleId)
        End If
    End Sub

    Private Sub LoadVehicleDetails(ByVal vehicleId As Integer)
        Dim sql As String = "SELECT v.*, d.Name As DeptName, e.EmployeeName As CreatorName FROM Vehicles v " &
                           "INNER JOIN Departments d ON v.DepartmentId = d.Id " &
                           "INNER JOIN Employee e ON v.EmployeeId = e.EmployeeId " &
                           "WHERE v.Id = @VehId LIMIT 1"
        
        Dim dt As DataTable = Database.ExecuteDataTable(sql, New SQLiteParameter("@VehId", vehicleId))
        If dt.Rows.Count = 0 Then Return

        Dim row As DataRow = dt.Rows(0)
        lblPlateNumber.Text = row("VehicleNumber").ToString()
        lblType.Text = row("VehicleType").ToString()
        lblDriver.Text = If(row("DriverName") Is DBNull.Value OrElse String.IsNullOrEmpty(row("DriverName").ToString()), "N/A", row("DriverName").ToString())
        lblVendor.Text = If(row("VendorName") Is DBNull.Value OrElse String.IsNullOrEmpty(row("VendorName").ToString()), "N/A", row("VendorName").ToString())
        lblCreator.Text = row("CreatorName").ToString()

        ' Verify state badge
        Dim isVerified As Boolean = Convert.ToBoolean(row("IsVerified"))
        If isVerified Then
            lblVerifiedBadge.Text = "Verified & Approved"
            lblVerifiedBadge.CssClass = "rounded-full px-2.5 py-0.5 text-[9px] font-extrabold uppercase bg-emerald-100 text-emerald-700"
            btnVerifyVehicle.Visible = False
        Else
            lblVerifiedBadge.Text = "Pending Verification"
            lblVerifiedBadge.CssClass = "rounded-full px-2.5 py-0.5 text-[9px] font-extrabold uppercase bg-amber-100 text-amber-700"
            btnVerifyVehicle.Visible = (Session("Role").ToString() = "SuperAdmin")
        End If

        ' Decommission permission
        Dim creatorId As Integer = Convert.ToInt32(row("EmployeeId"))
        Dim loggedInEmpId As Integer = Convert.ToInt32(Session("EmployeeId"))
        btnDecommission.Visible = (Session("Role").ToString() = "SuperAdmin" OrElse creatorId = loggedInEmpId)

        ' QR Code Windshield
        Dim url As String = Request.Url.GetLeftPart(UriPartial.Authority) & "/Verify.aspx?plate=" & Server.UrlEncode(row("VehicleNumber").ToString())
        lnkPrintGatePass.NavigateUrl = "~/Gate.aspx?print=1&plate=" & Server.UrlEncode(row("VehicleNumber").ToString())
        
        Try
            Using qrGen As New QRCodeGenerator()
                Dim qrData As QRCodeData = qrGen.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q)
                Using qrCode As New PngByteQRCode(qrData)
                    Dim base64 As String = Convert.ToBase64String(qrCode.GetGraphic(8))
                    imgQrCode.ImageUrl = "data:image/png;base64," & base64
                End Using
            End Using
        Catch
            imgQrCode.ImageUrl = ""
        End Try

        ' Load compliance checklist records
        Dim dtSlots As DataTable = Database.ExecuteDataTable("SELECT Id, VehicleId, LicenseType, LicenseNumber, ExpiryDate, Status FROM ComplianceRecords WHERE VehicleId = @VehId ORDER BY LicenseType", New SQLiteParameter("@VehId", vehicleId))
        rptComplianceSlots.DataSource = dtSlots
        rptComplianceSlots.DataBind()

        pnlDetails.Visible = True
        pnlNoDetails.Visible = False
    End Sub

    Private Sub LoadQrModal(ByVal vehicleId As Integer)
        Dim sql As String = "SELECT VehicleNumber, VehicleType, Id FROM Vehicles WHERE Id = @Id"
        Dim dt As DataTable = Database.ExecuteDataTable(sql, New SQLiteParameter("@Id", vehicleId))
        If dt.Rows.Count = 0 Then Return

        Dim row As DataRow = dt.Rows(0)
        Dim plate As String = row("VehicleNumber").ToString()
        lblModalPlateNumber.Text = plate
        lblModalType.Text = row("VehicleType").ToString()

        Dim url As String = Request.Url.GetLeftPart(UriPartial.Authority) & "/Verify.aspx?plate=" & Server.UrlEncode(plate)
        lblModalUrl.Text = url

        Try
            Using qrGen As New QRCodeGenerator()
                Dim qrData As QRCodeData = qrGen.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q)
                Using qrCode As New PngByteQRCode(qrData)
                    Dim base64 As String = Convert.ToBase64String(qrCode.GetGraphic(10))
                    imgModalQrCode.ImageUrl = "data:image/png;base64," & base64
                End Using
            End Using
        Catch
            imgModalQrCode.ImageUrl = ""
        End Try

        pnlQrModal.Visible = True
    End Sub

    Protected Sub btnCloseQrModal_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlQrModal.Visible = False
    End Sub

    Protected Sub btnOpenAddModal_Click(ByVal sender As Object, ByVal e As EventArgs)
        ' Only non-SuperAdmin users may register vehicles
        If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('SuperAdmin cannot register vehicles. Only employees may add vehicles.');", True)
            Return
        End If
        txtAddPlate.Text = ""
        txtAddType.Text = ""
        txtAddDriver.Text = ""
        txtAddVendor.Text = ""
        pnlAddModal.Visible = True
    End Sub

    Protected Sub btnCloseAddModal_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlAddModal.Visible = False
    End Sub

    Protected Sub btnSaveVehicle_Click(ByVal sender As Object, ByVal e As EventArgs)
        ' Block SuperAdmin from registering
        If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('SuperAdmin cannot register vehicles.');", True)
            Return
        End If

        Dim plate As String = txtAddPlate.Text.Trim().ToUpper()
        Dim vehicleType As String = txtAddType.Text.Trim()
        Dim driver As String = txtAddDriver.Text.Trim()
        Dim vendor As String = txtAddVendor.Text.Trim()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        If String.IsNullOrEmpty(plate) OrElse String.IsNullOrEmpty(vehicleType) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Plate Number and Vehicle Type are required.');", True)
            Return
        End If

        ' Validate uniqueness
        Dim countObj As Object = Database.ExecuteScalar("SELECT COUNT(*) FROM Vehicles WHERE VehicleNumber=@Plate", New SQLiteParameter("@Plate", plate))
        If Convert.ToInt32(countObj) > 0 Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Vehicle Plate Number is already registered.');", True)
            Return
        End If

        ' DeptId = 0 since departments have been removed
        Dim deptId As Integer = 0

        Try
            Dim sqlInsert As String = "INSERT INTO Vehicles (VehicleNumber, VehicleType, DepartmentId, DriverName, VendorName, QrCodeUrl, OverallStatus, IsVerified, EmployeeId, CreatedAt, UpdatedAt) " &
                                      "VALUES (@Plate, @Type, @DeptId, @Driver, @Vendor, '', 'PENDING', 0, @EmpId, datetime('now'), datetime('now'));"
            Database.ExecuteNonQuery(sqlInsert,
                New SQLiteParameter("@Plate", plate),
                New SQLiteParameter("@Type", vehicleType),
                New SQLiteParameter("@DeptId", deptId),
                New SQLiteParameter("@Driver", driver),
                New SQLiteParameter("@Vendor", vendor),
                New SQLiteParameter("@EmpId", empId)
            )

            Dim newVehId As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT Id FROM Vehicles WHERE VehicleNumber=@Plate", New SQLiteParameter("@Plate", plate)))
            Dim qrUrl As String = "/Verify.aspx?plate=" & Server.UrlEncode(plate)
            Database.ExecuteNonQuery("UPDATE Vehicles SET QrCodeUrl=@Url WHERE Id=@Id", New SQLiteParameter("@Url", qrUrl), New SQLiteParameter("@Id", newVehId))

            ' Upload directory for PDFs
            Dim uploadDir As String = Server.MapPath("~/App_Data/Uploads/" & newVehId.ToString() & "/")
            If Not Directory.Exists(uploadDir) Then Directory.CreateDirectory(uploadDir)

            Dim docTypes() As String = {"ROAD_PERMIT", "AGE_DETERMINATION", "PUC", "FITNESS", "EXPLOSIVE", "GREEN_CARD", "INSURANCE", "CALIBRATION"}

            For Each docType As String In docTypes
                ' Read dates from raw form POST (plain HTML inputs, not ASP controls)
                Dim issueDateStr As String = If(Request.Form("issueDate_" & docType), "")
                Dim expiryDateStr As String = If(Request.Form("expiryDate_" & docType), "")

                Dim issueDate As Object = DBNull.Value
                Dim expiryDate As Object = DBNull.Value
                Dim docStatus As String = "PENDING"

                Dim parsedIssue As DateTime
                Dim parsedExpiry As DateTime

                If DateTime.TryParse(issueDateStr, parsedIssue) Then
                    issueDate = parsedIssue.ToString("yyyy-MM-dd")
                End If

                If DateTime.TryParse(expiryDateStr, parsedExpiry) Then
                    expiryDate = parsedExpiry.ToString("yyyy-MM-dd")
                    Dim daysLeft As Integer = (parsedExpiry - DateTime.Today).Days
                    If daysLeft < 0 Then
                        docStatus = "EXPIRED"
                    ElseIf daysLeft <= 30 Then
                        docStatus = "CRITICAL"
                    ElseIf daysLeft <= 60 Then
                        docStatus = "WARNING"
                    Else
                        docStatus = "ACTIVE"
                    End If
                End If

                ' Insert compliance record
                Database.ExecuteNonQuery(
                    "INSERT INTO ComplianceRecords (VehicleId, LicenseType, LicenseNumber, IssuingAuthority, IssueDate, ExpiryDate, Status, IsVerified, CreatedAt, UpdatedAt) " &
                    "VALUES (@VId, @LType, NULL, NULL, @IDate, @EDate, @Status, 0, datetime('now'), datetime('now'));",
                    New SQLiteParameter("@VId", newVehId),
                    New SQLiteParameter("@LType", docType),
                    New SQLiteParameter("@IDate", issueDate),
                    New SQLiteParameter("@EDate", expiryDate),
                    New SQLiteParameter("@Status", docStatus)
                )

                ' Handle PDF upload if provided
                Dim fileKey As String = "docFile_" & docType
                Dim postedFile As HttpPostedFile = Request.Files(fileKey)
                If postedFile IsNot Nothing AndAlso postedFile.ContentLength > 0 Then
                    Dim safeName As String = docType & "_" & Path.GetFileName(postedFile.FileName).Replace(" ", "_")
                    Dim savePath As String = Path.Combine(uploadDir, safeName)
                    postedFile.SaveAs(savePath)

                    ' Get the compliance record Id just inserted
                    Dim recId As Object = Database.ExecuteScalar(
                        "SELECT Id FROM ComplianceRecords WHERE VehicleId=@VId AND LicenseType=@LType ORDER BY Id DESC LIMIT 1",
                        New SQLiteParameter("@VId", newVehId),
                        New SQLiteParameter("@LType", docType)
                    )

                    If recId IsNot Nothing AndAlso Not Convert.IsDBNull(recId) Then
                        Dim relPath As String = "~/App_Data/Uploads/" & newVehId.ToString() & "/" & safeName
                        Database.ExecuteNonQuery(
                            "INSERT INTO Documents (VehicleId, ComplianceRecordId, DocumentType, FileName, FilePath, UploadedBy, UploadedAt) " &
                            "VALUES (@VId, @RecId, @DocType, @FName, @FPath, @UpBy, datetime('now'));",
                            New SQLiteParameter("@VId", newVehId),
                            New SQLiteParameter("@RecId", Convert.ToInt32(recId)),
                            New SQLiteParameter("@DocType", docType),
                            New SQLiteParameter("@FName", safeName),
                            New SQLiteParameter("@FPath", relPath),
                            New SQLiteParameter("@UpBy", empId)
                        )
                    End If
                End If
            Next

            ' Update overall vehicle status
            Compliance.UpdateVehicleStatus(newVehId)

            ' Audit log
            Dim username As String = Session("EmployeeName").ToString()
            Database.ExecuteNonQuery(
                "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp, VehicleId) " &
                "VALUES (@EmpId, @User, 'VEHICLE_REGISTRATION', 'Registered vehicle ' || @Plate || ' with compliance documents.', @IP, datetime('now'), @VId);",
                New SQLiteParameter("@EmpId", empId),
                New SQLiteParameter("@User", username),
                New SQLiteParameter("@Plate", plate),
                New SQLiteParameter("@IP", Request.UserHostAddress),
                New SQLiteParameter("@VId", newVehId)
            )

            ' Notify SuperAdmins of new vehicle registration
            EmailService.NotifySuperAdminsOfNewVehicle(empId, plate)

            pnlAddModal.Visible = False
            LoadVehicles()
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Vehicle registered successfully!');", True)

        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Registration failed: " & Server.HtmlEncode(ex.Message).Replace("'", "&#39;") & "');", True)
        End Try
    End Sub

    Protected Sub btnVerifyVehicle_Click(ByVal sender As Object, ByVal e As EventArgs)
        If ViewState("SelectedVehicleId") Is Nothing Then Return
        Dim vehicleId As Integer = Convert.ToInt32(ViewState("SelectedVehicleId"))
        Dim username As String = Session("EmployeeName").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Try
            ' Verify vehicle registration RCs
            Database.ExecuteNonQuery("UPDATE Vehicles SET IsVerified = 1, VerifiedBy = @Verifier, UpdatedAt = datetime('now') WHERE Id = @Id", New SQLiteParameter("@Verifier", username), New SQLiteParameter("@Id", vehicleId))
            
            ' Auto-verify compliance items
            Database.ExecuteNonQuery("UPDATE ComplianceRecords SET IsVerified = 1, VerifiedBy = @Verifier, UpdatedAt = datetime('now') WHERE VehicleId = @Id", New SQLiteParameter("@Verifier", username), New SQLiteParameter("@Id", vehicleId))
            
            Compliance.UpdateVehicleStatus(vehicleId)

            ' Notify owner via email
            Dim dtCreator As DataTable = Database.ExecuteDataTable("SELECT EmployeeId, VehicleNumber FROM Vehicles WHERE Id = @Id LIMIT 1", New SQLiteParameter("@Id", vehicleId))
            If dtCreator.Rows.Count > 0 Then
                Dim creatorId As Integer = Convert.ToInt32(dtCreator.Rows(0)("EmployeeId"))
                Dim plateNo As String = dtCreator.Rows(0)("VehicleNumber").ToString()
                EmailService.NotifyEmployeeOfApproval(creatorId, plateNo)
            End If

            ' Log Audit Trail
            Database.ExecuteNonQuery("INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp, VehicleId) VALUES (" & empId & ", @User, 'VEHICLE_VERIFICATION', 'Approved vehicle details verification pass.', @IP, datetime('now'), " & vehicleId & ");", New SQLiteParameter("@User", username), New SQLiteParameter("@IP", Request.UserHostAddress))

            LoadVehicleDetails(vehicleId)
            LoadVehicles()
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Vehicle compliance pass verified successfully!');", True)

        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Verification approval failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    Protected Sub btnDecommission_Click(ByVal sender As Object, ByVal e As EventArgs)
        If ViewState("SelectedVehicleId") Is Nothing Then Return
        Dim vehicleId As Integer = Convert.ToInt32(ViewState("SelectedVehicleId"))
        DecommissionVehicle(vehicleId)
    End Sub

    Private Sub DecommissionVehicle(ByVal vehicleId As Integer)
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim username As String = Session("EmployeeName").ToString()

        Try
            Dim plate As String = Database.ExecuteScalar("SELECT VehicleNumber FROM Vehicles WHERE Id = " & vehicleId).ToString()
            
            ' Cascade deletes vehicles + compliance records
            Database.ExecuteNonQuery("DELETE FROM Vehicles WHERE Id = " & vehicleId)

            ' Log decommission action
            Database.ExecuteNonQuery("INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & empId & ", @User, 'VEHICLE_DECOMMISSION', 'Permanently decommissioned vehicle ' || @Plate || ' and compliance checklists.', @IP, datetime('now'));", New SQLiteParameter("@User", username), New SQLiteParameter("@Plate", plate), New SQLiteParameter("@IP", Request.UserHostAddress))

            pnlDetails.Visible = False
            pnlNoDetails.Visible = True
            LoadVehicles()
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Vehicle decommissioned successfully and records purged.');", True)

        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Decommission failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    ' ── Visual Helper Functions ──

    Public Function GetHeaderBg(ByVal status As Object) As String
        If status Is Nothing Then Return "bg-slate-600"
        Dim s As String = status.ToString()
        Select Case s
            Case "FULLY_COMPLIANT", "ACTIVE"
                Return "bg-emerald-600"
            Case "WARNING"
                Return "bg-yellow-600"
            Case "CRITICAL"
                Return "bg-orange-600"
            Case "EXPIRED"
                Return "bg-red-600"
            Case Else
                Return "bg-slate-600"
        End Select
    End Function

    Public Function GetStatusBadgeClass(ByVal statusObj As Object) As String
        If statusObj Is Nothing Then Return "bg-slate-100 text-slate-700 border-slate-200"
        Dim status As String = statusObj.ToString()
        Select Case status
            Case "FULLY_COMPLIANT", "ACTIVE"
                Return "bg-emerald-50 text-emerald-700 border-emerald-250"
            Case "WARNING"
                Return "bg-yellow-50 text-yellow-750 border-yellow-250"
            Case "CRITICAL", "MEDIUM_CRITICAL", "HIGH_CRITICAL"
                Return "bg-orange-50 text-orange-700 border-orange-200"
            Case "EXPIRED"
                Return "bg-red-50 text-red-700 border-red-200"
            Case Else
                Return "bg-slate-50 text-slate-600 border-slate-200"
        End Select
    End Function

    Public Function GetDotColor(ByVal status As String) As String
        Select Case status
            Case "FULLY_COMPLIANT", "ACTIVE"
                Return "bg-emerald-500"
            Case "WARNING"
                Return "bg-yellow-500"
            Case "CRITICAL", "MEDIUM_CRITICAL", "HIGH_CRITICAL"
                Return "bg-orange-500"
            Case "EXPIRED"
                Return "bg-red-500"
            Case Else
                Return "bg-slate-400"
        End Select
    End Function

    Public Function GetComplianceSlotsHtml(ByVal vehicleId As Integer) As String
        Dim sql As String = "SELECT LicenseType, Status FROM ComplianceRecords WHERE VehicleId = @VehId ORDER BY LicenseType"
        Dim dt As DataTable = Database.ExecuteDataTable(sql, New SQLiteParameter("@VehId", vehicleId))
        
        Dim sb As New System.Text.StringBuilder()
        For Each row As DataRow In dt.Rows
            Dim type As String = row("LicenseType").ToString()
            Dim status As String = row("Status").ToString()
            
            Dim label As String = type.Replace("_", " ")
            Dim badgeClass As String = GetStatusBadgeClass(status)
            Dim dotClass As String = GetDotColor(status)
            
            sb.Append("<span class=""inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[9px] font-bold border " & badgeClass & """ title=""" & label & ": " & status & """>")
            sb.Append("<span class=""h-1.5 w-1.5 rounded-full " & dotClass & """></span>")
            sb.Append("<span>" & label & "</span>")
            sb.Append("</span> ")
        Next
        Return sb.ToString()
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
End Class
