Imports System
Imports System.IO
Imports System.Data
Imports System.Data.SQLite
Imports System.Web
Imports System.Configuration

Public Class Database
    Private Shared _dbPath As String

    Shared Sub New()
        ' Determine physical database directory
        Dim appPath As String = AppDomain.CurrentDomain.BaseDirectory
        Dim appDataPath As String = Path.Combine(appPath, "App_Data")
        If Not Directory.Exists(appDataPath) Then
            Directory.CreateDirectory(appDataPath)
        End If
        ' Set DataDirectory for |DataDirectory| macro
        AppDomain.CurrentDomain.SetData("DataDirectory", appDataPath)
        _dbPath = Path.Combine(appDataPath, "iocl_compliance_forms.db")
    End Sub

    Public Shared Function GetConnectionString() As String
        Return ConfigurationManager.ConnectionStrings("SQLiteDB").ConnectionString
    End Function

    Public Shared Function GetConnection() As SQLiteConnection
        Dim conn As New SQLiteConnection(GetConnectionString())
        conn.Open()
        Return conn
    End Function

    Public Shared Function ExecuteNonQuery(ByVal sql As String, ByVal ParamArray parameters() As SQLiteParameter) As Integer
        Using conn As SQLiteConnection = GetConnection()
            Using cmd As New SQLiteCommand(sql, conn)
                If parameters IsNot Nothing Then
                    cmd.Parameters.AddRange(parameters)
                End If
                Return cmd.ExecuteNonQuery()
            End Using
        End Using
    End Function

    Public Shared Function ExecuteScalar(ByVal sql As String, ByVal ParamArray parameters() As SQLiteParameter) As Object
        Using conn As SQLiteConnection = GetConnection()
            Using cmd As New SQLiteCommand(sql, conn)
                If parameters IsNot Nothing Then
                    cmd.Parameters.AddRange(parameters)
                End If
                Return cmd.ExecuteScalar()
            End Using
        End Using
    End Function

    Public Shared Function ExecuteDataTable(ByVal sql As String, ByVal ParamArray parameters() As SQLiteParameter) As DataTable
        Dim dt As New DataTable()
        Using conn As SQLiteConnection = GetConnection()
            Using cmd As New SQLiteCommand(sql, conn)
                If parameters IsNot Nothing Then
                    cmd.Parameters.AddRange(parameters)
                End If
                Using adapter As New SQLiteDataAdapter(cmd)
                    adapter.Fill(dt)
                End Using
            End Using
        End Using
        Return dt
    End Function

    ' DB schema creation script
    Public Shared Sub InitializeDatabase()
        If File.Exists(_dbPath) Then Return

        WriteHostInfo("Initializing database for the first time...")
        SQLiteConnection.CreateFile(_dbPath)

        Using conn As SQLiteConnection = GetConnection()
            Using trans = conn.BeginTransaction()
                Try
                    ' 1. Departments Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS Departments (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT UNIQUE NOT NULL, Code TEXT UNIQUE NOT NULL, Description TEXT, Division TEXT DEFAULT 'Panipat Refinery', ComplianceScore REAL DEFAULT 100.0, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP);")

                    ' 2. Employee Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS Employee (EmployeeId INTEGER PRIMARY KEY AUTOINCREMENT, EmpNumber TEXT UNIQUE NOT NULL, EmployeeName TEXT NOT NULL, Department TEXT NOT NULL, Designation TEXT NOT NULL, EmailId TEXT NOT NULL, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP);")

                    ' 3. Authentication Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS Authentication (AuthenticationId INTEGER PRIMARY KEY AUTOINCREMENT, EmployeeId INTEGER NOT NULL, EmployeeName TEXT NOT NULL, Role TEXT CHECK(Role IN ('SuperAdmin', 'DEPT_ADMIN', 'GATEMAN', 'VIEWER', 'Employee')) NOT NULL, Password TEXT NOT NULL, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY(EmployeeId) REFERENCES Employee(EmployeeId) ON DELETE CASCADE);")

                    ' 4. Documents Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS Documents (Id INTEGER PRIMARY KEY AUTOINCREMENT, FileName TEXT NOT NULL, FilePath TEXT NOT NULL, FileType TEXT, FileSize INTEGER, UploadedBy INTEGER, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY(UploadedBy) REFERENCES Employee(EmployeeId) ON DELETE SET NULL);")

                    ' 5. Vehicles Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS Vehicles (Id INTEGER PRIMARY KEY AUTOINCREMENT, VehicleNumber TEXT UNIQUE NOT NULL, VehicleType TEXT NOT NULL, DepartmentId INTEGER NOT NULL, DriverName TEXT, VendorName TEXT, QrCodeUrl TEXT, OverallStatus TEXT DEFAULT 'FULLY_COMPLIANT', DocumentId INTEGER, LastUpdatedBy TEXT, LastUpdatedTimestamp TEXT, IsVerified INTEGER DEFAULT 0, VerifiedBy TEXT, EmployeeId INTEGER NOT NULL, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY(DepartmentId) REFERENCES Departments(Id) ON DELETE RESTRICT, FOREIGN KEY(DocumentId) REFERENCES Documents(Id) ON DELETE SET NULL, FOREIGN KEY(EmployeeId) REFERENCES Employee(EmployeeId) ON DELETE CASCADE);")

                    ' 6. ComplianceRecords Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS ComplianceRecords (Id INTEGER PRIMARY KEY AUTOINCREMENT, VehicleId INTEGER NOT NULL, LicenseType TEXT NOT NULL, LicenseNumber TEXT, IssuingAuthority TEXT, IssueDate TEXT, ExpiryDate TEXT, Status TEXT DEFAULT 'ACTIVE', DocumentId INTEGER, LastUpdatedBy TEXT, LastUpdatedTimestamp TEXT, IsVerified INTEGER DEFAULT 0, VerifiedBy TEXT, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY(VehicleId) REFERENCES Vehicles(Id) ON DELETE CASCADE, FOREIGN KEY(DocumentId) REFERENCES Documents(Id) ON DELETE SET NULL);")

                    ' 7. RenewalHistories Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS RenewalHistories (Id INTEGER PRIMARY KEY AUTOINCREMENT, VehicleId INTEGER NOT NULL, ComplianceRecordId INTEGER NOT NULL, LicenseType TEXT NOT NULL, OldExpiryDate TEXT, NewExpiryDate TEXT, OldDocumentId INTEGER, NewDocumentId INTEGER, RenewedBy INTEGER, RenewedAt TEXT DEFAULT CURRENT_TIMESTAMP, Remarks TEXT, FOREIGN KEY(VehicleId) REFERENCES Vehicles(Id) ON DELETE CASCADE, FOREIGN KEY(ComplianceRecordId) REFERENCES ComplianceRecords(Id) ON DELETE CASCADE, FOREIGN KEY(OldDocumentId) REFERENCES Documents(Id) ON DELETE SET NULL, FOREIGN KEY(NewDocumentId) REFERENCES Documents(Id) ON DELETE SET NULL, FOREIGN KEY(RenewedBy) REFERENCES Employee(EmployeeId) ON DELETE SET NULL);")

                    ' 8. Notifications Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS Notifications (Id INTEGER PRIMARY KEY AUTOINCREMENT, VehicleId INTEGER, DepartmentId INTEGER, Title TEXT NOT NULL, Message TEXT NOT NULL, Status TEXT DEFAULT 'UNREAD', Type TEXT NOT NULL, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY(VehicleId) REFERENCES Vehicles(Id) ON DELETE SET NULL, FOREIGN KEY(DepartmentId) REFERENCES Departments(Id) ON DELETE SET NULL);")

                    ' 9. AuditLogs Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS AuditLogs (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER, Username TEXT, Action TEXT NOT NULL, Description TEXT, OldValue TEXT, NewValue TEXT, IpAddress TEXT, Timestamp TEXT DEFAULT CURRENT_TIMESTAMP, VehicleId INTEGER, DepartmentId INTEGER, FOREIGN KEY(UserId) REFERENCES Employee(EmployeeId) ON DELETE SET NULL);")

                    ' 10. Reports Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS Reports (Id INTEGER PRIMARY KEY AUTOINCREMENT, Title TEXT NOT NULL, Type TEXT NOT NULL, DepartmentId INTEGER, GeneratedBy INTEGER, FilePath TEXT NOT NULL, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY(DepartmentId) REFERENCES Departments(Id) ON DELETE SET NULL, FOREIGN KEY(GeneratedBy) REFERENCES Employee(EmployeeId) ON DELETE SET NULL);")

                    trans.Commit()
                Catch ex As Exception
                    trans.Rollback()
                    Throw ex
                End Try
            End Using
        End Using
    End Sub

    Private Shared Sub ExecuteSql(ByVal conn As SQLiteConnection, ByVal sql As String)
        Using cmd As New SQLiteCommand(sql, conn)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    Private Shared Sub WriteHostInfo(ByVal msg As String)
        Console.WriteLine("[DB INFO] " & msg)
    End Sub
End Class
