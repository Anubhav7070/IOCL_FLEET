Imports System
Imports System.Data
Imports System.Data.SQLite
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports System.Collections.Generic
Imports System.IO

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
                LoadDecommissionedVehicles()
            End If

            If Request.QueryString("add") = "1" Then
                btnOpenAddModal_Click(Nothing, Nothing)
            End If

            Dim idVal As String = Request.QueryString("id")
            If Not String.IsNullOrEmpty(idVal) Then
                Dim vehicleId As Integer
                If Integer.TryParse(idVal, vehicleId) Then
                    ViewState("SelectedVehicleId") = vehicleId
                    LoadVehicleDetails(vehicleId)
                End If
            End If
        End If
    End Sub

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
        
        ddlDeptFilter.Items.Clear()
        ddlDeptFilter.Items.Add(New ListItem("All Divisions", ""))
        For Each row As DataRow In dt.Rows
            ddlDeptFilter.Items.Add(New ListItem(row("Code").ToString(), row("Code").ToString()))
        Next
    End Sub

    Private Sub LoadVehicles()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Dim sql As String = "SELECT v.Id, v.VehicleNumber, v.VehicleType, v.OverallStatus, " &
                           "v.Department As DeptCode, v.Department As DeptName " &
                           "FROM Vehicles v"
        
        Dim whereClauses As New List(Of String)()
        Dim parameters As New List(Of SQLiteParameter)()

        ' Exclude decommissioned vehicles
        whereClauses.Add("(v.IsDecommissioned = 0 OR v.IsDecommissioned IS NULL)")

        If role = "Employee" Then
            whereClauses.Add("v.EmployeeId = @EmpId")
            parameters.Add(New SQLiteParameter("@EmpId", empId))
        ElseIf role = "DEPT_ADMIN" Then
            Dim deptScope As String = If(Session("Department") IsNot Nothing, Session("Department").ToString(), "")
            If Not String.IsNullOrEmpty(deptScope) Then
                whereClauses.Add("v.Department = @DeptScope")
                parameters.Add(New SQLiteParameter("@DeptScope", deptScope))
            End If
        End If

        If Not String.IsNullOrEmpty(txtSearch.Text.Trim()) Then
            whereClauses.Add("(v.VehicleNumber LIKE @Search OR v.VehicleType LIKE @Search)")
            parameters.Add(New SQLiteParameter("@Search", "%" & txtSearch.Text.Trim() & "%"))
        End If

        If Not String.IsNullOrEmpty(ddlDeptFilter.SelectedValue) Then
            whereClauses.Add("v.Department = @Dept")
            parameters.Add(New SQLiteParameter("@Dept", ddlDeptFilter.SelectedValue))
        End If

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
        ElseIf e.CommandName = "DeleteVehicle" Then
            DecommissionVehicle(vehicleId)
        End If
    End Sub

    Private Sub LoadVehicleDetails(ByVal vehicleId As Integer)
        Dim sql As String = "SELECT v.*, e.EmployeeName As CreatorName, e.EmpNumber As CreatorNumber FROM Vehicles v " &
                            "INNER JOIN Employee e ON v.CreatedBy = e.EmployeeId " &
                            "WHERE v.Id = @VehId LIMIT 1"
        
        Dim dt As DataTable = Database.ExecuteDataTable(sql, New SQLiteParameter("@VehId", vehicleId))
        If dt.Rows.Count = 0 Then Return

        Dim row As DataRow = dt.Rows(0)
        lblPlateNumber.Text = row("VehicleNumber").ToString()
        lblType.Text = row("VehicleType").ToString()
        lblCreator.Text = row("CreatorName").ToString()

        ' Query the active allocation
        Dim sqlAlloc As String = "SELECT emp.EmpNumber, emp.EmployeeName, emp.Department " &
                                 "FROM VehicleAllocations a " &
                                 "INNER JOIN Employee emp ON a.EmployeeId = emp.EmployeeId " &
                                 "WHERE a.VehicleId = @VehId AND a.Status = 'Active' LIMIT 1"
        Dim dtAlloc As DataTable = Database.ExecuteDataTable(sqlAlloc, New SQLiteParameter("@VehId", vehicleId))
        If dtAlloc.Rows.Count > 0 Then
            Dim allocRow As DataRow = dtAlloc.Rows(0)
            lblAllocatedEmployee.Text = allocRow("EmployeeName").ToString() & " (" & allocRow("EmpNumber").ToString() & ")"
            lblAllocatedDept.Text = allocRow("Department").ToString()
        Else
            lblAllocatedEmployee.Text = row("CreatorName").ToString() & " (" & row("CreatorNumber").ToString() & ") [Owner]"
            lblAllocatedDept.Text = row("OwnerDepartment").ToString()
        End If

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

        Dim creatorId As Integer = Convert.ToInt32(row("CreatedBy"))
        Dim loggedInEmpId As Integer = Convert.ToInt32(Session("EmployeeId"))
        btnDecommission.Visible = (Session("Role").ToString() = "SuperAdmin" OrElse creatorId = loggedInEmpId)
        btnOpenEdit.Visible = (Session("Role").ToString() = "SuperAdmin")

        Dim dtSlots As DataTable = Database.ExecuteDataTable("SELECT Id, VehicleId, LicenseType, LicenseNumber, ExpiryDate, Status FROM ComplianceRecords WHERE VehicleId = @VehId ORDER BY LicenseType", New SQLiteParameter("@VehId", vehicleId))
        rptComplianceSlots.DataSource = dtSlots
        rptComplianceSlots.DataBind()

        pnlDetails.Visible = True
        pnlNoDetails.Visible = False
    End Sub



    Protected Sub btnOpenAddModal_Click(ByVal sender As Object, ByVal e As EventArgs)
        txtAddPlate.Text = ""
        ddlAddType.SelectedIndex = 0
        ddlAddDept.SelectedIndex = 0
        pnlAddModal.Visible = True
        pnlMainView.Visible = False
    End Sub

    Protected Sub btnCloseAddModal_Click(ByVal sender As Object, ByVal e As EventArgs)
        Response.Redirect("~/Default.aspx", False)
        HttpContext.Current.ApplicationInstance.CompleteRequest()
    End Sub

    Protected Sub btnSaveVehicle_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim plate As String = txtAddPlate.Text.Trim().ToUpper()
        Dim vehicleType As String = ddlAddType.SelectedValue
        
        Dim dept As String = ""
        If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then
            dept = "PR - Human Resources"
        ElseIf Session("Department") IsNot Nothing Then
            dept = Session("Department").ToString()
        End If
        If String.IsNullOrEmpty(dept) Then
            dept = "PR - Human Resources"
        End If

        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        If String.IsNullOrEmpty(plate) OrElse String.IsNullOrEmpty(vehicleType) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Plate Number and Vehicle Type are required.');", True)
            Return
        End If

        Dim countObj As Object = Database.ExecuteScalar("SELECT COUNT(*) FROM Vehicles WHERE VehicleNumber=@Plate", New SQLiteParameter("@Plate", plate))
        If Convert.ToInt32(countObj) > 0 Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Vehicle Plate Number is already registered.');", True)
            Return
        End If

        ' Validate mandatory uploads
        Dim rcFile As HttpPostedFile = Request.Files("docFile_RC")
        Dim insFile As HttpPostedFile = Request.Files("docFile_INSURANCE")
        Dim puccFile As HttpPostedFile = Request.Files("docFile_PUCC")
        Dim fitnessFile As HttpPostedFile = Request.Files("docFile_FITNESS")

        If rcFile Is Nothing OrElse rcFile.ContentLength = 0 OrElse
           insFile Is Nothing OrElse insFile.ContentLength = 0 OrElse
           puccFile Is Nothing OrElse puccFile.ContentLength = 0 OrElse
           fitnessFile Is Nothing OrElse fitnessFile.ContentLength = 0 Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('All 4 mandatory documents (RC, Insurance, PUCC, and Fitness Certificate) are required.');", True)
            Return
        End If

        ' Read Dates
        Dim rcIssueStr As String = Request.Form("issueDate_RC")
        Dim rcExpiryStr As String = Request.Form("expiryDate_RC")
        Dim insIssueStr As String = Request.Form("issueDate_INSURANCE")
        Dim insExpiryStr As String = Request.Form("expiryDate_INSURANCE")
        Dim puccIssueStr As String = Request.Form("issueDate_PUCC")
        Dim puccExpiryStr As String = Request.Form("expiryDate_PUCC")
        Dim fitnessIssueStr As String = Request.Form("issueDate_FITNESS")
        Dim fitnessExpiryStr As String = Request.Form("expiryDate_FITNESS")

        If String.IsNullOrEmpty(rcIssueStr) OrElse String.IsNullOrEmpty(rcExpiryStr) OrElse
           String.IsNullOrEmpty(insIssueStr) OrElse String.IsNullOrEmpty(insExpiryStr) OrElse
           String.IsNullOrEmpty(puccIssueStr) OrElse String.IsNullOrEmpty(puccExpiryStr) OrElse
           String.IsNullOrEmpty(fitnessIssueStr) OrElse String.IsNullOrEmpty(fitnessExpiryStr) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('All issue and expiry dates are required.');", True)
            Return
        End If

        Try
            ' Insert Vehicle
            Dim sqlInsert As String = "INSERT INTO Vehicles (VehicleNumber, VehicleType, Department, OwnerDepartment, OverallStatus, IsVerified, EmployeeId, CreatedBy, CreatedAt, UpdatedAt) " &
                                      "VALUES (@Plate, @Type, @Dept, @OwnerDept, 'Valid', 0, @EmpId, @EmpId, datetime('now'), datetime('now'));"
            Database.ExecuteNonQuery(sqlInsert,
                New SQLiteParameter("@Plate", plate),
                New SQLiteParameter("@Type", vehicleType),
                New SQLiteParameter("@Dept", dept),
                New SQLiteParameter("@OwnerDept", dept),
                New SQLiteParameter("@EmpId", empId)
            )

            Dim newVehId As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT Id FROM Vehicles WHERE VehicleNumber=@Plate", New SQLiteParameter("@Plate", plate)))

            Dim uploadDir As String = Server.MapPath("~/App_Data/Uploads/" & newVehId.ToString() & "/")
            If Not Directory.Exists(uploadDir) Then Directory.CreateDirectory(uploadDir)

            Dim compulsoryDocs() As String = {"RC", "INSURANCE", "PUCC", "FITNESS"}
            For Each docType As String In compulsoryDocs
                Dim issueDateStr As String = ""
                Dim expiryDateStr As String = ""
                Dim postedFile As HttpPostedFile = Nothing

                Select Case docType
                    Case "RC"
                        issueDateStr = rcIssueStr
                        expiryDateStr = rcExpiryStr
                        postedFile = rcFile
                    Case "INSURANCE"
                        issueDateStr = insIssueStr
                        expiryDateStr = insExpiryStr
                        postedFile = insFile
                    Case "PUCC"
                        issueDateStr = puccIssueStr
                        expiryDateStr = puccExpiryStr
                        postedFile = puccFile
                    Case "FITNESS"
                        issueDateStr = fitnessIssueStr
                        expiryDateStr = fitnessExpiryStr
                        postedFile = fitnessFile
                End Select

                Dim issueDate As Object = DBNull.Value
                Dim expiryDate As Object = DBNull.Value

                Dim parsedIssue, parsedExpiry As DateTime
                If DateTime.TryParse(issueDateStr, parsedIssue) Then issueDate = parsedIssue.ToString("yyyy-MM-dd")
                If DateTime.TryParse(expiryDateStr, parsedExpiry) Then expiryDate = parsedExpiry.ToString("yyyy-MM-dd")

                ' Save file and create Documents row
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

                Dim computedStatus As String = Compliance.CalculateStatus(docType, expiryDateStr)
                Dim freq As Integer = 0
                Select Case docType.ToUpper()
                    Case "RC" : freq = 30
                    Case "INSURANCE" : freq = 5
                    Case "PUCC" : freq = 3
                    Case "FITNESS" : freq = 7
                End Select

                ' Insert Compliance Record
                Database.ExecuteNonQuery(
                    "INSERT INTO ComplianceRecords (VehicleId, EmployeeId, LicenseType, LicenseNumber, IssuingAuthority, IssueDate, ExpiryDate, ReminderFrequency, Status, DocumentId, IsVerified, CreatedAt, UpdatedAt) " &
                    "VALUES (@VId, @EmpId, @LType, @LNo, 'Govt of India', @IDate, @EDate, @Freq, @Status, @DocId, 0, datetime('now'), datetime('now'));",
                    New SQLiteParameter("@VId", newVehId),
                    New SQLiteParameter("@EmpId", empId),
                    New SQLiteParameter("@LType", docType),
                    New SQLiteParameter("@LNo", "LIC-" & docType & "-" & newVehId),
                    New SQLiteParameter("@IDate", issueDate),
                    New SQLiteParameter("@EDate", expiryDate),
                    New SQLiteParameter("@Freq", freq),
                    New SQLiteParameter("@Status", computedStatus),
                    New SQLiteParameter("@DocId", docId)
                )

                If docType = "RC" Then
                    Database.ExecuteNonQuery("UPDATE Vehicles SET DocumentId=@DocId WHERE Id=@Id", New SQLiteParameter("@DocId", docId), New SQLiteParameter("@Id", newVehId))
                End If
            Next

            ' Calculate overall status
            Compliance.UpdateVehicleStatus(newVehId)

            ' Log Audit
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

            EmailService.NotifySuperAdminsOfNewVehicle(empId, plate)

            pnlAddModal.Visible = False
            pnlMainView.Visible = True
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
            Database.ExecuteNonQuery("UPDATE Vehicles SET IsVerified = 1, VerifiedBy = @Verifier, UpdatedAt = datetime('now') WHERE Id = @Id", New SQLiteParameter("@Verifier", username), New SQLiteParameter("@Id", vehicleId))
            Database.ExecuteNonQuery("UPDATE ComplianceRecords SET IsVerified = 1, VerifiedBy = @Verifier, UpdatedAt = datetime('now') WHERE VehicleId = @Id", New SQLiteParameter("@Verifier", username), New SQLiteParameter("@Id", vehicleId))
            
            Compliance.UpdateVehicleStatus(vehicleId)

            Dim dtCreator As DataTable = Database.ExecuteDataTable("SELECT EmployeeId, VehicleNumber FROM Vehicles WHERE Id = @Id LIMIT 1", New SQLiteParameter("@Id", vehicleId))
            If dtCreator.Rows.Count > 0 Then
                Dim creatorId As Integer = Convert.ToInt32(dtCreator.Rows(0)("EmployeeId"))
                Dim plateNo As String = dtCreator.Rows(0)("VehicleNumber").ToString()
                EmailService.NotifyEmployeeOfApproval(creatorId, plateNo)
            End If

            Database.ExecuteNonQuery("INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp, VehicleId) VALUES (" & empId & ", @User, 'VEHICLE_VERIFICATION', 'Approved vehicle details verification pass.', @IP, datetime('now'), " & vehicleId & ");", New SQLiteParameter("@User", username), New SQLiteParameter("@IP", Request.UserHostAddress))

            LoadVehicleDetails(vehicleId)
            LoadVehicles()
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Vehicle compliance verified successfully!');", True)

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
            Database.ExecuteNonQuery("UPDATE Vehicles SET IsDecommissioned = 1, UpdatedAt = datetime('now') WHERE Id = " & vehicleId)

            ' Complete any active allocations for this decommissioned vehicle
            Database.ExecuteNonQuery(
                "UPDATE VehicleAllocations SET Status = 'COMPLETED', EndDate = @EndDate WHERE VehicleId = @VehId AND Status = 'ACTIVE'",
                New SQLiteParameter("@EndDate", DateTime.Today.ToString("yyyy-MM-dd")),
                New SQLiteParameter("@VehId", vehicleId))

            Database.ExecuteNonQuery("INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp, VehicleId) VALUES (" & empId & ", @User, 'VEHICLE_DECOMMISSION', 'Decommissioned vehicle ' || @Plate || '.', @IP, datetime('now'), " & vehicleId & ");", New SQLiteParameter("@User", username), New SQLiteParameter("@Plate", plate), New SQLiteParameter("@IP", Request.UserHostAddress))

            pnlDetails.Visible = False
            pnlNoDetails.Visible = True
            LoadVehicles()
            If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then
                LoadDecommissionedVehicles()
            End If
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Vehicle decommissioned and archived successfully.');", True)

        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Decommission failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    Private Sub LoadDecommissionedVehicles()
        Dim dt As DataTable = Database.ExecuteDataTable(
            "SELECT Id, VehicleNumber, VehicleType, Department, OverallStatus FROM Vehicles WHERE IsDecommissioned = 1 ORDER BY VehicleNumber")
        rptDecommissioned.DataSource = dt
        rptDecommissioned.DataBind()
    End Sub

    Protected Sub rptDecommissioned_ItemCommand(ByVal source As Object, ByVal e As RepeaterCommandEventArgs)
        If e.CommandName = "Reactivate" Then
            Dim vehicleId As Integer = Convert.ToInt32(e.CommandArgument)
            ReactivateVehicle(vehicleId)
        End If
    End Sub

    Private Sub ReactivateVehicle(ByVal vehicleId As Integer)
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim username As String = Session("EmployeeName").ToString()
        Try
            Dim plate As String = Database.ExecuteScalar("SELECT VehicleNumber FROM Vehicles WHERE Id = " & vehicleId).ToString()
            Database.ExecuteNonQuery("UPDATE Vehicles SET IsDecommissioned = 0, UpdatedAt = datetime('now') WHERE Id = " & vehicleId)
            Database.ExecuteNonQuery(
                "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp, VehicleId) VALUES (" & empId & ", @User, 'VEHICLE_REACTIVATE', 'Reactivated vehicle ' || @Plate || '.', @IP, datetime('now'), " & vehicleId & ");",
                New SQLiteParameter("@User", username),
                New SQLiteParameter("@Plate", plate),
                New SQLiteParameter("@IP", Request.UserHostAddress))
            LoadVehicles()
            LoadDecommissionedVehicles()
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Vehicle reactivated successfully and returned to active fleet.');", True)
        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Reactivation failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    Public Function GetHeaderBg(ByVal status As Object) As String
        If status Is Nothing Then Return "bg-slate-600"
        Dim s As String = status.ToString()
        Select Case s
            Case "Valid"
                Return "bg-emerald-600"
            Case "Expiring"
                Return "bg-orange-600"
            Case "Expired"
                Return "bg-red-600"
            Case Else
                Return "bg-slate-600"
        End Select
    End Function

    Public Function GetStatusBadgeClass(ByVal statusObj As Object) As String
        If statusObj Is Nothing Then Return "bg-slate-100 text-slate-700 border-slate-200"
        Dim status As String = statusObj.ToString()
        Select Case status
            Case "Valid"
                Return "bg-emerald-50 text-emerald-700 border-emerald-250"
            Case "Expiring"
                Return "bg-orange-50 text-orange-700 border-orange-200"
            Case "Expired"
                Return "bg-red-50 text-red-700 border-red-200"
            Case Else
                Return "bg-slate-50 text-slate-600 border-slate-200"
        End Select
    End Function

    Public Function GetDotColor(ByVal status As String) As String
        Select Case status
            Case "Valid"
                Return "bg-emerald-500"
            Case "Expiring"
                Return "bg-orange-500"
            Case "Expired"
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

    ' edit vehicle handlers
    Protected Sub btnOpenEditModal_Click(ByVal sender As Object, ByVal e As EventArgs)
        If ViewState("SelectedVehicleId") Is Nothing Then Return
        Dim vehicleId As Integer = Convert.ToInt32(ViewState("SelectedVehicleId"))
        
        Dim sql As String = "SELECT * FROM Vehicles WHERE Id = @Id LIMIT 1"
        Dim dt As DataTable = Database.ExecuteDataTable(sql, New SQLiteParameter("@Id", vehicleId))
        If dt.Rows.Count = 0 Then Return

        Dim row As DataRow = dt.Rows(0)
        txtEditPlate.Text = row("VehicleNumber").ToString()
        
        Try
            ddlEditType.SelectedValue = row("VehicleType").ToString()
        Catch
            ddlEditType.SelectedIndex = 0
        End Try

        Dim deptVal As String = If(row("Department") Is DBNull.Value, "", row("Department").ToString())
        If Not String.IsNullOrEmpty(deptVal) AndAlso ddlEditDept.Items.FindByValue(deptVal) Is Nothing Then
            ddlEditDept.Items.Add(New ListItem(deptVal, deptVal))
        End If
        ddlEditDept.SelectedValue = deptVal

        pnlEditModal.Visible = True
    End Sub

    Protected Sub btnCloseEditModal_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlEditModal.Visible = False
    End Sub

    Protected Sub btnSaveEditVehicle_Click(ByVal sender As Object, ByVal e As EventArgs)
        If ViewState("SelectedVehicleId") Is Nothing Then Return
        Dim vehicleId As Integer = Convert.ToInt32(ViewState("SelectedVehicleId"))

        Dim plate As String = txtEditPlate.Text.Trim().ToUpper()
        Dim vehicleType As String = ddlEditType.SelectedValue
        
        Dim dept As String = ""
        If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin" Then
            dept = "PR - Human Resources"
        ElseIf Session("Department") IsNot Nothing Then
            dept = Session("Department").ToString()
        End If
        If String.IsNullOrEmpty(dept) Then
            dept = "PR - Human Resources"
        End If

        If String.IsNullOrEmpty(plate) OrElse String.IsNullOrEmpty(vehicleType) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Plate Number and Vehicle Type are required.');", True)
            Return
        End If

        If String.IsNullOrEmpty(dept) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Department selection is required.');", True)
            Return
        End If

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
                                      "Department = @Dept, " &
                                      "UpdatedAt = datetime('now') " &
                                      "WHERE Id = @Id;"
            Database.ExecuteNonQuery(sqlUpdate,
                New SQLiteParameter("@Plate", plate),
                New SQLiteParameter("@Type", vehicleType),
                New SQLiteParameter("@Dept", dept),
                New SQLiteParameter("@Id", vehicleId)
            )

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
