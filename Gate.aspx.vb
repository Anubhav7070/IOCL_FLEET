Imports System
Imports System.Data
Imports System.Data.SQLite
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports System.Web.Script.Serialization
Imports System.Collections.Generic

Public Class GatePage
    Inherits Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        ' Authenticate
        If Session("EmployeeId") Is Nothing Then
            Response.Redirect("Login.aspx")
        End If

        Dim role As String = Session("Role").ToString()
        If role <> "SuperAdmin" AndAlso role <> "GATEMAN" Then
            Response.Redirect("Default.aspx")
        End If

        If Not IsPostBack Then
            ' Check query string for direct print commands
            If Request.QueryString("plate") IsNot Nothing Then
                Dim plate As String = Request.QueryString("plate").ToString()
                txtPlateCheck.Text = plate
                ProcessGateCheck(plate)
                If Request.QueryString("print") = "1" Then
                    ' Trigger print immediately
                    TriggerJavaScriptPrint()
                End If
            End If
        End If
    End Sub

    Protected Sub btnCheck_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim plate As String = txtPlateCheck.Text.Trim().ToUpper()
        If String.IsNullOrEmpty(plate) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Please enter a vehicle plate number.');", True)
            Return
        End If
        ProcessGateCheck(plate)
    End Sub

    Protected Sub btnSearchScanned_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim scVehId As String = hdnScannedVehicleId.Value
        If Not String.IsNullOrEmpty(scVehId) Then
            Dim vehId As Integer
            If Integer.TryParse(scVehId, vehId) Then
                ProcessGateCheckById(vehId)
            End If
        End If
    End Sub

    Private Sub ProcessGateCheckById(ByVal vehId As Integer)
        Try
            Dim plateSql As String = "SELECT VehicleNumber FROM Vehicles WHERE Id = " & vehId
            Dim plateObj As Object = Database.ExecuteScalar(plateSql)
            If plateObj IsNot Nothing AndAlso Not Convert.IsDBNull(plateObj) Then
                txtPlateCheck.Text = plateObj.ToString()
                ProcessGateCheck(plateObj.ToString())
            Else
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Vehicle not found.');", True)
            End If
        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Check failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    Private Sub ProcessGateCheck(ByVal plate As String)
        pnlAwaiting.Visible = False
        pnlClearance.Visible = False

        Try
            Dim sql As String = "SELECT v.*, d.Name As DeptName FROM Vehicles v INNER JOIN Departments d ON v.DepartmentId = d.Id WHERE v.VehicleNumber = @Plate LIMIT 1"
            Dim dt As DataTable = Database.ExecuteDataTable(sql, New SQLiteParameter("@Plate", plate))
            If dt.Rows.Count = 0 Then
                pnlAwaiting.Visible = True
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Vehicle not found in the registry.');", True)
                Return
            End If

            Dim row As DataRow = dt.Rows(0)
            Dim vehId As Integer = Convert.ToInt32(row("Id"))
            Dim deptId As Integer = Convert.ToInt32(row("DepartmentId"))

            hdnVehicleId.Value = vehId.ToString()
            hdnDepartmentId.Value = deptId.ToString()

            ' Recalculate status of this vehicle before checking entry
            Compliance.UpdateVehicleStatus(vehId)

            ' Query updated record
            Dim dtUpdated As DataTable = Database.ExecuteDataTable("SELECT OverallStatus, IsVerified FROM Vehicles WHERE Id = " & vehId)
            Dim overallStatus As String = dtUpdated.Rows(0)("OverallStatus").ToString()
            Dim isVerified As Boolean = Convert.ToBoolean(dtUpdated.Rows(0)("IsVerified"))

            lblVehPlate.Text = row("VehicleNumber").ToString()
            lblVehType.Text = row("VehicleType").ToString()
            lblDriverName.Text = If(row("DriverName") Is DBNull.Value OrElse String.IsNullOrEmpty(row("DriverName").ToString()), "N/A", row("DriverName").ToString())
            lblVendorName.Text = If(row("VendorName") Is DBNull.Value OrElse String.IsNullOrEmpty(row("VendorName").ToString()), "N/A", row("VendorName").ToString())
            lblDeptName.Text = row("DeptName").ToString()

            ' A vehicle is ALLOWED entry ONLY if overall status is FULLY_COMPLIANT and verified by SuperAdmin
            If overallStatus = "FULLY_COMPLIANT" AndAlso isVerified Then
                pnlClearedBadge.Visible = True
                pnlDeniedBadge.Visible = False
            Else
                pnlClearedBadge.Visible = False
                pnlDeniedBadge.Visible = True
            End If

            ' Load checklist
            Dim dtRecords As DataTable = Database.ExecuteDataTable("SELECT LicenseType, LicenseNumber, ExpiryDate, Status FROM ComplianceRecords WHERE VehicleId = " & vehId)
            rptCompliance.DataSource = dtRecords
            rptCompliance.DataBind()

            pnlClearance.Visible = True

        Catch ex As Exception
            pnlAwaiting.Visible = True
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Check failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    Protected Sub btnClear_Click(ByVal sender As Object, ByVal e As EventArgs)
        ClearCheckTerminal()
    End Sub

    Private Sub ClearCheckTerminal()
        txtPlateCheck.Text = ""
        txtRemarks.Text = ""
        hdnVehicleId.Value = ""
        hdnDepartmentId.Value = ""
        pnlClearance.Visible = False
        pnlAwaiting.Visible = True
    End Sub

    Protected Sub btnAllow_Click(ByVal sender As Object, ByVal e As EventArgs)
        SubmitGateDecision(True)
    End Sub

    Protected Sub btnDeny_Click(ByVal sender As Object, ByVal e As EventArgs)
        SubmitGateDecision(False)
    End Sub

    Private Sub SubmitGateDecision(ByVal allowed As Boolean)
        If String.IsNullOrEmpty(hdnVehicleId.Value) Then Return

        Try
            Dim userId As Integer = Convert.ToInt32(Session("EmployeeId"))
            Dim username As String = Session("EmployeeName").ToString()
            Dim vehicleId As Integer = Convert.ToInt32(hdnVehicleId.Value)
            Dim deptId As Integer = Convert.ToInt32(hdnDepartmentId.Value)
            Dim plate As String = lblVehPlate.Text
            Dim remarks As String = txtRemarks.Text.Trim()

            Dim action As String = If(allowed, "GATE_ENTRY_ALLOW", "GATE_ENTRY_DENY")
            Dim desc As String = "Gateman " & username & If(allowed, " allowed", " denied") & " entry for vehicle " & plate & ". Reason/Remarks: " & If(String.IsNullOrEmpty(remarks), "None", remarks)

            Dim sql As String = "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp, VehicleId, DepartmentId) VALUES (@UserId, @User, @Action, @Desc, @IP, datetime('now'), @VehicleId, @DeptId);"
            
            Database.ExecuteNonQuery(sql, _
                New SQLiteParameter("@UserId", userId), _
                New SQLiteParameter("@User", username), _
                New SQLiteParameter("@Action", action), _
                New SQLiteParameter("@Desc", desc), _
                New SQLiteParameter("@IP", Request.UserHostAddress), _
                New SQLiteParameter("@VehicleId", vehicleId), _
                New SQLiteParameter("@DeptId", deptId))

            Dim decisionMsg As String = If(allowed, "ALLOWED", "DENIED")
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Gate entry " & decisionMsg & " logged successfully.');", True)
            
            ClearCheckTerminal()
        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Failed to log entry decision: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    Protected Sub btnPrint_Click(ByVal sender As Object, ByVal e As EventArgs)
        TriggerJavaScriptPrint()
    End Sub

    Private Sub TriggerJavaScriptPrint()
        Dim plate As String = lblVehPlate.Text
        If String.IsNullOrEmpty(plate) Then Return

        Try
            Dim sql As String = "SELECT v.*, d.Name As DeptName FROM Vehicles v INNER JOIN Departments d ON v.DepartmentId = d.Id WHERE v.VehicleNumber = @Plate LIMIT 1"
            Dim dt As DataTable = Database.ExecuteDataTable(sql, New SQLiteParameter("@Plate", plate))
            If dt.Rows.Count = 0 Then Return

            Dim row As DataRow = dt.Rows(0)
            Dim vehId As Integer = Convert.ToInt32(row("Id"))

            Dim plateNo As String = row("VehicleNumber").ToString()
            Dim type As String = row("VehicleType").ToString()
            Dim driver As String = If(row("DriverName") Is DBNull.Value OrElse String.IsNullOrEmpty(row("DriverName").ToString()), "N/A", row("DriverName").ToString())
            Dim vendor As String = If(row("VendorName") Is DBNull.Value OrElse String.IsNullOrEmpty(row("VendorName").ToString()), "N/A", row("VendorName").ToString())
            Dim dept As String = row("DeptName").ToString()
            Dim isVerified As Boolean = Convert.ToBoolean(row("IsVerified"))
            Dim status As String = If(row("OverallStatus").ToString() = "FULLY_COMPLIANT" AndAlso isVerified, "APPROVED", "DENIED")

            ' Load compliance docs list
            Dim dtDocs As DataTable = Database.ExecuteDataTable("SELECT LicenseType, LicenseNumber, ExpiryDate, Status FROM ComplianceRecords WHERE VehicleId = " & vehId)
            
            Dim docsList As New List(Of Dictionary(Of String, String))()
            For Each r As DataRow In dtDocs.Rows
                Dim d As New Dictionary(Of String, String)()
                d("Type") = r("LicenseType").ToString().Replace("_", " ")
                d("Number") = If(r("LicenseNumber") Is DBNull.Value, "PENDING", r("LicenseNumber").ToString())
                d("Expiry") = FmtDate(r("ExpiryDate"))
                d("Status") = r("Status").ToString()
                docsList.Add(d)
            Next

            Dim serializer As New JavaScriptSerializer()
            Dim docsJson As String = serializer.Serialize(docsList)

            ' Invoke printing Javascript
            Dim jsCmd As String = "triggerPrintPass('" & plateNo & "', '" & type & "', '" & driver & "', '" & vendor & "', '" & dept & "', '" & status & "', '" & docsJson.Replace("'", "\'") & "');"
            ClientScript.RegisterStartupScript(Me.GetType(), "PrintPass", jsCmd, True)

        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Print setup failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

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

    Public Function GetDotColor(ByVal status As String) As String
        Select Case status
            Case "ACTIVE"
                Return "bg-emerald-500"
            Case "WARNING"
                Return "bg-yellow-500"
            Case "MEDIUM_CRITICAL"
                Return "bg-orange-400"
            Case "HIGH_CRITICAL"
                Return "bg-orange-600"
            Case "EXPIRED"
                Return "bg-red-500"
            Case Else
                Return "bg-slate-500"
        End Select
    End Function

    Public Function GetStatusTextColor(ByVal status As String) As String
        Select Case status
            Case "ACTIVE"
                Return "text-emerald-500"
            Case "WARNING"
                Return "text-yellow-600"
            Case "MEDIUM_CRITICAL"
                Return "text-orange-500"
            Case "HIGH_CRITICAL"
                Return "text-orange-750"
            Case "EXPIRED"
                Return "text-red-500"
            Case Else
                Return "text-slate-500"
        End Select
    End Function
End Class
