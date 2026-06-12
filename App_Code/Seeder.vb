Imports System
Imports System.Data
Imports System.Data.SQLite
Imports BCrypt.Net

Public Class Seeder
    Public Shared Sub Seed()
        ' Ensure DB schema is initialized first
        Database.InitializeDatabase()

        ' Check if already seeded
        Dim count As Object = Database.ExecuteScalar("SELECT COUNT(*) FROM Employee")
        If Convert.ToInt32(count) > 0 Then Return

        Console.WriteLine("[SEED] Seeding data...")

        ' 1. Seed Employees
        Dim passwordHash As String = BCrypt.Net.BCrypt.HashPassword("password123")

        ' Insert SuperAdmin Employee
        Database.ExecuteNonQuery("INSERT INTO Employee (EmpNumber, EmployeeName, Department, Designation, EmailId) VALUES ('10000001', 'Super Admin', '', 'Superintendent', 'singhanubhav1562@gmail.com');")
        Dim superAdminEmpId As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT EmployeeId FROM Employee WHERE EmpNumber='10000001'"))
        Database.ExecuteNonQuery("INSERT INTO Authentication (EmployeeId, EmployeeName, Role, Password) VALUES (" & superAdminEmpId & ", 'Super Admin', 'SuperAdmin', '" & passwordHash & "');")

        ' Insert Employee (Logistics Manager)
        Database.ExecuteNonQuery("INSERT INTO Employee (EmpNumber, EmployeeName, Department, Designation, EmailId) VALUES ('20000001', 'Logistics Manager', 'PR - Refinery Operations', 'Manager', 'anubhav.singh0020vit@gmail.com');")
        Dim logisticsEmpId As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT EmployeeId FROM Employee WHERE EmpNumber='20000001'"))
        Database.ExecuteNonQuery("INSERT INTO Authentication (EmployeeId, EmployeeName, Role, Password) VALUES (" & logisticsEmpId & ", 'Logistics Manager', 'Employee', '" & passwordHash & "');")

        ' Insert Employee (Safety Engineer)
        Database.ExecuteNonQuery("INSERT INTO Employee (EmpNumber, EmployeeName, Department, Designation, EmailId) VALUES ('20000002', 'Safety Engineer', 'PR - Fire & Safety', 'Engineer', 'anubhav.singh0020vit@gmail.com');")
        Dim safetyEmpId As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT EmployeeId FROM Employee WHERE EmpNumber='20000002'"))
        Database.ExecuteNonQuery("INSERT INTO Authentication (EmployeeId, EmployeeName, Role, Password) VALUES (" & safetyEmpId & ", 'Safety Engineer', 'Employee', '" & passwordHash & "');")

        ' Insert Employee (Viewer Inspector)
        Database.ExecuteNonQuery("INSERT INTO Employee (EmpNumber, EmployeeName, Department, Designation, EmailId) VALUES ('30000001', 'Viewer Account', 'PR - Chemical & Laboratory', 'Inspector', 'singhanubhav1562@gmail.com');")
        Dim viewerEmpId As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT EmployeeId FROM Employee WHERE EmpNumber='30000001'"))
        Database.ExecuteNonQuery("INSERT INTO Authentication (EmployeeId, EmployeeName, Role, Password) VALUES (" & viewerEmpId & ", 'Viewer Account', 'Employee', '" & passwordHash & "');")

        ' 2. Seed Vehicles & Compliance Records
        Dim deptList() As String = {"PR - Fire & Safety", "PR - Refinery Operations", "PR - Chemical & Laboratory", "PNC - Fire & Safety", "PNC - Cracker Operations", "PNC - Chemical & Testing"}
        Dim complianceTypes() As String = {"ROAD_PERMIT", "AGE_DETERMINATION", "PUC", "FITNESS", "EXPLOSIVE", "GREEN_CARD", "INSURANCE", "CALIBRATION"}

        ' Add a dummy document reference in SQLite
        Database.ExecuteNonQuery("INSERT INTO Documents (FileName, FilePath, FileType, FileSize, UploadedBy) VALUES ('seeded_document.pdf', '/uploads/seeded/iocl_sample_doc.pdf', 'application/pdf', 1024, " & superAdminEmpId & ");")
        Dim dummyDocId As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT Id FROM Documents LIMIT 1"))

        Dim idx As Integer = 1
        For Each dept As String In deptList
            ' Vehicle A (Fully Compliant)
            Dim plateA As String = "HR26AB110" & idx
            Database.ExecuteNonQuery("INSERT INTO Vehicles (VehicleNumber, VehicleType, Department, DriverName, VendorName, QrCodeUrl, OverallStatus, DocumentId, LastUpdatedBy, LastUpdatedTimestamp, IsVerified, EmployeeId) VALUES ('" & plateA & "', 'Petroleum Tanker', '" & dept & "', 'Safe Driver " & idx & "', 'Refinery Carrier Corp', '', 'FULLY_COMPLIANT', " & dummyDocId & ", 'system', datetime('now'), 1, " & logisticsEmpId & ");")
            Dim vehIdA As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT Id FROM Vehicles WHERE VehicleNumber='" & plateA & "'"))
            ' Generate QR verification URL
            Dim qrUrlA As String = "/Verify.aspx?plate=" & plateA
            Database.ExecuteNonQuery("UPDATE Vehicles SET QrCodeUrl='" & qrUrlA & "' WHERE Id=" & vehIdA)

            For Each type As String In complianceTypes
                Database.ExecuteNonQuery("INSERT INTO ComplianceRecords (VehicleId, LicenseType, LicenseNumber, IssuingAuthority, IssueDate, ExpiryDate, Status, DocumentId, LastUpdatedBy, LastUpdatedTimestamp, IsVerified) VALUES (" & vehIdA & ", '" & type & "', 'LIC-" & type & "-" & New Random().Next(1000, 9999) & "', 'Govt of India', '2026-06-01', '2028-06-01', 'ACTIVE', " & dummyDocId & ", 'system', datetime('now'), 1);")
            Next

            ' Vehicle B (Near Expiry - PUC Warning)
            Dim plateB As String = "HR26AB990" & idx
            Database.ExecuteNonQuery("INSERT INTO Vehicles (VehicleNumber, VehicleType, Department, DriverName, VendorName, QrCodeUrl, OverallStatus, DocumentId, LastUpdatedBy, LastUpdatedTimestamp, IsVerified, EmployeeId) VALUES ('" & plateB & "', 'Cargo Truck', '" & dept & "', 'Alert Driver " & idx & "', 'Refinery Carrier Corp', '', 'WARNING', " & dummyDocId & ", 'system', datetime('now'), 1, " & logisticsEmpId & ");")
            Dim vehIdB As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT Id FROM Vehicles WHERE VehicleNumber='" & plateB & "'"))
            Dim qrUrlB As String = "/Verify.aspx?plate=" & plateB
            Database.ExecuteNonQuery("UPDATE Vehicles SET QrCodeUrl='" & qrUrlB & "' WHERE Id=" & vehIdB)

            For Each type As String In complianceTypes
                Dim isExpiringSoon As Boolean = (type = "PUC")
                Dim expiryDate As String = If(isExpiringSoon, DateTime.Today.AddDays(3).ToString("yyyy-MM-dd"), "2028-06-01")
                Dim status As String = If(isExpiringSoon, "WARNING", "ACTIVE")

                Database.ExecuteNonQuery("INSERT INTO ComplianceRecords (VehicleId, LicenseType, LicenseNumber, IssuingAuthority, IssueDate, ExpiryDate, Status, DocumentId, LastUpdatedBy, LastUpdatedTimestamp, IsVerified) VALUES (" & vehIdB & ", '" & type & "', 'LIC-" & type & "-" & New Random().Next(1000, 9999) & "', 'Govt of India', '2026-06-01', '" & expiryDate & "', '" & status & "', " & dummyDocId & ", 'system', datetime('now'), 1);")
            Next
            idx += 1
        Next

        ' 4. Seed Audit Trail
        Database.ExecuteNonQuery("INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & superAdminEmpId & ", 'superadmin', 'DATABASE_INITIALIZATION', 'IOCL Panipat Refinery Fleet Compliance SQLite DB seeded successfully. 6 departments, 4 employees seeded.', '127.0.0.1', datetime('now'));")

        Console.WriteLine("[SEED] Seeding completed successfully.")
    End Sub
End Class
