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
        Dim adminPasswordHash As String = BCrypt.Net.BCrypt.HashPassword("password123")

        ' Insert SuperAdmin Employee
        Database.ExecuteNonQuery("INSERT INTO Employee (EmpNumber, EmployeeName, Department, Designation, EmailId) VALUES ('10000001', 'Super Admin', '', 'Superintendent', 'singhanubhav1562@gmail.com');")
        Dim superAdminEmpId As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT EmployeeId FROM Employee WHERE EmpNumber='10000001'"))
        Database.ExecuteNonQuery("INSERT INTO Authentication (EmployeeId, EmployeeName, Role, Password) VALUES (" & superAdminEmpId & ", 'Super Admin', 'SuperAdmin', '" & adminPasswordHash & "');")

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

        ' 2. Seed Vehicles & Compliance Records (Removed completely so database starts clean with 0 vehicles)

        ' 4. Seed Audit Trail
        Database.ExecuteNonQuery("INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & superAdminEmpId & ", 'superadmin', 'DATABASE_INITIALIZATION', 'Company Vehicle Management Portal SQLite DB seeded successfully. 6 departments, 4 employees seeded.', '127.0.0.1', datetime('now'));")

        Console.WriteLine("[SEED] Seeding completed successfully.")
    End Sub
End Class
