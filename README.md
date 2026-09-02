# صندوق قرض‌الحسنه (GoodLoan)

سامانه عضویت، صف وام، اقساط ۱۰ ماهه، درگاه زرین‌پال و پیام‌رسان بله.

## راه‌اندازی

1. Connection string را در `WebApplication/appsettings.json` تنظیم کنید.
2. مقادیر `ZarinPal:MerchantId` و `Bale:BotToken` را وارد کنید.
3. پروژه را اجرا کنید:

```bash
cd GoodLoan/WebApplication
dotnet run
```

4. صفحات:
- `/` خانه
- `/register.html` عضویت
- `/login.html` ورود
- `/panel.html` پنل اعضا
- `/profile.html` پروفایل و کارت ملی
- `/admin.html` پنل مدیران
- `/swagger` API

در استارت‌آپ، اسکیما ارتقا و داده اولیه (نقش‌ها و تنظیمات) اعمال می‌شود.

## نقش‌ها

| RoleId | نقش |
|--------|-----|
| 1 | مدیر صندوق |
| 2 | عضو / وام‌گیرنده |
| 3 | خیر |

## تنظیمات مهم

- `MaxLoanAmount` — سقف وام
- `DefaultInstallmentCount` — پیش‌فرض ۱۰ قسط
- `FundInitialBalance` — موجودی اولیه دفترکل صندوق

## کوئری‌های آماده

فایل `Sql/Queries_And_Seed.sql`
