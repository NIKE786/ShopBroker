# ShopBroker (.NET 8 · ASP.NET Core MVC · EF Core · SQL Server · Identity)

Ek hi site mein: (1) 10-product Shop + easy checkout, (2) Insurance Broker Management.

## Run
1. .NET 8 SDK + SQL Server (ya LocalDB) install ho.
2. `appsettings.json` mein `ConnectionStrings:Default` set karein.
3. `dotnet restore` then `dotnet run`
   - Pehli run par database `ShopBrokerDb` khud ban jata hai (EnsureCreated) aur seed data aa jata hai.

## Default logins
| Role   | Email             | Password   |
|--------|-------------------|------------|
| Admin  | admin@shop.com    | Admin@123  |
| Broker | broker@shop.com   | Broker@123 |
(Production se pehle password zaroor badlein.)

## Features
**Shop:** 10 seeded products, session cart, qty update/remove, guest ya login checkout (naam, phone, address, COD/UPI/Card), stock auto-reduce, order confirmation, My Orders.
**Admin:** orders list + status update, product price/stock/active edit.
**Broker:** dashboard (clients, policies, dues, overdue, premium, commission), clients CRUD, companies CRUD, policies CRUD + search/filter, premium payment entry (next due date auto-advance, commission auto-calc), dues/renewals list, commission report by date range.

## Notes
- UPI/Card abhi sirf "Awaiting Payment" status set karte hain; real payment gateway (Razorpay etc.) alag se jodna hoga.
- Migrations chahiye to Program.cs mein `EnsureCreated()` ko `Migrate()` se badal kar `dotnet ef migrations add Init` chalayein.
