# OrtakRota – Arkadaşlarla Ortak Seyahat Planlayıcı

Bir grup arkadaşın aynı gezi planı üzerinde birlikte ve eş zamanlı çalışmasını sağlayan web portalı.
BLM4581 Ağ Tabanlı Teknolojiler ve Uygulamaları dersi dönem projesidir (2026–2027 Güz).

**Grup üyeleri:** Simge Karsavuranoğlu ([@simgekar](https://github.com/simgekar)), Rana Berkdemir ([@Ranaberkdemir](https://github.com/Ranaberkdemir))

## Kullanılan teknolojiler

- ASP.NET Core MVC (.NET 10)
- Entity Framework Core 10 + PostgreSQL (Npgsql), Code-First migration
- ASP.NET Core Identity (kullanıcı ve roller)
- Bootstrap 5, Bootstrap Icons, jQuery
- Sonraki haftalarda: SignalR, Leaflet + OpenStreetMap, SortableJS, Swagger

## Proje yapısı

```
OrtakRota/
├── OrtakRota.sln
└── OrtakRota.Web/
    ├── Controllers/     MVC controller'ları
    ├── Data/            AppDbContext (EF Core)
    ├── Models/          Varlık sınıfları (Trip, TripMember, Place, Vote, ...)
    ├── Migrations/      EF Core migration dosyaları
    ├── Views/           Razor görünümleri
    └── wwwroot/         CSS, JS ve diğer statik dosyalar
```

## Veritabanı tabloları

| Tablo | İçerik |
|---|---|
| AspNetUsers (AppUser) | Kullanıcılar |
| AspNetRoles | Site rolleri (Admin, User) |
| Trips | Geziler (ad, tarih, davet kodu, kurucu) |
| TripMembers | Gezi üyelikleri ve gezi içi rol (Owner / Editor / Viewer) |
| Places | Fikir havuzundaki mekanlar (konum, kategori) |
| Votes | Mekanlara verilen +1 / –1 oylar |
| ItineraryItems | Günlük plan kartları (gün, sıra, saat) |
| Expenses | Harcamalar |
| ExpenseShares | Harcamanın kişilere düşen payları |
| ChecklistItems | Ortak “Yanımıza Alınacaklar” listesi |

## Kurulum ve çalıştırma

### 1. Gerekenler

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (kontrol: `dotnet --list-sdks`)
- [PostgreSQL](https://www.postgresql.org/download/) (kurulumda `postgres` kullanıcısı için bir şifre belirleyin)
- VS Code (C# Dev Kit eklentisiyle) veya Visual Studio 2026

### 2. Veritabanını oluşturun

pgAdmin'de ya da `psql` ile boş bir veritabanı açın:

```sql
CREATE DATABASE ortakrota;
```

### 3. Bağlantı dizesini User Secrets'a yazın

Veritabanı şifresi **depoya konmaz**. Her geliştirici kendi bilgisayarında User Secrets kullanır.
Proje klasöründe (`OrtakRota.Web`) şu komutu çalıştırın; `SIFRENIZ` yerine kendi PostgreSQL şifrenizi yazın:

```bash
cd OrtakRota.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=ortakrota;Username=postgres;Password=SIFRENIZ"
```

Visual Studio kullanıyorsanız: projeye sağ tıklayın → **Manage User Secrets** → açılan `secrets.json` dosyasına yazın:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ortakrota;Username=postgres;Password=SIFRENIZ"
  }
}
```

### 4. Migration'ları uygulayın

EF Core komut satırı aracını bir kez kurun:

```bash
dotnet tool install --global dotnet-ef --version 10.0.0
```

Ardından tabloları oluşturun:

```bash
cd OrtakRota.Web
dotnet ef database update
```

Modellerde değişiklik yaptığınızda yeni migration ekleyip veritabanını güncelleyin:

```bash
dotnet ef migrations add DegisiklikAdi
dotnet ef database update
```

### 5. Uygulamayı çalıştırın

```bash
cd OrtakRota.Web
dotnet run
```

Tarayıcıda `http://localhost:5080` adresini açın. Visual Studio'da `OrtakRota.sln` dosyasını açıp **F5** ile de çalıştırabilirsiniz.

## Haftalık ilerleme

| Hafta | Lider | Yapılanlar |
|---|---|---|
| 1 | Simge Karsavuranoğlu | Depo, proje iskeleti, EF Core + PostgreSQL, varlık sınıfları, DbContext, ilk migration, Bootstrap sayfa düzeni |
