# Docker'a API projemizi nasýl derleyeceðini ve nasýl çalýþtýracaðýný söyleyecek.


# 1- .NET SDK ile temel görüntüyü kullanarak bir yapý aþamasý oluþturun, derleme aþamasý
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# WORKDIR=> Container içerisindeki çalýþma klasörünü belirler bizim kaynak kodlarýn bulunduðu klasör adýný yazdýk bunlarý kopyalýyor ve derliyor
WORKDIR /src

# 2- Proje dosyalarýný kopyalayýn
# COPY=> Proje dosyalarýný Docker ortamýna aktarýr.
# Ýlk nokta: Bilgisayarýmýzdaki Docker build context (CargoTracking ana klasörü).
# Ýkinci nokta: Container içerisindeki /src klasörü.
COPY . .

# 3- Uygulama kaynak kodunu kopyalayýn ve derleyin, API projesini derle ve yayýnla
RUN dotnet publish CargoTracking.API/CargoTracking.API.csproj -c Release -o /app/publish

# 4- .NET çalýþma zamaný görüntüsünü kullanarak bir çalýþma aþamasý oluþturun
FROM mcr.microsoft.com/dotnet/aspnet:9.0 as final
# Ýlk workdýrda kopyalanýp derlenen ve derleme sonrasýnda oluþan dosyalarý bu klasörde açar ve buradan çalýþtýrýr.
WORKDIR /app
# Derleme aþamasýnda oluþan yayýn dosyalarýný çalýþma aþamasýna kopyala.
COPY --from=build /app/publish .

# 5- Uygulamayý baþlat
# ENTRYPOINT=> Container baþladýðýnda API'yi çalýþtýrýr
ENTRYPOINT ["dotnet", "CargoTracking.API.dll"]
