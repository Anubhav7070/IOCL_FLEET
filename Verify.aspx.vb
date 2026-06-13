Imports System
Imports System.Data
Imports System.Data.SQLite
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.WebControls

Public Class VerifyPage
    Inherits Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            Dim idVal As String = Request.QueryString("id")
            Dim plateVal As String = Request.QueryString("plate")

            If Not String.IsNullOrEmpty(idVal) Then
                Dim vehId As Integer
                If Integer.TryParse(idVal, vehId) Then
                    ProcessVerificationById(vehId)
                Else
                    ShowErrorCard("INVALID QR SIGNATURE ID")
                End If
            ElseIf Not String.IsNullOrEmpty(plateVal) Then
                ProcessVerificationByPlate(plateVal.Trim().ToUpper())
            Else
                ShowErrorCard("NO CLEARANCE ID OR PLATE PROVIDED")
            End If
        End If
    End Sub

    Private Sub ProcessVerificationById(ByVal vehId As Integer)
        Try
            Dim plateSql As String = "SELECT VehicleNumber FROM Vehicles WHERE Id = " & vehId
            Dim plateObj As Object = Database.ExecuteScalar(plateSql)
            If plateObj IsNot Nothing AndAlso Not Convert.IsDBNull(plateObj) Then
                ProcessVerificationByPlate(plateObj.ToString())
            Else
                ShowErrorCard("VEHICLE ID NOT FOUND IN GATEWAY REGISTRY")
            End If
        Catch ex As Exception
            ShowErrorCard("GATE CHECK TIMEOUT")
            Console.WriteLine("Public QR verify failed: " & ex.Message)
        End Try
    End Sub

    Private Sub ProcessVerificationByPlate(ByVal plate As String)
        Try
            Dim sql As String = "SELECT v.*, v.Department As DeptName, e.EmployeeName As CreatorName, e.EmpNumber As CreatorNumber FROM Vehicles v " &
                               "INNER JOIN Employee e ON v.EmployeeId = e.EmployeeId " &
                               "WHERE v.VehicleNumber = @Plate LIMIT 1"
            Dim dt As DataTable = Database.ExecuteDataTable(sql, New SQLiteParameter("@Plate", plate))
            
            If dt.Rows.Count = 0 Then
                ShowErrorCard("VEHICLE PLATFORM CHECK FAILED - UNREGISTERED")
                Return
            End If

            Dim row As DataRow = dt.Rows(0)
            Dim vehId As Integer = Convert.ToInt32(row("Id"))

            ' Ensure status is dynamically recalculated
            Compliance.UpdateVehicleStatus(vehId)

            ' Re-query vehicle overall status
            Dim dtUpdated As DataTable = Database.ExecuteDataTable("SELECT OverallStatus, IsVerified FROM Vehicles WHERE Id = " & vehId)
            Dim overallStatus As String = dtUpdated.Rows(0)("OverallStatus").ToString()
            Dim isVerified As Boolean = Convert.ToBoolean(dtUpdated.Rows(0)("IsVerified"))

            pnlError.Visible = False
            pnlVerify.Visible = True

            lblPlate.Text = row("VehicleNumber").ToString()
            lblCategory.Text = row("VehicleType").ToString()
            lblDept.Text = row("DeptName").ToString()

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

            ' Clearance Rule: Approved ONLY if fully compliant and verified
            If overallStatus = "FULLY_COMPLIANT" AndAlso isVerified Then
                pnlClearedBadge.Visible = True
                pnlDeniedBadge.Visible = False
            Else
                pnlClearedBadge.Visible = False
                pnlDeniedBadge.Visible = True
            End If

            ' Load checklist
            Dim dtDocs As DataTable = Database.ExecuteDataTable("SELECT LicenseType, LicenseNumber, ExpiryDate, Status FROM ComplianceRecords WHERE VehicleId = " & vehId & " ORDER BY LicenseType")
            rptCompliance.DataSource = dtDocs
            rptCompliance.DataBind()

        Catch ex As Exception
            ShowErrorCard("GATE CHECK EXCEPTION")
            Console.WriteLine("Public verify by plate failed: " & ex.Message)
        End Try
    End Sub

    Private Sub ShowErrorCard(ByVal message As String)
        pnlVerify.Visible = False
        pnlError.Visible = True
        lblErrorMsg.Text = message
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
                Return "bg-orange-450"
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
                Return "text-emerald-400"
            Case "WARNING"
                Return "text-yellow-400"
            Case "MEDIUM_CRITICAL"
                Return "text-orange-405"
            Case "HIGH_CRITICAL"
                Return "text-orange-500"
            Case "EXPIRED"
                Return "text-red-400"
            Case Else
                Return "text-slate-400"
        End Select
    End Function
End Class
