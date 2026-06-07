FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ReceiptGenerator.sln ./
COPY src/ReceiptGenerator.Domain/ReceiptGenerator.Domain.csproj src/ReceiptGenerator.Domain/
COPY src/ReceiptGenerator.Application/ReceiptGenerator.Application.csproj src/ReceiptGenerator.Application/
COPY src/ReceiptGenerator.Infrastructure/ReceiptGenerator.Infrastructure.csproj src/ReceiptGenerator.Infrastructure/
COPY src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj src/ReceiptGenerator.Api/
RUN dotnet restore src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
COPY . .
RUN dotnet publish src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj -c Release -o /out --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ReceiptGenerator.Api.dll"]
