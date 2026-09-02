/* =============================================
   GoodLoan - Full Schema & Useful Queries
   صندوق قرض‌الحسنه
   ============================================= */

-- ---------- Roles ----------
IF NOT EXISTS (SELECT 1 FROM UserRole WHERE RoleName = N'مدیر صندوق')
    INSERT INTO UserRole (RoleName, Active, CreateDate) VALUES (N'مدیر صندوق', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM UserRole WHERE RoleName = N'عضو / وام‌گیرنده')
    INSERT INTO UserRole (RoleName, Active, CreateDate) VALUES (N'عضو / وام‌گیرنده', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM UserRole WHERE RoleName = N'خیر')
    INSERT INTO UserRole (RoleName, Active, CreateDate) VALUES (N'خیر', 1, GETDATE());

-- ---------- Settings ----------
IF NOT EXISTS (SELECT 1 FROM Setting WHERE SettingName = 'MaxLoanAmount')
    INSERT INTO Setting (SettingName, SettinCaption, SettingValue, Category, Active, CreateDate)
    VALUES ('MaxLoanAmount', N'سقف وام', '50000000', 'Loan', 1, GETDATE());

IF NOT EXISTS (SELECT 1 FROM Setting WHERE SettingName = 'DefaultInstallmentCount')
    INSERT INTO Setting (SettingName, SettinCaption, SettingValue, Category, Active, CreateDate)
    VALUES ('DefaultInstallmentCount', N'تعداد اقساط', '10', 'Loan', 1, GETDATE());

/* ========== Useful Queries ========== */

-- 1) صف انتظار وام (اولویت: زودتر درخواست داده)
-- SELECT L.LoanId, U.FullName, U.NationalCode, U.Mobile, L.LoanAmount, L.RequestDate, L.QueuePriority,
--        ROW_NUMBER() OVER (ORDER BY L.QueuePriority, L.RequestDate) AS QueuePosition
-- FROM Loan L
-- INNER JOIN [User] U ON U.UserId = L.UserId
-- WHERE L.LoanRequestStatus = 1 AND L.Active = 1
-- ORDER BY L.QueuePriority, L.RequestDate;

-- 2) موجودی فعلی صندوق
-- SELECT TOP 1 BalanceAfter AS FundBalance FROM FundTransaction ORDER BY FundTransactionId DESC;

-- 3) بدهی و اقساط یک عضو
-- DECLARE @UserId INT = 1;
-- SELECT I.*, L.LoanAmount, L.RemainAmount
-- FROM Installment I
-- INNER JOIN Loan L ON L.LoanId = I.LoanId
-- WHERE L.UserId = @UserId
-- ORDER BY I.InstallmentDate;

-- 4) جمع کمک‌های یک خیر
-- SELECT ISNULL(SUM(Amount),0) AS TotalDonation
-- FROM Payment
-- WHERE UserId = @UserId AND PaymentType = 2 AND Confirm = 1 AND GatewayStatus = 2;

-- 5) اقساط سررسید هفته آینده (برای یادآوری بله)
-- SELECT I.*, U.FullName, U.BaleChatId, U.Mobile
-- FROM Installment I
-- INNER JOIN Loan L ON L.LoanId = I.LoanId
-- INNER JOIN [User] U ON U.UserId = L.UserId
-- WHERE I.InstallmentStatus <> 1
--   AND I.InstallmentDate BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(DAY, 7, CAST(GETDATE() AS DATE));

-- 6) گزارش واریزی‌های امروز برای مدیران
-- SELECT P.*, U.FullName
-- FROM Payment P
-- LEFT JOIN [User] U ON U.UserId = P.UserId
-- WHERE CAST(P.PaymentDate AS DATE) = CAST(GETDATE() AS DATE)
--   AND P.Confirm = 1
-- ORDER BY P.PaymentDate DESC;

-- 7) تعداد افراد منتظر دریافت وام
-- SELECT COUNT(*) AS WaitingCount FROM Loan WHERE LoanRequestStatus = 1 AND Active = 1;

-- 8) وام‌گیرندگانی که می‌توان با موجودی فعلی پرداخت کرد
-- ;WITH Fund AS (
--     SELECT TOP 1 BalanceAfter FROM FundTransaction ORDER BY FundTransactionId DESC
-- ), Queue AS (
--     SELECT L.*, SUM(L.LoanAmount) OVER (ORDER BY L.QueuePriority, L.RequestDate) AS RunningTotal
--     FROM Loan L WHERE L.LoanRequestStatus = 1 AND L.Active = 1
-- )
-- SELECT Q.* FROM Queue Q CROSS JOIN Fund F WHERE Q.RunningTotal <= F.BalanceAfter;
