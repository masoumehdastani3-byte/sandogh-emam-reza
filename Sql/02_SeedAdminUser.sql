SET QUOTED_IDENTIFIER ON;
GO

-- کاربر پیش‌فرض مدیر صندوق
-- کد ملی: 1000000001
-- رمز عبور: Admin1234

DECLARE @RoleId int = (SELECT TOP 1 RoleId FROM UserRole WHERE RoleName = N'مدیر صندوق' ORDER BY RoleId);

IF @RoleId IS NULL
BEGIN
    INSERT INTO UserRole (RoleName, Active, CreateDate) VALUES (N'مدیر صندوق', 1, GETDATE());
    SET @RoleId = SCOPE_IDENTITY();
END

IF NOT EXISTS (SELECT 1 FROM [User] WHERE NationalCode = N'1000000001')
BEGIN
    INSERT INTO [User] (
        RoleId, FirstName, LastName, FullName, NationalCode, Mobile,
        UserName, Password, UserGender, UserStatusLife, UserActiveType, UserStatus,
        HasBaleApp, Active, CreateDate
    ) VALUES (
        @RoleId, N'مدیر', N'سیستم', N'مدیر سیستم', N'1000000001', N'09120000001',
        N'1000000001', N'Admin1234', 2, 1, 1, 5,
        1, 1, GETDATE()
    );
END
ELSE
BEGIN
    UPDATE [User]
    SET RoleId = @RoleId,
        Password = N'Admin1234',
        UserStatus = 5,
        Active = 1,
        UpdateDate = GETDATE()
    WHERE NationalCode = N'1000000001';
END
