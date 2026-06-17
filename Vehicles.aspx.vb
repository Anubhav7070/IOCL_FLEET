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
            Response.Redirect("~/Login.aspx", False)
            HttpContext.Current.ApplicationInstance.CompleteRequest()
            Return
        End If

        If Not IsPostBack Then
            LoadFilterDepartments()
            LoadVehicles()
            If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then
                LoadEditDepartments()
            End If
        End If
    End Sub


    ' Populates ddlEditDept for SuperAdmin vehicle edit
    Private Sub LoadEditDepartments()
        Dim dt As DataTable = Database.ExecuteDataTable(
            "SELECT DISTINCT Department As Code FROM Employee WHERE Department IS NOT NULL AND Department <> '' ORDER BY Department")
        If ddlEditDept Is Nothing Then Return
        ddlEditDept.Items.Clear()
        ddlEditDept.Items.Add(New ListItem("-- Select Department --", ""))
        For Each row As DataRow In dt.Rows
            ddlEditDept.Items.Add(New ListItem(row("Code").ToString(), row("Code").ToString()))
        Next
    End Sub

    Private Sub LoadFilterDepartments()
        Dim dt As DataTable = Database.ExecuteDataTable("SELECT DISTINCT Department As Code FROM Employee WHERE Department IS NOT NULL AND Department <> '' ORDER BY Department")
        
        ' Filter Ddl
        ddlDeptFilter.Items.Clear()
        ddlDeptFilter.Items.Add(New ListItem("All Divisions", ""))
        For Each row As DataRow In dt.Rows
            ddlDeptFilter.Items.Add(New ListItem(row("Code").ToString(), row("Code").ToString()))
        Next

        ' Modal Ddl - departments removed, nothing to populate
    End Sub

    Private Sub LoadVehicles()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Dim sql As String = "SELECT v.Id, v.VehicleNumber, v.VehicleType, v.DriverName, v.VendorName, v.OverallStatus, " &
                           "v.Department As DeptCode, v.Department As DeptName, v.OwnershipType " &
                           "FROM Vehicles v"
        
        Dim whereClauses As New List(Of String)()
        Dim parameters As New List(Of SQLiteParameter)()

        ' Employees see only self uploads
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

        ' Search query
        If Not String.IsNullOrEmpty(txtSearch.Text.Trim()) Then
            whereClauses.Add("(v.VehicleNumber LIKE @Search OR v.DriverName LIKE @Search OR v.VendorName LIKE @Search)")
            parameters.Add(New SQLiteParameter("@Search", "%" & txtSearch.Text.Trim() & "%"))
        End If

        ' Department filter
        If Not String.IsNullOrEmpty(ddlDeptFilter.SelectedValue) Then
            whereClauses.Add("v.Department = @Dept")
            parameters.Add(New SQLiteParameter("@Dept", ddlDeptFilter.SelectedValue))
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
        Dim sql As String = "SELECT v.*, v.Department As DeptName, e.EmployeeName As CreatorName, e.EmpNumber As CreatorNumber FROM Vehicles v " &
                           "INNER JOIN Employee e ON v.EmployeeId = e.EmployeeId " &
                           "WHERE v.Id = @VehId LIMIT 1"
        
        Dim dt As DataTable = Database.ExecuteDataTable(sql, New SQLiteParameter("@VehId", vehicleId))
        If dt.Rows.Count = 0 Then Return

        Dim row As DataRow = dt.Rows(0)
        lblPlateNumber.Text = row("VehicleNumber").ToString()
        lblType.Text = row("VehicleType").ToString()
        lblCreator.Text = row("CreatorName").ToString()

        Dim ownershipType As String = If(row("OwnershipType") Is DBNull.Value OrElse String.IsNullOrEmpty(row("OwnershipType").ToString()), "Contractual", row("OwnershipType").ToString())
        lblOwnership.Text = If(ownershipType = "Personal", "Personal (Car)", "Contractual")

        If ownershipType = "Personal" Then
            rowDriver.Visible = False
            rowVendor.Visible = False
            rowEmpNumber.Visible = True
            rowEmpName.Visible = True
            lblEmpNumber.Text = row("CreatorNumber").ToString()
            lblEmpName.Text = row("CreatorName").ToString()
        Else
            rowDriver.Visible = True
            rowVendor.Visible = True
            rowEmpNumber.Visible = False
            rowEmpName.Visible = False
            lblDriver.Text = If(row("DriverName") Is DBNull.Value OrElse String.IsNullOrEmpty(row("DriverName").ToString()), "N/A", row("DriverName").ToString())
            lblVendor.Text = If(row("VendorName") Is DBNull.Value OrElse String.IsNullOrEmpty(row("VendorName").ToString()), "N/A", row("VendorName").ToString())
        End If

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
        btnOpenEdit.Visible = (Session("Role").ToString() = "SuperAdmin")

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
        txtAddPlate.Text = ""
        txtAddType.Text = ""
        txtAddDriver.Text = ""
        txtAddVendor.Text = ""
        pnlAddModal.Visible = True
        ClientScript.RegisterStartupScript(Me.GetType(), "ToggleModalUI", "setTimeout(toggleOwnershipType, 100);", True)
    End Sub

    Protected Sub btnCloseAddModal_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlAddModal.Visible = False
    End Sub

    Protected Sub btnSaveVehicle_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim role As String = Session("Role").ToString()

        Dim ownershipType As String = If(Request.Form("ddlAddOwnershipType") IsNot Nothing, Request.Form("ddlAddOwnershipType").ToString(), "Contractual")
        Dim plate As String = txtAddPlate.Text.Trim().ToUpper()
        Dim vehicleType As String = txtAddType.Text.Trim()
        Dim driver As String = txtAddDriver.Text.Trim()
        Dim vendor As String = txtAddVendor.Text.Trim()

        ' Always use the currently logged-in employee as the registering owner
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim dept As String = If(Session("Department") IsNot Nothing, Session("Department").ToString(), "")


        If ownershipType = "Personal" Then
            vehicleType = "Car"
            driver = ""
            vendor = ""
        End If

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

        ' Validate compulsory uploads
        Dim rcFile As System.Web.HttpPostedFile = Request.Files("docFile_VEHICLE_RC")
        Dim insFile As System.Web.HttpPostedFile = Request.Files("docFile_INSURANCE")
        Dim ageFile As System.Web.HttpPostedFile = Request.Files("docFile_AGE_DETERMINATION")

        If rcFile Is Nothing OrElse rcFile.ContentLength = 0 Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Registration Card (RC) PDF file is required.');", True)
            Return
        End If

        If insFile Is Nothing OrElse insFile.ContentLength = 0 Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Vehicle Insurance PDF file is required.');", True)
            Return
        End If

        Dim insIssueStr As String = Request.Form("issueDate_INSURANCE")
        Dim insExpiryStr As String = Request.Form("expiryDate_INSURANCE")
        If String.IsNullOrEmpty(insIssueStr) OrElse String.IsNullOrEmpty(insExpiryStr) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Insurance Issue and Expiry Dates are required.');", True)
            Return
        End If

        If ageFile Is Nothing OrElse ageFile.ContentLength = 0 Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Age Determination / DOM PDF file is required.');", True)
            Return
        End If

        Dim ageIssueStr As String = Request.Form("issueDate_AGE_DETERMINATION")
        Dim ageExpiryStr As String = Request.Form("expiryDate_AGE_DETERMINATION")
        If String.IsNullOrEmpty(ageIssueStr) OrElse String.IsNullOrEmpty(ageExpiryStr) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Age Determination Issue and Expiry Dates are required.');", True)
            Return
        End If


        Try
            Dim sqlInsert As String = "INSERT INTO Vehicles (VehicleNumber, VehicleType, Department, DriverName, VendorName, QrCodeUrl, OverallStatus, IsVerified, EmployeeId, OwnershipType, CreatedAt, UpdatedAt) " &
                                      "VALUES (@Plate, @Type, @Dept, @Driver, @Vendor, '', 'PENDING', 0, @EmpId, @Ownership, datetime('now'), datetime('now'));"
            Database.ExecuteNonQuery(sqlInsert,
                New SQLiteParameter("@Plate", plate),
                New SQLiteParameter("@Type", vehicleType),
                New SQLiteParameter("@Dept", dept),
                New SQLiteParameter("@Driver", driver),
                New SQLiteParameter("@Vendor", vendor),
                New SQLiteParameter("@EmpId", empId),
                New SQLiteParameter("@Ownership", ownershipType)
            )

            Dim newVehId As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT Id FROM Vehicles WHERE VehicleNumber=@Plate", New SQLiteParameter("@Plate", plate)))
            Dim qrUrl As String = "/Verify.aspx?plate=" & Server.UrlEncode(plate)
            Database.ExecuteNonQuery("UPDATE Vehicles SET QrCodeUrl=@Url WHERE Id=@Id", New SQLiteParameter("@Url", qrUrl), New SQLiteParameter("@Id", newVehId))

            ' Upload directory for PDFs
            Dim uploadDir As String = Server.MapPath("~/App_Data/Uploads/" & newVehId.ToString() & "/")
            If Not Directory.Exists(uploadDir) Then Directory.CreateDirectory(uploadDir)

            ' 1. Save RC PDF to Documents
            Dim rcSafeName As String = "VEHICLE_RC_" & Path.GetFileName(rcFile.FileName).Replace(" ", "_")
            Dim rcSavePath As String = Path.Combine(uploadDir, rcSafeName)
            rcFile.SaveAs(rcSavePath)

            Dim rcRelPath As String = "~/App_Data/Uploads/" & newVehId.ToString() & "/" & rcSafeName
            Database.ExecuteNonQuery(
                "INSERT INTO Documents (FileName, FilePath, FileType, FileSize, UploadedBy, CreatedAt) " &
                "VALUES (@FName, @FPath, 'application/pdf', @FSize, @UpBy, datetime('now'));",
                New SQLiteParameter("@FName", rcSafeName),
                New SQLiteParameter("@FPath", rcRelPath),
                New SQLiteParameter("@FSize", rcFile.ContentLength),
                New SQLiteParameter("@UpBy", empId)
            )

            Dim rcDocId As Integer = Convert.ToInt32(Database.ExecuteScalar(
                "SELECT Id FROM Documents WHERE FilePath=@FPath ORDER BY Id DESC LIMIT 1",
                New SQLiteParameter("@FPath", rcRelPath)
            ))

            ' Link Vehicles to the RC DocumentId
            Database.ExecuteNonQuery("UPDATE Vehicles SET DocumentId=@DocId WHERE Id=@Id", New SQLiteParameter("@DocId", rcDocId), New SQLiteParameter("@Id", newVehId))

            ' 2. Process compulsory compliance documents
            Dim compulsoryDocs() As String = {"INSURANCE", "AGE_DETERMINATION"}
            For Each docType As String In compulsoryDocs
                Dim issueDateStr As String = If(docType = "INSURANCE", insIssueStr, ageIssueStr)
                Dim expiryDateStr As String = If(docType = "INSURANCE", insExpiryStr, ageExpiryStr)
                Dim postedFile As System.Web.HttpPostedFile = If(docType = "INSURANCE", insFile, ageFile)

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
                    Dim daysLeft As Integer = (parsedExpiry.Date - DateTime.Today).Days
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

                ' Save file to disk and insert to Documents
                Dim safeName As String = docType & "_" & Path.GetFileName(postedFile.FileName).Replace(" ", "_")
                Dim savePath As String = Path.Combine(uploadDir, safeName)
                postedFile.SaveAs(savePath)

                Dim relPath As String = "~/App_Data/Uploads/" & newVehId.ToString() & "/" & safeName
                Database.ExecuteNonQuery(
                    "INSERT INTO Documents (FileName, FilePath, FileType, FileSize, UploadedBy, CreatedAt) " &
                    "VALUES (@FName, @FPath, 'application/pdf', @FSize, @UpBy, datetime('now'));",
                    New SQLiteParameter("@FName", safeName),
                    New SQLiteParameter("@FPath", relPath),
                    New SQLiteParameter("@FSize", postedFile.ContentLength),
                    New SQLiteParameter("@UpBy", empId)
                )

                Dim docId As Integer = Convert.ToInt32(Database.ExecuteScalar(
                    "SELECT Id FROM Documents WHERE FilePath=@FPath ORDER BY Id DESC LIMIT 1",
                    New SQLiteParameter("@FPath", relPath)
                ))

                ' Insert compliance record linked to DocumentId
                Database.ExecuteNonQuery(
                    "INSERT INTO ComplianceRecords (VehicleId, LicenseType, LicenseNumber, IssuingAuthority, IssueDate, ExpiryDate, Status, DocumentId, IsVerified, CreatedAt, UpdatedAt) " &
                    "VALUES (@VId, @LType, NULL, NULL, @IDate, @EDate, @Status, @DocId, 0, datetime('now'), datetime('now'));",
                    New SQLiteParameter("@VId", newVehId),
                    New SQLiteParameter("@LType", docType),
                    New SQLiteParameter("@IDate", issueDate),
                    New SQLiteParameter("@EDate", expiryDate),
                    New SQLiteParameter("@Status", docStatus),
                    New SQLiteParameter("@DocId", docId)
                )
            Next

            ' 3. Process optional compliance documents
            Dim optDocCountStr As String = Request.Form("optDocCount")
            Dim optDocCount As Integer = 0
            If Integer.TryParse(optDocCountStr, optDocCount) AndAlso optDocCount > 0 Then
                For i As Integer = 1 To optDocCount
                    Dim docTypeVal As String = Request.Form("optDocType_" & i)
                    If String.IsNullOrEmpty(docTypeVal) Then Continue For

                    Dim licenseType As String = docTypeVal
                    If docTypeVal = "CUSTOM" Then
                        licenseType = Request.Form("optDocCustomName_" & i)
                        If String.IsNullOrEmpty(licenseType) Then
                            licenseType = "CUSTOM_DOCUMENT"
                        End If
                    End If

                    ' Clean licenseType name for filename use
                    Dim cleanType As String = licenseType.Replace(" ", "_").Replace("/", "_").Replace("\", "_")

                    Dim issueDateStr As String = Request.Form("optDocIssueDate_" & i)
                    Dim expiryDateStr As String = Request.Form("optDocExpiryDate_" & i)
                    Dim postedFile As System.Web.HttpPostedFile = Request.Files("optDocFile_" & i)

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
                        Dim daysLeft As Integer = (parsedExpiry.Date - DateTime.Today).Days
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

                    Dim docId As Object = DBNull.Value

                    ' If a file was uploaded, save it and create Document record
                    If postedFile IsNot Nothing AndAlso postedFile.ContentLength > 0 Then
                        Dim safeName As String = cleanType & "_" & Path.GetFileName(postedFile.FileName).Replace(" ", "_")
                        Dim savePath As String = Path.Combine(uploadDir, safeName)
                        postedFile.SaveAs(savePath)

                        Dim relPath As String = "~/App_Data/Uploads/" & newVehId.ToString() & "/" & safeName
                        Database.ExecuteNonQuery(
                            "INSERT INTO Documents (FileName, FilePath, FileType, FileSize, UploadedBy, CreatedAt) " &
                            "VALUES (@FName, @FPath, 'application/pdf', @FSize, @UpBy, datetime('now'));",
                            New SQLiteParameter("@FName", safeName),
                            New SQLiteParameter("@FPath", relPath),
                            New SQLiteParameter("@FSize", postedFile.ContentLength),
                            New SQLiteParameter("@UpBy", empId)
                        )

                        docId = Convert.ToInt32(Database.ExecuteScalar(
                            "SELECT Id FROM Documents WHERE FilePath=@FPath ORDER BY Id DESC LIMIT 1",
                            New SQLiteParameter("@FPath", relPath)
                        ))
                    End If

                    ' Insert into ComplianceRecords
                    Database.ExecuteNonQuery(
                        "INSERT INTO ComplianceRecords (VehicleId, LicenseType, LicenseNumber, IssuingAuthority, IssueDate, ExpiryDate, Status, DocumentId, IsVerified, CreatedAt, UpdatedAt) " &
                        "VALUES (@VId, @LType, NULL, NULL, @IDate, @EDate, @Status, @DocId, 0, datetime('now'), datetime('now'));",
                        New SQLiteParameter("@VId", newVehId),
                        New SQLiteParameter("@LType", licenseType),
                        New SQLiteParameter("@IDate", issueDate),
                        New SQLiteParameter("@EDate", expiryDate),
                        New SQLiteParameter("@Status", docStatus),
                        New SQLiteParameter("@DocId", docId)
                    )
                Next
            End If

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

    ' â”€â”€ Visual Helper Functions â”€â”€

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

    Public Function GetDriverVendorHtml(ByVal ownershipTypeObj As Object, ByVal driverObj As Object, ByVal vendorObj As Object) As String
        Dim ownershipType As String = If(ownershipTypeObj Is DBNull.Value OrElse String.IsNullOrEmpty(ownershipTypeObj.ToString()), "Contractual", ownershipTypeObj.ToString())
        Dim driver As String = If(driverObj Is DBNull.Value OrElse String.IsNullOrEmpty(driverObj.ToString()), "N/A", driverObj.ToString())
        Dim vendor As String = If(vendorObj Is DBNull.Value OrElse String.IsNullOrEmpty(vendorObj.ToString()), "N/A", vendorObj.ToString())

        If ownershipType = "Personal" Then
            Return "<div class=""col-span-2"">" &
                   "  <p class=""text-[9px] font-bold text-slate-400 uppercase tracking-widest"">Ownership</p>" &
                   "  <p class=""font-bold text-[#0054A6] mt-0.5"">Personal (Car)</p>" &
                   "</div>"
        Else
            Return "<div>" &
                   "  <p class=""text-[9px] font-bold text-slate-400 uppercase tracking-widest"">Driver</p>" &
                   "  <p class=""font-bold text-slate-700 truncate mt-0.5"">" & Server.HtmlEncode(driver) & "</p>" &
                   "</div>" &
                   "<div>" &
                   "  <p class=""text-[9px] font-bold text-slate-400 uppercase tracking-widest"">Vendor</p>" &
                   "  <p class=""font-bold text-slate-700 truncate mt-0.5"">" & Server.HtmlEncode(vendor) & "</p>" &
                   "</div>"
        End If
    End Function

    ' ─── Edit Vehicle Handlers ─────────────────────────────────────────────────

    Protected Sub btnOpenEditModal_Click(ByVal sender As Object, ByVal e As EventArgs)
        If ViewState("SelectedVehicleId") Is Nothing Then Return
        Dim vehicleId As Integer = Convert.ToInt32(ViewState("SelectedVehicleId"))
        
        Dim sql As String = "SELECT * FROM Vehicles WHERE Id = @Id LIMIT 1"
        Dim dt As DataTable = Database.ExecuteDataTable(sql, New SQLiteParameter("@Id", vehicleId))
        If dt.Rows.Count = 0 Then Return

        Dim row As DataRow = dt.Rows(0)
        txtEditPlate.Text = row("VehicleNumber").ToString()
        txtEditType.Text = row("VehicleType").ToString()
        txtEditDriver.Text = If(row("DriverName") Is DBNull.Value, "", row("DriverName").ToString())
        txtEditVendor.Text = If(row("VendorName") Is DBNull.Value, "", row("VendorName").ToString())

        Dim ownershipType As String = If(row("OwnershipType") Is DBNull.Value OrElse String.IsNullOrEmpty(row("OwnershipType").ToString()), "Contractual", row("OwnershipType").ToString())
        ddlEditOwnershipType.SelectedValue = ownershipType

        ' Populate/Select Department
        Dim deptVal As String = If(row("Department") Is DBNull.Value, "", row("Department").ToString())
        If Not String.IsNullOrEmpty(deptVal) AndAlso ddlEditDept.Items.FindByValue(deptVal) Is Nothing Then
            ddlEditDept.Items.Add(New ListItem(deptVal, deptVal))
        End If
        ddlEditDept.SelectedValue = deptVal

        ' Toggle visibility based on ownership
        If ownershipType = "Personal" Then
            divEditTypeContainer.Visible = False
            divEditDriverContainer.Visible = False
            divEditVendorContainer.Visible = False
        Else
            divEditTypeContainer.Visible = True
            divEditDriverContainer.Visible = True
            divEditVendorContainer.Visible = True
        End If

        pnlEditModal.Visible = True
    End Sub

    Protected Sub btnCloseEditModal_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlEditModal.Visible = False
    End Sub

    Protected Sub ddlEditOwnershipType_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
        If ddlEditOwnershipType.SelectedValue = "Personal" Then
            divEditTypeContainer.Visible = False
            divEditDriverContainer.Visible = False
            divEditVendorContainer.Visible = False
        Else
            divEditTypeContainer.Visible = True
            divEditDriverContainer.Visible = True
            divEditVendorContainer.Visible = True
        End If
    End Sub

    Protected Sub btnSaveEditVehicle_Click(ByVal sender As Object, ByVal e As EventArgs)
        If ViewState("SelectedVehicleId") Is Nothing Then Return
        Dim vehicleId As Integer = Convert.ToInt32(ViewState("SelectedVehicleId"))

        Dim ownershipType As String = ddlEditOwnershipType.SelectedValue
        Dim plate As String = txtEditPlate.Text.Trim().ToUpper()
        Dim vehicleType As String = txtEditType.Text.Trim()
        Dim driver As String = txtEditDriver.Text.Trim()
        Dim vendor As String = txtEditVendor.Text.Trim()
        Dim dept As String = ddlEditDept.SelectedValue

        If ownershipType = "Personal" Then
            vehicleType = "Car"
            driver = ""
            vendor = ""
        End If

        If String.IsNullOrEmpty(plate) OrElse String.IsNullOrEmpty(vehicleType) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Plate Number and Vehicle Type are required.');", True)
            Return
        End If

        If String.IsNullOrEmpty(dept) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Department selection is required.');", True)
            Return
        End If

        ' Validate uniqueness (exclude current vehicle)
        Dim countObj As Object = Database.ExecuteScalar(
            "SELECT COUNT(*) FROM Vehicles WHERE VehicleNumber=@Plate AND Id<>@Id",
            New SQLiteParameter("@Plate", plate),
            New SQLiteParameter("@Id", vehicleId))
        If Convert.ToInt32(countObj) > 0 Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Vehicle Plate Number is already registered by another vehicle.');", True)
            Return
        End If

        Try
            Dim sqlUpdate As String = "UPDATE Vehicles SET " &
                                      "VehicleNumber = @Plate, " &
                                      "VehicleType = @Type, " &
                                      "OwnershipType = @Ownership, " &
                                      "DriverName = @Driver, " &
                                      "VendorName = @Vendor, " &
                                      "Department = @Dept, " &
                                      "UpdatedAt = datetime('now') " &
                                      "WHERE Id = @Id;"
            Database.ExecuteNonQuery(sqlUpdate,
                New SQLiteParameter("@Plate", plate),
                New SQLiteParameter("@Type", vehicleType),
                New SQLiteParameter("@Ownership", ownershipType),
                New SQLiteParameter("@Driver", driver),
                New SQLiteParameter("@Vendor", vendor),
                New SQLiteParameter("@Dept", dept),
                New SQLiteParameter("@Id", vehicleId)
            )

            ' Log Audit Trail
            Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))
            Dim username As String = Session("EmployeeName").ToString()
            Database.ExecuteNonQuery(
                "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp, VehicleId) " &
                "VALUES (@EmpId, @User, 'VEHICLE_EDIT', 'Modified vehicle ' || @Plate || ' registration parameters.', @IP, datetime('now'), @VId);",
                New SQLiteParameter("@EmpId", empId),
                New SQLiteParameter("@User", username),
                New SQLiteParameter("@Plate", plate),
                New SQLiteParameter("@IP", Request.UserHostAddress),
                New SQLiteParameter("@VId", vehicleId)
            )

            ' Run compliance updates to recheck overall vehicle status with new dates/specs
            Compliance.UpdateVehicleStatus(vehicleId)

            pnlEditModal.Visible = False
            LoadVehicles()
            LoadVehicleDetails(vehicleId)
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Vehicle updated successfully!');", True)

        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Edit failed: " & Server.HtmlEncode(ex.Message).Replace("'", "&#39;") & "');", True)
        End Try
    End Sub
End Class

